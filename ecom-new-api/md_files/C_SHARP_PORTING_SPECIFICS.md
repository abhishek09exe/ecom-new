# C# Porting Specifics (Configurator + Cart)

This is a focused migration checklist for moving the current Try/Buy/Upgrade configurator flow from Lithium/PHP to a modern C# service.

## 1) Files to Port First

### API routing and composition
- ecomwebdev/apps/partner_api/config/routes.php
- ecomwebdev/apps/partner_api/webroot/index.php
- ecomwebdev/apps/partner_api/config/bootstrap/libraries.php

### API controllers (business flow)
- ecomwebdev/apps/partner_api/controllers/CartLicenseOptionController.php
- ecomwebdev/apps/partner_api/controllers/CartNewOptionController.php
- ecomwebdev/apps/partner_api/controllers/CartOrderController.php
- ecomwebdev/apps/partner_api/controllers/CartAwareRestController.php
- ecomwebdev/apps/partner_api/controllers/PartnerAccountAwareRestController.php

### Core model logic used by configurator
- ecomwebdev/li3_wr/models/cart_order/PartnerCartNewOption.php
- ecomwebdev/li3_wr/models/cart_order/LicenseOption.php
- ecomwebdev/li3_wr/models/cart_order/LicenseProfile.php
- ecomwebdev/li3_wr/models/cart_order/BillingModel.php
- ecomwebdev/li3_wr/models/license/CategoryUpgrade.php
- ecomwebdev/li3_wr/models/license/License.php

### Cart create/update models (order submission path)
- ecomwebdev/li3_wr/models/cart_order/PartnerCartOrder.php
- ecomwebdev/li3_wr/models/cart_order/PartnerCartItem.php
- ecomwebdev/li3_wr/models/cart_order/Order.php
- ecomwebdev/li3_wr/models/cart_order/Item.php

### Procedure wrapper files to replicate as C# repository methods
- ecomwebdev/li3_wr/extensions/procedures/cart_order/select/PartnerOrderPageDetails.php
- ecomwebdev/li3_wr/extensions/procedures/cart_order/select/MessageKey.php
- ecomwebdev/li3_wr/extensions/procedures/cart_order/select/LicenseProfile.php
- ecomwebdev/li3_wr/extensions/procedures/cart_order/select/BillingModel.php
- ecomwebdev/li3_wr/extensions/procedures/license/select/CategoryUpgrade.php
- ecomwebdev/li3_wr/extensions/procedures/license/select/SelectByLicenseId.php
- ecomwebdev/li3_wr/extensions/procedures/cart_order/insert/Order.php
- ecomwebdev/li3_wr/extensions/procedures/cart_order/insert/Item.php
- ecomwebdev/li3_wr/extensions/procedures/cart_order/select/Order.php
- ecomwebdev/li3_wr/extensions/procedures/cart_order/select/Item.php
- ecomwebdev/li3_wr/extensions/procedures/cart_order/update/Order.php
- ecomwebdev/li3_wr/extensions/procedures/cart_order/update/Item.php
- ecomwebdev/li3_wr/extensions/procedures/cart_order/select/OrderPageDetails.php (needed if /buy flow is included)

### Frontend block sources if UI is being moved too
- ecomwebdev/concrete5-cms/src/application/blocks/form_console_keycode/controller.php
- ecomwebdev/concrete5-cms/src/application/blocks/form_console_keycode/view.php
- ecomwebdev/concrete5-cms/src/application/blocks/form_console_keycode/view-form.js
- ecomwebdev/concrete5-cms/src/application/blocks/form_gsm_try_buy_configurator/controller.php
- ecomwebdev/concrete5-cms/src/application/blocks/form_gsm_try_buy_configurator/view.php
- ecomwebdev/concrete5-cms/src/application/blocks/form_gsm_try_buy_configurator/view-form.js

### Connection/config source to migrate to appsettings + secrets
- ecomwebdev/etc/connections.dev.json

## 2) Stored Procedures to Check and Update

These are the minimum procedures to review for a C# port of the current flow.

### Configurator read path (/configure and /upgrade)
- usp_cart_select_message_key
- usp_license_select_license_by_id
- usp_cart_select_license_profile
- usp_product_select_license_category_upgrade
- usp_cart_select_license_billing_model
- usp_partner_cart_select_order_page_details

### New product read path (/buy, optional but usually needed)
- usp_cart_select_order_page_details

### Cart create and fetch path
- usp_cart_insert_cart_order
- usp_cart_insert_cart_order_item
- usp_cart_select_cart_order
- usp_cart_select_cart_order_item

### Cart update path
- usp_cart_update_cart_order
- usp_cart_update_cart_order_item

### Product storage support (used by item model)
- usp_license_select_license_category_storage

## 3) What to Change in Those Stored Procedures

- Confirm parameter names/types exactly match C# request DTOs (especially: locale, language_code, location_code, partner_key, item_hierarchy_id).
- Standardize JSON fields returned for list-like data (years_list, seat_list, pricing_level_list, usage_pricing_model, vault, retention_model, storage_list).
- Keep output contract stable for cart write procs that return response_code and message.
- Verify null handling for optional parameters (site_id, license_category_id, language/location codes).
- Ensure deterministic ordering for selectable options so UI behavior is stable.
- Validate collation/case behavior for brand/category comparisons used in upgrade logic.
- Add/confirm EXEC permissions for the new C# service identity.

## 4) C# Implementation Mapping (simple)

- Controllers -> ASP.NET Core controllers or minimal API endpoints
- Lithium model filters -> service layer methods (pre-process + post-process)
- Procedure classes -> repository methods using Microsoft.Data.SqlClient + stored procedures
- Entity hydration + decodeFields -> typed DTO mapping + JSON deserialization
- Partner/account authorization filters -> ASP.NET Core authentication + authorization policies

## 5) Suggested Migration Order

1. Port read-only endpoints first: /configure, /upgrade, /buy.
2. Port data shaping logic from PartnerCartNewOption and CartLicenseOptionController.
3. Port cart write endpoints: /cart-orders and /cart-items behaviors.
4. Add contract tests comparing old PHP JSON response vs new C# JSON response for same inputs.
5. Cut over endpoint-by-endpoint behind feature flags.

## 6) Out of Scope for First Pass

- Apache vhost files under ecomwebdev/apache_vhosts
- Minified frontend artifacts (*.min.js, *.min.css, *.map)
- Unrelated cron/csi/internal_api stored procedure wrappers

