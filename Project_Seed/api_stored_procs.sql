-- =============================================================================
-- Seed stored procedures for local/dev environments.
--
-- ecom-new-api only calls a small subset of the 1383 procedures found in the
-- full Project_Seed/allecom_stored_procs.sql export. That full export also
-- contains widespread source corruption (identifiers split across line
-- breaks by whatever tool produced it), so rather than repairing all ~1400
-- procedures we seed only the ones actually used by the API, using the
-- verified-clean source for each.
--
-- Included (verbatim from the export, syntax-checked against a live
-- SQL Server instance):
--   - usp_cart_select_cart_discount
--   - usp_cart_select_cart_discount_item
--   - usp_cart_select_message_key
--   - usp_cart_select_new_product_discount
--   - usp_message_select_message_campaign_cart_discount
--
-- Stubbed (the real procedures are large, have internal corruption, and pull
-- in further undocumented proc/function dependencies - out of scope for
-- local dev seeding). Each stub returns one hardcoded row shaped to match
-- the C# POCO it's read into (see Data/Entities/ConfiguratorPricingResult.cs
-- and Data/Entities/LicenseByIdProcedureRow.cs), so the API can be exercised
-- end-to-end locally without a real pricing/license engine:
--   - usp_cart_select_license_configurator_pricing
--   - usp_license_select_license_by_id
--
-- Not seeded (referenced only inside a try/catch in MessageKeyService.cs,
-- which already treats it as optional and logs+continues on failure):
--   - usp_cart_select_license_campaign
--
-- usp_LogError, called from every procedure's CATCH block, is intentionally
-- left unseeded. SQL Server defers name resolution for procedure bodies, so
-- this does not block CREATE PROCEDURE; it would only matter if a procedure
-- actually hit its error path, which local/dev smoke testing shouldn't rely on.
-- =============================================================================

-- =============================================================================
-- PROCEDURE: [dbo].[usp_cart_select_cart_discount]
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_cart_select_cart_discount]

(
	@cart_discount_id int
)

/*	
	DATE		AUTHOR		REMARKS
	2012-10-15	esmart		Initial creation.
	2014-08-27	esmart		Add license_distribution_method to the output.

	DESCRIPTION
	select cart discount
*/

AS
	set nocount on				

	declare	@cart_discount_specials_code_list varchar(max),
			@cart_link varchar(200),
			@response_code int,
			@message varchar(100)

	begin try

	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	-- 1.) select
	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	-- 1.1) @cart_discount_specials_code_list
		select @cart_discount_specials_code_list = coalesce(@cart_discount_specials_code_list+', ','')+ cast(convert(varchar(20),specials_code) as varchar(100)) 
		from cart_discount_specials_mapping 
		where cart_discount_id = @cart_discount_id


	-- 1.2) @cart_link
	if (select count(*) from cart_discount_item	where product_type_id = 1 and license_category_id is not null and license_seats is not null and years is not null and cart_discount_id = @cart_discount_id) > 0
		begin
			select @cart_link = 'https://www.webroot.com/us/en/cart/update?key='+ convert(varchar(36),cart_discount_key)
			from cart_discount
			where cart_discount_id = @cart_discount_id
		end
	else
		begin
			select @cart_link = ''
		end

	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	-- 2.) result
	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	-- 2.1) result
		select d.cart_discount_id, cart_discount_description, cart_discount_type_id, cart_discount_status_id, cart_discount_key = convert(varchar(36),cart_discount_key), cart_discount_code, last_modified_date, last_modified_by,
			m.license_distribution_method_id,
			cart_discount_specials_code_list = @cart_discount_specials_code_list,
			cart_link = @cart_link
		from cart_discount d
		left join cart_discount_license_distribution_method m
			on d.cart_discount_id = m.cart_discount_id
		where d.cart_discount_id = @cart_discount_id
	
	end try

	begin catch

		set @response_code = -200
		set @message = case when @message is null then 'select failed'
							else @message end

		declare @DBName nvarchar(128)
		set @DBName = db_name()
		exec usp_LogError @ErrorDB = @DBName
		
	end catch;
GO

-- =============================================================================
-- PROCEDURE: [dbo].[usp_cart_select_cart_discount_item]
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_cart_select_cart_discount_item]

(
	@cart_discount_id int
)

/*	
	DATE		AUTHOR		REMARKS
	2012-10-15	esmart		Initial creation.
	2014-08-04	esmart		Add license module

	DESCRIPTION
	select 
*/

AS
	set nocount on				

	declare	@response_code int,
			@message varchar(100)

	begin try

	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	-- 1.) select
	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	select i.cart_discount_item_id, i.cart_discount_id, i.cart_discount_method_id, i.discount, i.low_range, i.high_range, i.product_type_id, i.product_line_id, i.license_category_id, i.license_category_name, i.license_seats, i.storage_gb, i.years, i.last_modified_date, i.last_modified_by, i.product_id,
		m.license_module_id, m.license_module_code, m.license_module_name
	from cart_discount_item i
	left join cart_discount_item_license_module im
		on i.cart_discount_item_id = im.cart_discount_item_id
	left join license_module m
		on im.license_module_id = m.license_module_id
	where cart_discount_id = @cart_discount_id
	
	end try

	begin catch

		set @response_code = -200
		set @message = case when @message is null then 'select failed'
							else @message end

		declare @DBName nvarchar(128)
		set @DBName = db_name()
		exec usp_LogError @ErrorDB = @DBName
		
	end catch;
GO

-- =============================================================================
-- PROCEDURE: [dbo].[usp_cart_select_message_key]
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_cart_select_message_key]
(
    @message_key VARCHAR(36),
    @license_category_name VARCHAR(20) = NULL,
    @years INT = NULL,
    @seats INT = NULL,
    @sku VARCHAR(200) = NULL
)

/*	
	DATE		AUTHOR		REMARKS
	2017-07-11	esmart		Initial creation.
	2018-07-30	esmart		Add license_keycode_type_id
	2020-08-06	wbarton		For license_message_key, changing the payment_header_id and customer_id to reference the license's most recent order data rather than license_message_value, which has data-maintenance issues. 
	2024-03-05	rambasna	Ecom-4248 For zuora product price, return zuora campagn_id and rate_plan_id

	DESCRIPTION
	select message key data

*/

AS
SET NOCOUNT ON;

DECLARE @message_key_type VARCHAR(50),
        @message_key_json NVARCHAR(MAX),
        @keycode VARCHAR(40),
        @license_id INT,
        @cart_order_id INT,
        @response_code INT,
        @message VARCHAR(100);

BEGIN TRY

    -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    -- 1.) select
    -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    --1.0) zoura_campaign_id

    IF ISNUMERIC(@message_key) = 0
    BEGIN
        -- 1.1) license_key
        IF @message_key_type IS NULL
            SELECT @message_key_type = 'license_key'
            FROM license_key
            WHERE license_key = @message_key;

        -- 1.1.1) license_message
        IF @message_key_type IS NULL
        BEGIN
            SELECT @message_key_type = 'license_message_key',
                   @license_id = l.license_id,
                   @keycode = l.keycode
            FROM license_message m
                INNER JOIN license l
                    ON l.license_id = m.license_id
            WHERE m.license_message_key = @message_key;
        END;

        -- 1.1.2) cart_discount_key
        IF @message_key_type IS NULL
        BEGIN
            SELECT @message_key_type = 'cart_discount_key'
            FROM cart_discount
            WHERE cart_discount_key = @message_key;
        END;

        -- 1.1.3) cart_discount_message_key
        IF @message_key_type IS NULL
        BEGIN
            SELECT @message_key_type = 'cart_discount_message_key'
            FROM cart_discount_message
            WHERE cart_discount_message_key = @message_key;
        END;
    END;

    -- 1.1.4) zuora_campaign_id
    IF ISNUMERIC(@message_key) = 1
    BEGIN
        IF EXISTS
        (
            SELECT 1
            FROM dbo.zuora_product_pricing
            WHERE campaign_id = @message_key
        )
        BEGIN
            SELECT @message_key_type = 'zuora_campaign_id'
            FROM dbo.zuora_product_pricing
            WHERE campaign_id = @message_key;
        END;
    END;

    -- select message_key_type = @message_key_type

    -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    -- 2.) 
    -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    --2.0) zoura_campaign_id 
    IF @message_key_type = 'zuora_campaign_id'
    BEGIN

        IF @sku IS NOT NULL
        BEGIN
            SELECT @message_key_json =
            (
                SELECT DISTINCT
                   zp.campaign_id,
                       lc.license_category_name,
                       zp.years,
                       zp.seats,
                       zp.rate_plan_id,
                       zp.sku,
                       ((zp.renewal_price - zp.retail_price) / zp.renewal_price) * 100 discount
                FROM dbo.zuora_product_pricing zp
                    LEFT JOIN license_category lc
                        ON lc.license_category_id = zp.license_category_id
                WHERE campaign_id = @message_key
                      AND ISNULL(zp.years, 0) = ISNULL(@years, 0)
                      AND ISNULL(zp.seats, 0) = ISNULL(@seats, 0)
                      AND ISNULL(lc.license_category_name, 0) = ISNULL(@license_category_name, 0)
                      AND zp.sku = @sku
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            );
        END;
        ELSE IF @license_category_name IS NOT NULL AND @years IS NOT NULL AND @seats IS NOT null
		   SELECT @message_key_json =
            (
                SELECT DISTINCT
                       zp.campaign_id,
                       lc.license_category_name,
                       zp.years,
                       zp.seats,
                       zp.rate_plan_id,
                       zp.sku,
                       ((zp.renewal_price - zp.retail_price) / zp.renewal_price) * 100 discount
                FROM dbo.zuora_product_pricing zp
                    LEFT JOIN license_category lc
                        ON lc.license_category_id = zp.license_category_id
                WHERE campaign_id = @message_key
                      AND ISNULL(zp.years, 0) = ISNULL(@years, 0)
                      AND ISNULL(zp.seats, 0) = ISNULL(@seats, 0)
                      AND ISNULL(lc.license_category_name, 0) = ISNULL(@license_category_name, 0)
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            );

        SELECT message_key_type = @message_key_type,
               message_key_json = @message_key_json;
        RETURN;
    END;

    -- 2.1) license_key data
    IF @message_key_type = 'license_key'
    BEGIN
        SELECT @message_key_json =
        (
            SELECT l.license_id,
                   l.keycode,
                   customer_id,
                   l.license_keycode_type_id
            FROM license_key k
                INNER JOIN license l
                    ON k.license_id = l.license_id
            WHERE k.license_key = @message_key
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        SELECT message_key_type = @message_key_type,
               message_key_json = @message_key_json;
        RETURN;
    END;

    -- 2.2) license_message data
    IF @message_key_type = 'license_message_key'
    BEGIN
        -- select @message_key_type

        DECLARE @license_message_values TABLE
        (
            message_value_type_name VARCHAR(50),
            value_id VARCHAR(20)
        );
        DECLARE @payment_header_id INT,
                @customer_id INT;

        INSERT INTO @license_message_values
        (
            message_value_type_name,
            value_id
        )
        SELECT t.message_value_type_name,
               v.value_id
        FROM license_message m
            INNER JOIN license_message_value v
                ON v.license_message_id = m.license_message_id
            INNER JOIN message_value_type t
                ON v.message_value_type_id = t.message_value_type_id
        WHERE m.license_message_key = @message_key;

        --Setting @payment_header_id and @customer_id to values from the license's most recent order
        SELECT @payment_header_id = oph.payment_header_id,
               @customer_id = co.customer_id
        FROM order_payment_header oph
            INNER JOIN
            (
                SELECT oil.license_id,
                       MAX(oi.order_header_id) AS most_recent_order
                FROM order_item_license oil
                    INNER JOIN order_item oi
                        ON oi.order_item_id = oil.order_item_id
                WHERE oil.license_id = @license_id
                GROUP BY oil.license_id
            ) oi
                ON oph.order_header_id = oi.most_recent_order
            INNER JOIN dbo.customer_order co
                ON co.order_header_id = oph.order_header_id
                   AND co.customer_type_id = 1;

        SELECT @message_key_json =
        (
            SELECT license_id = @license_id,
                   keycode = @keycode,
                   payment_header_id = @payment_header_id,
                   customer_id = @customer_id,
                   cart_discount_id =
        (
            SELECT value_id
            FROM @license_message_values
            WHERE message_value_type_name = 'cart_discount_id'
        )   ,
                   p_rc =
        (
            SELECT value_id
            FROM @license_message_values
            WHERE message_value_type_name = 'p_rc'
        )   ,
                   p_rsc =
        (
            SELECT value_id
            FROM @license_message_values
            WHERE message_value_type_name = 'p_rsc'
        )   ,
                   p_ac =
        (
            SELECT value_id
            FROM @license_message_values
            WHERE message_value_type_name = 'p_ac'
        )   ,
                   trx_rc =
        (
            SELECT value_id
            FROM @license_message_values
            WHERE message_value_type_name = 'trx_rc'
        )   ,
                   trx_rsc =
        (
            SELECT value_id
            FROM @license_message_values
            WHERE message_value_type_name = 'trx_rsc'
        )   ,
                   trx_ac =
        (
            SELECT value_id
            FROM @license_message_values
            WHERE message_value_type_name = 'trx_ac'
        )
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        SELECT message_key_type = @message_key_type,
               message_key_json = @message_key_json;
        RETURN;
    END;

    -- 2.3) cart_discount_key data
    IF @message_key_type = 'cart_discount_key'
    BEGIN

        SELECT @message_key_json =
        (
            SELECT d.cart_discount_id,
                   i.license_category_name,
                   i.license_seats,
                   i.storage_gb,
                   years = CONVERT(DECIMAL(4, 2), i.years),
                   i.cart_discount_method_id,
                   discount = CONVERT(DECIMAL(10, 4), i.discount),
                   license_keycode_type_id = CASE
                                                 WHEN i.license_category_id IN ( 220, 230, 231 ) THEN
                                                     3
                                                 ELSE
                                                     1
                                             END
            FROM cart_discount d
                INNER JOIN cart_discount_item i
                    ON i.cart_discount_id = d.cart_discount_id
            WHERE d.cart_discount_key = @message_key
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        SELECT message_key_type = @message_key_type,
               message_key_json = @message_key_json;
        RETURN;
    END;

    -- 2.3) cart_discount_message_key data
    IF @message_key_type = 'cart_discount_message_key'
    BEGIN

        DECLARE @discount_message_values TABLE
        (
            message_value_type_name VARCHAR(50),
            value_id VARCHAR(20)
        );

        INSERT INTO @discount_message_values
        (
            message_value_type_name,
            value_id
        )
        SELECT t.message_value_type_name,
               v.value_id
        FROM cart_discount_message m
            INNER JOIN cart_discount_message_value v
                ON v.cart_discount_message_id = m.cart_discount_message_id
            INNER JOIN message_value_type t
                ON v.message_value_type_id = t.message_value_type_id
        WHERE m.cart_discount_message_key = @message_key;

        SELECT @cart_order_id = value_id
        FROM @discount_message_values
        WHERE message_value_type_name = 'cart_order_id';

        IF @cart_order_id IS NOT NULL
        BEGIN
            IF
            (
                SELECT cart_order_status_id
                FROM cart_order
                WHERE cart_order_id = @cart_order_id
            ) = 2
            BEGIN
                SELECT @message_key_json = NULL;
            END;
            ELSE
            BEGIN
                SELECT @message_key_json =
                (
                    SELECT cart_order_id,
                           vendor_order_code
                    FROM cart_order
                    WHERE cart_order_id = @cart_order_id
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                );
            END;
        END;

        SELECT message_key_type = @message_key_type,
               message_key_json = @message_key_json;
        RETURN;
    END;

--end
END TRY
BEGIN CATCH

    SET @response_code = -200;
    SET @message = CASE
                       WHEN @message IS NULL THEN
                           'select failed'
                       ELSE
                           @message
                   END;

    DECLARE @DBName NVARCHAR(128);
    SET @DBName = DB_NAME();
    EXEC usp_LogError @ErrorDB = @DBName;

END CATCH;
GO

-- =============================================================================
-- PROCEDURE: [dbo].[usp_cart_select_new_product_discount]
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_cart_select_new_product_discount]

(
		@license_category_name varchar(10), 
		@license_seats int,
		@storage_gb int,
		@years float,
		@cart_discount_method_id tinyint,
		@discount float,
		@language_code varchar(2) = null,
		@location_code varchar(3) = null,
		@cart_type_id tinyint = null
)

/*	
	DATE		AUTHOR		REMARKS
	2010-09-10	esmart		Initial creation.

	DESCRIPTION
	select discount
*/

AS
	set nocount on				

	declare	@product_line_id int,
			@license_category_id tinyint,
			
			@response_code int,
			@message varchar(100)

	begin try

	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	-- 1.) validate
	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	-- 1.0) @language_code and @location_code
		if @language_code is null or @location_code is null
			begin
			select @language_code = 'EN', @location_code = 'USA'
			end

	-- 1.1) select @license_category_id
		select @license_category_id = license_category_id
		from license_category
		where license_category_name = @license_category_name

	-- 1.3) @license_seats
		if @license_seats is null or @license_seats = 0
			begin
				select @license_seats = min(seats) 
				from product_license_category_seat
				where license_category_id = @license_category_id
			end
			
	-- 1.4) @license_storage
		if @storage_gb is null or @storage_gb = 0
			begin
				select @storage_gb = min(storage_gb) 
				from product_license_category_storage
				where license_category_id = @license_category_id
				
				if @storage_gb is null begin select @storage_gb = 0 end
			end

	-- 1.5) @years
--		if @years is null or @years = 0
--			begin
--				select @years = 1
--			end

	-- 1.6) @discount
		if @discount is null 
			begin
			select @discount = 0
			end

	-- 1.7) @cart_discount_method_id
		if @cart_discount_method_id is null or @cart_discount_method_id = 0
			begin
			select @cart_discount_method_id = 1
			end

	-- 1.5) product_line_id
		if @cart_type_id is null
			begin
			select @cart_type_id = 1
			end

			select @product_line_id = product_line_id
			from license_category_product_line
			where license_category_id = @license_category_id and 
				language_code = @language_code and 
				location_code = @location_code and
				cart_type_id = @cart_type_id

	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	-- 1.) select
	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

--select product_line_id = @product_line_id, license_category_id = @license_category_id, license_seats = @license_seats, storage_gb = @storage_gb, years = @years, discount = @discount

	select d.cart_discount_id, d.cart_discount_description, d.cart_discount_type_id, d.cart_discount_status_id, 
		cart_discount_key = convert(varchar(36),d.cart_discount_key), d.cart_discount_code, d.last_modified_date, d.last_modified_by
	from cart_discount_item i
	inner join cart_discount d
		on i.cart_discount_id = d.cart_discount_id
	where i.product_type_id = 1 and
			i.license_category_name = @license_category_name and
			i.license_seats = @license_seats and
			i.storage_gb = @storage_gb and
			i.years = @years and
			i.discount = @discount and 
			i.cart_discount_method_id = @cart_discount_method_id and
			i.product_line_id = @product_line_id

	end try

	begin catch

		set @response_code = -200
		set @message = case when @message is null then 'insert failed'
							else @message end

		declare @DBName nvarchar(128)
		set @DBName = db_name()
		exec usp_LogError @ErrorDB = @DBName
		
	end catch;
GO

-- =============================================================================
-- PROCEDURE: [dbo].[usp_message_select_message_campaign_cart_discount]
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_message_select_message_campaign_cart_discount]

(
	@message_campaign_id int = null,
    @message_campaign_key uniqueidentifier = null,
	@license_category_name varchar(10) = NULL,
	@license_seats int = NULL
)

/*
	DATE		AUTHOR		REMARKS
	2012-11-30	esmart		Initial creation.
	2019-06-12	wbarton		Adding optional parameters so the cart can select discounts based on license_category_name and license_seats
	2020-02-28	jnavarra	Allow message_campaign_key parameter, simplify logic

	DESCRIPTION
	select
*/

as
	set nocount on				

	declare	@response_code int,
			@message varchar(100)


	begin try

	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	-- 1.) select
	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	-- 1.1) select message_content
	select d.message_campaign_cart_discount_int, d.message_campaign_id, c.cart_discount_id, convert(char(36), c.cart_discount_key) as cart_discount_key, c.cart_discount_description, cdi.license_category_name
	from message_campaign_cart_discount d
	inner join cart_discount c
		on d.cart_discount_id = c.cart_discount_id
	inner join dbo.cart_discount_item cdi
		on cdi.cart_discount_id = c.cart_discount_id
	inner join dbo.message_campaign mc
		on mc.message_campaign_id = d.message_campaign_id
	where (d.message_campaign_id = @message_campaign_id or mc.message_campaign_key = @message_campaign_key)
		and (@license_seats is null or cdi.license_seats = @license_seats)
		and (@license_category_name is null or cdi.license_category_name = @license_category_name)


	end try

	begin catch

		set @response_code = -200
		set @message = case when @message is null then 'select failed'
							else @message end

		declare @DBName nvarchar(128)
		set @DBName = db_name()
		exec usp_LogError @ErrorDB = @DBName
		
	end catch;
GO

-- =============================================================================
-- PROCEDURE: [dbo].[usp_cart_select_license_configurator_pricing] (STUBBED)
--
-- The real procedure is ~1750 lines, has multiple internal corruption breaks
-- in the export, and calls out to usp_cart_select_renewal_product_set,
-- usp_license_select_category_discount_model, and six custom scalar/table
-- functions - none of which are seeded here. This stub returns one
-- hardcoded row shaped to match Data/Entities/ConfiguratorPricingResult.cs
-- so local API calls succeed end-to-end with visible sample data.
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_cart_select_license_configurator_pricing]
(
    @item_json NVARCHAR(MAX),
    @bundle_json NVARCHAR(MAX),
    @opt_args VARCHAR(100) = ''
)
AS
SET NOCOUNT ON;

SELECT
    line_item = 1,
    quantity = 1,
    list_price = CONVERT(MONEY, 79.99),
    unit_price = CONVERT(MONEY, 59.99),
    usage_price = CONVERT(MONEY, 0.00),
    equivalent_year_price = CONVERT(MONEY, 59.99),
    order_item_offer_amount = CONVERT(MONEY, 0.00),
    product_description = 'Mock Product (seeded stub)',
    product_type_description = 'New',
    license_category_name = 'AV',
    license_category_description = 'Antivirus (mock)',
    product_family_description = 'Mock Family',
    start_date = CONVERT(DATE, GETDATE()),
    expiration_date = DATEADD(yy, 1, CONVERT(DATE, GETDATE())),
    cart_item_bundle_id = 1,
    item_hierarchy_id = CONVERT(TINYINT, 1),
    license_keycode_type_id = 1,
    dependent_cart_order_item_id = NULL,
    storage_gb = NULL,
    usage_pricing_model_id = NULL,
    retention_model_id = NULL,
    retention_term = NULL,
    retention_model_name = NULL,
    actual_storage_quantity = CONVERT(DECIMAL(12, 5), 0);
GO

-- =============================================================================
-- PROCEDURE: [dbo].[usp_license_select_license_by_id] (STUBBED)
--
-- The real procedure depends on fn_license_select_effective_object_element,
-- fn_license_select_sfdc_license_category_mapping_values, and
-- fn_app_config_select_key_values, plus several reference tables/views not
-- seeded here. This stub returns one hardcoded row shaped to match
-- Data/Entities/LicenseByIdProcedureRow.cs.
-- =============================================================================
CREATE PROCEDURE [dbo].[usp_license_select_license_by_id]
(
    @license_id INT
)
AS
SET NOCOUNT ON;

SELECT
    start_date = CONVERT(DATE, GETDATE()),
    end_date = DATEADD(yy, 1, CONVERT(DATE, GETDATE())),
    license_type_description = 'Retail (mock)',
    max_daily_activations = 5,
    parent_keycode = NULL,
    consumed_seats = 1,
    seats_used = 1,
    storage_gb = NULL,
    license_attribute_description = 'Standard (mock)',
    license_attribute_tag = 'STD',
    license_attribute_license_value = 1,
    license_attribute_license_value_description = 'Standard (mock)',
    license_attribute_last_modified = GETDATE(),
    oem_type = NULL,
    portal_flag = 0,
    renewal_count = 0,
    license_origin_channel_name = 'Direct (mock)',
    license_original_activation_date = GETDATE(),
    email_opt_in = 1,
    license_distribution_method_code = 'RTL',
    next_bill_date = DATEADD(yy, 1, CONVERT(DATE, GETDATE()));
GO
