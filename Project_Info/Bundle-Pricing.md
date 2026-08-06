# Bundle Pricing

## Purpose

This document captures the bundle pricing flow behind `GET /api/bundle-pricing`.

## API scope

- Build pricing inputs from request items/modules.
- Resolve message-key discount context.
- Execute configurator pricing procedures.
- Return fully calculated totals for UI consumption.

## Current status

- Endpoint, service, repository, and DTO pipeline are implemented.
- Unit tests for core pricing math are in place.
- Production-readiness work remains for middleware and integration verification.

## Request and response notes

- Required: `locale`, `license_keycode_type_id`, and at least one `items[]` entry.
- Important item fields include license category, seat count, years, and billing model attributes.
- Response includes itemized pricing and computed totals.

## Core dependencies

- `PricingService`
- `MessageKeyService`
- `CurrencyService`
- `PricingRepository`

## Key stored procedures and references

- [usp_cart_select_license_configurator_pricing.md](./StoredProcedures/usp_cart_select_license_configurator_pricing.md)
- [usp_cart_select_message_key.md](./StoredProcedures/usp_cart_select_message_key.md)
- [usp_message_select_message_campaign_cart_discount.md](./StoredProcedures/usp_message_select_message_campaign_cart_discount.md)
- [usp_cart_select_new_product_discount.md](./StoredProcedures/usp_cart_select_new_product_discount.md)
- [cart_discount.md](./StoredProcedures/cart_discount.md)

## Test guide references

- Validate seat count, multi-year, and multi-item module scenarios.
- Validate locale/currency mapping and error behavior.

## Related docs

- [License-Options.md](./License-Options.md)
- [Cart.md](./Cart.md)
- [README.md](./README.md)
