# Gap Fill Plan — SP Migration to EF Core

> Full analysis of all 4 stored procedures vs current C# implementation.
> Status column reflects work as of the latest implementation session.
>
> **Intentionally skipped (frontend-owned):**
> All pricing computation (SP sections 3.x, 4.x), product selection via `fn_product_select_profile` /
> `usp_cart_select_renewal_product_set`, date computation for renewals (2.1/2.2/2.3),
> storage GB computation (`fn_get_item_storage_gb`), license attribute resolution (1.9.x),
> and auto-generated upgrade items (2.2.1, 2.3.1). These values come from the frontend payload.

---

## P0 — Done ✅

| ID  | Description | SP Section | Status |
|-----|-------------|------------|--------|
| G1  | Cart totals (`total_amount`, `sub_total_amount`) recalculated after insert | insert_item 5.5 | ✅ Done |
| G2  | `vendor_order_code` prefix from `cart_site_id_order_code_prefix` table | insert_order 2.1 | ✅ Done |
| G3  | Sequential ID via local DB SEQUENCE (mirrors `usp_next_id @Type=3`) | insert_order 2.1 | ✅ Done |
| G4  | `cart_order_route` insert when `routing_action` provided | insert_order 2.4 | ✅ Done |
| G5  | `cart_order_message` insert + `license_key` lookup when `key` provided | insert_order 2.5 | ✅ Done |
| G6  | Partner currency fallback via `partner_configuration_partner` | insert_order 1.3.2 | ✅ Done |
| G7  | `order_type` and `site_url` both set to `site_id` | insert_order 2.2 | ✅ Done |
| G8  | `cart_order_item_json` insert — per-item JSON dimensions (vault, retention, platform) | insert_item 5.3.3 | ✅ Done |
| G9  | `cart_order_item_license` insert — keycode linkage per item | insert_item 5.4 | ✅ Done |
| G14 | Quote-key pivot — `FindExistingVendorOrderCodeByKeyAsync` via `cart_order_message` | service layer | ✅ Done |

---

## P1 — Insert Correctness (Next to implement)

### G10. `cart_order_item_json_log` insert (raw JSON audit log)
**SP:** `usp_cart_insert_cart_order_item` section 5.0
```sql
INSERT INTO cart_order_item_json_log (cart_order_id, item_json, bundle_json)
VALUES (@cart_order_id, @item_json, @bundle_json)
```
**Current state:** Table exists in local DB but not mapped in EF. Insert never happens.
**Schema confirmed:** `cart_order_item_json_log_id INT IDENTITY, cart_order_id INT, item_json NVARCHAR(MAX), bundle_json NVARCHAR(MAX), insert_date DATETIME`
**Fix:** Map `cart_order_item_json_log` entity in `AppDbContext`. After saving items, insert one log row per API call with the serialized `item_json` and `bundle_json` from the request.
**Files:** `EfCartOrderRepository.cs`, `AppDbContext.cs`, new `CartOrderItemJsonLog.cs`

---

### G11. `line_item` sequencing — respects existing items in cart
**SP:** `usp_cart_insert_cart_order_item` section 5.1
```sql
SELECT @max_line_item = MAX(line_item) FROM cart_order_item WHERE cart_order_id = @cart_order_id
UPDATE @item_table SET line_item = IIF(@max_line_item IS NULL, item_id, item_id + @max_line_item)
```
**Current state:** `line_item` always starts at 1 — adding more items to an existing cart would cause duplicate `line_item` values.
**Fix:** Before inserting items, query `MAX(line_item)` for the existing cart and offset new items accordingly.
**Files:** `EfCartOrderRepository.cs`

---

### G12. `cart_order` header update — CBCART routing hack
**SP:** `usp_cart_insert_cart_order_item` section 5.2
```sql
UPDATE cart_order SET locale='en_US', site_id='WRCART', order_type='WRCART'
WHERE cart_order_id = @cart_order_id AND site_id = 'CBCART'
  AND currency_id IN (1,2,3,4,29) AND product_type_id = 3 (upgrade)
```
**Current state:** Never applied — `CBCART` orders with upgrades stay on wrong locale/site.
**Fix:** After item insert, check if `site_id = 'CBCART'`, any item is an upgrade, and currency is USD/EUR/AUD/GBP/CAD. If so, update the cart_order header.
**Files:** `EfCartOrderRepository.cs`

---

### G13. CD line date sync
**SP:** `usp_cart_insert_cart_order_item` section 5.6
```sql
UPDATE coi2 SET start_date = coi.start_date, expiration_date = coi.expiration_date
FROM cart_order_item coi INNER JOIN cart_order_item coi2
  ON coi2.cart_item_bundle_id = coi.cart_item_bundle_id
WHERE product_family_id != 8 AND p2.product_family_id = 8  -- CD product
```
**Current state:** S/H (shipping/handling, `product_family_id=8`) lines always keep their input dates instead of inheriting from the primary product in the same bundle.
**Fix:** After item insert, sync start/expiration from non-CD product to CD product within the same `cart_item_bundle_id`.
**Files:** `EfCartOrderRepository.cs`

---

## P2 — Response Enrichment (Fix before frontend integration)

### G15. Header response — missing SP fields
**SP:** `usp_cart_select_cart_order` section 1 SELECT
**Missing from current response:** `offer_amount`, `tax_amount`, `user_ip`
**Fix:** Already present on `CartOrder` entity. Just add to `CartOrderResponse` DTO and `MapToResponse()`.
**Files:** `CartOrderResponse.cs`, `EfCartOrderRepository.cs`

---

### G16. Item response — product type, keycode type, family, product line
**SP:** `usp_cart_select_cart_order_item` section 1.2
**Missing:** `product_type_id`, `product_type_description` (JOIN `product_type`), `license_keycode_type_description` (JOIN `license_keycode_type`), `product_family_description` (JOIN `product_family`), `product_line_cart_type` (JOIN `product_line` via `product_line_product`).
**Local DB status:** All four tables (`product_type`, `license_keycode_type`, `product_family`, `product_line`, `product_line_product`) now exist in `local_dev_setup.sql` with seed data.
**Fix:** Map entities in `AppDbContext`. Add navigation properties to `Product`. Enrich item response in `MapItemToResponse`.
**Files:** `CartOrderItemResponse.cs`, `EfCartOrderRepository.cs`, `AppDbContext.cs`, new entities

---

### G17. Item response — license_category via `product_license_category`
**SP:** `usp_cart_select_cart_order_item` section 1.2
**Missing:** `license_category_id`, `license_category_name`, `license_category_description`, `min_order_quantity`, `max_order_quantity`
**Local DB status:** Both `product_license_category` and `license_category` now exist in `local_dev_setup.sql` with seed data.
**Fix:** Map `product_license_category` join table in `AppDbContext`. Enrich item response via EF include.
**Files:** `CartOrderItemResponse.cs`, `EfCartOrderRepository.cs`, `AppDbContext.cs`

---

### G18. Item response — `cart_order_item_json` enrichment (vault, retention, platform)
**SP:** `usp_cart_select_cart_order_item` section 1.2 via `fn_cart_select_cart_order_item_json`
**Missing:** `usage_pricing_model_id/name`, `retention_model_id/name/term/type`, `product_platform_id/name`, `vault_id/datacenter/array`, `product_pricing_level_id/description`, raw `cart_order_item_json` blob.
**Depends on:** G8 must be populated first.
**Fix:** In `SelectCartOrderAsync`, include `CartOrderItemJson`. Deserialize fields into item response.
**Files:** `CartOrderItemResponse.cs`, `EfCartOrderRepository.cs`

---

### G19. Item response — `keycode` from `cart_order_item_license`
**SP:** `usp_cart_select_cart_order_item` section 1.2 `LEFT JOIN cart_order_item_license`
**Missing:** `keycode` field on item response.
**Depends on:** G9 must be populated first.
**Fix:** Include `CartOrderItemLicense` in EF item select and return `keycode`.
**Files:** `CartOrderItemResponse.cs`, `EfCartOrderRepository.cs`

---

### G20. Item response — `equivalent_year_price`
**SP:** `usp_cart_select_cart_order_item` section 1.2 via `fn_cart_select_one_year_products` + `product_pricing`
**Description:** Converts any product price to its 1-year equivalent using locale-based `product_pricing`. Complex but important for multi-year display.
**Fix:** Map `product_pricing`, `product_years` tables. Compute `retail_price * years` for the 1-year equivalent product.
**Files:** `CartOrderItemResponse.cs`, `EfCartOrderRepository.cs`, `AppDbContext.cs`

---

### G21. Item response — remaining scalar fields
**SP:** `usp_cart_select_cart_order_item` section 1.2
**Missing:** `unit_price_pre_vat`, `tax_item_total`, `seats` (from `product_seat`), `years` (from `product_years`), `order_item_offer_amount`, `dependent_cart_order_item_id`, `license_attribute_license_value_description` (from `license_attribute_license_value` table).
**Fix:** Add to item response DTO. For `seats`/`years`, map `product_seat` and `product_years`.
**Files:** `CartOrderItemResponse.cs`, `EfCartOrderRepository.cs`, `AppDbContext.cs`

---

## P3 — Future Phases

### G22. Deduplication of existing bundle items before insert
**SP:** `usp_cart_insert_cart_order_item` section 5.0 — remove-and-replace loop via `usp_cart_delete_cart_order_item`
When re-adding the same `cart_item_bundle_id`, SP removes existing items first to prevent double-charge.
**Depends on:** G26 (DELETE endpoint).

---

### G23. Load `AllowedSiteIds` and `AllowedLicenseCategoryNames` from DB
Currently hardcoded in `CartOrderService`. Should query `cart_site_id_order_code_prefix` (for site IDs) and `license_category` (for category names) tables at startup or via cached service.

---

### G24. `GET /cart/cart-orders/{vendorOrderCode}` — load existing cart
Uses `usp_cart_select_cart_order` + `usp_cart_select_cart_order_item`.
Needed for the right-panel cart summary on the page.

---

### G25. `PUT /cart/cart-orders/{vendorOrderCode}/items/{id}` — update cart item
Triggers deduplication logic from SP section 5.0. Needs G26 (delete) as a prerequisite.

---

### G26. `DELETE /cart/cart-orders/{vendorOrderCode}/items/{id}` — remove item
Prerequisite for G22 and G25. Mirrors `usp_cart_delete_cart_order_item`.

---

## Execution Order

```
✅ G7   order_type/site_url = site_id
✅ G1   cart totals (unit_price × quantity)
✅ G2   vendor_order_code prefix from cart_site_id_order_code_prefix
✅ G3   sequential ID via local SEQUENCE (usp_next_id mirror)
✅ G4   cart_order_route insert
✅ G5   cart_order_message insert + license_key lookup
✅ G6   partner currency fallback via partner_configuration_partner
✅ G8   cart_order_item_json insert (per-item JSON dimensions)
✅ G9   cart_order_item_license insert (keycode linkage)
✅ G14  quote-key pivot via cart_order_message

── P1: Remaining Insert Correctness ────────────────────────────────
G10  cart_order_item_json_log insert (needs QA schema first)
G11  line_item max-offset sequencing for multi-call carts
G12  CBCART routing hack (site_id/locale update on upgrade)
G13  CD line date sync (product_family_id=8 dates inherit from primary)

── P2: Response Enrichment ─────────────────────────────────────────
G15  header: add offer_amount, tax_amount, user_ip to response
G16  item: product_type, keycode_type, product_family, product_line
G17  item: license_category via product_license_category
G18  item: cart_order_item_json JSON fields (depends on G8)
G19  item: keycode from cart_order_item_license (depends on G9)
G20  item: equivalent_year_price
G21  item: remaining scalar fields (seats, years, unit_price_pre_vat, etc.)

── P3: Future Phases ───────────────────────────────────────────────
G22  deduplication/replace on bundle re-add (depends on G26)
G23  AllowedSiteIds/AllowedLicenseCategoryNames from DB
G24  GET /cart/cart-orders/{vendorOrderCode}
G25  PUT update cart item (depends on G26)
G26  DELETE cart item
```


> Full analysis of all 4 stored procedures vs current C# implementation.
> Gaps are prioritized: P0 = done, P1 = insert correctness, P2 = response enrichment, P3 = complex/deferred.
>
> **Intentionally skipped (frontend-owned):**
> All pricing computation (SP sections 3.x, 4.x), product selection via `fn_product_select_profile` /
> `usp_cart_select_renewal_product_set`, date computation for renewals (2.1/2.2/2.3),
> storage GB computation (`fn_get_item_storage_gb`), license attribute resolution (1.9.x),
> and auto-generated upgrade items (2.2.1, 2.3.1). These values come from the frontend payload.

---

## P0 — Done

| ID | Description | SP Section |
|----|-------------|------------|
| G1 | Cart totals (`total_amount`, `sub_total_amount`) recalculated after insert | insert_item 5.5 |
| G2 | `vendor_order_code` prefix from `cart_site_id_order_code_prefix` table | insert_order 2.1 |
| G3 | Sequential ID via local DB SEQUENCE (mirrors `usp_next_id @Type=3`) | insert_order 2.1 |
| G7 | `order_type` and `site_url` both set to `site_id` | insert_order 2.2 |

---

## P1 — Insert Correctness (Fix before QA smoke test)

### G4. `cart_order_route` insert when `routing_action` is provided
**SP:** `usp_cart_insert_cart_order` section 2.4
```sql
INSERT INTO cart_order_route (cart_order_id, routing_action, insert_date)
SELECT @cart_order_id, @routing_action, @insert_date
```
**Current state:** `routing_action` captured in request, saved only in `cart_json` blob — table row never inserted.
**Fix:** Map `cart_order_route` entity. When `request.RoutingAction` is not null/empty, insert one row after cart_order save.
**Files:** `EfCartOrderRepository.cs`, `AppDbContext.cs`, new `CartOrderRoute.cs`

---

### G5. `cart_order_message` insert when `key` (message_key) is provided
**SP:** `usp_cart_insert_cart_order` section 2.5
```sql
SELECT @license_id = license_id FROM license_key WHERE license_key = @message_key
INSERT INTO cart_order_message (cart_order_id, message_key, message_campaign_id,
  message_campaign_platform, cart_discount_id, license_id)
```
**Current state:** `Key` field saved only in `cart_json` blob — table row never inserted.
**Fix:** Map `cart_order_message` and `license_key` tables. When `request.Key` is not null, resolve `license_id` from `license_key` then insert.
**Files:** `EfCartOrderRepository.cs`, `AppDbContext.cs`, new `CartOrderMessage.cs`, `LicenseKey.cs`

---

### G6. Partner currency fallback via `partner_configuration_partner`
**SP:** `usp_cart_insert_cart_order` section 1.3.2
```sql
SELECT @currency_code = c.currency_code, @currency_id = c.currency_id
FROM partner_configuration_partner cp
INNER JOIN currency c ON cp.configuration_value = c.currency_code
WHERE cp.partner_id = @partner_id AND cp.partner_configuration_id = 15
```
**Current state:** Falls back directly to `currency_id = 1` (USD) when currency not in request.
**Fix:** Before the USD fallback, if `partner_id` is known, query `partner_configuration_partner` for the configured currency.
**Files:** `EfCartOrderRepository.cs`, `AppDbContext.cs`, new `PartnerConfigurationPartner.cs`

---

### G8. `cart_order_item_json_log` insert (raw JSON audit log)
**SP:** `usp_cart_insert_cart_order_item` section 5.0
```sql
INSERT INTO cart_order_item_json_log (cart_order_id, item_json, bundle_json)
VALUES (@cart_order_id, @item_json, @bundle_json)
```
**Current state:** Table not mapped, insert never happens.
**Fix:** Map `cart_order_item_json_log` entity. After saving items, insert one log row per call with the raw request JSON.
**Files:** `EfCartOrderRepository.cs`, `AppDbContext.cs`, new `CartOrderItemJsonLog.cs`

---

### G9. `cart_order_item_json` insert (per-item JSON dimensions)
**SP:** `usp_cart_insert_cart_order_item` section 5.3.3
```sql
INSERT INTO cart_order_item_json (cart_order_item_id, cart_order_item_json)
SELECT i.cart_order_item_id, (SELECT usage_pricing_model_id, retention_model_id,
  retention_term, product_platform_id, product_pricing_level_id, vault_id,
  vault_array, license_attribute_license_value, actual_storage_quantity,
  item_total, amended_contract, license_category_name, cart_order_item_json_log_id
  FROM @item_table FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
```
**Current state:** Table not mapped, insert never happens. Vault/platform/retention data is lost.
**Fix:** Map `cart_order_item_json` entity. After item insert, build and persist the JSON blob per item.
**Files:** `EfCartOrderRepository.cs`, `AppDbContext.cs`, new `CartOrderItemJson.cs`

---

### G10. `cart_order_item_license` insert (keycode linkage)
**SP:** `usp_cart_insert_cart_order_item` section 5.4
```sql
IF @keycode IS NOT NULL AND @keycode <> ''
  INSERT INTO cart_order_item_license (cart_order_item_id, keycode, insert_date, insert_by, ...)
```
**Current state:** Table not mapped — keycode is accepted on the request but never linked to items.
**Fix:** Map `cart_order_item_license`. When bundle JSON carries a `keycode`, insert rows linking each inserted item to that keycode.
**Files:** `EfCartOrderRepository.cs`, `AppDbContext.cs`, new `CartOrderItemLicense.cs`

---

### G11. `line_item` sequencing — respects existing items
**SP:** `usp_cart_insert_cart_order_item` section 5.1
```sql
SELECT @max_line_item = MAX(line_item) FROM cart_order_item WHERE cart_order_id = @cart_order_id
UPDATE @item_table SET line_item = IIF(@max_line_item IS NULL, item_id, item_id + @max_line_item)
```
**Current state:** `line_item` is always set starting from 1 — adding items to an existing cart would duplicate line numbers.
**Fix:** Before inserting items, query `MAX(line_item)` for the existing cart and offset new items accordingly.
**Files:** `EfCartOrderRepository.cs`

---

### G12. `cart_order` update — CBCART routing hack
**SP:** `usp_cart_insert_cart_order_item` section 5.2
```sql
UPDATE cart_order SET locale='en_US', site_id='WRCART', order_type='WRCART'
WHERE cart_order_id = @cart_order_id AND site_id = 'CBCART'
  AND currency_id IN (1,2,3,4,29) AND product_type_id = 3
```
**Current state:** Never applied.
**Fix:** After item insert, if `site_id = 'CBCART'` and any item is an upgrade (product_type_id = 3) and currency is USD/EUR/AUD/GBP/CAD, update the cart_order header.
**Files:** `EfCartOrderRepository.cs`

---

### G13. CD line date sync
**SP:** `usp_cart_insert_cart_order_item` section 5.6
```sql
UPDATE coi2 SET coi2.start_date = coi.start_date, coi2.expiration_date = coi.expiration_date
FROM cart_order_item coi INNER JOIN cart_order_item coi2
  ON coi2.cart_item_bundle_id = coi.cart_item_bundle_id
WHERE product_family_id != 8 AND p2.product_family_id = 8
```
**Current state:** Never applied — S/H (shipping) lines always retain whatever dates were provided.
**Fix:** After item insert, sync start/expiration dates from the non-CD product to the CD product in the same bundle.
**Files:** `EfCartOrderRepository.cs`

---

### G14. Quote-key pivot — update path via `cart_order_message`
**SP:** N/A direct, but `FindExistingVendorOrderCodeByKeyAsync` is intended to detect existing carts by `message_key`.
**Current state:** Always returns null — insert path always taken even if a cart with that key already exists.
**Depends on:** G5 (cart_order_message must exist first).
**Fix:** After G5, query `cart_order_message` by `message_key` to return an existing `vendor_order_code` and let the service pivot to update flow.
**Files:** `EfCartOrderRepository.cs`, `CartOrderService.cs`

---

## P2 — Response Enrichment (Fix before frontend integration)

### G15. Header response — missing SP fields
**SP:** `usp_cart_select_cart_order` section 1 SELECT list
Missing from current response: `offer_amount`, `tax_amount`, `user_ip`.
**Fix:** Add these fields to `CartOrderResponse` and populate from the entity/select.
**Files:** `CartOrderResponse.cs`, `EfCartOrderRepository.cs` (MapToResponse)

---

### G16. Item response — product enrichment
**SP:** `usp_cart_select_cart_order_item` section 1.2
Missing: `product_type_id`, `product_type_description` (JOIN `product_type`), `license_keycode_type_id`, `license_keycode_type_description` (JOIN `license_keycode_type`), `product_family_description` (JOIN `product_family`), `product_line_cart_type` (JOIN `product_line`).
**Fix:** Map `product_type`, `license_keycode_type`, `product_family`, `product_line` tables. Enrich item response.
**Files:** `CartOrderItemResponse.cs`, `EfCartOrderRepository.cs`, `AppDbContext.cs`, new entities

---

### G17. Item response — license_category enrichment
**SP:** `usp_cart_select_cart_order_item` section 1.2
Missing: `license_category_id`, `license_category_name`, `license_category_description`, `min_order_quantity`, `max_order_quantity` (JOIN `license_category` via `product_license_category`).
**Fix:** Map `product_license_category`. Enrich item response via EF join.
**Files:** `CartOrderItemResponse.cs`, `EfCartOrderRepository.cs`, `AppDbContext.cs`

---

### G18. Item response — cart_order_item_json enrichment (vault, retention, platform)
**SP:** `usp_cart_select_cart_order_item` section 1.2 via `fn_cart_select_cart_order_item_json`
Missing: `usage_pricing_model_id/name`, `retention_model_id/name/term/type`, `product_platform_id/name`, `vault_id/datacenter/array`, `product_pricing_level_id/description`, `cart_order_item_json` (raw blob).
**Depends on:** G9 (cart_order_item_json must be written first).
**Fix:** Join `cart_order_item_json` in select; deserialize fields into response.
**Files:** `CartOrderItemResponse.cs`, `EfCartOrderRepository.cs`

---

### G19. Item response — keycode from `cart_order_item_license`
**SP:** `usp_cart_select_cart_order_item` section 1.2 `LEFT JOIN cart_order_item_license`
Missing: `keycode` field on item response.
**Depends on:** G10 (cart_order_item_license must be written first).
**Fix:** Include `cart_order_item_license` in EF item select and return `keycode`.
**Files:** `CartOrderItemResponse.cs`, `EfCartOrderRepository.cs`

---

### G20. Item response — `equivalent_year_price`
**SP:** `usp_cart_select_cart_order_item` section 1.2 via `fn_cart_select_one_year_products` + `product_pricing`
Complex computed field: converts any product's price to a 1-year equivalent using locale-based pricing.
**Local DB status:** `product_pricing` and `product_years` tables now exist in `local_dev_setup.sql`. No seed rows in `product_pricing` — these will be added when the pricing logic gap is implemented.
**Fix:** Map `product_pricing`, `product_years` entities. Compute `retail_price * years` for the 1-year product version.
**Files:** `CartOrderItemResponse.cs`, `EfCartOrderRepository.cs`, `AppDbContext.cs`

---

### G21. Item response — remaining missing scalar fields
**SP:** `usp_cart_select_cart_order_item` section 1.2
Missing: `unit_price_pre_vat`, `tax_item_total`, `seats` (from `product_seat`), `years` (from `product_years`), `order_item_offer_amount`, `dependent_cart_order_item_id`, `license_attribute_license_value_description`.
**Local DB status:** `product_seat`, `product_years`, and `license_attribute_license_value` tables now exist in `local_dev_setup.sql` with seed data for seat/years. `license_attribute_license_value` has no seed rows — values come from real license config.
**Fix:** Add fields to item response DTO. For `seats`/`years`, join `product_seat` and `product_years`. For `license_attribute_license_value_description`, join the value table via the item's `license_attribute_license_value` column.
**Files:** `CartOrderItemResponse.cs`, `EfCartOrderRepository.cs`, `AppDbContext.cs`

---

## P3 — Future Phases

### G22. Deduplication of existing bundle items before insert
**SP:** `usp_cart_insert_cart_order_item` section 5.0 (remove-and-replace loop via `usp_cart_delete_cart_order_item`)
When re-adding the same `cart_item_bundle_id`, SP removes existing items first to prevent double-charge.
**Depends on:** DELETE endpoint (G25).

---

### G23. Load `AllowedSiteIds` and `AllowedLicenseCategoryNames` from DB
Currently hardcoded in `CartOrderService`. Should query `cart_site_id_order_code_prefix` and `license_category` tables at startup.

---

### G24. `GET /cart/cart-orders/{vendorOrderCode}` — load existing cart
Uses `usp_cart_select_cart_order` + `usp_cart_select_cart_order_item`.

---

### G25. `PUT /cart/cart-orders/{vendorOrderCode}/items/{id}` — update cart item
Triggers deduplication logic from SP section 5.0. Needs `usp_cart_delete_cart_order_item` equivalent.

---

### G26. `DELETE /cart/cart-orders/{vendorOrderCode}/items/{id}` — remove item
Prerequisite for G22 and G25.

---

## Execution Order

```
✅ G7   order_type/site_url fix
✅ G1   cart totals (simplified — unit_price * quantity)
✅ G2   vendor_order_code prefix from DB
✅ G3   sequential ID via SEQUENCE

── P1: Insert Correctness ──────────────────────────────────────────
G4   cart_order_route insert
G5   cart_order_message insert
G6   partner currency fallback
G8   cart_order_item_json_log insert
G9   cart_order_item_json insert (per-item JSON)
G10  cart_order_item_license insert (keycode)
G11  line_item sequencing (max_line_item offset)
G12  CBCART routing hack
G13  CD line date sync
G14  quote-key pivot (depends on G5)

── P2: Response Enrichment ─────────────────────────────────────────
G15  header response missing fields
G16  item: product_type, license_keycode_type, product_family, product_line
G17  item: license_category (via product_license_category)
G18  item: cart_order_item_json enrichment (depends on G9)
G19  item: keycode (depends on G10)
G20  item: equivalent_year_price
G21  item: remaining scalar fields (seats, years, unit_price_pre_vat, etc.)

── P3: Future Phases ───────────────────────────────────────────────
G22  deduplication/replace on bundle re-add (depends on G26)
G23  AllowedSiteIds/AllowedLicenseCategoryNames from DB
G24  GET /cart/cart-orders/{vendorOrderCode}
G25  PUT update cart item
G26  DELETE cart item
```


> Based on full analysis of the 4 real stored procedures vs current C# implementation.
> Gaps are prioritized by impact on correctness of the POST /cart/cart-orders response.
> **Local DB is now complete** — all tables touched by the 4 SPs exist in `local_dev_setup.sql`.
> Remaining work is logic only (EF mappings + C# code), not schema additions.
