# Bundle Pricing — Postman Test Guide

## Endpoint

```
GET http://localhost:5193/api/bundle-pricing
```

## How Parameters Are Populated

### Request Parameters

| Parameter | Where it comes from | Notes |
|---|---|---|
| `locale` | Caller (e.g. `en_US`) | Drives currency lookup via `currency_language_location` table |
| `LicenseKeycodeTypeId` | Caller | Passed into pricing SP as `@license_keycode_type_id`; controls keycode type context |
| `Items[0].LicenseCategoryName` | Caller | e.g. `ADP`, `SAEP` — must exist in `dbo.license_category` |
| `Items[0].LicenseSeats` | Caller | Number of seats/devices |
| `Items[0].Years` | Caller | Subscription length (0=monthly, 1=annual, 2=2yr, 3=3yr) |
| `Items[0].LicenseAttributeLicenseValue` | Caller | Billing model: 1=Standard, 11=Overage, 12=Utility |
| `Items[0].MessageKey` | Caller (optional) | UUID from `dbo.cart_route.message_key` — drives renewal/discount path |

### Response Parameters

| Field | How it is calculated |
|---|---|
| `list_price` | `product_pricing.retail_price` from the pricing SP |
| `unit_price` | Discounted price after tier/partner discount applied in SP |
| `usage_price` | Non-zero for LALV=11 (Overage) or LALV=12 (Utility) billing models |
| `equivalent_year_price` | `retail_price × years` — annualised comparison price |
| `calculated_discount` | `list_price - unit_price` per item |
| `calculated_discount_pct` | `(discount / list_price) × 100` rounded to nearest 0.5 |
| `sub_total_*` | Quantity × per-item value |
| `*_fmt` | Formatted with currency symbol e.g. `$19.99` |
| `totals` | Sum across all items in the request |
| `product_totals` | Breakdown per `license_category_name` |
| `currency_code` / `currency_symbol` | Resolved from `currency_language_location` by locale |

---

## Scenario 1 — Basic new purchase (no message_key)

**What it tests:** Simplest path. No message_key → `MessageKeyService` skips all SP calls, falls through to site-discount fallback. Pricing SP returns list/unit price for the category.

```
GET http://localhost:5193/api/bundle-pricing?locale=en_US&LicenseKeycodeTypeId=1&Items[0].LicenseCategoryName=ADP&Items[0].LicenseSeats=1&Items[0].Years=1&Items[0].LicenseAttributeLicenseValue=1
```

**Expected:** `200 OK` — `list_price`, `unit_price`, discount fields populated; `currency_code=USD`.

---

## Scenario 2 — Multi-seat purchase

**What it tests:** `quantity` in response reflects seats; `sub_total_amount = unit_price × seats`.

```
GET http://localhost:5193/api/bundle-pricing?locale=en_US&LicenseKeycodeTypeId=1&Items[0].LicenseCategoryName=ADP&Items[0].LicenseSeats=5&Items[0].Years=1&Items[0].LicenseAttributeLicenseValue=1
```

**Expected:** `200 OK` — `quantity=5`, `sub_total_amount` = 5× unit_price.

---

## Scenario 3 — Multi-year subscription

**What it tests:** `equivalent_year_price` = retail_price × years; contract dates span 2 years.

```
GET http://localhost:5193/api/bundle-pricing?locale=en_US&LicenseKeycodeTypeId=1&Items[0].LicenseCategoryName=ADP&Items[0].LicenseSeats=1&Items[0].Years=2&Items[0].LicenseAttributeLicenseValue=1
```

**Expected:** `200 OK` — `years=2` reflected in `equivalent_year_price`; `expiration_date` ~2 years from today.

---

## Scenario 4 — Overage billing model (LALV=11)

**What it tests:** `license_attribute_license_value=11` triggers overage path in SP — both `unit_price` and `usage_price` should be non-zero.

```
GET http://localhost:5193/api/bundle-pricing?locale=en_US&LicenseKeycodeTypeId=1&Items[0].LicenseCategoryName=ADP&Items[0].LicenseSeats=1&Items[0].Years=1&Items[0].LicenseAttributeLicenseValue=11
```

**Expected:** `200 OK` or `422` if no product exists for this LALV — if 200, `usage_price > 0`.

---

## Scenario 5 — Utility billing model (LALV=12)

**What it tests:** `license_attribute_license_value=12` triggers utility path — `unit_price=0`, `usage_price` set.

```
GET http://localhost:5193/api/bundle-pricing?locale=en_US&LicenseKeycodeTypeId=1&Items[0].LicenseCategoryName=ADP&Items[0].LicenseSeats=1&Items[0].Years=1&Items[0].LicenseAttributeLicenseValue=12
```

**Expected:** `200 OK` with `unit_price=0.00` and `usage_price > 0`, or `422` if no utility product for ADP.

---

## Scenario 6 — With a real message_key (renewal/discount path)

**What it tests:** UUID from `dbo.cart_route.message_key` triggers `MessageKeyService.ClassifyKeyAsync` → resolves keycode/campaign/discount depending on type.

```
GET http://localhost:5193/api/bundle-pricing?locale=en_US&LicenseKeycodeTypeId=1&Items[0].LicenseCategoryName=ADP&Items[0].LicenseSeats=1&Items[0].Years=1&Items[0].LicenseAttributeLicenseValue=1&Items[0].MessageKey=0EB4BDD8-2C08-4549-93AE-AD2C57816AB1
```

Other real UUIDs from QA DB:
- `68B2334B-CC25-4344-A87D-823656E4BD7F`
- `54E77D4C-D743-48C0-B592-9A2DA595E264`
- `DFF361CB-4D40-45A0-B089-53584A8FCEF6`

**Expected:** `200 OK` — `cart_discount_id` may be populated if message_key resolved to a discount; pricing may differ from Scenario 1.

---

## Scenario 7 — Multi-item bundle (primary + module)

**What it tests:** Two items in one request — `totals` should aggregate both; `product_totals` should show separate entries per `license_category_name`.

```
GET http://localhost:5193/api/bundle-pricing?locale=en_US&LicenseKeycodeTypeId=1&Items[0].LicenseCategoryName=ADP&Items[0].LicenseSeats=1&Items[0].Years=1&Items[0].LicenseAttributeLicenseValue=1&Items[1].LicenseCategoryName=ADE&Items[1].LicenseSeats=1&Items[1].Years=1&Items[1].LicenseAttributeLicenseValue=1
```

**Expected:** `200 OK` — `items` array has 2 entries; `totals.sub_total_amount` = sum of both.

---

## Scenario 8 — Non-US locale (currency resolution)

**What it tests:** `locale=de_DE` → `currency_language_location` lookup returns EUR; all `*_fmt` fields use `€`.

```
GET http://localhost:5193/api/bundle-pricing?locale=de_DE&LicenseKeycodeTypeId=1&Items[0].LicenseCategoryName=ADP&Items[0].LicenseSeats=1&Items[0].Years=1&Items[0].LicenseAttributeLicenseValue=1
```

**Expected:** `200 OK` — `currency_code=EUR`, `currency_symbol=€`, formatted prices in EUR.

---

## Scenario 9 — Validation error (missing locale) → 400

**What it tests:** `locale` is `[Required]` — omitting it triggers model validation failure before the service is called.

```
GET http://localhost:5193/api/bundle-pricing?LicenseKeycodeTypeId=1&Items[0].LicenseCategoryName=ADP&Items[0].LicenseSeats=1&Items[0].Years=1&Items[0].LicenseAttributeLicenseValue=1
```

**Expected:** `400 Bad Request` — `errors.locale` field in response.

---

## Scenario 10 — No pricing rows for category → 422

**What it tests:** Category name exists but pricing SP returns no rows (wrong seats/years combo or non-existent category) → service returns empty items list → controller returns 422.

```
GET http://localhost:5193/api/bundle-pricing?locale=en_US&LicenseKeycodeTypeId=1&Items[0].LicenseCategoryName=ZZZNOTREAL&Items[0].LicenseSeats=1&Items[0].Years=1&Items[0].LicenseAttributeLicenseValue=1
```

**Expected:** `422 Unprocessable Entity` — `{"error":"No pricing found for these items."}`.
