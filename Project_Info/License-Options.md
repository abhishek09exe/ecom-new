# License Options

## Purpose

This document tracks the `GET /license-options`, `GET /configure`, and `GET /upgrade` read flow used by the configurator.

## API scope

- Resolve a `message_key` context.
- Return license profiles and upgrade options.
- Keep response shape compatible with the legacy API.

## Current status

- Endpoint family is part of migration scope.
- Detailed implementation and parity work is in progress.
- Middleware parity (auth, CSRF, permission, locale/account context) is still required before production cutover.

## Core dependencies

- Service layer under `LicenseOptions`.
- Repository layer under `LicenseOptions`.
- Stored procedures for message and configurator context.

## Key stored procedures and references

- [usp_cart_select_message_key.md](./StoredProcedures/usp_cart_select_message_key.md)
- [usp_cart_select_license_configurator_pricing.md](./StoredProcedures/usp_cart_select_license_configurator_pricing.md)
- [usp_cart_select_new_product_discount.md](./StoredProcedures/usp_cart_select_new_product_discount.md)
- [usp_message_select_message_campaign_cart_discount.md](./StoredProcedures/usp_message_select_message_campaign_cart_discount.md)

## Testing focus

- Verify query contract parity with known PHP responses.
- Validate locale and message-key paths.
- Include negative cases: invalid locale, unknown message key, empty result sets.

## Related docs

- [Bundle-Pricing.md](./Bundle-Pricing.md)
- [Cart.md](./Cart.md)
- [README.md](./README.md)
