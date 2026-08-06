# Cart

## Purpose

This document tracks the cart order creation and retrieval flow, centered on `POST /cart/cart-orders`.

## API scope

- Create cart order header and items.
- Persist partner, routing, message, and JSON metadata rows.
- Return consistent order and item data for downstream flows.

## Current status

- Core cart create path is implemented in API, service, and repository layers.
- Full parity with legacy behavior is still in progress for edge cases and calculated fields.
- Middleware parity is still required before production cutover.

## Current flow summary

1. Validate request payload and context headers.
2. Insert cart order header.
3. Insert cart order items.
4. Recalculate totals and dependent values.
5. Return persisted order data.

## Key stored procedures and references

- [usp_cart_insert_cart_order.md](./StoredProcedures/usp_cart_insert_cart_order.md)
- [usp_cart_insert_cart_order_item.md](./StoredProcedures/usp_cart_insert_cart_order_item.md)
- [usp_cart_select_cart_order.md](./StoredProcedures/usp_cart_select_cart_order.md)
- [usp_cart_select_cart_order_item.md](./StoredProcedures/usp_cart_select_cart_order_item.md)
- [usp_next_id.md](./StoredProcedures/usp_next_id.md)

## Validation and parity focus

- Request-level validations (auth, CSRF, account context, locale).
- Item-level and order-level validation matrix parity.
- DB side effects parity with legacy flow.

## Smoke test (example)

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

## Related docs

- [License-Options.md](./License-Options.md)
- [Bundle-Pricing.md](./Bundle-Pricing.md)
- [README.md](./README.md)
