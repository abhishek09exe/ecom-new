# Architecture - Current Implementation State

> Last updated after full schema alignment pass (POST /cart/cart-orders end-to-end verified working).
> All NOT NULL schema gaps filled, all EF Core type mismatches resolved, 63/63 tests passing.
> `POST /cart/cart-orders` successfully inserts and returns a full cart order response.
> Remaining work: pricing/date derivation logic (SEC 1–4 of usp_cart_insert_cart_order_item), G10–G13 insert correctness, equivalent_year_price exact computation, P3 endpoints.

---

## 1. End-to-End Flow

```
FRONTEND  (PHP / Webroot - existing)
  |
  |  Page Load GETs (frontend-owned, NOT this API):
  |    GET /license-options  -> product catalog, license profiles
  |    GET /bundle-pricing   -> unit_price, list_price, discounts, dates  <-- prices sent to backend
  |    GET /ecom-token       -> CSRF token for subsequent POST
  |
  |  User clicks "Add to Cart":
  |    POST /cart/cart-orders   (body carries pre-computed prices from /bundle-pricing)
  |
  v

C# API  (ecom-new-api)
  |
  |  CartOrdersController  [Route: "cart"]
  |  - Injects UserIp from HttpContext.Connection.RemoteIpAddress
  |  - POST /cart/cart-orders  ->  _service.CreateCartOrderAsync()
  |  - Returns 201 / 400 / 401 / 403 / 500
  |
  v

  CartOrderService
  |
  |  1. ValidateCreateRequest()
  |       - site_id required + in AllowedSiteIds
  |       - locale required (e.g. "en_US")
  |       - currency_code must be 3-char ISO 4217
  |       - partner_key must be valid UUID if provided
  |       - items[]: license_category_name, quantity, license_seats, years validated
  |
  |  2. Quote-key pivot check  (G14 - now live)
  |       FindExistingVendorOrderCodeByKeyAsync(request.Key)
  |         -> queries cart_order_message by message_key GUID
  |         -> returns existing vendor_order_code  (UPDATE path - not yet wired)
  |         -> returns null                        (INSERT path - current flow)
  |
  |  3. InsertCartOrderAsync(request)  -> vendorOrderCode
  |
  |  4. SelectCartOrderAsync(vendorOrderCode)  -> CartOrderResponse
  |
  v

  EfCartOrderRepository
  (see Section 3 for full logic walkthrough)
  |
  v

SQL Server  (ecom_cart_dev local / ecommerce_VH14 QA)
  |
  |  Tables written on every POST:
  |    cart_order              - order header (1 row)
  |    cart_order_item         - one row per item in request
  |    cart_json               - serialized extension fields blob
  |    cart_order_partner      - partner link if partner_key resolves  (optional)
  |    cart_order_route        - routing_action row if provided        (optional)  [G4]
  |    cart_order_message      - message_key row if key is a GUID      (optional)  [G5]
  |    cart_order_item_json    - per-item dimensions JSON blob          (always)    [G8]
  |    cart_order_item_license - keycode link per item if keycode sent  (optional)  [G9]
  |
  |  Tables read for setup:
  |    cart_site_id_order_code_prefix  - vendor_order_code prefix           [G2]
  |    partner_configuration_partner   - partner default currency            [G6]
  |    license_key                     - license_id lookup for message_key   [G5]
  |
  |  Tables read for response:
  |    currency, partner, product
  |
  v

HTTP 201 Created  ->  CartOrderResponse (JSON)
```

---

## 2. Project Structure

```
ecom-new-api/
|
+-- Controllers/
|   +-- CartOrdersController.cs           POST /cart/cart-orders entry point
|
+-- Services/
|   +-- CartOrderService.cs               validation + orchestration + pivot check
|   +-- ICartOrderService.cs
|   +-- ServiceResult.cs
|
+-- Repositories/
|   +-- ICartOrderRepository.cs           contract: InsertCartOrderAsync / SelectCartOrderAsync / FindExistingVendorOrderCodeByKeyAsync
|   +-- CartOrderRepository.cs            EF Core implementation (replaces EfCartOrderRepository.cs after pull)
|                                         methods: InsertCartOrderHeaderAsync, InsertCartOrderItemAsync,
|                                                  SelectCartOrderHeaderAsync, SelectCartOrderItemsAsync,
|                                                  FindExistingVendorOrderCodeByKeyAsync
|
+-- Data/
|   +-- AppDbContext.cs                   all EF Fluent mappings
|   +-- Entities/
|       +-- CartOrder.cs
|       +-- CartOrderItem.cs
|       +-- CartJson.cs
|       +-- CartOrderPartner.cs
|       +-- CartOrderRoute.cs             [G4]
|       +-- CartOrderMessage.cs           [G5]
|       +-- CartOrderItemJson.cs          [G8]
|       +-- CartOrderItemLicense.cs       [G9]
|       +-- CartOrderItemJsonLog.cs       [G10 - table mapped, write not yet called]
|       +-- Currency.cs
|       +-- CartOrderStatus.cs
|       +-- Partner.cs
|       +-- PartnerAccount.cs             [now mapped + used in partner_account JOIN]
|       +-- Account.cs                    [now mapped + used in partner_account JOIN]
|       +-- PartnerConfigurationPartner.cs  [G6]
|       +-- LicenseKey.cs                 [G5]
|       +-- LicenseKeycodeType.cs         [G16]
|       +-- LicenseAttributeLicenseValue.cs [G21]
|       +-- LicenseCategory.cs
|       +-- CartSiteIdOrderCodePrefix.cs  [G2]
|       +-- Product.cs
|       +-- ProductType.cs                [G16]
|       +-- ProductFamily.cs              [G16]
|       +-- ProductLine.cs                [G16]
|       +-- ProductLineProduct.cs         [G16]
|       +-- ProductYears.cs               [G21]
|       +-- ProductSeat.cs                [G21]
|       +-- ProductLicenseCategory.cs     [G17]
|       +-- ProductPricing.cs             [G20 - basic fallback price read]
|       +-- NextIdResult.cs               [sequence helper]
|
+-- Infrastructure/
|   +-- SnakeCaseNamingPolicy.cs
|
+-- Models/
|   +-- Requests/
|   |   +-- CartOrderCreateRequest.cs
|   |   +-- CartOrderItemRequest.cs
|   +-- Responses/
|       +-- CartOrderResponse.cs          header + grouped items dict + formatted price strings
|       +-- CartOrderItemResponse.cs      full item response (all SP columns now mapped)
|       +-- CartOrderPartnerResponse.cs   partner sub-response
|       +-- ApiResponse.cs
|       +-- ReadEndpointResponses.cs
|
+-- sql/
	+-- local_dev_setup.sql               34 tables; message_key=UNIQUEIDENTIFIER, vendor_order_code UNIQUE
	+-- patches/
		+-- patch_001_cart_order_message_key_guid.sql      VARCHAR(36) → UNIQUEIDENTIFIER migration
		+-- patch_002_cart_order_vendor_order_code_unique.sql  deduplicate + add UNIQUE constraint
```

---

## 3. CartOrderRepository — Logic Walkthrough

The repository is now split into 4 methods matching the 4 SPs exactly.

### InsertCartOrderHeaderAsync  ← `usp_cart_insert_cart_order`

```
1. Resolve partner_id from partner_key GUID (optional)
2. Resolve currency_id:
   a. Direct match on currency_code from request
   b. Fallback: partner_configuration_partner WHERE partner_configuration_id=15  [G6]
      Note: PartnerConfigurationId is byte (tinyint) in SQL; compared as (byte)15 in query
   c. Final fallback: currency_id = 1 (USD)
3. Generate vendor_order_code:
   a. prefix → cart_site_id_order_code_prefix WHERE site_id = @site_id  [G2]
   b. next integer → SELECT NEXT VALUE FOR dbo.cart_order_next_id          [G3 proxy — swap to usp_next_id @Type=3 before QA]
   c. code = "{prefix}{nextId}"
4. INSERT cart_order (order_type = site_id, site_url = site_id)          [G7]
   - cart_customer_id, invoice_in_process_id default to 0 (sentinel values)
   - submission_date, insert_by, modified_by always populated
5. INSERT cart_order_partner + resolve partner_account_id via
   partner_account JOIN account WHERE account_user_name = @username       [✅ implemented]
6. INSERT cart_order_route if routing_action provided                     [G4]
7. INSERT cart_order_message + license_key lookup if message_key provided [G5]
   - message_key stored as Guid (uniqueidentifier) — matches SQL schema
8. INSERT cart_json with extension fields blob                            [always]
```

### InsertCartOrderItemAsync  ← `usp_cart_insert_cart_order_item`

```
1. Lookup license_category_id from license_category_name
2. Resolve unit_price / list_price:
   - Use request.UnitPrice if provided
   - Fallback: product_pricing.retail_price (basic locale-agnostic lookup)
     ⚠ Full pricing derivation (SEC 1–4 of the SP) NOT YET implemented — see Remaining section
3. INSERT cart_order_item with all scalar fields
   - vendor_id defaults to 1 (Webroot) when not supplied
   - invoice_item_in_process_id defaults to 0 (sentinel)
4. INSERT cart_order_item_json blob (vault, platform, retention, pricing level) [G8]
5. INSERT cart_order_item_license when VendorOrderItemCode (keycode) is present [G9 ✅]
   ⚠ line_item offset (G11), CBCART hack (G12), CD date sync (G13) NOT yet done
   ⚠ cart_order_item_json_log (G10) NOT yet written
```

### SelectCartOrderHeaderAsync  ← `usp_cart_select_cart_order`

```
SELECT cart_order JOIN currency LEFT JOIN cart_order_partner LEFT JOIN partner LEFT JOIN cart_json
Returns: header fields + currency_code + partner_key + cart_json blob    [G15 ✅]
```

### SelectCartOrderItemsAsync  ← `usp_cart_select_cart_order_item`

```
Full JOIN chain:
  cart_order_item
  JOIN  product → product_family, product_line_product → product_line, product_type
  LEFT JOIN cart_order_item_json
  LEFT JOIN product_license_category → license_category                  [G17 ✅]
  LEFT JOIN license_keycode_type                                          [G16 ✅]
  LEFT JOIN product_years                                                 [G21 ✅]
  LEFT JOIN product_seat                                                  [G21 ✅]
  LEFT JOIN license_attribute_license_value                               [G21 ✅]
  LEFT JOIN cart_order_item_license (keycode)                             [G19 ✅]

Post-query enrichment:
  - ParseCartOrderItemJson() → reads vault/platform/retention from JSON blob [G18 ✅]
  - equivalentYearPrice proxy: unit_price * years                         [G20 partial]
  - BuildDependentItemMapAsync() → dependent_cart_order_item_id           [G21 ✅]
  - FormatCurrency() → all *Fmt string fields
  - sub-total computations (list, unit, pre-vat, equivalent year)
  - items grouped by cart_item_bundle_id into dict
```

```csharp
// 1a. Try exact match on currency_code from request
var currency = await _db.Currencies
	.FirstOrDefaultAsync(c => c.CurrencyCode == request.CurrencyCode);

// 1b. G6: if still null AND partner found, look up partner's configured currency
//     SELECT configuration_value FROM partner_configuration_partner
//     WHERE partner_id = @partner_id AND partner_configuration_id = 15
if (currency is null && partnerFound)
	currency = await lookupPartnerCurrency();

// 1c. Final fallback: USD (currency_id = 1)
currency ??= await _db.Currencies.FirstAsync(c => c.CurrencyCode == "USD");
```

### Step 2 - Resolve partner  (SP section 1.3)

```csharp
// Only attempted when partner_key is a valid GUID
partner = await _db.Partners
	.FirstOrDefaultAsync(p => p.PartnerKey == partnerGuid);
```

### Step 3 - Generate vendor_order_code  (SP section 2.1)

```csharp
// G2: prefix comes from DB, not hardcoded
//     SELECT vendor_order_code_prefix FROM cart_site_id_order_code_prefix WHERE site_id = @site_id
var prefix = prefixRow?.VendorOrderCodePrefix
	?? request.SiteId[..3].ToUpper();   // fallback if site_id not in table

// G3: next sequential integer - local SEQUENCE mirrors usp_next_id @Type=3
//     TO SWITCH TO QA: replace this with EXEC usp_next_id @Type=3
var nextId = await _db.Database
	.SqlQueryRaw<int>("SELECT NEXT VALUE FOR dbo.cart_order_next_id AS Value")
	.FirstAsync();

vendorOrderCode = $"{prefix}{nextId:D8}";   // e.g. "ECM10000001"
```

### Step 4 - Build order entity graph and save  (SP sections 2.2, 2.3, 2.6)

```csharp
// EF builds the full object graph - one SaveChangesAsync generates all INSERTs
var order = new CartOrder
{
	VendorOrderCode = vendorOrderCode,
	SiteId          = request.SiteId,
	OrderType       = request.SiteId,    // G7: SP sets order_type = @site_id
	SiteUrl         = request.SiteId,    // G7: SP sets site_url   = @site_id
	CurrencyId      = currency.CurrencyId,
	// ... all other header fields ...

	Items = request.Items.Select(item => new CartOrderItem
	{
		LineItem    = index + 1,
		ProductId   = item.ProductId,
		UnitPrice   = item.UnitPrice ?? 0m,     // price from frontend (pre-computed)
		Quantity    = item.Quantity ?? 1,
		// ... all other item fields ...
	}).ToList(),

	CartJson = new CartJson
	{
		// stores all extension fields as JSON blob:
		// currency_code, partner_key, routing_action, key, message_campaign_id, etc.
		Json = JsonSerializer.Serialize(extensionFields)
	},

	CartOrderPartner = partner != null
		? new CartOrderPartner { PartnerId = partner.PartnerId }
		: null
};

_db.CartOrders.Add(order);
await _db.SaveChangesAsync();
// EF generates:
//   INSERT INTO cart_order (...)            VALUES (...)
//   INSERT INTO cart_order_item (...)       VALUES (...)  x N items
//   INSERT INTO cart_json (...)             VALUES (...)
//   INSERT INTO cart_order_partner (...)    VALUES (...)  (only if partner found)
```

### Step 5 - Recalculate and persist cart totals  (SP section 5.5)

```csharp
// SP does: UPDATE cart_order SET total_amount = SUM(unit_price * quantity)
var total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
order.TotalAmount    = total;
order.SubTotalAmount = total;
await _db.SaveChangesAsync();
// EF generates:
//   UPDATE cart_order SET total_amount = @total, sub_total_amount = @total
//   WHERE cart_order_id = @id
```

### Step 6 - Insert optional and per-item rows  (SP sections 2.4, 2.5, 5.3.3, 5.4)

```csharp
// G4: cart_order_route - only when routing_action is provided
if (request.RoutingAction != null)
	_db.CartOrderRoutes.Add(new CartOrderRoute
	{
		CartOrderId   = order.CartOrderId,
		RoutingAction = request.RoutingAction,
		InsertDate    = now
	});

// G5: cart_order_message - only when request.Key is a valid GUID (message_key)
if (Guid.TryParse(request.Key, out var msgGuid))
{
	// Resolve license_id from license_key table
	//   SELECT license_id FROM license_key WHERE license_key = @msgGuid
	var licenseId = await _db.LicenseKeys
		.Where(lk => lk.LicenseKeyValue == msgGuid)
		.Select(lk => (int?)lk.LicenseId)
		.FirstOrDefaultAsync();

	_db.CartOrderMessages.Add(new CartOrderMessage
	{
		CartOrderId             = order.CartOrderId,
		MessageKey              = msgGuid,
		LicenseId               = licenseId,           // null if key not in license_key
		CartDiscountId          = request.CartDiscountId,
		StatusId                = 1,
		MessageCampaignId       = request.MessageCampaignId,
		MessageCampaignPlatform = request.MessageCampaignPlatform
	});
}

// G8: cart_order_item_json - one JSON blob per saved item
//     stores vault_id, retention_model_id, retention_term, product_platform_id,
//     product_pricing_level_id, license_attribute_license_value, item_total
foreach (var (reqItem, savedItem) in request.Items.Zip(order.Items))
	_db.CartOrderItemJsons.Add(new CartOrderItemJson
	{
		CartOrderItemId        = savedItem.CartOrderItemId,
		CartOrderItemJsonValue = JsonSerializer.Serialize(new
		{
			vault_id                        = reqItem.VaultId,
			retention_model_id              = reqItem.RetentionModelId,
			retention_term                  = reqItem.RetentionTerm,
			product_platform_id             = reqItem.ProductPlatformId,
			product_pricing_level_id        = reqItem.ProductPricingLevelId,
			license_attribute_license_value = reqItem.LicenseAttributeLicenseValue,
			item_total                      = reqItem.UnitPrice * reqItem.Quantity
		}),
		InsertDate   = now,
		ModifiedDate = now
	});

// G9: cart_order_item_license - only when VendorOrderItemCode (keycode) is present
if (bundleKeycode != null)
	foreach (var savedItem in order.Items)
		_db.CartOrderItemLicenses.Add(new CartOrderItemLicense
		{
			CartOrderItemId   = savedItem.CartOrderItemId,
			Keycode           = bundleKeycode,
			InsertDate        = now,
			InsertBy          = request.AccountUserName ?? "system",
			ModifiedDate      = now,
			ModifiedBy        = request.AccountUserName ?? "system",
			CartOrderStatusId = 1
		});

await _db.SaveChangesAsync();
// EF generates INSERT for each of the above (only if their conditions were met)
---

## 4. Database Tables - Current State

> ✅ local = table in `local_dev_setup.sql`  |  ✅ EF = entity mapped in AppDbContext  |  ✍ = actively written  |  👁 = actively read

| Table | local DB | EF Mapped | Written | Read | Notes |
|-------|----------|-----------|---------|------|-------|
| `cart_order` | ✅ | ✅ | ✍ | 👁 | Header row |
| `cart_order_item` | ✅ | ✅ | ✍ | 👁 | One row per item |
| `cart_json` | ✅ | ✅ | ✍ | 👁 | Extension fields blob |
| `cart_order_partner` | ✅ | ✅ | ✍ | 👁 | Partner link; now resolves partner_account_id too |
| `cart_order_route` | ✅ | ✅ | ✍ | - | Optional — when routing_action provided [G4] |
| `cart_order_message` | ✅ | ✅ | ✍ | 👁 | Optional — when message_key provided [G5]; pivot check |
| `cart_order_item_json` | ✅ | ✅ | ✍ | 👁 | Per-item JSON blob — vault/platform/retention [G8] |
| `cart_order_item_license` | ✅ | ✅ | ✍ | 👁 | Write path active (G9); read in SELECT |
| `cart_order_item_json_log` | ✅ | ✅ | - | - | Table + entity exist; insert call not yet added [G10] |
| `currency` | ✅ | ✅ | - | 👁 | Lookup by currency_code |
| `partner` | ✅ | ✅ | - | 👁 | Lookup by GUID |
| `partner_account` | ✅ | ✅ | - | 👁 | Joined for partner_account_id resolution |
| `account` | ✅ | ✅ | - | 👁 | Joined via partner_account for account_user_name |
| `partner_configuration_partner` | ✅ | ✅ | - | 👁 | Partner default currency fallback [G6] |
| `license_key` | ✅ | ✅ | - | 👁 | license_id lookup for message_key [G5] |
| `cart_site_id_order_code_prefix` | ✅ | ✅ | - | 👁 | vendor_order_code prefix [G2] |
| `product` | ✅ | ✅ | - | 👁 | JOINed in item select |
| `product_type` | ✅ | ✅ | - | 👁 | JOINed in item select [G16] |
| `product_family` | ✅ | ✅ | - | 👁 | JOINed in item select [G16] |
| `product_line` | ✅ | ✅ | - | 👁 | JOINed in item select [G16] |
| `product_line_product` | ✅ | ✅ | - | 👁 | JOINed in item select [G16] |
| `product_years` | ✅ | ✅ | - | 👁 | LEFT JOINed in item select [G21] |
| `product_seat` | ✅ | ✅ | - | 👁 | LEFT JOINed in item select [G21] |
| `product_license_category` | ✅ | ✅ | - | 👁 | LEFT JOINed in item select [G17] |
| `license_category` | ✅ | ✅ | - | 👁 | LEFT JOINed in item select [G17] |
| `license_keycode_type` | ✅ | ✅ | - | 👁 | LEFT JOINed in item select [G16] |
| `license_attribute_license_value` | ✅ | ✅ | - | 👁 | LEFT JOINed in item select [G21] |
| `product_pricing` | ✅ | ✅ | - | 👁 | Basic retail_price fallback in item insert; full locale pricing deferred [G20] |
| `cart_order_item_license` (read) | ✅ | ✅ | - | 👁 | keycode in SELECT [G19] |

| `currency` | ✅ | ✅ EF | - | YES | Lookup by currency_code |
| `partner` | ✅ | ✅ EF | - | YES | Lookup by GUID |
| `partner_configuration_partner` | ✅ | ✅ EF | - | YES | Partner default currency [G6] |
| `partner_account` | ✅ | ✅ EF | - | YES | Joined for partner_account_id resolution |
| `account` | ✅ | ✅ EF | - | YES | Joined via partner_account for account_user_name |
| `license_key` | ✅ | ✅ EF | - | YES | license_id lookup for message_key [G5] |
| `cart_site_id_order_code_prefix` | ✅ | ✅ EF | - | YES | vendor_order_code prefix [G2] |
| `product` | ✅ | ✅ EF | - | YES | Product description on response |
| `product_type` | ✅ | ✅ EF | - | YES | JOINed in item select [G16] |
| `product_family` | ✅ | ✅ EF | - | YES | JOINed in item select [G16] |
| `product_line` | ✅ | ✅ EF | - | YES | JOINed in item select [G16] |
| `product_line_product` | ✅ | ✅ EF | - | YES | JOINed in item select [G16] |
| `product_years` | ✅ | ✅ EF | - | YES | LEFT JOINed in item select [G21]; type fixed: `double` not `decimal` |
| `product_seat` | ✅ | ✅ EF | - | YES | LEFT JOINed in item select [G21] |
| `product_license_category` | ✅ | ✅ EF | - | YES | LEFT JOINed in item select [G17] |
| `product_pricing` | ✅ | ✅ EF | - | YES | Basic retail_price fallback; full locale pricing deferred [G20] |
| `product_platform` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G18] |
| `license_category` | ✅ | ✅ EF | - | YES | Used in validation + item select |
| `license_keycode_type` | ✅ | ✅ EF | - | YES | LEFT JOINed in item select [G16] |
| `license_attribute_license_value` | ✅ | ✅ EF | - | YES | LEFT JOINed in item select [G21] |
| `retention_model` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G18] |
| `usage_pricing_model` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G18] |
| `cart_discount_method` | ✅ | 🔲 | - | - | Table + seed exist; used as FK reference |

---

## 5. What Is Implemented vs What Remains

### Done ✅
- Full `usp_cart_insert_cart_order` parity: G2, G3, G4, G5, G6, G7
- `usp_cart_insert_cart_order_item`: item insert, basic price fallback from `product_pricing`, cart_order_item_json (G8)
- `usp_cart_select_cart_order`: full header with currency, partner, cart_json, all SP columns (G15)
- `usp_cart_select_cart_order_item`: full JOIN chain — product_type, product_family, product_line (G16), license_category via product_license_category (G17), cart_order_item_json enrichment (G18), keycode (G19), seats/years/license_attribute_license_value_description (G21), dependent_cart_order_item_id, all formatted price strings
- Quote-key pivot check via cart_order_message (G14)
- partner_account_id resolution via account JOIN (previously missing)
- Local DB: all 34 tables present
- **Schema alignment (completed):** all NOT NULL columns added to entities:
  - `cart_order`: `cart_customer_id` (sentinel 0), `invoice_in_process_id` (sentinel 0), `order_type`, `site_url`, `p_rc`, `payment_method`, `session_id`
  - `cart_order_item`: `vendor_id` (default 1 = Webroot), `invoice_item_in_process_id` (sentinel 0)
  - `cart_order_message.message_key`: changed `string?` → `Guid` (`uniqueidentifier NOT NULL`)
  - `cart_json.cart_json`: made non-nullable
  - `submission_date`, `order_type`, `site_url` made non-nullable on `CartOrder`
  - `list_price`, `unit_price` made non-nullable on `CartOrderItem`
- **Type mismatch fixes (completed):** all EF entity types now match SQL Server column types exactly:
  - `tinyint` → `byte`: `CartOrder.CartOrderStatusId`, `CartOrderItem.OrderItemUpdateTypeId`, `CartOrderItem.ItemHierarchyId`, `CartOrderItem.CartDiscountMethodId`, `PartnerConfigurationPartner.PartnerConfigurationId`
  - `float` → `double`: `ProductYears.Years`
  - Removed illegal SQL-layer cast `(int)cu.CurrencyId` from EF projections; cast moved to post-materialization
- **DB patches created:**
  - `sql/patches/patch_001_cart_order_message_key_guid.sql` — VARCHAR(36) → UNIQUEIDENTIFIER
  - `sql/patches/patch_002_cart_order_vendor_order_code_unique.sql` — cleanup duplicates + add UNIQUE constraint
- **local_dev_setup.sql updated:** `message_key` is now `UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()`, `vendor_order_code` has `UNIQUE` constraint
- **63/63 tests passing**
- **`POST /cart/cart-orders` end-to-end verified working against local SQL Server**

### Remaining — Insert Correctness (P1)

| Gap | What | Notes |
|-----|------|-------|
| G10 | `cart_order_item_json_log` insert | Entity exists, insert call not added yet |
| G11 | `line_item` offset for multi-call carts | Always starts at 1; offset by MAX(line_item) needed |
| G12 | CBCART routing hack | Update locale/site_id for CBCART + upgrade orders |
| G13 | CD line date sync | product_family_id=8 inherits dates from primary bundle item |

### Remaining — Pricing & Date Derivation (P1 complex — SP SEC 1–4)

These are the most complex gaps — they require additional entities and logic not yet ported:

| What | SP Section | Needs |
|------|-----------|-------|
| Locale split → language_code + location_code | 1.2 | C# helper (no new entity) |
| License profile load (renewal/upgrade detection) | 1.3 | `License`, `LicenseMessage` entities |
| Product line resolution from license_category | 1.9 | `LicenseCategoryProductLine` entity |
| Per-partner usage/retention/platform overrides | 1.12–1.14 | `PartnerUsagePricingModel` etc. entities |
| start_date / expiration_date derivation | 2.1–2.5 | license profile + years + billing model |
| `fn_product_select_profile` — resolve product_id from 12 attributes | 2.1 | Complex multi-attribute lookup |
| Consumer pricing (retail_price × discount) | SEC 3 | `usp_cart_select_renewal_product_set` |
| Business direct pricing (pro-rated, tier discount) | SEC 4 | `ProductCapability`, leap-day helper |

### Remaining — equivalent_year_price exact computation (G20 partial)

Current code: `unit_price * years` proxy.
Exact SP logic: `fn_cart_select_one_year_products(product_id)` + `product_pricing` JOIN by locale.
Fix needs: locale split helper + 1-year product variant lookup via `product_pricing` + `product_years`.

### Future Phases (P3)

| Gap | Description |
|-----|-------------|
| G22 | Bundle deduplication on re-insert |
| G23 | Load `AllowedSiteIds` / `AllowedLicenseCategoryNames` from DB |
| G24 | `GET /cart/cart-orders/{vendorOrderCode}` |
| G25 | `PUT /cart/cart-orders/{vendorOrderCode}/items/{id}` |
| G26 | `DELETE /cart/cart-orders/{vendorOrderCode}/items/{id}` |

---

## 6. Going Live - What Happens Automatically vs What Needs Work

### Works without any code change when pointing to QA/Production

- All EF Core table/column mappings match QA schema (`ecommerce_VH14`) exactly - **connection string change only**
- All `tinyint` columns now correctly mapped to `byte` in C# — no materialization cast failures
- `cart_order_message.message_key` now uses `Guid` (matching `uniqueidentifier` in both local and QA schema)
- `product_years.years` now mapped to `double` (matching SQL `float`)
- `cart_order_route`, `cart_order_message`, `cart_order_item_json`, `cart_order_item_license` all insert correctly for relevant request fields
- Partner currency fallback via `partner_configuration_partner` works against real partner data
- `FindExistingVendorOrderCodeByKeyAsync` queries `cart_order_message` live — quote-key detection is active
- Request/response shape matches what the PHP frontend sends and reads

### Still needs work before production

| Task | Detail |
|------|--------|
| Replace SEQUENCE with `usp_next_id` | In `CartOrderRepository`, swap `SELECT NEXT VALUE FOR dbo.cart_order_next_id` with `EXEC usp_next_id @Type=3` — one line change; the sequence is used locally only |
| CSRF + CSI user auth | `X-WRCART-CSRF`, `X-CSI-USER`, `X-CSI-USER-ID` headers not yet validated by middleware |
| Update path | Service detects existing cart via `FindExistingVendorOrderCodeByKeyAsync` but update logic is not yet implemented — INSERT is always taken |
| G10 `cart_order_item_json_log` | Entity exists, insert call not yet added |
| G11 `line_item` offset | Always starts at 1; offset by MAX(line_item) needed for multi-call carts |
| G12 CBCART routing hack | Update locale/site_id for CBCART + upgrade orders |
| G13 CD line date sync | product_family_id=8 inherits dates from primary bundle item |
| Full pricing derivation (SEC 1–4) | See Remaining section above |
| Smoke test vs QA DB | POST with real partner_key and product_id to verify FK constraints pass against `ecommerce_VH14` |
