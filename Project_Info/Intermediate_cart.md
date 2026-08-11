# IntermediateCart Architecture & Data Flow

**Document Version:** 1.0  
**Last Updated:** 2026-08-10  
**Purpose:** Complete end-to-end architectural documentation for IntermediateCart feature

---

## Table of Contents

1. [Overview](#overview)
2. [System Architecture](#system-architecture)
3. [API Endpoints](#api-endpoints)
4. [Frontend Implementation](#frontend-implementation)
5. [Data Models](#data-models)
6. [Pricing Logic](#pricing-logic)
7. [Module & Category Handling](#module--category-handling)
8. [Testing Guide](#testing-guide)

---

## Overview

### Purpose
The IntermediateCart feature allows users to view their existing license details and select upgrade/renewal options with real-time pricing for both annual and monthly billing cycles.

### Tech Stack
- **Frontend:** Next.js 14.2.23 (App Router), React, TypeScript
- **Backend:** ASP.NET Core Web API (.NET 10), Entity Framework Core 9, Microsoft SQL Server (QA DB)
- **API Communication:** REST with fetch API
- **Development Environment:**
  - Frontend: http://localhost:3000
  - Backend: http://localhost:5193

### Key Features
- License key validation and details retrieval
- Real-time bundle pricing calculation
- Monthly and Annual billing options
- Add seats vs. upgrade product differentiation
- Multi-module support (SS, QA_AUTOGEN, WSAC, WSAI, WSAV)

---

## System Architecture

### High-Level Flow

```
User enters license key → Browser loads IntermediateCart page
                            ↓
                    Frontend makes 2 parallel API calls:
                            ↓
            ┌───────────────┴───────────────┐
            ↓                               ↓
    1. GET /license-options         2. POST /bundle-pricing (Annual)
       - Validates license key         - Gets annual pricing
       - Returns license profile       - Returns product totals
       - Returns upgrade categories    
       - Returns module list              ↓
            ↓                          Additional POST /bundle-pricing (Monthly)
            └─────────────┬──────────────┘
                          ↓
                  Data processing & UI render
                          ↓
                User sees products with pricing options
```

### Component Structure

```
site_smb/src/app/IntermediateCart/
├── page.tsx                           # Main page component
├── services/
│   └── cartapi.ts                     # API integration layer
│       ├── CallLicenseOptions()       # GET /license-options
│       └── CallBundlePricing()        # POST /bundle-pricing
├── hooks/
│   └── useProductHook.ts              # Main data fetching hook
└── components/                        # UI components (exact structure TBD)
```

---

## API Endpoints

### 1. License Options Endpoint

**Endpoint:** `GET /license-options`

**Purpose:** Validates license key and returns license details, module information, and available upgrade options.

**Request:**
```
GET http://localhost:5193/license-options?locale=en_US&message_key={LICENSE_KEY}
```

**Request Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `locale` | string | Yes | Language/region code (e.g., "en_US") |
| `message_key` | string | Yes | License key to validate (e.g., C52ED110-F0A3-4D72-812B-47007A98C948) |

**Response Structure:**
```typescript
{
  responseData: {
    data: {
      license: {
        keycode: string,
        product_line_description: string | null,
        license_status_id: number | null,
        license_type_description: string | null,
        license_keycode_type_id: number | null,
        max_daily_activations: number | null,
        license_expiration_date: string | null,   // ISO datetime
        parent_keycode: string | null,
        license_key: string | null,               // GUID string (= message_key)
        license_seats: number | null,
        consumed_seats: number | null,
        seats_used: number,
        storage_gb: number | null,
        license_category_name: string | null,
        license_category_description: string | null,
        start_date: string | null,
        end_date: string | null,
        days_remaining: number,
        is_expired: boolean,
        license_attribute_description: string | null,
        license_attribute_tag: string | null,
        license_attribute_license_value: number | null,  // 11 = Annual, 12 = Monthly
        license_attribute_license_value_description: string | null,
        license_attribute_last_modified: string | null,
        oem_type: string | null,
        portal_flag: number,
        renewal_count: number | null,
        license_origin_channel_name: string | null,
        license_original_activation_date: string | null,
        email_opt_in: number | null,
        license_distribution_method_code: string | null,
        next_bill_date: string | null,
        capability_type_description: string | null,
      },
      license_profile: {
        [module_name: string]: {
          license_category_name: string | null,
          license_category_description: string | null,
          license_category_id: number | null,
          license_keycode_type_id: number | null,
          category_type_name: string | null,      // "full", "upgrade", etc. — from capability type
          license_status_id: number | null,
          license_status_description: string | null,
          start_date: string | null,
          expiration_date: string | null,
          license_seats: number | null,
          storage_gb: number | null,
          license_attribute_id: number | null,
          license_attribute_description: string | null,
          license_attribute_license_value: number | null,  // 11 or 12
          license_attribute_license_value_description: string | null,
          item_hierarchy_id: number | null,
          item_hierarchy_name: string | null,
          autorenewal_cycle_name: string | null,
          autorenewal_cycle: number | null,
          usage_pricing_model_id: number | null,
          usage_pricing_model_name: string | null,
          retention_model_id: number | null,
          retention_model_name: string | null,
          retention_term: number | null,
          retention_model_type_id: number | null,
          product_platform_id: number | null,
          product_platform_name: string | null,
          license_autorenewal_value: number | null,
          product_pricing_level_id: number | null,
          pricing_level: string | null,
          pricing_level_description: string | null,
          license_vault_json: string | null,
          most_recent_order_term: number | null,
        }
      },
      upgrade_categories: {
        [module_name: string]: {
          license_category_name: string | null,          // base category name
          upgrade_license_category_name: string | null,  // upgrade target name (= the key)
          item_hierarchy_id: number | null,
          item_hierarchy_name: string | null,
        }
      },
      product_options: Array<{
        product_id: number,
        product_name: string,
        license_category_name: string | null,
        product_type_description: string | null,  // "Renewal" (type 2) or "New" (type 1)
        price: number | null,                     // retail_price from product_pricing
        years: number[],                          // available term lengths
        seats: number[],                          // available seat counts
      }>,
      keycode: string,
      license_key: string | null,
      license_status: string | null,
      product_line: string | null,
      license_category: string | null,
      license_category_description: string | null,
      license_seats: number | null,
      expiration_date: string | null,
      license_verified: boolean,
      license_site_id: null,                      // reserved, always null currently
      billing_models: [],                         // reserved, always empty currently
    }
  }
}
```

**Key Response Fields:**
- `license.license_attribute_license_value`: Determines license billing type
  - `11` = Annual billing
  - `12` = Monthly billing
- `license_profile`: Object with modules currently in the license (SS, QA_AUTOGEN, WSAC, WSAI, WSAV)
- `upgrade_categories`: Available upgrade modules (includes QA_AUTOGEN)
- `product_options`: Array of available product actions (Renewal, Upgrade, Add Seats)

**Backend Implementation Notes:**

**Stack:** `LicenseOptionsController` → `LicenseOptionsService` → `CartOrderService` → `CartOrderRepository.SelectLicenseOptionsAsync()`

**Database tables queried (in order):**
| Table / Object | Purpose |
|---|---|
| `license_key` + `license` | Resolve `keycode` from `message_key` GUID |
| `license` + `license_status` + `product_line` + `license_key` | Core license row |
| `usp_license_select_license_by_id` (SP) | Legacy license detail (expiry, seats, channel, attribute, etc.) |
| `license_type` | Fallback license type description |
| `license_parent` + `license` | Parent keycode |
| `license_active_seats` | Consumed seat count fallback |
| `license_storage` | Storage GB fallback |
| `license_attribute_license` + `license_attribute` + `license_attribute_license_value` | Billing attribute (11/12) fallback |
| `order_item_license` + `order_item` + `product` | Renewal count fallback |
| `license_history` + `license_distribution_method_channel` + `channel` | Origin channel fallback |
| `license_distribution_method` | Distribution code fallback |
| `license_next_bill_date` | Next billing date fallback |
| `customer` | Email opt-in fallback |
| `license_category_license` + `license_category` | All module categories on the license |
| `license_capability` + `capability_type` | Capability type → `category_type_name` |
| `fn_license_select_license_profile` (TVF) | Full license profile per module (SQL Server only) |
| `product_license_category_upgrade` + `license_category` + `item_hierarchy` | Available upgrade target categories |
| `license_seat` | Current seat count |
| `product_license_category` + `product` + `product_type` + `license_category` | Product options |
| `product_license_category_years` | Available term years per category |
| `product_license_category_seat` | Available seat counts per category |
| `product_pricing` | Retail price per product |

**`license_attribute_license_value` (11 vs 12):**
- Read from `license_attribute_license` → `license_attribute_license_value` column
- Primary source: `usp_license_select_license_by_id` SP result
- Fallback: direct EF query on `license_attribute_license` ordered by most recent `license_attribute_license_id DESC`
- `11` = Annual billing; `12` = Monthly billing
- This value is returned in both `license.license_attribute_license_value` (top-level) and per-module in `license_profile[module].license_attribute_license_value`

**What determines `license_profile` vs `upgrade_categories`:**
- `license_profile`: modules the license **currently has** — sourced from `fn_license_select_license_profile` TVF (or `license_category_license` fallback), grouped by `license_category_name`
- `upgrade_categories`: modules available to **add/upgrade to** — sourced from `product_license_category_upgrade` where `license_category_id = primaryCategory` AND `item_hierarchy_id = 1` AND locale matches
- A module appears in `upgrade_categories` keyed by its `upgrade_license_category_name` (e.g., `QA_AUTOGEN`)

**How `QA_AUTOGEN` is included:**
- `QA_AUTOGEN` is a `upgrade_license_category_name` in the `product_license_category_upgrade` table pointing from the base SS category
- It is NOT a separate API call — it arrives in `upgrade_categories` as part of the single `/license-options` response
- It appears there only if a row exists in `product_license_category_upgrade` with `upgrade_license_category_id` matching QA_AUTOGEN's `license_category_id` for the license's primary category

**`category_type_name` values:** Derived from `capability_type.capability_type_description` via `license_capability.base_capability_id`. Common values: `"full"`, `"upgrade"`, `null`

**`product_type_description` values:** `"New"` (product_type_id = 1), `"Renewal"` (product_type_id = 2)

**Validation rules for `message_key`:**
1. Must not be null/empty → `400 Bad Request`
2. Must be a valid GUID format → `400 Bad Request`
3. Must resolve to a `keycode` via `license_key` join → `404 Not Found`
4. `keycode` must have a matching `license` record → `404 Not Found`

**Error response structure:**
```json
// 400
{ "success": false, "errors": ["message_key must be a valid GUID"] }
// 404
{ "success": false, "message": "No license found for message_key '...'" }
// 200
{ "success": true, "data": { ...LicenseOptionsResponse } }
```

---

### 2. Bundle Pricing Endpoint

**Endpoint:** `POST /bundle-pricing`

**Purpose:** Calculates pricing for license actions (renewal, upgrade, add seats) based on current license state and requested changes.

**Request:**
```
POST http://localhost:5193/bundle-pricing?locale=en_US
Content-Type: application/x-www-form-urlencoded

Items[0].MessageKey={LICENSE_KEY}&
Items[0].Action={ACTION}&
Items[0].Quantity={QUANTITY}&
Items[0].CategoryTypeName={CATEGORY_TYPE}&
Items[0].LicenseAttributeLicenseValue={BILLING_CODE}&
Items[0].LicenseCategoryName={MODULE_NAME}
```

**Request Parameters:**
| Parameter | Type | Required | Conditional | Description |
|-----------|------|----------|-------------|-------------|
| `Items[0].MessageKey` | string | Yes | - | License key |
| `Items[0].Action` | string | Yes | - | "addseats", "upgrade", "buy", "renew" |
| `Items[0].Quantity` | number | Yes | - | Number of seats/licenses |
| `Items[0].CategoryTypeName` | string | No | Required for "buy" action | "full" for new purchases |
| `Items[0].LicenseAttributeLicenseValue` | number | No | Not sent for "addseats" | Billing code: 11 (annual) or 12 (monthly) |
| `Items[0].LicenseCategoryName` | string | No | - | Module name (SS, WSAC, WSAI, WSAV, QA_AUTOGEN) |

**Frontend Request Logic:**
```typescript
// From cartapi.ts CallBundlePricing function
if (isBuy) {
  // New purchase scenario
  requestparams["Items[0].CategoryTypeName"] = "full"
} else if (!isAddSeats) {
  // Renewal or upgrade scenario (but NOT add seats)
  const attributeValue = isMonthly ? 12 : licenseResponse.license.license_attribute_license_value
  if (attributeValue != null) {
    requestparams["Items[0].LicenseAttributeLicenseValue"] = attributeValue
  }
}
// Note: Add seats action does NOT send LicenseAttributeLicenseValue
```

**Response Structure:**
```typescript
{
  responseData: {
    data: {
      product_totals: {
        [module_name: string]: {
          annual_price: number,
          annual_price_fmt: string,          // "$299.99"
          usage_price: number,               // Monthly price (e.g., 5.83)
          usage_price_fmt: string,           // "$5.83"
          estimated_monthly_price: number,   // Same as usage_price
          estimated_monthly_price_fmt: string, // "$5.83"
          // [BACKEND_TEAM: Add other pricing fields]
        }
      },
      bundle_total: {
        annual_price: number,
        annual_price_fmt: string,
        usage_price: number,
        usage_price_fmt: string,
        estimated_monthly_price: number,
        estimated_monthly_price_fmt: string,
        // [BACKEND_TEAM: Add other bundle fields]
      },
      // [BACKEND_TEAM: Add other response fields]
    }
  }
}
```

**Key Response Fields:**
- `product_totals[module].annual_price`: Yearly price for the module
- `product_totals[module].usage_price`: Monthly price (this is THE source field for monthly pricing)
- `product_totals[module].estimated_monthly_price`: Derived from usage_price, displayed in UI
- `bundle_total`: Aggregated pricing across all modules in the request

**Backend Implementation Notes:**

**Stack:** `BundlePricingController` → `PricingService.GetBundlePricingAsync()` → `MessageKeyService.ResolveAsync()` → `PricingRepository.GetConfiguratorPricingAsync()` → `usp_cart_select_license_configurator_pricing` SP

**Pricing calculation algorithm:**
1. For each `Item` in the request, resolve the `message_key` → `keycode` + `cart_discount_id` via `MessageKeyService`
2. Build two JSON blobs: `@items_json` (array, one entry per item/module) and `@bundle_json` (cart-level context with keycode, locale, LALV, discount)
3. Call `usp_cart_select_license_configurator_pricing` SP with those JSON blobs — it returns one row per priced product
4. Each module (QA_AUTOGEN, WSAC, WSAI, WSAV) is priced via a **separate SP call** as a standalone primary item (not as hierarchy-2 sub-items), because the SP's consumer path only resolves `item_hierarchy_id = 1`
5. `MapRow()` converts each SP result row into a `PricingLineItem`
6. `ApplyTotals()` accumulates per-product and bundle-level totals, computing subtotals × quantity and formatting currency

**`usage_price` and how it is calculated:**
- `usage_price` is returned directly by the SP (`usp_cart_select_license_configurator_pricing`) from the product pricing tables
- For **consumer-path products** (SS key type), the SP does NOT populate `usage_price` — it returns 0
- Backend fallback in `PricingService.MapRow()`: if SP returns `usage_price = 0` AND `lalv == 12` (monthly request) AND `unit_price > 0`, then `usage_price = Math.Round(unit_price / 12, 2)`
- This derives the monthly price as `annual unit_price ÷ 12` (e.g., $49.99/yr → $4.17/mo)

**Why `usage_price` was $0 before the fix:**
- The SP's consumer product path (`usp_cart_select_renewal_product_set`) does not write a `usage_price` value for SS-type keys
- `product_pricing.usage_price` was NULL for QA products, so the SP returned 0
- The backend now derives it from `unit_price / 12` for monthly requests as a safe fallback

**How `LicenseAttributeLicenseValue` affects pricing:**
- Passed as `license_attribute_license_value` in both `@items_json` and `@bundle_json`
- `11` → SP returns annual pricing row (`unit_price` = annual price, `usage_price` = 0 for consumer path)
- `12` → SP attempts monthly pricing; backend derives `usage_price` from `unit_price / 12` if SP returns 0
- Default value is `1` if not supplied

**Why different Actions require different parameters:**
- `renew` / `upgrade`: require `LicenseAttributeLicenseValue` to select annual vs monthly product
- `addseats`: inherits billing cycle from the existing license — the SP resolves it from the keycode context; sending LALV would override the license's own billing type
- `buy`: requires `CategoryTypeName = "full"` because no existing license context exists; LALV is not applicable

**Database tables / objects used by the SP:**
| Object | Purpose |
|---|---|
| `usp_cart_select_license_configurator_pricing` | Main pricing SP; drives entire bundle pricing flow |
| `usp_cart_select_renewal_product_set` (called inside SP) | Resolves renewal products for consumer-type keys |
| `product_pricing` | `unit_price`, `list_price`, `usage_price` per product |
| `product`, `product_type`, `product_license_category` | Product metadata and category mapping |
| `license_category` | Category name/description |
| `license`, `license_key` | Keycode/key resolution |

**Discount / tax:**
- Discount: `calculated_discount = equivalent_year_price − unit_price` per line item; `calculated_discount_pct = sub_total_calculated_discount / sub_total_equivalent_year_price × 100` (rounded to nearest 0.5 — legacy pricing behaviour)
- Tax: not currently applied in the backend; tax fields are not present in the SP output
- Cart-level discounts: `cart_discount_id` is passed from `MessageKeyService` if a campaign discount applies

**Error scenarios:**
- No pricing rows returned → `422 Unprocessable Entity` (no matching product found for the given keycode + LALV + category)
- Invalid `message_key` format → `400 Bad Request`
- DB connectivity failure → `500 Internal Server Error` with exception details (Development only)

---

## Frontend Implementation

### Entry Point: useProductHook.ts

**File:** `site_smb/src/app/IntermediateCart/hooks/useProductHook.ts`

**Purpose:** Main React hook that orchestrates data fetching on page load

**Flow on Page Load:**
```typescript
useEffect(() => {
  // Step 1: Call license-options endpoint
  const licenseResponse = await CallLicenseOptions(key)
  
  // Step 2: Make TWO bundle-pricing calls in parallel
  const [annualResponse, monthlyResponse] = await Promise.all([
    CallBundlePricing(key, licenseResponse, "renew", seats, false),  // Annual
    CallBundlePricing(key, licenseResponse, "renew", seats, true)    // Monthly
  ])
  
  // Step 3: Process and merge data
  const products = Object.keys(licenseResponse.data.license_profile).map(module => ({
    module_name: module,
    annual_price: annualResponse.product_totals[module].annual_price,
    monthly_price: monthlyResponse.product_totals[module].estimated_monthly_price,
    // ... other fields
  }))
  
  // Step 4: Set state and render UI
  setProducts(products)
}, [key])
```

**Key Code Locations:**
- Lines 46-59: Parallel API calls for annual and monthly pricing
- Lines 82-95: Extraction of `usage_price` into `estimated_monthly_price`
- Line 99: Product generation from `license_profile` keys

---

### API Integration Layer: cartapi.ts

**File:** `site_smb/src/app/IntermediateCart/services/cartapi.ts`

#### Function: CallLicenseOptions

**Purpose:** Fetches license details and available modules

**Signature:**
```typescript
async function CallLicenseOptions(key: string): Promise<LicenseResponse>
```

**Implementation Details:**
```typescript
// Lines 129-165
export async function CallLicenseOptions(key: string) {
  // Makes GET request to /license-options
  const response = await fetch(`http://localhost:5193/license-options?locale=en_US&message_key=${key}`)
  const raw = await ProcessResponse(response)
  const d = raw.responseData.data
  
  // Frontend data enrichment:
  // 1. Map top-level expiration_date into nested license.end_date if null
  if (d.license && d.license.end_date == null && d.expiration_date) {
    d.license.end_date = d.expiration_date
  }
  
  // 2. Map expiration_date into each license_profile module
  if (d.license_profile) {
    for (const cat of Object.keys(d.license_profile)) {
      if (d.license_profile[cat].expiration_date == null && d.expiration_date) {
        d.license_profile[cat].expiration_date = d.expiration_date
      }
      
      // 3. Derive category_type_name from product_options when available
      if (d.license_profile[cat].category_type_name == null && Array.isArray(d.product_options)) {
        const hasRenewal = d.product_options.some((o: any) => 
          o.license_category_name === cat && o.product_type_description === "Renewal"
        )
        if (hasRenewal) d.license_profile[cat].category_type_name = "full"
      }
    }
  }
  
  return { ...raw, responseData: d }
}
```

**Frontend Data Enrichment:**
- Fills in null expiration dates from top-level field
- Derives `category_type_name` from product_options array
- No data invention - only mapping existing data

---

#### Function: CallBundlePricing

**Purpose:** Calculates pricing for a specific action (renew, upgrade, add seats, buy)

**Signature:**
```typescript
async function CallBundlePricing(
  key: string,
  licenseResponse: LicenseResponse,
  action: string,      // "renew", "upgrade", "addseats", "buy"
  quantity: number,
  isMonthly: boolean,
  module?: string,     // Optional: specific module to price
  isAddSeats?: boolean,
  isBuy?: boolean
): Promise<BundleResponse>
```

**Implementation Details:**
```typescript
// Lines 157-220
export async function CallBundlePricing(...params) {
  // Build request parameters
  const requestparams = {
    "Items[0].MessageKey": key,
    "Items[0].Action": action,
    "Items[0].Quantity": quantity,
  }
  
  // Conditional parameter logic
  if (isBuy) {
    // New purchase requires CategoryTypeName
    requestparams["Items[0].CategoryTypeName"] = "full"
  } else if (!isAddSeats) {
    // Renewals and upgrades require LicenseAttributeLicenseValue
    // Add seats action does not require this parameter
    const attributeValue = isMonthly ? 12 : licenseResponse.license.license_attribute_license_value
    if (attributeValue != null) {
      requestparams["Items[0].LicenseAttributeLicenseValue"] = attributeValue
    }
  }
  
  // Add module name if specified
  if (module) {
    requestparams["Items[0].LicenseCategoryName"] = module
  }
  
  // Make POST request
  const response = await fetch(
    "http://localhost:5193/bundle-pricing?locale=en_US",
    {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams(requestparams)
    }
  )
  
  return ProcessResponse(response)
}
```

**Parameter Logic by Action Type:**
```typescript
// Lines 174-180
if (isBuy) {
  // Buy action requires CategoryTypeName
  requestparams["Items[0].CategoryTypeName"] = "full"
} else if (!isAddSeats) {
  // Renewals and upgrades require LicenseAttributeLicenseValue
  // Add seats inherits billing from existing license
  const attributeValue = isMonthly ? 12 : licenseResponse.license.license_attribute_license_value
  if (attributeValue != null) {
    requestparams["Items[0].LicenseAttributeLicenseValue"] = attributeValue
  }
}
```

---

### Helper Function: ProcessResponse

**Purpose:** Standardized response processing and error handling

```typescript
async function ProcessResponse(response: Response) {
  if (!response.ok) {
    console.error(`[API ERROR] Status ${response.status}`)
    // [FRONTEND_TEAM: Add error handling logic]
  }
  
  const data = await response.json()
  return {
    responseData: data,
    status: response.status
  }
}
```

---

## Data Models

### TypeScript Interfaces

```typescript
// License response from /license-options
interface LicenseResponse {
  responseData: {
    data: {
      license: {
        end_date: string | null
        license_attribute_license_value: number  // 11 or 12
        // ... other fields
      }
      license_profile: {
        [module: string]: {
          category_type_name: string | null
          expiration_date: string | null
          current_seats: number
          // ... other fields
        }
      }
      upgrade_categories: {
        [module: string]: any  // [BACKEND_TEAM: Define structure]
      }
      product_options: Array<{
        license_category_name: string
        product_type_description: string
        // ... other fields
      }>
      expiration_date: string | null
    }
  }
  status: number
}

// Bundle pricing response from /bundle-pricing
interface BundleResponse {
  responseData: {
    data: {
      product_totals: {
        [module: string]: {
          annual_price: number
          annual_price_fmt: string
          usage_price: number              // Source of monthly pricing
          usage_price_fmt: string
          estimated_monthly_price: number  // Derived from usage_price
          estimated_monthly_price_fmt: string
          // ... other fields
        }
      }
      bundle_total: {
        annual_price: number
        annual_price_fmt: string
        usage_price: number
        usage_price_fmt: string
        estimated_monthly_price: number
        estimated_monthly_price_fmt: string
        // ... other fields
      }
    }
  }
  status: number
}
```

---

## Pricing Logic

### Annual vs Monthly Pricing

**Two Separate API Calls:**
The frontend makes TWO bundle-pricing calls on page load:

1. **Annual Call:**
   ```typescript
   CallBundlePricing(key, licenseResponse, "renew", seats, false)
   // isMonthly = false
   // Sends: LicenseAttributeLicenseValue = 11 (or from license.license_attribute_license_value)
   ```

2. **Monthly Call:**
   ```typescript
   CallBundlePricing(key, licenseResponse, "renew", seats, true)
   // isMonthly = true
   // Sends: LicenseAttributeLicenseValue = 12
   ```

**Response Processing:**
```typescript
// From useProductHook.ts lines 82-95
const monthlyPricing = monthlyResponse.product_totals[module]
const estimatedMonthly = monthlyPricing.usage_price  // This is the monthly price!

// UI displays:
// - Annual: annualResponse.product_totals[module].annual_price
// - Monthly: monthlyResponse.product_totals[module].estimated_monthly_price
```

**Key Field: `usage_price`**
- This is THE source field for monthly pricing
- Backend calculates this based on `LicenseAttributeLicenseValue = 12`
- Frontend copies this to `estimated_monthly_price` for clarity
- Displayed to user as "est. $5.83 / month"

---

### Billing Code Logic

| Code | Meaning | Sent When | Frontend Logic |
|------|---------|-----------|----------------|
| `11` | Annual billing | License is annual type | Use `license.license_attribute_license_value` |
| `12` | Monthly billing | User selects monthly OR license is monthly | Hardcoded to `12` when `isMonthly = true` |

**Decision Tree:**
```
Is this an "addseats" action?
├─ YES → Do NOT send LicenseAttributeLicenseValue
└─ NO → Is this a "buy" action?
    ├─ YES → Send CategoryTypeName = "full" instead
    └─ NO → Is monthly pricing requested?
        ├─ YES → Send LicenseAttributeLicenseValue = 12
        └─ NO → Send LicenseAttributeLicenseValue = license.license_attribute_license_value
```

---

## Module & Category Handling

### Module Types

The system supports 5 module categories:

| Module Code | Name | Source | Typical Status |
|-------------|------|--------|----------------|
| `SS` | Base/Standard | `license_profile` | Current (in license) |
| `QA_AUTOGEN` | Auto-generated QA | `upgrade_categories` | Available upgrade |
| `WSAC` | WebSecurity Advanced Complete | `license_profile` or `upgrade_categories` | Current or upgrade |
| `WSAI` | WebSecurity AI | `license_profile` or `upgrade_categories` | Current or upgrade |
| `WSAV` | WebSecurity Antivirus | `license_profile` or `upgrade_categories` | Current or upgrade |

### QA_AUTOGEN Handling

**Question 1: Is QA_AUTOGEN called separately?**
- **Answer:** NO. QA_AUTOGEN is included in the /license-options response within the `upgrade_categories` object
- It appears alongside other modules like WSAC, WSAI, WSAV
- No separate API call is needed

**Question 2: Where does QA_AUTOGEN data come from?**
- **Answer:** Backend /license-options endpoint returns it in `responseData.data.upgrade_categories.QA_AUTOGEN`
- [BACKEND_TEAM: Document the business logic for when/why QA_AUTOGEN is included]

**Question 3: Why are there two bundle-pricing calls?**
- **Answer:** To get BOTH annual and monthly pricing for the same products
- This allows users to toggle between billing cycles in the UI
- Both calls use the same module list, different `LicenseAttributeLicenseValue`

### Module List Generation

```typescript
// From useProductHook.ts
// Products are generated from license_profile keys
const modules = Object.keys(licenseResponse.data.license_profile)
// Result: ["SS", "WSAC", "WSAI", "WSAV", ...]

// QA_AUTOGEN comes from upgrade_categories, displayed separately
const upgradeModules = Object.keys(licenseResponse.data.upgrade_categories)
// Result: ["QA_AUTOGEN", ...]
```

---

## Testing Guide

### Test License Key
```
C52ED110-F0A3-4D72-812B-47007A98C948
```

### Test URL
```
http://localhost:3000/IntermediateCart/?key=C52ED110-F0A3-4D72-812B-47007A98C948
```

### Manual Testing Checklist

1. **License Validation:**
   - [ ] Page loads without errors
   - [ ] License details display correctly
   - [ ] Expiration date shows

2. **Module Display:**
   - [ ] All current modules show (SS, WSAC, WSAI, WSAV)
   - [ ] Upgrade modules show (QA_AUTOGEN)
   - [ ] Module names and descriptions are correct

3. **Annual Pricing:**
   - [ ] Annual prices display for all modules
   - [ ] Bundle total calculates correctly
   - [ ] Formatted prices show with $ symbol

4. **Monthly Pricing:**
   - [ ] Toggle to monthly billing works
   - [ ] Monthly prices are NOT $0
   - [ ] "est. $X.XX / month" displays correctly
   - [ ] Monthly pricing is reasonable (not equal to annual)

5. **Actions:**
   - [ ] Renew action works
   - [ ] Upgrade action works
   - [ ] Add seats action works
   - [ ] Buy new action works

### Expected API Behavior

**On Page Load:**
```
1. GET /license-options → 200 OK
2. POST /bundle-pricing (Annual) → 200 OK
3. POST /bundle-pricing (Monthly) → 200 OK
```

---

## Backend Documentation Requests

### Section 1: License Options Endpoint Logic

1. **Tables queried:** See the full table list in the Backend Implementation Notes above under `/license-options`.
2. **`license_attribute_license_value` (11 vs 12):** Read from `license_attribute_license` joined to `license_attribute` and `license_attribute_license_value`. Primary source is `usp_license_select_license_by_id` SP; fallback is a direct EF query ordered by `license_attribute_license_id DESC`.
3. **`license_profile` vs `upgrade_categories`:** `license_profile` = modules the license currently owns (from `fn_license_select_license_profile` TVF or `license_category_license` fallback). `upgrade_categories` = modules the license can be upgraded to (from `product_license_category_upgrade` for the primary `license_category_id`, `item_hierarchy_id = 1`, locale-matched).
4. **QA_AUTOGEN inclusion:** Included only if a matching row exists in `product_license_category_upgrade` linking the license's primary category to QA_AUTOGEN's `license_category_id`. Not guaranteed — depends on DB seeding for the product line.
5. **`message_key` validation:** Must be non-empty and a valid GUID; must resolve to a `keycode` in `license_key` → `license`; `404` returned if no match.
6. **`category_type_name` values:** Derived from `capability_type.capability_type_description`. Observed values: `"full"`, `"upgrade"`. Can be `null` if no capability row exists.
7. **`product_type_description` values:** `"New"` (`product_type_id = 1`), `"Renewal"` (`product_type_id = 2`).
8. **Error responses:** `400` for missing/invalid GUID, `404` for no license found, `200` with `license_verified: true` on success.

### Section 2: Bundle Pricing Endpoint Logic

1. **Pricing algorithm:** `message_key` → `keycode` + discount context via `MessageKeyService`; build `@items_json` + `@bundle_json`; call `usp_cart_select_license_configurator_pricing` once per item/module; map rows → `PricingLineItem`; accumulate totals × quantity.
2. **`usage_price`:** Returned by SP from `product_pricing.usage_price`. For consumer-path (SS-type) products the SP returns 0; backend derives `Math.Round(unit_price / 12, 2)` when `lalv == 12` and `unit_price > 0`.
3. **Why $0 before the fix:** `product_pricing.usage_price` was NULL in QA for consumer products; the SP's consumer path does not calculate it. Backend now falls back to `unit_price / 12`.
4. **`LicenseAttributeLicenseValue` effect:** Passed into both the item and bundle JSON blobs as `license_attribute_license_value`. Controls which product row the SP selects — `11` for annual products, `12` for monthly. Defaults to `1` if not sent.
5. **Different parameters per action:** `renew`/`upgrade` send LALV to select annual or monthly product; `buy` sends `CategoryTypeName = "full"` (no existing license context); `addseats` sends neither — the SP infers billing cycle from the keycode's existing license.
6. **`addseats` omits LALV:** Add-seats action augments the current license; the SP uses the keycode's existing `license_attribute_license_value` internally so sending one would conflict.
7. **Validation:** `message_key` must be a valid GUID; `Items` must not be empty; `Quantity` and `LicenseSeats` must be positive if supplied. Returns `422` when SP returns zero pricing rows.
8. **Quantity:** Multiplied into all subtotals (`SubTotalAmount = UnitPrice × Quantity`). Seat counts are passed to the SP as `license_seats` in the item JSON; the SP uses them for per-seat pricing.
9. **Discounts/taxes:** Discount = `EquivalentYearPrice − UnitPrice` per line, aggregated and expressed as a percentage rounded to nearest 0.5. Tax is not currently calculated.
10. **Error scenarios:** `422` = SP returned no rows (product not found for keycode/LALV/category); `400` = invalid request params; `500` = DB error.

### Section 3: Data Models

**Key backend C# models (source of truth):**
- `LicenseOptionsResponse` — top-level response; in `Models/Responses/ReadEndpointResponses.cs`
- `LicenseInfoResponse` — the `license` sub-object; same file
- `LicenseProfileEntryResponse` — each entry in `license_profile`; same file
- `UpgradeCategoryResponse` — each entry in `upgrade_categories`; same file
- `ProductOptionResponse` — each entry in `product_options`; same file
- `BundlePricingResponse` + `PricingLineItem` + `PricingTotals` — bundle pricing; in `Models/Responses/BundlePricingResponse.cs`

**Billing code enum:**
| Value | Meaning |
|---|---|
| `11` | Annual billing |
| `12` | Monthly billing |
| `1` | Default / not specified |

**Action type enum (frontend → backend):**
| Value | Meaning |
|---|---|
| `renew` | Renew existing license |
| `upgrade` | Upgrade to higher tier |
| `addseats` | Add seats to current license |
| `buy` | New purchase (no existing license) |

**Product type enum:**
| `product_type_id` | `product_type_description` |
|---|---|
| `1` | New |
| `2` | Renewal |

**Item hierarchy enum:**
| `item_hierarchy_id` | Meaning |
|---|---|
| `1` | Primary item |
| `2` | Module / secondary item |

### Section 4: Business Rules

1. **Renew vs upgrade:** Determined by `product_type_description` in `product_options`. `"Renewal"` products (`product_type_id = 2`) are returned when the category already exists on the license. `"New"`/upgrade products (`product_type_id = 1`) are returned for categories in `upgrade_categories` that aren't yet on the license.
2. **Adding seats:** The `addseats` action does not send `LicenseAttributeLicenseValue` — the SP inherits the billing cycle from the existing license. The frontend passes the current seat count as `Quantity`.
3. **Min/max seats:** Available seat options come from `product_license_category_seat` per `license_category_id`. The UI should constrain the selector to the values returned in `product_options[n].seats`. No hard-coded backend min/max beyond what the DB rows define.
4. **Module dependencies:** Not enforced in the backend pricing layer — each module is priced independently via separate SP calls. The `upgrade_categories` list implicitly defines valid add-on modules for a given base product line. The frontend controls which modules are shown together.
5. **Expired licenses:** `is_expired = true` and `days_remaining < 0` are set in the response. The SP and product option queries still execute — it is the frontend's responsibility to restrict available actions (e.g., block renewal if too far past expiry). Backend does not currently block pricing calls for expired licenses.
6. **Monthly ↔ annual conversion:** Supported — the frontend sends `LicenseAttributeLicenseValue = 11` or `12` independently of the license's current billing type. The SP will price accordingly. The `license.license_attribute_license_value` field tells the frontend what the current license type is; `isMonthly = true` overrides it to `12` for the monthly pricing call.

---

## Appendix

### File Locations

**Frontend Files:**
- `site_smb/src/app/IntermediateCart/services/cartapi.ts` - API integration
- `site_smb/src/app/IntermediateCart/hooks/useProductHook.ts` - Data fetching
- `site_smb/src/app/IntermediateCart/page.tsx` - Main page component

**Documentation:**
- `docs/IntermediateCart_Architecture.md` - This file

### Related Documentation

- Main project README: `README.md`
- Wiki: `wiki/technical-design.md`
- API documentation: Swagger UI available at `http://localhost:5193/swagger` in Development mode

---

## Change Log

| Date | Author | Changes |
|------|--------|---------|
| 2026-08-10 | Frontend Team | Initial documentation created |
| 2026-08-10 | Backend Team | Filled all backend implementation placeholders |

---

## Questions & Contact

**Frontend Questions:** [TEAM: Add contact]  
**Backend Questions:** [TEAM: Add contact]  
**Architecture Questions:** [TEAM: Add contact]

---

**END OF DOCUMENT**

*This is a living document. Please keep it updated as the system evolves.*
