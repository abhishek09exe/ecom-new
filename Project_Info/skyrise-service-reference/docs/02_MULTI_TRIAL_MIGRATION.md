# Multi Trial (GSM Net New Trial) — C# / EF Core Migration Guide

## 1. Overview

Multi Trial is a **specialized trial registration flow** for GSM (Global Site Manager) customers that allows registering trials for multiple business products simultaneously (SAEP + SDNS + SECA + MDR + EDR) in a single form submission. Unlike the standard cart, this does NOT process payments — it generates trial keycodes.

**Live URL**: `https://cybersecurity.opentext.com/products/endpoint-security/multi-trial/`  
**UI Flow**: Step 1 (Select Your Trials) → Step 2 (Enter Your Information) → Step 3 (Install Your Trials)

**Current Stack**: Concrete5 CMS block → `FormHandler.js` → Partner API → Lithium (PHP) → SQL Server + Skyrise  
**Target Stack**: C# ASP.NET Core Web API + EF Core + Skyrise + Salesforce

The Multi Trial form uses the **Partner API** (`/partner-api/trial-registrations`) rather than the shared `api.webroot.com/forms/submissions` endpoint. Both ultimately call the same `TrialRegistration::save()` model with its filter chain. Multi Trial sets `is_partnercart_trial = true` and uses the `PartnerCartTrial` sub-model instead of `BusinessTrialForm`.

### Products Available in Multi Trial Form
- ☐ Endpoint Protection (SAEP) — primary, required
- ☐ DNS Protection (SDNS)
- ☐ Security Awareness Training (SECA)
- ☐ Managed Detection & Response (OTMDR)
- ☐ Endpoint Detection & Response trial (OTEDR)

---

## 2. Architecture Summary

```
Browser (Concrete5 CMS)                      Partner API (this repo)
┌───────────────────────────────┐            ┌──────────────────────────────────────┐
│ form_gsm_net_new_trial block  │  XHR POST  │ apps/partner_api/controllers/        │
│ Step 1: Select products       │───────────▶│ TrialRegistrationController.php      │
│ Step 2: Enter info            │ urlencoded │ (validates modules, Pillr rules)     │
│ Step 3: Install trials        │            └──────────────────────────────────────┘
└───────────────────────────────┘                          │
         │                                                 ▼
         │                              ┌──────────────────────────────────────┐
         │                              │ li3_wr/models/trial/                 │
         │                              │ TrialRegistration::save() chain      │
         │                              │ → PartnerCartTrial (validation)      │
         │                              │ → buildTrialRegistrationInsert()     │
         │                              └──────────────────────────────────────┘
         │                                    │              │             │
         │                                    ▼              ▼             ▼
         │                              ┌──────────┐  ┌──────────┐  ┌──────────────┐
         │                              │ Skyrise  │  │ SQL DB   │  │ Email + SFDC │
         │                              │ Keycode  │  │ (procs)  │  │ (async queue)│
         │                              └──────────┘  └──────────┘  └──────────────┘
         │
         │  200 {form_response_key, entity: {trial, modules}}
         ◀──────────────────────────────────────────────────
         │
         ▼
    FormHandler.js detects form_type == 'MULTI'
    → gtmTracking.pushMultiTrialData()
    → redirect successPage?key=...&products=SAEP,SDNS,SECA
```

The `FormHandler.js` response branching (Section 1.4.3) checks `form_type == 'MULTI'` to route to `pushMultiTrialData` instead of `pushTrialData`. The `data.entity.message` guard prevents duplicate submissions from firing GTM conversion events.

---

## 3. PHP Source File Map

### 3.1 Frontend (Concrete5 CMS Block)

| File | Purpose |
|------|---------|
| `concrete5-cms/src/application/blocks/form_gsm_net_new_trial/controller.php` | Block controller — sets `businessFormType = 'MULTI'` |
| `concrete5-cms/src/application/blocks/form_gsm_net_new_trial/view.php` | 3-step trial form template (product select → info → confirm) |
| `concrete5-cms/src/application/blocks/form_gsm_net_new_trial/view.js` | JS logic — product cart management, module hidden fields, form submit |

### 3.2 JavaScript Framework

| File | Purpose |
|------|---------|
| `concrete5-cms/src/application/javascript/framework/FormHandler.js` (line 334) | Detects `form_type == 'MULTI'` → routes to multi-trial tracking |
| `concrete5-cms/src/application/javascript/framework/gtmTracking.js` (line 266) | `pushMultiTrialData()` — GTM analytics for multi-product trials |

### 3.3 API Controller

| File | Purpose |
|------|---------|
| `apps/partner_api/controllers/TrialRegistrationController.php` | REST API endpoint — validates modules, Pillr restrictions, processes trial |

### 3.4 Core Models

| File | Purpose |
|------|---------|
| `li3_wr/models/trial/TrialRegistration.php` | **Core model** — orchestrates validation, JSON build, keycode generation, DB insert |
| `li3_wr/models/trial/Trial.php` | Trial record — email sending, Eloqua params, campaign mapping |
| `li3_wr/models/trial/trial_ext/PartnerCartTrial.php` | Validation model for partner cart trials — module validation |

### 3.5 Stored Procedure Wrappers

| File | Stored Procedure |
|------|-----------------|
| `li3_wr/extensions/procedures/trial/insert/TrialRegistration.php` | `usp_trial_insert_trial_registration` |
| `li3_wr/extensions/procedures/trial/insert/Trial.php` | `usp_trial_insert_trial` |
| `li3_wr/extensions/procedures/trial/insert/TrialModule.php` | `usp_trial_insert_trial_module` |

### 3.6 External Service Integration

| File | Purpose |
|------|---------|
| `li3_wr/models/skyrise/GenerateKeycode.php` | Calls Skyrise service to generate trial keycodes |
| `li3_wr/extensions/util/EmailHtml.php` | Email rendering for trial notifications |
| `li3_wr/resources/mail_templates/trials/en/saep.html.php` | SAEP trial email |
| `li3_wr/resources/mail_templates/trials/en/saep_gsm.html.php` | SAEP GSM (multi) trial email |
| `li3_wr/resources/mail_templates/trials/en/sdns.html.php` | SDNS trial email |
| `li3_wr/resources/mail_templates/trials/en/seca.html.php` | SECA trial email |

---

### 3.7 Related Endpoint — Post-Creation Module Trial Attachment (`ModuleTrialController`)

Distinct from the GSM net-new registration flow documented above, a **separate endpoint** exists for attaching a module trial to an **already-existing** license/account, found in two places:

| File | Notes |
|------|-------|
| `apps/partner_api/controllers/ModuleTrialController.php` | Partner API variant — resolves the target license directly by `keycode` |
| `api-staging/controllers/ModuleTrialController.php` | Public-facing variant — resolves `keycode` indirectly via a `message_key` lookup (`MessageKey` model, `message_key_json` → `keycode`) before delegating to the same base model |
| `li3_wr/models/trial/ModuleTrial.php` | Shared base model for both controllers |

**Key behavior** (from `ModuleTrial.php`):
- `getLicense($entity)` finds the target license by `keycode` — this flow never creates a new `trial_registration` row.
- `getModules()`/`getModuleConfig()` build one config entry per requested module, defaulting `days` to `TRIAL_DAYS = 30` unless `trial_days` is set on the entity.
- `validatePillrModules()` enforces the same Pillr parent/sub-module dependency rules documented in Section 8 (Business Rules) — implemented here independently of `PartnerCartTrial`'s validation.
- `licenseServiceFactory()` dispatches via `Service::create(['license_service_type_name' => $entity->getLicenseServiceType(), 'service_json' => $entity->getModules()])`, where the service type is `'site_secondary_trial'` (child/site license) or `'license_secondary_trial'` (top-level license), determined by `$license->isChild()`.

This is a **third mechanism** for applying module data, alongside the two found inside the stored procedures (Section 6.2's embedded block and 6.3's standalone proc) — see the coverage gap table below. Because it operates on an existing license via a generic License Service dispatch rather than either module-insert stored proc directly, it is plausibly the actual production path used once a GSM primary trial already exists and a customer/partner adds more modules afterward. This needs confirmation from the DB/ecom team before the C# port commits to one path.

---

## 4. API Endpoint

```
POST /partner-api/trial-registrations
```

**Request Body:**
```json
{
    "first_name": "John",
    "last_name": "Doe",
    "customer_email": "john@company.com",
    "company_name": "Acme Corp",
    "company_type_name": "SMB",
    "phone_number": "555-0123",
    "address_1": "123 Main St",
    "city": "Denver",
    "state": "CO",
    "postal_code": "80202",
    "country": "US",
    "language_code": "en",
    "location_code": "us",
    "license_category_name": "SAEP",
    "license_seats": 100,
    "trial_days": 30,
    "license_keycode_type_id": 3,
    "partner_key": "uuid-partner-key",
    "account_user_name": "admin@partner.com",
    "partner_account_code": "PAC001",
    "form_type": "MULTI",
    "modules": [
        {
            "license_category_name": "SDNS",
            "license_seats": 100
        },
        {
            "license_category_name": "SECA",
            "license_seats": 100
        }
    ]
}
```

**Success Response:**
```json
{
    "form_response_key": "abc123",
    "entity": {
        "trial_registration_id": 12345,
        "license_category_name": "SAEP",
        "license_seats": 100,
        "form_type": "MULTI",
        "trial": {
            "keycode": "XXXX-XXXX-XXXX-XXXX",
            "license_key": "abc-def-ghi"
        },
        "modules": [
            { "license_category_name": "SDNS", "license_seats": 100 },
            { "license_category_name": "SECA", "license_seats": 100 }
        ]
    }
}
```

---

## 5. Core Business Logic (PHP)

### 5.1 Controller Filter (TrialRegistrationController::_init)

```php
// Pre-processing before trial creation:
1. Block PILLR_PRODUCT ('PLRP') from trials entirely
2. Parse modules array from request
3. Filter modules into:
   - eppPillrModules (OTEDR, OTMDR)
   - pillrSubModules (PLRM, PLRCS, PLRCB)
4. Block PLRCS (compliance storage) from trials
5. Validate: pillrSubModules require parent pillrModules (OTEDR/OTMDR)
6. Set is_partnercart_trial = true on entity
7. Validate vault IDs against products (ModuleHandlerService)
8. If Pillr modules: validate vault IDs, process sub-modules
9. Trial days: Pillr modules require exactly 30 days
10. On failure: return 'PartnerTrialExists' if response_code < 0
```

### 5.2 Trial Registration Save (TrialRegistration.php)

```php
// Main save() filter:
1. Detect trial type via flags → selects PartnerCartTrial model for multi-trial
2. Create typed trial entity (PartnerCartTrial::create)
3. Transform entity + validate
4. Call exportTrialRegistration() → filtered data
5. Find license distribution method code by:
   - license_keycode_type_id
   - license_category_id
   - trial = 1
   - partner_id
6. Call buildTrialRegistrationInsert() → builds JSON payload
7. Execute stored procedure chain (insert trial registration)
8. If response_code >= 0 → Generate keycode via Skyrise:
   - LicenseDistCode, LicenseCategory, Storage, FirstName, LastName
   - Email, DurationInDays, Seats, IsTrial=true
   - LicenseTypeId, LicenseCategoryId, LicenseKeycodeTypeId, iso
9. If Skyrise fails → log SkyriseFailure record
10. Save Trial record (keycode, trial_registration_id, account_order_json)
```

### 5.3 Build Trial Registration JSON (buildTrialRegistrationInsert)

```php
// Constructs the JSON passed to usp_trial_insert_trial_registration
$trialInsertData = [
    'keycode' => '',
    'license_keycode_type_id' => ...,
    'license_distribution_method_code' => ...,
    'language_code' => ...,
    'location_code' => ...,
    'license_category' => [
        [   // PRIMARY product (always SAEP for multi-trial)
            'license_category_name' => 'SAEP',
            'license_seats' => 100,
            'days' => 30,
            'category_type_name' => 'trial',
            'item_hierarchy_id' => 1,       // PRIMARY
            'vault_id' => ...,
            'usage_pricing_model_id' => ...,
            'product_platform_id' => ...,
            'start_date' => ...,
            'expiration_date' => ...,
        ],
        [   // SECONDARY modules (SDNS, SECA)
            'license_category_name' => 'SDNS',
            'license_seats' => 100,
            'days' => 30,
            'category_type_name' => 'trial',
            'item_hierarchy_id' => 2,       // SECONDARY
            'start_date' => ...,
            'expiration_date' => ...,
        ],
        // ... more modules
    ],
    'customer' => [...],
    'partner' => [
        'partner_key' => ...,
        'account_user_name' => ...,
        'partner_account_code' => ...,
        'partner_product_id' => ...
    ],
    'sfdc' => [...]
];
```

### 5.4 Module Validation (PartnerCartTrial)

```php
// Valid module categories:
const MODULE_CATEGORYS = ['SDNS', 'SECA', 'OTSF', 'CBEP', 'PLRM', 'PLRCB', 'PLRCS', 'OTEDR', 'OTMDR'];

// Enterprise required fields (when license is enterprise product):
const ENTERPRISE_REQUIRED_FIELDS = ['first_name', 'last_name', 'company_name', 'country'];

// Module validation:
- Each module must have valid license_category_name (in MODULE_CATEGORYS)
- Each module must have license_seats >= 1
- Supports legacy keys: license_module_code → license_category_name
- Supports legacy keys: license_module_seats → license_seats
```

### 5.5 Trial Email Sending (Trial.php)

```php
// For GSM Parent (multi-trial):
1. Get license profile (all products in trial)
2. For each product with item_hierarchy_id == 2: add template (sdns, seca)
3. For primary (item_hierarchy_id == 1): add 'saep_gsm' template
4. Render and send each email template separately

// Campaign mapping for GTM/SFDC:
'SAEP' => campaign_id '701F0000000dDPC'
'GSMTrial' => campaign_id '701F0000000fEt6'  // Multi-trial specific campaign
```

### 5.6 Default Trial Days

```php
const DEFAULT_TRIAL_DAYS = [
    'SAEP' => 30,
    'SAUP' => 30,
    'ADE'  => 30,
    'WSAV' => 14,
    'WSAI' => 14,
    'WSAC' => 14,
    'WIFI' => 7
];

const TRIAL_DAYS = [14, 30, 60, 90];  // Valid trial day options
```

---

## 6. Stored Procedures

### 6.1 usp_trial_insert_trial_registration

| Parameter | Type | Notes |
|-----------|------|-------|
| `first_name` | nvarchar(225) | |
| `last_name` | nvarchar(225) | |
| `company_name` | nvarchar(255) | |
| `company_type_name` | varchar(50) | |
| `phone_number` | varchar(64) | |
| `customer_email` | varchar(100) | |
| `address_1` | nvarchar(255) | |
| `address_2` | nvarchar(255) | |
| `city` | nvarchar(130) | |
| `state` | nvarchar(2) | internal state code, not state_id |
| `postal_code` | nvarchar(32) | |
| `country` | varchar(75) | |
| `language_code` | varchar(2) | |
| `location_code` | varchar(3) | |
| `license_category_name` | varchar(10) | Primary product (SAEP) |
| `license_seats` | int | |
| `years` | float | |
| `trial_days` | int | |
| `license_keycode_type_id` | int | 1 or 3 |
| `partner_key` | varchar(36) | |
| `account_user_name` | varchar(100) | |
| `partner_account_code` | varchar(50) | |
| `sfdc_lead_id` | varchar(18) | |
| `sfdc_opportunity_id` | varchar(18) | |
| `sfdc_trial_id` | varchar(18) | |
| `salesforce_campaign_id` | varchar(18) | |
| `partner_product_id` | varchar(20) | |
| `sfdc_distributor_id` | varchar(18) | |
| `sfdc_reseller_id` | varchar(18) | |
| `eloqua_parameters` | nvarchar(max) | JSON |
| `license_distribution_method_code` | char(4) | |
| `salesforce_license_id` | varchar(18) | |
| `trial_registration_json` | varchar(max) | Full trial data including modules; validated with `ISJSON`, not a native SQL JSON type |
| `opt_in` | int | Not previously documented — passed through to `usp_customer_insert_customer_service` |

**Returns on success**: `trial_registration_id`, `request_type`, `first_name`, `last_name`, `customer_email`, `product_line_id`, `license_category_name`, `license_category_id`, `license_module_list`, `license_type`, `license_type_id`, `capability_activation_days`, `license_seats`, `storage_gb`, `license_distribution_method_code` — **no `response_code` column on success**. `response_code` is only present on the duplicate (`-110`) and error (`-200`) paths. See EP Trial doc Section 7.1 for the full "absent = success" porting constraint.

**Duplicate path is not read-only**: if the incoming `sfdc_trial_id` differs from the value already stored on the matched registration, the proc writes an `sfdc_trial_audit` row and updates `trial_registration`/`sfdc_trial` before returning `-110`. See EP Trial doc Section 7.1.

### 6.2 usp_trial_insert_trial

| Parameter | Type | Notes |
|-----------|------|-------|
| `trial_registration_id` | int | FK to trial_registration |
| `keycode` | varchar(40) | Passed in from the PHP-side Skyrise call when available |
| `account_order_json` | json | Contains `account_order_staging_id` |

**Keycode fallback is DB-side, not Skyrise**: when `keycode` is null/empty, this proc generates one itself via `usp_license_generate_keycode_cart` against a hardcoded `product_id`, independent of the PHP Skyrise client. Idempotent — re-invoking with a `trial_registration_id` that already has a `trial` row skips straight to the SFDC-update step. Also contains an MSP/WorldAuth registration sync hook (`usp_sky_insert_update`, template `msp_registration`) for `request_type = 'create_master'` + `SAEP` — despite the `sky_*` naming, this queue is consumed by `MspRegistration.php`, which calls `Connections::get('mspregistration')` (`worldauth.webrootanywhere.com`), a separate legacy system, **not Skyrise**. It can also independently apply module data from `trial_registration_json` via the same routine `usp_trial_insert_trial_module` uses. Full behavior documented in EP Trial doc Section 7.2.

### 6.3 usp_trial_insert_trial_module

| Parameter | Type | Notes |
|-----------|------|-------|
| `keycode` | varchar(40) | Parent keycode |
| `trial_registration_json` | varchar(max) | Flat module fields: `license_module_code`, `days`, `module_seats` — parsed via legacy `fn_parse_json`, not `OPENJSON` |

Overlaps with the module-insertion logic embedded in `usp_trial_insert_trial` (Section 6.2) — confirm with the DB team which call site is authoritative before porting. Full behavior documented in EP Trial doc Section 7.3.

---

## 7. External Dependencies (NOT in this codebase)

| Dependency | Purpose | Notes |
|-----------|---------|-------|
| **Skyrise Keycode Service** | Generates trial keycodes | REST API at `skyrise.webroot.com` — calls `GenerateKeycode::create()` |
| **Salesforce** | Lead/opportunity/campaign tracking | Trial creates SFDC lead + campaign membership |
| **Eloqua** | Marketing automation | Sends trial params (keycode, category, seats, dates) |
| **ModuleHandlerService** | Vault ID validation for Pillr modules | Internal service class — validates vault_id assignments |
| **Partner Account validation** | Validates partner_key, partner permissions | Partner products configured in DB |

---

## 8. Validation Rules

### Primary Product Validation
```
customer_email:          valid email (required)
language_code:           valid language code (optional)
license_category_name:   notEmpty (required)
license_seats:           positive integer (required)
trial_days:              inList [14, 30, 60, 90]
years:                   inList Item::ITEM_YEARS
license_keycode_type_id: inList [1, 3] (optional)
```

### Enterprise Fields (required when enterprise product)
```
first_name:   notEmpty
last_name:    notEmpty
company_name: notEmpty
country:      notEmpty
```

### Module Validation
```
modules[].license_category_name: inList ['SDNS','SECA','OTSF','CBEP','PLRM','PLRCB','PLRCS','OTEDR','OTMDR']
modules[].license_seats:         >= 1
```

### Business Rules
- PLRP (Pillr base product) cannot be trialed
- PLRCS (compliance storage) cannot be trialed
- Pillr sub-modules (PLRM, PLRCB) require OTEDR or OTMDR parent module
- Pillr modules require exactly 30 trial days
- SAEP is required as primary product for GSM multi-trial

---

## 9. Database Tables

| Table | Purpose |
|-------|---------|
| `trial_registration` | Main trial registration record |
| `trial` | Individual trial record (keycode, dates) |
| `trial_module` | Secondary product modules linked to trial |
| `license_category` | Product category definitions |
| `license_distribution_method` | Distribution codes per category/partner |
| `skyrise_failure` | Log of failed Skyrise keycode requests |

---

## 11. C# Migration Plan

### 11.1 API Endpoint to Create

```csharp
[ApiController]
[Route("api/trials")]
public class TrialRegistrationController : ControllerBase
{
    [HttpPost("registrations")]  // Create multi-trial registration
    public async Task<ActionResult<TrialRegistrationResponse>> CreateTrialRegistration(
        [FromBody] TrialRegistrationRequest request) { }
}
```

### 11.2 EF Core Entities

```csharp
public class TrialRegistration
{
    public int TrialRegistrationId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string CompanyName { get; set; }
    public string? CompanyTypeName { get; set; }
    public string CustomerEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; }
    public string LanguageCode { get; set; }
    public string LocationCode { get; set; }
    public string LicenseCategoryName { get; set; }
    public int LicenseSeats { get; set; }
    public int TrialDays { get; set; }
    public int? LicenseKeycodeTypeId { get; set; }
    public string? PartnerKey { get; set; }
    public string? AccountUserName { get; set; }
    public string? PartnerAccountCode { get; set; }
    public string? SfdcLeadId { get; set; }
    public string? SfdcOpportunityId { get; set; }
    public string? SfdcTrialId { get; set; }
    public string? SalesforceCampaignId { get; set; }
    public string? PartnerProductId { get; set; }
    public int ResponseCode { get; set; }
    public string? TrialRegistrationJson { get; set; }
}

public class Trial
{
    public int TrialId { get; set; }
    public int TrialRegistrationId { get; set; }
    public string? Keycode { get; set; }
    public string? SalesforceTrialId { get; set; }
    public string? AccountOrderJson { get; set; }
}

public class TrialModule
{
    public int TrialModuleId { get; set; }
    public string Keycode { get; set; }
    public string LicenseCategoryName { get; set; }
    public int LicenseSeats { get; set; }
    public int ItemHierarchyId { get; set; }  // 2 = secondary
}
```

### 11.3 Request/Response DTOs

```csharp
public class TrialRegistrationRequest
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string CustomerEmail { get; set; }
    public string CompanyName { get; set; }
    public string? CompanyTypeName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string LocationCode { get; set; } = "us";
    public string LicenseCategoryName { get; set; }  // Primary: SAEP
    public int LicenseSeats { get; set; }
    public int TrialDays { get; set; } = 30;
    public int? LicenseKeycodeTypeId { get; set; }
    public string? PartnerKey { get; set; }
    public string? AccountUserName { get; set; }
    public string? PartnerAccountCode { get; set; }
    public string? FormType { get; set; }  // "MULTI"
    public List<TrialModuleRequest>? Modules { get; set; }
}

public class TrialModuleRequest
{
    public string LicenseCategoryName { get; set; }
    public int LicenseSeats { get; set; }
    public int? VaultId { get; set; }
}

public class TrialRegistrationResponse
{
    public string FormResponseKey { get; set; }
    public int TrialRegistrationId { get; set; }
    public string LicenseCategoryName { get; set; }
    public int LicenseSeats { get; set; }
    public string FormType { get; set; }
    public TrialKeycodeInfo Trial { get; set; }
    public List<TrialModuleResponse>? Modules { get; set; }
}
```

### 11.4 Services to Create

```csharp
// 1. Trial Registration Service
public interface ITrialRegistrationService
{
    Task<TrialRegistrationResult> CreateMultiTrialAsync(TrialRegistrationRequest request);
    TrialRegistrationJson BuildTrialRegistrationJson(TrialRegistrationRequest request);
}

// 2. Skyrise Keycode Service
public interface ISkyriseKeycodeService
{
    Task<KeycodeResult> GenerateTrialKeycodeAsync(KeycodeRequest request);
}

// 3. Trial Notification Service
public interface ITrialNotificationService
{
    Task SendTrialEmailsAsync(TrialRegistration registration, List<string> productCategories);
    Task SendEloquaParamsAsync(TrialRegistration registration, Trial trial);
    Task CreateSalesforceCampaignMemberAsync(string campaignId, string leadId);
}

// 4. Module Validation Service
public interface IModuleValidationService
{
    ValidationResult ValidateModules(List<TrialModuleRequest> modules, string primaryProduct);
    ValidationResult ValidatePillrVaultIds(List<TrialModuleRequest> pillrModules);
}
```

### 11.5 Stored Procedure Calls via EF Core

```csharp
// Insert trial registration
var result = await _context.Database
    .SqlQueryRaw<TrialRegistrationResult>(
        "EXEC usp_trial_insert_trial_registration " +
        "@first_name={0}, @last_name={1}, @company_name={2}, @customer_email={3}, " +
        "@license_category_name={4}, @license_seats={5}, @trial_days={6}, " +
        "@trial_registration_json={7}, ...",
        firstName, lastName, companyName, email,
        licenseCategoryName, licenseSeats, trialDays,
        trialRegistrationJson)
    .FirstOrDefaultAsync();

// Insert trial record
await _context.Database.ExecuteSqlInterpolatedAsync(
    $"EXEC usp_trial_insert_trial @trial_registration_id={registrationId}, @keycode={keycode}, @account_order_json={accountOrderJson}");

// Insert trial module
await _context.Database.ExecuteSqlInterpolatedAsync(
    $"EXEC usp_trial_insert_trial_module @keycode={keycode}, @trial_registration_json={moduleJson}");
```

### 11.6 FluentValidation Rules

```csharp
public class TrialRegistrationRequestValidator : AbstractValidator<TrialRegistrationRequest>
{
    private static readonly string[] ValidModuleCategories = 
        { "SDNS", "SECA", "OTSF", "CBEP", "PLRM", "PLRCB", "PLRCS", "OTEDR", "OTMDR" };
    private static readonly int[] ValidTrialDays = { 7, 14, 30, 45, 60, 90 };

    public TrialRegistrationRequestValidator()
    {
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.LicenseCategoryName).NotEmpty();
        RuleFor(x => x.LicenseSeats).GreaterThan(0);
        RuleFor(x => x.TrialDays).Must(d => ValidTrialDays.Contains(d));
        RuleFor(x => x.LicenseKeycodeTypeId).Must(id => id == null || id == 1 || id == 3);
        
        // Enterprise fields required for enterprise products
        When(x => IsEnterpriseProduct(x.LicenseCategoryName), () => {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.CompanyName).NotEmpty();
            RuleFor(x => x.Country).NotEmpty();
        });

        // Module validation
        RuleForEach(x => x.Modules).ChildRules(module => {
            module.RuleFor(m => m.LicenseCategoryName)
                .Must(cat => ValidModuleCategories.Contains(cat));
            module.RuleFor(m => m.LicenseSeats).GreaterThanOrEqualTo(1);
        });

        // Pillr restrictions
        RuleFor(x => x.LicenseCategoryName)
            .NotEqual("PLRP").WithMessage("Pillr base product cannot be trialed");
        RuleFor(x => x.Modules)
            .Must(modules => !modules?.Any(m => m.LicenseCategoryName == "PLRCS") ?? true)
            .WithMessage("Compliance storage cannot be trialed");
    }
}
```

### 11.7 Migration Estimate

| Component | Effort | Notes |
|-----------|--------|-------|
| API Controller + DTOs | 1-2 days | Single endpoint, request/response models |
| Trial Registration Service | 2-3 days | JSON build, orchestration logic |
| Module Validation | 1-2 days | Pillr rules, category validation, vault IDs |
| EF Core Entities + DbContext | 1 day | 3-4 entities, proc mappings |
| Skyrise Keycode Client | 1-2 days | HTTP client for keycode generation |
| Email Notification Service | 2-3 days | Multi-template email sending (SAEP + modules) |
| Salesforce Integration | 1-2 days | Campaign membership, lead updates |
| Eloqua Integration | 1 day | Parameter passing for marketing automation |
| FluentValidation Rules | 1 day | All validation logic |
| Unit Tests | 2 days | Service + validation tests |
| Integration Tests | 1-2 days | End-to-end with mocked Skyrise |
| **TOTAL** | **~14-20 days** | For one developer |

## 12. Porting Risks — Coverage Gap Assessment

Since Multi Trial shares the `TrialRegistration::save()` chain with the EP trial, most core logic gaps are shared.

### RESOLVED

| # | Gap Item | Status |
|---|----------|--------|
| 1 | `TrialRegistration::save()` chain | **RESOLVED** — full 500-line filter read, including module processing in `buildTrialRegistrationInsert()` (`item_hierarchy_id=2`, legacy key mapping, vault_id handling) |
| 2 | `trial_registration_json` shape | **RESOLVED** — includes `license_category[]` array with primary (`item_hierarchy_id=1`) + modules (`item_hierarchy_id=2`), customer/partner/sfdc/eloqua blocks |
| 3 | Stored procedure contracts | **RESOLVED** — full T-SQL source reviewed. `usp_trial_insert_trial_registration` uses OPENJSON, duplicate detection on `(email+category+keycode_type)` with a mutating audit-write side effect, success result has NO `response_code` column (null >= 0 = true in PHP). `usp_trial_insert_trial`'s keycode fallback is DB-side (`usp_license_generate_keycode_cart`), not Skyrise, and is idempotent. `usp_trial_insert_trial_module` uses legacy `fn_parse_json` and overlaps with module logic embedded in `usp_trial_insert_trial`. See EP Trial doc Sections 7.1–7.3. |
| 4 | SkyRise `GenerateKeycode` | **RESOLVED** — `BulkGenerator`, `PRODUCT_MATRIX`, `TransactionRefId`, `isTemplate` logic |
| 5 | Email rendering | **RESOLVED** — GSM multi-template loop: iterates license_profile, one template per product (saep_gsm + module templates). Japan special-casing. |
| 6 | `PartnerCartTrial` sub-model | **RESOLVED** — MODULE_CATEGORYS, enterprise required fields, module validation, legacy key support |
| 7 | Validator laxness | **RESOLVED** — state only validated for US/CA, matchingField passes when absent, email/phone more lenient than .NET defaults |
| 8 | `sky_update` queue / `usp_sky_insert_update` | **RESOLVED** — the `msp_registration` template triggered from `usp_trial_insert_trial` does **not** call Skyrise; it's consumed by `MspRegistration.php` → `Connections::get('mspregistration')` (`worldauth.webrootanywhere.com`), a separate legacy system. `sky_update` is a generic external-sync queue; only `sky_update_type_id = 4` is real Skyrise, handled by the standalone `apps/skyrise_update/` app. See EP Trial doc Section 17, item 9. |

### REMAINING — External assets only

| # | Gap Item | Risk | Notes |
|---|----------|------|-------|
| 1 | **Email template view files** | Low | Render targets in `li3_wr/resources/mail_templates/trials/` — mapping logic documented in Section 5.5. |
| 2 | **Serialisation fidelity** | **High** | Same PHP vs .NET JSON risks as EP trial — dates `Y-m-d`, empty string keycode, int hierarchy IDs. |
| 3 | **ModuleHandlerService vault validation** | Medium | `validateVaultIdsAgainstProducts()` and `processSubModules()` only partially traced. |
| 4 | **Dual module-insertion path** | Medium | Now known to be a **triple** path: the embedded block in `usp_trial_insert_trial`, the standalone `usp_trial_insert_trial_module`, and a separate `ModuleTrialController` → License Service dispatch (`site_secondary_trial`/`license_secondary_trial`) documented in Section 3.7, which operates on an existing license and looks like the real production mechanism for post-creation module attachment. Need DB/ecom team confirmation on which is authoritative before the C# port implements only one. See EP Trial doc Section 7.3. |

---

## 13. Complete Data Flow

```
1. GSM CUSTOMER visits form_gsm_net_new_trial CMS page

2. STEP 1 - Product Selection (view.js):
   ├── SAEP always shown as primary (required)
   ├── SDNS shown as optional add-on
   ├── SECA shown as optional add-on
   └── Each product has configurable seat count (default 5)

3. STEP 2 - Customer Info:
   ├── First/Last Name
   ├── Company Name
   ├── Email
   ├── Phone
   ├── Address
   └── Country

4. STEP 3 - Review & Submit:
   └── view.js::submitTrialOrder()
       ├── Validate SAEP selected (primary required)
       ├── Build hidden form fields:
       │   ├── license_category_name = "SAEP"
       │   ├── license_seats = 100
       │   ├── form_type = "MULTI"
       │   ├── modules[0][license_category_name] = "SDNS"
       │   ├── modules[0][license_seats] = 100
       │   ├── modules[1][license_category_name] = "SECA"
       │   └── modules[1][license_seats] = 100
       └── POST to trial registration API

5. API (TrialRegistrationController::create):
   ├── Block Pillr product trials
   ├── Validate module categories + seats
   ├── Set is_partnercart_trial = true
   └── Pass to TrialRegistration model

6. MODEL (TrialRegistration::save):
   ├── Detect type → PartnerCartTrial
   ├── Validate (enterprise fields + modules)
   ├── Find license distribution code
   ├── buildTrialRegistrationInsert():
   │   ├── Primary: SAEP, item_hierarchy_id=1
   │   ├── Module: SDNS, item_hierarchy_id=2
   │   └── Module: SECA, item_hierarchy_id=2
   ├── EXEC usp_trial_insert_trial_registration
   ├── Generate keycode via Skyrise
   ├── EXEC usp_trial_insert_trial
   └── Return trial_registration_id + keycode

7. RESPONSE → FormHandler.js:
   ├── Detect form_type == 'MULTI'
   ├── gtmTracking.pushMultiTrialData()
   │   ├── Track SAEP seats + dates
   │   ├── Track SDNS seats + dates
   │   ├── Track SECA seats + dates
   │   └── Calculate eACV/eTCV
   └── Redirect to success page with products in query

8. POST-PROCESSING:
   ├── Trial.php::sendEmail() → sends per-product templates
   ├── Eloqua params sent for marketing automation
   └── SFDC campaign membership created
```
