# ecomwebdev C# Cart Port

This repository contains an in-progress ASP.NET Core port of the legacy Lithium/PHP cart flow.

Primary goals:
- Preserve behavioral parity for cart creation and configurator read flows.
- Keep response contracts stable during phased migration.
- Move from scaffolded behavior to full stored-procedure-backed parity on SQL Server.

## Current Scope

Migration target (bare minimum to cut over):
- Write path: `POST /cart/cart-orders`
- Read path used by configurator: `GET /license-options`, `GET /configure`, `GET /upgrade`

Current implementation status:
- `POST /cart/cart-orders`: implemented in API + application services.
- `GET /license-options`: yet-to-be-decided.
- `GET /configure`: yet-to-be-decided.
- `GET /upgrade`: yet-to-be-decided.
- Root health endpoint (`GET /`): not currently implemented.
- Request pipeline parity middleware (auth, CSRF, permission, account-context injection): pending.

## Technology

- .NET `10.0`
- ASP.NET Core Web API
- EF Core + SQL Server

## Local Testing

## Endpoint Smoke Tests

License options read path:

```bash
curl -i "http://localhost:5280/license-options?locale=en_US&message_key=YOUR_KEY"
curl -i "http://localhost:5280/configure?locale=en_US&message_key=YOUR_KEY"
curl -i "http://localhost:5280/upgrade?locale=en_US&message_key=YOUR_KEY"
```

Cart create path:

```bash
curl -i -X POST "http://localhost:5280/cart/cart-orders" \
  -H "Content-Type: application/json" \
  -H "X-CSI-USER: test.user" \
  -H "X-CSI-USER-ID: 12345" \
  -H "X-CSI-LOCALE: en_US" \
  -H "X-WRCART-CSRF: test-token" \
  -d '{
    "siteId": "WRCOM",
    "locale": "en_US",
    "userIp": "127.0.0.1",
    "items": []
  }'
```

Note: headers above mirror legacy request context expectations, but full middleware enforcement parity is still in progress.

## Request Pipeline Parity Checklist

Target behavior to match legacy CSI/Lithium flow:
- Session/cart bootstrap from `vendor_order_code`
- CSRF validation for non-GET requests using `X-WRCART-CSRF`
- Authentication from `X-CSI-USER` and `X-CSI-USER-ID`
- Permission check for cart create
- Account context injection (`username`, `csi_user_id`, `p_rc`, `trx_rc`)
- Locale injection from `X-CSI-LOCALE`

## Stored Procedure Parity

Read endpoints currently call key stored procedures through repository methods (message key, license profile/header, billing model, order page details).

Write-path parity still needs full legacy alignment:
- Procedure-level create/update branching parity (quote-key create->update behavior)
- Full validation matrix parity with legacy order/item model filters
- Re-check all computed response fields against legacy aggregate behavior

## Configurator Flow Context

Legacy UI flow being ported:
1. User submits keycode.
2. Keycode/session context redirects to configurator route.
3. Configurator loads and calls read endpoints (`/license-options`, `/configure`, `/upgrade`).
4. UI renders TRIAL / RENEW / ADD SEATS options.
5. UI posts selected items to `POST /cart/cart-orders`.

## Migration Notes

- Migration strategy is intentionally incremental: parity first, cleanup second.
- Keep response contracts stable to avoid frontend regressions in phased rollout.
- Prioritize behavior correctness over architectural refactors until cutover criteria are met.
