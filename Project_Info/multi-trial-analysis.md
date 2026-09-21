# Multi Trial → .NET Migration Analysis

## 1. Feature Overview

The Multi Trial feature allows a customer to select multiple Webroot products and register for a 30-day trial in a single submission.

| Product | Code | Role |
|---|---|---|
| Endpoint Protection | `SAEP` | Primary / Parent |
| DNS Protection | `SDNS` | Secondary module |
| Security Awareness Training | `SECA` | Secondary module |
| EDR | `OTEDR` | Secondary module |
| MDR | `OTMDR` | Secondary module |

The .NET migration must preserve the parent + secondary-module relationship.

---

## 2. Current API

### Main Multi Trial API

```text
POST /forms/submissions
```

Current external endpoint:

```text
https://api.webroot.com/forms/submissions
```

The frontend currently reaches this through its Vercel proxy.

Multi Trial is identified by:

```json
{
  "form_name": "BusinessTrialRegistration",
  "form_type": "MULTI"
}
```

### Important distinction

There is another API:

```text
POST /module-trials
```

Do **not** use this as the Multi Trial implementation.

`/module-trials` is for adding secondary products to an already existing GSM license.

Multi Trial creates the initial trial registration with the parent and selected modules.

---

## 3. Current Request

Important fields include:

```json
{
  "form_name": "BusinessTrialRegistration",
  "form_type": "MULTI",
  "license_category_name": "SAEP",
  "license_seats": "1",
  "is_saep_selected": "1",
  "modules": [
    {
      "license_category_name": "SDNS",
      "license_seats": "1"
    },
    {
      "license_category_name": "SECA",
      "license_seats": "1"
    }
  ]
}
```

The request also contains customer, country/state, company type, marketing and other fields.

### Migration principle

Keep the existing frontend request contract wherever possible:

```text
Next.js
   ↓
same request
   ↓
.NET /forms/submissions
```

This minimizes frontend changes.

---

## 4. Current Backend Flow

```text
Multi Trial UI
       │
       ▼
POST /forms/submissions
       │
       ▼
FormsController
       │
       ├── Email verification
       ├── FormSubmit
       │
       ▼
BusinessTrialRegistration
       │
       ▼
BusinessTrialForm
       │
       ▼
TrialRegistration
       │
       ├── Determine GSM
       ├── Build registration JSON
       ├── Primary SAEP
       └── Secondary modules
       │
       ▼
usp_trial_insert_trial_registration
       │
       ▼
Skyrise / SkyIdentity
       │
       ▼
Keycode
       │
       ▼
Trial::save()
       │
       ▼
usp_trial_insert_trial
       │
       ▼
usp_license_service_insert_service
       │
       ▼
usp_license_service_insert_license
       │
       ├── License
       ├── Categories
       ├── Seats
       ├── Modules
       ├── Vault
       └── Supporting license data
       │
       ▼
Trial creation completed
       │
       ├──────────────► Email queue
       │
       └──────────────► Salesforce / Marketo queue
       │
       ▼
HTTP Response
```

The core trial/license creation is synchronous.

Email and CRM/marketing processing are handled asynchronously through queues.

---

## 5. Customer Validation and Mapping

Current customer information includes:

- First name
- Last name
- Email
- Company
- Phone
- Country
- State
- Company type
- Email opt-in

Important mappings:

```text
firstname      → first_name
lastname       → last_name
emailaddress   → customer_email
company        → company_name
phonenumber    → phone_number
```

Current company types:

```text
14 → Business
1  → Distributor
3  → Managed Services Provider
2  → Reseller
11 → MSP/VAR Hybrid
```

The existing flow performs validation for:

- Required fields
- Email
- Phone
- Country/state
- Company type
- Seat count

The .NET implementation must preserve the existing validation behavior.

---

## 6. Email Verification – Webbula

Before trial creation, the customer email passes through multiple validation steps.

```text
Customer Email
      ↓
DNS MX Lookup
      ↓
CustomerEmailVerify
      ↓
Webbula Cloud Hygiene API
      ↓
Valid / Unverified → Continue
Invalid → Reject
```

Webbula is an external email verification service used by the current PHP implementation.

Important behavior:

- DNS MX validation is performed before the Webbula call.
- Webbula validation is synchronous.
- A valid email continues the trial flow.
- An explicitly invalid Webbula result causes the request to be rejected.
- Non-successful or malformed Webbula responses are handled fail-open by the current implementation.

The .NET migration must preserve this email-validation boundary and behavior.

Webbula configuration and credentials must come from environment/secret configuration and must not be hardcoded.

---

## 7. Product Selection Logic

### Primary

```text
SAEP
```

### Secondary

```text
SDNS
SECA
OTEDR
OTMDR
```

The resulting structure is:

```text
SAEP  → hierarchy 1
SDNS  → hierarchy 2
SECA  → hierarchy 2
EDR   → hierarchy 2
MDR   → hierarchy 2
```

The UI explicitly auto-selects SAEP when EDR is selected.

Do not redesign the existing product-selection behavior during migration unless the requirement changes.

---

## 8. GSM / Trial Registration

For Multi Trial, SAEP is treated as the GSM parent.

The PHP implementation uses:

```text
license_keycode_type_id = 3
```

This causes:

```text
TrialRegistration::isGSM()
       ↓
true
```

The selected secondary products are then included in the trial registration structure.

The migration must preserve:

```text
Multi Trial
   ↓
SAEP
   ↓
GSM parent
   ↓
Selected secondary modules
```

---

## 9. Trial Registration Structure

The PHP code creates registration data conceptually similar to:

```json
{
  "license_category": [
    {
      "license_category_name": "SAEP",
      "license_seats": 1,
      "days": 30,
      "category_type_name": "trial",
      "item_hierarchy_id": 1
    },
    {
      "license_category_name": "SDNS",
      "license_seats": 1,
      "days": 30,
      "category_type_name": "trial",
      "item_hierarchy_id": 2
    }
  ]
}
```

For EDR/MDR, vault information must also be retained.

This structure drives downstream license creation and determines:

- Parent category
- Secondary category
- Seats
- Trial duration
- Vault
- License relationships

The selected secondary products are represented as secondary `license_category` entries during the current license creation flow.

---

## 10. Database / Stored Procedures

Important existing procedures include:

```text
usp_trial_insert_trial_registration
usp_trial_insert_trial
usp_license_service_insert_service
usp_license_service_insert_license
usp_license_insert_effective_object
usp_cart_select_new_product_set_ENT
usp_cart_select_campaign_cart_discount_new
usp_product_select_license_distribution_method
usp_LogError
```

### Critical business logic to preserve

- Product/category resolution
- Trial registration
- License creation
- Secondary modules
- Seats
- Vault
- Customer creation
- Keycode handling
- External integrations
- Duplicate checks
- Trial status and dates
- Email processing
- CRM/marketing processing

### Multi Trial – Required Database Tables

1. `form_submit`
2. `form_response`
3. `trial_registration`
4. `trial_registration_json`
5. `trial`
6. `customer`
7. `customer_address`
8. `license_service`
9. `license_service_json`
10. `license_service_value`
11. `license`

### Multi Trial – Indirectly Required Database Tables

1. `company_type`
2. `lu_countries`
3. `partner_account`
4. `account`
5. `partner_service_product`
6. `partner`
7. `product_line_product`
8. `license_category_product_line`
9. `license_category`
10. `product`
11. `product_license_category`
12. `product_capability`
13. `product_years`
14. `license_category_module`
15. `license_module`
16. `license_module_type`
17. `license_distribution_method`
18. `product_line_license_distribution_method`
19. `cart_discount`
20. `cart_discount_item`
21. `cart_discount_item_discount`
22. `cart_discount_item_license_module`
23. `message_campaign`
24. `message_campaign_platform`
25. `message_campaign_cart_discount`
26. `message_campaign_language_location`
27. `product_pricing`
28. `currency`
29. `product_seat`
30. `product_storage`
31. `product_license_category_seat`
32. `product_license_category_storage`
33. `license_service_type`
34. `license_service_status`
35. `license_service_value_type`
36. `license_service_value_type_alias`
37. `license_service_archive`
38. `license_service_failure`
39. `license_category_capability`
40. `capability_type`
41. `partner_license_distribution_method`
42. `vault_license_category`
43. `sky_api_license`
44. `prevx_update_license`
45. `sky_template`
46. `effective_object`
47. `effective_object_transaction_type`
48. `DBErrorLog`
49. `sfdc_trial_audit`
50. `email_message`
51. `exact_target_queue`
52. `sfdc_opportunity_update`

The indirect table list includes tables reached through the existing stored-procedure and downstream processing paths. Some are conditional depending on partner, vault, error, or specific license configuration.

---

## 11. Product / Module Configuration

The database contains the product/category/module configuration required by the Multi Trial flow.

Current Multi Trial categories:

```text
SAEP
SDNS
SECA
OTEDR
OTMDR
```

All currently map to:

```text
product_line_id = 300
cart_type_id    = 2
```

The product and module selection is configuration-driven.

```text
Product Code
     ↓
Product Configuration
     ↓
License Category
     ↓
Module / License Configuration
```

Avoid unnecessary hardcoding of database IDs in .NET.

---

## 12. EDR / MDR Vault

EDR and MDR require vault information.

Current configured data centers include:

```text
US → 131
UK → 132
```

Conceptually:

```text
EDR / MDR
    ↓
Data Center
    ↓
vault_id
```

The vault identifies the configured backend/data-center environment associated with the EDR/MDR service.

The .NET implementation must preserve vault information through the request, domain and license creation flow.

---

## 13. Keycode Generation – Skyrise / SkyIdentity

The current PHP trial registration flow uses Skyrise / SkyIdentity to generate the trial/license keycode after trial registration.

```text
Trial Registration
       ↓
Skyrise / SkyIdentity
       ↓
Keycode
       ↓
Trial / License Creation
```

The request contains information such as:

- License distribution
- License category
- Storage
- Customer name/email
- Trial duration
- Seats
- Trial flag
- License type
- License category ID
- Keycode type
- Country/ISO

### Important Behavior

Skyrise keycode generation is synchronous.

If Skyrise fails, the current PHP flow does not immediately fail the entire trial creation.

The database/license creation flow has a fallback that can generate the keycode when the keycode is still missing.

The .NET migration must preserve the Skyrise integration, failure handling and fallback behavior.

---

## 14. License Creation Flow

The current PHP implementation creates the license through:

```text
TrialRegistration
      ↓
usp_trial_insert_trial_registration
      ↓
usp_trial_insert_trial
      ↓
usp_license_service_insert_service
      ↓
usp_license_service_insert_license
```

`usp_license_service_insert_license` creates the main license-related records, including:

- License
- License categories
- Capabilities
- Seats
- Modules
- License key
- License contract
- Effective objects
- Required downstream license records

For the current Multi Trial flow, the selected secondary products are represented as secondary `license_category` entries in the registration JSON and are used during license creation.

---

## 15. Email / Notification

The current trial email is not sent directly from the API request.

The existing flow is:

```text
TrialRegistration
      ↓
Trial::sendEmail()
      ↓
email_message
      ↓
exact_target_queue
      ↓
ExactTarget / Salesforce Marketing Cloud
```

ExactTarget / Salesforce Marketing Cloud is an external email delivery service.

The email processing is asynchronous.

The .NET implementation must preserve the existing functional email behavior and queue processing.

---

## 16. Salesforce / Marketo Processing

The current Multi Trial flow creates a downstream CRM/marketing processing record.

```text
TrialRegistration
      ↓
sfdc_opportunity_update
      ↓
SfdcUpdateQueue
      ↓
Salesforce / Marketo
```

The current Multi Trial flow uses the:

```text
update_marketo
```

processing template.

This processing is asynchronous and separate from the core trial/license creation flow.

The .NET implementation must preserve the required downstream Salesforce/Marketo processing behavior.

---

## 17. Important External Dependencies

The current Multi Trial flow depends on the following external services:

| Dependency | Purpose | Processing |
|---|---|---|
| Webbula | Email verification | Synchronous |
| Skyrise / SkyIdentity | Trial keycode generation | Synchronous |
| ExactTarget / Salesforce Marketing Cloud | Trial email delivery | Asynchronous |
| Salesforce | CRM/license synchronization | Asynchronous |
| Marketo | Lead/activity processing | Asynchronous |
| DNS MX lookup | Email domain validation | Synchronous |

These integrations are part of the existing Multi Trial behavior and must be considered during migration.

---

## 18. Error Handling

The current API uses HTTP 422 for validation failures.

Important error scenarios include:

```text
Invalid email
Duplicate trial
Missing required field
Invalid company type
Invalid seat count
Invalid product
Invalid vault
```

Duplicate trial validation is performed synchronously and returns a validation error when an existing trial is found.

The .NET implementation should preserve the existing error contract where practical so the current frontend can continue handling validation errors.

---

## 19. Current Processing Characteristics

### Synchronous

```text
Request
  ↓
Validation
  ↓
Email verification
  ↓
Trial registration
  ↓
Keycode generation
  ↓
License creation
  ↓
Response
```

### Asynchronous

```text
Email
  ↓
email_message
  ↓
exact_target_queue
  ↓
ExactTarget
```

```text
CRM / Marketing
  ↓
sfdc_opportunity_update
  ↓
SfdcUpdateQueue
  ↓
Salesforce / Marketo
```

This distinction must be preserved during migration.

---

## 20. Core Migration Objective

The .NET implementation must reproduce the existing Multi Trial business behavior:

> **Create SAEP as the primary 30-day GSM trial, attach the selected SDNS/SECA/EDR/MDR products as secondary trial modules, preserve seats and EDR/MDR vault information, generate the required keycode, perform email verification, preserve notification and downstream CRM/marketing processing, and return a compatible response to the existing frontend.**

The migration should preserve the existing functional behavior while moving the relevant application/database business logic into the new .NET/EF Core implementation.
