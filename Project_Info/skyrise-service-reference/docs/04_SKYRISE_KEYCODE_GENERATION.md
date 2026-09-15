# Skyrise & Keycode Generation — Complete Reference

## 1. Scope

This document consolidates everything verified about Skyrise integration and trial keycode generation, scoped specifically to the C# port of this subsystem. It draws from `li3_wr/models/skyrise/GenerateKeycode.php`, the local `skyrise/` SDK repository, `etc/connections.dev.json`, the `sky_update` DB queue and its consumers (`apps/cron/extensions/queue/sky/*`, `apps/skyrise_update/`), and the stored procedures `usp_trial_insert_trial` and `usp_sky_insert_update`.

Related documents: `01_ENDPOINT_PROTECTION_TRIAL_MIGRATION.md` Sections 7.1–7.2, `02_MULTI_TRIAL_MIGRATION.md` Section 6.2 (copy these directly from `cPort/docs/` in the main workspace — see this package's README).

## 2. Architecture Overview

There are exactly **two** real Skyrise touchpoints in this codebase. Everything else that has "sky" in its name is either unrelated legacy infrastructure or a separate, independently-deployed application.

```
Trial creation (PHP, synchronous)
  TrialRegistration::save()
    → GenerateKeycode::save()
        → Connections::get('skyrise')
        → SkyIdentityAuthToken (client_id/client_secret)
        → SkyRise\services\GenerateBulkLicenses (BulkGenerator)
        → POST to Skyrise API  ─────────────────────────▶  REAL SKYRISE
        ← licenseKey or errors[]
    → passes keycode (or null on failure) into usp_trial_insert_trial

usp_trial_insert_trial (T-SQL)
  if @keycode is null/empty:
    → usp_license_generate_keycode_cart (hardcoded product_id)   [DB-ONLY, NOT SKYRISE]
  if request_type = 'create_master' and category = 'SAEP':
    → usp_sky_insert_update (template 'msp_registration')
        → sky_update table (generic external-sync queue)
        → apps/cron .../MspRegistration.php
        → Connections::get('mspregistration')
        → worldauth.webrootanywhere.com                          [NOT SKYRISE — legacy WorldAuth]

Independently, elsewhere in the system:
  sky_update rows with sky_update_type_id = 4 ("update_license")
    → apps/skyrise_update/ (standalone app, own deployment)
        → SkyIdentity OAuth
        → SkyRise\services\GenerateBulkLicenses (same SDK class)
        → REAL SKYRISE                                            [ongoing license-state sync,
                                                                     not triggered by trial creation]
```

## 3. Synchronous Keycode Generation — `GenerateKeycode.php`

**File**: `li3_wr/models/skyrise/GenerateKeycode.php`
**Trigger**: called from `TrialRegistration::save()` before the DB insert, for every trial that needs a keycode.

### 3.1 Connection and auth

```php
$conf = Connections::get('skyrise', ['config' => true]);
$client = new Client(Environment::is('production') ? SkyRiseEnv::PRODUCTION : SkyRiseEnv::DEVELOPMENT);
$token = SkyIdentityAuthToken::createFromCredentials($client, $conf['client_id'], $conf['client_secret']);
```

- Uses the `skyrise` connection profile (not `skyrise_create`, which exists in config but is not referenced by this file).
- Auth is **SkyIdentity OAuth** (`SkyIdentityAuthToken`), not the legacy key-based scheme.
- **Correction — this is a nested two-token scheme, not a single-token one**: `GenerateKeycode.php`'s outer call goes through `GenerateBulkLicenses` → `Calls::BULK` (`POST /api/v1/bulk`), which per `Calls.php`'s `$_authTypes` map is authenticated with `SkyIdentityAuthToken` (`client_id`/`client_secret`, from the `sky_identity` connection — confirmed working from `SkyIdentityAuthToken::createFromCredentials()` → `POST https://skyidentity-qa.webrootcloudav.com/connect/token`, standard OAuth2 `client_credentials` grant — though see item 11 in Section 10 for the currently-dead credential value). **But** each individual item inside that bulk request's array wraps an inner call (`/key/createkey` or `/master/createmasterkey`, per `LicenseKeycodeTypeId`), and those specific call types are mapped to `LegacyAuthToken` instead — which needs the `skyrise` connection's `client_name`/`access_key`/`secret_key` (confirmed from `LegacyAuthToken::createFromArray()`'s required fields) to compute a **timestamped, per-call signed token** (`_getRequestToken()`, referencing an internal "Signing Requests" spec — the exact HMAC/signing algorithm is now fully extracted, see Section 3.8). So **both** credential sets are genuinely required for an end-to-end keycode-creation call: `sky_identity` for the outer bulk envelope, `skyrise` for the inner signed sub-request. Neither set is dead/unused, contrary to what was previously stated here — see Section 3.8 for the full path/method map.

### 3.2 Request construction

- `SkyRise\services\GenerateBulkLicenses` (aliased `BulkGenerator`) batches one or more `GenerateCombinedRequest` (aliased `Generator`) objects, one per trial request.
- Each request's data comes from `PRODUCT_MATRIX` (static per-category defaults) merged with entity-specific fields, then `isTemplate = (LicenseKeycodeTypeId == 3)` is computed.
- Request fields sent to Skyrise: `LicenseDistCode`, `LicenseCategory`, `Storage`, `FirstName`, `LastName`, `Email`, `DurationInDays`, `Seats`, `IsTrial` (always `true` here), `LicenseTypeId`, `LicenseCategoryId`, `LicenseKeycodeTypeId`, `iso`.
- `TransactionRefId` is generated as timestamp + random value.

### 3.3 PRODUCT_MATRIX (full, verified)

```php
const PRODUCT_MATRIX = [
    'SAEP' => ['LicenseCategory' => 'SAEP', 'LicenseCategoryId' => '220', 'LicenseType' => 'enterprise,trial', 'Storage' => '0', 'LicenseKeycodeTypeId' => 3],
    'WIFI' => ['LicenseCategory' => 'WIFI', 'LicenseCategoryId' => '130', 'LicenseType' => 'consumer,trial',   'Storage' => '0', 'LicenseKeycodeTypeId' => 1],
    'ADE'  => ['LicenseCategory' => 'ADE',  'LicenseCategoryId' => '55',  'LicenseType' => 'enterprise,trial', 'Storage' => '0', 'LicenseKeycodeTypeId' => 3],
    'WSAV' => ['LicenseCategory' => 'WSAV', 'LicenseCategoryId' => '100', 'LicenseType' => 'consumer,trial',   'Storage' => '0', 'LicenseKeycodeTypeId' => 1],
    'WSAI' => ['LicenseCategory' => 'WSAI', 'LicenseCategoryId' => '115', 'LicenseType' => 'consumer,trial',   'Storage' => '0', 'LicenseKeycodeTypeId' => 1],
    'WSAC' => ['LicenseCategory' => 'WSAC', 'LicenseCategoryId' => '120', 'LicenseType' => 'consumer,trial',   'Storage' => 25,  'LicenseKeycodeTypeId' => 1],
    'CBSB' => ['LicenseCategory' => 'CBSB', 'LicenseCategoryId' => '200', 'LicenseType' => 'enterprise,trial'],   // no Storage/LicenseKeycodeTypeId set
    'OTSF' => ['LicenseCategory' => 'OTSF', 'LicenseCategoryId' => '233', 'LicenseType' => 'enterprise,trial'],   // no Storage/LicenseKeycodeTypeId set
    'CBEP' => ['LicenseCategory' => 'CBEP', 'LicenseCategoryId' => '234', 'LicenseType' => 'enterprise,trial'],   // no Storage/LicenseKeycodeTypeId set
    'WE'   => ['LicenseCategory' => 'WE',   'LicenseCategoryId' => '117', 'LicenseType' => 'consumer,trial', 'Storage' => 0, 'LicenseKeycodeTypeId' => 1],
];
```

`SDNS` and `SECA` (used by GSM Multi Trial as modules) are **not present** in this matrix — confirms they are handled as license modules attached post-creation, not as their own Skyrise product entries. `CBSB`, `OTSF`, `CBEP` omit `Storage`/`LicenseKeycodeTypeId` entirely — verify how the SDK/API treats these missing keys (default vs. required).

### 3.4 LICENSE_TYPE_ID_MAP (verified)

```php
const LICENSE_TYPE_ID_MAP = [
    'consumer,full'    => 1,
    'enterprise,full'  => 4,
    'consumer,trial'   => 101,
    'enterprise,trial' => 104,
];
```

This matches the `license_type_id` derivation logic independently found in `usp_trial_insert_trial_registration` (Section 7.1 of doc 01) — both systems agree on this matrix, a good cross-check for the C# port.

### 3.5 Response and failure handling (previously documented, reconfirmed)

- Success: response is a `SkyRise\response\License` object → `licenseKey` becomes the trial's keycode.
- Failure: errors array returned; `SkyriseFailure` is persisted for audit; `keycode` is passed as `null` into `usp_trial_insert_trial`. This is **non-fatal** — the DB-side fallback (Section 4) takes over.

### 3.6 Additional confirmed call sites (from `api-staging1`)

`GenerateKeycode` is not trial-specific — the same class is called from at least two other unrelated flows, which independently confirms the request/failure model documented above:

- `controllers/amazon_aia/AmazonAiaController.php` — builds keycode parameters for Amazon AIA product registrations, then `GenerateKeycode::create([...])->save()`.
- `controllers/cleverbridge/CleverbridgeKeygenController.php` — builds keycode parameters from a Cleverbridge order, calls `GenerateKeycode::create(['requests' => $requests])->save()`.

Both confirm the exact `SkyriseFailure` persistence shape on failure:

```php
SkyriseFailure::create([
    'skyrise_request' => json_encode($requests),
    'skyrise_failure_message' => json_encode($skyrise->errors)
]);
```

The Cleverbridge controller also confirms the **system-wide fallback pattern** already found in `usp_trial_insert_trial` (Section 4) is not trial-specific: when `$skyrise->save()` fails or returns no keycode, the same `LicenseService::create(['license_service_type_name' => 'license_create', 'service_json' => ...])` call is used to let the DB generate the keycode instead, and the resulting keycode is read back off the `LicenseService` record. This is strong cross-validation that the Skyrise-then-DB-fallback pattern is a single, consistent convention across the whole codebase, not something trial-specific that the C# port can special-case.

The Cleverbridge controller's `LicenseKeycodeTypeId` derivation — `($license_type_id === 4 && $license_category_id === 220) ? 3 : 1` — matches `LICENSE_TYPE_ID_MAP['enterprise,full'] = 4` and `PRODUCT_MATRIX['SAEP']['LicenseCategoryId'] = '220'` exactly, cross-confirming both tables are correct and stable across call sites.

### 3.7 Wire-level request format (verified from local SDK source)

From `skyrise/src/RequestBody.php` and `skyrise/src/LicenseRequestBody.php`:

- `RequestBody implements \JsonSerializable` — every field not explicitly provided defaults to an **empty string `''`**, not `null` (`const FIELD_DEFAULT = ''`). The C# request model should mirror this (empty string defaults), not use nullable fields, to match what Skyrise expects on the wire.
- `StartDate`/`EndDate` are **omitted entirely** from the serialized payload if both are empty — sending them as empty strings causes a Skyrise validation error ("end date is not greater than start date").
- `LicenseTypeId`, `SeatCount`, and `DurationInDays` can be **auto-derived** by the SDK itself from `LicenseType`, `Seats`, and `StartDate`/`EndDate` respectively — the C# port can either replicate this derivation or just always pass explicit values and skip it.
- `LicenseRequestBody::LICENSE_TYPE_ID_MAP` is baked into the SDK itself and is **identical** to the one in `GenerateKeycode.php` (Section 3.4) — triple-confirmed stable (PHP app code, SDK, and independent call sites in Section 3.6).
- `LicenseRequestBody::TEST_LICENSE_DISTI_CODE = 'TEST'` — a built-in override specifically for **non-production environments**, meaning `LicenseDistCode` does not need to be a real, valid distribution code when testing against QA/development. This directly enables local/QA testing without needing production distribution codes.

### 3.8 Confirmed endpoint paths, methods, and auth-per-call (from `Calls.php`)

| Call | HTTP method | Path | Auth type |
|---|---|---|---|
| `CREATE_KEY` | POST | `/key/createkey` | `LegacyAuthToken` (`client_name`/`access_key`/`secret_key`) |
| `CREATE_MASTER_KEY` | POST | `/master/createmasterkey` | `LegacyAuthToken` |
| `BULK` (used by `GenerateKeycode.php`/`GenerateBulkLicenses`) | POST | `/api/v1/bulk` | `SkyIdentityAuthToken` (`client_id`/`client_secret`) |
| `BATCH_LICENSES`, `GET_LICENSE`, `POST_LICENSE`, `PUT_PRODUCT_*`, etc. | varies | `/api/v1/...` | `SkyIdentityAuthToken` |
| `S2_GET_MESSAGE` | GET | `/api/v1/messages?licensekey=:keycode` | `SkyAuthToken` (not yet reviewed) |

**How a trial keycode request actually nests** (`GenerateKeycode.php` → `GenerateBulkLicenses` → `Bulk` request body):

1. Outer request: `POST <skyrise-host>/api/v1/bulk`, headers `Content-Type: application/json`, `Accept: application/json`, `Authorization: Bearer <SkyIdentity access token>` (from Section 3.1's token exchange). **Host correction**: `SkyRise\Client` does not use `connections.dev.json`'s `skyrise.host` field at all — it derives the host purely from `SkyRise\Environment::HOSTS`, keyed by environment name passed into its constructor. Per that file, the current dev/QA host is `https://skyrise-qa.webrootcloudav.com` (no `svc`) — the `skyrisesvc-qa.webrootcloudav.com` value from `connections.dev.json` is commented out in the SDK source as superseded. There's also a `pre` (pre-production) environment at `https://skyrise-pre.webrootcloudav.com` for third-party integration testing outside Webroot's network. Use `skyrise-qa.webrootcloudav.com` for the outer bulk call, not the connections-file value.
2. Body: a JSON array. Each element is produced by `Request::toBulk()` and has the shape `{ "method": "POST", "bulkId": "...", "path": "/key/createkey" | "/master/createmasterkey", "headers": "...", "body": "..." }`.
3. Each element's embedded `body`/`headers` carries a **`LegacyAuthToken`-signed request token** (`RequestToken` field / `Authorisation` header — British spelling is intentional in the SDK), computed from the `skyrise` connection's `client_name`/`access_key`/`secret_key` plus a timestamp and the call's action name.

**The signing algorithm is now fully extracted** (from `LegacyAuthToken::_getRequestToken()`):

```php
$elems = [$clientName, $actionName, $keycode /* 'noKeycode' default */, $accessKey, $unixTimestamp];

// lowercase every element, sort alphabetically, concatenate with no separator
$sorted = $elems; // (utf8_encode + strtolower each, then sort(SORT_STRING))
$signingString = implode($sorted);

$hmac256 = strtoupper(hash_hmac('sha256', $signingString, $secretKey, false)); // hex digest

$token = $hmac256 . ',' . implode(',', $elems); // note: unsorted, original-case elems here
```

Key details for a faithful C# port:
- The 5 elements are: `clientName`, `actionName` (from `Calls::getAction()`, e.g. `"createkey"`), `keycode` (defaults to the literal string `"noKeycode"` when not applicable), `accessKey`, and a Unix timestamp (seconds).
- The **signing input** uses a lowercased, alphabetically-sorted, no-separator concatenation of those 5 elements.
- The **HMAC** is SHA-256, keyed with `secretKey`, hex-encoded (not base64), uppercased.
- The **final token value** is `{hexHmac},{clientName},{actionName},{keycode},{accessKey},{timestamp}` — using the *original* (unsorted, original-case) values joined by commas, not the sorted/lowercased ones used only for computing the signature.
- This token is sent as the `Authorisation` header (note the British spelling — intentional, not a typo) for direct calls, or as a `RequestToken` field embedded in the request body when nested inside a bulk item.
- There's a small timestamp-adjustment quirk in the PHP source (a `+0 seconds` no-op modification, apparently a leftover from a past DMZ clock-skew fix) — safe to ignore in the C# port; just use the current Unix timestamp in seconds.

**Practical implication for manual testing**: Step 1 (Section 3.1's token exchange) is directly testable in Postman as a static request. The actual keycode-creation call is not — it requires computing the `LegacyAuthToken` signature above per-request, which needs a script (Postman pre-request script or a small C#/PHP snippet), not a static header value.

## 4. DB-Side Keycode Fallback — `usp_trial_insert_trial`

When the PHP-side Skyrise call fails or is skipped, `usp_trial_insert_trial` generates a keycode itself:

```
if @keycode is null or '':
    EXEC usp_license_generate_keycode_cart @product_id = 152200130, @keycode OUTPUT, @response_code OUTPUT, @message OUTPUT
    set @ecom_create_license = 1
    (written back into trial_registration_json via JSON_MODIFY)
EXEC usp_license_service_insert_service @license_service_type_name = 'license_create', @trial_registration_json, @return_license_service_id
```

- This is a completely independent, product-agnostic DB keycode generator — it does **not** call Skyrise.
- `product_id = 152200130` is hardcoded regardless of the trial's actual product; its real meaning (what product row this is, why it's used generically) is not yet confirmed — see Section 11.
- **`usp_license_generate_keycode_cart` algorithm — now fully reviewed** (source obtained, see `stored_procs/usp_license_generate_keycode_cart.sql` in this package): validates `product_id` (must exist and have `uses_keycode = 1`, else `response_code = -103`) and `license_distribution_method_id` (else `-107`). Looks up `product_family_prefix` and a 2-char version code from `product_versions`. Gets a unique sequence number by inserting into `keycode_sequence` and converting the identity value to a base-26 alphabetic string (9 chars, left-padded with `'A'`). Builds a base keycode as `product_family_prefix + product_version_char + license_distribution_method_code + sequenceString`. **Two distinct final formats depending on `product_family_id`:**
  - For families `100, 200, 300, 50, 51, 55` ("new keycode format"): SHA1-hashes the base keycode, strips `'0'`/`'1'` characters (to avoid confusion with `O`/`I`), and rebuilds the keycode from hash fragments, checking for collisions against the `license` table and shifting the substring window on collision until unique.
  - For all other families: appends a custom 3-char base-26 checksum (a hand-rolled weighted-sum algorithm over the base keycode's characters) instead of hashing.
  - **QA/test marker**: if `environment.keycode_modification_string` is set (non-null), part of the generated keycode is overwritten with that string — meaning QA/dev-generated keycodes are structurally distinguishable from production ones by design. Useful for visually confirming which path generated a given test keycode.
- The `ecom_create_license` flag persists which path generated the keycode (Skyrise vs. DB fallback), which the C# port should replicate for observability/audit parity.

## 5. The `sky_update` Queue — Clarified, Not Skyrise-Specific

`usp_sky_insert_update` is a **generic external-integration queue inserter**, used by many unrelated subsystems (cart order license update, child license insert, license migration, license service insert, license module update, message deprovision, trial insert, partner license transaction). The `sky_` prefix is historical/misleading.

### 5.1 Proc mechanics (verified from source)

- Resolves `sky_update_type_id` from `sky_template_id` via `sky_template_update_type` (uses the minimum matching row).
- `@reregister = 1` archives prior active entries (`sky_update_archive.sky_update_status_id = 4`).
- **De-dup guard exists only for `sky_update_type_id = 1`**: checks both `sky_update_archive` and `sky_update` for an existing active row for the same `license_id` + type, and silently returns (no insert) if found. No such guard exists for other types — they can be inserted repeatedly without protection at the DB layer.
- Inserts into `sky_update`, optionally with an extra JSON payload into `sky_update_json`.

### 5.2 Template dispatch (from `SkyUpdateQueueRecord.php`)

| `sky_template_name` | Target system | Status |
|---|---|---|
| `msp_registration` | WorldAuth (`worldauth.webrootanywhere.com`) via `Connections::get('mspregistration')` | **Confirmed — not Skyrise** |
| `update_license_sdns` | Likely BrightCloud (`sdns_license_sync` → `sdnsapi-qa.brightcloud.com`) | Inferred from connection name match, not yet traced in code |
| `O365_update` | Office 365 API (`backupoffice365api.endpoint.carbonitestage.com`) | Inferred from connection name match, not yet traced in code |
| `license_console_registration`, `dns_registration`, `update_wifi`, `gsmc_migration`, `update_license_wsat`, `CBEP_update`, `CBEP_delete`, `s2_redemption`, `s2_registration`, `msp_migration` | Unknown | Not traced |
| `update_license` (`sky_update_type_id = 4`) | **Real Skyrise** — standalone app | See Section 6 |
| `sky_template_id = 8` (BRTC license update) | Special-cased in `SkyUpdateQueue::_fetchNextRecord()`, routed to `BrtcLicenseupdateQueueRecord` instead of the normal template dispatch | Confirmed routing, target system not traced |

### 5.3 What the trial-creation flow actually triggers

Only `msp_registration`, for `request_type = 'create_master'` + `license_category_name = 'SAEP'` (parent/GSM keycode creation). This queue hook has **no effect on Skyrise** — it registers the new MSP account with the legacy WorldAuth system. `MspRegistration.php` builds:

```php
[
    'logonemail'   => customer email,
    'parentkeycode'=> license keycode,
    'marname'      => company name,
    'producttype'  => 'PrePay',
    'languagecode' => 'EN',
]
```

with a special case: if the partner name is `"Zix - Reseller"`, `data['partner'] = 'ZIX'` is added.

## 6. Modern Batch Skyrise Sync — `apps/skyrise_update/`

A **separate, independently deployed application** (own `composer.json`, own `connections.json`, own SkyIdentity credentials) — not part of the main `li3_wr`/`cron` codebase.

- Processes only `sky_update` rows where `sky_update_type_id = 4`, in batches of 100.
- Uses the same SDK classes as the synchronous trial-time call: `SkyRise\services\GenerateBulkLicenses`, `SkyRise\response\License`.
- Bootstraps its own token via `skyrise_get_token()` (SkyIdentity OAuth flow).
- Toggles production vs. non-production Skyrise environment via the `WR_ENV` environment variable.
- **Not yet traced**: what code path inserts `sky_update_type_id = 4` rows in the first place. It is not inserted by any of the trial-creation procs reviewed so far (`usp_trial_insert_trial_registration`, `usp_trial_insert_trial`, `usp_trial_insert_trial_module`, `usp_license_update_license_module_license`) — likely triggered from a different flow (renewals, upgrades, or manual license admin actions). This determines whether the C# trial port needs to emit anything into this queue at all, or whether it's entirely out of scope.

## 7. SkyRise SDK — Local Repository Inventory

A full copy of the SDK is present at `skyrise/` (own git repo, `composer.json`), so the wire-level client does not need to be requested from elsewhere — it can be read directly. This package's `php/skyrise-sdk/` and `php/skyidentity-sdk/` folders contain reference copies of the key files.

| Path | Contents |
|---|---|
| `skyrise/src/Client.php` | Base HTTP client |
| `skyrise/src/Environment.php` | `PRODUCTION`/`DEVELOPMENT`/`PRE_PRODUCTION` environment constants and hosts |
| `skyrise/src/Calls.php`, `Action.php`, `Request.php`, `RequestBody.php` | Request/action plumbing |
| `skyrise/src/LicenseRequestBody.php` | License-specific request body shape |
| `skyrise/src/auth/LegacyAuthToken.php` | Key-based auth (`client_name`/`access_key`/`secret_key` style) — full signing algorithm in Section 3.8 |
| `skyrise/src/auth/SkyAuthToken.php`, `SkyIdentityAuthToken.php` | OAuth-style auth (`client_id`/`client_secret`) — used by `GenerateKeycode.php` |
| `skyrise/src/services/GenerateBulkLicenses.php` | Bulk license/keycode generation (used by both the synchronous trial call and the batch sync app) |
| `skyrise/src/services/GenerateCombinedRequest.php` | Per-request builder used inside the bulk generator |
| `skyrise/src/services/GenerateS2Request.php`, `TranslateProductToRequest.php` | Additional request builders, not yet traced to a call site |
| `skyrise/src/response/` | Response models (e.g., `License`) |
| `skyrise/src/Errors.php` | **Internal SDK error constants only** (invalid environment/action/path/method/auth-token) — this is not the Skyrise API's own error/response taxonomy. The API-level `errors[]` array parsed in `GenerateKeycode.php` on failure is a separate, undocumented shape. |
| `skyrise/tests/` | Existing test suite — useful for understanding expected request/response fixtures |

## 8. Connections / Configuration Inventory

From `etc/connections.dev.json` (QA/dev values only) — see `config/skyrise-connections-reference.json` in this package for a scoped extract:

| Connection key | Adapter | Host | Auth fields present | Used by |
|---|---|---|---|---|
| `skyrise` | `DerbySkyRise` | `skyrisesvc-qa.webrootcloudav.com` | `client_name`, `access_key`, `secret_key` | Two roles, not reconciled away: (1) `GenerateKeycode.php`'s top-level `Connections::get('skyrise')` call reads `client_id`/`client_secret` from this config, which this dev copy doesn't have — still an open discrepancy for that specific access path; (2) confirmed genuinely used as `LegacyAuthToken`'s `client_name`/`access_key`/`secret_key` to sign the inner `createkey`/`createmasterkey` sub-requests (Section 3.1, 3.8). Not dead config either way. |
| `skyrise_create` | `SkyriseCreateKeycode` | `skyrisesvc-qa.webrootcloudav.com` | `client_name`, `access_key`, `secret_key` | Not referenced by `GenerateKeycode.php`; call site not yet found |
| `sky_identity` / `skyidentity` | `DerbySkyIdentity` | `skyidentity-qa.webrootcloudav.com` | `client_id`, `client_secret` | Confirmed the correct **shape** for `SkyIdentityAuthToken`'s outer bulk-call auth (Section 3.1). The specific committed **value** is confirmed dead via live Postman test — `400 invalid_client` (Section 10 item 11, Section 12.1) — needs replacement from the ecom team. |
| `mspregistration` | `DerbyMSPRegistrationAPI` | `worldauth.webrootanywhere.com` | none shown (HTTP only) | `MspRegistration.php` (sky_update queue consumer) — **not Skyrise** |
| `skyrise_sqs` | — (AWS SQS) | `SkyRiseNotifications.fifo` (us-west-2 / us-east-2) | `key`, `secret` | Producer/consumer not yet traced |

## 9. What Is RESOLVED (ready to design the C# port around)

- Synchronous keycode generation call: trigger point, request fields, `PRODUCT_MATRIX`, `LICENSE_TYPE_ID_MAP`, non-fatal failure handling.
- DB-side fallback exists and is fully independent of Skyrise (`usp_license_generate_keycode_cart` via hardcoded `product_id`), full algorithm documented.
- The `sky_update` table is a generic queue, not Skyrise-specific; only `msp_registration` (→ WorldAuth) is triggered by trial creation, and it is **not** a Skyrise call.
- The only two genuine Skyrise touchpoints in the whole reviewed codebase: (1) the synchronous `GenerateKeycode.php` call, and (2) the standalone `apps/skyrise_update` batch app (unrelated to trial creation).
- The full SDK (`skyrise/`) is present locally and can be read directly — no external repo access needed for the wire-protocol client itself.
- The complete `LegacyAuthToken` HMAC-SHA256 signing algorithm (Section 3.8).

## 10. What Is NEEDED FROM ELSEWHERE

| # | Item | Why it's needed | Who to ask |
|---|------|------------------|------------|
| 1 | **Reconcile `skyrise` connection auth fields** — code expects `client_id`/`client_secret`, dev config has `client_name`/`access_key`/`secret_key` | **PARTIALLY RESOLVED** — `client_name`/`access_key`/`secret_key` are confirmed genuinely used, as `LegacyAuthToken` credentials for signing the inner `createkey`/`createmasterkey` sub-requests (Section 3.1, 3.8) — not an unused/dead auth class as earlier stated here. Still open: why `GenerateKeycode.php`'s own top-level code reads `client_id`/`client_secret` directly off the `skyrise` connection when this dev config only has the Legacy-style fields; the real staging/production `connections.json` may simply carry both field sets on the same connection. | Ecom platform/config team — confirm the real `skyrise` connection's full field list in staging/production |
| 2 | **Production Skyrise endpoint and credentials** | Only QA hosts (`skyrisesvc-qa.webrootcloudav.com`, `skyidentity-qa.webrootcloudav.com`) are visible in this workspace | Ecom team — request via approved secret store, not source control |
| 3 | **Skyrise API's own error/response contract** (the `errors[]` array shape on failure) | `Errors.php` only covers internal SDK errors, not what the live API actually returns. Confirmed the array is persisted as `json_encode($skyrise->errors)` into `SkyriseFailure.skyrise_failure_message` across multiple call sites; the request wire format itself is now fully extracted from local SDK source (Section 3.7), but the failure/error response body's internal structure is still unconfirmed | Skyrise API docs/OpenAPI spec, or the Skyrise service team |
| 4 | **`usp_license_generate_keycode_cart` source** | **RESOLVED** — full proc source reviewed; two-format algorithm (SHA1-hash vs. checksum-based) documented in Section 4, including the QA keycode-modification marker. | N/A |
| 5 | **What `product_id = 152200130` represents** | Hardcoded fallback product; need to confirm it's intentional and not a stale reference | QA DB — `SELECT * FROM product WHERE product_id = 152200130` |
| 6 | **Confirm whether `skyrise_create` connection is used anywhere** | Currently appears unreferenced; may be dead config or used in a flow not yet reviewed (e.g., cart/order licensing rather than trials) | Workspace-wide search, or ecom team confirmation if outside this workspace |
| 7 | **`skyrise_sqs` producer/consumer** | Purpose of the SQS notification channel and whether it affects keycode/license lifecycle is unknown | Ecom team — likely a separate service or Lambda not present in this workspace |
| 8 | **What inserts `sky_update_type_id = 4` rows** | Determines whether the trial-creation C# port needs to feed the ongoing Skyrise sync batch app, or whether it's fully out of scope | Ecom team or further code search outside the trial-creation call paths already reviewed |
| 9 | **`dns_registration` and other untraced `sky_update` templates' real target systems** | Only `msp_registration` and `update_license` (type 4) are confirmed; the rest are inferred from connection-name matches only | Further source review of `apps/cron/extensions/queue/sky/template/*.php`, or ecom team confirmation |
| 10 | **Missing `Storage`/`LicenseKeycodeTypeId` handling for `CBSB`/`OTSF`/`CBEP`** in `PRODUCT_MATRIX` | Unclear whether the SDK defaults these or whether the API requires them | Skyrise API docs, or ecom team confirmation |
| 11 | **Working `sky_identity` credentials** | Confirmed via live Postman test (Section 12.1): the committed `client_id`/`client_secret` (`EcomConsumer` / `4ddefd26-...`) returns `400 invalid_client` against `https://skyidentity-qa.webrootcloudav.com/connect/token` with a request format that exactly matches the SDK's own implementation. The credential is dead, not the request. Blocks any live QA testing until replaced. | Ecom team — request a current, active `client_id`/`client_secret` for `sky_identity`, and confirm whether `EcomConsumer` is still the correct client name |

## 11. Recommended C# Component Boundaries

> **Superseded in part by Section 12** — the two points below reflect the nested two-token auth scheme (Section 3.1, 3.8) and the confirmed-dead QA credential (Section 10 item 11), which this section originally predated. The `LegacyAuthToken` signing algorithm referenced below is now fully documented in Section 3.8 — no longer an open item.

Based on what's resolved above, the C# port for this scope should have three clearly separated pieces:

1. **`ISkyriseKeycodeClient`** — wraps the synchronous call (Section 3), composed of **two** distinct auth components: an OAuth2 client-credentials handler for the outer bulk envelope (`sky_identity`, fully understood, but currently blocked on a working credential — item 11), and a separate `LegacyAuthToken`-equivalent signer for each inner `createkey`/`createmasterkey` sub-request (`skyrise` connection, signing algorithm fully documented in Section 3.8 — HMAC-SHA256 over a sorted/lowercased element list, keyed with `secretKey`). Keep these as two isolated, swappable components, not one combined auth handler.
2. **DB-side fallback replication** — only needed if the C# service is expected to generate keycodes itself when Skyrise is unavailable (pending item 4 on the actual algorithm); otherwise this can remain a database-only concern untouched by the C# layer.
3. **No `sky_update`/WorldAuth/queue replication is in scope for trial creation** — confirmed this queue hook is not a Skyrise call, so unless the ecom team says otherwise, the C# trial-creation port does not need to touch `sky_update`, `MspRegistration`, or `apps/skyrise_update` at all.

## 12. Can Coding Start Now?

**Yes — including running and testing the real end-to-end flow locally against QA, not just writing code in isolation.**

What's safe to start immediately, since it's fully resolved (Sections 3–7):

- `ISkyriseKeycodeClient` interface + request/response DTOs (`KeycodeRequest`, `KeycodeResult`), matching the fields in Section 3.2 and the exact wire-format rules in Section 3.7 (empty-string defaults, `StartDate`/`EndDate` omission).
- A `PRODUCT_MATRIX`-equivalent lookup (Section 3.3) and `LICENSE_TYPE_ID_MAP`-equivalent lookup (Section 3.4) — cross-confirmed stable across three independent PHP call sites plus the SDK itself (Section 3.6, 3.7), so these are low-risk to port as-is.
- The non-fatal failure state machine: success → keycode; failure → structured error result, never an exception that blocks the caller (Section 3.5, reinforced by Section 3.6).
- The auth handler: build it around **two tokens**, not one — `sky_identity`'s `client_id`/`client_secret` for the outer bulk envelope (simple OAuth2 client-credentials exchange, fully resolved), and a `LegacyAuthToken`-equivalent signer using the `skyrise` connection's `client_name`/`access_key`/`secret_key` for each inner `createkey`/`createmasterkey` sub-request (Section 3.8) — both parts are now fully resolved, including the exact HMAC-SHA256 signing algorithm.
- Bulk-request shape (one request per product line, batched via `GenerateBulkLicenses`), if trial creation ever needs to request more than one keycode per call.

**EF Core's role here is narrow and worth deciding explicitly up front**: this service does not need EF Core to talk to Skyrise itself (that's a pure HTTP client concern). EF Core only comes into play for two optional pieces:
- A `SkyriseFailure`-equivalent audit table (mirroring `skyrise_request`/`skyrise_failure_message`, Section 3.6), if the C# port wants the same failure audit trail.
- Reading `PRODUCT_MATRIX`-equivalent data from the real `license_category`/`product` tables instead of hardcoding it, which is recommended over a hardcoded C# constant so the mapping can't silently drift from the database — but this is a design choice, not a blocker.

### 12.1 Does the missing production config block local development and testing?

**Update — tested live, and the `sky_identity` QA credential does NOT work.** A real Postman request was sent to `POST https://skyidentity-qa.webrootcloudav.com/connect/token` with `Content-Type: application/x-www-form-urlencoded` and body `client_id=EcomConsumer`, `client_secret=4ddefd26-e06f-4352-b89d-5e46d7218491`, `grant_type=client_credentials` — an exact match for what `SkyIdentityAuthToken::createFromCredentials()` sends. Result: `400 Bad Request`, `{"error": "invalid_client"}`. Since the request format is confirmed correct, this means the committed credential itself is rejected by the live server — most likely rotated/revoked since `connections.dev.json` was last updated, or the `EcomConsumer` client was never enabled in this QA tenant. **This is now a confirmed blocker, not a theoretical one**, and retrying with the same static values will not resolve it.

- **`LicenseRequestBody::TEST_LICENSE_DISTI_CODE = 'TEST'`** (Section 3.7) still exists specifically so `LicenseDistCode` doesn't need to be a real distribution code once a working token can be obtained.
- **The wire-level format is still fully extracted from the local SDK** (Section 3.7) and remains valid regardless of the credential issue.
- **Resolved regardless of the above**: the actual keycode-creation call nests a second, `LegacyAuthToken`-signed sub-request inside the bulk envelope (Section 3.8), using the `skyrise` connection's `client_name`/`access_key`/`secret_key`. That signing algorithm (`_getRequestToken()`) is now fully documented (Section 3.8) — HMAC-SHA256 over a sorted/lowercased element list. Those credentials should still be presumed unverified until tested live, given the `sky_identity` set just failed.

What genuinely **is** production-only, and only matters at cutover time, not now:
1. **Real production Skyrise/SkyIdentity host and credentials** — a separate concern from the QA credential failure above; still deployment-time only.
2. **Confirming the Skyrise API's own error/response taxonomy** (item 3, Section 10) — affects how precisely failures can be categorized, but does not prevent building or running the happy path and the basic failure path (non-2xx / exception → treat as failure, log `errors` as opaque JSON, exactly like the PHP code does).

So: the client/service code can still be built now (Section 12's list is unaffected), but **live end-to-end testing against QA is currently blocked** on getting a working `sky_identity` credential from the ecom team — see the new item 11 in Section 10.

None of the above requires anything from the form-submission developer — it's all internal to the Skyrise SDK/config, though the credential refresh itself now requires the ecom team.

## 13. What's Needed From the Form-Submission Developer

Skyrise keycode generation is invoked **synchronously, mid-request**, after the form POST has been received, validated, and turned into a trial entity — but **before** the trial/license rows are persisted (Section 2). In the C# port this means the Skyrise service is called *by* the form/orchestration service, not the other way around, so the two developers need to agree on a clean internal contract rather than reverse-engineer it from each other's code later. Concretely, ask the form-submission developer for:

1. **The exact request DTO fields they will supply**, mapped from the incoming form/trial data:
   - `LicenseCategory` (license_category_name), `LicenseCategoryId`, `LicenseTypeId`, `LicenseKeycodeTypeId` — confirm whether the *form service* computes `LicenseKeycodeTypeId` (SAEP → 3, Japan → 3, GSM → 3 + SAEP, per the rules already documented in doc 01 Section 5.2) and hands it in already resolved, or expects the Skyrise service to derive it from category + language + form type. **This needs to be agreed as one owner, not split.**
   - `FirstName`, `LastName`, `Email`, `iso` (country), `Seats`, `DurationInDays`, `Storage`, `LicenseDistCode` — confirm these arrive already validated/defaulted (per doc 01 Section 7.1's server-side default rules) so the Skyrise service doesn't need to re-implement any of that validation.
2. **Whether module products (SDNS, SECA, etc.) are ever sent to this service at all.** `PRODUCT_MATRIX` has no entries for modules, which lines up with doc 02 Section 3.7's finding that modules are attached to an *existing* license through a separate mechanism after the primary keycode exists — confirm the form developer agrees the Skyrise keycode service is **primary-product-only**, and modules are explicitly out of scope for this service.
3. **Call contract**: confirm it's a single synchronous in-process call (`Task<KeycodeResult> GenerateAsync(KeycodeRequest request)`) that blocks the HTTP response, matching the current PHP behavior — not a queued/background call. If the form developer is assuming async/fire-and-forget, that's a mismatch that needs resolving now.
4. **Failure contract**: what the form service does with a failure result. The current PHP behavior is "log and continue with `keycode = null`, let the DB generate one" (Section 4). Confirm the C# form service will do the same — i.e., a Skyrise failure must never fail the whole trial-creation request — and agree on the shape of the failure result returned (e.g., `KeycodeResult { Success, Keycode, ErrorMessage }`) so the form developer can log it and proceed correctly.
5. **Who owns the DB-side fallback** (Section 4). This is currently a stored-procedure concern (`usp_license_generate_keycode_cart`), triggered by the *persistence* layer, not the Skyrise service. Confirm with the form developer whether the C# port keeps this as a DB-only fallback (Skyrise service returns null/failure and the persistence layer's SQL still generates a keycode), or whether it should be pulled into the Skyrise service itself as a code fallback — this changes the service's public contract.
6. **Environment/config ownership**: whether environment selection (QA vs. production Skyrise host) is driven by shared app configuration the form developer also uses, or is fully internal to the Skyrise client — needed so there's one source of truth for `Environment.Is("production")`-equivalent logic rather than two independent flags that can disagree.
