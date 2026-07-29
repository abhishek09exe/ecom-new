-- =============================================================================
-- LOCAL DEV DATABASE SETUP  —  schema aligned to QA DB (ecommerce_VH14)
-- =============================================================================
-- Usage: Run once in SSMS connected to master.
-- =============================================================================

USE master;
GO

IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'ecom_cart_dev')
BEGIN
    ALTER DATABASE ecom_cart_dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ecom_cart_dev;
END
GO

CREATE DATABASE ecom_cart_dev;
GO

USE ecom_cart_dev;
GO

-- =============================================================================
-- REFERENCE TABLES
-- =============================================================================

CREATE TABLE dbo.currency (
    currency_id          TINYINT      NOT NULL PRIMARY KEY IDENTITY(1,1),
    currency_code        CHAR(3)      NULL,
    currency_description VARCHAR(20)  NOT NULL,
    symbol_html          VARCHAR(10)  NULL,
    symbol_utf8          NVARCHAR(10) NULL,
    symbol_text          VARCHAR(10)  NULL,
    exchange_rate        FLOAT        NULL,
    exchange_multiplier  FLOAT        NULL,
    dr_locale            VARCHAR(10)  NULL,
    active               TINYINT      NULL DEFAULT 0,
    last_modified_date   DATETIME     NULL DEFAULT GETDATE(),
    last_modified_by     VARCHAR(200) NULL DEFAULT SUSER_SNAME(),
    vat_rate             FLOAT        NULL
);

INSERT INTO dbo.currency (currency_code, currency_description, active)
VALUES ('USD', 'US Dollar', 1), ('EUR', 'Euro', 1), ('GBP', 'Brit Pound', 1);
GO

CREATE TABLE dbo.cart_order_status (
    cart_order_status_id          TINYINT      NOT NULL PRIMARY KEY IDENTITY(1,1),
    cart_order_status_description VARCHAR(50)  NOT NULL,
    insert_date                   DATETIME     NOT NULL DEFAULT GETDATE(),
    insert_by                     VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME()
);

INSERT INTO dbo.cart_order_status (cart_order_status_description)
VALUES ('pending'), ('complete'), ('cancelled'), ('quote');
GO

CREATE TABLE dbo.partner (
    partner_id          INT              NOT NULL PRIMARY KEY IDENTITY(1,1),
    partner_name        NVARCHAR(100)    NOT NULL,
    partner_type_id     TINYINT          NOT NULL DEFAULT 1,
    partner_status_id   TINYINT          NOT NULL DEFAULT 1,
    partner_key         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    parent_partner_id   INT              NULL,
    salesforce_id       VARCHAR(20)      NULL,
    oracle_id           VARCHAR(20)      NULL,
    last_modified_date  DATETIME         NOT NULL DEFAULT GETDATE(),
    last_modified_by    VARCHAR(200)     NOT NULL DEFAULT SUSER_SNAME(),
    account_owner_id    VARCHAR(18)      NULL
);

INSERT INTO dbo.partner (partner_name, partner_type_id, partner_status_id)
VALUES ('Dev Partner', 1, 1);
GO

CREATE TABLE dbo.product (
    product_id           INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    product_description  VARCHAR(100) NOT NULL,
    product_type_id      INT          NOT NULL DEFAULT 1,
    product_family_id    INT          NULL,
    product_lifecycle_id INT          NULL DEFAULT 1,
    license_keycode_type_id INT       NULL,
    root_product_id      INT          NULL,
    uses_keycode         INT          NOT NULL DEFAULT 0,
    cd_product_id        INT          NULL DEFAULT 0,
    retail_price         MONEY        NULL,
    pict                 VARCHAR(100) NULL DEFAULT '',
    basename             VARCHAR(32)  NULL,
    insert_date          DATETIME     NOT NULL DEFAULT GETDATE(),
    insert_by            VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME(),
    modified_date        DATETIME     NOT NULL DEFAULT GETDATE(),
    modified_by          VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME(),
    online_refund_flag   TINYINT      NULL DEFAULT 1
);

INSERT INTO dbo.product (product_description, product_type_id, uses_keycode)
VALUES ('Mock Product A', 1, 0), ('Mock Product B', 1, 0);
GO

CREATE TABLE dbo.license_category (
    license_category_id          INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    license_category_name        VARCHAR(50)  NULL,
    license_category_description VARCHAR(255) NULL,
    min_order_quantity           INT          NULL,
    max_order_quantity           INT          NULL
);

INSERT INTO dbo.license_category (license_category_name)
VALUES ('SAEP'), ('SAAP'), ('SASP'), ('SOHO'), ('SMB'), ('ENT');
GO

CREATE TABLE dbo.site (
    site_id VARCHAR(65) NOT NULL PRIMARY KEY
);

INSERT INTO dbo.site (site_id) VALUES ('gsm'), ('webroot'), ('ecm');
GO

-- =============================================================================
-- LOOKUP / CLASSIFICATION TABLES  (referenced by product & licensing SPs)
-- =============================================================================

-- license_keycode_type  (used by product and usp_cart_select_cart_order_item)
CREATE TABLE dbo.license_keycode_type (
    license_keycode_type_id          INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    license_keycode_type_description VARCHAR(50)  NOT NULL,
    insert_date                      DATETIME     NOT NULL DEFAULT GETDATE(),
    insert_by                        VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME(),
    modified_date                    DATETIME     NOT NULL DEFAULT GETDATE(),
    modified_by                      VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME()
);

INSERT INTO dbo.license_keycode_type (license_keycode_type_description)
VALUES ('Standard'), ('Volume'), ('OEM'), ('Trial');
GO

-- product_type  (used by usp_cart_insert_cart_order_item & usp_cart_select_cart_order_item)
CREATE TABLE dbo.product_type (
    product_type_id          INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    product_type_description VARCHAR(50)  NULL,
    insert_date              DATETIME     NOT NULL DEFAULT GETDATE(),
    insert_by                VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME(),
    modified_date            DATETIME     NOT NULL DEFAULT GETDATE(),
    modified_by              VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME()
);

INSERT INTO dbo.product_type (product_type_description)
VALUES ('Consumer'), ('Business'), ('Enterprise'), ('Trial');
GO

-- product_family  (used by usp_cart_insert_cart_order_item & usp_cart_select_cart_order_item)
CREATE TABLE dbo.product_family (
    product_family_id          INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    product_family_description VARCHAR(50)  NOT NULL,
    product_family_prefix      CHAR(2)      NULL,
    insert_date                DATETIME     NOT NULL DEFAULT GETDATE(),
    insert_by                  VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME(),
    modified_date              DATETIME     NOT NULL DEFAULT GETDATE(),
    modified_by                VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME()
);

INSERT INTO dbo.product_family (product_family_description, product_family_prefix)
VALUES ('Internet Security', 'IS'), ('Endpoint Protection', 'EP'), ('DNS Protection', 'DN');
GO

-- account  (used by usp_cart_insert_cart_order via partner_account JOIN)
CREATE TABLE dbo.account (
    account_id           INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    account_user_name    VARCHAR(100) NOT NULL,
    account_password     VARCHAR(64)  NOT NULL DEFAULT '',
    password_hint        VARCHAR(50)  NULL,
    account_type_id      TINYINT      NOT NULL DEFAULT 1,
    account_status_id    TINYINT      NOT NULL DEFAULT 1,
    parent_account_id    INT          NULL,
    insert_date          DATETIME     NOT NULL DEFAULT GETDATE(),
    insert_by            VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME(),
    modified_date        DATETIME     NOT NULL DEFAULT GETDATE(),
    modified_by          VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME(),
    WARN_opt_in          TINYINT      NOT NULL DEFAULT 0,
    email_opt_in         TINYINT      NULL,
    password_reset_date  DATETIME     NOT NULL DEFAULT GETDATE()
);

INSERT INTO dbo.account (account_user_name, account_type_id, account_status_id)
VALUES ('dev-account@test.com', 1, 1);
GO

-- partner_account  (used by usp_cart_insert_cart_order to look up account for partner)
CREATE TABLE dbo.partner_account (
    partner_account_id         INT              NOT NULL PRIMARY KEY IDENTITY(1,1),
    partner_id                 INT              NOT NULL,
    account_id                 INT              NOT NULL,
    partner_account_key        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    insert_by                  VARCHAR(200)     NOT NULL DEFAULT SUSER_SNAME(),
    shared_account             TINYINT          NOT NULL DEFAULT 0,
    partner_account_status_id  TINYINT          NOT NULL DEFAULT 1,

    CONSTRAINT FK_pa_partner FOREIGN KEY (partner_id) REFERENCES dbo.partner(partner_id),
    CONSTRAINT FK_pa_account FOREIGN KEY (account_id) REFERENCES dbo.account(account_id)
);

INSERT INTO dbo.partner_account (partner_id, account_id, shared_account, partner_account_status_id)
VALUES (1, 1, 0, 1);
GO

-- =============================================================================
-- PRODUCT CATALOG TABLES  (referenced by item insert / item select SPs)
-- =============================================================================

-- product_line  (used by usp_cart_insert_cart_order_item & usp_cart_select_cart_order_item)
CREATE TABLE dbo.product_line (
    product_line_id          INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    product_line_description VARCHAR(40)  NOT NULL,
    product_line_prefix      CHAR(2)      NOT NULL DEFAULT '',
    root_product_id          INT          NOT NULL DEFAULT 0,
    insert_date              DATETIME     NOT NULL DEFAULT GETDATE(),
    insert_by                VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME(),
    modified_date            DATETIME     NOT NULL DEFAULT GETDATE(),
    modified_by              VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME(),
    status                   TINYINT      NULL,
    product_line_cart_type   VARCHAR(20)  NULL
);

INSERT INTO dbo.product_line (product_line_description, product_line_prefix, root_product_id, product_line_cart_type)
VALUES ('Internet Security Line', 'IS', 1, 'NEW'), ('Endpoint Protection Line', 'EP', 2, 'NEW');
GO

-- product_line_product  (maps products to product lines)
CREATE TABLE dbo.product_line_product (
    product_line_product_id INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    product_line_id         INT NOT NULL,
    product_id              INT NOT NULL,

    CONSTRAINT FK_plp_product_line FOREIGN KEY (product_line_id) REFERENCES dbo.product_line(product_line_id),
    CONSTRAINT FK_plp_product      FOREIGN KEY (product_id)      REFERENCES dbo.product(product_id)
);

INSERT INTO dbo.product_line_product (product_line_id, product_id)
VALUES (1, 1), (2, 2);
GO

-- product_years  (used by usp_cart_insert_cart_order_item & usp_cart_select_cart_order_item)
CREATE TABLE dbo.product_years (
    product_years_id INT   NOT NULL PRIMARY KEY IDENTITY(1,1),
    product_id       INT   NOT NULL,
    years            FLOAT NOT NULL,
    upgrade_months   TINYINT NULL,
    upgrade_days     INT  NULL,

    CONSTRAINT FK_py_product FOREIGN KEY (product_id) REFERENCES dbo.product(product_id)
);

INSERT INTO dbo.product_years (product_id, years)
VALUES (1, 1.0), (1, 2.0), (2, 1.0), (2, 2.0);
GO

-- product_seat  (used by usp_cart_insert_cart_order_item & usp_cart_select_cart_order_item)
CREATE TABLE dbo.product_seat (
    product_seat_id INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    product_id      INT          NOT NULL,
    seats           INT          NOT NULL,
    insert_date     DATETIME     NOT NULL DEFAULT GETDATE(),
    insert_by       VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME(),
    modified_date   DATETIME     NOT NULL DEFAULT GETDATE(),
    modified_by     VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME(),
    current_seats   INT          NULL,

    CONSTRAINT FK_ps_product FOREIGN KEY (product_id) REFERENCES dbo.product(product_id)
);

INSERT INTO dbo.product_seat (product_id, seats, current_seats)
VALUES (1, 1, 1), (1, 5, 5), (2, 1, 1), (2, 5, 5);
GO

-- product_license_category  (used by usp_cart_select_cart_order_item LEFT JOIN)
CREATE TABLE dbo.product_license_category (
    product_license_category_id INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    product_id                  INT NOT NULL,
    license_category_id         INT NOT NULL,
    current_license_category_id INT NULL,

    CONSTRAINT FK_plc_product          FOREIGN KEY (product_id)         REFERENCES dbo.product(product_id),
    CONSTRAINT FK_plc_license_category FOREIGN KEY (license_category_id) REFERENCES dbo.license_category(license_category_id)
);

INSERT INTO dbo.product_license_category (product_id, license_category_id)
VALUES (1, 1), (2, 1);
GO

-- product_pricing  (used by usp_cart_select_cart_order_item LEFT JOIN; logic deferred)
CREATE TABLE dbo.product_pricing (
    product_pricing_id   INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    product_id           INT          NOT NULL,
    language_code        VARCHAR(2)   NOT NULL,
    location_code        VARCHAR(3)   NOT NULL,
    currency_id          INT          NOT NULL,
    retail_price         MONEY        NOT NULL DEFAULT 0,
    last_modified_date   DATETIME     NOT NULL DEFAULT GETDATE(),
    last_modified_by     VARCHAR(200) NOT NULL DEFAULT SUSER_SNAME(),
    edu_nfp_price        MONEY        NULL,
    govt_price           MONEY        NULL,
    usage_price          MONEY        NULL,

    CONSTRAINT FK_pp_product FOREIGN KEY (product_id) REFERENCES dbo.product(product_id)
    -- NOTE: pricing logic (locale-based lookups, partner pricing) is deferred to a later gap
);
GO

-- product_platform  (used by usp_cart_select_cart_order_item LEFT JOIN)
CREATE TABLE dbo.product_platform (
    product_platform_id   TINYINT      NOT NULL PRIMARY KEY IDENTITY(1,1),
    product_platform_name VARCHAR(50)  NOT NULL
);

INSERT INTO dbo.product_platform (product_platform_name)
VALUES ('On-Prem'), ('Cloud'), ('Hybrid');
GO

-- =============================================================================
-- LICENSING REFERENCE TABLES  (referenced by item insert / item select SPs)
-- =============================================================================

-- license_attribute_license_value  (used by usp_cart_insert_cart_order_item & usp_cart_select_cart_order_item)
CREATE TABLE dbo.license_attribute_license_value (
    license_attribute_license_value       INT         NOT NULL PRIMARY KEY,
    license_attribute_id                  INT         NOT NULL DEFAULT 0,
    license_attribute_license_value_description VARCHAR(50) NOT NULL,
    license_module_type_id                TINYINT     NULL,
    autobilling_enabled                   BIT         NULL
);
-- No seed rows needed; values come from the license configuration in the real DB
GO

-- retention_model  (used by usp_cart_insert_cart_order_item)
CREATE TABLE dbo.retention_model (
    retention_model_id      TINYINT      NOT NULL PRIMARY KEY IDENTITY(1,1),
    retention_model_name    VARCHAR(50)  NOT NULL,
    retention_model_type_id TINYINT      NULL
);

INSERT INTO dbo.retention_model (retention_model_name)
VALUES ('Standard'), ('Autorenewal'), ('Manual');
GO

-- usage_pricing_model  (used by usp_cart_insert_cart_order_item)
CREATE TABLE dbo.usage_pricing_model (
    usage_pricing_model_id   TINYINT      NOT NULL PRIMARY KEY IDENTITY(1,1),
    usage_pricing_model_name VARCHAR(50)  NOT NULL
);

INSERT INTO dbo.usage_pricing_model (usage_pricing_model_name)
VALUES ('Flat'), ('Per-Seat'), ('Per-GB');
GO

-- cart_discount_method  (referenced in cart_order_item.cart_discount_method_id)
CREATE TABLE dbo.cart_discount_method (
    cart_discount_method_id          TINYINT      NOT NULL PRIMARY KEY IDENTITY(1,1),
    cart_discount_method_description VARCHAR(50)  NOT NULL
);

INSERT INTO dbo.cart_discount_method (cart_discount_method_description)
VALUES ('None'), ('Percent'), ('Fixed'), ('Volume');
GO

-- =============================================================================
-- CORE ORDER TABLES
-- =============================================================================

CREATE TABLE dbo.cart_order (
    cart_order_id           INT          NOT NULL PRIMARY KEY IDENTITY(1000,1),
    cart_customer_id        INT          NOT NULL DEFAULT 0,
    invoice_in_process_id   INT          NOT NULL DEFAULT 0,
    vendor_order_code       VARCHAR(100) NULL UNIQUE,
    order_type              VARCHAR(30)  NOT NULL,
    site_id                 VARCHAR(65)  NOT NULL,
    site_url                VARCHAR(1025) NOT NULL,
    p_rc                    VARCHAR(50)  NOT NULL DEFAULT '1',
    p_rsc                   VARCHAR(50)  NULL,
    p_ac                    VARCHAR(100) NULL,
    trx_rc                  VARCHAR(50)  NULL,
    trx_rsc                 VARCHAR(50)  NULL,
    trx_ac                  VARCHAR(100) NULL,
    aid                     VARCHAR(50)  NULL,
    pid                     VARCHAR(50)  NULL,
    sid                     VARCHAR(100) NULL,
    offer_id                VARCHAR(65)  NULL,
    offer_amount            MONEY        NULL DEFAULT 0,
    total_amount            MONEY        NULL DEFAULT 0,
    sub_total_amount        MONEY        NOT NULL DEFAULT 0,
    tax_amount              MONEY        NULL DEFAULT 0,
    payment_method          VARCHAR(255) NOT NULL DEFAULT '',
    exchange_rate           MONEY        NULL,
    session_id              BIGINT       NOT NULL DEFAULT 0,
    submission_date         DATETIME     NOT NULL DEFAULT GETDATE(),
    sales_order_date        DATETIME     NULL,
    locale                  CHAR(5)      NOT NULL,
    subject                 VARCHAR(255) NULL,
    comment                 VARCHAR(8000) NULL,
    insert_date             DATETIME     NOT NULL DEFAULT GETDATE(),
    insert_by               VARCHAR(50)  NOT NULL DEFAULT SUSER_SNAME(),
    modified_date           DATETIME     NOT NULL DEFAULT GETDATE(),
    modified_by             VARCHAR(50)  NOT NULL DEFAULT SUSER_SNAME(),
    cart_order_status_id    TINYINT      NOT NULL DEFAULT 1,
    currency_id             TINYINT      NULL,
    customer_profile_token  VARCHAR(24)  NULL,
    cart_order_in_process_id INT         NULL,
    user_ip                 VARCHAR(16)  NULL,
    restriction             VARCHAR(20)  NULL,

    CONSTRAINT FK_cart_order_currency   FOREIGN KEY (currency_id)          REFERENCES dbo.currency(currency_id),
    CONSTRAINT FK_cart_order_status     FOREIGN KEY (cart_order_status_id)  REFERENCES dbo.cart_order_status(cart_order_status_id)
);
GO

CREATE TABLE dbo.cart_order_item (
    cart_order_item_id              INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    cart_order_id                   INT          NOT NULL,
    invoice_item_in_process_id      INT          NOT NULL DEFAULT 0,
    vendor_id                       INT          NOT NULL DEFAULT 1,
    line_item                       INT          NOT NULL,
    vendor_product_id               INT          NULL,
    quantity                        INT          NOT NULL,
    order_item_offer_code           VARCHAR(65)  NULL,
    order_item_offer_amount         MONEY        NULL,
    list_price                      MONEY        NOT NULL DEFAULT 0,
    unit_price                      MONEY        NOT NULL DEFAULT 0,
    tax_item_total                  MONEY        NOT NULL DEFAULT 0,
    tax_exempt                      BIT          NOT NULL DEFAULT 0,
    product_id                      INT          NOT NULL,
    conversion_product_id           INT          NULL,
    product_locale                  CHAR(5)      NULL,
    insert_date                     DATETIME     NOT NULL DEFAULT GETDATE(),
    insert_by                       VARCHAR(50)  NOT NULL DEFAULT SUSER_SNAME(),
    modified_date                   DATETIME     NOT NULL DEFAULT GETDATE(),
    modified_by                     VARCHAR(50)  NOT NULL DEFAULT SUSER_SNAME(),
    cart_order_status_id            TINYINT      NOT NULL DEFAULT 1,
    cart_order_item_in_process_id   INT          NULL,
    cart_item_bundle_id             INT          NULL,
    start_date                      DATETIME     NULL,
    expiration_date                 DATETIME     NULL,
    vendor_order_item_code          VARCHAR(36)  NULL,
    order_item_update_type_id       TINYINT      NULL DEFAULT 1,
    license_attribute_license_value INT          NULL,
    item_hierarchy_id               TINYINT      NULL DEFAULT 1,
    cart_discount_id                INT          NULL,
    cart_discount_method_id         TINYINT      NULL,
    discount                        FLOAT        NULL,
    unit_price_pre_vat              MONEY        NULL,
    usage_price                     MONEY        NULL DEFAULT 0,
    opportunity_line_item_id        VARCHAR(18)  NULL,
    sap_material_number             INT          NULL,
    storage_gb                      INT          NULL,

    CONSTRAINT FK_coi_cart_order  FOREIGN KEY (cart_order_id) REFERENCES dbo.cart_order(cart_order_id),
    CONSTRAINT FK_coi_product     FOREIGN KEY (product_id)    REFERENCES dbo.product(product_id)
);
GO

CREATE TABLE dbo.cart_json (
    cart_json_id              INT           NOT NULL PRIMARY KEY IDENTITY(1,1),
    cart_json                 NVARCHAR(MAX) NOT NULL,
    cart_order_id             INT           NULL,
    cart_order_in_process_id  INT           NULL,

    CONSTRAINT FK_cj_cart_order FOREIGN KEY (cart_order_id) REFERENCES dbo.cart_order(cart_order_id)
);
GO

CREATE TABLE dbo.cart_order_partner (
    cart_order_partner_id INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    cart_order_id         INT NOT NULL,
    partner_id            INT NOT NULL,
    partner_account_id    INT NULL,

    CONSTRAINT FK_cop_cart_order FOREIGN KEY (cart_order_id) REFERENCES dbo.cart_order(cart_order_id),
    CONSTRAINT FK_cop_partner    FOREIGN KEY (partner_id)    REFERENCES dbo.partner(partner_id)
);
GO

-- FK to partner_account (table now exists)
ALTER TABLE dbo.cart_order_partner
    ADD CONSTRAINT FK_cop_partner_account
        FOREIGN KEY (partner_account_id) REFERENCES dbo.partner_account(partner_account_id);
GO

-- =============================================================================
-- cart_order_route  (G4)
-- =============================================================================
CREATE TABLE dbo.cart_order_route (
    cart_order_route_id   INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    cart_order_id         INT          NOT NULL,
    routing_action        VARCHAR(50)  NOT NULL,
    insert_date           DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_cor_cart_order FOREIGN KEY (cart_order_id) REFERENCES dbo.cart_order(cart_order_id)
);
GO

-- =============================================================================
-- cart_order_message  (G5)
-- =============================================================================
CREATE TABLE dbo.cart_order_message (
    cart_order_message_id       INT              NOT NULL PRIMARY KEY IDENTITY(1,1),
    cart_order_id               INT              NOT NULL,
    message_key                 UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    license_id                  INT              NULL,
    cart_discount_id            INT              NULL,
    status_id                   TINYINT          NOT NULL DEFAULT 1,
    message_campaign_id         INT              NULL,
    message_campaign_platform   VARCHAR(50)      NULL,
    CONSTRAINT FK_com_cart_order FOREIGN KEY (cart_order_id) REFERENCES dbo.cart_order(cart_order_id)
);
GO

-- =============================================================================
-- license_key  (G5 lookup — read-only from this service)
-- =============================================================================
CREATE TABLE dbo.license_key (
    license_key_id          INT              NOT NULL PRIMARY KEY IDENTITY(1,1),
    license_key             UNIQUEIDENTIFIER NOT NULL,
    license_id              INT              NOT NULL,
    salesforce_license_id   VARCHAR(50)      NULL
);
GO

-- =============================================================================
-- partner_configuration_partner  (G6)
-- =============================================================================
CREATE TABLE dbo.partner_configuration_partner (
    partner_configuration_partner_id   INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    partner_id                         INT          NOT NULL,
    partner_configuration_id           TINYINT      NOT NULL,
    configuration_value                VARCHAR(100) NOT NULL,
    CONSTRAINT FK_pcp_partner FOREIGN KEY (partner_id) REFERENCES dbo.partner(partner_id)
);
GO

-- Seed: partner_configuration_id = 15 means "default currency for this partner"
-- Add real rows here when known; empty is fine for local dev
-- INSERT INTO dbo.partner_configuration_partner (partner_id, partner_configuration_id, configuration_value)
-- VALUES (1, 15, 'EUR');

-- =============================================================================
-- cart_order_item_json  (G8)
-- =============================================================================
CREATE TABLE dbo.cart_order_item_json (
    cart_order_item_json_id   INT            NOT NULL PRIMARY KEY IDENTITY(1,1),
    cart_order_item_id        INT            NOT NULL,
    cart_order_item_json      NVARCHAR(MAX)  NOT NULL,
    insert_date               DATETIME       NOT NULL DEFAULT GETDATE(),
    modified_date             DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_coij_item FOREIGN KEY (cart_order_item_id) REFERENCES dbo.cart_order_item(cart_order_item_id)
);
GO

-- =============================================================================
-- cart_order_item_json_log  (audit log for item JSON; writes deferred to later gap)
-- =============================================================================
CREATE TABLE dbo.cart_order_item_json_log (
    cart_order_item_json_log_id INT           NOT NULL PRIMARY KEY IDENTITY(1,1),
    cart_order_id               INT           NULL,
    item_json                   NVARCHAR(MAX) NULL,
    bundle_json                 NVARCHAR(MAX) NULL,
    insert_date                 DATETIME      NULL DEFAULT GETDATE()
);
GO

-- =============================================================================
-- cart_order_item_license  (G9)
-- =============================================================================
CREATE TABLE dbo.cart_order_item_license (
    cart_order_item_license_id   INT         NOT NULL PRIMARY KEY IDENTITY(1,1),
    cart_order_item_id           INT         NOT NULL,
    keycode                      VARCHAR(40) NOT NULL,
    insert_date                  DATETIME    NOT NULL DEFAULT GETDATE(),
    insert_by                    VARCHAR(100) NOT NULL DEFAULT '',
    modified_date                DATETIME    NOT NULL DEFAULT GETDATE(),
    modified_by                  VARCHAR(100) NOT NULL DEFAULT '',
    cart_order_status_id         TINYINT     NOT NULL DEFAULT 1,
    CONSTRAINT FK_coil_item FOREIGN KEY (cart_order_item_id) REFERENCES dbo.cart_order_item(cart_order_item_id)
);
GO

-- =============================================================================
-- cart_site_id_order_code_prefix
-- Mirrors the QA table used in SP section 2.1:
--   SELECT vendor_order_code_prefix FROM cart_site_id_order_code_prefix WHERE site_id = @site_id
-- =============================================================================
CREATE TABLE dbo.cart_site_id_order_code_prefix (
    cart_site_id_order_code_prefix_id   INT          NOT NULL PRIMARY KEY IDENTITY(1,1),
    site_id                             VARCHAR(65)  NOT NULL UNIQUE,
    vendor_order_code_prefix            VARCHAR(5)   NOT NULL,
    site_id_description                 VARCHAR(100) NULL
);
GO

-- Seed: add the site_ids that are used in local/QA testing.
-- Extend this list as new site_ids are encountered.
INSERT INTO dbo.cart_site_id_order_code_prefix (site_id, vendor_order_code_prefix, site_id_description) VALUES
    ('gsm',        'GSM',  'GSM storefront'),
    ('WRCART',     'ECM',  'WR Cart / eCommerce'),
    ('ecom',       'ECM',  'eCommerce default'),
    ('test',       'TST',  'Test environment'),
    ('default',    'ORD',  'Generic fallback');
GO

-- =============================================================================
-- Sequence: cart_order_next_id
-- Local equivalent of usp_next_id @Type=3 used in SP section 2.1.
-- When the code points to QA, replace the SEQUENCE call with:
--   DECLARE @t TABLE (id INT); INSERT INTO @t EXEC usp_next_id @Type=3; SELECT TOP 1 id FROM @t;
-- Start at 10000001 so local codes are visually distinct from QA codes.
-- =============================================================================
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'cart_order_next_id' AND schema_id = SCHEMA_ID('dbo'))
    DROP SEQUENCE dbo.cart_order_next_id;
GO

CREATE SEQUENCE dbo.cart_order_next_id
    AS INT
    START WITH 10000001
    INCREMENT BY 1
    NO CYCLE;
GO

-- =============================================================================
-- SANITY CHECK
-- =============================================================================
SELECT TABLE_NAME AS [Table]
FROM   INFORMATION_SCHEMA.TABLES
WHERE  TABLE_TYPE = 'BASE TABLE'
ORDER  BY TABLE_NAME;
GO

PRINT '=== ecom_cart_dev setup complete ===';
GO
