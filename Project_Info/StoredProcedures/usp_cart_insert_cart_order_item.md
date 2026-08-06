USE [ecommerce_VH14]
GO
/****** Object:  StoredProcedure [dbo].[usp_cart_insert_cart_order_item]    Script Date: 15-07-2026 11:18:17 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[usp_cart_insert_cart_order_item]
(
    @vendor_order_code VARCHAR(100),
    @item_json NVARCHAR(MAX),
    @bundle_json NVARCHAR(MAX),
    @response_code INT OUTPUT,
    @message VARCHAR(500) OUTPUT
)

/*
	DATE			AUTHOR			REMARKS
	2009-02-23		esmart			Initial creation.
	2013-12-18		mrobbins		rounding on unit
	2017-07-15		esmart			Refactor
	2017-11-01		esmart			Utility to annual
	2018-01-08		esmart			Consumer
	2018-07-19		cnovac			Added WIFI code and vendor_expiration_date
	2018-10-01		cnovac			Added code to override expiration date based on Apple orders
	2018-10-13		cnovac			Took out code to override expiration date based on Apple orders
	2018-10-19		esmart			Fix issue with keycode from second bundle being added to cart with existing new
	2018-10-23		esmart			Implement quantity
	2018-11-15		cnovac			Added isnull check to @order_item_update_type_id
	2018-11-16		esmart			Add usage_price for autobilling utility and overage
	2018-12-03		esmart			Fix issue with adding secondary on primary renewal
	2018-12-11		esmart			Fix online discounting
	2018-12-20		esmart			Use start_date and expiration on secondary product when explicit
	2019-01-23		acoffel			Storage and CD fix
	2019-03-04		esmart			Fix issues with renewals from monthly
	2019-04-02		esmart			Support multi-year annual in partner pricing
	2019-04-27		jnavarra		Compensate for leap year
	2019-06-05		esmart			Usage pricing and leap year support for partners
	2019-06-30		esmart			No upgrade items on monthly billing models
	2019-08-08		esmart			Add usage_pricing_model_id
	2019-09-12		cnovac			Added 5.6) to update the CD start/end dates in cart_order_item to match coresponding non-cd product
	2019-10-17		jnavarra		Change CD priority to use bundle first, then fallback to cart
	2019-10-25		esmart			Correct issue with WIFI renewal product_type
	2019-11-01		esmart			opportunity_line_item_id
	2019-11-25		esmart			Remove upgrade product insert for SFDC orders
	2019-12-03		cnovac			Added code to return out of proc if no unit_price is set for partner partner in 4.2
	2019-12-03		cnovac			Added to 2.1 to set start date to today if going from monthly billing to another monthly billing
	2019-12-04		esmart			Change O365 to OTSF
	2019-12-17		esmart			Add support for multiyear product from SFDC
	2019-12-11		wbarton			Added logic in 2.1 to set start_date and expiration_date in rare edge cases where licenses in ecom have an activation, but NULL expiration
	2019-12-17		wbarton			Adding a default case to 2.3. This prevents a scenario where hierarchy 2 products can have no product_type_id defined.
	2020-01-06		wbarton			Modified sections 4.1.2 and 4.1.7.4 to introduce unit_price and usage_price discount grandfathering via newly created discount models
	2020-02-14		wbarton			Refactoring 4.1.2 to support JSON operations in/out of usp_license_select_category_discount_model.
	2020-02-19		esmart			vault_id
	2020-06-29		cnovac			Updated code to find usage_price for overage/utility when ordered by partner
	2020-07-10		esmart			Add usage_pricing_model and product_pricing_level_id for pricing. New retention_model_id.
	2020-07-14		esmart			Add product_platform_id and cart_order_item_json to store new dimensions
	2020-08-19		jnavarra		Default secondary product start_date as expiration_date (fallback to primary product's new start_date)
	2020-09-09		esmart			Correct issues with duplicate cart_order_item_json
	2020-09-22		jnavarra		Correct previous secondary product start_date to account for upgrades
	2020-09-25		esmart			Add storage_gb and support for professional services
	2020-09-28		wbarton			Updating vault_id to int and adding support for an array of multiple vault_ids
	2020-10-14		cnovac			added logic for storage_gb
	2020-11-06		abelenkiy		Changed message_campaign_name in table variable from varchar(50) to varchar(65) to match message_campaign table (section 4.1.5)
									Hardcoded @source_caller input to usp_license_select_category_discount_model to avoid exceeding length limit (section 4.1.2)
	2020-11-10		abelenkiy		Setting order_type to WRCART for international upgrades (section 5.2)
	2020-11-10		jnavarra		Replace existing cart items with incoming if they match cart_item_bundle_id (attempt to prevent double-charge)
	2020-12-01		abelenkiy		AXIOM-2275 - Hotfix to use retail price for list price when discount value of 0 passed in explicitly (section 3.1.4)
	2020-12-21		esmart			Fix partner pricing issues on upgrade.
	2021-01-15		jnavarra		Update rounding precision (sections 3.1.4, 3.1.5, 4.1.6)
	2021-10-29		nprimo			Add logic to make retention_id=2 mean '7Years, after the retention_model update of retention_model_id=7:'7 Years'
	2021-11-11		jberry			Update years to 0 for a retention model upgrade (section 2.2.2)
	2021-11-18		nprimo			Added 1.14.1 to have a default prodduct_platform_id of 3=On-Prem
	2021-12-08		psatish			Added Perpetual billing model
	2021-12-18		psatish			Commented the Retention model update
	2021-12-29		psatish			Commented the section 1.14.1 and set the Productplatform as 1 for CEP orders 
	2022-01-06		gblandford		SMCI-6214 - Add retention_term handling, add 'CBSB' to 'OTSF' license_category criteria
	2022-05-05		abelenkiy		Added handling of item_total being provided for SFDC orders; enhanced error handling
	2022-05-16      psatish			ALM# 990 Save the sap_material_number in the cart_order_item table
	2022-05-26		abelenkiy		Added license_seats to unit override in section 1.4.2 so original input seat value is retained in section 1.6 (ALM-1385)
	2022-07-07		jberry			Updated license_seats in unit override in section 1.5 so license seats reflect the actual qty being purchased (ALM-1497)
	2023-02-08		gblandford		https://jira.opentext.com/browse/ECOM-143 - section 2.1.1 
									- address incorrect license_effective_object license_attibute_license_value on secondary product for renewal orders
									- this issue manifests itself when the billig model is switched, and the fix relates specifcally to that, but may manifest elsewhere
	2023-03-31		psatish			ECOM-899 Save the storage_GB from the JSON if an order comes through salesforce in the cart_order_item table 
									else use the existing logic from this function:fn_get_item_storage_gb
	2023-06-07		schindar		Ecom-1146. 	3.1.5) - removing filter for storage_gb not null
	2023-06-12		psatish			ECOM-1542 Modified the section:5.3.1 to update the unit_price for Consumer Orders
	2023-08-28		jberry			ECOM-1181 Added amended_contract param to ignore unit override logic for amended upsell orders to section 1.6	
	2023-09-19		jberry			ECOM-2220 - Modified sec 4.1 to fix discount logic to allow percentage off and total off discounts from CSI for business products
	2024-09-18		mgolla			ECOM-5196 - Modified 4.1.7.1 section , Enabling upsell for the Pillr Utility product from CSI.
	2024-09-29      lpraharsha      ECOM-5198- modified section 4, pricing details for pillr products in csi cart.
	2025-04-22      psatish			US-4727275 Modified the section: 5.3.3 to add vault_array in the select clause to support multiple vaults
	2025-05-30		mgolla			US-4220862 - I have added conditions in section 1.3 and 4.1.1 that for Pillr products, if it's a monthly utility, the price should display as 0. 
	2025-07-15		jberry			US-4396390 - added sap_material_number as a parameter for calls to fn_product_select_profile
	2025-09-24		jberry			US-4833298 Added storage_gb and retention to @item_discount_json
						            - Updated sec 4.1.4.1 so the discount will be populated on cart_order_item
						            - Added section 2.3.3 to calculate storage value for upgrades
						            - Added logic to 4.1.7.4 to ignore discounts for licenses with a usage_pricing model (carbonite)
	2025-10-14		jberry			US-5004939 - Added actual_storage_quantity.  This field converts storage_gb to TBs to use for pricing calculations for storage products
	          		      			- Added logic in sec 5.5 to to handle storage in cart order totals calculation
	2025-11-14		jberry			US-5076905 - Modified sec 4.1.2 to return sap_material_number in @item_discount_profile and populate it on cart_order_item
	2025-11-19		jberry			US-5093883 - Modified sec 2.2.1 to block upgrades from being added to an order for carbonite renewals (mid-term upgrades)
	2025-11-25		psatish			D-5042530 Added section 1.9.1.1 to set up the license_attribute_license_value(billing model) for a business keycode if it's NULL in the bundle JSON
	2026-04-01      psatish			US-5283106 Added section 1.4.2 and modified section 1.9.1.2 to set up the license_attribute_license_value(billing model) to Overage 
									for USA customers when the value is null
	2026-06-17		psatish			D-5401642 Modified the section 1.9.1.2 to update the billing model for US orders

	DESCRIPTION
	Populating data into cart_order_item table

	DEPENDENCIES:
	usp_license_bulk_load_insert_wifi_licenses
	usp_netsuite_order_license_insert
	Also Called from PHP

*/

AS
SET NOCOUNT ON

BEGIN TRY

    DECLARE @cart_order_id INT,
            @locale VARCHAR(5),
            @language_code VARCHAR(2),
            @location_code VARCHAR(3),
            @keycode VARCHAR(40),
            @license_id INT,
            @license_category_name VARCHAR(10),
            @license_category_name_cd VARCHAR(10),
            @license_seats INT,
            @storage_gb INT,
            @years DECIMAL(18, 3),
            @cart_discount_id INT,
            @license_distribution_method_id INT,
            @license_attribute_id INT,
            @license_attribute_license_value INT,
            @license_keycode_type_id INT,
            @partner_id INT,
            @site_id VARCHAR(20),
            @order_item_update_type_id TINYINT,
            @insert_date DATETIME,
            @insert_by VARCHAR(20),
            @max_line_item INT,
            @currency_id TINYINT,
            @next_process_date DATETIME,
            @product_line_id INT,
            @percent_discount FLOAT,
            @product_type_id INT,
            @pricing_term VARCHAR(10),
            @product_pricing_level_id TINYINT,
            @cart_order_item_json_log_id INT,
			@has_utility INT=0
			

    DECLARE @item_table TABLE
    (
        item_id INT IDENTITY(1, 1),
        license_category_name VARCHAR(10),
        quantity INT,
        license_seats INT,
        total_license_seats INT,
        storage_gb INT,
        years DECIMAL(18, 3),
        license_keycode_type_id INT,
        start_date DATETIME,
        expiration_date DATETIME,
        vendor_order_item_code VARCHAR(36),
        cart_item_bundle_id INT,
        item_hierarchy_id TINYINT,
        product_id INT,
        product_type_id INT,
        order_item_offer_amount MONEY
            DEFAULT 0,
        list_price MONEY
            DEFAULT 0,
        unit_price MONEY
            DEFAULT 0,
        item_total MONEY,
        discount FLOAT,
        cart_discount_method_id TINYINT,
        cart_discount_id INT,
        vendor_expiration_date DATE,
        usage_price MONEY
            DEFAULT 0,
        opportunity_line_item_id VARCHAR(18),
        vault_id INT,
        vault_array NVARCHAR(MAX),
        usage_pricing_model_id TINYINT,
        retention_model_id TINYINT,
        retention_term TINYINT,
        product_platform_id TINYINT,
        line_item INT,
        sap_material_number INT,
        amended_contract VARCHAR(18),
        actual_storage_quantity DECIMAL(12,5),
		license_category_id INT
    )

    DECLARE @license_table TABLE
    (
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
        autorenewal_cycle DECIMAL(18, 3),
        usage_pricing_model_id TINYINT,
        retention_model_id TINYINT,
        retention_term TINYINT,
        product_platform_id TINYINT
    )

    DECLARE @unit_override TABLE
    (
        cart_order_id INT,
        item_id INT,
        unit_price MONEY,
        usage_price MONEY,
        item_total MONEY,
        license_seats INT,
        license_category_name VARCHAR(10)
    )
    DECLARE @PILLR_LICENSE_CATEGORY TABLE
    (
        license_category_name VARCHAR(10)
    )

	 DECLARE @UTILITY_BILLING_MODELS TABLE
    (
        license_attribute_license_value INT

    )

    DECLARE @CARBONITE_LICENSE_CATEGORIES TABLE
    (
        license_category_id INT,
		license_category_name VARCHAR(10)
    )

	DECLARE @DEFAULT_BUSINESS_BILLING_MODEL TABLE
	(
		license_attribute_id INT,
		license_attribute_license_value INT
	)

    INSERT INTO @PILLR_LICENSE_CATEGORY
    (
        license_category_name
    )
    SELECT f.[key]
    FROM [dbo].[fn_app_config_select_key_values]('PILLR_LICENSE_CATEGORIES', 'PILLR') f

	INSERT INTO @UTILITY_BILLING_MODELS
	(
	    license_attribute_license_value
	)
	 SELECT f.[key]
    FROM [dbo].[fn_app_config_select_key_values]('UTILITY_BILLING_MODELS', 'GENERAL') f
    SELECT @insert_date = GETDATE(),
           @insert_by = SUSER_SNAME()

    INSERT INTO @CARBONITE_LICENSE_CATEGORIES
    (
        license_category_id,
		license_category_name
    )
    SELECT f.[key],
			f.value
    FROM [dbo].[fn_app_config_select_key_values]('CARBONITE_LICENSE_CATEGORIES', 'CARBONITE') f

	INSERT INTO @DEFAULT_BUSINESS_BILLING_MODEL
	(
		license_attribute_id,
		license_attribute_license_value
	)
	SELECT la.license_attribute_id,
		   la.license_attribute_license_value
	FROM [dbo].[fn_app_config_select_key_values]('DEFAULT_BUSINESS_BILLING_MODEL', 'GENERAL')
		INNER JOIN dbo.license_attribute_license_value la
			ON la.license_attribute_license_value = [key];

    SELECT @insert_date = GETDATE(),
           @insert_by = SUSER_SNAME()

    -------------------------------------------------------------------------------------------------------------------------------------------------------------------
    -- 1.) select
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------

    -- 1.1) @cart_order_id
    SELECT @cart_order_id = cart_order_id,
           @currency_id = currency_id
    FROM dbo.cart_order
    WHERE vendor_order_code = @vendor_order_code

    -- 1.2) @locale
    SELECT @locale = co.locale,
           @site_id = co.site_id,
           @partner_id = cp.partner_id
    FROM dbo.cart_order co
        LEFT JOIN cart_order_partner cp
            ON cp.cart_order_id = co.cart_order_id
    WHERE co.cart_order_id = @cart_order_id

    -- 1.2.1) @language_code and @location_code
    SELECT @language_code = language_code,
           @location_code = location_code
    FROM dbo.fn_locale_to_lang_loc(@locale)

    -- 1.3) @bundle_json
    SELECT @keycode = CASE
                          WHEN keycode = '' THEN
                              NULL
                          ELSE
                              keycode
                      END,
           @license_attribute_license_value = license_attribute_license_value,
           @license_keycode_type_id = license_keycode_type_id,
           @order_item_update_type_id = CASE
                                            WHEN order_item_update_type_id IS NULL THEN
                                                1
                                            ELSE
                                                order_item_update_type_id
                                        END,
           @cart_discount_id = cart_discount_id,
           @product_pricing_level_id = product_pricing_level_id
    FROM
        OPENJSON(@bundle_json)
        WITH
        (
            keycode VARCHAR(40) '$.keycode',
            license_attribute_license_value INT '$.license_attribute_license_value',
            license_keycode_type_id INT '$.license_keycode_type_id',
            order_item_update_type_id TINYINT '$.order_item_update_type_id',
            message_key VARCHAR(36) '$.message_key',
            cart_discount_id INT '$.cart_discount_id',
            product_pricing_level_id TINYINT '$.product_pricing_level_id'
        )

		IF EXISTS
			(
			    SELECT 1
			    FROM @UTILITY_BILLING_MODELS AS ubm
			    WHERE ubm.license_attribute_license_value = @license_attribute_license_value
			)
		 BEGIN
		     SET @has_utility = 1
		 END

    -- 1.3.1) @license_id
    IF @keycode IS NOT NULL
    BEGIN
        SELECT @license_id = license_id,
               @license_keycode_type_id = CASE
                                              WHEN @license_keycode_type_id IS NULL THEN
                                                  license_keycode_type_id
                                              ELSE
                                                  @license_keycode_type_id
                                          END,
               @license_distribution_method_id = license_distribution_method_id,
               @product_line_id = product_line_id
        FROM dbo.license
        WHERE keycode = @keycode

    END

    -- 1.3.2) @license_attribute_id
    SELECT @license_attribute_id = license_attribute_id
    FROM dbo.license_attribute_license_value
    WHERE license_attribute_license_value = @license_attribute_license_value

    -- 1.3.3) next monthly process_date
    SELECT @next_process_date = process_date
    FROM dbo.license_message
    WHERE message_type_id = 10
          AND message_status_id = 1
          AND license_id = @license_id

    -- 1.4) @item_table
    INSERT INTO @item_table
    (
        license_category_name,
        quantity,
        license_seats,
        total_license_seats,
        storage_gb,
        years,
        license_keycode_type_id,
        start_date,
        expiration_date,
        vendor_order_item_code,
        cart_item_bundle_id,
        item_hierarchy_id,
        discount,
        cart_discount_method_id,
        vendor_expiration_date,
        usage_pricing_model_id,
        opportunity_line_item_id,
        unit_price,
        item_total,
        usage_price,
        vault_id,
        vault_array,
        retention_model_id,
        retention_term,
        product_platform_id,
        product_id,
        sap_material_number,
        amended_contract
    )
    SELECT license_category_name,
           quantity,
           license_seats,
           license_seats,
           storage_gb,
           years,
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
           cart_item_bundle_id,
           item_hierarchy_id,
           discount,
           cart_discount_method_id,
           vendor_expiration_date,
           usage_pricing_model_id,
           opportunity_line_item_id,
           unit_price,
           item_total,
           usage_price,
           vault_id,
           vault,
           retention_model_id,
           retention_term,
           product_platform_id,
           product_id,
           sap_material_number,
           amended_contract = CASE
                                  WHEN amended_contract = '' THEN
                                      NULL
                                  ELSE
                                      amended_contract
                              END
    FROM
        OPENJSON(@item_json)
        WITH
        (
            license_category_name VARCHAR(10) '$.license_category_name',
            quantity INT '$.quantity',
            license_seats INT '$.license_seats',
            license_seats INT '$.license_seats',
            storage_gb INT '$.storage_gb',
            years DECIMAL(18, 3) '$.years',
            license_keycode_type_id INT '$.license_keycode_type_id',
            locale VARCHAR(5) '$.locale',
            license_attribute_license_value INT '$.license_attribute_license_value',
            start_date DATETIME '$.start_date',
            expiration_date DATETIME '$.expiration_date',
            cart_item_bundle_id INT '$.cart_item_bundle_id',
            item_hierarchy_id INT '$.item_hierarchy_id',
            vendor_order_item_code VARCHAR(36) '$.vendor_order_item_code',
            discount FLOAT '$.discount',
            cart_discount_method_id TINYINT '$.cart_discount_method_id',
            vendor_expiration_date DATE '$.vendor_expiration_date',
            usage_pricing_model_id TINYINT '$.usage_pricing_model_id',
            opportunity_line_item_id VARCHAR(18) '$.opportunity_line_item_id',
            unit_price MONEY '$.unit_price',
            item_total MONEY '$.item_total',
            usage_price MONEY '$.usage_price',
            vault_id INT '$.vault_id',
            vault NVARCHAR(MAX) AS JSON,
            retention_model_id TINYINT '$.retention_model_id',
            retention_term TINYINT '$.retention_term',
            product_platform_id TINYINT '$.product_platform_id',
            product_id INT '$.product_id',
            sap_material_number INT '$.sap_material_number',
            amended_contract VARCHAR(18) '$.amended_contract'
        )

    -- 1.4.1.1) Remove the option to pick 7 years for OTSF licenses as they won't be sold through partnercart
    IF EXISTS
    (
        SELECT *
        FROM @item_table i
        WHERE i.license_category_name = 'OTSF'
              AND i.retention_model_id = 7
    )
    BEGIN

        SELECT @response_code = -1,
               @message = 'No product unit price found in partner_pricing_tier'
        RETURN
    END
    --Commented the Retention model update
    -- 1.4.1.2) Switch retention_id=2 to retention_id=2, to comply with partner_api using retention_id=2 as '7 Years'
    /*update i set i.retention_model_id = 7
			from @item_table i
			where i.license_category_name = 'OTSF' and i.retention_model_id = 2

			if @@rowcount = 1
			 begin
			
				set @item_json = replace(@item_json, '"retention_model_id":"2"', '"retention_model_id":"7"')
			end
			*/
	--1.4.2 update license_category_id in the @item_json table
	UPDATE it
	SET it.license_category_id = ct.license_category_id
	FROM @item_table it
		INNER JOIN dbo.license_category ct
			ON ct.license_category_name = it.license_category_name;
	
    -- 1.5) @license_table
    IF @license_id IS NOT NULL
    BEGIN
        INSERT INTO @license_table
        (
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
            autorenewal_cycle,
            usage_pricing_model_id,
            retention_model_id,
            retention_term,
            product_platform_id
        )
        SELECT license_id,
               license_category_name,
               license_seats,
               storage_gb,
               license_keycode_type_id,
               license_attribute_license_value,
               start_date,
               expiration_date,
               category_type_name,
               item_hierarchy_id,
               autorenewal_cycle,
               usage_pricing_model_id,
               retention_model_id,
               retention_term,
               product_platform_id
        FROM dbo.fn_license_select_license_profile(@license_id)
    END

    -- 1.5.1) unit override
    IF @site_id = 'SFDC'
    BEGIN
        INSERT INTO @unit_override
        (
            cart_order_id,
            item_id,
            unit_price,
            usage_price,
            item_total,
            license_seats,
            license_category_name
        )
        SELECT @cart_order_id,
               i.item_id,
               i.unit_price,
               i.usage_price,
               i.item_total,
               i.license_seats - l.license_seats,
               i.license_category_name
        FROM @item_table i
            LEFT JOIN @license_table l
                ON l.license_category_name = i.license_category_name
        WHERE i.unit_price IS NOT NULL
    END

    -- 1.6) upgrade license_seats
    UPDATE @item_table
    SET license_seats = ISNULL(uo.license_seats, i.license_seats - l.license_seats)
    FROM @item_table i
        INNER JOIN @license_table l
            ON l.license_category_name = i.license_category_name
        LEFT JOIN @unit_override uo
            ON uo.license_category_name = i.license_category_name
    WHERE i.years = 0
          AND i.amended_contract IS NULL

    -- 1.7) monthly to annual
    UPDATE @item_table
    SET start_date = @next_process_date,
        product_type_id = 2
    FROM @item_table i
        INNER JOIN @license_table l
            ON l.license_category_name = i.license_category_name
    WHERE l.license_attribute_license_value IN ( 12, 110, 111, 112, 210, 211, 212, 13, 113, 213 )
          AND @license_attribute_license_value IN ( 20, 120, 220 )

    -- 1.7.1) monthly to annual secondary
    UPDATE i2
    SET start_date = @next_process_date,
        product_type_id = 2
    FROM @item_table i
        INNER JOIN @license_table l
            ON l.license_category_name = i.license_category_name
        INNER JOIN @item_table i2
            ON i2.cart_item_bundle_id = i.cart_item_bundle_id
               AND i2.item_hierarchy_id = 2
    WHERE l.license_attribute_license_value IN ( 12, 110, 111, 112, 210, 211, 212, 13, 113, 213 )
          AND @license_attribute_license_value IN ( 20, 120, 220 )

    -- 1.8) existing item
    DECLARE @existing_item_table TABLE
    (
        cart_order_item_id INT,
        license_category_name VARCHAR(10),
        cart_item_bundle_id TINYINT,
        item_hierarchy_id TINYINT,
        start_date DATETIME,
        expiration_date DATETIME,
        license_attribute_id INT,
        license_attribute_license_value INT
    )

    INSERT INTO @existing_item_table
    (
        cart_order_item_id,
        license_category_name,
        cart_item_bundle_id,
        item_hierarchy_id,
        start_date,
        expiration_date,
        license_attribute_id,
        license_attribute_license_value
    )
    SELECT i.cart_order_item_id,
           lc.license_category_name,
           i.cart_item_bundle_id,
           i.item_hierarchy_id,
           i.start_date,
           i.expiration_date,
           v.license_attribute_id,
           v.license_attribute_license_value
    FROM dbo.cart_order_item i
        INNER JOIN dbo.product_license_category plc
            ON plc.product_id = i.product_id
        INNER JOIN dbo.license_category lc
            ON lc.license_category_id = plc.license_category_id
        LEFT JOIN dbo.license_attribute_license_value v
            ON i.license_attribute_license_value = v.license_attribute_license_value
    WHERE i.cart_order_id = @cart_order_id

    -- 1.9) @product_line_id
    IF @product_line_id IS NULL
    BEGIN
        SELECT @product_line_id = pl.product_line_id
        FROM @item_table i
            INNER JOIN dbo.license_category lc
                ON lc.license_category_name = i.license_category_name
            INNER JOIN dbo.license_category_product_line pl
                ON pl.license_category_id = lc.license_category_id
                   AND pl.language_code = @language_code
                   AND pl.location_code = @location_code
    END

    -- 1.9.1) @product_line_id from @existing_item_table
    IF @product_line_id IS NULL
    BEGIN
        SELECT @product_line_id = pl.product_line_id
        FROM @existing_item_table i
            INNER JOIN dbo.license_category lc
                ON lc.license_category_name = i.license_category_name
            INNER JOIN dbo.license_category_product_line pl
                ON pl.license_category_id = lc.license_category_id
                   AND pl.language_code = @language_code
                   AND pl.location_code = @location_code
    END

	

	-- 1.9.1.2 set up @license_attribute_license_value for a business keycode if it is null
	IF EXISTS
	(
		SELECT 1
		FROM [dbo].[fn_app_config_select_key_values]('BUSINESS_PRODUCT_LINE', 'GENERAL')
		WHERE [key] = @product_line_id
	)
	BEGIN

		--Set up @license_attribute_license_value to Overage for the USA cutsomers
		--Begins
		IF EXISTS
		(
			SELECT 1
			FROM @item_table it
				INNER JOIN dbo.license_category_product_line pl
					ON pl.license_category_id = it.license_category_id
				INNER JOIN dbo.license_category_product_line_license_attribute_license_value lav
					ON pl.license_category_product_line_id = lav.license_category_product_line_id
			WHERE pl.product_line_id = @product_line_id
				  AND pl.location_code = @location_code
		)
		BEGIN

			SELECT @license_attribute_license_value = lv.license_attribute_license_value,
				   @license_attribute_id = lv.license_attribute_id
			FROM @item_table it
				INNER JOIN dbo.license_category_product_line pl
					ON pl.license_category_id = it.license_category_id
				INNER JOIN dbo.license_category_product_line_license_attribute_license_value lav
					ON pl.license_category_product_line_id = lav.license_category_product_line_id
				INNER JOIN dbo.license_attribute_license_value lv
					ON lv.license_attribute_license_value = lav.license_attribute_license_value
			WHERE pl.product_line_id = @product_line_id
				  AND pl.location_code = @location_code
				  AND
				  (
					  @license_attribute_license_value IS NULL
					  OR EXISTS
			(
				SELECT 1
				FROM @DEFAULT_BUSINESS_BILLING_MODEL
				WHERE license_attribute_license_value = @license_attribute_license_value
			)
				  );
		END;
		--Ends
		ELSE 
		BEGIN
			SELECT @license_attribute_license_value = ISNULL(@license_attribute_license_value,license_attribute_license_value),
				   @license_attribute_id = ISNULL(@license_attribute_id,license_attribute_id)
			FROM @DEFAULT_BUSINESS_BILLING_MODEL;
		END;
	END;
    -- 1.9.2)
    IF @product_line_id IN ( 1, 6 )
    BEGIN
        SELECT @product_line_id = CASE
                                      WHEN @product_line_id = 1 THEN
                                          100
                                      WHEN @product_line_id = 6 THEN
                                          200
                                  END
    END

    -- 1.10) down rev category fix
    UPDATE @item_table
    SET license_category_name = CASE
                                    WHEN license_category_name IN ( 'AD' ) THEN
                                        'ADP'
                                    WHEN license_category_name IN ( 'WAV', 'SS' ) THEN
                                        'WSAV'
                                    WHEN license_category_name IN ( 'WISE', 'WSAE' ) THEN
                                        'WSAI'
                                    WHEN license_category_name = 'WISC' THEN
                                        'WSAC'
                                END
    WHERE license_category_name IN ( 'SS', 'WAV', 'WISC', 'WISE', 'WSAE', 'AD' )

    UPDATE @license_table
    SET license_category_name = CASE
                                    WHEN license_category_name IN ( 'WAV', 'SS' ) THEN
                                        'WSAV'
                                    WHEN license_category_name IN ( 'WISE', 'WSAE' ) THEN
                                        'WSAI'
                                    WHEN license_category_name = 'WISC' THEN
                                        'WSAC'
                                END
    WHERE license_category_name IN ( 'SS', 'WAV', 'WISC', 'WISE', 'WSAE' )

    -- 1.11) get attribute data from primary
    IF @license_attribute_license_value IS NULL
    BEGIN
        SELECT @license_attribute_id = e.license_attribute_id,
               @license_attribute_license_value = e.license_attribute_license_value
        FROM @existing_item_table e
        WHERE e.item_hierarchy_id = 1
    END

    -- 1.12) usage_pricing_model
    UPDATE @item_table
    SET usage_pricing_model_id = l.usage_pricing_model_id
    FROM @item_table i
        INNER JOIN @license_table l
            ON i.license_category_name = l.license_category_name
    WHERE i.usage_pricing_model_id IS NULL
          AND l.usage_pricing_model_id IS NOT NULL

    IF @partner_id IS NOT NULL
    BEGIN
        UPDATE @item_table
        SET usage_pricing_model_id = ISNULL(m.usage_pricing_model_id, 1)
        FROM @item_table i
            INNER JOIN dbo.license_category lc
                ON i.license_category_name = lc.license_category_name
            INNER JOIN dbo.partner_usage_pricing_model m
                ON lc.license_category_id = m.license_category_id
        WHERE m.partner_id = @partner_id
              AND m.site_id = @site_id
              AND i.usage_pricing_model_id IS NULL
    END
    ELSE
    BEGIN
        UPDATE @item_table
        SET usage_pricing_model_id = 1
        WHERE license_category_name IN ( 'OTSF', 'CBEP' )
              AND usage_pricing_model_id IS NULL
    END

    -- 1.13) retention_model
    UPDATE @item_table
    SET retention_model_id = l.retention_model_id,
        retention_term = l.retention_term
    FROM @item_table i
        INNER JOIN @license_table l
            ON i.license_category_name = l.license_category_name
    WHERE i.retention_model_id IS NULL
          AND l.retention_model_id IS NOT NULL

    IF @partner_id IS NOT NULL
    BEGIN
        UPDATE @item_table
        SET retention_model_id = ISNULL(m.retention_model_id, 1) -- default values for 1 year retention
        FROM @item_table i
            INNER JOIN dbo.license_category lc
                ON i.license_category_name = lc.license_category_name
            INNER JOIN dbo.partner_retention_model m
                ON lc.license_category_id = m.license_category_id
        WHERE m.partner_id = @partner_id
              AND m.site_id = @site_id
              AND i.retention_model_id IS NULL
    END
    ELSE
    BEGIN
        UPDATE @item_table
        SET retention_model_id = 1, -- default values for 1 year retention
            retention_term = 1
        WHERE license_category_name IN ( 'OTSF', 'CBSB' )
              AND retention_model_id IS NULL
    END

    -- 1.14) product_platform
    UPDATE @item_table
    SET product_platform_id = l.product_platform_id
    FROM @item_table i
        INNER JOIN @license_table l
            ON i.license_category_name = l.license_category_name
    WHERE i.product_platform_id IS NULL
          AND l.product_platform_id IS NOT NULL

    IF @partner_id IS NOT NULL
    BEGIN
        UPDATE @item_table
        SET product_platform_id = ISNULL(m.product_platform_id, 1)
        FROM @item_table i
            INNER JOIN dbo.license_category lc
                ON i.license_category_name = lc.license_category_name
            INNER JOIN dbo.partner_product_platform m
                ON lc.license_category_id = m.license_category_id
        WHERE m.partner_id = @partner_id
              AND m.site_id = @site_id
              AND i.product_platform_id IS NULL
    END
    ELSE
    BEGIN

        ----1.14.1) If the order came from SFDC and the product is CBEP set the product_platform_id to 3
        --update @item_table
        --set product_platform_id = (case when @site_id = 'SFDC' then 3 else 1 end)
        --where license_category_name in ('CBEP') and product_platform_id is null
        UPDATE @item_table
        SET product_platform_id = 1
        WHERE license_category_name = 'CBEP'
              AND product_platform_id IS NULL
    END

    -- 1.15) storage_gb
    UPDATE @item_table
    SET storage_gb = dbo.fn_get_item_storage_gb(
                                                   quantity,
                                                   license_category_name,
                                                   DEFAULT,
                                                   usage_pricing_model_id,
                                                   DEFAULT,
                                                   DEFAULT
                                               )
    WHERE storage_gb IS NULL

    -------------------------------------------------------------------------------------------------------------------------------------------------------------------
    -- 2.) product set
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------

    -- 2.1) primary start_date and expiration_date
    UPDATE @item_table
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
                              AND l.license_attribute_license_value IN ( 12, 110, 111, 112, 210, 211, 212 ) THEN
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
                              AND
                              (
                                  l.expiration_date IS NULL
                                  OR l.expiration_date < GETDATE()
                              ) THEN
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
                                   AND
                                   (
                                       l.expiration_date IS NULL
                                       OR l.expiration_date < GETDATE()
                                   ) THEN
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
    WHERE i.item_hierarchy_id = 1


    -- 2.1.1) secondary start_date and expiration_date
    UPDATE i
    SET i.start_date = CASE
                           WHEN ISNULL(l.license_attribute_license_value, @license_attribute_license_value) <> @license_attribute_license_value THEN
                               i2.start_date -- billing model switched, use primary 
                           WHEN i2.start_date IS NULL
                                AND i.start_date IS NOT NULL THEN
                               i.start_date  -- pure upsell
                           WHEN i2.start_date IS NULL
                                AND i.years = 0 THEN
                               CONVERT(DATE, GETDATE())
                           WHEN i2.start_date IS NULL
                                AND l.category_type_name = 'trial'
                                AND i.years > 0 THEN
                               CONVERT(DATE, GETDATE())
                           WHEN i2.start_date IS NULL
                                AND i.years > 0 THEN
                               ISNULL(l.expiration_date, CONVERT(DATE, GETDATE()))
                           WHEN i2.start_date IS NULL
                                AND e.start_date IS NOT NULL THEN
                               e.start_date
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
                                WHEN i2.expiration_date IS NULL
                                     AND i.expiration_date IS NOT NULL THEN
                                    i.expiration_date  -- pure upsell
                                WHEN i2.expiration_date IS NOT NULL THEN
                                    i2.expiration_date -- expiration from primary product
                                WHEN e.expiration_date IS NOT NULL THEN
                                    e.expiration_date  -- expiration from primary product already in cart_order_item
                                WHEN i2.expiration_date IS NULL THEN
                                    x.expiration_date  -- upgrade of secondary only
                                                       --else i2.expiration_date -- TODO: need a default?
                            END
    FROM @item_table i
        LEFT JOIN @license_table l
            ON l.license_category_name = i.license_category_name
        LEFT JOIN @item_table i2
            ON i2.cart_item_bundle_id = i.cart_item_bundle_id
               AND i2.item_hierarchy_id = 1
        LEFT JOIN @existing_item_table e
            ON e.cart_item_bundle_id = i.cart_item_bundle_id
               AND e.item_hierarchy_id = 1
        LEFT JOIN
        (
            SELECT license_id,
                   expiration_date = MAX(expiration_date)
            FROM @license_table
            GROUP BY license_id
        ) x
            ON x.license_id = @license_id
    WHERE i.item_hierarchy_id = 2

    --override the expriation date if WIFI since it's set by Apple or Google
    UPDATE @item_table
    SET expiration_date = ISNULL(vendor_expiration_date, expiration_date)
    WHERE license_category_name = 'WIFI'
          AND item_hierarchy_id = 1

    -- 2.2) primary product_type
    UPDATE @item_table
    SET product_type_id = CASE
                              WHEN l.license_id IS NULL THEN
                                  1 -- new
                              WHEN l.license_id IS NOT NULL
                                   AND l.category_type_name = 'trial' THEN
                                  1 -- trial conversion
                              WHEN l.license_id IS NOT NULL
                                   AND l.category_type_name = 'full'
                                   AND i.expiration_date > l.expiration_date THEN
                                  2
                              WHEN l.license_id IS NOT NULL
                                   AND l.category_type_name = 'full'
                                   AND l.license_category_name = 'WIFI'
                                   AND CAST(i.expiration_date AS DATE) > CAST(l.expiration_date AS DATE) THEN
                                  2
                              WHEN l.license_id IS NOT NULL
                                   AND l.category_type_name = 'full'
                                   AND l.license_category_name = 'WIFI'
                                   AND
                                   (
                                       i.license_seats > l.license_seats
                                       OR i.years > l.autorenewal_cycle
                                   ) THEN
                                  3
                              WHEN l.license_id IS NOT NULL
                                   AND l.category_type_name = 'full'
                                   AND i.expiration_date = l.expiration_date
                                   AND
                                   (
                                       i.license_category_name <> l.license_category_name
                                       OR i.total_license_seats > l.license_seats
                                   ) THEN
                                  3
                              ELSE
                                  2 -- when no match add as renewal
                          END
    FROM @item_table i
        LEFT JOIN @license_table l
            ON l.item_hierarchy_id = i.item_hierarchy_id
    WHERE i.item_hierarchy_id = 1
          AND i.product_type_id IS NULL

    -- 2.2.1) upgrade
    IF @product_line_id IN ( 100, 200 )
    BEGIN
        INSERT INTO @item_table
        (
            license_category_name,
            quantity,
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
        SELECT i.license_category_name,
               i.quantity,
               l.license_seats,
               i.total_license_seats,
               i.storage_gb,
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
              AND
              (
                  i.license_category_name <> l.license_category_name
                  OR i.license_seats > l.license_seats
              )
              AND l.expiration_date > GETDATE()
              AND i.license_category_name != 'WIFI'
    END
    ELSE
    BEGIN
        INSERT INTO @item_table
        (
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
            usage_pricing_model_id,
            retention_model_id,
            retention_term,
            product_platform_id
        )
        SELECT i.license_category_name,
               i.license_seats - l.license_seats,
               i.total_license_seats,
               i.storage_gb,
               0,
               i.license_keycode_type_id,
               CONVERT(DATE, GETDATE()),
               l.expiration_date,
               i.cart_item_bundle_id,
               i.item_hierarchy_id,
               product_type_id = 3,
               i.usage_pricing_model_id,
               i.retention_model_id,
               i.retention_term,
               i.product_platform_id
        FROM @item_table i
            INNER JOIN @license_table l
                ON l.item_hierarchy_id = i.item_hierarchy_id
            LEFT JOIN @CARBONITE_LICENSE_CATEGORIES clc
                ON clc.license_category_name = i.license_category_name
        WHERE i.item_hierarchy_id = 1
              AND i.product_type_id = 2
              AND
              (
                  i.license_category_name <> l.license_category_name
                  OR i.license_seats > l.license_seats
              )
              AND l.expiration_date > GETDATE()
              AND clc.license_category_name IS null
              AND
              (
                  @license_attribute_license_value NOT IN ( 12, 110, 111, 112, 210, 211, 212, 13, 213, 113 )
                  AND l.license_attribute_license_value NOT IN ( 12, 110, 111, 112, 210, 211, 212 )
              )
              AND @site_id <> 'SFDC'
    END

    -- 2.2.2) retention model upgrade
    UPDATE i
    SET i.product_type_id = 3,
        i.years = 0
    FROM @item_table i
        INNER JOIN @license_table l
            ON l.item_hierarchy_id = i.item_hierarchy_id
    WHERE i.item_hierarchy_id = 1 --Is not a module
          AND i.product_type_id = 2 --product type is a renewal
          AND l.expiration_date > GETDATE() --license is not expired
          AND i.retention_term > l.retention_term --retention model is going from 1 to 7 yr retention
          AND @site_id = 'SFDC'

    -- 2.3) secondary product_type
    UPDATE @item_table
    SET product_type_id = CASE
                              WHEN l.license_id IS NULL THEN
                                  1 -- new
                              WHEN l.license_id IS NOT NULL
                                   AND l.category_type_name = 'trial' THEN
                                  1 -- trial conversion
                              WHEN l.license_id IS NOT NULL
                                   AND l.category_type_name = 'full'
                                   AND i.expiration_date > l.expiration_date THEN
                                  2
                              WHEN l.license_id IS NOT NULL
                                   AND l.category_type_name = 'full'
                                   AND i.expiration_date = l.expiration_date
                                   AND i.total_license_seats > l.license_seats THEN
                                  3
                              ELSE
                                  2
                          END
    FROM @item_table i
        LEFT JOIN @license_table l
            ON l.item_hierarchy_id = i.item_hierarchy_id
               AND l.license_category_name = i.license_category_name
    WHERE i.item_hierarchy_id = 2
          AND i.product_type_id IS NULL

    -- 2.3.1) upgrade
    INSERT INTO @item_table
    (
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
        usage_pricing_model_id,
        retention_model_id,
        retention_term,
        product_platform_id
    )
    SELECT i.license_category_name,
           i.license_seats - l.license_seats,
           i.license_seats,
           i.storage_gb,
           0,
           i.license_keycode_type_id,
           CONVERT(DATE, GETDATE()),
           l.expiration_date,
           i.cart_item_bundle_id,
           i.item_hierarchy_id,
           product_type_id = 3,
           i.usage_pricing_model_id,
           i.retention_model_id,
           i.retention_term,
           i.product_platform_id
    FROM @item_table i
        INNER JOIN @license_table l
            ON l.item_hierarchy_id = i.item_hierarchy_id
               AND l.license_category_name = i.license_category_name
    WHERE i.item_hierarchy_id = 2
          AND i.product_type_id = 2
          AND i.total_license_seats > l.license_seats
          AND
          (
              @license_attribute_license_value NOT IN ( 12, 110, 111, 112, 210, 211, 212, 13, 113, 213 )
              AND l.license_attribute_license_value NOT IN ( 12, 110, 111, 112, 210, 211, 212, 13, 213, 113 )
          )
          AND @site_id <> 'SFDC'

    -- 2.3.2) update years on new upgrade
    UPDATE @item_table
    SET years = 1
    FROM @item_table i
    WHERE i.item_hierarchy_id = 2
          AND i.product_type_id IN ( 1, 2 )
          AND i.years = 0

    -- 2.3.3) update storage on upgrade
    UPDATE @item_table
    SET storage_gb = i.storage_gb - l.storage_gb
    FROM @item_table i
        INNER JOIN @license_table l
            ON l.license_category_name = i.license_category_name
    WHERE i.years = 0
		AND l.usage_pricing_model_id = 2   --capacity
		AND @site_id <> 'SFDC'

    -- 2.4) product

    -- 2.4.1) update years from SFDC opps
    IF @site_id = 'SFDC'
    BEGIN
        UPDATE i
        SET years = CASE
                        WHEN DATEDIFF(dd, i.start_date, i.expiration_date) <= 366 THEN
                            1
                        WHEN DATEDIFF(dd, i.start_date, i.expiration_date) > 366
                             AND DATEDIFF(dd, i.start_date, i.expiration_date) <= 731 THEN
                            2
                        WHEN DATEDIFF(dd, i.start_date, i.expiration_date) > 731 THEN
                            3
                    END
        FROM @item_table i
        WHERE i.product_type_id <> 3
    END

    -- 2.4.2) update product_id business
    IF @product_line_id IN ( 300 )
    BEGIN
        UPDATE i
        SET i.product_id = f.product_id
        FROM @item_table i
            INNER JOIN dbo.license_category lc
                ON lc.license_category_name = i.license_category_name
            CROSS APPLY dbo.fn_product_select_profile(
                                                         @product_line_id,
                                                         lc.license_category_id,
                                                         i.years,
                                                         i.quantity,
                                                         i.storage_gb,
                                                         DATEDIFF(dd, i.start_date, i.expiration_date),
                                                         i.product_type_id,
                                                         i.license_keycode_type_id,
                                                         i.usage_pricing_model_id,
                                                         i.retention_model_id,
                                                         i.product_platform_id,
                                                         i.sap_material_number
                                                     ) f






    END
    -- 2.4.3) update product_id consumer
    ELSE
    BEGIN
        UPDATE i
        SET i.product_id = f.product_id
        FROM @item_table i
            INNER JOIN dbo.license_category lc
                ON lc.license_category_name = i.license_category_name
            CROSS APPLY dbo.fn_product_select_profile(
                                                         @product_line_id,
                                                         lc.license_category_id,
                                                         i.years,
                                                         i.quantity,
                                                         i.storage_gb,
                                                         DATEDIFF(dd, i.start_date, i.expiration_date),
                                                         i.product_type_id,
                                                         i.license_keycode_type_id,
                                                         i.usage_pricing_model_id,
                                                         i.retention_model_id,
                                                         i.product_platform_id,
                                                         i.sap_material_number
                                                     ) f
													 
    END

    -- 2.5) product_type_id for storage upgrade
    UPDATE @item_table
    SET product_type_id = 3
    FROM @item_table i
    WHERE i.storage_gb IS NOT NULL
          AND i.product_type_id IS NULL

    --select * from @item_table
    --return

    -------------------------------------------------------------------------------------------------------------------------------------------------------------------
    -- 3.) @product_set consumer
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------

    -- 3.1) consumer
    IF @product_line_id NOT IN ( 55, 300 )
    BEGIN
        DECLARE @product_set TABLE
        (
            line_item INT IDENTITY(1, 1),
            product_id INT,
            product_description VARCHAR(100),
            quantity INT
                DEFAULT 1,
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
        )

        --select @license_category_name = license_category_name, @storage_gb = storage_gb, @license_seats = license_seats
        --from @license_table
        --where item_hierarchy_id = 1

        -- 3.1.1) varibles
        SELECT @license_category_name = license_category_name,
               @storage_gb = storage_gb
        FROM @item_table
        WHERE item_hierarchy_id = 1

        SELECT @product_type_id = product_type_id
        FROM @item_table
        WHERE item_hierarchy_id = 1
              AND license_category_name = 'WIFI'

        -- 3.1.2) years, seats
        SELECT @years = MAX(years),
               @license_seats = MAX(total_license_seats)
        FROM @item_table
        WHERE item_hierarchy_id = 1

        -- 3.1.3) insert product_set
        INSERT INTO @product_set
        (
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
        EXEC usp_cart_select_renewal_product_set @keycode,
                                                 @license_category_name,
                                                 @license_seats,
                                                 @storage_gb,
                                                 @years,
                                                 @cart_discount_id,
                                                 @language_code,
                                                 @location_code,
                                                 NULL,
                                                 @currency_id,
                                                 @product_type_id

        -- 3.1.4) update @item_table
        UPDATE @item_table
        SET product_id = p.product_id,
            list_price = p.retail_price,
            unit_price = CASE
                             WHEN i.cart_discount_method_id = 3
                                  AND i.discount > 0 THEN
                                 p.retail_price - ROUND((i.discount / 100) * p.retail_price, 2)
                             WHEN i.cart_discount_method_id = 1
                                  AND i.discount > 0 THEN
                                 p.retail_price - i.discount
                             WHEN i.discount = 0 THEN
                                 p.retail_price
                             ELSE
                                 p.upgrade_price
                         END
        FROM @item_table i
            INNER JOIN @product_set p
                ON p.license_category_name = i.license_category_name
                   AND p.product_type_id = i.product_type_id
                   AND i.item_hierarchy_id = 1 -- and i.product_type_id in (2,3)

        -- 3.1.5) update pricing for storage upgrade
        UPDATE @item_table
        SET product_id = p.product_id,
            list_price = p.retail_price,
            unit_price = CASE
                             WHEN i.cart_discount_method_id = 3
                                  AND i.discount > 0 THEN
                                 p.retail_price - ROUND((i.discount / 100) * p.retail_price, 2)
                             WHEN i.cart_discount_method_id = 1
                                  AND i.discount > 0 THEN
                                 p.retail_price - i.discount
                             ELSE
                                 p.upgrade_price
                         END
        FROM @item_table i
            INNER JOIN @product_set p
                ON p.product_type_id = i.product_type_id
                   AND i.item_hierarchy_id = 1
                   --AND i.storage_gb is not null 
                   AND i.product_type_id = 3

        -- 3.1.6) insert storage product
        INSERT INTO @item_table
        (
            product_id,
            license_category_name,
            quantity,
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
        SELECT s.product_id,
               NULL,
               1,
               s.seats,
               s.seats,
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
        ORDER BY p.product_type_id DESC


        IF @license_category_name IS NULL
        BEGIN
            SELECT @license_category_name = license_category_name
            FROM @license_table
            WHERE item_hierarchy_id = 1
        END

        IF @license_category_name IS NULL
        BEGIN
            SELECT @license_category_name = license_category_name
            FROM @existing_item_table
            WHERE item_hierarchy_id = 1
        END

        -- select @license_category_name

        -- 3.1.7) S/H
        -- First, try to get a CD for the top item in the bundle's hierarchy
        UPDATE s
        SET s.license_seats = 1,
            s.total_license_seats = 1,
            s.product_id = p.product_id,
            s.list_price = pp.retail_price,
            s.unit_price = pp.retail_price
        FROM @item_table s
            LEFT JOIN @item_table i
                ON i.cart_item_bundle_id = s.cart_item_bundle_id
                   AND i.item_hierarchy_id = 1
            INNER JOIN dbo.product p
                ON p.product_family_id = 8
                   AND p.product_lifecycle_id = 1
            INNER JOIN dbo.license_category lc
                ON lc.license_category_name = CASE
                                                  WHEN i.license_category_name IS NULL THEN
                                                      @license_category_name
                                                  ELSE
                                                      i.license_category_name
                                              END
            INNER JOIN dbo.product_license_category plc
                ON plc.product_id = p.product_id
                   AND plc.license_category_id = lc.license_category_id
            INNER JOIN dbo.product_pricing pp
                ON pp.product_id = p.product_id
                   AND pp.language_code = @language_code
                   AND pp.location_code = @location_code
        WHERE s.license_category_name = 'S/H'

        IF @@rowcount = 0
        BEGIN
            -- There's no CD for the top item in the bundle's hierarchy, grab the first item from the cart

            SELECT @license_category_name_cd = lc.license_category_name
            FROM dbo.cart_order_item i
                INNER JOIN dbo.product_license_category c
                    ON i.product_id = c.product_id
                INNER JOIN dbo.license_category lc
                    ON c.license_category_id = lc.license_category_id
            WHERE cart_order_id = @cart_order_id
                  AND i.line_item = 1

            UPDATE s
            SET s.license_seats = 1,
                s.total_license_seats = 1,
                s.product_id = p.product_id,
                s.list_price = pp.retail_price,
                s.unit_price = pp.retail_price
            FROM @item_table s
                LEFT JOIN @item_table i
                    ON i.cart_item_bundle_id = s.cart_item_bundle_id
                       AND i.item_hierarchy_id = 1
                INNER JOIN dbo.product p
                    ON p.product_family_id = 8
                       AND p.product_lifecycle_id = 1
                INNER JOIN dbo.license_category lc
                    ON lc.license_category_name = @license_category_name_cd
                INNER JOIN dbo.product_license_category plc
                    ON plc.product_id = p.product_id
                       AND plc.license_category_id = lc.license_category_id
                INNER JOIN dbo.product_pricing pp
                    ON pp.product_id = p.product_id
                       AND pp.language_code = @language_code
                       AND pp.location_code = @location_code
            WHERE s.license_category_name = 'S/H'
        END

        -- 3.1.8) update dates
        UPDATE @item_table
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
        WHERE i.item_hierarchy_id = 1

    END

    --select '@item_table', days_ = datediff(dd,start_date,expiration_date), * from @item_table
    --return

    -------------------------------------------------------------------------------------------------------------------------------------------------------------------
    -- 4.) pricing business
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------

    IF @product_line_id IN ( 55, 300 )
    --return
    BEGIN
        -- 4.1) direct
        IF @partner_id IS NULL
           OR @partner_id = 8208 -- not Needle
        BEGIN
            SELECT @license_category_name = it.license_category_name
            FROM @item_table AS it

            -- 4.1.1) update @item_table
            UPDATE @item_table
            --select i.product_id, i.product_type_id, i.start_date, i.expiration_date, i.license_seats, days_ = datediff(dd,i.start_date,i.expiration_date), pp.retail_price, y.upgrade_days,c.capability_activation_days,
            SET list_price = CASE
                                 WHEN @license_category_name IN ( lc.license_category_name )  AND 
								 @has_utility=1 THEN
                                     0
                                 ELSE
                                     pp.retail_price
                             END,
                unit_price = CASE
                                 WHEN @license_category_name IN ( lc.license_category_name )  AND 
								 @has_utility=1 THEN 
								 0
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
                LEFT JOIN @PILLR_LICENSE_CATEGORY AS lc
                    ON lc.license_category_name = i.license_category_name
            WHERE pp.language_code = @language_code
                  AND pp.location_code = @location_code
                  AND plp.product_line_id IN ( 300, 55 )

            --@discount and @cart_discount_method_id
            DECLARE @discount FLOAT,
                    @cart_discount_method_id TINYINT

            SELECT @discount = ISNULL(discount, 0),
                   @cart_discount_method_id = cart_discount_method_id
            FROM @item_table

            -- 4.1.2) Tier discount models - expanded to support SFDC discount models via usp_license_select_category_discount_model
            DECLARE @item_discount_profile TABLE
            (
                item_id INT,
                license_id INT,
                license_category_name VARCHAR(10),
                product_type_id INT,
                discount FLOAT,
                cart_discount_method_id INT,
                cart_discount_id INT,
                sap_material_number INT
            )

            DECLARE @item_discount_json VARCHAR(MAX)
            SET @item_discount_json =
            (
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
            )

            DECLARE @item_discount_profile_json NVARCHAR(MAX)
            EXEC usp_license_select_category_discount_model @item_json = @item_discount_json,
                                                            @source_caller = 'usp_cart_insert_cart_order_item',
                                                            @item_discount_profile_json = @item_discount_profile_json OUTPUT

            INSERT INTO @item_discount_profile
            (
                item_id,
                license_id,
                license_category_name,
                product_type_id,
                discount,
                cart_discount_method_id,
                cart_discount_id,
                sap_material_number
            )
            SELECT item_id,
                   license_id,
                   license_category_name,
                   product_type_id,
                   discount,
                   cart_discount_method_id,
                   cart_discount_id,
                   sap_material_number
            FROM
                OPENJSON(@item_discount_profile_json)
                WITH
                (
                    item_id INT '$.item_id',
                    license_id INT '$.license_id',
                    license_category_name VARCHAR(10) '$.license_category_name',
                    product_type_id INT '$.product_type_id',
                    discount FLOAT '$.discount',
                    cart_discount_method_id INT '$.cart_discount_method_id',
                    cart_discount_id INT '$.cart_discount_id',
                    sap_material_number INT '$.sap_material_number'
                )

            UPDATE s
            SET s.unit_price = ROUND((1.00 - idp.discount / 100) * s.unit_price, 2),
                s.cart_discount_id = idp.cart_discount_id,
                s.discount = idp.discount,
                s.sap_material_number = ISNULL(s.sap_material_number,idp.sap_material_number)
            FROM @item_table s
                INNER JOIN @item_discount_profile idp
                    ON idp.item_id = s.item_id
                       AND idp.license_category_name = s.license_category_name
                       AND idp.product_type_id = s.product_type_id
            WHERE @discount = 0 --added to apply tiered discount only if a discount is not provided in @item_json

            -- 4.1.4) license_Cart_discount
            SELECT @discount = d.discount,
                   @cart_discount_method_id = d.cart_discount_method_id
            FROM
            (
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
            ) d

            -- 4.1.4.1) update @item_table discount
            IF @discount > 0
            BEGIN
                UPDATE @item_table
                SET discount = @discount,
                    cart_discount_method_id = @cart_discount_method_id
            --where discount is null --commenting out to allow message_campaign discounts to update @item_table, since 4.1.2 is setting a discount value
            END

            -- 4.1.5) @message_campaign_cart_discount
            DECLARE @message_campaign_cart_discount TABLE
            (
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
            )

            INSERT INTO @message_campaign_cart_discount
            (
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
                  AND @insert_date < m.message_campaign_end_date

            -- 4.1.5.1) update @item_table discount
            UPDATE @item_table
            SET discount = di.discount,
                cart_discount_method_id = di.cart_discount_method_id
            --select i.*, di.discount, di.cart_discount_method_id, c.*
            FROM @message_campaign_cart_discount c
                INNER JOIN dbo.cart_discount_item di
                    ON c.cart_discount_id = di.cart_discount_id
                       AND di.license_category_id = c.license_category_id
                INNER JOIN @item_table i
                    ON c.license_seats = i.license_seats
                       AND c.license_category_name = i.license_category_name
                       AND di.product_type_id = i.product_type_id
            WHERE di.cart_discount_method_id = 3
                  AND c.autorenewal_opt_id = CASE
                                                 WHEN @license_attribute_license_value = 1 THEN
                                                     1
                                                 ELSE
                                                     0
                                             END
                  AND
                  (
                      c.license_distribution_method_id IS NULL
                      OR c.license_distribution_method_id = @license_distribution_method_id
                  )
                  AND
                  (
                      c.location_code IS NULL
                      OR
                      (
                          c.language_code = @language_code
                          AND c.location_code = @location_code
                      )
                  ) --and
                     --i.discount is null --commenting out to allow message_campaign discounts to update @item_table, since 4.1.2 is setting a discount value

            -- 4.1.6) update pricing
            UPDATE @item_table
            SET unit_price = CASE
                                 WHEN cart_discount_method_id = 1 THEN
                                     unit_price - ROUND(discount, 2) --added so a total off discount can be applied in CSI cart
                                 WHEN cart_discount_method_id = 3 THEN
                                     unit_price - ROUND((discount / 100) * unit_price, 2)
                                 ELSE
                                     unit_price
                             END
            WHERE discount IS NOT NULL

            -- 4.1.7) monthly utility
            IF @license_attribute_license_value IN ( 11, 12, 13 )
            BEGIN
                -- 4.1.7.1) remove upsell in the case of utility
                DELETE FROM it
                FROM @item_table AS it
                    LEFT JOIN @PILLR_LICENSE_CATEGORY AS lc2
                        ON it.license_category_name = lc2.license_category_name
                WHERE product_type_id NOT IN ( 1, 2 )
                      AND @license_attribute_license_value = 12
                      AND lc2.license_category_name IS NULL

                -- 4.1.7.2) update pricing
                UPDATE @item_table
                SET unit_price = CASE
                                     WHEN @license_attribute_license_value = 12 THEN
                                         0
                                     ELSE
                                         i.unit_price
                                 END,
                    list_price = CASE
                                     WHEN @license_attribute_license_value = 12 THEN
                                         0
                                     ELSE
                                         i.list_price
                                 END
                FROM @item_table i

                -- 4.1.7.3) update usage_price
                UPDATE @item_table
                SET usage_price = pp.usage_price
                FROM @item_table i
                    INNER JOIN dbo.product_pricing pp
                        ON i.product_id = pp.product_id
                WHERE pp.location_code = @location_code
                      AND pp.language_code = @language_code
                      AND pp.currency_id = @currency_id

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

                -- 4.1.7.5) populate actual_storage_quantity - this is used to calculate the total amount for a capacity order
                UPDATE i
                SET i.actual_storage_quantity = CASE
                                                    WHEN i.storage_gb >= 1024 THEN CONVERT(DECIMAL(12,5), CONVERT(DECIMAL(12,5),i.storage_gb)/1024)
                                                    WHEN i.storage_gb < 1024 THEN 1
                                                    ELSE NULL
                                                END
                FROM @item_table i
                    LEFT JOIN @unit_override uo
                        ON uo.item_id = i.item_id
                WHERE i.usage_pricing_model_id = 2 --capacity
                    AND uo.item_id IS NULL;

            END

        END

        -- 4.2) partner
        ELSE
        BEGIN

            SELECT @pricing_term = CASE
                                       WHEN @license_attribute_id IS NULL
                                            OR @license_attribute_license_value IN ( 20, 120, 220, 11 ) THEN
                                           'annual'
                                       ELSE
                                           'monthly'
                                   END
            IF @product_pricing_level_id IS NULL
            BEGIN
                SELECT @product_pricing_level_id = 1
            END

            UPDATE i
            SET list_price = f.retail_price,
                unit_price = CASE
                                 WHEN @pricing_term = 'monthly' THEN
                                     f.retail_price
                                 WHEN @pricing_term = 'annual'
                                      AND i.product_type_id IN ( 1, 2 ) THEN
                                     ROUND(
                                              f.retail_price
                                              * (CONVERT(
                                                            FLOAT,
                                                            DATEDIFF(dd, i.start_date, i.expiration_date)
                                                            - dbo.fn_leap_days_between(i.start_date, i.expiration_date)
                                                        ) / 365.0
                                                ),
                                              2
                                          )
                                 WHEN @pricing_term = 'annual'
                                      AND i.product_type_id IN ( 3 ) THEN
                                     ROUND(
                                              f.retail_price
                                              * (CONVERT(FLOAT, DATEDIFF(dd, i.start_date, i.expiration_date)) / 365.0),
                                              2
                                          )
                             END,
                usage_price = CASE
                                  WHEN @pricing_term = 'monthly' THEN
                                      f.retail_price
                                  WHEN @pricing_term = 'annual' THEN
                                      ROUND(f.retail_price / (12.0), 2)
                              END
            FROM @item_table i
                INNER JOIN dbo.license_category lc
                    ON i.license_category_name = lc.license_category_name
                CROSS APPLY dbo.fn_partner_select_pricing(
                                                             @partner_id,
                                                             @site_id,
                                                             lc.license_category_id,
                                                             @license_attribute_id,
                                                             i.total_license_seats,
                                                             CASE
                                                                 WHEN i.product_type_id = 3
                                                                      AND DATEDIFF(dd, i.start_date, i.expiration_date) <= 365 THEN
                                                                     1
                                                                 WHEN i.product_type_id = 3
                                                                      AND DATEDIFF(dd, i.start_date, i.expiration_date) > 365
                                                                      AND DATEDIFF(dd, i.start_date, i.expiration_date) <= 730 THEN
                                                                     2
                                                                 WHEN i.product_type_id = 3
                                                                      AND DATEDIFF(dd, i.start_date, i.expiration_date) > 730 THEN
                                                                     3
                                                                 ELSE
                                                                     i.years
                                                             END,
                                                             @pricing_term,
                                                             @product_pricing_level_id,
                                                             @currency_id,
                                                             i.usage_pricing_model_id,
                                                             i.retention_model_id,
                                                             i.product_platform_id,
                                                             'active'
                                                         ) f
            -- 4.2.3) make a check to see if unit_price is not set in code above.  This means there's no product match in partner_pricing_tier based on license_attribute_id
            IF EXISTS (SELECT 1 FROM @item_table WHERE unit_price IS NULL)
            BEGIN

                SELECT @response_code = -1,
                       @message = 'No product unit price found in partner_pricing_tier'
                RETURN
            END

        END

    END
	   
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------
    -- 5.) insert
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------

    -- 5.0) log
    INSERT INTO dbo.cart_order_item_json_log
    (
        cart_order_id,
        item_json,
        bundle_json
    )
    VALUES
    (@cart_order_id, @item_json, @bundle_json)

    SELECT @cart_order_item_json_log_id = SCOPE_IDENTITY()

    -- Remove existing line items to make way for new cart_item_bundle_ids
    DECLARE @replacement_items TABLE
    (
        line_item INT
    )
    DECLARE @line_item_to_remove INT

    INSERT INTO @replacement_items
    (
        line_item
    )
    SELECT DISTINCT
           coi.line_item
    FROM dbo.cart_order_item coi
    WHERE coi.cart_order_id = @cart_order_id
          AND EXISTS
    (
        SELECT *
        FROM @item_table i
        WHERE i.cart_item_bundle_id = coi.cart_item_bundle_id
              AND
              (
                  i.item_hierarchy_id = 1
                  OR
                  (
                      i.item_hierarchy_id = coi.item_hierarchy_id
                      AND i.product_id = coi.product_id
                  )
              )
    )

    WHILE (1 = 1)
    BEGIN
        SELECT TOP (1)
               @line_item_to_remove = line_item
        FROM @replacement_items
        ORDER BY line_item

        IF @@rowcount = 0
        BEGIN
            BREAK -- Nothing left
        END

        EXEC dbo.usp_cart_delete_cart_order_item @vendor_order_code = @vendor_order_code,
                                                 @line_item = @line_item_to_remove

        DELETE FROM @replacement_items
        WHERE line_item = @line_item_to_remove
    END

    -- 5.1) @max_line_item
    SELECT @max_line_item = MAX(line_item)
    FROM dbo.cart_order_item
    WHERE cart_order_id = @cart_order_id

    -- 5.2) this is a hack to basically route any CB orders which have an upgrade to US cart since CB doesn't support upgrades
    UPDATE cart_order
    SET locale = 'en_US',
        site_id = 'WRCART',
        order_type = 'WRCART'
    FROM @item_table it
    WHERE cart_order_id = @cart_order_id
          AND site_id = 'CBCART'
          AND currency_id IN ( 1, 2, 3, 4, 29 ) --USD,EUR,AUD,GBP,CAD
          AND it.product_type_id = 3 --Upgrade

    -- 5.3) cart_order_item

    -- 5.3.1) unit price and usage_price if override data exists
    IF EXISTS (SELECT 1 FROM @unit_override)
    BEGIN
        UPDATE i
        SET i.unit_price = CASE
                               WHEN @product_line_id IN ( 55, 300 ) THEN
                                   u.item_total / i.license_seats
                               ELSE
                                   u.unit_price
                           END,
            i.usage_price = u.usage_price
        FROM @item_table i
            INNER JOIN @unit_override u
                ON i.item_id = u.item_id
    END

    UPDATE @item_table
    SET line_item = IIF(@max_line_item IS NULL, item_id, item_id + @max_line_item)

    -- 5.3.2) insert cart_order_item
    INSERT INTO dbo.cart_order_item
    (
        cart_order_id,
        line_item,
        quantity,
        order_item_offer_amount,
        list_price,
        unit_price,
        unit_price_pre_vat,
        usage_price,
        product_id,
        cart_item_bundle_id,
        start_date,
        expiration_date,
        vendor_order_item_code,
        order_item_update_type_id,
        license_attribute_license_value,
        item_hierarchy_id,
        discount,
        cart_discount_method_id,
        cart_discount_id,
        insert_date,
        insert_by,
        modified_date,
        modified_by,
        opportunity_line_item_id,
        sap_material_number,
        storage_gb
    )
    SELECT @cart_order_id,
           i.line_item,
           quantity = CASE
                          WHEN @product_line_id IN ( 55, 300 ) THEN
                              i.license_seats
                          ELSE
                              i.quantity
                      END,
           i.order_item_offer_amount,
           CASE WHEN i.usage_pricing_model_id = 2 AND i.storage_gb < 1024 THEN i.unit_price ELSE i.list_price end,
           i.unit_price,
           i.unit_price,
           i.usage_price,
           i.product_id,
           i.cart_item_bundle_id,
           i.start_date,
           i.expiration_date,
           i.vendor_order_item_code,
           ISNULL(@order_item_update_type_id, 1),
           @license_attribute_license_value,
           i.item_hierarchy_id,
           discount = CASE WHEN @cart_discount_id IS NULL THEN i.discount ELSE NULL END,
           cart_discount_method_id = ISNULL(@cart_discount_id ,i.cart_discount_method_id),
           cart_discount_id = ISNULL(@cart_discount_id ,i.cart_discount_id),
           @insert_date,
           @insert_by,
           @insert_date,
           @insert_by,
           i.opportunity_line_item_id,
           i.sap_material_number,
           i.storage_gb
    FROM @item_table i

    -- 5.3.3) cart_order_item_json
    INSERT INTO dbo.cart_order_item_json
    (
        cart_order_item_id,
        cart_order_item_json
    )
    SELECT DISTINCT
           i.cart_order_item_id,
           (
               SELECT it.usage_pricing_model_id,
                      it.retention_model_id,
                      it.retention_term,
                      ii.product_platform_id,
                      @product_pricing_level_id AS product_pricing_level_id,
                      it.vault_id,
                      JSON_QUERY(it.vault_array) AS vault,
                      @license_attribute_license_value AS license_attribute_license_value,
                      CASE WHEN it.usage_pricing_model_id = 2 THEN it.actual_storage_quantity ELSE NULL END AS actual_storage_quantity,
                      it.item_total,
                      it.amended_contract,
                      it.license_category_name,
                      @cart_order_item_json_log_id AS cart_order_item_json_log_id
               FROM @item_table it
               WHERE it.line_item = ii.line_item
               FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
           ) AS cart_order_item_json
    FROM dbo.cart_order_item i
        INNER JOIN @item_table ii
            ON i.line_item = ii.line_item
    WHERE i.cart_order_id = @cart_order_id
          AND
          (
              SELECT it.usage_pricing_model_id,
                     it.retention_model_id,
                     it.retention_term,
                     ii.product_platform_id,
                     @product_pricing_level_id AS product_pricing_level_id,
                     it.vault_id,
                     it.amended_contract,
					 it.vault_array
              FROM @item_table it
              WHERE it.cart_item_bundle_id = ii.cart_item_bundle_id
                    AND it.product_id = ii.product_id
              FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
          ) <> '{}'

    -- 5.4) cart_order_item_license
    IF @keycode IS NOT NULL
       AND @keycode <> ''
    BEGIN
        INSERT INTO cart_order_item_license
        (
            cart_order_item_id,
            keycode,
            insert_date,
            insert_by,
            modified_date,
            modified_by
        )
        SELECT DISTINCT
               i.cart_order_item_id,
               @keycode,
               @insert_date,
               @insert_by,
               @insert_date,
               @insert_by
        FROM dbo.cart_order_item i
            INNER JOIN @item_table ii
                ON i.cart_item_bundle_id = ii.cart_item_bundle_id
            LEFT JOIN dbo.cart_order_item_license il
                ON il.cart_order_item_id = i.cart_order_item_id
        WHERE i.cart_order_id = @cart_order_id
              AND il.cart_order_item_license_id IS NULL
    END

    -- 5.5) cart_order totals
    ;WITH item_total AS (
    SELECT 
        coi.cart_order_id,
        coi.line_item,
        coi.cart_item_bundle_id,
        TRY_CAST(CASE 
            WHEN JSON_VALUE(coij.cart_order_item_json, '$.item_total') IS NOT NULL 
                THEN TRY_CAST(JSON_VALUE(coij.cart_order_item_json, '$.item_total') AS MONEY)
            WHEN i.actual_storage_quantity IS NOT NULL 
                THEN i.unit_price * i.actual_storage_quantity
            ELSE coi.unit_price * coi.quantity
        END AS MONEY) AS item_total_amount
    FROM dbo.cart_order_item coi
	LEFT JOIN @item_table i
	    ON i.line_item = coi.line_item
    LEFT JOIN dbo.cart_order_item_json coij
        ON coij.cart_order_item_id = coi.cart_order_item_id
    WHERE coi.cart_order_id = @cart_order_id
	),
	aggregated_totals AS (
		SELECT 
			cart_order_id,
			TRY_CAST(SUM(item_total_amount) AS NUMERIC(22,2)) AS total_amount
		FROM item_total
		GROUP BY cart_order_id
	)
	UPDATE co
	SET co.total_amount = at.total_amount,
		co.sub_total_amount = at.total_amount
	FROM dbo.cart_order co
	INNER JOIN aggregated_totals at
		ON co.cart_order_id = at.cart_order_id;

    -- 5.6) update the cd lines to have correct start and end dates based on what the correspending product was
    UPDATE coi2
    SET coi2.start_date = coi.start_date,
        coi2.expiration_date = coi.expiration_date
    FROM dbo.cart_order_item coi
        INNER JOIN dbo.product p
            ON p.product_id = coi.product_id
        INNER JOIN dbo.cart_order_item coi2
            ON coi2.cart_order_id = coi.cart_order_id
               AND coi2.cart_item_bundle_id = coi.cart_item_bundle_id
        INNER JOIN dbo.product p2
            ON p2.product_id = coi2.product_id
    WHERE coi.cart_order_id = @cart_order_id
          AND
          (
              p.product_family_id != 8
              OR p.product_family_id IS NULL
          ) --not cd product
          AND p2.product_family_id = 8 --cd has this

    -------------------------------------------------------------------------------------------------------------------------------------------------------------------
    -- 7.) return
    -------------------------------------------------------------------------------------------------------------------------------------------------------------------


    SELECT @response_code = 0,
           @message = 'Success'

END TRY
BEGIN CATCH

    SET @message = 'vendor_order_code = ' + @vendor_order_code + ': ' + ERROR_MESSAGE()

    RAISERROR(@message, 16, 1)

    INSERT INTO DBErrorlog
    (
        ErrorNumber,
        ErrorSeverity,
        ErrorState,
        ErrorProcedure,
        ErrorLine,
        ErrorMessage,
        ErrorServer,
        ErrorDB,
        ErrorUser
    )
    SELECT ERROR_NUMBER(),
           ERROR_SEVERITY(),
           ERROR_STATE(),
           ERROR_PROCEDURE(),
           ERROR_LINE(),
           @message,
           @@servername,
           DB_NAME(),
           SUSER_NAME()

END CATCH;
