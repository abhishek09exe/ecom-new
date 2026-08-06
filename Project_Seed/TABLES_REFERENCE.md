# Stored Procedures - Tables Reference

## Overview
This document outlines all tables referenced across the cart-related stored procedures in the `ecommerce_vh14` database.

---

## 1. usp_cart_insert_cart_order
**Purpose**: Insert a new cart order  
**Database**: ecommerce_vh14

### Tables Referenced:
| Table Name | Operation | Purpose |
|---|---|---|
| `partner` | SELECT | Look up partner by partner_key |
| `currency` | SELECT | Look up currency by currency_code |
| `partner_configuration_partner` | SELECT | Get partner's default currency configuration |
| `cart_site_id_order_code_prefix` | SELECT | Get vendor order code prefix for site |
| `cart_order` | INSERT, SELECT | Main order record |
| `partner_account` | SELECT | Look up partner account for user |
| `account` | JOIN | Account information for partner account lookup |
| `cart_order_partner` | INSERT | Link order to partner |
| `cart_order_route` | INSERT | Add routing action for order |
| `license_key` | SELECT | Look up license_id by message_key |
| `cart_order_message` | INSERT | Store message/campaign info for order |
| `cart_json` | INSERT | Store extended order metadata as JSON |

### Key Columns:
- `cart_order.cart_order_id` (PRIMARY)
- `cart_order.vendor_order_code`
- `partner.partner_id`, `partner.partner_key`
- `currency.currency_id`, `currency.currency_code`

---

## 2. usp_cart_insert_cart_order_item
**Purpose**: Insert line items into a cart order  
**Database**: ecommerce_vh14

### Tables Referenced:
| Table Name | Operation | Purpose |
|---|---|---|
| `cart_order` | SELECT | Get order context (locale, currency) |
| `cart_order_item` | INSERT, SELECT, UPDATE | Main line item records |
| `product` | SELECT | Product details, licensing info |
| `license_category` | SELECT | Product licensing category |
| `product_years` | SELECT | Supported years for product |
| `product_seat` | SELECT | Seat counts for product |
| `product_type` | SELECT | Product type classification |
| `product_family` | SELECT | Product family info |
| `product_line_product` | SELECT | Product line associations |
| `product_line` | SELECT | Product line details |
| `currency` | SELECT | Currency for pricing |
| `license_attribute_license_value` | SELECT, INSERT | License attribute values |
| `cart_discount_method` | SELECT | Discount method type |
| `cart_order_item_json` | INSERT | Store item metadata as JSON |
| `cart_order_item_license` | INSERT | Link keycodes to items |
| `partner` | SELECT | Partner pricing lookups |
| `license_key` | SELECT, INSERT | License key generation |
| `product_pricing` | SELECT | Retail/partner pricing |
| `product_locale_pricing` | SELECT | Locale-specific pricing |
| `product_platform` | SELECT | Platform type (On-Prem, Cloud, etc.) |
| `retention_model` | SELECT | License retention models |
| `usage_pricing_model` | SELECT | Usage/overage pricing |

### Key Columns:
- `cart_order_item.cart_order_item_id` (PRIMARY)
- `cart_order_item.cart_order_id` (FOREIGN KEY → cart_order)
- `cart_order_item.product_id` (FOREIGN KEY → product)
- `cart_order_item.line_item`
- `cart_order_item.vendor_order_item_code`

---

## 3. usp_cart_select_cart_order
**Purpose**: Retrieve a complete cart order with all related data  
**Database**: ecommerce_vh14

### Tables Referenced:
| Table Name | Operation | Purpose |
|---|---|---|
| `cart_order` | SELECT | Main order record |
| `cart_order_partner` | LEFT JOIN | Partner association |
| `partner` | LEFT JOIN | Partner details |
| `currency` | LEFT JOIN | Currency info |
| `cart_json` | LEFT JOIN | Extended JSON metadata |

### Key Columns:
- `cart_order.cart_order_id`
- `cart_order.vendor_order_code` (PARAMETER)
- `cart_order.currency_id`

---

## 4. usp_cart_select_cart_order_item
**Purpose**: Retrieve line items for a cart order with full product details and pricing  
**Database**: ecommerce_vh14

### Tables Referenced:
| Table Name | Operation | Purpose |
|---|---|---|
| `cart_order` | INNER JOIN | Get cart context and locale |
| `cart_order_item` | INNER JOIN | Line item records |
| `product` | INNER JOIN | Product details |
| `product_family` | INNER JOIN | Product family |
| `product_line_product` | INNER JOIN | Product line mapping |
| `product_line` | INNER JOIN | Product line info |
| `product_type` | INNER JOIN | Product type |
| `cart_order_item_json` | LEFT JOIN | Extended item metadata |
| `product_license_category` | LEFT JOIN | Product's license categories |
| `license_category` | LEFT JOIN | License category details |
| `license_keycode_type` | LEFT JOIN | Keycode type info |
| `product_years` | LEFT JOIN | Supported years |
| `product_seat` | LEFT JOIN | Seat count |
| `license_attribute_license_value` | LEFT JOIN | License attribute values |
| `cart_order_item_license` | LEFT JOIN | Keycodes for item |
| `product_pricing` | LEFT JOIN | Pricing by locale/language |
| `product_platform` | LEFT JOIN | Platform type |

### Key Columns:
- `cart_order_item.cart_order_item_id`
- `cart_order.vendor_order_code` (PARAMETER)
- `cart_order_item.product_id`
- `cart_order_item.line_item`

---

## 5. usp_cart_insert_cart_order_ef_core
**Purpose**: EF Core/C# equivalent of usp_cart_insert_cart_order  
**Database**: ecommerce_vh14

### Entity Classes (Tables) Referenced:
| Entity | Operation | Purpose |
|---|---|---|
| `CartOrder` | INSERT | Create new order |
| `Partner` | SELECT | Lookup by partner_key |
| `Currency` | SELECT | Lookup by currency code |
| `PartnerConfigurationPartner` | SELECT | Get partner config |
| `CartSiteIdOrderCodePrefix` | SELECT | Get order code prefix |
| `PartnerAccount` | SELECT | Partner account lookup |
| `Account` | SELECT | Account info |
| `CartOrderPartner` | INSERT | Link to partner |
| `CartOrderRoute` | INSERT | Add routing |
| `LicenseKey` | SELECT | Lookup license |
| `CartOrderMessage` | INSERT | Store message/campaign |
| `CartJson` | INSERT | Store JSON metadata |

---

## Complete List of All Unique Tables

### Core Transaction Tables:
- `cart_order` - Main cart/order header
- `cart_order_item` - Cart line items
- `cart_order_partner` - Partner association
- `cart_order_message` - Message/campaign tracking
- `cart_order_route` - Order routing info
- `cart_order_item_license` - Item keycode mapping
- `cart_order_item_json` - Item extended metadata
- `cart_json` - Order extended metadata

### Product & Catalog:
- `product` - Product master
- `product_type` - Product type classification
- `product_family` - Product family grouping
- `product_line` - Product line
- `product_line_product` - Product line membership
- `product_years` - Years supported by product
- `product_seat` - Seat counts for product
- `product_license_category` - Product's license categories
- `product_platform` - Platform type (On-Prem, Cloud)
- `product_pricing` - Pricing by locale

### Licensing:
- `license_category` - License category (e.g., Product, Utility)
- `license_keycode_type` - Keycode type
- `license_key` - License keys
- `license_attribute_license_value` - License attributes
- `retention_model` - License retention models
- `usage_pricing_model` - Usage/overage pricing models

### Partner & Account:
- `partner` - Partner/reseller info
- `partner_account` - Partner account mapping
- `partner_configuration_partner` - Partner configuration
- `account` - Account information
- `cart_site_id_order_code_prefix` - Vendor order code prefixes

### Reference Data:
- `currency` - Currency master

---

## Table Dependencies / Relationship Diagram

```
cart_order (root)
├── cart_order_partner ──> partner
│   ├── partner_configuration_partner ──> currency
│   └── partner_account ──> account
├── cart_order_item (many)
│   ├── product ──> product_family
│   │   ├── product_line_product ──> product_line
│   │   ├── product_type
│   │   ├── product_years
│   │   ├── product_seat
│   │   ├── product_license_category ──> license_category
│   │   ├── product_platform
│   │   └── product_pricing (by locale)
│   ├── cart_order_item_license ──> license_key
│   ├── cart_order_item_json
│   └── license_attribute_license_value
├── cart_order_route
├── cart_order_message ──> license_key
├── cart_order_partner
└── cart_json
```

---

## CREATE TABLE Scripts (Template)

Below are template CREATE TABLE statements for all referenced tables. **Adjust data types and constraints based on your actual schema**:

### 1. cart_order
```sql
CREATE TABLE cart_order (
    cart_order_id INT PRIMARY KEY IDENTITY(1,1),
    vendor_order_code VARCHAR(100) NOT NULL UNIQUE,
    order_type VARCHAR(50),
    site_id VARCHAR(65),
    site_url VARCHAR(255),
    sales_order_date DATE,
    submission_date DATETIME,
    locale CHAR(5),
    user_ip VARCHAR(16),
    currency_id TINYINT FOREIGN KEY REFERENCES currency(currency_id),
    offer_amount MONEY DEFAULT 0,
    total_amount MONEY DEFAULT 0,
    sub_total_amount MONEY DEFAULT 0,
    tax_amount MONEY DEFAULT 0,
    cart_order_status_id TINYINT DEFAULT 1,
    insert_date DATETIME DEFAULT GETDATE(),
    insert_by VARCHAR(20),
    modified_date DATETIME,
    modified_by VARCHAR(20)
);
```

### 2. cart_order_item
```sql
CREATE TABLE cart_order_item (
    cart_order_item_id INT PRIMARY KEY IDENTITY(1,1),
    cart_order_id INT NOT NULL FOREIGN KEY REFERENCES cart_order(cart_order_id),
    product_id INT NOT NULL FOREIGN KEY REFERENCES product(product_id),
    line_item INT,
    quantity INT DEFAULT 1,
    vendor_order_item_code VARCHAR(36),
    cart_item_bundle_id INT,
    item_hierarchy_id TINYINT,
    list_price MONEY DEFAULT 0,
    unit_price MONEY DEFAULT 0,
    unit_price_pre_vat MONEY DEFAULT 0,
    tax_item_total MONEY DEFAULT 0,
    usage_price MONEY DEFAULT 0,
    order_item_offer_amount MONEY DEFAULT 0,
    discount FLOAT DEFAULT 0,
    cart_discount_method_id TINYINT,
    cart_discount_id INT,
    start_date DATETIME,
    expiration_date DATETIME,
    order_item_update_type_id TINYINT,
    license_attribute_license_value INT,
    opportunity_line_item_id VARCHAR(18),
    vault_id INT,
    storage_gb INT,
    product_locale VARCHAR(5),
    retention_model_id TINYINT,
    retention_term TINYINT,
    usage_pricing_model_id TINYINT,
    product_platform_id TINYINT,
    product_pricing_level_id TINYINT,
    sap_material_number INT,
    insert_date DATETIME DEFAULT GETDATE(),
    modified_date DATETIME,
    INDEX idx_cart_order_id (cart_order_id),
    INDEX idx_product_id (product_id)
);
```

### 3. cart_order_partner
```sql
CREATE TABLE cart_order_partner (
    cart_order_partner_id INT PRIMARY KEY IDENTITY(1,1),
    cart_order_id INT NOT NULL FOREIGN KEY REFERENCES cart_order(cart_order_id),
    partner_id INT NOT NULL FOREIGN KEY REFERENCES partner(partner_id),
    partner_account_id INT FOREIGN KEY REFERENCES partner_account(partner_account_id),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 4. cart_order_message
```sql
CREATE TABLE cart_order_message (
    cart_order_message_id INT PRIMARY KEY IDENTITY(1,1),
    cart_order_id INT NOT NULL FOREIGN KEY REFERENCES cart_order(cart_order_id),
    message_key VARCHAR(36),
    message_campaign_id INT,
    message_campaign_platform VARCHAR(50),
    cart_discount_id INT,
    license_id INT FOREIGN KEY REFERENCES license_key(license_id),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 5. cart_order_route
```sql
CREATE TABLE cart_order_route (
    cart_order_route_id INT PRIMARY KEY IDENTITY(1,1),
    cart_order_id INT NOT NULL FOREIGN KEY REFERENCES cart_order(cart_order_id),
    routing_action VARCHAR(50),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 6. cart_order_item_license
```sql
CREATE TABLE cart_order_item_license (
    cart_order_item_license_id INT PRIMARY KEY IDENTITY(1,1),
    cart_order_item_id INT NOT NULL FOREIGN KEY REFERENCES cart_order_item(cart_order_item_id),
    keycode VARCHAR(40),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 7. cart_order_item_json
```sql
CREATE TABLE cart_order_item_json (
    cart_order_item_json_id INT PRIMARY KEY IDENTITY(1,1),
    cart_order_item_id INT NOT NULL UNIQUE FOREIGN KEY REFERENCES cart_order_item(cart_order_item_id),
    cart_order_item_json NVARCHAR(MAX),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 8. cart_json
```sql
CREATE TABLE cart_json (
    cart_json_id INT PRIMARY KEY IDENTITY(1,1),
    cart_order_id INT NOT NULL FOREIGN KEY REFERENCES cart_order(cart_order_id),
    cart_json NVARCHAR(MAX),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 9. cart_site_id_order_code_prefix
```sql
CREATE TABLE cart_site_id_order_code_prefix (
    site_id VARCHAR(65) PRIMARY KEY,
    vendor_order_code_prefix VARCHAR(5)
);
```

### 10. product
```sql
CREATE TABLE product (
    product_id INT PRIMARY KEY IDENTITY(1,1),
    product_description NVARCHAR(255),
    product_family_id INT FOREIGN KEY REFERENCES product_family(product_family_id),
    product_type_id INT FOREIGN KEY REFERENCES product_type(product_type_id),
    license_keycode_type_id INT FOREIGN KEY REFERENCES license_keycode_type(license_keycode_type_id),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 11. product_type
```sql
CREATE TABLE product_type (
    product_type_id INT PRIMARY KEY IDENTITY(1,1),
    product_type_description VARCHAR(100),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 12. product_family
```sql
CREATE TABLE product_family (
    product_family_id INT PRIMARY KEY IDENTITY(1,1),
    product_family_description VARCHAR(100),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 13. product_line
```sql
CREATE TABLE product_line (
    product_line_id INT PRIMARY KEY IDENTITY(1,1),
    product_line_description VARCHAR(100),
    product_line_cart_type VARCHAR(50),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 14. product_line_product
```sql
CREATE TABLE product_line_product (
    product_line_product_id INT PRIMARY KEY IDENTITY(1,1),
    product_line_id INT FOREIGN KEY REFERENCES product_line(product_line_id),
    product_id INT FOREIGN KEY REFERENCES product(product_id),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 15. product_years
```sql
CREATE TABLE product_years (
    product_years_id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT FOREIGN KEY REFERENCES product(product_id),
    years DECIMAL(18,3),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 16. product_seat
```sql
CREATE TABLE product_seat (
    product_seat_id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT FOREIGN KEY REFERENCES product(product_id),
    seats INT,
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 17. license_category
```sql
CREATE TABLE license_category (
    license_category_id INT PRIMARY KEY IDENTITY(1,1),
    license_category_name VARCHAR(10),
    license_category_description VARCHAR(100),
    min_order_quantity INT DEFAULT 1,
    max_order_quantity INT,
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 18. product_license_category
```sql
CREATE TABLE product_license_category (
    product_license_category_id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT FOREIGN KEY REFERENCES product(product_id),
    license_category_id INT FOREIGN KEY REFERENCES license_category(license_category_id),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 19. license_keycode_type
```sql
CREATE TABLE license_keycode_type (
    license_keycode_type_id INT PRIMARY KEY IDENTITY(1,1),
    license_keycode_type_description VARCHAR(100),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 20. license_key
```sql
CREATE TABLE license_key (
    license_key_id INT PRIMARY KEY IDENTITY(1,1),
    license_id INT,
    license_key VARCHAR(100) UNIQUE,
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 21. license_attribute_license_value
```sql
CREATE TABLE license_attribute_license_value (
    license_attribute_license_value INT PRIMARY KEY IDENTITY(1,1),
    license_attribute_license_value_description VARCHAR(100),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 22. partner
```sql
CREATE TABLE partner (
    partner_id INT PRIMARY KEY IDENTITY(1,1),
    partner_key VARCHAR(36) UNIQUE,
    partner_name VARCHAR(100),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 23. partner_account
```sql
CREATE TABLE partner_account (
    partner_account_id INT PRIMARY KEY IDENTITY(1,1),
    partner_id INT FOREIGN KEY REFERENCES partner(partner_id),
    account_id INT FOREIGN KEY REFERENCES account(account_id),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 24. account
```sql
CREATE TABLE account (
    account_id INT PRIMARY KEY IDENTITY(1,1),
    account_user_name VARCHAR(100) UNIQUE,
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 25. partner_configuration_partner
```sql
CREATE TABLE partner_configuration_partner (
    partner_configuration_partner_id INT PRIMARY KEY IDENTITY(1,1),
    partner_id INT FOREIGN KEY REFERENCES partner(partner_id),
    partner_configuration_id INT,
    configuration_value VARCHAR(255),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 26. currency
```sql
CREATE TABLE currency (
    currency_id TINYINT PRIMARY KEY IDENTITY(1,1),
    currency_code VARCHAR(3) UNIQUE,
    currency_description VARCHAR(100),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 27. product_platform
```sql
CREATE TABLE product_platform (
    product_platform_id TINYINT PRIMARY KEY IDENTITY(1,1),
    product_platform_name VARCHAR(50),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 28. retention_model
```sql
CREATE TABLE retention_model (
    retention_model_id TINYINT PRIMARY KEY IDENTITY(1,1),
    retention_model_name VARCHAR(50),
    retention_model_type_id TINYINT,
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 29. usage_pricing_model
```sql
CREATE TABLE usage_pricing_model (
    usage_pricing_model_id TINYINT PRIMARY KEY IDENTITY(1,1),
    usage_pricing_model_name VARCHAR(50),
    insert_date DATETIME DEFAULT GETDATE()
);
```

### 30. product_pricing
```sql
CREATE TABLE product_pricing (
    product_pricing_id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT FOREIGN KEY REFERENCES product(product_id),
    language_code VARCHAR(2),
    location_code VARCHAR(3),
    retail_price MONEY,
    insert_date DATETIME DEFAULT GETDATE(),
    INDEX idx_product_locale (product_id, language_code, location_code)
);
```

---

## Summary

**Total Tables: 30**

### Transaction Tables: 8
- cart_order
- cart_order_item
- cart_order_partner
- cart_order_message
- cart_order_route
- cart_order_item_license
- cart_order_item_json
- cart_json

### Master/Reference Tables: 22
- Product/Catalog: 8 tables
- Licensing: 6 tables
- Partner/Account: 5 tables
- Configuration: 3 tables

---

## Notes

- All CREATE TABLE scripts are **templates** and should be validated against your actual schema
- Ensure all FOREIGN KEY relationships are properly defined
- Add appropriate indexes on frequently queried columns (cart_order_id, product_id, vendor_order_code)
- Consider adding CHECK constraints for status fields (cart_order_status_id, order_item_update_type_id)
- Add UNIQUE constraints on natural keys (vendor_order_code, partner_key, etc.)
