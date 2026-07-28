# CSI Endpoint Porting: POST /cart/cart-orders

Endpoint target: https://cartapi.webroot.com/cart/cart-orders

This document is scoped only to the CSI cart create endpoint and captures controller flow, DB operations, and validation rules to preserve behavior during C# porting.

## Route And Runtime Wiring

- Route definition: [apps/csi/config/routes/cart.php](/ecomwebdev/apps/csi/config/routes/cart.php)
  - POST /cart/cart-orders -> AccountAwareRest::create
  - Route model: cart_order
- Model binding: [apps/csi/webroot/index.php](/ecomwebdev/apps/csi/webroot/index.php)
  - cart_order -> wr\\models\\cart_order\\CSIOrder
- CSI controller inheritance chain:
  - csi\\controllers\\AccountAwareRestController
  - csi\\controllers\\RestController
  - wr_api\\controllers\\RestController

## Request Pipeline (Controller Layer)

1. Session + cart bootstrap ([apps/csi/config/bootstrap/session.php](/ecomwebdev/apps/csi/config/bootstrap/session.php))
- Attempts to resolve existing cart from session key vendor_order_code.
- Injects resolved cart object as dispatcher option cartOrder.
- Registers a model save filter for cart_order to persist vendor_order_code into session after successful save.

2. CSRF validation ([apps/csi/config/bootstrap/session.php](/ecomwebdev/apps/csi/config/bootstrap/session.php))
- For non-GET requests, validates request token from header X-WRCART-CSRF (read as http:x_wrcart_csrf) or request body token.
- On failure returns BadRequest and sets/refreshes csi_csrf cookie.

3. Authentication + permission context ([apps/csi/config/bootstrap/auth.php](/ecomwebdev/apps/csi/config/bootstrap/auth.php))
- Resolves authenticated CSI user from Auth::check('csi_user').
- Loads permissions from account or resource table.
- Injects account and permissions into controller options.

4. AccountAwareRestController::create() ([apps/csi/controllers/AccountAwareRestController.php](/ecomwebdev/apps/csi/controllers/AccountAwareRestController.php))
- Calls checkAccess():
  - Fails with Unauthorized if no account.
  - Fails with Forbidden if permission check fails.
- Applies create filter:
  - Merges account-derived fields into payload:
    - username, account_user_name, csi_user_id, insert_by, p_rc, trx_rc.

5. csi\\controllers\\RestController create filter ([apps/csi/controllers/RestController.php](/ecomwebdev/apps/csi/controllers/RestController.php))
- Adds locale from route context to payload: locale.

6. wr_api\\controllers\\RestController::create() ([li3_wr_api/controllers/RestController.php](/ecomwebdev/li3_wr_api/controllers/RestController.php))
- Creates empty CSIOrder entity.
- Calls $entity->save($data, $save_options).
- On success: HTTP 201 and refreshed entity response.
- On validation failure: HTTP 422 with aggregated validation errors.

## Model Behavior For This Endpoint

CSIOrder extends Order and reuses Order::config() and Order::save() behavior, while swapping related classes (CSIItem, CSICustomer, CSICustomers, CSIItemBundle).

### Core create path in Order::save() ([li3_wr/models/cart_order/Order.php](/ecomwebdev/li3_wr/models/cart_order/Order.php))

1. If payload contains key and key resolves to quote key:
- Reads message key (MessageKey::find('first')).
- If quote references existing pending cart, updates existing cart with UPDATE_FIELDS only and returns that cart (no new cart insert).

2. Validation phase ($entity->validates(...))
- Runs base order validation + order-level filter validation + per-item validation.

3. Stored procedure execution for cart header create
  - Procedure class: [li3_wr/extensions/procedures/cart_order/insert/Order.php](/ecomwebdev/li3_wr/extensions/procedures/cart_order/insert/Order.php)
- Procedure name: usp_cart_insert_cart_order
- Inputs:
  - site_id, locale, user_ip, cart_extension_json
- Outputs:
  - response_code, message

4. If cart header create succeeds and items were included in request:
- Each item is saved via CSIItem->save(validate=false).
- Item procedure:
  - usp_cart_insert_cart_order_item
  - Inputs: vendor_order_code, item_json, bundle_json
  - Outputs: response_code, message

5. Conditional customer auto-association
- If inferred customer id is present and autosave allowed, loads customer and writes billing/shipping rows for the new order.

6. Rest layer refresh after create
- Re-reads created cart by key (vendor_order_code) for response payload.
- This read hydrates extension fields, items, customers, route, and formatting fields.

## DB Operations Observed (Create Endpoint)

The endpoint can trigger these DB operations in normal or conditional flows:

1. usp_cart_insert_cart_order
- Insert cart header.

2. usp_cart_insert_cart_order_item (0..N)
- Insert each item/bundle payload.

3. usp_cart_select_cart_order
- Read cart by vendor_order_code for API response refresh.

4. Item reads during refresh (CSIItemBundle/CSIItem path)
- Reads cart items to build bundles and computed product fields.

5. Customer reads/writes (conditional)
- Reads customer by customer_id and updates/inserts cart customer rows.

6. Currency and route reads during refresh
- Currency lookup by currency_id.
- Route resolution from message/license routing logic.

7. Quote-key branch (conditional)
- Message key lookup and cart lookup; may execute update path instead of insert.

## Validation Matrix To Mirror In C#

### Security/access validations

- Authenticated CSI user is required.
- Permission check is required for resource/action (cart_order.create or wildcard).
- CSRF token required for non-GET requests.

### Order-level validations (Order)

- site_id must be one of allowed values (includes CSI).
- locale required.
- currency_code must be valid ISO list if provided.
- sales_order_date date format validation if provided.
- vendor_order_code cannot be empty if provided.
- message_campaign_id must be positive integer if provided.
- message_campaign_platform non-empty if provided.
- partner_key must be UUID if provided.
- account_user_name non-empty if provided.
- url_link must be valid URL if provided.

### Item-level validations (CSIItem / Item)

- Category validation (license_category_name in allowed set).
- Numeric validations (license_seats, quantity, cart_item_bundle_id, etc.).
- years in allowed year set.
- Date validations (start_date, expiration_date, vendor_expiration_date).
- Hierarchy validation (item_hierarchy_id in [1,2]).
- Upgrade/renewal constraints on vault/platform/retention changes.
- Storage-seat compatibility checks against configured storage options.
- Vault validation against configured vaults for product/category.
- Module-level validation mirrors many of the above rules for each module.
- Disallow dependent line updates and enterprise quantity update constraints (relevant for update path, but keep for parity in shared validator layer).

### Order save-time behaviors that affect validation/business outcome

- user_ip is always set server-side (get_user_ip()).
- Item bundle assignment logic groups keycode items and assigns bundle ids.
- Discount handling depends on allow_discount (not passed for this endpoint, default behavior applies).
- Negative response codes from stored procedures are surfaced as validation-style failures.

## Response Semantics

- Success: HTTP 201 + hydrated cart payload.
- Validation/business failure: HTTP 422 + structured error payload.
- Unauthorized: HTTP 401.
- Forbidden: HTTP 403.
- Bad request (CSRF): HTTP 400.

## C# Port Blueprint (Recommended)

Mirror behavior as:

1. POST /cart/cart-orders endpoint in ASP.NET Core.
2. Middleware/pipeline:
- auth/session identity,
- CSRF/header validation,
- permission policy check.
3. Service layer (CartOrderCreateService):
- augment payload with account + locale context,
- validate order + items (FluentValidation or equivalent),
- execute usp_cart_insert_cart_order,
- execute usp_cart_insert_cart_order_item for each item,
- re-read cart aggregate for response.
4. Repository layer:
- Stored procedure wrappers with strict input/output contracts.
5. Contract tests:
- Golden tests comparing PHP vs C# responses for identical requests.

## Porting Notes (Risk Areas)

- Quote-key path can turn create into update behavior.
- Validation is distributed across controller filters, model validates, and model save filters.
- Response payload is not raw insert output; it is a refreshed, computed aggregate.
- Some behavior is conditional on session state (vendor_order_code) and locale route context.
