# Complete Flow & Files - Try/Buy/Upgrade Configurator

## Flow Overview

```
User enters keycode
    ↓
Form Console Keycode Block
    ↓
Session saved + Redirect to /cart/update?key=...&routing_action=configurator
    ↓
Try/Buy/Configurator Block loads
    ↓
JavaScript calls API: /license-options?message_key=...
    ↓
CartLicenseOptionController retrieves DB data
    ↓
JS renders TRIAL / RENEW / ADD SEATS tabs
    ↓
User selects products & quantities
    ↓
JS calls cartAPI.createCart() to submit order
```

---

## Step-by-Step Breakdown

### STEP 1: User Entry Point — Keycode Form

**Files:**
- `concrete5-cms/src/application/blocks/form_console_keycode/controller.php`
- `concrete5-cms/src/application/blocks/form_console_keycode/view.php`
- `concrete5-cms/src/application/blocks/form_console_keycode/view-form.min.js`

**What Happens:**
- User lands on CMS page with "Form Console Keycode" block
- Enters keycode + selects console
- POSTs to `Controller::action_submit_form()`
  ```php
  $session->set('WebrootGsmConfigurator-Key', $key);
  $session->set('WebrootGsmConfigurator-Console', $console[1]);
  $this->redirect("/{$region}/cart/update?key={$key}&routing_action=configurator");
  ```
- Key stored in PHP session
- Redirects to `/us/en/cart/update?key=...&routing_action=configurator`

---

### STEP 2: Configurator Block Initializes

**Files:**
- `concrete5-cms/src/application/blocks/form_gsm_try_buy_configurator/controller.php`
- `concrete5-cms/src/application/blocks/form_gsm_try_buy_configurator/view.php`

**What Happens:**
- CMS page renders with "Form GSM Try Buy Configurator" block
- `Controller::view()` reads key from session:
  ```php
  $viewVars['userLicenseKey'] = $session->get('WebrootGsmConfigurator-Key');
  ```
- Passes product display strings + key to view:
  ```php
  $this->set('userLicenseKey', $viewVars['userLicenseKey']);
  $this->set('contentJson', json_encode($content)); // product names, descriptions
  ```
- HTML rendered with block attributes:
  ```html
  <div class="block form_gsm_try_buy_configurator" 
       data-key="<?=$userLicenseKey?>" 
       data-content='<?=$contentJson?>'>
  ```

---

### STEP 3: JavaScript Loads & Fetches License Data

**Files:**
- `concrete5-cms/src/application/blocks/form_gsm_try_buy_configurator/view-form.js`
- `concrete5-cms/src/application/blocks/form_gsm_try_buy_configurator/view-form.min.js`

**What Happens:**
- `initialize()` function runs:
  ```js
  messageKey = window.getQueryVariable('key'); // or $block.data('key')
  var jqxhr = getLicenseData();
  ```
- `getLicenseData()` makes AJAX call:
  ```js
  api = 'https://cartapi.webroot.com/license-options?locale=' + apiLocale + '&message_key=' + messageKey;
  return $.ajax(api, {type: 'GET', dataType: 'json', xhrFields: {withCredentials: true}});
  ```
- Response callback:
  ```js
  jqxhr.done(function (licenseData) {
    licenseProfile = $.extend({}, licenseProfile, licenseData);
    setupDefaultCarts();
    revealUpgradeOptions();
    activateTabs();
  });
  ```

---

### STEP 4: Backend API Call — Cart License Options

**Files:**
- `apps/partner_api/config/routes.php` (lines 297, 306)
- `apps/partner_api/controllers/CartLicenseOptionController.php`

**Route Mappings:**
```php
// Line 297
Router::connect('/configure', [
    'CartLicenseOption::getAll',
    'model' => 'cart_renewal_option',
    'http:method' => 'GET'
]);

// Line 306
Router::connect('/upgrade', [
    'CartLicenseOption::getAll',
    'model' => 'cart_upgrade_option',
    'http:method' => 'GET'
]);
```

**What Happens in `CartLicenseOptionController::getAll()`:**
1. Checks if partner account exists
2. Determines action type (renew vs upgrade from URL model parameter)
3. Calls parent `getAll()` to fetch license data
4. **PRIMARY DB CALL:**
   ```php
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
   ```
5. For each primary product, finds secondary products:
   ```php
   $secondaryProducts = PartnerCart::getSecondary($secondaryConditions);
   ```
6. Attaches license profile data to products:
   ```php
   $order_option->profile = $profile[$order_option->license_category_name] ?? [];
   ```
7. For upgrades, trims storage options based on current seat count
8. Returns formatted data array:
   ```php
   return compact('site_id', 'locale', 'locales', 'sites', 'billing_model_tooltip', 
                  'order_options', 'license', 'profile', 'monthly_lalvs');
   ```

---

### STEP 5: JS Renders Tabs with License Data

**Files:**
- `concrete5-cms/src/application/blocks/form_gsm_try_buy_configurator/view-form.js`

**What Happens:**
- `licenseProfile` object now contains:
  - `license`: Current license info + expiration date
  - `license_profile`: Product profiles (trial/full, active dates, seats)
  - `upgrade_categories`: Available products to upgrade to
- JS logic determines which tabs to show:
  ```js
  licenseProfile.hasTrialProductActive('SDNS') ? showTrialTab()
  licenseProfile.hasFullProductActive('SDNS') ? showRenewalTab()
  licenseProfile.hasUpgradeAvailable('SECA') ? showUpgradeTab()
  ```
- Populates three templates:
  - **TRIAL tab**: New trial products available
  - **RENEW tab**: Existing full products to renew
  - **ADD SEATS tab**: Existing products to add seats/upgrade

---

### STEP 6: User Submits Order

**Files:**
- `concrete5-cms/src/application/blocks/form_gsm_try_buy_configurator/view-form.js` (lines 1654-1795 for createCart)

**What Happens:**
- User selects products, quantities, years
- Clicks "Add to Cart" button
- JS builds `postCartData` object with selected items:
  ```js
  var postCartData = {
      site_id: selectedSiteId,
      routing_action: 'configurator',
      endpointRenewal: cartRenewal.items, // or cartTrial.items or cartUpgrade.items
      cart_item_bundle_id: licenseProfile.license.cart_item_bundle_id
  };
  ```
- Calls `cartAPI.createCart(postCartData)`
- Response redirects to checkout or success page

---

## Database Model Layer

**Files:**
- `apps/partner_api/models/PartnerCartNewOption.php` (aliased as `PartnerCart` in controller)

**Key Methods Called:**
- `PartnerCart::find('all', ['conditions' => [...]])`
- `PartnerCart::getSecondary($conditions)`
- `PartnerCart::getBillingModelTooltip($products)`
- `PartnerCart::getFlags($product, $site_id)`
- `PartnerCart::isWebrootLikeProduct($product)`
- `PartnerCart::hasEdrMdrProducts($products)`
- `PartnerCart::checkStorageDisplay($product, $site_id, $locale)`

---

## Related Blocks

**Files:**
- `concrete5-cms/src/application/blocks/form_gsm_sidebar_cart/controller.php`
  - Displays keycode + products on right sidebar
  - Reads `WebrootGsmConfigurator-Console` from session

---

## Additional View: Purchase Options MSP

**Likely MSP Purchase Options views/files:**
- [ecomwebdev/concrete5-cms/src/application/blocks/purchase_options/view.php](ecomwebdev/concrete5-cms/src/application/blocks/purchase_options/view.php)
- [ecomwebdev/concrete5-cms/src/application/blocks/purchase_options/controller.php](ecomwebdev/concrete5-cms/src/application/blocks/purchase_options/controller.php)
- [ecomwebdev/apps/pages/views/purchase_option/index.html.php](ecomwebdev/apps/pages/views/purchase_option/index.html.php)

**Related API purchase-options view (includes site selection such as MSP):**
- [ecomwebdev/apps/partner_api/views/cart_new_option/get_all.html.php](ecomwebdev/apps/partner_api/views/cart_new_option/get_all.html.php)

---

## Apache Virtual Hosts

**Files:**
- `apache_vhosts/` directory contains various `.conf` files for different environments
- Relevant endpoints:
  - `cartapi.webroot.com` → API server for license-options
  - Main web server handles CMS pages

---

## Summary Table

| Stage | File(s) | Action |
|-------|---------|--------|
| **1. Entry** | form_console_keycode/* | User submits keycode → stores in session |
| **2. Initialization** | form_gsm_try_buy_configurator/controller.php | Block retrieves key from session, injects into HTML |
| **3. Frontend** | form_gsm_try_buy_configurator/view.php | Renders HTML with tabs template |
| **4. JS Startup** | form_gsm_try_buy_configurator/view-form.js | `initialize()` calls getLicenseData() |
| **5. API Call** | cartapi.webroot.com/license-options | Routes to CartLicenseOptionController |
| **6. Backend** | CartLicenseOptionController.php | DB queries via PartnerCart model |
| **7. Response** | cartapi.webroot.com/license-options | Returns license + products JSON |
| **8. Tab Render** | form_gsm_try_buy_configurator/view-form.js | Renders TRIAL/RENEW/UPGRADE tabs |
| **9. Submit** | form_gsm_try_buy_configurator/view-form.js | `cartAPI.createCart()` sends order |

---

## Key Session Variables

- `WebrootGsmConfigurator-Key`: User's license keycode
- `WebrootGsmConfigurator-Console`: Selected console/account type

---

## Important Constants & Configuration

**Product Categories (from view-form.js):**
- `SAEP`: Product with no trial allowed
- `SDNS`: DNS Protection (allows trial)
- `SECA`: Security Awareness (allows trial)
- `OTEDR`: OpenText EDR (allows trial)
- `OTMDR`: OpenText MDR (allows trial)
- `PLRM`: Pillr (allows trial)
- `PLRCS`: Pillr Consumer Suite (no trial)
- `PLRCB`: Pillr Commercial Business (allows trial)

**Billing Configuration:**
- `annual`: billing_code = 11
- `monthly`: billing_code = 12

**Item Hierarchy:**
- `1`: Primary product
- `2`: Secondary product

---

## Data Flow Visualization

```
┌─────────────────────────────────────────────────────────────────┐
│                    User Browser                                 │
│                                                                 │
│  1. form_console_keycode block → POST keycode                  │
│  2. Redirect to /cart/update?key=XXX&routing_action=configurator│
│  3. Load form_gsm_try_buy_configurator block                   │
│  4. JavaScript initialize() → AJAX GET /license-options        │
└──────────────┬──────────────────────────────────────────────────┘
               │
               │ HTTP GET: /license-options?locale=...&message_key=...
               ↓
┌──────────────────────────────────────────────────────────────────┐
│            Partner API (cartapi.webroot.com)                    │
│                                                                  │
│  CartLicenseOptionController::getAll()                          │
│  ├─ Authenticate via partner account provider                  │
│  ├─ Query: PartnerCart::find('all', [...conditions...])       │
│  ├─ Load primary products from database                        │
│  ├─ For each primary: PartnerCart::getSecondary(...)          │
│  ├─ Attach license profile (trial/full, dates, seats)         │
│  ├─ For upgrades: trim storage options                        │
│  └─ Return JSON with products + pricing                       │
└──────────────┬──────────────────────────────────────────────────┘
               │
               │ HTTP 200: JSON response with licenseProfile
               ↓
┌──────────────────────────────────────────────────────────────────┐
│                    User Browser (continued)                      │
│                                                                  │
│  5. JavaScript receives licenseProfile JSON                    │
│  6. JS determines which tabs to show:                          │
│     - TRIAL: if user has trial slots available                │
│     - RENEW: if user has full products to renew               │
│     - UPGRADE: if user has upgrade available                  │
│  7. Render tab templates with products + pricing              │
│  8. User selects products → clicks "Add to Cart"              │
│  9. JS calls cartAPI.createCart(postCartData)                 │
│  10. Redirect to checkout or confirmation page                │
└──────────────────────────────────────────────────────────────────┘
```

---

## Notes

- All API calls use credentials (xhrFields: {withCredentials: true})
- License key is passed through query string or session
- Product pricing is dynamically fetched from CartLicenseOptionController
- Storage configuration is trimmed to prevent exceeding maximum capacity
- EDR/MDR products support nested secondary products
- Different logic for trial vs full licenses
