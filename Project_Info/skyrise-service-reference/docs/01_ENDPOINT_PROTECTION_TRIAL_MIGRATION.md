# Endpoint Protection Trial (Single Product) — C# / EF Core Migration Guide

## 1. Overview

The Endpoint Protection trial is a **single-product trial registration** form that posts to the shared `POST /forms/submissions` API endpoint. It uses the `form_trial_business` CMS block with `license_category_name = 'SAEP'` and `formName = 'BusinessTrialRegistration'`.

**Live URL**: `https://cybersecurity.opentext.com/products/endpoint-security/endpoint-protection/free-trial/`  
**UI Flow**: Step 1 (Register Your Trial) → Step 2 (Create Account) → Step 3 (Install Trial)

**Current Stack**: Concrete5 CMS block → `FormHandler.js` → `POST api.webroot.com/forms/submissions` → Lithium (PHP) → SQL Server + Skyrise  
**Target Stack**: ASP.NET Core 8/9 Web API + EF Core + Skyrise

The route and controller for `/forms/submissions` were originally in a separate repository (`gitlab:ecomwebdev/apps/api`). A staging extract is available in **`api-staging/`**, including `FormsController.php`, form-specific models such as `BusinessTrialRegistration.php`, and route definitions. The separate License API is available in `license_api-staging/`.

---

## 2. Architecture (from Technical Design Document)

```
Browser (Concrete5 CMS)                    Apache vhost: api.webroot.com
┌─────────────────────────┐                ┌────────────────────────────────────┐
│ form_trial_business      │  XHR POST     │ mod_headers (CORS allow-list)      │
│ data-plugin=FormHandler  │──────────────▶│ mod_security2 (rate limit 20/300s) │
│ data-action=/forms/      │  urlencoded   │ mod_rewrite → index.php            │
│   submissions            │               └────────────────────────────────────┘
└─────────────────────────┘                              │
         │                                               ▼
         │                              ┌────────────────────────────────────┐
         │                              │ Lithium Dispatcher → Router        │
         │                              │ Forms/Submissions Controller       │
         │                              │ (api-staging/ in this workspace)   │
         │                              └────────────────────────────────────┘
         │                                               │
         │                              ┌────────┬───────┴────────┬──────────┐
         │                              ▼        ▼                ▼          ▼
         │                        FormSubmit  TrialRegistration  FormResponse  Queue
         │                        (audit)     + save() chain    (response key) (async)
         │                              │        │                             │
         │                              ▼        ▼                             ▼
         │                        usp_form_  usp_trial_insert_  Skyrise   Marketo/
         │                        insert_    trial_registration  Keycode   Eloqua/SFDC
         │                        form_submit                    Service   (cron worker)
         │
         │  200 {form_response_key, entity}
         ◀─────────────────────────────────
         │
         ▼
    gtmTracking.pushTrialData()
    redirect → successPage?key=form_response_key
```

---

## 3. PHP Source File Map

### 3.1 CMS Block (Frontend)

| File | Purpose |
|------|---------|
| `concrete5-cms/src/application/blocks/form_trial_business/controller.php` | Block controller — assembles `data-options` JSON, maps product config (SAEP, GSM, ADE, SAWS, SA, MULTI) |
| `concrete5-cms/src/application/blocks/form_trial_business/view.php` | Form HTML — posts to `api.webroot.com/forms/submissions` via `data-plugin="FormHandler"` |

### 3.2 JavaScript Client

| File | Purpose |
|------|---------|
| `concrete5-cms/src/application/javascript/framework/FormHandler.js` | jQuery plugin — country/state lists, UTM/SC fields, GA hidden fields, submit, response branching |
| `concrete5-cms/src/application/javascript/framework/gtmTracking.js` | `pushTrialData()` — GTM event on success |

### 3.3 Server-Side Models (li3_wr — this repo)

| File | Purpose |
|------|---------|
| `li3_wr/models/form/FormSubmit.php` | Audit record — writes raw POST to `form_submit` table BEFORE validation |
| `li3_wr/models/trial/TrialRegistration.php` | **Core orchestrator** — `save()` filter chain: validate → transform → insert → keycode → email → queue |
| `li3_wr/models/trial/trial_ext/BusinessTrialForm.php` | Validation rules + transform logic for business trials (SAEP, SAUP, SAWS, ADE, CBEP, OTSF) |
| `li3_wr/models/trial/Trial.php` | Trial record — email sending, Eloqua params, campaign mapping |
| `li3_wr/models/form/FormResponse.php` | Response handle — stores response JSON, returns `form_response_key` |
| `li3_wr/models/skyrise/GenerateKeycode.php` | Skyrise API call — generates trial keycode |

### 3.4 Stored Procedure Wrappers

| File | Stored Procedure |
|------|-----------------|
| `li3_wr/extensions/procedures/form/insert/FormSubmit.php` | `usp_form_insert_form_submit` |
| `li3_wr/extensions/procedures/trial/insert/TrialRegistration.php` | `usp_trial_insert_trial_registration` |
| `li3_wr/extensions/procedures/trial/insert/Trial.php` | `usp_trial_insert_trial` |
| `li3_wr/extensions/procedures/form/insert/FormResponse.php` | `usp_form_insert_form_response` |

### 3.5 Controller (in api-staging/)

| File | Purpose |
|------|--------|
| `api-staging/controllers/FormsController.php` | **The actual controller** — `createFormSubmit()` action for `POST /forms/submissions` |
| `api-staging/config/routes/form.php` | Route definitions — `/forms/submissions`, `/forms/responses/{key}`, `/forms/countries` |
| `api-staging/models/FormSubmit.php` | Extends `wr\models\form\FormSubmit` — adds `ls` → `lead_source` override |
| `api-staging/models/form/BusinessTrialRegistration.php` | Extends `TrialRegistration` — sets `is_business_online_trial=true`, filters response fields |
| `api-staging/models/form/ConsumerTrialRegistration.php` | Extends `TrialRegistration` — sets `is_consumer_online_trial=true` |

### 3.6 License API (in license_api-staging/ — separate service)

| File | Purpose |
|------|--------|
| `license_api-staging/openapi.yaml` | OpenAPI spec for `license-api.webroot.com` — license + customer lookup |
| `license_api-staging/src/controllers/LicenseController.php` | License data retrieval (not part of trial flow) |

---

## 4. API Endpoint

```
POST https://api.webroot.com/forms/submissions
Content-Type: application/x-www-form-urlencoded (NOT JSON)
```

**Request (form-urlencoded):**
```
form_name=BusinessTrialRegistration
form_type=SAEP
first_name=John
last_name=Doe
customer_email=john@company.com
company_name=Acme+Corp
phone_number=555-0123
license_seats=5
license_category_name=SAEP
country=US
state=CO
language_code=en
location_code=us
company_type_id=14
campaign_id=7013z000001vdjvAAA
m_campaign_id=7013z000001vdjvAAA
lead_source=Trial
SMBC_Email_Opt_In__c=1
utm_source=...
utm_medium=...
```

**Success Response (200):**
```json
{
    "form_response_key": "abc123-uuid",
    "entity": {
        "license_category_name": "SAEP",
        "form_type": "SAEP",
        "trial": {
            "keycode": "XXXX-XXXX-XXXX-XXXX",
            "license_key": "abc-def-ghi"
        }
    }
}
```

**Validation Failure (422):**
```json
{
    "errors": {
        "first_name": "ERR_EMPTY_FIRST_NAME",
        "customer_email": "ERR_INVALID_EMAIL",
        "company_type_id": "ERR_INVALID_COMPANY_TYPE"
    }
}
```

**Duplicate Trial (200 with message — suppresses GTM):**
```json
{
    "form_response_key": "abc123-uuid",
    "entity": {
        "message": "Trial already exists for this category"
    }
}
```

---

## 5. Core Business Logic

### 5.0 FormsController::createFormSubmit() — The Actual Controller (api-staging/)

This is the thin controller that was previously in a separate repo. Now available at `api-staging/controllers/FormsController.php`:

```php
public function createFormSubmit() {
    $data = $this->request->data;

    // 1. Trim whitespace from key fields
    foreach (['first_name','last_name','customer_email','phone_number','company_name','comments'] as $f) {
        $data[$f] = preg_replace('/\s+/', ' ', trim($data[$f]));
    }

    // 2. Webbula email verification (for BusinessTrialRegistration + others)
    $emailVerifyErrors = $this->verifyCustomerEmail($data['form_name'], $data['customer_email']);
    if (!empty($emailVerifyErrors)) return $this->errorResponse('VALIDATION', $emailVerifyErrors);

    // 3. Japan hack — force state=1
    if (strtoupper($data['country']) == 'JP') $data['state'] = 1;

    // 4. Create FormSubmit (audit record — ALWAYS first)
    $submit = FormSubmit::create([
        'ip_address' => $this->request->env('REMOTE_ADDR'),
        'form_json'  => json_encode($data),
        'form_url'   => $this->request->referer(),
        'form_name'  => $data['form_name']
    ]);

    // 5. Locate form-specific model (BusinessTrialRegistration, ConsumerTrialRegistration, etc.)
    $model = Libraries::locate('api_form', $submit->form_name);
    if (!$model) return $this->errorResponse('VALIDATION', ['form_name' => 'Invalid Form name']);

    // 6. Save audit record to DB (usp_form_insert_form_submit)
    if (!$submit->save()) return $this->errorResponse('VALIDATION', $submit->errors());

    // 7. Create + save form-specific entity (triggers TrialRegistration::save() chain)
    $entity = $model::create(['form_submit_id' => $submit->id()] + json_decode($submit->form_json, true));
    if (!$entity->save()) return $this->errorResponse('VALIDATION', $entity->errors());

    // 8. Create FormResponse (stores response JSON, generates form_response_key)
    $submit->set(['entity' => $entity]);
    $response = FormResponse::create([
        'form_submit_id' => $submit->id(),
        'form_response'  => json_encode($submit->data())
    ]);
    if (!$response->save()) return $this->errorResponse('FORM_SUBMIT_FAIL');

    // 9. Return form_response_key + entity to client
    $submit->set(['form_response_key' => $response->form_response_key]);
    $this->_render['data'] = $submit->data();
    $this->_render['status'] = 201;
    return $submit;
}
```

**BusinessTrialRegistration** (`api-staging/models/form/BusinessTrialRegistration.php`) extends `TrialRegistration` and:
- Sets `is_business_online_trial = true` (triggers `BusinessTrialForm` sub-model)
- On success, filters response to schema fields only (strips internal data from browser response)
- Returns `trial.trial_id`, `trial.trial_registration_id`, `trial.license_key`, `trial.keycode`

**Webbula email verification** runs for these form names: `ConsumerTrialRegistration`, `BusinessTrialRegistration`, `TrialWSATContactUs`, `TrialDemoContactUs`, `BrightCloudContactUs`, `SystemAnalyzerTrial`, `PartnerContactUs`

### 5.1 Request Lifecycle (from PDF Section 1.3)

1. **FormHandler.js** on page load: `GET /forms/countries` → populate selects, preselect from `window.locale`
2. On submit: `fillGAHiddenFields` → `setUTMFields` + `setSCfield` (m_sc cookie) → `disableForm` → serialize form → POST
3. **Apache edge**: CORS origin check + mod_security rate counter++
4. **Controller** (`api-staging/controllers/FormsController.php`): routes to `FormSubmit` + `TrialRegistration`
5. **FormSubmit::save()**: writes audit record (`usp_form_insert_form_submit`) — ALWAYS, BEFORE validation
6. **TrialRegistration::save()** filter chain (see below)
7. **Response**: 200 with `form_response_key` + entity → client deletes m_sc cookie, GTM push, redirect

### 5.2 TrialRegistration::save() Filter Chain

```
save(entity)
  │
  ├── years = 0
  ├── locale → language_code + location_code
  ├── trial_days empty? → DEFAULT_TRIAL_DAYS (SAEP=30, WSAV/WSAI/WSAC=14, WIFI=7)
  │
  ├── Pick sub-model by flag:
  │   └── is_business_online_trial → BusinessTrialForm
  │
  ├── validates? → NO → copy errors up, return false → 422
  │
  ├── transformEntity() + exportTrialRegistration()
  │   ├── SAEP → license_keycode_type_id = 3 (parent/GSM)
  │   ├── language_code == 'ja' → license_keycode_type_id = 3
  │   ├── form_type == 'GSM' → force keycode_type=3, category='SAEP'
  │   ├── license_distribution_method_code == 'BRNC' → trial_days = 43
  │   └── Always: campaign_id, send_email=true, insert_queue_record=true
  │
  ├── license_distribution_method_code not set? → lookup from DB
  │   └── Category by product_lines (300,200,100,50,51,55,25)
  │       then LicenseDistributionMethodCode (trial=1)
  │
  ├── Country/State lookup (state_iso3 → internal state_id)
  │
  ├── buildTrialRegistrationInsert()
  │   └── required: customer_email, license_category_name, license_seats, trial_days
  │
  ├── EXEC usp_trial_insert_trial_registration
  │   └── response_code >= 0? → continue
  │   └── response_code < 0? → error "Trial already exists" → 422
  │
  ├── Skyrise GenerateKeycode (POST /api/v1/bulk)
  │   └── Success? → keycode from licenseKey
  │   └── Failure? → log SkyriseFailure, keycode=null (DB fallback, NOT fatal)
  │
  ├── Trial::create + save (usp_trial_insert_trial)
  ├── Attach license / license_profile / customer
  ├── send_email? → trial->sendEmail()
  ├── insert_queue_record? → OpportunityUpdate (update_marketo)
  └── Return true → 200
```

### 5.3 BusinessTrialForm Validation Rules

| Field | Rule | Error Code |
|-------|------|-----------|
| `first_name` | notEmpty | `ERR_EMPTY_FIRST_NAME` |
| `last_name` | notEmpty | `ERR_EMPTY_LAST_NAME` |
| `company_name` | notEmpty | `ERR_EMPTY_COMPANY_NAME` |
| `phone_number` | phone | `ERR_INVALID_PHONE` |
| `customer_email` | email | `ERR_INVALID_EMAIL` |
| `confirm_customer_email` | matchingField (required=false) | `ERR_EMPTY_CONFIRM_CUSTOMER_EMAIL` |
| `country` | countryISO | `ERR_INVALID_COUNTRY` |
| `state` | state | `ERR_INVALID_STATE` |
| `language_code` | languageCode | `ERR_EMPTY_LANGUAGE_CODE` |
| `location_code` | locationCode | `ERR_EMPTY_LOCATION_CODE` |
| `license_seats` | positiveInteger + inRange 0..1000000 | `ERR_EMPTY_LICENSE_SEATS` |
| `company_type_id` | inList [14,1,3,2,11] | `ERR_INVALID_COMPANY_TYPE` |
| `license_category_name` | inList [SAEP,SAUP,SAWS,ADE,CBEP,OTSF] | `ERR_EMPTY_LICENSE_CATEGORY_NAME` |

### 5.4 Transform Rules (BusinessTrialForm::transformEntity)

| Condition | Effect |
|-----------|--------|
| `license_category_name == 'SAEP'` | `license_keycode_type_id = 3` (parent/GSM) |
| `language_code == 'ja'` | `license_keycode_type_id = 3` (Japan always parent) |
| `form_type === 'GSM'` | Force `license_keycode_type_id = 3` AND `license_category_name = 'SAEP'` |
| `license_distribution_method_code == 'BRNC'` | `trial_days = 43` |
| Always | `campaign_id = utm_campaign`; `send_email = true`; `insert_queue_record = true` |

### 5.5 Eloqua Parameters (packed into trial_registration_json)

```json
{
    "elqFormName": "TrialRequest",
    "elqSiteID": "323",
    "key": "65",
    "lead_source": "Trial",
    "utm_source": "...",
    "utm_medium": "...",
    "utm_campaign": "...",
    "SMBC_Email_Opt_In__c": "1",
    "sFDCEmailOptOut1": "0"
}
```

---

## 6. Product Configuration (from controller.php)

```php
'SAEP' => [
    'name'  => 'Endpoint Protection',
    'formName' => 'BusinessTrialRegistration',
    'elqFormName' => 'TrialRequest',
    'marketoFormName' => 'EndPoint GSM Protection Trial Form',
    'marketoPageName' => 'WWW Endpoint Trial',
    'memberStatus' => 'Trialed',
    'trackCompanyType' => true,
    'key' => '65',
    'extraFields' => [
        'campaign_id' => '7013z000001vdjvAAA',
        'm_campaign_id' => '7013z000001vdjvAAA',
        'lead_source' => 'Trial',
        'tvm_group' => 'GSM_Trial',
    ]
]
```

---

## 7. Stored Procedures

| Procedure | Parameters | Purpose |
|-----------|-----------|---------|
| `usp_form_insert_form_submit` | `form_name` (varchar 50), `ip_address` (varchar 24), `form_url` (varchar 500), `form_json` (nvarchar max), `insert_by` (varchar 20) | Audit record — written FIRST, never rolled back |
| `usp_trial_insert_trial_registration` | `trial_registration_json` (json) + 25 scalar params (legacy) | Creates trial registration. Proc is driven almost entirely by `trial_registration_json` via `OPENJSON`. |
| `usp_trial_insert_trial` | `trial_registration_id` (int), `keycode` (varchar 40), `account_order_json` (json) | Creates trial record |
| `usp_form_insert_form_response` | `form_submit_id` (int), `form_response` (text), `form_response_key` (varchar) | Stores response, returns opaque key for success page |
| `usp_sfdc_insert_sfdc_opportunity_update` | `sfdc_template_name`, `form_submit_id`, `trial_id` | Queues async Marketo/SFDC sync |
| `usp_insert_email_receipts_dl_instructions` | (email body, recipients) | Queues trial confirmation email |

### 7.1 usp_trial_insert_trial_registration — Critical Behaviour (verified against proc source)

**Duplicate detection** → `response_code = -110`: matched on (`customer_email` + `license_category_name` + `license_keycode_type_id`), or on `sfdc_trial_id`. This path is **not read-only**: if the incoming `sfdc_trial_id` differs from the value already stored on the matched registration, the proc still writes before returning the duplicate response — it inserts an audit row into `sfdc_trial_audit`, updates `trial_registration.sfdc_trial_id`, and updates `sfdc_trial.salesforce_trial_id`. The C# port must replicate this write, not just short-circuit on duplicate.

**`trial_registration_json` storage**: persisted into a separate **child table** `trial_registration_json` keyed by `trial_registration_id` — it is not a column on `trial_registration` itself.

**Server-side defaults applied by the proc:**
- `license_keycode_type_id` → 1 if null
- `language_code` → `'EN'` if empty
- `location_code` → `'USA'` if empty; length-2 codes converted to iso3
- `salesforce_campaign_id` length-15 → converted to 18-char via `fn_ConvertSalesforceID_15to18`
- `company_type_name` — when parsed from JSON, resolved via a lookup against the `company_type` table (`company_type_id → company_type_name`); this is a **different mapping mechanism** than the hardcoded string remap used in `usp_trial_insert_trial` (see 7.2) — the two procs do not agree on how they normalize company type
- Derives `product_id`, `product_line_id`, `license_type`, `request_type` (`create_master` if keycode_type 3, else `create_key`)

**Dynamic product/distribution-method resolution** (when `product_id` isn't already resolved from `partner_product_id`):
- Enterprise product lines (`55`, `300`) resolve via `usp_cart_select_new_product_set_ENT`; all other product lines via `usp_cart_select_new_product_set`
- `license_type_id`/`license_type` derived from a product-line × capability-type matrix (enterprise/consumer × trial/full)
- `license_distribution_method_code = 'NFRS'` is preserved as-is; any other non-null value passed in is **overridden** by the partner's configured method (`partner_license_distribution_method`) if one exists for that partner; if still unresolved, `usp_product_select_license_distribution_method` picks one dynamically. The passed-in code cannot be treated as authoritative for non-NFRS partners.

**Success result set columns** (no `response_code` column on success):
`trial_registration_id`, `request_type`, `first_name`, `last_name`, `customer_email`, `product_line_id`, `license_category_name`, `license_category_id`, `license_module_list`, `license_type`, `license_type_id`, `capability_activation_days`, `license_seats`, `storage_gb`, `license_distribution_method_code`

> **Porting Constraint — Successful Result Shape**: On the SUCCESS path, the result set has **no `response_code` column**. In PHP, `$entity->response_code` is `null`, and `null >= 0` evaluates to `true` → keycode generation proceeds. Only duplicate (`-110`) and error (`-200`) paths return a `response_code`. The C# port must reproduce this "absent = success" convention exactly — do NOT default `response_code` to 0.

**SQL type length caps to watch:**
- `ip_address varchar(24)` — IPv6 addresses may truncate
- `form_name varchar(50)`
- `first_name`/`last_name` `nvarchar(225)`, `company_name`/`address_1`/`address_2` `nvarchar(255)`, `city nvarchar(130)`, `postal_code nvarchar(32)`
- `license_category_name varchar(10)`
- `state nvarchar(2)` — internal state_id, not state_iso3
- `license_distribution_method_code char(4)`
- `trial_registration_json` is declared `varchar(max)` (validated with `ISJSON`), not a native SQL Server JSON type — there is no such type

### 7.2 usp_trial_insert_trial — Critical Behaviour (verified against proc source)

**Idempotency**: if a `trial` row already exists for the given `trial_registration_id`, the proc skips license/customer creation entirely and jumps straight to the SFDC-update section. Safe to invoke more than once for the same registration.

**Keycode fallback is NOT Skyrise**: when `@keycode` is null/empty, the proc generates one itself via `usp_license_generate_keycode_cart` against a hardcoded `product_id = 152200130`, sets `ecom_create_license = 1` back into `trial_registration_json`, then creates the license record via `usp_license_service_insert_service @license_service_type_name = 'license_create'`. Skyrise is only ever called from the PHP layer *before* this proc runs — this proc's own fallback path is a separate, product-agnostic, DB-side license generator, not a Skyrise call.

**Single-seat enforcement**: for any `license_category_name` other than `WIFI`, when the resolved `product_line_id` is not enterprise (`55`/`300`), `license_seats` is forced to `1` regardless of the input value.

**MSP/WorldAuth registration sync hook**: when `request_type = 'create_master'` and `license_category_name = 'SAEP'`, the proc calls `usp_sky_insert_update` using the `msp_registration` row from `sky_template`. Despite the `sky_*` naming, this does **not** call Skyrise — the queue is consumed by `apps/cron/extensions/queue/sky/template/MspRegistration.php`, which calls `Connections::get('mspregistration')`, pointing at `worldauth.webrootanywhere.com` (a separate legacy MSP registration system). The `sky_update` table is a generic external-sync queue shared by many unrelated integrations (WorldAuth, BrightCloud DNS, Office 365, and — for `sky_update_type_id = 4` only — the real Skyrise API via the standalone `apps/skyrise_update/` app). The genuine synchronous Skyrise call for keycode generation is the `GenerateKeycode.php` → `Connections::get('skyrise')` call already documented above; it is separate from this queue hook.

**Module insertion can also happen here**: if `trial_registration_json` contains module fields (`license_module_code`, `days`, `module_seats`), this proc resolves the module's `product_id` and applies it via `usp_license_update_license_module_license` — the same routine `usp_trial_insert_trial_module` calls independently (see 7.3). A **third, separate mechanism** also exists outside the trial-creation stored procs entirely: `ModuleTrialController` (present in both `apps/partner_api/` and the newer `api-staging/`) backed by `li3_wr/models/trial/ModuleTrial.php` looks up an **existing** license by keycode (or by `message_key` in the api-staging variant) and attaches modules via `Service::create(['license_service_type_name' => 'site_secondary_trial' | 'license_secondary_trial', 'service_json' => ...])` — a generic License Service dispatcher, not a direct call to either module proc. This is likely the actual production path for attaching a module trial to an account *after* the primary trial exists; confirm with the DB/ecom team which of the three mechanisms (embedded block, standalone proc, License Service dispatch) is authoritative before the C# port picks one.

**Company type normalization** (legacy non-JSON path only) uses a **hardcoded string remap**, distinct from the lookup-table approach in `usp_trial_insert_trial_registration`: `'Managed Service Provider' → 'MSP'`, `'MSP/VAR (Hybrid)' → 'MSP/Reseller Hybrid'`, `'Value Added Reseller' → 'Reseller'`, `'' → 'Business'`.

**Side effects on success**: inserts `partner_trial` (when `partner_id` resolved) and `sfdc_opportunity_update` using template `update_license` — a queue entry distinct from the `UpdateMarketo`/`UpdateEloquaTrial` templates in Section 14.

**WIFI-only branch**: if `license_category_name = 'WIFI'` and an `account_order_staging_id` is present, calls `usp_account_order_finalize`. Not relevant to the EP/GSM flows but present in the shared proc.

**Error path**: `response_code = -200`, logged via `usp_LogError`.

### 7.3 usp_trial_insert_trial_module — Critical Behaviour (verified against proc source)

This proc parses JSON via the legacy `dbo.fn_parse_json` table-valued function, not `OPENJSON` — it predates the refactor applied to the other two procs. It expects flat top-level fields (`license_module_code`, `days`, `module_seats`) in `trial_registration_json`, resolves the module's `product_id` via `product_license_module` + `license_module` (filtered to `product_type_id = 4`), and applies it with `usp_license_update_license_module_license` — the same call used internally by `usp_trial_insert_trial`'s own module block (see 7.2). A third path (`ModuleTrialController` → License Service dispatch, see 7.2) exists outside both of these procs. Confirm with the DB team which of the three mechanisms is authoritative for GSM module attachment before porting; running more than one could double-apply modules.

---

## 8. External Dependencies

| Dependency | Purpose | In This Repo? |
|-----------|---------|--------------|
| **Skyrise** (`skyrisesvc.webrootcloudav.com`) | Keycode generation — `POST /api/v1/bulk` | SDK in `skyrise/` — YES |
| **SkyIdentity** | OAuth token for Skyrise | SDK in `skyidentity/` — YES |
| **Marketo** | Lead sync (via cron queue) | Worker in `apps/cron/` — YES |
| **Eloqua** | Marketing automation (via queue, fallback sync) | Worker in `apps/cron/` — YES |
| **Salesforce** | Lead/opportunity sync (via queue) | Worker in `apps/cron/` — YES |
| **Controller** (`api-staging/`) | Route + controller for `/forms/submissions` | **YES — extracted to api-staging/** |

---

## 9. Data Model

```
form_submit (PK: form_submit_id)
  ├── form_name, ip_address, form_url, form_json
  │
  ├──▶ form_response (FK: form_submit_id)
  │      └── form_response_key (opaque handle returned to browser)
  │
  ├──▶ opportunity_update (FK: form_submit_id, trial_id)
  │      └── sfdc_template_name = 'update_marketo'
  │
  └──▶ trial_registration (PK: trial_registration_id)
         ├── trial_registration_json, response_code
         └──▶ trial (FK: trial_registration_id)
                ├── keycode, salesforce_trial_id
                └──▶ license → license_profile
```

---

## 10. Non-Negotiable Client Contract (from PDF Section 3.4)

The .NET endpoint must be **byte-compatible** with PHP because `FormHandler.js` cannot be redeployed simultaneously.

| # | Constraint | .NET Action |
|---|-----------|-------------|
| 1 | Request is `application/x-www-form-urlencoded` | `[FromForm]` + `[Consumes(...)]`, NOT `[FromBody]` |
| 2 | Unknown fields must survive (form_json stores entire POST) | Capture `Request.Form` into `Dictionary<string,string>` |
| 3 | JSON output is `snake_case` | `JsonNamingPolicy.SnakeCaseLower` |
| 4 | 422 body is `{"errors": {"field": "ERR_CODE"}}` | Suppress default `ProblemDetails` |
| 5 | Error codes, not sentences | FluentValidation `.WithMessage("ERR_...")` |
| 6 | `form_response_key` present on success | Always populate |
| 7 | `entity.message` present means duplicate (suppresses GTM) | Preserve exactly |
| 8 | Trailing-slash variant must work | Map both routes |
| 9 | CORS echoes Origin with `Allow-Credentials: true` | `SetIsOriginAllowed` + `AllowCredentials` |
| 10 | 503 + `Retry-After` on throttle (NOT 429) | `RateLimiter` `OnRejected` |

---

## 11. Edge / Infrastructure Config

**CORS** (from `api.conf`):
```
Origin allow-list: ^https://([a-z0-9-]+\.)*webroot\.com$ | ^https://([a-z0-9-]+\.)?vercel\.app$
Allow-Credentials: true
Allow-Methods: POST, GET, PUT, DELETE, OPTIONS
Allow-Headers: Authorization, X-WRCART-CSRF, X-WRAPI-CSRF, Content-Type
Expose-Headers: X-Client-IP
Max-Age: 3600
```

**Rate Limiting** (from `fail2ban.conf`):
```
Counter key: <X-Forwarded-For or REMOTE_ADDR> + <User-Agent hash>
Threshold: ACCESS_COUNT > 20 → pause 360ms, deny 503, Retry-After: 360000
Decay: 20 per 300 seconds
```

---

## 12. Validator Laxness Rules (from validation.php — must replicate in C#)

These custom Lithium rules are more permissive than .NET defaults. Rejecting inputs PHP accepted will silently drop leads.

| Rule | Behaviour |
|------|-----------|
| `positiveInteger` | `FILTER_VALIDATE_INT` with `min_range = 1` (>= 1) |
| `matchingField` | Compares two fields; **returns true if either value is absent** (lax) |
| `countryISO` | `^[A-Za-z]{2}$` AND a `Country` row with that `iso` exists |
| `locationCode` | `^[A-Za-z]{3}$` AND a `Country` row with that `iso3` exists |
| `languageCode` | `^[A-Za-z]{2}$` AND a `Language` row exists (case-insensitive) |
| `state` | **Only validated for US & CA** — matches `state_iso3`. For every other country (incl. AU/NZ) returns **true without checking**. If country not found, also true. |
| `email` | `filter_var(FILTER_VALIDATE_EMAIL)` — **more lenient than .NET `EmailAddressAttribute`** |
| `phone` | Lithium's permissive regex — **more lenient than typical .NET phone validation** |
| `zipCode` | Only enforced when country is `USA`; otherwise always true |

---

## 13. Email Template Mapping (Trial::sendEmail)

Renders HTML locally, persists as `EmailMessage` row (cron mailer sends later — no live mail API call).

| Category | Template | Adapter |
|----------|----------|---------|
| wsav / wsai / wsac | `wsa` | `trials` |
| we | `we` | `trials` |
| saep | `saep` | `trials` |
| saup | `saup` | `trials` |
| ade | `ade` | `trials` |
| seca | `seca_web` | `trials` |
| sdns | `sdns_web` | `trials` |
| otsf | `o365` | `trials_o365` |
| cbep | `cbep` | `trials_cbep` |

**GSM parent (multi-template loop)**: iterates `license_profile` entries — `item_hierarchy_id == 2` → module's template, else `saep_gsm`. Produces **N templates → N `EmailMessage` rows**.

**Non-GSM**: single template. No category match + no `options['template_name']` → `sendEmail()` returns false (no email sent, silently).

**Japan special-casing**: `language == 'ja'` OR `country == 'Japan'` → forces `locale = jp`, `lang = ja`, `country = ja_JP`.

---

## 14. Async Downstream Sync (opportunity_update queue)

The endpoint only enqueues a row. A cron worker runs template classes later — rehydrating from `form_submit.form_json`.

### UpdateMarketo template steps:
1. **updateLicense()** — creates/syncs SFDC License object (skipped for SAWS)
2. **createPerson()** — Marketo person upsert: `firstName`, `email`, `company`, `numberOfEmployees←license_seats`, opt-in flags, `Lead_Source_Most_Recent__c`, GA ids, campaign fields, `Ecomm_License_Id__c`
3. **associatePerson()** — binds Marketo `leadId` to `_mkt_trk` cookie (skipped if no cookie)
4. **createActivity()** — custom activity (`website_form_fill = 100008`) with UTM + page/browser/referrer + full `form_json` as `formFields`

### UpdateEloquaTrial template:
- `insertTrialLead()` — POST to Eloqua with `trial->getEloquaParams()`
- Synchronous Eloqua fallback only fires if the queue INSERT itself fails

---

## 15. C# Migration Plan

### 15.1 Critical Semantics to Preserve

1. **`form_submit` is written BEFORE validation and NEVER rolled back** — it's the audit log and cron worker's only copy of the original payload
2. **Never hold a DB transaction across the Skyrise HTTP call** — connection pool + lock escalation hazard
3. **Skyrise failure is NOT fatal** — log `SkyriseFailure`, pass `keycode = null`, DB generates it, still return 200
4. **form_json stores the ENTIRE raw POST** including arbitrary marketing fields — typed binding drops them

### 15.2 Target Solution Layout (from PDF Section 3.5)

```
Webroot.Forms.sln
├── src/
│   ├── Webroot.Forms.Api/              # endpoints, DI, CORS, rate limit
│   ├── Webroot.Forms.Application/      # orchestration (port of save() chain)
│   ├── Webroot.Forms.Domain/           # port of trial_ext models + validation
│   └── Webroot.Forms.Infrastructure/   # EF Core, Skyrise client, email
└── tests/
    ├── Webroot.Forms.ContractTests/    # golden-file replay against PHP
    ├── Webroot.Forms.UnitTests/
    └── Webroot.Forms.IntegrationTests/
```

### 15.3 Migration Estimate

| Component | Effort | Risk | Notes |
|-----------|--------|------|-------|
| Endpoint + form binding + response shaping | S | Medium | Raw-form capture is the trap |
| CORS + rate limit + forwarded headers | S | Low | Or leave in Apache |
| EF Core context + lookup entities | M | Low | Country, State, LicenseCategory, DistMethodCode |
| Procedure wrappers | M | Medium | OUTPUT params, multi-result-set friction |
| Validators (all form types) | M | **High** | Strictness deltas silently drop leads |
| Transforms (Business/Consumer/Sfdc/PartnerCart) | L | **High** | Undocumented rules: GSM, Japan, BRNC |
| `trial_registration_json` builder | M | **High** | Serialisation fidelity — build a differ |
| Skyrise client | M | Medium | Non-idempotent retry hazard |
| Email rendering | L | Medium | Full template library port, GSM multi-template loop |
| Contract test suite | M | Low | **Do this first** |
| Shadow + canary infrastructure | M | Medium | Phase-based rollout |
| **TOTAL** | | | **~20-30 days** for one developer |

### 15.4 Migration Strategy (Strangler Pattern)

1. **Phase 1 — Shadow**: Apache mirrors POST to .NET, PHP still authoritative, compare responses offline
2. **Phase 2 — Canary**: Route `EmployeeProductRegistration` (lowest volume, internal users)
3. **Phase 3 — Ramp**: Consumer trials, then business trials (SAEP)
4. **Phase 4 — Cutover**: PHP as fallback behind feature flag
5. **Phase 5 — Decommission**

---

## 16. Relationship to Multi Trial

The **Endpoint Protection trial** and **Multi Trial** share the same backend pipeline (`/forms/submissions` → `TrialRegistration::save()`). The difference is:

| Aspect | Single EP Trial | Multi Trial |
|--------|----------------|-------------|
| CMS Block | `form_trial_business` | `form_gsm_net_new_trial` |
| `form_type` | `'SAEP'` | `'MULTI'` |
| Products | SAEP only | SAEP + SDNS + SECA (modules array) |
| GTM tracking | `pushTrialData()` | `pushMultiTrialData()` |
| API endpoint | Same: `/forms/submissions` | Partner API: `/partner-api/trial-registrations` |
| Backend model | `BusinessTrialForm` | `PartnerCartTrial` |

The single EP trial goes through the **www-facing API** (`api.webroot.com/forms/submissions`), while the Multi Trial goes through the **Partner API** (`apps/partner_api/controllers/TrialRegistrationController.php`). Both ultimately call `TrialRegistration::save()`.

---

## 17. Porting Risks — Coverage Gap Assessment

Cross-referenced against `forms-submissions-and-skyrise-analysis.md` (Section: Porting risks).

### RESOLVED — Source fully read and documented

| # | Gap Item | Status | Evidence |
|---|----------|--------|----------|
| 1 | `TrialRegistration::save()` chain body | **RESOLVED** | Full 500-line filter chain read from `li3_wr/models/trial/TrialRegistration.php:100-500`. Includes: `years=0`, locale split, trial_days defaults, sub-model dispatch, distribution code lookup, `state_iso3 → state_id` mapping, `buildTrialRegistrationInsert()`, Skyrise call, Trial insert, license/profile/customer attach, email, queue insert. |
| 2 | `buildTrialRegistrationInsert()` → `trial_registration_json` | **RESOLVED** | Full method read from `TrialRegistration.php:510-710`. Exact JSON shape documented: `{keycode, license_keycode_type_id, license_distribution_method_code, language_code, location_code, license_category[{name,seats,days,category_type_name,item_hierarchy_id,vault_id,...}], customer{...}, partner{...}, sfdc{...}, eloqua_parameters{...}}`. Date format is `Y-m-d` (no time). Modules appended for GSM with `item_hierarchy_id=2`. |
| 4 | SkyRise 2.x `GenerateKeycode` request/response | **RESOLVED** | Full `GenerateKeycode.php` read. Uses `BulkGenerator` + `GenerateCombinedRequest`. Request fields: `LicenseDistCode, LicenseCategory, Storage, FirstName, LastName, Email, DurationInDays, Seats, IsTrial, LicenseTypeId, LicenseCategoryId, LicenseKeycodeTypeId, iso`. Response: `License` object → `licenseKey`. Failure: errors array, `SkyriseFailure` persisted, keycode=null. `PRODUCT_MATRIX` defines static params per category. `TransactionRefId` = timestamp+random. `isTemplate = (LicenseKeycodeTypeId == 3)`. |
| 6 | Sub-models — `BusinessTrialForm` | **RESOLVED** | Full `transformEntity()` read: SAEP→keycode_type_id=3, Japan→3, GSM form_type→SAEP+3, BRNC→trial_days=43, campaign_id, product_user_license_type, send_email=true, insert_queue_record=true, eloqua_parameters JSON blob with 15 fields. `exportTrialRegistration()` = schema-filtered data. |
| 6 | Sub-models — `ConsumerTrialForm` | **RESOLVED** | Validation rules read in earlier session: seat-category enforcement (WSAV→1, WSAI→3, WSAC→5, WE→1/3/5), productTrialDays validation. |
| 6 | Sub-models — `PartnerCartTrial` | **RESOLVED** | Full file read: MODULE_CATEGORYS, ENTERPRISE_REQUIRED_FIELDS, module validation (category + seats), legacy key support, isEnterprise() check. |
| 3 | Stored procedure contracts | **RESOLVED** | Full T-SQL source reviewed for all three trial procs. `usp_trial_insert_trial_registration`: OPENJSON schema, defaults, duplicate `-110` (with a mutating audit-write side effect), error `-200`, success result set with NO `response_code` column, dynamic product/distribution-method resolution. `usp_trial_insert_trial`: keycode fallback is DB-side (`usp_license_generate_keycode_cart`), not Skyrise; idempotent re-invocation; MSP/Skyrise sync hook via `sky_template`. `usp_trial_insert_trial_module`: legacy `fn_parse_json` parsing, overlaps with module logic embedded in `usp_trial_insert_trial`. `usp_form_insert_form_submit` param contract + length caps. See Sections 7.1–7.3. |
| 5 | Email rendering | **RESOLVED** | `Trial::sendEmail()` — complete category→template→adapter mapping, GSM multi-template loop, Japan casing, `EmailMessage` persistence. See Section 13. |
| 7 | Validator laxness | **RESOLVED** | All custom Lithium rules documented: state only checked for US/CA, matchingField passes when absent, email/phone more lenient than .NET. See Section 12. |
| 8 | Async workers | **RESOLVED** | `UpdateMarketo` (4-step flow) + `UpdateEloquaTrial` fully documented. See Section 14. |
| — | Sub-models — `SfdcTrial` | **RESOLVED** | Full transform: GSM type→keycode_type_id=3, vault map for CBEP/OTSF, customer from Lead/Opportunity/Account, modules from License_Modules__r. |
| — | Sub-models — `PartnerServiceTrial` | **RESOLVED** | Validation: external_account_id required, productIdTrial validates against partner key, enterprise requires customer_email. |
| 9 | `sky_update` queue / `usp_sky_insert_update` | **RESOLVED** | Full proc source reviewed plus consumer code traced end-to-end. The `msp_registration` template triggered from `usp_trial_insert_trial` does **not** call Skyrise — it's consumed by `apps/cron/extensions/queue/sky/template/MspRegistration.php`, which calls `Connections::get('mspregistration')` (`worldauth.webrootanywhere.com`), a separate legacy MSP system. The `sky_update` table is a generic external-sync queue shared by unrelated integrations (WorldAuth, BrightCloud DNS, Office 365); only `sky_update_type_id = 4` (`update_license` template) is real Skyrise, processed by the standalone `apps/skyrise_update/` app via the genuine Skyrise SDK. The synchronous `GenerateKeycode.php` → `Connections::get('skyrise')` call remains the only Skyrise touchpoint the trial-creation flow itself triggers. |

### REMAINING — External assets only, not business logic

| # | Gap Item | Risk | What's Needed |
|---|----------|------|---------------|
| 1 | **Email template view files** | Low | The `.html.php` render targets in `li3_wr/resources/mail_templates/trials/` — template mapping logic is now documented in Section 13. |
| 2 | **Marketo/Eloqua/SFDC service configs** | Low | HTTP client configs and credentials for async workers — environment-specific. Field mappings documented in Section 14. |
| 3 | **Webbula API contract** | Medium | `EmailVerifyController` — DB cache + Webbula API. Fail-open. Need contract for C# `HttpClient`. |
| 4 | **Serialisation fidelity** | **High** | Must build a differ: dates `Y-m-d` (no time), `keycode` is empty string `""` not null, numeric fields are real JSON numbers, `item_hierarchy_id` is int 1/2. |
| 5 | **Dual module-insertion path** | Medium | Now known to be a **triple** path: `usp_trial_insert_trial`'s embedded block, standalone `usp_trial_insert_trial_module`, and a separate `ModuleTrialController` → License Service dispatch (`site_secondary_trial`/`license_secondary_trial`) that operates on an existing license via keycode/`message_key` lookup, found in `li3_wr/models/trial/ModuleTrial.php`. The third path looks like the real production mechanism for post-creation module attachment. Need DB/ecom team confirmation on which is authoritative before the C# port implements only one. |
