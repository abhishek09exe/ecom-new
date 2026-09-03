# Latency Optimization Summary — `/bundle-pricing` & `/license-options`

_Last updated: backend optimization pass on `feature-sp-migration`_

## Context

The configurator UI calls two separate read-only APIs sequentially while loading:

1. `GET /bundle-pricing` — pricing for the selected bundle items/modules.
2. `GET /license-options` — license aggregate, product options, and upgrade categories for the message key.

The UI was taking **~5-6 seconds** to load. This document captures what was measured, what was
changed, and what is still on the table for both backend and frontend.

---

## Before → After

| Endpoint | Before | After | Change |
|---|---|---|---|
| `/bundle-pricing` (7-item request, warmed) | Sequential per-item queries; several SQL round trips per item (~N × several 100ms) | ~1.9–2.1s | Batched into 2 DB round trips total (was N×several) |
| `/license-options` (warmed) | ~2.48s (already partially parallelized) | ~2.3–2.8s (no regression; correctness bug fixed) | Fixed a real concurrency bug; removed 1 round trip via cache; confirmed remaining round-trip count is the floor |

**Combined sequential frontend cost today:** roughly **4.3–4.9s** for the two calls back-to-back,
which lines up with the ~5-6s the UI observes once frontend render/hydration overhead is added.

---

## What We Did (Backend)

### `/bundle-pricing` — `PricingRepository.GetItemPricingAsync`
- **Before:** looped over each bundle item (and its modules) and issued a separate chain of
  EF Core queries per item — for a 7-item bundle this meant many sequential remote SQL round trips.
- **After:** rewritten to run in **two batched round trips total**, regardless of item count:
  1. One query fetches all candidate products across every category name / years combination
	 present in the request.
  2. One query fetches all pricing rows for the matched product IDs.
  - In-memory matching (language/location code) uses `StringComparison.OrdinalIgnoreCase` to
	preserve SQL Server's case-insensitive collation behavior that the old per-item SQL WHERE
	clauses relied on implicitly.
- **Result:** ~1.9–2.1s warmed up, independent of item count (previously scaled with item count).

### `/license-options` — `LicenseOptionsRepository.SelectLicenseOptionsAsync`
- **Existing groundwork already in place:** pooled `IDbContextFactory<AppDbContext>`, `IMemoryCache`
  for reference data, and partial `Task.WhenAll` parallelization.
- **Bug fixed:** several queries (product-options block: products/years/seats/pricing) were
  sharing the single scoped `AppDbContext` while being awaited "in parallel" — this is unsafe in
  EF Core and was intermittently throwing
  `InvalidOperationException: A second operation was started on this context instance...`,
  surfacing as random 500s under load. Fixed by giving every concurrently-awaited query its own
  isolated `DbContext` via `RunIsolatedAsync` (backed by the pooled factory).
- **Round trip removed:** `LicenseTypeDescription` lookup now uses the existing
  `GetLicenseTypesCachedAsync()` in-memory cache (4-hour TTL, reference data rarely changes)
  instead of a live per-request query.
- **Attempted but reverted:** merging the legacy stored-procedure call and the
  `fn_license_select_license_profile` function call into the same large parallel batch (~14
  concurrent connections) made things **worse** (~2.9–3.0s vs ~2.48s baseline) — likely because the
  remote SQL Server connection is `Encrypt=True` (TLS), and opening many new connections
  simultaneously pays a handshake cost that outweighs the round-trip savings. Reverted to the
  proven ordering.
- **Connection pool tuning:** added `Min Pool Size=20;Max Pool Size=100` to the `EcomDb` connection
  string to pre-warm pooled connections ahead of the isolated-context fan-out. Did not measurably
  change latency in this environment (the floor is dominated by genuine per-query SQL execution
  time, not connection setup), but is a safe, low-risk change to keep.
- **Result:** ~2.3–2.8s warmed up — same ballpark as the 2.48s baseline, but now **correct**
  (no more concurrency exceptions) and with one fewer round trip.

### Why `/license-options` didn't drop further
The repository still issues roughly a dozen distinct queries (license lookup, legacy SP, profile
function, category/capability lookups, upgrade categories, seats, and the product-options block).
Even fully parallelized, wall-clock time is bounded by:
- The **slowest single query** in the batch (not the sum, but not free either — remote SQL Server
  round trips to `qadenecom6.services.webroot` appear to sit around 200-300ms+ each even for simple
  lookups, likely network latency to the remote host).
- The **legacy stored procedure** (`usp_license_select_license_by_id`) and the
  **`fn_license_select_license_profile` function**, both of which must run against SQL Server
  specifically and can't be trivially rewritten as LINQ/batched with the rest.

---

## What's Left — Backend

1. **Reduce the legacy SP / function call cost directly.**
   - Ask a DBA to check the execution plan for `usp_license_select_license_by_id` and
	 `fn_license_select_license_profile` for missing indexes — these are two of the more expensive
	 calls and are SQL-only (can't be replaced with LINQ).
   - If feasible, consider replacing the scalar function with an inline table-valued function
	 (`fn_license_select_license_profile` — if it's currently a multi-statement TVF, converting to
	 inline can allow the optimizer to fold it into the outer query instead of running as a black box).
2. **Investigate raw network latency to `qadenecom6.services.webroot`.**
   - If the app and SQL Server are in different regions/networks, even a "cheap" query pays a fixed
	 round-trip tax. Co-locating the API host closer to the DB (or moving to Azure with both in the
	 same region/VNet) would shrink every one of these round trips proportionally — this benefits
	 both endpoints, not just `/license-options`.
3. **Consider `SqlBulkCopy`/table-valued parameters** is not applicable here (this is all reads),
   but for reads, consider **combining the remaining independent queries into fewer physical round
   trips** using `FromSqlInterpolated` with a single multi-result-set stored procedure (SQL Server
   supports multiple `SELECT`s in one batch, and EF Core / raw `SqlDataReader` can consume multiple
   result sets from one `EXEC`). This trades LINQ readability for one network round trip instead of ~12.
4. **Re-attempt the "merge more into one batch" idea, but staggered.** Instead of firing all ~14
   isolated-context queries at once, group them into 2-3 waves (e.g., wave 1 = license-dependent
   fast lookups, wave 2 = legacy SP + profile function) so the connection pool isn't asked to
   establish many new TLS connections in the same instant. This might recover the small win without
   the regression seen in this session.
5. **Cache more reference data.** `ProductType` is already cache-eligible via
   `GetProductTypesCachedAsync` but isn't wired into the hot path yet the way `LicenseType` now is —
   audit for other reference-only lookups (channels, distribution methods) that rarely change and
   could move to `IMemoryCache`.
6. **`/bundle-pricing`:** if item counts grow much larger than 7, re-verify the 2-query batch still
   holds — right now it's O(1) round trips regardless of item count, which is the ideal shape; no
   further backend work needed unless a specific new bottleneck is measured.

---

## What's Left — Frontend

1. **Fire both requests in parallel instead of sequentially**, if the UI doesn't strictly need
   `/license-options` data before it can start the `/bundle-pricing` call (or vice versa). Even
   without any further backend change, running them concurrently on the client would cut the
   **combined** wait from ~4.3–4.9s to roughly **max(bundle-pricing, license-options) ≈ 2.3–2.8s** —
   the single biggest lever available right now, and it requires no backend changes.
2. **Show partial/progressive UI.** Render whichever response comes back first (likely
   `/bundle-pricing` at ~2s) while `/license-options` is still in flight, instead of blocking the
   whole page on both.
3. **Cache `/license-options` client-side per message_key** for the session — if the user
   navigates back to the same configurator state, avoid re-fetching identical license data.
4. **Debounce configurator interactions** that re-trigger `/bundle-pricing` (e.g., seat/year
   sliders) so rapid user input doesn't fire a new request per keystroke/tick.
5. **Add a loading skeleton keyed to each panel** (license info vs. pricing) so perceived latency
   drops even if actual wall-clock time doesn't change further.

---

## Recommended Next Step

Given current numbers, the highest-value, lowest-risk next step is **#1 under Frontend**
(parallel fetch) — it turns the effective UI latency from ~4.3–4.9s into ~2.3–2.8s without touching
the backend at all. Backend items #1–#3 (index/query-plan tuning on the legacy SP and function,
network locality, and multi-result-set batching) are the next tier if further improvement is
needed after that.
