Text
CREATE PROCEDURE [dbo].[usp_cart_select_license_configurator_pricing] (  
    @item_json NVARCHAR(MAX),  
    @bundle_json NVARCHAR(MAX),  
    @opt_args VARCHAR(100) = ''  
)  
  
/*  
 DATE   AUTHOR  REMARKS  
 2018-04-25  esmart  Initial creation.  
 2018-10-22  esmart  Add usage_price for autobilling utility and overage  
 2018-12-03  esmart  Fix issue with adding secondary on primary renewal  
 2018-12-11  esmart  Fix online discounting  
 2019-04-01  jnavarra Add consumer pricing  
 2019-04-02  jnavarra Compensate for leap years  
 2019-05-21  esmart  Fix consumer  
 2019-06-30  esmart  No upgrade items on monthly billing models  
 2019-10-23  jnavarra Force quantity "1" on consumer products  
 2019-12-17  jnavarra Add equivalent_year_price  
 2020-01-06  wbarton  Modified sections 4.1.2 and 4.1.7.2 to introduce unit_price and usage_price discount grandfathering via newly created discount models. Logic in 4.1.7.2-4 now matches usp_cart_insert_cart_order_item.  
 2020-01-14  jnavarra    equivalent_year_price prioritize locale pricing  
 2020-02-12  wbarton  Hotfix in 4.1.2, replacing usp_license_select_category_discount_model with a function call, since email processes call the configurator as an insert into - exec, creating a nesting problem  
 2020-02-14  wbarton  Refactoring 4.1.2 to support JSON operations in/out of usp_license_select_category_discount_model. This allows us to log data from the subroutine, but avoids creating insert into - exec nesting  
 2020-04-14  jnavarra Parse cart_discount_id  
 2020-08-19  jnavarra Default secondary product start_date as expiration_date (fallback to primary product's new start_date)  
 2020-09-22  jnavarra Correct previous secondary product start_date to account for upgrades  
 2020-11-06  abelenkiy   Changed message_campaign_name in table variable from varchar(50) to varchar(65) to match message_campaign table (section 4.1.5)  
                                Hardcoded @source_caller input to usp_license_select_category_discount_model to avoid exceeding length limit (section 4.1.2)  
 2020-12-01  abelenkiy   AXIOM-2275 - Hotfix to use retail price for list price when discount value of 0 passed in explicitly (section 3.1.4)  
 2021-01-15  jnavarra Update rounding precision (sections 3.1.4, 3.1.5, 4.1.6)  
 2021-12-08  psatish  Added Perpetual billing model for Usage pricing  
 2024-02-26  rambasana Update price from cpProduct API, if zuora campaign_id exists.   
 2024-12-27  psatish  US-4224198 Modified the section 3.1.3 to pass the message campaign name to the procedure: usp_cart_select_renewal_product_set  
 2025-06-16  jberry  US-4833298 Update sec 2.4 to use fn_product_select_profile to select the product  
                    - Changed sec 4.1.7.4 to ignore discounts for licenses with a usage_pricing model (carbonite)  
                    - Added storage_gb and retention to @item_discount_json and to the return dataset  
 2025-10-14  jberry  US-5004939 - Added actual_storage_quantity to return. This field converts storage_gb to TBs to use for pricing calculations for storage products  
 2025-11-19  jberry  US-5093883 - Modified sec 2.2.1 so an upgrade line is not added for carbonite orders when the quantity is being increased on an order  
    2026-06-04      gblandford  D-5375484 - https://ot-internal.saas.microfocus.com/ui/entity-navigation?p=4001/26009&entityType=work_item&id=5375484  
                                - Utility licenses with expiration beyond a year were falling through the primary product_type logic.   
                                  (Utility licenses are now extended by two years and in the billing expiration update job).  
                                  This was causing the configurator to not assign a product_type_id to the primary product and subsequently not allow checkout  
                                  in cart. Resolved by adding an ELSE statement to the primary product_type_id assignment logic in section 2.2  
                                - formatting and case changes, removed commented out code, and added additional comments for clarity  
                                - added @opt_args to assist in debugging  
                                - renumbered sections  
                                - added schemas where missing  
                                - added SET TRANSACTION ISOLATION LEVEL at the beginning of the proc  
  
    DESCRIPTION  
    Calculates and returns cart pricing rows for license configurator requests.  
    The procedure normalizes incoming item/bundle JSON, derives product type and  
    product selection, applies discount/business pricing rules, and returns  
    checkout-ready item rows including list/unit/usage pricing.  
  
    RUN CONTEXT  
    - Invoked by configurator and renewal messaging flows (including auto-renewal  
        and message output workflows).  
    - Executes under READ UNCOMMITTED isolation and uses OPENJSON-based parsing.  
    - Supports debug output when @opt_args contains ''debug''.  
    - Handles both direct and partner pricing paths.  
  
    HIGH-LEVEL FLOW  
    1) Parse request payload  
         - Read @bundle_json and @item_json into working variables/table sets.  
    2) Resolve license context  
         - Load existing license/profile data and derive locale/language/location.  
    3) Build product intent  
         - Determine start/expiration dates, product_type_id, and upgrade behavior.  
    4) Resolve products and prices  
         - Select products, populate list/unit/usage pricing, and apply campaign/  
             tier/discount logic.  
    5) Return output rows  
         - Emit normalized line items with pricing, product metadata, discount data,  
             and storage/retention-related fields.  
  
    PARAMETERS  
    @item_json NVARCHAR(MAX)  
        JSON array of incoming cart lines (category, seats, years, dates, bundle,  
        hierarchy, discount, storage/retention attributes, etc.).  
  
    @bundle_json NVARCHAR(MAX)  
        JSON object with request-level context (locale, keycode, billing model,  
        campaign/message values, and optional discount identifiers).  
  
    @opt_args VARCHAR(100) = ''  
        Optional execution flags; currently used for debug behavior when it contains  
        ''debug''.  
  
    DEPENDENCIES  
    Procedures  
    - dbo.usp_cart_select_renewal_product_set  
    - dbo.usp_license_select_category_discount_model  
    - dbo.usp_LogError  
  
    Functions  
    - dbo.fn_locale_to_lang_loc  
    - dbo.fn_license_select_license_profile  
    - dbo.fn_product_select_profile  
    - dbo.fn_leap_days_between  
    - dbo.fn_cart_select_one_year_products  
    - dbo.fn_app_config_select_key_values  
  
    Primary reference tables/views (non-exhaustive)  
    - dbo.license, dbo.license_distribution_method  
    - dbo.license_category, dbo.license_category_product_line  
    - dbo.product, dbo.product_pricing, dbo.product_years,  
        dbo.product_capability, dbo.product_storage, dbo.product_extension  
    - dbo.partner_pricing_tier, dbo.zuora_product_pricing  
    - dbo.license_cart_discount, dbo.cart_discount_item  
    - dbo.message_campaign and related message_campaign_* tables  
  
*/  
  
AS  
  
SET NOCOUNT ON;  
  
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;  
  
BEGIN TRY  
  
    DECLARE @locale VARCHAR(5),  
        @language_code VARCHAR(2),  
        @location_code VARCHAR(3),  
        @keycode VARCHAR(40),  
        @license_id INT,  
        @license_category_name VARCHAR(10),  
        @license_seats INT,  
        @storage_gb INT,  
        @years FLOAT,  
        @cart_discount_id INT,  
        @license_distribution_method_id INT,  
        @license_attribute_id INT,  
        @license_attribute_license_value INT,  
        @license_keycode_type_id INT,  
        @partner_id INT,  
        @site_id VARCHAR(20),  
        @insert_date DATETIME,  
        @insert_by VARCHAR(20),  
        @currency_id TINYINT,  
        @product_line_id INT,  
        @percent_discount FLOAT,  
        @product_type_id INT,  
        @message_key VARCHAR(36),  
        @response_code INT,  
        @message VARCHAR(100),  
        @campaign_id INT,  
        @license_distribution_method_code VARCHAR(4),  
     @message_campaign_name VARCHAR(65),  
        @debug_mode BIT = 0  
  
    DECLARE @item_table TABLE (  
        item_id INT IDENTITY(1, 1),  
        license_category_name VARCHAR(10),  
        license_seats INT,  
        total_license_seats INT,  
        storage_gb INT,  
        retention_model_id INT,  
        usage_pricing_model_id INT,  
        years FLOAT,  
        license_keycode_type_id INT,  
        start_date DATETIME,  
        expiration_date DATETIME,  
        vendor_order_item_code VARCHAR(36),  
        cart_item_bundle_id INT,  
        item_hierarchy_id TINYINT,  
        product_id INT,  
        product_type_id INT,  
        order_item_offer_amount MONEY DEFAULT 0,  
        list_price MONEY DEFAULT 0,  
        unit_price MONEY DEFAULT 0,  
        discount FLOAT,  
        cart_discount_method_id TINYINT,  
        cart_discount_id INT,  
        usage_price MONEY DEFAULT 0  
    );  
  
    DECLARE @license_table TABLE (  
        license_id INT,  
        license_category_name VARCHAR(10),  
        license_seats INT,  
        storage_gb INT,  
        license_keycode_type_id INT,  
        license_attribute_license_value INT,  
        start_date DATETIME,  
        expiration_date DATETIME,  
        category_type_name VARCHAR(50),  
        item_hierarchy_id TINYINT,  
        usage_pricing_model_id INT,  
        retention_model_id INT,  
        product_platform_id INT  
    );  
  
    IF @opt_args LIKE '%debug%' BEGIN  
            SET @debug_mode = 1;  
        END;  
  
    DECLARE @CARBONITE_LICENSE_CATEGORIES TABLE (  
        license_category_id INT,  
  license_category_name VARCHAR(10)  
    )  
  
    DECLARE @message_campaign_cart_discount TABLE (  
        message_campaign_id INT,  
        message_campaign_name VARCHAR(65),  
        message_campaign_class_id TINYINT,  
        message_campaign_class_name VARCHAR(50),  
        message_campaign_start_date DATETIME,  
        message_campaign_end_date DATETIME,  
        license_distribution_method_id INT,  
        inclusive TINYINT,  
        license_seats INT,  
        license_category_id INT,  
        license_category_name VARCHAR(10),  
        autorenewal_opt_id TINYINT,  
        language_code VARCHAR(2),  
        location_code VARCHAR(3),  
        cart_discount_id INT  
    );  
  
  
    INSERT INTO @CARBONITE_LICENSE_CATEGORIES (  
        license_category_id,  
  license_category_name  
    )  
    SELECT f.[key],  
   f.value  
    FROM [dbo].[fn_app_config_select_key_values]('CARBONITE_LICENSE_CATEGORIES', 'CARBONITE') f  
  
    SELECT @insert_date = GETDATE(),  
        @insert_by = SUSER_SNAME();  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT 'CARBONITE_LICENSE_CATEGORIES' AS debug_message, * FROM @CARBONITE_LICENSE_CATEGORIES;  
        END;  
  
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------  
    -- 1.) select  
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------  
  
    -- 1.1) @bundle_json  
    SELECT @locale = locale,  
           @keycode = ( CASE  
                              WHEN keycode = '' THEN NULL  
                              ELSE keycode  
                          END ),  
           @license_attribute_license_value = license_attribute_license_value,  
           @license_keycode_type_id = license_keycode_type_id,  
           @message_key = message_key,  
           @cart_discount_id = cart_discount_id,  
     @message_campaign_name=message_campaign_name  
    FROM OPENJSON( @bundle_json )  
    WITH (  
        locale VARCHAR(40) '$.locale',  
        keycode VARCHAR(40) '$.keycode',  
        license_attribute_license_value INT '$.license_attribute_license_value',  
        license_keycode_type_id INT '$.license_keycode_type_id',  
        message_key VARCHAR(36) '$.message_key',  
        cart_discount_id INT '$.cart_discount_id',  
  message_campaign_name VARCHAR(65) '$.message_campaign_name'  
    );  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '1.1' AS debug_message,  
                @locale AS locale,  
                @keycode AS keycode,  
                @license_attribute_license_value AS license_attribute_license_value,  
                @license_keycode_type_id AS license_keycode_type_id,  
                @message_key AS message_key,  
                @cart_discount_id AS cart_discount_id,  
                @message_campaign_name AS message_campaign_name;  
        END;  
  
  
    -- 1.1.1) @language_code and @location_code  
    SELECT @language_code = language_code,  
        @location_code = location_code  
    FROM dbo.fn_locale_to_lang_loc(@locale);  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '1.1.1' AS debug_message,  
                @language_code AS language_code,  
                @location_code AS location_code;  
        END;  
  
    -- 1.1.2) @license_id  
    IF @keycode IS NOT NULL BEGIN  
            SELECT   
                @license_id = l.license_id,  
                @license_keycode_type_id = ( CASE  
                                                    WHEN @license_keycode_type_id IS NULL THEN l.license_keycode_type_id  
                                                    ELSE @license_keycode_type_id  
                                                END ),  
                @license_distribution_method_id = l.license_distribution_method_id,  
                @product_line_id = l.product_line_id,  
                @license_distribution_method_code = ldm.license_distribution_method_code  
            FROM dbo.license l  
                INNER JOIN dbo.license_distribution_method AS ldm  
                    ON ldm.license_distribution_method_id = l.license_distribution_method_id  
            WHERE l.keycode = @keycode;  
  
        END;  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '1.1.2' AS debug_message,  
                @license_id AS license_id,  
                @license_keycode_type_id AS license_keycode_type_id,  
                @license_distribution_method_id AS license_distribution_method_id,  
                @product_line_id AS product_line_id,  
                @license_distribution_method_code AS license_distribution_method_code;  
        END;  
  
    -- 1.1.3) @license_attribute_id  
    SELECT   
        @license_attribute_id = license_attribute_id  
    FROM dbo.license_attribute_license_value  
    WHERE license_attribute_license_value = @license_attribute_license_value;  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '1.1.3' AS debug_message,  
                @license_attribute_id AS license_attribute_id;  
        END;  
      
    -- 1.2) @item_table  
    INSERT INTO @item_table (  
        license_category_name,  
        license_seats,  
        total_license_seats,  
        storage_gb,  
        retention_model_id,  
        years,  
        license_keycode_type_id,  
        start_date,  
        expiration_date,  
        vendor_order_item_code,  
        cart_item_bundle_id,  
        item_hierarchy_id,  
        discount,  
        cart_discount_method_id  
    )  
    SELECT   
        license_category_name,  
        license_seats,  
        license_seats,  
        storage_gb,  
        retention_model_id,  
        years = CASE  
                    WHEN years = '' THEN  
                        0  
                    ELSE  
                        years  
                END,  
        @license_keycode_type_id,  
        start_date = CASE  
                        WHEN start_date = '' THEN  
                            NULL  
                        ELSE  
                            start_date  
                    END,  
        expiration_date = CASE  
                                WHEN expiration_date = '' THEN  
                                    NULL  
ELSE  
                                    expiration_date  
                            END,  
        vendor_order_item_code = CASE  
                                    WHEN vendor_order_item_code = '' THEN  
                                        NULL  
                                    ELSE  
                                        vendor_order_item_code  
                                END,  
        cart_item_bundle_id = ISNULL(cart_item_bundle_id, 1),  
        item_hierarchy_id,  
        discount,  
        cart_discount_method_id  
  
    FROM OPENJSON( @item_json )  
    WITH (  license_category_name VARCHAR(10) '$.license_category_name',  
            license_seats INT '$.license_seats',  
            storage_gb INT '$.storage_gb',  
            retention_model_id INT '$.retention_model_id',  
            years FLOAT '$.years',  
            license_keycode_type_id INT '$.license_keycode_type_id',  
            locale VARCHAR(5) '$.locale',  
            license_attribute_license_value INT '$.license_attribute_license_value',  
            start_date DATETIME '$.start_date',  
            expiration_date DATETIME '$.expiration_date',  
            cart_item_bundle_id INT '$.cart_item_bundle_id',  
            item_hierarchy_id INT '$.item_hierarchy_id',  
            vendor_order_item_code VARCHAR(36) '$.vendor_order_item_code',  
            discount FLOAT '$.discount',  
            cart_discount_method_id TINYINT '$.cart_discount_method_id'  
        );  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '1.2' AS debug_message,  
                *  
            FROM @item_table;  
        END;  
  
    -- 1.3) @license_table  
    IF ( @license_id IS NOT NULL ) BEGIN  
            INSERT INTO @license_table (  
                license_id,  
                license_category_name,  
                license_seats,  
                storage_gb,  
                license_keycode_type_id,  
                license_attribute_license_value,  
                start_date,  
                expiration_date,  
                category_type_name,  
                item_hierarchy_id,  
                usage_pricing_model_id,  
                retention_model_id,  
                product_platform_id  
            )  
            SELECT   
                license_id,  
                license_category_name,  
                license_seats,  
                storage_gb,  
                license_keycode_type_id,  
                license_attribute_license_value,  
                start_date,  
                expiration_date,  
                category_type_name,  
                item_hierarchy_id,  
                usage_pricing_model_id,  
                retention_model_id,  
                product_platform_id  
            FROM dbo.fn_license_select_license_profile(@license_id);  
        END;  
   
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '1.3' AS debug_message,  
                *  
            FROM @license_table;  
        END;  
  
    -- 1.4) upgrade license_seats and storage  
    IF @product_line_id IN ( 55, 300 ) BEGIN  
            UPDATE i  
                SET license_seats = i.license_seats - l.license_seats  
            FROM @item_table i  
                INNER JOIN @license_table l  
                    ON l.license_category_name = i.license_category_name  
            WHERE i.years = 0;  
  
            UPDATE i  
               SET storage_gb = i.storage_gb - l.storage_gb  
            FROM @item_table i  
               INNER JOIN @license_table l  
                   ON l.license_category_name = i.license_category_name  
            WHERE i.years = 0  
                AND l.usage_pricing_model_id = 2;   --capacity  
        END;  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '1.4' AS debug_message,  
                *  
            FROM @item_table;  
        END;  
  
  
    -- 1.9) @product_line_id  
    IF ( @product_line_id IS NULL ) BEGIN  
            SELECT   
                @product_line_id = pl.product_line_id  
            FROM @item_table i  
INNER JOIN dbo.license_category lc  
                    ON lc.license_category_name = i.license_category_name  
                INNER JOIN dbo.license_category_product_line pl  
                    ON pl.license_category_id = lc.license_category_id  
                       AND pl.language_code = @language_code  
                       AND pl.location_code = @location_code;  
        END;  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '1.9' AS debug_message,  
                @product_line_id AS product_line_id;  
        END;  
  
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------  
    -- 2.) product set  
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------  
  
    -- 2.1) primary start_date and expiration_date  
    UPDATE i  
        SET start_date = CASE  
                             WHEN l.license_id IS NULL  
                                  AND i.start_date IS NULL THEN  
                                 CONVERT(DATE, GETDATE())  
                             WHEN l.license_id IS NOT NULL  
                                  AND i.start_date IS NULL  
                                  AND l.category_type_name = 'trial' THEN  
                                 CONVERT(DATE, GETDATE())  
                             WHEN l.license_id IS NOT NULL  
                                  AND i.start_date IS NULL  
                                  AND l.category_type_name = 'full'  
                                  AND i.years = 0 THEN  
                                 CONVERT(DATE, GETDATE())  
                             WHEN l.license_id IS NOT NULL  
                                  AND i.start_date IS NULL  
                                  AND l.category_type_name = 'full'  
                                  AND i.years <> 0  
                                  AND l.expiration_date < GETDATE() THEN  
                                 CONVERT(DATE, GETDATE())  
                             WHEN l.license_id IS NOT NULL  
                                  AND i.start_date IS NULL  
                                  AND l.category_type_name = 'full'  
                                  AND i.years <> 0  
                                  AND l.expiration_date >= GETDATE() THEN  
                                 l.expiration_date  
                             ELSE  
                                 i.start_date  
                         END,  
        expiration_date = CASE  
                              WHEN l.license_id IS NULL  
                                   AND i.expiration_date IS NULL THEN  
                                  DATEADD(mm, i.years * 12, CONVERT(DATE, GETDATE()))  
                              WHEN l.license_id IS NOT NULL  
                                   AND i.expiration_date IS NULL  
                                   AND l.category_type_name = 'trial' THEN  
                                  DATEADD(mm, i.years * 12, CONVERT(DATE, GETDATE()))  
                              WHEN l.license_id IS NOT NULL  
                                   AND i.expiration_date IS NULL  
                                   AND l.category_type_name = 'full'  
                                   AND i.start_date IS NULL  
                                   AND l.expiration_date < GETDATE() THEN  
                                  DATEADD(mm, i.years * 12, CONVERT(DATE, GETDATE()))  
                              WHEN l.license_id IS NOT NULL  
                                   AND i.expiration_date IS NULL  
                                   AND l.category_type_name = 'full'  
                                   AND i.start_date IS NULL  
                                   AND l.expiration_date >= GETDATE() THEN  
                                  DATEADD(mm, i.years * 12, l.expiration_date)  
                              WHEN l.license_id IS NOT NULL  
                                   AND i.expiration_date IS NULL  
                                   AND l.category_type_name = 'full'  
                                   AND i.start_date IS NOT NULL THEN  
                                  DATEADD(mm, i.years * 12, i.start_date)  
                              ELSE  
                                  i.expiration_date  
                          END  
    FROM @item_table i  
        LEFT JOIN @license_table l  
            ON l.item_hierarchy_id = i.item_hierarchy_id  
    WHERE i.item_hierarchy_id = 1;  
  
    if ( @debug_mode = 1 ) BEGIN  
            SELECT '2.1' AS debug_message,  
                *  
            FROM @item_table;  
        END;  
  
    -- 2.1.1) secondary start_date and expiration_date  
    UPDATE i  
        SET i.start_date = CASE  
                               WHEN i2.start_date IS NULL  
                                    AND i.years = 0 THEN  
                                   CONVERT(DATE, GETDATE())  
                               WHEN i2.start_date IS NULL  
                                    AND i.years > 0 THEN  
                                   ISNULL(l.expiration_date, CONVERT(DATE, GETDATE()))  
                               WHEN i2.start_date IS NOT NULL  
                                    AND i.years > 0  
                                    AND  
                                    (  
                                        l.category_type_name IS NULL  
                                        OR l.category_type_name = 'trial'  
                                    ) THEN  
                                   CONVERT(DATE, GETDATE())  
                               ELSE  
                                   COALESCE(  
                                               CASE  
                                                   WHEN i.years = 0 THEN  
                                                       i2.start_date  
                                                   WHEN l.expiration_date < GETDATE() -- Default to the previous expiration date, but don't set the start date any earlier than today  
                                               THEN  
                                                       CONVERT(DATE, GETDATE())  
                                                   ELSE  
                                                       l.expiration_date  
                                               END,  
                                               i2.start_date -- Without the previous expiration date, use the start_date of the primary product  
                                           )  
                           END,  
        i.expiration_date = CASE  
                                WHEN i2.expiration_date IS NOT NULL THEN  
                                    i2.expiration_date -- expiration from primary product  
                                WHEN i2.expiration_date IS NULL THEN  
                                    x.expiration_date  -- upgrade of secondary only  
                            END  
    FROM @item_table i  
        LEFT JOIN @license_table l  
            ON l.license_category_name = i.license_category_name  
        LEFT JOIN @item_table i2  
            ON i2.cart_item_bundle_id = i.cart_item_bundle_id  
               AND i2.item_hierarchy_id = 1  
        LEFT JOIN (  
                    SELECT license_id,  
                           expiration_date = MAX(expiration_date)  
                    FROM @license_table  
                    GROUP BY license_id  
                ) x  
            ON x.license_id = @license_id  
    WHERE i.item_hierarchy_id = 2;  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '2.1.1' AS debug_message,  
                *  
            FROM @item_table;  
        END;  
  
    -- 2.2) primary product_type  
    UPDATE i  
        SET product_type_id = CASE  
                                    WHEN l.license_id IS NULL THEN 1 -- New  
                                    WHEN l.license_id IS NOT NULL  
                                        AND l.category_type_name = 'trial' THEN 1 -- Trial conversion  
                                    WHEN l.license_id IS NOT NULL  
                                        AND l.category_type_name = 'full'  
                                        AND i.expiration_date > l.expiration_date THEN 2 -- Renewal  
                                    WHEN l.license_id IS NOT NULL  
                                        AND l.category_type_name = 'full'  
                                        AND i.expiration_date = l.expiration_date  
                                        AND (    i.license_category_name <> l.license_category_name  
                                                    OR i.total_license_seats > l.license_seats  
                                                    OR i.storage_gb > l.storage_gb ) THEN 3 -- Upgrade  
                                    ELSE 2 -- Default to Renewal to match logic in cart_item insert  
                                  END  
    FROM @item_table i  
        LEFT JOIN @license_table l  
            ON l.item_hierarchy_id = i.item_hierarchy_id  
    WHERE i.item_hierarchy_id = 1  
          AND i.product_type_id IS NULL;  
  
    if ( @debug_mode = 1 ) BEGIN  
            SELECT '2.2' AS debug_message,  
                *  
            FROM @item_table;  
        END;  
  
    -- 2.2.1) upgrade  
    IF @product_line_id IN ( 100, 200 ) BEGIN  
            INSERT INTO @item_table (  
                license_category_name,  
                license_seats,  
                total_license_seats,  
                storage_gb,  
                retention_model_id,  
                years,  
                license_keycode_type_id,  
                start_date,  
                expiration_date,  
                cart_item_bundle_id,  
                item_hierarchy_id,  
                product_type_id  
            )  
            SELECT   
                i.license_category_name,  
                l.license_seats,  
                i.total_license_seats,  
                i.storage_gb,  
                i.retention_model_id,  
                years = 0,  
                i.license_keycode_type_id,  
                start_date = CONVERT(DATE, GETDATE()),  
                l.expiration_date,  
                i.cart_item_bundle_id,  
                i.item_hierarchy_id,  
                product_type_id = 3  
            FROM @item_table i  
                INNER JOIN @license_table l  
                    ON l.item_hierarchy_id = i.item_hierarchy_id  
            WHERE i.item_hierarchy_id = 1  
                AND i.product_type_id = 2  
                AND ( i.license_category_name <> l.license_category_name  
                    OR i.license_seats > l.license_seats )  
                AND l.expiration_date > GETDATE();  
        END;  
    ELSE BEGIN  
            INSERT INTO @item_table (  
                license_category_name,  
                license_seats,  
                total_license_seats,  
                storage_gb,  
                retention_model_id,  
                years,  
                license_keycode_type_id,  
                start_date,  
                expiration_date,  
                cart_item_bundle_id,  
                item_hierarchy_id,  
                product_type_id,  
                cart_discount_id,  
                discount,  
                cart_discount_method_id  
            )  
            SELECT   
                i.license_category_name,  
                i.license_seats - l.license_seats,  
                i.total_license_seats,  
                i.storage_gb - l.storage_gb,  
                i.retention_model_id,  
                0,  
                i.license_keycode_type_id,  
                CONVERT(DATE, GETDATE()),  
                l.expiration_date,  
                i.cart_item_bundle_id,  
                i.item_hierarchy_id,  
                product_type_id = 3,  
                i.cart_discount_id,  
                i.discount,  
                i.cart_discount_method_id  
            FROM @item_table i  
                INNER JOIN @license_table l  
                    ON l.item_hierarchy_id = i.item_hierarchy_id  
                LEFT JOIN @CARBONITE_LICENSE_CATEGORIES clc  
                    ON clc.license_category_name = i.license_category_name  
            WHERE i.item_hierarchy_id = 1  
                  AND i.product_type_id = 2  
                  AND ( i.license_category_name <> l.license_category_name  
                      OR i.license_seats > l.license_seats  
                      OR i.storage_gb > l.storage_gb )  
                  AND l.expiration_date > GETDATE()  
                  AND clc.license_category_name IS null  
                  AND ( @license_attribute_license_value NOT IN (   
                            12,   
                            110,   
                            111,   
                            112,   
                            210,   
                            211,   
                            212,   
                            13,   
                            113,   
                            213   
                        )  
                        AND l.license_attribute_license_value NOT IN (   
                            12,   
                            110,   
                            111,   
                            112,   
                            210,   
                            211,   
                            212,   
                            13,   
                            113,   
                            213   
                        )  
                  );  
        END;  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '2.2.1' AS debug_message,  
                *  
            FROM @item_table;  
        END;  
  
    -- 2.3) secondary product_type  
    UPDATE i  
       SET product_type_id = CASE  
                                WHEN l.license_id IS NULL THEN 1 -- New  
                                WHEN l.license_id IS NOT NULL  
                                    AND l.category_type_name = 'trial' THEN 1 -- trial conversion  
                                WHEN l.license_id IS NOT NULL  
                                    AND l.category_type_name = 'full'  
                                    AND i.expiration_date > l.expiration_date THEN 2  
                                WHEN l.license_id IS NOT NULL  
                                    AND l.category_type_name = 'full'  
                                    AND i.expiration_date = l.expiration_date  
                                    AND i.total_license_seats > l.license_seats THEN 3  
                            END  
    FROM @item_table i  
        LEFT JOIN @license_table l  
            ON l.item_hierarchy_id = i.item_hierarchy_id  
                AND l.license_category_name = i.license_category_name  
    WHERE i.item_hierarchy_id = 2  
        AND i.product_type_id IS NULL;  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '2.3' AS debug_message,  
                *  
            FROM @item_table;  
        END;  
  
    -- 2.3.1) upgrade  
    INSERT INTO @item_table (  
        license_category_name,  
        license_seats,  
        total_license_seats,  
        storage_gb,  
        years,  
        license_keycode_type_id,  
        start_date,  
        expiration_date,  
        cart_item_bundle_id,  
        item_hierarchy_id,  
        product_type_id  
    )  
    SELECT   
        i.license_category_name,  
        i.license_seats - l.license_seats,  
        i.license_seats,  
        i.storage_gb,  
        0,  
        i.license_keycode_type_id,  
        CONVERT(DATE, GETDATE()),  
        l.expiration_date,  
        i.cart_item_bundle_id,  
        i.item_hierarchy_id,  
        product_type_id = 3  
    FROM @item_table i  
        INNER JOIN @license_table l  
            ON l.item_hierarchy_id = i.item_hierarchy_id  
               AND l.license_category_name = i.license_category_name  
    WHERE i.item_hierarchy_id = 2  
        AND i.product_type_id = 2  
        AND i.total_license_seats > l.license_seats  
        AND ( @license_attribute_license_value NOT IN (   
                12,   
                110,   
                111,   
                112,   
                210,   
                211,   
                212,   
                13,   
                113,   
                213   
            )   
        AND l.license_attribute_license_value NOT IN (  
                12,   
                110,   
                111,   
                112,   
                210,   
                211,   
                212,   
                13,   
                113,   
                213   
            )  
        );  
  
    -- 2.3.2) update years on new upgrade  
    UPDATE I  
        SET years = 1  
    FROM @item_table i  
    WHERE i.item_hierarchy_id = 2  
        AND i.product_type_id IN ( 1, 2 )  
        AND i.years = 0;  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '2.3.2' AS debug_message,  
                *  
            FROM @item_table;  
        END;  
  
    -- 2.3.3) usage pricing model from @license_table   
    UPDATE i  
        SET usage_pricing_model_id = l.usage_pricing_model_id  
    FROM @item_table i  
        INNER JOIN @license_table l  
            ON l.license_category_name = i.license_category_name  
    WHERE l.usage_pricing_model_id IS NOT NULL;  
  
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '2.3.3' AS debug_message,  
                *  
            FROM @item_table;  
        END;  
  
    -- 2.4) product  
    IF @product_line_id IN (55,300,100) BEGIN  
            UPDATE i  
                SET i.product_id = f.product_id  
            FROM @item_table i  
                INNER JOIN dbo.license_category lc  
                    ON lc.license_category_name = i.license_category_name  
                LEFT JOIN @license_table l  
                    ON l.license_category_name = i.license_category_name   
                        AND l.item_hierarchy_id = i.item_hierarchy_id  
                CROSS APPLY dbo.fn_product_select_profile (  
                                                        @product_line_id,  
                                                        lc.license_category_id,  
                                                        i.years,  
                                                        ( CASE  
                                                                WHEN @product_line_id IN (300,55) THEN 1   
                                                                ELSE i.license_seats   
                                                            END ),  
                                                        i.storage_gb,  
                                                        DATEDIFF(dd, i.start_date, i.expiration_date),  
                                                        i.product_type_id,  
                                                        i.license_keycode_type_id,  
                                                        l.usage_pricing_model_id,  
                                                        i.retention_model_id,  
                                                        l.product_platform_id,  
                                                        NULL   --sap_material_number  
                                                        ) f  
        END;   
   
    IF ( @debug_mode = 1 ) BEGIN  
            SELECT '2.4' AS debug_message,  
                *  
            FROM @item_table;  
        END;  
  
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------  
    -- 3.) @product_set consumer  
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------  
  
    -- 3.1) consumer  
    IF @product_line_id NOT IN ( 55, 300 ) BEGIN  
  
  
        DECLARE @product_set TABLE (  
            line_item INT IDENTITY(1, 1),  
            product_id INT,  
          product_description VARCHAR(100),  
            quantity INT DEFAULT 1,  
            retail_price MONEY,  
            upgrade_price MONEY,  
            discount MONEY,  
            standard_discount MONEY,  
            net_price MONEY,  
            product_type_id INT,  
            license_category_name VARCHAR(10),  
            seats INT,  
            years DECIMAL(18, 3),  
            p_type VARCHAR(20),  
            cart_item_class_id TINYINT,  
            cart_item_bundle_id INT,  
            keycode VARCHAR(40),  
            currency_id TINYINT,  
            currency_code CHAR(3),  
            currency_description VARCHAR(20),  
            symbol_html VARCHAR(10),  
            symbol_utf8 NVARCHAR(10),  
            symbol_text VARCHAR(10),  
            cart_discount_id INT,  
            license_category_id INT,  
            license_attribute_id INT,  
            license_attribute_license_value INT,  
            start_date DATETIME,  
            expiration_date DATETIME,  
            contract_days FLOAT  
        );  
  
        -- 3.1.1) varibles  
        SELECT   
            @license_category_name = license_category_name,  
            @storage_gb = storage_gb  
        FROM @item_table  
        WHERE item_hierarchy_id = 1;  
  
        SELECT   
            @product_type_id = product_type_id  
        FROM @item_table  
        WHERE item_hierarchy_id = 1  
            AND license_category_name = 'WIFI';  
  
        -- 3.1.2) years, seats  
        SELECT   
            @years = MAX(years),  
            @license_seats = MAX(total_license_seats)  
        FROM @item_table  
        WHERE item_hierarchy_id = 1;  
  
        IF @debug_mode = 1 BEGIN  
                SELECT '3.1.1 and 3.1.2' AS debug_message,  
                    @license_category_name AS license_category_name,  
                    @storage_gb AS storage_gb,  
                    @product_type_id AS product_type_id,  
                    @years AS years,  
                    @license_seats AS license_seats;  
            END;  
  
        -- 3.1.3) insert product_set  
        INSERT INTO @product_set (  
            product_id,  
            product_description,  
            retail_price,  
            upgrade_price,  
            discount,  
            standard_discount,  
            net_price,  
            product_type_id,  
            license_category_name,  
            seats,  
            years,  
            p_type,  
            currency_id,  
            currency_code,  
            currency_description,  
            symbol_html,  
            symbol_utf8,  
            symbol_text,  
            cart_discount_id  
        )  
  EXECUTE dbo.usp_cart_select_renewal_product_set @keycode = @keycode,                     -- varchar(40)  
                                             @license_category_name = @license_category_name, -- varchar(10)  
                                             @license_seats = @license_seats,                 -- int  
                                             @storage_gb = @storage_gb,                       -- int  
                                             @years = @years,                                 -- float  
                                             @cart_discount_id = @cart_discount_id,           -- int  
                                             @language_code = @language_code,                 -- varchar(2)  
                                             @location_code = @location_code,                 -- varchar(3)  
                                             @cart_type_id = NULL,                            -- tinyint  
                                             @currency_id = @currency_id,                     -- tinyint  
                                             @product_type_id = @product_type_id,             -- int  
                                             @message_campaign_name = @message_campaign_name  -- varchar(65)  
  
        IF @debug_mode = 1 BEGIN  
                SELECT '3.1.3' AS debug_message,  
                    *  
                FROM @product_set;  
            END;  
  
        -- 3.1.4) update @item_table  
        UPDATE i  
            SET product_id = p.product_id,  
                list_price = p.retail_price,  
                unit_price = CASE  
                                    WHEN i.cart_discount_method_id = 3  
                                        AND i.discount > 0 THEN p.retail_price - ROUND((i.discount / 100) * p.retail_price, 2)  
                                    WHEN i.cart_discount_method_id = 1  
                                        AND i.discount > 0 THEN p.retail_price - i.discount  
                                    WHEN i.discount = 0 THEN p.retail_price  
                                    ELSE p.upgrade_price  
                                END,  
                license_seats = 1,  
                total_license_seats = 1  
            FROM @item_table i  
                INNER JOIN @product_set p  
                    ON p.license_category_name = i.license_category_name  
                        AND p.product_type_id = i.product_type_id  
                        AND i.item_hierarchy_id = 1; -- and i.product_type_id in (2,3)  
  
        IF @debug_mode = 1 BEGIN  
                SELECT '3.1.4' AS debug_message,  
                    *  
                FROM @item_table;  
            END;  
  
        -- 3.1.5) update pricing for storage upgrade  
        UPDATE i  
            SET product_id = p.product_id,  
                list_price = p.retail_price,  
                unit_price = CASE  
                                WHEN i.cart_discount_method_id = 3  
                                    AND i.discount > 0 THEN p.retail_price - ROUND((i.discount / 100) * p.retail_price, 2)  
                                WHEN i.cart_discount_method_id = 1  
                                    AND i.discount > 0 THEN p.retail_price - i.discount  
                                ELSE p.upgrade_price  
                            END  
        FROM @item_table i  
            INNER JOIN @product_set p  
                ON p.product_type_id = i.product_type_id  
                   AND i.item_hierarchy_id = 1  
                   AND i.storage_gb IS NOT NULL  
                   AND i.product_type_id = 3;  
  
        IF @debug_mode = 1 BEGIN  
                SELECT '3.1.5' AS debug_message,  
                    *  
                FROM @item_table;  
            END;  
  
        -- 3.1.6) insert storage product  
        INSERT INTO @item_table (  
            product_id,  
            license_category_name,  
            license_seats,  
            total_license_seats,  
            storage_gb,  
            years,  
            license_keycode_type_id,  
            start_date,  
            expiration_date,  
            cart_item_bundle_id,  
            item_hierarchy_id,  
            product_type_id,  
            list_price,  
            unit_price  
        )  
        SELECT  
            s.product_id,  
            NULL,  
            1,  
            1,  
            ps.storage_gb,  
            ISNULL(s.years, 0),  
            p.license_keycode_type_id,  
            NULL,  
            NULL,  
            1,  
            1,  
            s.product_type_id,  
            s.retail_price,  
            s.upgrade_price  
        FROM @product_set s  
            INNER JOIN dbo.product_storage ps  
                ON ps.product_id = s.product_id  
            INNER JOIN dbo.product p  
                ON p.product_id = s.product_id  
            LEFT JOIN @item_table i  
                ON i.product_id = s.product_id  
        WHERE i.product_id IS NULL  
        ORDER BY p.product_type_id DESC;  
  
        IF @debug_mode = 1 BEGIN  
                SELECT '3.1.6' AS debug_message,  
                    *  
                FROM @item_table;  
            END;  
  
        IF @license_category_name IS NULL BEGIN  
                SELECT @license_category_name = license_category_name  
                FROM @license_table  
                WHERE item_hierarchy_id = 1;  
            END;  
    -- 3.1.7) get license_category_name if empty  
        IF @debug_mode = 1 BEGIN  
                SELECT '3.1.7' AS debug_message,  
                    @license_category_name AS license_category_name;  
            END;  
  
        -- 3.1.8) update dates  
        UPDATE i  
            SET start_date = CASE  
                                WHEN l.license_id IS NULL  
                                    AND i.start_date IS NULL THEN  
                                    CONVERT(DATE, GETDATE())  
                                WHEN l.license_id IS NOT NULL  
                                    AND i.start_date IS NULL  
                                    AND l.category_type_name = 'trial' THEN  
                                    CONVERT(DATE, GETDATE())  
                                WHEN l.license_id IS NOT NULL  
                                    AND i.start_date IS NULL  
                                    AND l.category_type_name = 'full'  
                                    AND i.years = 0 THEN  
                                    CONVERT(DATE, GETDATE())  
                                WHEN l.license_id IS NOT NULL  
                                    AND i.start_date IS NULL  
                                    AND l.category_type_name = 'full'  
                                    AND i.years <> 0  
                                    AND l.expiration_date < GETDATE() THEN  
                                    CONVERT(DATE, GETDATE())  
                                WHEN l.license_id IS NOT NULL  
                                    AND i.start_date IS NULL  
                                    AND l.category_type_name = 'full'  
                                    AND i.years <> 0  
                                    AND l.expiration_date >= GETDATE() THEN  
                                    l.expiration_date  
                                ELSE  
                                    i.start_date  
                            END,  
                expiration_date = CASE  
                                  WHEN l.license_id IS NULL  
                                       AND i.expiration_date IS NULL THEN DATEADD(mm, i.years * 12, CONVERT(DATE, GETDATE()))  
                                  WHEN l.license_id IS NOT NULL  
                                       AND i.expiration_date IS NULL  
                                       AND l.category_type_name = 'trial' THEN DATEADD(mm, i.years * 12, CONVERT(DATE, GETDATE()))  
                                  WHEN l.license_id IS NOT NULL  
                                       AND i.expiration_date IS NULL  
                                       AND l.category_type_name = 'full'  
                                       AND i.start_date IS NULL  
                                       AND l.expiration_date < GETDATE() THEN DATEADD(mm, i.years * 12, CONVERT(DATE, GETDATE()))  
                                  WHEN l.license_id IS NOT NULL  
                                       AND i.expiration_date IS NULL  
                                       AND l.category_type_name = 'full'  
                                       AND i.start_date IS NULL  
                                       AND l.expiration_date >= GETDATE() THEN DATEADD(mm, i.years * 12, l.expiration_date)  
                                  WHEN l.license_id IS NOT NULL  
                                       AND i.expiration_date IS NULL  
                                       AND l.category_type_name = 'full'  
                                       AND i.start_date IS NOT NULL THEN DATEADD(mm, i.years * 12, i.start_date)  
                                  ELSE i.expiration_date  
                              END  
        FROM @item_table i  
            LEFT JOIN @license_table l  
                ON l.item_hierarchy_id = i.item_hierarchy_id  
        WHERE i.item_hierarchy_id = 1;  
  
    END;  
  
 IF @debug_mode = 1 BEGIN  
                SELECT '3.1.8' AS debug_message,  
                    *  
                FROM @item_table;  
            END;  
  
    --3.1.9) Update Zuora price if Campaign_id exists  
    IF EXISTS (  
                SELECT 1  
                FROM dbo.zuora_product_pricing  
                WHERE campaign_id = @message_key )  
                    AND @message_key NOT LIKE '%[A-Z]%'  
                    AND @locale = 'en_US' BEGIN  
  
            SET @campaign_id = @message_key;  
  
            UPDATE i  
                SET i.list_price = ISNULL(zp.renewal_price, i.list_price),  
                    i.unit_price = ISNULL(zp.retail_price,i.unit_price),  
                    license_seats = 1,  
                    total_license_seats = 1  
            FROM @item_table i  
                INNER JOIN dbo.zuora_product_pricing zp  
                    ON zp.product_type_id = i.product_type_id  
                INNER JOIN dbo.license_category lc  
                    ON lc.license_category_id = zp.license_category_id  
                        AND lc.license_category_name = i.license_category_name  
            WHERE zp.campaign_id = @campaign_id  
                AND i.item_hierarchy_id = 1  
                AND zp.product_id = i.product_id;  
  
        END;  
  
    IF @debug_mode = 1 BEGIN  
                SELECT '3.1.9' AS debug_message,  
                    *  
                FROM @item_table;  
            END;  
  
  
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------  
    -- 4.) pricing business  
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------  
  
    DECLARE @discount FLOAT,  
        @cart_discount_method_id TINYINT;  
  
    IF @product_line_id IN ( 55, 300 ) BEGIN  
  
            -- 4.1) direct  
            IF @partner_id IS NULL BEGIN  
  
                    -- 4.1.1) update @item_table  
                    UPDATE @item_table  
                        SET list_price = pp.retail_price,  
                            unit_price = CASE  
                                            WHEN i.product_type_id IN ( 1, 2 ) THEN  
                                                ROUND(  
                                                        (CONVERT(  
                                                                    FLOAT,  
                                                                    DATEDIFF(dd, i.start_date, i.expiration_date)  
                                                                    - dbo.fn_leap_days_between(i.start_date, i.expiration_date)  
                                                                ) / c.capability_activation_days  
                                                        ) * pp.retail_price,  
                                                        2  
                                                    )  
                                            WHEN i.product_type_id IN ( 3 ) THEN  
                                                ROUND(  
                                                        (CONVERT(FLOAT, DATEDIFF(dd, i.start_date, i.expiration_date))  
                                                        / y.upgrade_days  
                                                        ) * pp.retail_price,  
                                                        2  
                                                    )  
                                        END  
                    FROM @item_table i  
                        INNER JOIN dbo.product_line_product plp  
                            ON plp.product_id = i.product_id  
                        INNER JOIN dbo.product_pricing pp  
                            ON pp.product_id = i.product_id  
                        INNER JOIN dbo.product_years y  
                            ON y.product_id = pp.product_id  
                        INNER JOIN dbo.product_capability c  
                            ON c.product_id = y.product_id  
     WHERE pp.language_code = @language_code  
                        AND pp.location_code = @location_code  
                        AND plp.product_line_id IN ( 55, 100, 300 );  
  
                    IF @debug_mode = 1 BEGIN  
                            SELECT '4.1.1' AS debug_message,  
                                *  
                            FROM @item_table;  
                        END;  
  
                    -- 4.1.2) Tier discount models - expanded to support SFDC discount models via usp_license_select_category_discount_model  
                    DECLARE @item_discount_profile TABLE (  
                        item_id INT,  
                        license_id INT,  
                        license_category_name VARCHAR(10),  
                        product_type_id INT,  
                        discount FLOAT,  
                        cart_discount_method_id INT,  
                        cart_discount_id INT  
                    );  
  
                    DECLARE @item_discount_json VARCHAR(MAX);  
                    SET @item_discount_json = (  
                        SELECT item_id,  
                            @license_id AS license_id,  
                            license_category_name,  
                            total_license_seats AS license_seats,  
                            product_type_id,  
                            years,  
                            @license_attribute_license_value AS license_attribute_license_value,  
                            retention_model_id,  
                            storage_gb  
                        FROM @item_table  
                        FOR JSON PATH  
                    );  
  
                    DECLARE @item_discount_profile_json NVARCHAR(MAX);  
                    EXECUTE dbo.usp_license_select_category_discount_model @item_json = @item_discount_json,  
                                                                    @source_caller = 'usp_cart_select_license_configurator_pricing',  
                                                                    @item_discount_profile_json = @item_discount_profile_json OUTPUT;  
  
                    INSERT INTO @item_discount_profile (  
                        item_id,  
                        license_id,  
                        license_category_name,  
                        product_type_id,  
                        discount,  
                        cart_discount_method_id,  
                        cart_discount_id  
                    )  
                    SELECT item_id,  
                        license_id,  
                        license_category_name,  
                        product_type_id,  
                        discount,  
                        cart_discount_method_id,  
                        cart_discount_id  
                    FROM OPENJSON(@item_discount_profile_json)  
                    WITH (  
                        item_id INT '$.item_id',  
                        license_id INT '$.license_id',  
                        license_category_name VARCHAR(10) '$.license_category_name',  
                        product_type_id INT '$.product_type_id',  
                        discount FLOAT '$.discount',  
                        cart_discount_method_id INT '$.cart_discount_method_id',  
                        cart_discount_id INT '$.cart_discount_id'  
                    );  
  
                    UPDATE s  
                        SET s.unit_price = ROUND((1.00 - idp.discount / 100) * s.unit_price, 2),  
                            s.cart_discount_id = idp.cart_discount_id,  
                            s.discount = idp.discount  
                    FROM @item_table s  
                        INNER JOIN @item_discount_profile idp  
                            ON idp.item_id = s.item_id  
                                AND idp.license_category_name = s.license_category_name  
                                AND idp.product_type_id = s.product_type_id;  
  
                    IF @debug_mode = 1 BEGIN  
          SELECT '4.1.2' AS debug_message,  
                                *  
                            FROM @item_table;  
                        END;  
  
                    -- 4.1.3) license_Cart_discount  
                    SELECT @discount = d.discount,  
                        @cart_discount_method_id = d.cart_discount_method_id  
                    FROM (  
                            SELECT TOP 1  
                                di.discount,  
                                di.cart_discount_method_id  
                            FROM dbo.license_cart_discount ld  
                                INNER JOIN dbo.cart_discount_item di  
                                    ON di.cart_discount_id = ld.cart_discount_id  
                            WHERE ld.license_id = @license_id  
                                AND ld.start_date <= @insert_date  
                                AND @insert_date < ld.end_date  
                            ORDER BY ld.license_cart_discount_id DESC  
                        ) d;  
  
                    IF @debug_mode = 1 BEGIN  
                            SELECT '4.1.3' AS debug_message,  
                                @discount AS discount,  
                                @cart_discount_method_id AS cart_discount_method_id;  
                        END;  
  
                    -- 4.1.4) update @item_table discount  
                    IF @discount IS NOT NULL BEGIN  
                            UPDATE @item_table  
                                SET discount = @discount,  
                                    cart_discount_method_id = @cart_discount_method_id;  
                        --where discount is null --commenting out to allow license_cart_discount to update @item_table, since 4.1.2 is setting a discount value  
                        END;  
  
                    IF @debug_mode = 1 BEGIN  
                            SELECT '4.1.4' AS debug_message,  
                                *  
                            FROM @item_table;  
                        END;  
  
                    -- 4.1.5) @message_campaign_cart_discount  
                    INSERT INTO @message_campaign_cart_discount (  
                        message_campaign_id,  
                        message_campaign_name,  
                        message_campaign_class_id,  
                        message_campaign_class_name,  
                        message_campaign_start_date,  
                        message_campaign_end_date,  
                        license_distribution_method_id,  
                        inclusive,  
                        license_seats,  
                        license_category_id,  
                        license_category_name,  
                        autorenewal_opt_id,  
                        language_code,  
                        location_code,  
                        cart_discount_id  
                    )  
                    SELECT DISTINCT  
                        m.message_campaign_id,  
                        m.message_campaign_name,  
                        cc.message_campaign_class_id,  
                        cc.message_campaign_class_name,  
                        m.message_campaign_start_date,  
                        m.message_campaign_end_date,  
                        d.license_distribution_method_id,  
                        d.inclusive,  
                        s.license_seats,  
                        c.license_category_id,  
                        lc.license_category_name,  
                        ar.autorenewal_opt_id,  
                        ll.language_code,  
                        ll.location_code,  
                        cd.cart_discount_id  
                    FROM dbo.message_campaign m  
                        INNER JOIN dbo.message_campaign_class cc  
                            ON cc.message_campaign_class_id = m.message_campaign_class_id  
                        INNER JOIN dbo.message_campaign_cart_discount cd  
                            ON m.message_campaign_id = cd.message_campaign_id  
                        INNER JOIN dbo.message_campaign_platform pf  
                            ON m.message_campaign_id = pf.message_campaign_id  
                        LEFT JOIN dbo.message_campaign_seat s  
                            ON m.message_campaign_id = s.message_campaign_id  
                        LEFT JOIN dbo.message_campaign_license_category c  
                            ON m.message_campaign_id = c.message_campaign_id  
                        LEFT JOIN dbo.license_category lc  
                            ON lc.license_category_id = c.license_category_id  
                        LEFT JOIN dbo.message_campaign_product_line pl  
                            ON m.message_campaign_id = pl.message_campaign_id  
                        LEFT JOIN dbo.message_campaign_autorenewal ar  
                            ON m.message_campaign_id = ar.message_campaign_id  
                        LEFT JOIN dbo.message_campaign_license_distribution_method d  
                            ON m.message_campaign_id = d.message_campaign_id  
                        LEFT JOIN dbo.message_campaign_language_location ll  
                            ON m.message_campaign_id = ll.message_campaign_id  
                    WHERE m.message_campaign_enabled = 1  
                        AND pf.message_platform_id = 8  
                        AND m.message_campaign_start_date <= @insert_date  
                        AND @insert_date < m.message_campaign_end_date;  
  
                    IF @debug_mode = 1 BEGIN  
                            SELECT '4.1.5' AS debug_message,  
                                *  
                            FROM @message_campaign_cart_discount;  
                        END;  
  
                    -- 4.1.5.1) update @item_table_discount  
                    UPDATE i  
                        SET discount = di.discount,  
                            cart_discount_method_id = di.cart_discount_method_id  
                    FROM @message_campaign_cart_discount c  
                        INNER JOIN dbo.cart_discount_item di  
                            ON c.cart_discount_id = di.cart_discount_id  
                                AND di.license_category_id = c.license_category_id  
                        INNER JOIN @item_table i  
                            ON c.license_seats = i.license_seats  
                                AND c.license_category_name = i.license_category_name  
                                AND di.product_type_id = i.product_type_id  
                    WHERE di.cart_discount_method_id = 3  
                        AND c.autorenewal_opt_id = ( CASE  
                                                        WHEN @license_attribute_license_value = 1 THEN 1  
                                                        ELSE 0  
                                                    END )  
                        AND ( c.license_distribution_method_id IS NULL  
                                OR c.license_distribution_method_id = @license_distribution_method_id )  
                        AND ( c.location_code IS NULL  
                            OR (  
                                c.language_code = @language_code  
                                    AND c.location_code = @location_code )  
                        );   
  
                    IF @debug_mode = 1 BEGIN  
                            SELECT '4.1.5.1' AS debug_message,  
                                *  
                            FROM @message_campaign_cart_discount;  
                        END;  
  
                    -- 4.1.6) update pricing  
                    UPDATE @item_table  
                        SET unit_price = CASE  
                                            --when @cart_discount_method_id = 1 then  
                                            WHEN cart_discount_method_id = 3 THEN unit_price - ROUND((discount / 100) * unit_price, 2)  
                                            ELSE unit_price  
                                        END  
                        WHERE discount IS NOT NULL;  
  
                    IF @debug_mode = 1 BEGIN  
                            SELECT '4.1.6' AS debug_message,  
                                *  
                            FROM @item_table;  
                        END;  
                              
                    -------------------------------------------------------------------------------------------------------------------------------------------------------------------  
                    -- x.) Business-only pricing  
                    -------------------------------------------------------------------------------------------------------------------------------------------------------------------  
  
                    -- 4.1.7) monthly utility  
                    IF @license_attribute_license_value IN ( 11, 12, 13 ) BEGIN  
  
                            -- 4.1.7.1) remove upsell in the case of utility  
                            DELETE   
                            FROM @item_table  
                            WHERE product_type_id NOT IN ( 1, 2 )  
                                AND @license_attribute_license_value = 12;  
  
                            IF @debug_mode = 1 BEGIN  
                                    SELECT '4.1.7.1' AS debug_message,  
                                        *  
                                    FROM @item_table;  
                                END;  
  
                            -- 4.1.7.2) update pricing  
                            UPDATE i  
                                SET unit_price = CASE  
                                                    WHEN @license_attribute_license_value = 12 THEN 0  
                                                    ELSE i.unit_price  
                                                END,  
                                    list_price = CASE  
                                                    WHEN @license_attribute_license_value = 12 THEN 0  
                                                    ELSE i.list_price  
                                                END  
                            FROM @item_table i;  
  
                            IF @debug_mode = 1 BEGIN  
                                    SELECT '4.1.7.2' AS debug_message,  
                                        *  
                                    FROM @item_table;  
                                END;  
  
                            -- 4.1.7.3) update usage_price  
                            UPDATE i  
                                SET usage_price = pp.usage_price  
                            FROM @item_table i  
                                INNER JOIN dbo.product_pricing pp  
                                    ON i.product_id = pp.product_id  
                            WHERE pp.location_code = @location_code  
                                AND pp.language_code = @language_code;  
  
                            IF @debug_mode = 1 BEGIN  
                                    SELECT '4.1.7.3' AS debug_message,  
                                        *  
                                    FROM @item_table;  
                                END;  
  
                            -- 4.1.7.4) apply tier discount model to usage_price when the SFDC Utility discount is selected  
                            UPDATE it  
                                SET it.usage_price = ROUND((1.00 - idp.discount / 100) * it.usage_price, 2)  
                            FROM @item_table it  
                                INNER JOIN @item_discount_profile idp  
                                    ON idp.item_id = it.item_id  
                                        AND idp.license_category_name = it.license_category_name  
                                        AND idp.product_type_id = it.product_type_id  
                                LEFT JOIN @CARBONITE_LICENSE_CATEGORIES clc  
                                    ON clc.license_category_name = it.license_category_name  
                            WHERE clc.license_category_name IS NULL;    --do not apply discount to carbonite orders  
  
                            IF @debug_mode = 1 BEGIN  
                                    SELECT '4.1.7.4' AS debug_message,  
                                        *  
                                    FROM @item_table;  
                                END;  
  
                        END;  
                END;  
        ELSE BEGIN  
  
                -- 4.2) partner  
  
                -- 4.2.1) annual  
                IF ( @license_attribute_id IS NULL  
                    OR @license_attribute_license_value IN ( 120, 220 ) ) BEGIN  
                        UPDATE @item_table  
                        SET list_price = t.retail_price,  
                            unit_price = CASE  
                                            WHEN t.pricing_term = 'monthly' THEN t.retail_price  
                                            WHEN t.pricing_term = 'annual' THEN ROUND( t.retail_price  
                                                                                        * ( CONVERT(FLOAT, DATEDIFF(dd, i.start_date, i.expiration_date))  
                                                                                            / 365.0 ),  
                                                                                        2  
                                                                                    )  
                                        END  
                        FROM @item_table i  
                            INNER JOIN dbo.license_category lc  
                                ON lc.license_category_name = i.license_category_name  
                            INNER JOIN dbo.partner_pricing_tier t  
                                ON t.license_category_id = lc.license_category_id  
                        WHERE t.pricing_status = 'active'  
                            AND t.currency_id = @currency_id  
                            AND t.partner_id = @partner_id  
                            AND t.site_id = @site_id  
                            AND t.low_range <= i.total_license_seats  
                            AND i.total_license_seats < t.high_range  
                            AND t.license_attribute_id IS NULL;  
  
                        IF @debug_mode = 1 BEGIN  
                                SELECT '4.2.1' AS debug_message,  
                                    *  
                                FROM @item_table;  
                            END  
                    END;  
  
                -- 4.2.2) monthly  
                ELSE BEGIN  
                        UPDATE i  
                            SET list_price = t.retail_price,  
                                unit_price = CASE  
                                                WHEN t.pricing_term = 'monthly' THEN t.retail_price  
                                                WHEN t.pricing_term = 'annual' THEN ROUND( t.retail_price  
                                                                                                * ( CONVERT(FLOAT, DATEDIFF(dd, i.start_date, i.expiration_date))  
                                                                                                    / 365.0 ),  
                                                                                                2 )  
                                            END  
                        FROM @item_table i  
                            INNER JOIN dbo.license_category lc  
                                ON lc.license_category_name = i.license_category_name  
                            INNER JOIN dbo.partner_pricing_tier t  
                                ON t.license_category_id = lc.license_category_id  
                        WHERE t.pricing_status = 'active'  
                            AND t.currency_id = @currency_id  
                            AND t.partner_id = @partner_id  
                            AND t.site_id = @site_id  
                            AND t.low_range <= i.total_license_seats  
                            AND i.total_license_seats < t.high_range  
                            AND t.license_attribute_id = @license_attribute_id;  
                    END;  
  
                IF @debug_mode = 1 BEGIN  
                        SELECT '4.2.2' AS debug_message,  
                            *  
                        FROM @item_table;  
                    END;  
  
            END;  
    END;  
  
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------  
    --RETURN  
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------  
  
    SELECT   
        line_item = i.item_id,  
        quantity = i.license_seats,  
        i.order_item_offer_amount,  
        equivalent_year_price = ( CASE  
                                        WHEN @campaign_id IS NOT NULL THEN CONVERT(MONEY, zp.renewal_price * i.years)  
                                        ELSE ( CASE  
                                                    WHEN pp.retail_price IS NULL  
                                                            OR i.years IS NULL THEN  
                                                        NULL  
                                                    ELSE  
                                                        CONVERT(MONEY, pp.retail_price * i.years)  
                                                END )  
                                    END ),  
        i.list_price,  
        i.unit_price,  
        i.usage_price,  
        i.product_id,  
        p.product_description,  
        i.product_type_id,  
        t.product_type_description,  
        i.license_keycode_type_id,  
        lc.license_category_id,  
        i.license_category_name,  
        lc.license_category_description,  
        f.product_family_description,  
        i.start_date,  
        i.expiration_date,  
        i.cart_item_bundle_id,  
        i.item_hierarchy_id,  
        dependent_cart_order_item_id = NULL,  
        keycode = @keycode,  
        i.discount,  
        i.cart_discount_method_id,  
        i.cart_discount_id,  
        i.storage_gb,  
        i.usage_pricing_model_id,  
        i.retention_model_id,       
        retention_term = JSON_VALUE(pe.product_extension_json,'$.retention_term'),  
        retention_model_name = JSON_VALUE(pe.product_extension_json,'$.retention_model_name'),  
        actual_storage_quantity = ( CASE WHEN i.usage_pricing_model_id = 2   
                                            AND i.storage_gb >= 1024 THEN CONVERT(DECIMAL(12,5),CONVERT(DECIMAL(12,5),i.storage_gb)/1024)  
                                        WHEN i.usage_pricing_model_id = 2   
                                            AND i.storage_gb < 1024 THEN 1.00000  
                                        ELSE 0   
                                    END )  
    FROM @item_table i  
        INNER JOIN dbo.product p  
            ON p.product_id = i.product_id  
        INNER JOIN dbo.product_family f  
            ON f.product_family_id = p.product_family_id  
        INNER JOIN dbo.product_type t  
            ON t.product_type_id = i.product_type_id  
        INNER JOIN dbo.license_category lc  
            ON lc.license_category_name = i.license_category_name  
        LEFT JOIN dbo.product_extension pe  
            ON pe.product_id = p.product_id  
        LEFT JOIN dbo.zuora_product_pricing zp  
            ON zp.product_id = p.product_id  
               AND zp.campaign_id = @campaign_id  
        OUTER APPLY dbo.fn_cart_select_one_year_products(i.product_id) oyp  
        LEFT JOIN dbo.product_pricing pp  
            ON pp.product_id = oyp.product_id  
               AND pp.language_code = @language_code  
               AND pp.location_code = @location_code;  
  
END TRY  
  
BEGIN CATCH  
  
    SET @response_code = -200;  
    SET @message = CASE  
                       WHEN @message IS NULL THEN 'execute dbo.usp_cart_select_license_configurator_pricing failed'  
                       ELSE @message  
                   END;  
  
    DECLARE @DBName NVARCHAR(128);  
    SET @DBName = DB_NAME();  
    EXECUTE dbo.usp_LogError @ErrorDB = @DBName;  
  
END CATCH;  