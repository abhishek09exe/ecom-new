# Architecture - Current Implementation State

> Last updated after local DB table-coverage expansion (all 30 SP-touched tables now in `local_dev_setup.sql`).
> Implemented gaps: G1–G9, G14. Remaining work is C# logic only — no more schema additions needed for the current scope.

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
|
+-- Repositories/
|   +-- ICartOrderRepository.cs           contract: InsertCartOrderAsync / SelectCartOrderAsync / FindExistingVendorOrderCodeByKeyAsync
|   +-- EfCartOrderRepository.cs          EF Core implementation (all real logic lives here)
|   +-- MockCartOrderRepository.cs        in-memory stub for unit tests
|
+-- Data/
|   +-- AppDbContext.cs                   all EF Fluent mappings (19 entities)
|   +-- Entities/
|       +-- CartOrder.cs
|       +-- CartOrderItem.cs
|       +-- CartJson.cs
|       +-- CartOrderPartner.cs
|       +-- CartOrderRoute.cs             [G4] new
|       +-- CartOrderMessage.cs           [G5] new
|       +-- CartOrderItemJson.cs          [G8] new
|       +-- CartOrderItemLicense.cs       [G9] new
|       +-- Currency.cs
|       +-- CartOrderStatus.cs
|       +-- Partner.cs
|       +-- PartnerConfigurationPartner.cs  [G6] new
|       +-- LicenseKey.cs                 [G5] new
|       +-- Product.cs
|       +-- LicenseCategory.cs
|       +-- CartSiteIdOrderCodePrefix.cs  [G2] new
|       +-- Account.cs                    [local DB added]
|       +-- PartnerAccount.cs             [local DB added]
|       +-- ProductType.cs                [local DB added - G16]
|       +-- ProductFamily.cs              [local DB added - G16]
|       +-- ProductLine.cs                [local DB added - G16]
|       +-- ProductLineProduct.cs         [local DB added - G16]
|       +-- ProductYears.cs               [local DB added - G21]
|       +-- ProductSeat.cs                [local DB added - G21]
|       +-- ProductLicenseCategory.cs     [local DB added - G17]
|       +-- ProductPricing.cs             [local DB added - G20, logic deferred]
|       +-- ProductPlatform.cs            [local DB added - G18]
|       +-- LicenseAttributeLicenseValue.cs [local DB added - G21]
|       +-- RetentionModel.cs             [local DB added - G18]
|       +-- UsagePricingModel.cs          [local DB added - G18]
|       +-- CartDiscountMethod.cs         [local DB added]
|       +-- CartOrderItemJsonLog.cs       [local DB added - G10]
|
+-- Models/
|   +-- Requests/
|   |   +-- CartOrderCreateRequest.cs     POST body DTO (all fields the frontend sends)
|   |   +-- CartOrderItemRequest.cs       per-item DTO nested inside CartOrderCreateRequest
|   +-- Responses/
|       +-- CartOrderResponse.cs          201 response DTO (order header)
|       +-- CartOrderItemResponse.cs      per-item response DTO
|
+-- sql/
	+-- local_dev_setup.sql               drops + recreates ecom_cart_dev (QA-aligned schema)
```

---

## 3. EfCartOrderRepository.InsertCartOrderAsync - Logic Walkthrough

Every `POST /cart/cart-orders` runs through the following steps in order.

### Step 1 - Resolve currency  (SP section 1.3)

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
```

### Step 7 - Read back and map to response  (usp_cart_select_cart_order)

```csharp
var order = await _db.CartOrders
	.Include(o => o.Currency)
	.Include(o => o.CartOrderPartner).ThenInclude(cop => cop.Partner)
	.Include(o => o.CartJson)
	.Include(o => o.Items).ThenInclude(i => i.Product)
	.AsNoTracking()
	.FirstOrDefaultAsync(o => o.VendorOrderCode == vendorOrderCode);

return MapToResponse(order);
// EF generates one SELECT with all JOINs - no N+1 queries
```

---

## 4. Database Tables - Current State

> ✅ = local `ecom_cart_dev` table exists  |  🔲 = not yet mapped in EF  |  ✅ EF = mapped and active

| Table | local DB | EF Mapped | Written | Read | Notes |
|-------|----------|-----------|---------|------|-------|
| `cart_order` | ✅ | ✅ EF | YES | YES | Header; totals updated in second save |
| `cart_order_item` | ✅ | ✅ EF | YES | YES | One row per item |
| `cart_json` | ✅ | ✅ EF | YES | YES | Extension fields JSON blob |
| `cart_order_partner` | ✅ | ✅ EF | YES | YES | Partner link — optional |
| `cart_order_route` | ✅ | ✅ EF | YES | - | routing_action row — optional [G4] |
| `cart_order_message` | ✅ | ✅ EF | YES | YES | message_key row — optional [G5]; drives pivot check |
| `cart_order_item_json` | ✅ | ✅ EF | YES | - | Per-item vault/retention/platform blob [G8] |
| `cart_order_item_license` | ✅ | ✅ EF | YES | - | Keycode link per item — optional [G9] |
| `cart_order_item_json_log` | ✅ | 🔲 | NO | - | Audit log — table exists, EF mapping + write deferred [G10] |
| `currency` | ✅ | ✅ EF | - | YES | Lookup by currency_code |
| `partner` | ✅ | ✅ EF | - | YES | Lookup by GUID |
| `partner_configuration_partner` | ✅ | ✅ EF | - | YES | Partner default currency [G6] |
| `partner_account` | ✅ | 🔲 | - | - | Table exists; read logic deferred |
| `account` | ✅ | 🔲 | - | - | Table exists; read logic deferred |
| `license_key` | ✅ | ✅ EF | - | YES | license_id lookup for message_key [G5] |
| `cart_site_id_order_code_prefix` | ✅ | ✅ EF | - | YES | vendor_order_code prefix [G2] |
| `product` | ✅ | ✅ EF | - | YES | Product description on response |
| `product_type` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G16] |
| `product_family` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G16] |
| `product_line` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G16] |
| `product_line_product` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G16] |
| `product_years` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G21] |
| `product_seat` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G21] |
| `product_license_category` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G17] |
| `product_pricing` | ✅ | 🔲 | - | - | Table exists, no seed rows; pricing logic deferred [G20] |
| `product_platform` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G18] |
| `license_category` | ✅ | ✅ EF | - | YES | Used in validation |
| `license_keycode_type` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G16] |
| `license_attribute_license_value` | ✅ | 🔲 | - | - | Table exists, no seed rows; logic deferred [G21] |
| `retention_model` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G18] |
| `usage_pricing_model` | ✅ | 🔲 | - | - | Table + seed exist; EF mapping deferred [G18] |
| `cart_discount_method` | ✅ | 🔲 | - | - | Table + seed exist; used as FK reference |

---

## 5. What Is Implemented vs What Remains

### Done — insert path
- Full `usp_cart_insert_cart_order` parity: G2, G3, G4, G5, G6, G7 all done
- Partial `usp_cart_insert_cart_order_item` parity: item insert (G1), totals (G1), item_json (G8), item_license (G9) done
- Local DB schema complete: all 30 tables touched by the 4 SPs now exist in `local_dev_setup.sql`

### Remaining — insert correctness (P1) — C# logic only, tables exist

| Gap | What it does | SP section | Table |
|-----|-------------|------------|-------|
| G10 | `cart_order_item_json_log` insert — raw JSON audit log | 5.0 | `cart_order_item_json_log` ✅ |
| G11 | `line_item` offset sequencing — adding items to existing cart | 5.1 | `cart_order_item` ✅ |
| G12 | CBCART routing hack — update locale/site_id for CB upgrade orders | 5.2 | `cart_order` ✅ |
| G13 | CD line date sync — product_family_id=8 inherits dates from primary | 5.6 | `cart_order_item` ✅ |

### Remaining — response enrichment (P2) — C# logic only, tables exist

| Gap | What it adds | Tables needed |
|-----|-------------|---------------|
| G15 | Header: `offer_amount`, `tax_amount`, `user_ip` | `cart_order` ✅ |
| G16 | Item: `product_type`, `license_keycode_type`, `product_family`, `product_line_cart_type` | all ✅ |
| G17 | Item: `license_category` via `product_license_category` | both ✅ |
| G18 | Item: `vault_id`, `retention_model`, `product_platform` from `cart_order_item_json` | all ✅ |
| G19 | Item: `keycode` from `cart_order_item_license` | ✅ |
| G20 | Item: `equivalent_year_price` via `product_pricing` + `product_years` | both ✅ (no pricing seed yet) |
| G21 | Item: `seats`, `years`, `unit_price_pre_vat`, `license_attribute_license_value_description` | all ✅ |

### Future phases (P3)

| Gap | Description |
|-----|-------------|
| G22 | Bundle deduplication on re-insert (remove existing bundle before adding updated one) |
| G23 | Load `AllowedSiteIds` / `AllowedLicenseCategoryNames` from DB instead of hardcoded |
| G24 | `GET /cart/cart-orders/{vendorOrderCode}` — load cart for right-panel display |
| G25 | `PUT /cart/cart-orders/{vendorOrderCode}/items/{id}` — update cart item |
| G26 | `DELETE /cart/cart-orders/{vendorOrderCode}/items/{id}` — remove cart item |

---

## 6. Going Live - What Happens Automatically vs What Needs Work

### Works without any code change when pointing to QA/Production

- All EF Core table/column mappings match QA schema (`ecommerce_VH14`) exactly - **connection string change only**
- `cart_order_route`, `cart_order_message`, `cart_order_item_json`, `cart_order_item_license` all insert correctly for relevant request fields
- Partner currency fallback via `partner_configuration_partner` works against real partner data
- `FindExistingVendorOrderCodeByKeyAsync` now queries `cart_order_message` live - quote-key detection is active
- Request/response shape matches what the PHP frontend sends and reads

### Still needs work before production

| Task | Detail |
|------|--------|
| Replace SEQUENCE with `usp_next_id` | In `EfCartOrderRepository`, swap `SELECT NEXT VALUE FOR dbo.cart_order_next_id` with `EXEC usp_next_id @Type=3` - one line change |
| CSRF + CSI user auth | `X-WRCART-CSRF`, `X-CSI-USER`, `X-CSI-USER-ID` headers not yet validated by middleware |
| Update path | Service detects existing cart via `FindExistingVendorOrderCodeByKeyAsync` but update logic is not yet implemented - INSERT is always taken |
| G10-G13 inserts | See P1 table above |
| Smoke test vs QA DB | POST with real partner_key and product_id to verify FK constraints pass against `ecommerce_VH14` |
