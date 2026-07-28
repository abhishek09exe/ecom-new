# Database Model Layer Deep Dive

## Connection String Configuration

### Location: `etc/connections.dev.json`

The database connection is defined as:

```json
"webroot_primary": {
    "type": "SqlServer",
    "sources": "ecommerce",
    "dsn": "sqlsrv:Server=qadenecom6.services.webroot;Database=ecommerce_vh14;Encrypt=YES;TrustServerCertificate=YES;MultipleActiveResultSets=TRUE",
    "login": "ecomuser",
    "password": "k74*8lp!"
}
```

**Connection Details:**
- **Type**: SqlServer (SQL Server database)
- **Server**: `qadenecom6.services.webroot`
- **Database**: `ecommerce_vh14`
- **User**: `ecomuser`
- **Password**: `k74*8lp!`
- **Protocol**: Uses PHP PDO with `sqlsrv:` DSN
- **Encryption**: YES (TLS/SSL encrypted connection)
- **Multiple Active Result Sets**: TRUE (allows multiple concurrent queries)

---

## Lithium Framework ORM Architecture

The system uses **Lithium PHP Framework** with a **custom SQL Server adapter** that exclusively uses **stored procedures**.

### Framework Stack:
```
Lithium Core
    ↓
li3_wr (Webroot Core Library)
    ↓
li3_wr_api (Webroot API Extensions)
    ↓
partner_api (Current Application)
```

---

## Model Definition

### File: `li3_wr/models/cart_order/PartnerCartNewOption.php`

```php
class PartnerCartNewOption extends \wr\extensions\data\Model {
    protected $_meta = [
        'connection' => 'webroot_primary',  // Uses the connection defined above
        'source' => 'partner_cart_new_option',
        'key' => false
    ];

    protected static $_procedures = [
        'select' => 'wr\extensions\procedures\cart_order\select\PartnerOrderPageDetails'
    ];
}
```

**Key Points:**
- `connection`: Points to `webroot_primary` connection (SQL Server)
- `source`: References the stored procedure name
- `_procedures`: Maps 'select' operations to stored procedure class
- `key`: Set to false because stored procedures don't have traditional PKs

---

## Stored Procedure Mapping

### File: `li3_wr/extensions/procedures/cart_order/select/PartnerOrderPageDetails.php`

```php
class PartnerOrderPageDetails extends \wr\extensions\data\Procedure {
    protected $_meta = [
        'connection' => 'webroot_primary',
        'source' => 'usp_partner_cart_select_order_page_details'  // SQL Server stored proc
    ];

    protected $_schema = [
        'partner_key' => ['type' => 'string', 'length' => 36],
        'language_code' => ['type' => 'string', 'length' => 2, 'null' => true],
        'location_code' => ['type' => 'string', 'length' => 3, 'null' => true],
        'product_line_cart_type' => ['type' => 'string', 'length' => 20, 'null' => true],
        'site_id' => ['type' => 'string', 'null' => true],
        'license_category_id' => ['type' => 'integer', 'null' => true],
        'item_hierarchy_id' => ['type' => 'integer', 'null' => false]
    ];
}
```

**What This Does:**
- Maps to SQL Server stored procedure: `usp_partner_cart_select_order_page_details`
- Defines the input parameters (schema) that the stored proc expects
- Each call to `PartnerCartNewOption::find()` invokes this stored procedure with the provided conditions

---

## Data Flow: From API Call to Database Query

### Step 1: API Request

**File:** `apps/partner_api/controllers/CartLicenseOptionController.php`

```php
public function getAll() {
    // Controller receives HTTP request with conditions:
    // - message_key (license key)
    // - locale (language_code + location_code)
    // - site_id (MSP vs RESELLER)
    
    $primaryProduct = PartnerCart::find('all', [
        'conditions' => [
            'license_category_id' => $license->license_category_id,
            'item_hierarchy_id' => 1,
            'site_id' => $site_id,
            'partner_key' => $partner->partner_key,
            'language_code' => $language_code,
            'location_code' => $location_code,
        ]
    ]);
}
```

### Step 2: Lithium Model Filter (Config Hook)

**File:** `li3_wr/models/cart_order/PartnerCartNewOption.php`

```php
public static function config(array $options = []) {
    static::applyFilter('find', function ($self, $params, $chain) {
        // INTERCEPT 1: Parse locale into language_code + location_code
        if (isset($conds['locale'])) {
            $locale = $conds['locale'];
            $lang = Locale::language($locale);     // Extract "en" from "en_US"
            $loc = Locale::territory($locale);     // Extract "US" from "en_US"
            
            $countries = Country::find('all');
            $country = $countries->first(fn => $loc === $el->iso);
            
            $conds['language_code'] = $lang;                   // "en"
            $conds['location_code'] = $country->iso3;         // "USA"
        }

        // INTERCEPT 2: Auto-populate site_id from partner
        $site_id = PartnerCartNewOption::getPartnerSite($params);
        if($site_id) {
            $params['options']['conditions']['site_id'] = $site_id;
        }

        // CALL STORED PROCEDURE
        $find_results = $chain->next($self, $params, $chain);

        // POST-PROCESS: Decode JSON fields, build select menus
        PartnerCartNewOption::decodeFields($find_results, $site_id);
        $find_results->sync();

        return $find_results;
    });
}
```

**What Happens:**
1. The `find()` call triggers the registered filter
2. Filter normalizes the input conditions (locale → language_code + location_code)
3. Filter calls `$chain->next()` which executes the stored procedure
4. Results are decoded (JSON fields → objects)
5. Select menus are generated from JSON arrays

### Step 3: Stored Procedure Execution

**SQL Server:**

```sql
EXECUTE usp_partner_cart_select_order_page_details
    @partner_key = '12345-67890-abcde-fghij',
    @language_code = 'en',
    @location_code = 'USA',
    @product_line_cart_type = 'cart_renewal_option',
    @site_id = 'MSP',
    @license_category_id = 5,
    @item_hierarchy_id = 1
```

**Stored Procedure Returns:**
- Rows with columns:
  - `vendor_order_code`: Product ID
  - `product_name`: Display name
  - `years_list`: JSON array of year options
  - `seat_list`: Comma-delimited seat options
  - `pricing_level_list`: JSON pricing tiers
  - `storage_list`: JSON storage options
  - `retention_model`: JSON backup retention models
  - `vault`: JSON data center options
  - etc.

### Step 4: Lithium ORM Converts DB Rows → PHP Objects

```php
// Raw SQL result:
// [
//   {
//     vendor_order_code: 'WSAV-001',
//     product_name: 'Antivirus',
//     years_list: '["1","3","5"]',
//     ...
//   }
// ]

// Lithium converts to:
$productObject = (object) [
    'vendor_order_code' => 'WSAV-001',
    'product_name' => 'Antivirus',
    'years_list' => ["1", "3", "5"],  // JSON decoded
    ...
]
```

### Step 5: Post-Processing in Model Filter

```php
PartnerCartNewOption::decodeFields($products, $site_id) {
    foreach ($products as $product) {
        // Decode JSON fields
        foreach (['years_list', 'seat_list', 'pricing_level_list', ...] as $key) {
            if(is_string($product->{$key})) {
                $product->{$key} = json_decode($product->{$key});
            }
        }

        // Build select menus for UI dropdowns
        if(!empty($product->years_list)) {
            $product->years_select = PartnerCartNewOption::getSelect(
                $product->years_list,
                'years',
                'years_description'
            );
        }
        
        // Result: $product->years_select = 
        // [
        //   {'years': '1', 'years_description': '1 Year'},
        //   {'years': '3', 'years_description': '3 Years'},
        //   {'years': '5', 'years_description': '5 Years'}
        // ]
    }
}
```

---

## Complete Request-to-Response Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. BROWSER: AJAX Call                                          │
│    GET /license-options?locale=en_US&message_key=ABC123        │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. PARTNER API: Route Handler                                   │
│    apps/partner_api/config/routes.php                          │
│    GET /configure → CartLicenseOptionController::getAll()      │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. CONTROLLER: Prepare Conditions                              │
│    CartLicenseOptionController::getAll()                       │
│    - Authenticate partner                                      │
│    - Extract message_key                                       │
│    - Build $primaryConditions array                            │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. LITHIUM ORM: Model Call                                      │
│    $primaryProduct = PartnerCart::find('all', [               │
│        'conditions' => $primaryConditions                      │
│    ]);                                                          │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. MODEL FILTER: Pre-Processing                                │
│    PartnerCartNewOption::config() filter triggered            │
│    - Parse locale (en_US → en + USA)                          │
│    - Auto-populate site_id from partner                       │
│    - Normalize conditions                                      │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. LITHIUM ADAPTER: SQL Generation                             │
│    li3_wr/extensions/data/source/SqlServer.php                 │
│    - Reads $_meta['source'] = 'usp_partner_cart_...'         │
│    - Reads $_procedures['select'] = PartnerOrderPageDetails  │
│    - Maps conditions to stored procedure params               │
│    - Builds: EXECUTE usp_partner_cart_select_order_page...   │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 7. DATABASE: Connection Pool                                   │
│    etc/connections.dev.json → webroot_primary                 │
│    - Host: qadenecom6.services.webroot                        │
│    - Database: ecommerce_vh14                                 │
│    - User: ecomuser (via PDO)                                 │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 8. SQL SERVER: Stored Procedure Execution                      │
│    EXECUTE usp_partner_cart_select_order_page_details         │
│        @partner_key = '...'                                    │
│        @language_code = 'en'                                   │
│        @location_code = 'USA'                                  │
│        @site_id = 'MSP'                                        │
│        @license_category_id = 5                                │
│        @item_hierarchy_id = 1                                  │
│                                                                 │
│    Returns: Rows with product configs + pricing               │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 9. LITHIUM ORM: Row → Object Hydration                          │
│    Each SQL row converted to:                                  │
│    stdClass {                                                  │
│        vendor_order_code: 'WSAV-001'                           │
│        product_name: 'Antivirus'                               │
│        years_list: (raw JSON string)                           │
│        seat_list: (raw comma-delimited string)                 │
│        ...                                                      │
│    }                                                            │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 10. MODEL FILTER: Post-Processing                              │
│    PartnerCartNewOption::decodeFields($products, $site_id)    │
│    - JSON decode: years_list → array                          │
│    - Build select menus: years_select → option objects        │
│    - Call: $provider->checkStorageDisplay(...)                │
│    - Sync results with updated data                           │
│                                                                 │
│    Result: Fully processed product objects                    │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 11. SECONDARY PRODUCTS: Find Related Items                     │
│    For each primary product, call:                             │
│    $secondaryProducts = PartnerCart::getSecondary(             │
│        $secondaryConditions                                    │
│    );                                                           │
│    (Repeats steps 5-10 for secondary products)                │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 12. CONTROLLER: Format Response                                │
│    CartLicenseOptionController::getAll()                      │
│    $data = compact('site_id', 'locale', 'locales', 'sites',  │
│                    'billing_model_tooltip', 'order_options',  │
│                    'license', 'profile', 'monthly_lalvs');    │
│                                                                 │
│    $this->_render['data'] = $data;                            │
│    return $data;                                               │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 13. RESPONSE: JSON to Browser                                   │
│    HTTP 200 OK                                                 │
│    Content-Type: application/json                              │
│    {                                                            │
│        "site_id": "MSP",                                       │
│        "locale": "en_US",                                      │
│        "license": {...},                                       │
│        "profile": {...},                                       │
│        "order_options": [                                      │
│            {                                                   │
│                "vendor_order_code": "WSAV-001",              │
│                "product_name": "Antivirus",                  │
│                "years_select": [{...}, {...}],              │
│                "seat_list": [1, 5, 10, 25],                │
│                "secondary_product_data": {...},             │
│                ...                                           │
│            }                                                 │
│        ]                                                       │
│    }                                                            │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│ 14. BROWSER: JavaScript Processing                             │
│    view-form.js receives JSON                                  │
│    licenseProfile = $.extend({}, licenseProfile, licenseData) │
│    setupDefaultCarts()                                         │
│    revealUpgradeOptions()                                      │
│    activateTabs()                                              │
│    renderProductTabs()                                         │
└─────────────────────────────────────────────────────────────────┘
```

---

## Key Technical Details

### 1. Connection Management

**Framework**: Lithium uses PDO for database abstraction

```php
// PDO Connection String
$dsn = "sqlsrv:Server=qadenecom6.services.webroot;Database=ecommerce_vh14;Encrypt=YES;TrustServerCertificate=YES;MultipleActiveResultSets=TRUE";

$pdo = new PDO($dsn, 'ecomuser', 'k74*8lp!');
```

**Pool Behavior:**
- Connections are pooled (reused across requests)
- Multiple active result sets allowed (can have multiple queries running)
- SSL/TLS encryption enabled for security
- Certificate verification disabled in dev (TrustServerCertificate=YES)

### 2. Stored Procedure Advantages

This architecture uses **stored procedures exclusively** instead of traditional SQL queries:

✅ **Benefits:**
- Security: SQL injection impossible (params passed separately)
- Performance: Compiled logic on database server
- Reusability: Shared across multiple APIs/applications
- Complex Logic: Business rules live in database, not app code

❌ **Tradeoffs:**
- Less flexibility (changes require DB deployment)
- Harder to version control (stored procs in DB, not git)
- Database-specific (SQL Server syntax, can't easily migrate DB)

### 3. Type Mapping

**SQL Server Types → PHP Types:**

```
NVARCHAR(MAX)  → string
INT            → integer
FLOAT          → float
DATETIME       → DateTime object
BIT            → boolean
```

### 4. JSON Handling

**Flow:**
```
SQL Server (JSON) → PDO (string) → json_decode() (PHP array/object) → JS (JSON)
```

Example:
```sql
-- SQL Server returns:
years_list: N'[{"years":"1","years_description":"1 Year"},{"years":"3"...}]'

-- PHP decodes to:
$product->years_list = [
    (object)['years' => '1', 'years_description' => '1 Year'],
    (object)['years' => '3', 'years_description' => '3 Years'],
    ...
]

-- Re-encodes to JSON for JavaScript:
{
    "years_list": [
        {"years":"1","years_description":"1 Year"},
        {"years":"3","years_description":"3 Years"},
        ...
    ]
}
```

### 5. Error Handling

```php
// In CartLicenseOptionController::getAll()
if(!$provider) {
    $this->_setError('Unauthorized');
    return false;
}

if(!$license) {
    $this->_setError('NotFound');
    return false;
}

// HTTP responses:
// 401 Unauthorized - No valid partner account
// 404 Not Found - License doesn't exist
// 200 OK - Success with JSON data
```

---

## Configuration Files Summary

| File | Purpose | Key Setting |
|------|---------|-------------|
| `etc/connections.dev.json` | Database credentials | `webroot_primary` connection string |
| `apps/partner_api/config/bootstrap/libraries.php` | Load Lithium + extensions | Registers `li3_wr`, `li3_wr_api` libraries |
| `li3_wr/models/cart_order/PartnerCartNewOption.php` | ORM Model | Maps to `webroot_primary` connection |
| `li3_wr/extensions/procedures/cart_order/select/PartnerOrderPageDetails.php` | Procedure definition | Maps to `usp_partner_cart_select_order_page_details` stored proc |
| `apps/partner_api/config/routes.php` | HTTP routing | `/configure` → `CartLicenseOptionController::getAll()` |

---

## Performance Considerations

### Query Optimization:
1. **Stored Procedure**: Pre-compiled, optimized by SQL Server
2. **Result Set**: Only returns needed rows (filtered in DB, not app)
3. **Pagination**: Not needed (result set is typically small)
4. **Caching**: Could cache results in Redis/Memcached (currently not implemented)

### Database Indexes:
Stored procedure likely uses indexes on:
- `partner_key` (quick partner lookup)
- `language_code` + `location_code` (localization)
- `license_category_id` (product type filtering)
- `item_hierarchy_id` (primary vs secondary products)

---

## Troubleshooting Connection Issues

**Common Error Scenarios:**

```
Error: SQLSTATE[42S02]: Base table or view not found
Solution: Check stored procedure name spelling and parameters

Error: SQLSTATE[HY000]: General error: 208
Solution: Check database user permissions (ecomuser might not have EXEC rights)

Error: Connection timeout
Solution: Verify server hostname (qadenecom6.services.webroot) is accessible

Error: SSL/TLS certificate error
Solution: TrustServerCertificate=YES allows self-signed certs in dev
```

---

## Next Steps for Deep Inspection

1. **SQL Server**: Connect directly and inspect `usp_partner_cart_select_order_page_details` stored procedure
2. **Database Schema**: Review `partner_cart_new_option` table/view definition
3. **Monitoring**: Enable query logging to see actual SQL Server calls being made
4. **Performance**: Profile stored proc execution time with SQL Server Management Studio

---

## CSI Endpoint Deep Dive: https://cartapi.webroot.com/cart/cart-orders (POST)

This section documents only the `/cart/cart-orders` create endpoint used by Cart API for CSI, including controller flow, DB operations, and validations needed for C# parity.

## Route And Runtime Wiring

- Route definition: `apps/csi/config/routes/cart.php`
  - `POST /cart/cart-orders` -> `AccountAwareRest::create`
  - Route model: `cart_order`
- Model binding: `apps/csi/webroot/index.php`
  - `cart_order` -> `wr\models\cart_order\CSIOrder`
- CSI controller inheritance chain:
  - `csi\controllers\AccountAwareRestController`
  - `csi\controllers\RestController`
  - `wr_api\controllers\RestController`

## Request Pipeline (Controller Layer)

1. Session + cart bootstrap (`apps/csi/config/bootstrap/session.php`)
    - Attempts to resolve existing cart from session key `vendor_order_code`.
    - Injects resolved cart object as dispatcher option `cartOrder`.
    - Registers a model `save` filter for `cart_order` to persist `vendor_order_code` into session after successful save.

2. CSRF validation (`apps/csi/config/bootstrap/session.php`)
    - For non-GET requests, validates request token from header `X-WRCART-CSRF` (read as `http:x_wrcart_csrf`) or request body token.
    - On failure returns `BadRequest` and sets/refreshes `csi_csrf` cookie.

3. Authentication + permission context (`apps/csi/config/bootstrap/auth.php`)
    - Resolves authenticated CSI user from `Auth::check('csi_user')`.
    - Loads permissions from account or resource table.
    - Injects `account` and `permissions` into controller options.

4. `AccountAwareRestController::create()`
    - Calls `checkAccess()`:
      - Fails with `Unauthorized` if no account.
      - Fails with `Forbidden` if permission check fails.
    - Applies create filter:
      - Merges account-derived fields into payload:
         - `username`, `account_user_name`, `csi_user_id`, `insert_by`, `p_rc`, `trx_rc`.

5. `csi\controllers\RestController` create filter
    - Adds locale from route context to payload: `locale`.

6. `wr_api\controllers\RestController::create()`
    - Creates empty `CSIOrder` entity.
    - Calls `$entity->save($data, $save_options)`.
    - On success: HTTP 201 and refreshed entity response.
    - On validation failure: HTTP 422 with aggregated validation errors.

## Model Behavior For This Endpoint

`CSIOrder` extends `Order` and reuses `Order::config()` and `Order::save()` behavior, while swapping related classes (`CSIItem`, `CSICustomer`, `CSICustomers`, `CSIItemBundle`).

### Core create path in `Order::save()`

1. If payload contains `key` and key resolves to quote key:
    - Reads message key (`MessageKey::find('first')`).
    - If quote references existing pending cart, updates existing cart with `UPDATE_FIELDS` only and returns that cart (no new cart insert).

2. Validation phase (`$entity->validates(...)`)
    - Runs base order validation + order-level filter validation + per-item validation.

3. Stored procedure execution for cart header create
    - Procedure class: `li3_wr/extensions/procedures/cart_order/insert/Order.php`
    - Procedure name: `usp_cart_insert_cart_order`
    - Inputs:
      - `site_id`, `locale`, `user_ip`, `cart_extension_json`
    - Outputs:
      - `response_code`, `message`

4. If cart header create succeeds and items were included in request:
    - Each item is saved via `CSIItem->save(validate=false)`.
    - Item procedure:
      - `usp_cart_insert_cart_order_item`
      - Inputs: `vendor_order_code`, `item_json`, `bundle_json`
      - Outputs: `response_code`, `message`

5. Conditional customer auto-association
    - If inferred customer id is present and autosave allowed, loads customer and writes billing/shipping rows for the new order.

6. Rest layer refresh after create
    - Re-reads created cart by key (`vendor_order_code`) for response payload.
    - This read hydrates extension fields, items, customers, route, and formatting fields.

## DB Operations Observed (Create Endpoint)

The endpoint can trigger these DB operations in normal or conditional flows:

1. `usp_cart_insert_cart_order`
    - Insert cart header.

2. `usp_cart_insert_cart_order_item` (0..N)
    - Insert each item/bundle payload.

3. `usp_cart_select_cart_order`
    - Read cart by `vendor_order_code` for API response refresh.

4. Item reads during refresh (`CSIItemBundle`/`CSIItem` path)
    - Reads cart items to build bundles and computed product fields.

5. Customer reads/writes (conditional)
    - Reads customer by `customer_id` and updates/inserts cart customer rows.

6. Currency and route reads during refresh
    - Currency lookup by `currency_id`.
    - Route resolution from message/license routing logic.

7. Quote-key branch (conditional)
    - Message key lookup and cart lookup; may execute update path instead of insert.

## Validation Matrix To Mirror In C#

### Security/access validations

- Authenticated CSI user is required.
- Permission check is required for resource/action (`cart_order.create` or wildcard).
- CSRF token required for non-GET requests.

### Order-level validations (`Order`)

- `site_id` must be one of allowed values (includes `CSI`).
- `locale` required.
- `currency_code` must be valid ISO list if provided.
- `sales_order_date` date format validation if provided.
- `vendor_order_code` cannot be empty if provided.
- `message_campaign_id` must be positive integer if provided.
- `message_campaign_platform` non-empty if provided.
- `partner_key` must be UUID if provided.
- `account_user_name` non-empty if provided.
- `url_link` must be valid URL if provided.

### Item-level validations (`CSIItem` / `Item`)

- Category validation (`license_category_name` in allowed set).
- Numeric validations (`license_seats`, `quantity`, `cart_item_bundle_id`, etc.).
- `years` in allowed year set.
- Date validations (`start_date`, `expiration_date`, `vendor_expiration_date`).
- Hierarchy validation (`item_hierarchy_id` in [1,2]).
- Upgrade/renewal constraints on vault/platform/retention changes.
- Storage-seat compatibility checks against configured storage options.
- Vault validation against configured vaults for product/category.
- Module-level validation mirrors many of the above rules for each module.
- Disallow dependent line updates and enterprise quantity update constraints (relevant for update path, but keep for parity in shared validator layer).

### Order save-time behaviors that affect validation/business outcome

- `user_ip` is always set server-side (`get_user_ip()`).
- Item bundle assignment logic groups keycode items and assigns bundle ids.
- Discount handling depends on `allow_discount` (not passed for this endpoint, default behavior applies).
- Negative response codes from stored procedures are surfaced as validation-style failures.

## Response Semantics

- Success: HTTP 201 + hydrated cart payload.
- Validation/business failure: HTTP 422 + structured error payload.
- Unauthorized: HTTP 401.
- Forbidden: HTTP 403.
- Bad request (CSRF): HTTP 400.

## C# Port Blueprint (Recommended)

Mirror behavior as:

1. `POST /cart/cart-orders` endpoint in ASP.NET Core.
2. Middleware/pipeline:
    - auth/session identity,
    - CSRF/header validation,
    - permission policy check.
3. Service layer (`CartOrderCreateService`):
    - augment payload with account + locale context,
    - validate order + items (FluentValidation or equivalent),
    - execute `usp_cart_insert_cart_order`,
    - execute `usp_cart_insert_cart_order_item` for each item,
    - re-read cart aggregate for response.
4. Repository layer:
    - Stored procedure wrappers with strict input/output contracts.
5. Contract tests:
    - Golden tests comparing PHP vs C# responses for identical requests.

## Porting Notes (Risk Areas)

- Quote-key path can turn create into update behavior.
- Validation is distributed across controller filters, model `validates`, and model `save` filters.
- Response payload is not raw insert output; it is a refreshed, computed aggregate.
- Some behavior is conditional on session state (`vendor_order_code`) and locale route context.
