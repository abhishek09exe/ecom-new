# End-to-End Architecture — ecom-new-api

## Flow Overview

```
Swagger UI  →  Controller  →  Service  →  Repository  →  EF Core  →  SQL Server DB
```

---

## Layer by Layer

### 1. Swagger UI (Entry Point)
- Auto-generated from your controller attributes
- You send a JSON request body → hits the HTTP endpoint

---

### 2. Controller — `CartOrdersController`
- Receives the HTTP request
- **Injects `UserIp`** from `HttpContext` (never trusted from client)
- Calls the **Service** layer
- Maps `ServiceResult` → HTTP response (`201`, `400`, `404`, `500`)

---

### 3. Service — `ICartOrderService / CartOrderService`
- All **business logic** lives here
- Validates the request
- Checks if a `Key` resolves to an existing quote cart (pivot to UPDATE)
- Calls **Repository** methods

---

### 4. Repository — `EfCartOrderRepository`
- **No stored procedures, no raw SQL**
- Builds EF Core entity objects and saves them
- EF Core auto-generates all SQL

---

### 5. EF Core (ORM)
- Translates C# entity operations into real SQL statements

---

### 6. SQL Server DB — `ecom_cart_dev`
- Data is persisted across these tables:

```
cart_order          ← main header row
cart_order_item     ← one row per line item
cart_json           ← extension JSON blob
cart_order_partner  ← links order to a partner (optional)
```

---

## POST Flow (Create Cart)

```
POST /cart/cart-orders
		│
		▼
Controller → injects UserIp
		│
		▼
Service → validates SiteId, Locale, items
		│
		▼
Repository → builds CartOrder entity + CartOrderItems + CartJson
		│
		▼
EF Core → INSERT INTO cart_order (...)
		  INSERT INTO cart_order_item (...) × N items
		  INSERT INTO cart_json (...)
		  INSERT INTO cart_order_partner (...) if partnerKey given
		│
		▼
Returns vendorOrderCode (e.g. "GSM-AB12CD34EF56")
		│
		▼
Repository → immediately re-reads with all JOINs (SelectCartOrderAsync)
		│
		▼
Controller → 201 Created + full CartOrderResponse JSON
```

---

## GET Endpoints — Current Status

There is only one working GET right now — it is called **internally** after every POST
(not exposed as a standalone endpoint yet). The other 3 GETs are stubs:

| Endpoint              | Status                          | Priority |
|-----------------------|---------------------------------|----------|
| `SelectCartOrderAsync`| ✅ Works — used internally after POST | N/A |
| `GET /license-options`| ❌ NotImplementedException       | **🔥 CRITICAL** |
| `GET /configure`      | ❌ NotImplementedException       | Medium |
| `GET /upgrade`        | ❌ NotImplementedException       | Medium |

**NEXT STEP:** Implement `GET /license-options` - see [IMPLEMENTATION_PLAN_LICENSE_OPTIONS_API.md](./IMPLEMENTATION_PLAN_LICENSE_OPTIONS_API.md) for complete guide.

---

## What the POST Response Gives You

The `201 Created` response returns a fully hydrated `CartOrderResponse` with:

| Field             | Description                                          |
|-------------------|------------------------------------------------------|
| `vendorOrderCode` | Your cart's unique ID for all future operations      |
| `cartOrderId`     | Internal DB primary key                              |
| `currencyCode`    | Resolved currency (e.g. "USD")                       |
| `locale`          | BCP-47 locale tag (e.g. "en-US")                     |
| `siteId`          | Site that placed the order (e.g. "gsm")              |
| `items[]`         | All line items with product + license category info  |
| `cartJson`        | Raw extension JSON blob stored in DB                 |
| `partnerKey`      | Partner UUID if a partner was linked                 |

> **Important:** Save the `vendorOrderCode` from the POST response.
> You will need it for all future GET / UPDATE calls on the same cart.

---

## Sample POST Request Body

```json
{
  "siteId": "gsm",
  "locale": "en-US",
  "currencyCode": "USD",
  "items": [
	{
	  "licenseCategoryName": "SMB",
	  "productId": 1,
	  "quantity": 2,
	  "years": 1.0,
	  "unitPrice": 99.99
	}
  ]
}
```

### Required fields
- `siteId`
- `locale`

### Server-injected (do NOT send from client)
- `userIp` — injected from `HttpContext.Connection.RemoteIpAddress`

### Optional fields
- `currencyCode`, `vendorOrderCode`, `partnerKey`, `routingAction`,
  `salesOrderDate`, `messageCampaignId`, `key`, `cartDiscountId`, `urlLink`
