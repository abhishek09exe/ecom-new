# ecomwebdev C# Cart Port (ecom-new)

This repository contains an in-progress C# port of the legacy Lithium/PHP cart flow to ASP.NET Core.

Current focus:
- Port cart order creation behavior (`POST /cart/cart-orders`)
- Preserve request pipeline behavior (auth, locale, CSRF, request context)
- Move incrementally toward stored-procedure-backed implementation and full configurator parity

## Technology

- .NET `10.0`
- ASP.NET Core Web API


## API Endpoints (Current)

- `GET /`
  - Health-style ready endpoint
  - Returns service metadata

- `POST /cart/cart-orders`
  - Current scaffold for cart order create path

## Prerequisites

- .NET SDK 10.x preview/compatible SDK installed

## Run

From repository root:

```bash
cd cPort
dotnet restore
dotnet build
dotnet run
```

Default local URLs are determined by ASP.NET launch settings/environment.

## Quick Smoke Test

```bash
curl -i http://localhost:5000/
```

Example `POST /cart/cart-orders` test (adjust URL/JSON as needed):

```bash
curl -i -X POST "http://localhost:5000/cart/cart-orders" \
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

## Configuration

- `appsettings.json`
- `appsettings.Development.json`

## Notes

- This project is intentionally incremental: behavior parity first, architecture cleanup second.
- Keep API response contracts stable to avoid frontend regressions during phased rollout.
