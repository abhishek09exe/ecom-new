# Skyrise Keycode Generation Service — Reference Package

Paste this whole folder into your existing .NET ecom Visual Studio project as a reference package for building the Skyrise keycode generation service.

## Scope note

You're building in an **existing** .NET ecom codebase connected to the **same QA DB**, and your code will eventually **merge** with the form-submission developer's code (keycode generation is invoked mid-way through the trial-creation POST). Because of that, this package includes not just the narrow Skyrise-client scope, but enough surrounding trial-flow context for a smooth future merge.

## What's in here

### `docs/`
- **`04_SKYRISE_KEYCODE_GENERATION.md`** — the primary spec for this work. Everything resolved/verified about Skyrise auth, request/response shapes, the DB-side fallback, and what's still needed from elsewhere. Start here.
- `01_ENDPOINT_PROTECTION_TRIAL_MIGRATION.md` and `02_MULTI_TRIAL_MIGRATION.md` are **not duplicated here** — copy them directly from `cPort/docs/` in the main workspace (they're long, unchanged, complete files; re-copying risked introducing transcription errors). They give the full trial-flow context (form validation, `TrialRegistration::save()` chain, stored proc contracts) that the form-submission developer owns and that your merged codebase will eventually need to understand end-to-end.

### `php/`
Reference copies of the actual PHP source this service is porting. Some files are **complete**, some are **key excerpts** (clearly marked at the top of the file) where the full file wasn't captured — for those, pull the complete original from the workspace paths noted in each file's header comment before treating it as final.

- `GenerateKeycode.php` — the actual class that calls Skyrise today (complete)
- `skyrise-sdk/` — the vendored SkyRise PHP SDK (`skyrise/src/`)
- `skyidentity-sdk/` — the vendored SkyIdentity PHP SDK (`skyidentity/src/`)

### `stored_procs/`
Full T-SQL source for every proc touched by keycode generation, pulled from your QA DB via `sp_helptext`:
- `usp_trial_insert_trial_registration.sql` — creates the trial registration row
- `usp_trial_insert_trial.sql` — creates the trial record; contains the DB-side keycode fallback call
- `usp_trial_insert_trial_module.sql` — legacy module-attach path
- `usp_sky_insert_update.sql` — the generic (misleadingly-named) external-sync queue inserter
- `usp_license_generate_keycode_cart.sql` — the actual DB-side keycode algorithm (two formats: SHA1-hash vs. checksum, by product family)

### `config/`
- `skyrise-connections-reference.json` — **only** the Skyrise/SkyIdentity-relevant connection blocks extracted from `etc/connections.dev.json`, not the full file (that file has unrelated Salesforce/AWS/other secrets — no reason to carry those into a new project). The `sky_identity` credential in here is **confirmed dead** (`400 invalid_client` on live test) — needed as a shape reference only, not a working value. Get a fresh one from the ecom team before live-testing.

## Before you start coding

Read Section 12 and 12.1 of `04_SKYRISE_KEYCODE_GENERATION.md` first — it directly answers "can I start coding now" and "what's actually blocked vs. not." Short version: yes, build the client and its two auth components now (both fully documented, including the complete HMAC-SHA256 signing algorithm for the inner request signer); live end-to-end testing against QA is blocked only on getting a working `sky_identity` credential.
