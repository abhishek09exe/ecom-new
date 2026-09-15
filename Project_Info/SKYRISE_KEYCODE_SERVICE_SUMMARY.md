# Skyrise Keycode Generation Service — Implementation Summary

> Status: internal service implemented, not yet wired into the trial-creation flow.
> SkyIdentity OAuth2 token exchange **confirmed working live against QA** (2024 update).
> Branch: `feature/skyrise-keycode`

## 1. What This Is

A C# port of PHP's `GenerateKeycode.php`, built as an injectable, internal-only service
(`ISkyriseKeycodeClient`) inside `ecom-new-api`. It synchronously requests a trial keycode
from Skyrise using a two-token auth scheme, and never throws on failure — it returns a
structured result and logs an audit row, so callers can fall back to the existing DB-side
keycode generation (`usp_license_generate_keycode_cart`) exactly as the PHP flow does today.

Reference material this was built from: `Project_Info/skyrise-service-reference/` (see
`docs/04_SKYRISE_KEYCODE_GENERATION.md` for the full spec this implementation follows).

No controller/API endpoint was added. This is purely a service to be called in-process by
the trial-creation flow once it exists in this codebase.

## 2. Files Added

```
ecom-new-api/
├── Services/Skyrise/
│   ├── SkyIdentityOptions.cs        — config: SkyIdentity OAuth2 (client_id/client_secret/token URL)
│   ├── SkyriseOptions.cs            — config: Skyrise host + LegacyAuthToken creds (client_name/access_key/secret_key)
│   ├── KeycodeModels.cs             — KeycodeRequest / KeycodeResult DTOs
│   ├── ProductMatrixProvider.cs     — IProductMatrixProvider: hardcoded PRODUCT_MATRIX + LICENSE_TYPE_ID_MAP
│   ├── LegacyAuthTokenSigner.cs     — ILegacyAuthTokenSigner: HMAC-SHA256 inner-request signing
│   ├── SkyIdentityTokenProvider.cs  — ISkyIdentityTokenProvider: cached OAuth2 client-credentials token
│   └── SkyriseKeycodeClient.cs      — ISkyriseKeycodeClient: builds/signs/sends the bulk call
├── Repositories/Skyrise/
│   ├── ISkyriseFailureRepository.cs
│   └── SkyriseFailureRepository.cs — persists SkyriseFailure audit rows via EF Core
├── Data/Entities/
│   └── SkyriseFailure.cs           — audit entity (skyrise_license_failure table, confirmed via QA DB)
├── Data/AppDbContext.cs            — + DbSet<SkyriseFailure>
├── Program.cs                      — DI registrations, options binding, typed HttpClients
├── appsettings.json                — empty placeholder Skyrise/SkyIdentity config sections
└── appsettings.Development.json    — real QA values (gitignored, not committed)
```

## 3. Architecture Flow

### 3.1 Call sequence (once wired into trial creation)

```
Trial-creation caller (future)
	│
	│  new KeycodeRequest { LicenseCategory, LicenseDistCode, FirstName, LastName,
	│                        Email, DurationInDays, Seats, Iso, LicenseType? }
	▼
ISkyriseKeycodeClient.GenerateAsync(request)
	│
	├─▶ IProductMatrixProvider.GetProductMatrixEntry(LicenseCategory)
	│      → PRODUCT_MATRIX lookup (LicenseCategoryId, Storage, LicenseKeycodeTypeId, LicenseType)
	│      → fail fast (audit + KeycodeResult.Failed) if category unknown
	│
	├─▶ IProductMatrixProvider.GetLicenseTypeId(LicenseType)
	│      → LICENSE_TYPE_ID_MAP lookup (e.g. "enterprise,trial" → 104)
	│
	├─▶ Decide action: LicenseKeycodeTypeId == 3 → "createmasterkey" (/master/createmasterkey)
	│                  else                       → "createkey"       (/key/createkey)
	│
	├─▶ ILegacyAuthTokenSigner.GetRequestToken(actionName)
	│      → HMAC-SHA256 over sorted/lowercased [clientName, actionName, keycode, accessKey, timestamp]
	│      → signed using SkyriseOptions.SecretKey (the "skyrise" connection's inner-request signer)
	│
	├─▶ Build inner request body (wire format: empty-string defaults, no StartDate/EndDate)
	│      → wrap as one "bulk item" { method, bulkId, path, headers, body, requestToken }
	│
	├─▶ ISkyIdentityTokenProvider.GetAccessTokenAsync()
	│      → cached OAuth2 client_credentials token exchange against SkyIdentityOptions.TokenUrl
	│      → POST client_id/client_secret/grant_type=client_credentials
	│      → caches token until ~30s before expiry
	│
	├─▶ POST {SkyriseOptions.BaseUrl}/api/v1/bulk
	│      Authorization: Bearer <SkyIdentity token>
	│      Body: [ bulkItem ]
	│
	├─▶ On success (2xx + parseable licenseKey in response)
	│      → KeycodeResult.Ok(keycode)
	│
	└─▶ On any failure (bad category, HTTP error, exception, missing licenseKey)
		   → ISkyriseFailureRepository.RecordFailureAsync(requestJson, failureMessageJson)
				→ EF Core insert into skyrise_failure (audit trail, never throws back to caller)
		   → KeycodeResult.Failed(errorMessage)
```

### 3.2 Two-token auth model (why there are two separate credential sets)

| Token | Purpose | Source | Component |
|---|---|---|---|
| SkyIdentity OAuth2 access token | Authenticates the **outer** `/api/v1/bulk` envelope | `client_id` / `client_secret` (`sky_identity` connection) | `SkyIdentityTokenProvider` |
| LegacyAuthToken signature | Authenticates each **inner** `createkey`/`createmasterkey` sub-request nested inside the bulk body | `client_name` / `access_key` / `secret_key` (`skyrise` connection) | `LegacyAuthTokenSigner` |

Both are required for a real call to succeed — this mirrors the PHP `GenerateKeycode.php` →
`GenerateBulkLicenses` → `Calls::BULK` nesting documented in Section 3.1/3.8 of the reference doc.

### 3.3 Failure philosophy

`SkyriseKeycodeClient` is designed to **never throw** for Skyrise-side problems (unknown
category, HTTP error, malformed response, network exception). Every failure path:
1. Serializes the original request + attempted bulk payload as `skyrise_request`.
2. Serializes the error message as `skyrise_failure_message`.
3. Writes both to `skyrise_failure` via `SkyriseFailureRepository` (itself wrapped in
   try/catch so a DB problem during audit-logging can't blow up the caller either).
4. Returns `KeycodeResult.Failed(...)` so the caller can proceed with the DB-side fallback.

### 3.4 What is explicitly OUT of scope in this service

- **DB-side fallback** (`usp_license_generate_keycode_cart`) — stays a persistence-layer/stored-procedure
  concern, triggered by whatever code inserts the trial record, not by this service.
- **`sky_update` queue / WorldAuth / MspRegistration** — confirmed not a Skyrise call; irrelevant here.
- **Controller/HTTP endpoint** — this is an internal service only; nothing calls it yet.
- **Production credentials/host** — only QA values are wired (see Follow-Ups).

## 4. Configuration

`appsettings.json` (committed, empty placeholders):
```json
"SkyIdentity": { "TokenUrl": "", "ClientId": "", "ClientSecret": "" },
"Skyrise":     { "BaseUrl": "", "ClientName": "", "AccessKey": "", "SecretKey": "" }
```

`appsettings.Development.json` (gitignored, real QA values already wired):
```json
"SkyIdentity": {
  "TokenUrl": "https://skyidentity-qa.webrootcloudav.com/connect/token",
  "ClientId": "EcomConsumer",
  "ClientSecret": "4ddefd26-e06f-4352-b89d-5e46d7218491"   // CONFIRMED WORKING — successfully returns an access token (previously misdiagnosed as dead due to testing with an incorrect secret value)
},
"Skyrise": {
  "BaseUrl": "https://skyrise-qa.webrootcloudav.com",
  "ClientName": "ThundercatsQA",
  "AccessKey": "ab387584-11f8-473e-a1e8-e3a4c1784661",
  "SecretKey": "vZv4bKfrZA88ZAeuAzC9JZT7Xqu5xFUWuL3-mEg4EahJ"  // not yet live-tested
}
```

## 5. Follow-Up Requirements

1. **Create the `skyrise_failure` table in the QA database.** This project has no EF Core
   migrations mechanism (schema is managed outside EF Core), so the table must be created
   manually. Suggested columns:
   - `skyrise_failure_id` (int, PK, identity)
   - `skyrise_request` (nvarchar(max)/json)
   - `skyrise_failure_message` (nvarchar(max)/json)
   - `insert_date` (datetime)

2. ~~Get a working `sky_identity` credential from the ecom team.~~ **RESOLVED** — the
   `EcomConsumer` / `4ddefd26-e06f-4352-b89d-5e46d7218491` credential is confirmed working
   and successfully returns an access token from SkyIdentity. The earlier "dead credential"
   finding was a misdiagnosis (an incorrect secret value was used during that test, not this
   one). No further action needed here — proceed to live-testing the full bulk keycode call.

3. **Live-test the `skyrise` (LegacyAuthToken) credentials end-to-end.** `ThundercatsQA` / the
   access/secret key pair have not yet been tested against a real Skyrise call — only the
   signing algorithm itself has been verified against the documented spec. Now that the outer
   SkyIdentity token exchange is confirmed working, this is the next real blocker to validate:
   run a full `GenerateAsync` call against QA and confirm the inner `createkey`/`createmasterkey`
   sub-request is accepted.

   **UPDATE — live-tested, currently failing with `401 Unauthorized`.** Running
   `SkyriseKeycodeClientLiveTests` (see Section 6 below) against real QA endpoints confirms the
   SkyIdentity token exchange succeeds (the call reaches the bulk-request step at all), but the
   `POST /api/v1/bulk` call itself is rejected with a bare `401` (empty body). Possible causes,
   not yet isolated:
   - The SkyIdentity access token's scope/audience doesn't include the bulk API.
   - The `ThundercatsQA` LegacyAuthToken credentials (`AccessKey`/`SecretKey`) are inactive/wrong,
     causing the *inner* signed sub-request to be rejected in a way that surfaces as an outer 401.
   - A request-shape mismatch (headers, bulk envelope structure) versus what the live API expects,
     since the Skyrise API's own error/response contract is still unconfirmed (item 4 below).
   Next step: get a verbose/raw response body from Skyrise (currently empty on 401) — try hitting
   `/api/v1/bulk` directly via Postman with the same bearer token to isolate outer-vs-inner-token
   fault, and ask the ecom team to confirm the `ThundercatsQA` credential is active and scoped for
   the bulk endpoint.

   **UPDATE 2 — found and fixed three confirmed bulk-item shape bugs by reading the actual
   `php/skyrise-sdk/` source (`Request.php`, `Calls.php`, `auth/LegacyAuthToken.php`,
   `RequestBody.php`, `LicenseRequestBody.php`), not just the doc summary:**
   1. **`RequestToken` was sent as a sibling field of the bulk item** (`{ method, bulkId, path,
      headers, body, requestToken }`), but PHP's `Request::getData()` merges it INTO the body
      itself (`$data = $tok->getRequestData() + $data`, where `getRequestData()` returns
      `['RequestToken' => ...]`). Fixed: `RequestToken` is now a field inside the JSON `body`
      string, matching PHP's `toBulk()` shape (`{ method, bulkId, path, body }`) exactly.
   2. **A `headers: {"Content-Type": "application/json"}` field was added to every bulk item**,
      but PHP's `toBulk()` only includes `headers` for CUSTOM headers (default empty) and this key
      is stripped entirely via `array_filter()`/an explicit `unset()` when empty. `Content-Type` is
      only set for direct, non-bulk requests (`Request::getHeaders()`), never inside a bulk item.
      Fixed: removed the `headers` field from the C# bulk item entirely.
   3. **`LicenseModules` and `SeatCount` fields were missing from the request body.**
      `GenerateKeycode.php::getTrialParams()` always sets `LicenseModules` (empty string by
      default, or a DB-sourced module string keyed by `license_category_name` + `iso`).
      `getAdditionalParams()`/`isParentProduct()` sets `SeatCount = Seats` specifically for
      master-key/template requests (`LicenseKeycodeTypeId == 3`) — which includes **SAEP**, the
      exact category seen failing in the live `skyrise_license_failure` audit rows. Fixed: added
      `LicenseModules` to `KeycodeRequest` (defaults to `""`, matching PHP), and `SkyriseKeycodeClient`
      now sets `SeatCount` when `isTemplate` is true.

   These are confirmed, source-verified fixes (not guesses) and directly address plausible causes
   of the 401/validation failures — a malformed bulk-item shape being rejected outright is a very
   plausible explanation for an auth-layer-looking rejection, since Skyrise's own error handling
   for malformed bulk items is unconfirmed (item 4). **Re-run `SkyriseKeycodeClientLiveTests`
   after this fix** to see whether the 401 persists, changes to a different status code, or
   resolves. If it still fails identically, the credential-scope hypotheses above remain the next
   thing to chase (via Postman + the ecom/Skyrise team).

   **UPDATE 3 — reran after the shape fixes: identical result (`401`, empty body).** Ruled out
   host/URL mismatch (both `SkyIdentity.TokenUrl` and `Skyrise.BaseUrl` in
   `appsettings.Development.json` exactly match the confirmed-correct values in
   `php/skyidentity-sdk/Environment.php` and `php/skyrise-sdk/Environment.php`). This, combined
   with the empty response body (vs. the detailed JSON validation-error bodies seen in historical
   `skyrise_license_failure` rows for genuinely malformed requests), pointed strongly at a true
   auth/credential rejection happening before request parsing — not a payload shape issue.

   **UPDATE 4 — ROOT CAUSE CONFIRMED, independent of our C# code entirely.** Built a standalone
   PowerShell script that: (1) performs the exact SkyIdentity `client_credentials` token exchange
   using `EcomConsumer`, (2) computes a fresh `LegacyAuthTokenSigner`-equivalent HMAC-SHA256
   `RequestToken` using `ThundercatsQA`, and (3) sends a `curl` POST to `/api/v1/bulk` — completely
   outside our codebase, no C# involved. **Result: identical 401 with an empty body.** This proves
   conclusively the 401 is NOT a bug in our C# port (auth header attachment, bulk envelope shape,
   or HMAC signing algorithm) — it is a genuine credential/scope rejection at the Skyrise API/
   gateway layer.

   **Smoking gun: decoded the `EcomConsumer` SkyIdentity JWT payload.** It contains:
   ```json
   {
     "aud": "Services.Consumer.AccountMgmt",
     "scope": [
       "Services.Consumer.AccountMgmt.CanRequestAccount",
       "Services.Consumer.AccountMgmt.CanRetryEmail",
       "Services.Consumer.AccountMgmt.CanUpdateLicenses"
     ],
     "client_id": "EcomConsumer"
   }
   ```
   `EcomConsumer`'s token audience/scopes are entirely about **consumer account management**
   (registration, retry-email, license updates) — **none of them authorize the bulk
   keycode-generation API** (`/api/v1/bulk`). This is exactly the mismatch the user originally
   suspected ("the token we generate is from client id ecomConsumer but the skyrise client id is
   thunder something") — confirmed correct, now with hard evidence (the JWT claims themselves)
   rather than a hunch.

   **Next action (blocking, not something we can fix in code): get the correct SkyIdentity
   client_id/client_secret pair that is actually scoped/authorized for `/api/v1/bulk` from the
   ecom/Skyrise team.** Whatever `GenerateKeycode.php`'s production `Connections::get('skyrise')`
   config resolves to at runtime is the authoritative source of truth for which SkyIdentity client
   PHP actually uses for this flow — `EcomConsumer` may simply be the wrong client for this
   specific service, unrelated to whether it's "dead" (it's alive, just wrongly scoped). Once a
   correctly-scoped client_id/secret is obtained, swap it into `SkyIdentity:ClientId`/
   `SkyIdentity:ClientSecret` in `appsettings.Development.json` and rerun
   `SkyriseKeycodeClientLiveTests` — expect either a 200-with-license-key or a different,
   more specific error at that point.

4. **Confirm the Skyrise API's actual error/response shape.** The client's `ExtractLicenseKey`
   does a best-effort recursive search for a `licenseKey` property; the real API's success/error
   JSON contract (Section 10, item 3 in the reference doc) is still unconfirmed and may need
   adjustment once live responses are seen.

5. **Resolve `CBSB`/`OTSF`/`CBEP` missing `Storage`/`LicenseKeycodeTypeId` handling.** Currently
   ported as-is from PHP (fields omitted/empty) — unclear whether Skyrise defaults or requires
   these (doc item 10).

5a. **NEW — resolve the WIFI `LicenseCategoryId` discrepancy (130 vs. 135).** QA DB query against
   `license_category` confirms `WIFI = 135` in the ecom schema, but PHP's `PRODUCT_MATRIX` (and our
   port in `ProductMatrixProvider.cs`) hardcodes `130` — a value sent *to* the external Skyrise API,
   not looked up from our local DB. These may be two independent ID spaces that happen to coincide
   for the other 9 categories (SAEP=220, ADE=55, WSAV=100, WSAI=115, WSAC=120, CBSB=200, OTSF=233,
   CBEP=234, WE=117 all matched exactly). Left as `130` (matching verified PHP source) until
   confirmed which value Skyrise's live API actually expects for WIFI — do not assume DB
   `license_category_id` and Skyrise's `LicenseCategoryId` are the same numbering scheme without
   evidence.

5b. **NEW — `product_id = 152200130` confirmed legitimate (doc item 5 RESOLVED).** QA DB query
   confirms this is "OpenText Core Endpoint Protection 30 Day Trial", `product_family_id=300`,
   `license_keycode_type_id=3`, `uses_keycode=1` — a real, intentional generic trial product used
   by the DB-side fallback proc, not a stale/orphaned reference.

5c. **NEW — `sky_update_type_id = 4` (`update_license`, the standalone Skyrise batch sync) rows ARE
   actively being inserted (doc item 8 partially resolved).** QA DB query shows recent rows
   (including one dated 2026-07-24) alongside a legacy row from 2023 — this queue is not dormant.
   Still unconfirmed: which code path inserts these (doc item 8's original question). Given the
   volume is very low (3 rows total in the sample vs. thousands of `CBEP_delete` type-13 rows),
   this is likely triggered by a narrow, infrequent flow (e.g. manual license admin action or a
   specific renewal path) — worth tracing before deciding if the C# trial-creation port needs to
   feed this queue at all.

5d. **RESOLVED — table name was wrong, not missing.** The real table is
   `dbo.skyrise_license_failure` (found via SSMS object browser), not `skyrise_failure` — that's
   why the earlier query returned "Invalid object name". Confirmed schema via
   `INFORMATION_SCHEMA.COLUMNS`:
   - `skyrise_license_failure_id` (int, PK)
   - `skyrise_request` (nvarchar(max))
   - `skyrise_failure_message` (nvarchar(max))
   - `failure_date` (datetime) — NOT `insert_date`

   Updated `SkyriseFailure.cs` and `SkyriseFailureRepository.cs` to match: `[Table("skyrise_license_failure")]`,
   `SkyriseLicenseFailureId`/`skyrise_license_failure_id`, `FailureDate`/`failure_date`. No DDL needed —
   table already exists in QA. Build verified green after the fix.

5f. **CORRECTED — "Skyrise keycode generation is disabled." is NOT a PHP kill-switch; there is
   nothing to port.** Checked the actual PHP call site:
   ```php
   $sky_key = GenerateKeycode::create($keycodeRequestData);
   if ($sky_key->save()) {
       // success
   } else {
       // Keycode will now be generated through ecom (the old, fallback method)
       if ($sky_key->errors) {
           $skyriseFailure = SkyriseFailure::create([
               'skyrise_request' => json_encode($keycodeRequestData),
               'skyrise_failure_message' => json_encode($sky_key->errors)
           ]);
           $skyriseFailure->save();
       }
   }
   ```
   PHP treats `$sky_key->errors` as an **opaque, generic failure** — it never inspects or
   pattern-matches the message text. "Skyrise keycode generation is disabled." is simply whatever
   error string the Skyrise API (or its SDK) happened to return that day; it is API-controlled and
   could change wording at any time without notice. **Action for our C# port (folded into item 3
   below): keep failure handling fully generic** — log the raw message as-is, fall back to the
   DB-side proc, and never branch application logic on specific substrings of
   `skyrise_failure_message`. If we want to know *whether* this reflects an actual account/
   environment-level toggle Skyrise enforces server-side, that has to come from Skyrise's own API
   docs or the Skyrise service team directly — not from anything in the PHP repo, since PHP has no
   special knowledge of it either.

5g. **CRITICAL NEW FINDING — real historical bulk-failure rows suggest our `/api/v1/bulk` request
   shape does not match Skyrise's actual contract.** Several `skyrise_license_failure` rows for
   `SDNS` (not one of our currently-ported categories, but same bulk endpoint family) show requests
   hitting `/api/v1/templates/{bulkId}/products/sdns` and failing with:
   ```json
   {"errors":{"updateModel":["The updateModel field is required."],
   "$.startDate":["Value should be a integral number between -9223372036854775808 and 9223372036854775807"]}}
   ```
   Two takeaways:
   - The real API expects a wrapping `updateModel` field in the request body — worth checking
     whether our `SkyriseKeycodeClient`'s bulk envelope includes this.
   - `startDate`/`endDate` are Unix epoch integers (seconds), and PHP's own historical requests
     *also* triggered this exact validation error — meaning this specific failure mode is a known,
     recurring PHP-side bug/edge case, not necessarily something our port is doing wrong. But it's
     strong evidence the live API is picky about the bulk envelope shape, which is relevant to
     debugging our current 401 (item #3 below) — a shape mismatch could plausibly surface as an
     auth-layer rejection depending on how the API validates requests.
   This does not directly explain the 401 seen in our own live test (different category, different
   error code), but reinforces that the next debugging step should be to capture the *exact* raw
   request/response bytes for our own bulk call, not just assume it's a credential problem.

5e. **NEW — QA keycode-modification marker confirmed.** `environment` table shows
   `environment_type='QA'`, `keycode_modification_string='ZZZZ'` — any keycode generated by the
   DB-side fallback proc in this QA environment will have part of it overwritten with `ZZZZ`,
   confirming Section 4 of the reference doc. Useful for visually identifying QA-generated
   keycodes during testing; no code change needed (pure stored-proc behavior).

6. **Wire `ISkyriseKeycodeClient` into the actual trial-creation flow** once the
   form-submission/trial-persistence code exists in this codebase. Per Section 13 of the
   reference doc, agree on:
   - who computes `LicenseKeycodeTypeId` (form service vs. this service),
   - the exact request field validation/defaulting owner,
   - that failures here should never fail the overall trial-creation request.

7. **Production configuration.** Only QA host/credentials are wired. Before any prod cutover,
   add a production `SkyIdentity`/`Skyrise` config section (via a secrets manager, not
   `appsettings.json`) and confirm the production Skyrise host.

8. **Consider promoting `IProductMatrixProvider` to a DB-backed implementation** (reading
   `license_category`/`product` tables) instead of hardcoded constants, per the original
   design intent — the interface already supports this swap without touching callers.

9. **Reference: full `sky_update_type` lookup table (from QA DB)**, useful context for
    any future work touching the `sky_update` queue:
    | id | name |
    |----|------|
    | 1  | msp_registration |
    | 2  | license_console_registration |
    | 3  | dns_registration |
    | 4  | update_license |
    | 5  | gsmc_migration |
    | 6  | update_license_sdns |
    | 7  | update_wifi |
    | 8  | brtc_update |
    | 9  | update_license_wsat |
    | 10 | O365_update |
    | 11 | CBEP_update |
    | 12 | CBEP_soft_delete |
    | 13 | CBEP_delete |
    | 14 | s2_redemption |
    | 15 | msp_migration |
    | 16 | s2_registration |
    | 17 | update_license_seat |

    Also worth noting: other `sky_*` tables exist in QA (`sky_license`, `sky_api_license`,
    `sky_license_feature`, `sky_active_key_sync_saep`, `sky_template`, etc.) that were not yet
    explored — `sky_api_license` in particular looks like a strong candidate DB source for item
    #8 (DB-backed `IProductMatrixProvider`), but this hasn't been investigated yet.

## 6. Testing

- **`ecom-new-api.Tests/Services/Skyrise/LegacyAuthTokenSignerTests.cs`** — fast, network-free
  unit tests verifying the HMAC-SHA256 signing algorithm against the documented spec (Section 3.8).
  Runs automatically as part of the normal test suite.
- **`ecom-new-api.Tests/Services/Skyrise/SkyriseKeycodeClientLiveTests.cs`** — opt-in integration
  test that calls the real QA SkyIdentity + Skyrise endpoints end-to-end. Disabled by default
  (returns immediately unless `SKYRISE_LIVE_TEST=1` is set), so it never affects normal
  `dotnet test`/CI runs. Run manually with:
  ```powershell
  $env:SKYRISE_LIVE_TEST = "1"
  dotnet test --filter FullyQualifiedName~SkyriseKeycodeClientLiveTests --logger "console;verbosity=detailed"
  ```
  Last live run result (see follow-up #3 above): `Success=False`, `ErrorMessage=Skyrise bulk call
  returned 401`. The SkyIdentity token exchange succeeded; the bulk call itself was rejected.
