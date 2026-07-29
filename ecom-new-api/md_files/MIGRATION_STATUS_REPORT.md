# Migration Feasibility: Cart-Orders API & Interstitial Cart Page
**Prepared for:** Engineering  
**Date:** 2026-07-10 — last revised after full schema alignment pass  
**Type:** Initial feasibility assessment — bare-minimum scope to migrate and go live  
**Current stack:** PHP / Lithium Framework on SQL Server  
**Target stack:** ASP.NET Core (C#) / .NET 10 on the same SQL Server database

> **Status as of last revision:** `POST /cart/cart-orders` is working end-to-end against the local DB.  
> All schema/type mismatches have been resolved. 63/63 tests passing.  
> The write path is production-ready pending the QA smoke test and SEQUENCE → `usp_next_id` swap.

---

## What Are We Migrating?

Two tightly coupled surfaces that together form the GSM cart purchase flow:

1. **Cart-Orders API** — `POST /cart/cart-orders` at `cartapi.webroot.com`  
   The backend endpoint that creates a new cart/order record. Called by the frontend JS when a user clicks "Add to Cart."

2. **Interstitial Cart Page** (GSM Try/Buy/Upgrade Configurator)  
   The page a user lands on after entering a keycode. It fetches their license data, renders TRIAL / RENEW / ADD SEATS tabs, and submits the order. Currently a Concrete5 CMS block backed by several read-only API endpoints.

These two cannot be migrated independently — the write endpoint (`POST /cart/cart-orders`) is the final step of a flow that starts with read endpoints. Both must be working before a cutover is viable.

---

## Is It Feasible?

**Yes.** The SQL Server database and all stored procedures already exist and are stable. The PHP layer is essentially a thin orchestration wrapper over those procedures. The migration is a well-scoped rewrite of that orchestration layer — no schema changes, no new stored procedures needed for the bare minimum.

The main complexity is behavioral fidelity: validation rules are scattered across multiple PHP layers (controller filters, model validates, model save filters) and must all be consolidated into the new service layer. Nothing architecturally novel is required.

---

## Bare-Minimum Scope to Go Live

### 1. Backend Service (ASP.NET Core)

A single ASP.NET Core Web API service is needed. It must expose the following endpoints:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `POST` | `/cart/cart-orders` | Create a new cart order (write path) |
| `GET` | `/license-options` | Fetch license + available products for a keycode |
| `GET` | `/configure` | Renewal product options for a license |
| `GET` | `/upgrade` | Upgrade product options for a license |

The first endpoint is the write path. The three `GET` endpoints are what the configurator page loads before the user can interact with anything — without them the page is blank.

**Cart update** (`PUT /cart/cart-orders`) is _not_ required for the first pass but is needed if the user can modify their cart before checkout.

---

### 2. Request Pipeline (must mirror PHP behavior exactly)

The following must be implemented as middleware, in order:

| Step | What It Does | HTTP Behavior |
|------|-------------|---------------|
| Session / cart bootstrap | Resolve existing cart from `vendor_order_code` in session; inject as context | — |
| CSRF validation | Read `X-WRCART-CSRF` header (or body token); reject non-GET requests without it | `400` + refresh `csi_csrf` cookie |
| Authentication | Resolve CSI user from `X-CSI-USER` / `X-CSI-USER-ID` headers | `401` if absent |
| Permission check | Verify `cart_order.create` permission for the authenticated user | `403` if denied |
| Account context injection | Merge `username`, `csi_user_id`, `p_rc`, `trx_rc` from account into request payload | — |
| Locale injection | Inject `locale` from `X-CSI-LOCALE` header into payload | — |

---

### 3. Stored Procedures to Wire (Minimum Set)

All procedures already exist in SQL Server (`ecommerce_vh14`). The new service only needs to call them via `Microsoft.Data.SqlClient`.

**Write path — required for `POST /cart/cart-orders`:**

| Procedure | What It Does | Status |
|-----------|-------------|--------|
| `usp_cart_insert_cart_order` | Insert cart header; returns `vendor_order_code` via output params | ✅ Implemented (EF Core) |
| `usp_cart_insert_cart_order_item` | Insert each line item (called 0..N times per order) | ✅ Implemented (EF Core) |
| `usp_cart_select_cart_order` | Re-read the full cart after insert (response payload is this, not the raw insert output) | ✅ Implemented (EF Core) |
| `usp_cart_select_cart_order_item` | Read items for response hydration | ✅ Implemented (EF Core) |

**Read path — required for `GET /license-options`, `/configure`, `/upgrade`:**

| Procedure | What It Does |
|-----------|-------------|
| `usp_cart_select_message_key` | Resolve keycode to internal message key / license record |
| `usp_license_select_license_by_id` | Fetch current license details (seats, expiry, category) |
| `usp_cart_select_license_profile` | Get trial/full product profile for the key |
| `usp_product_select_license_category_upgrade` | Get available upgrade product options |
| `usp_cart_select_license_billing_model` | Fetch annual/monthly billing model tooltip data |
| `usp_partner_cart_select_order_page_details` | Full product catalog (primary + secondary products, pricing, years, seats, storage) |

**Optional for first pass (needed if cart editing is in scope):**

| Procedure | What It Does |
|-----------|-------------|
| `usp_cart_update_cart_order` | Update cart header |
| `usp_cart_update_cart_order_item` | Update/replace line items |

---

### 4. Business Logic to Reimplement

This is the bulk of the migration effort. The PHP model layer handles these behaviors inline during `save()` — they must all be made explicit in the C# service layer:

**Order-level logic:**
- Quote-key detection: if `message_key` resolves to a quote key, the create call must pivot to an **update** of the existing pending cart instead of inserting a new one
- `vendor_order_code` generation if not supplied (currently delegated to `usp_next_id` + a site prefix table)
- `user_ip` is always set server-side, never trusted from the client
- Currency resolution: use `currency_code` from payload → fall back to partner-configured currency → fall back to default (currency_id = 1)
- Sales order date defaults to today if not provided

**Validation rules to enforce (order level):**
- `site_id` required, must be in allowed set
- `locale` required
- `currency_code` must be valid ISO 4217 if provided
- `sales_order_date` date format if provided
- `vendor_order_code` non-empty if provided
- `message_campaign_id` positive integer if provided
- `message_campaign_platform` non-empty if provided
- `partner_key` UUID format if provided
- `account_user_name` non-empty if provided
- `url_link` valid absolute URL if provided

**Validation rules to enforce (item level):**
- `license_category_name` in allowed set
- `quantity` and `license_seats` positive if provided
- `years` in allowed year set
- `item_hierarchy_id` in [1, 2]
- Date fields (`start_date`, `expiration_date`, `vendor_expiration_date`) valid format if provided
- Storage/seat compatibility checks (storage options must be within configured maximums)
- Vault must be in configured vault list for the product/category

**Response construction:**
- The API response is **not** the raw stored procedure output. After insert, the service must re-read the full cart aggregate via `usp_cart_select_cart_order` + `usp_cart_select_cart_order_item` and return that as the 201 payload. The frontend depends on computed fields (currency, route, item bundles) that are only present in the re-read.

---

### 5. Frontend (Interstitial Page)

The current configurator page is a Concrete5 CMS block (`form_gsm_try_buy_configurator`). The `react-cart-ui/` directory exists in the repo and is the target for the migrated UI.

Minimum frontend work:

| Component | What Needs to Change |
|-----------|---------------------|
| Keycode entry form (`form_console_keycode`) | Port to React; on submit stores key in state and redirects to configurator route |
| Configurator page (`form_gsm_try_buy_configurator`) | Port tab rendering (TRIAL / RENEW / ADD SEATS) and product selection logic to React |
| API calls | Update `getLicenseData()` and `cartAPI.createCart()` to target the new C# service URLs |
| Credentials flow | `withCredentials: true` AJAX pattern must be preserved; CSRF token must be read from `csi_csrf` cookie and sent as `X-WRCART-CSRF` header |

The frontend cannot go live until the read endpoints (`GET /license-options`, `/configure`, `/upgrade`) are working — they are the first call the page makes.

---

## What Is Out of Scope (First Pass)

- Apache vhost config changes (ops concern, not code)
- Minified/bundled frontend artifact pipeline
- Any cart endpoints not listed above (internal_api, CSI admin flows, etc.)
- Customer auto-association (conditional path in PHP — can be a follow-on)
- Discount handling beyond default behavior
- Any stored procedure schema changes

---

## Key Risks

| Risk | Why It Matters | Status |
|------|---------------|--------|
| Quote-key path silently converts create → update | If missed, a user re-submitting a quote creates a duplicate cart instead of updating | ⚠ Detection implemented (`FindExistingVendorOrderCodeByKeyAsync`); UPDATE path not yet wired — INSERT is always taken |
| Validation scattered across 3 PHP layers | Easy to miss a rule; broken validation = bad orders in DB | ✅ Consolidated in `CartOrderService.ValidateCreateRequest()` |
| Response is a computed aggregate | Returning the raw insert result will break the frontend silently | ✅ Always re-reads via `SelectCartOrderAsync` after insert |
| DB credentials currently in plaintext | `etc/connections.dev.json` contains the server password in plain text | ⚠ Must move to environment secrets / Key Vault before any deployment |
| New service DB identity needs EXEC grants | PHP app user has permissions; C# service will run as a different identity | ⚠ DBA must grant EXEC on all listed stored procedures to the new service account |
| Frontend session → stateless header handoff | PHP used server-side session for `vendor_order_code`; the new service must accept it via request context or header | ⚠ Design this handoff before starting frontend work |
| `tinyint`/`byte` type mismatches | SQL tinyint cast to int32 causes `InvalidCastException` at EF materialization | ✅ Resolved — all tinyint columns mapped to `byte` in C# entities |
| `uniqueidentifier`/`Guid` for message_key | String → Guid mismatch caused insert failures | ✅ Resolved — `CartOrderMessage.MessageKey` is `Guid`; local DB patched |
| Duplicate `vendor_order_code` in local dev DB | Repeated test runs caused UNIQUE constraint violations after patch | ✅ Resolved — patch_002 cleans up duplicates and adds UNIQUE constraint |
| `FLOAT`/`double` mismatch on `product_years.years` | Caused `InvalidCastException` in item select path | ✅ Resolved — `ProductYears.Years` changed to `double` |

---

## Source Files Being Replaced

These are the PHP files whose behavior the new service must replicate. Nothing in this list needs to be modified — they are read-only references.

**API routing & bootstrap:**
- `apps/partner_api/config/routes.php`
- `apps/csi/config/routes/cart.php`
- `apps/csi/config/bootstrap/session.php`
- `apps/csi/config/bootstrap/auth.php`

**Controllers:**
- `apps/csi/controllers/AccountAwareRestController.php`
- `apps/csi/controllers/RestController.php`
- `apps/partner_api/controllers/CartLicenseOptionController.php`
- `li3_wr_api/controllers/RestController.php`

**Models (business logic to port):**
- `li3_wr/models/cart_order/Order.php`
- `li3_wr/models/cart_order/Item.php`
- `li3_wr/models/cart_order/PartnerCartNewOption.php`
- `li3_wr/models/cart_order/LicenseProfile.php`
- `li3_wr/models/cart_order/BillingModel.php`

**Stored procedure wrappers (to replace with C# repository methods):**
- `li3_wr/extensions/procedures/cart_order/insert/Order.php`
- `li3_wr/extensions/procedures/cart_order/insert/Item.php`
- `li3_wr/extensions/procedures/cart_order/select/Order.php`
- `li3_wr/extensions/procedures/cart_order/select/Item.php`
- `li3_wr/extensions/procedures/cart_order/select/PartnerOrderPageDetails.php`
- `li3_wr/extensions/procedures/cart_order/select/MessageKey.php`
- `li3_wr/extensions/procedures/cart_order/select/LicenseProfile.php`
- `li3_wr/extensions/procedures/cart_order/select/BillingModel.php`
- `li3_wr/extensions/procedures/license/select/CategoryUpgrade.php`
- `li3_wr/extensions/procedures/license/select/SelectByLicenseId.php`

**Frontend blocks (to replace with React):**
- `concrete5-cms/src/application/blocks/form_console_keycode/`
- `concrete5-cms/src/application/blocks/form_gsm_try_buy_configurator/`

---

## Reference Documents

| Document | What It Contains |
|----------|-----------------|
| [CART_CART_ORDERS_CSHARP_PORTING.md](CART_CART_ORDERS_CSHARP_PORTING.md) | Full PHP behavior spec, validation matrix, DB operations, response semantics for `POST /cart/cart-orders` |
| [FLOW_AND_FILES.md](FLOW_AND_FILES.md) | End-to-end flow of the interstitial cart page, step by step with file references |
| [DATABASE_MODEL_LAYER.md](DATABASE_MODEL_LAYER.md) | DB connection config, ORM patterns, how stored procedures are invoked from PHP |
| [C_SHARP_PORTING_SPECIFICS.md](C_SHARP_PORTING_SPECIFICS.md) | Complete file list, stored procedure checklist, and suggested migration order |
| [usp_cart_insert_cart_order.md](usp_cart_insert_cart_order.md) | Full source of the cart header insert stored procedure |
