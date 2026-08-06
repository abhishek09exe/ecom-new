USE [ecommerce_VH14]
GO
/****** Object:  StoredProcedure [dbo].[usp_cart_select_cart_order_item]    Script Date: 15-07-2026 12:26:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[usp_cart_select_cart_order_item]

(
		@vendor_order_code VARCHAR(100),
		@line_item INT = NULL,
		@cart_item_bundle_id INT = NULL,
		@item_hierarchy_id TINYINT = NULL

)

/*
	DATE			AUTHOR		REMARKS
	2017-07-25		esmart		Initial creation.
	2018-03-30		esmart		product_line_cart_type
	2018-10-22		esmart		Add usage_price for autobilling utility and overage
	2019-08-08		esmart		Add usage_pricing_model
	2019-08-29		esmart		Add opportunity_line_item_id
	2019-11-12		jnavarra	Add equivalent_year_price
	2019-12-09		jnavarra	Rollback equivalent_year_price for deeper testing
	2019-12-09		jnavarra	Re-add equivalent_year_price
	2020-01-14		jnavarra	equivalent_year_price prioritize locale pricing
	2020-02-14		jnavarra	equivalent_year_price product locale fallback to cart locale
	2020-02-19		esmart		vault_id
	2020-05-04		esmart		retention_model, product_platform, cart_order_item_json
	2020-07-29		wbarton		Adding support for an array of multiple vault_ids and datacenters from fn_cart_select_cart_order_item_json
	2020-10-14		cnovac		added logic for storage_gb
	2022-01-07		gblandford	SMCI-6214 - Add retention_term & retention_model_type_id
	2023-03-31      psatish		ECOM-899 Get the storage_GB from cart_order_item table
	2025-10-14      jberry		US-5004939 - Added logic in sec 1.2 to handle equivalent_year_price for capacity orders

	DESCRIPTION
*/

AS
	SET NOCOUNT ON
	BEGIN TRY

	DECLARE @cart_order_id INT,
			@storage_gb INT,
			@cart_locale CHAR(5),
			@response_code INT,
			@message VARCHAR(100)

	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	-- 1.) select
	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	-- 1.1) @cart_order_id
	SELECT @cart_order_id = cart_order_id, @cart_locale = locale
	FROM cart_order
	WHERE vendor_order_code = @vendor_order_code

	-- 1.2) select
	SELECT i.cart_order_item_id, i.cart_order_id, i.line_item, i.quantity, s.seats, 
		storage_gb = ISNULL(i.storage_gb,dbo.fn_get_item_storage_gb(i.quantity, DEFAULT, i.product_id, DEFAULT, DEFAULT, DEFAULT)),		
		y.years, i.order_item_offer_amount,
	    CASE 
		    WHEN pp.retail_price IS NULL OR y.years IS NULL THEN NULL
		    WHEN jp.usage_pricing_model_id = 2 
			    AND JSON_VALUE(coij.cart_order_item_json, '$.item_total') IS NULL
				AND i.storage_gb < 1024  THEN NULL
			ELSE CONVERT(MONEY, pp.retail_price * y.years) 
	    END AS equivalent_year_price,
		i.list_price, i.unit_price, i.unit_price_pre_vat, i.tax_item_total, i.usage_price, 
		i.product_id, p.product_description, t.product_type_id, t.product_type_description, p.license_keycode_type_id, kt.license_keycode_type_description, lc.license_category_id, lc.license_category_name, lc.license_category_description, pf.product_family_description,
		i.start_date, i.expiration_date, i.cart_item_bundle_id, i.item_hierarchy_id, dependent_cart_order_item_id = d.cart_order_item_id,
		il.keycode, i.license_attribute_license_value, v.license_attribute_license_value_description,
		i.vendor_order_item_code, i.order_item_update_type_id,
		prl.product_line_cart_type, i.discount, i.cart_discount_method_id, i.cart_discount_id, lc.min_order_quantity, lc.max_order_quantity, i.opportunity_line_item_id,
		jp.usage_pricing_model_id, jp.usage_pricing_model_name,
		jp.retention_model_id, jp.retention_model_name, jp.retention_term, jp.retention_model_type_id,
		jp.product_platform_id, jp.product_platform_name,
		jp.vault_id, jp.vault_datacenter_name, jp.vault,
		jp.product_pricing_level_id, jp.pricing_level_description,
		ij.cart_order_item_json
	FROM cart_order_item i
	INNER JOIN product p
		ON p.product_id = i.product_id
	INNER JOIN product_family pf
		ON pf.product_family_id = p.product_family_id
	INNER JOIN product_line_product plp
		ON plp.product_id = p.product_id
	INNER JOIN product_line prl
		ON plp.product_line_id = prl.product_line_id
	INNER JOIN product_type t
		ON t.product_type_id = p.product_type_id
	LEFT JOIN cart_order_item_json ij
		ON ij.cart_order_item_id = i.cart_order_item_id
	LEFT JOIN product_license_category plc
		ON plc.product_id = p.product_id
	LEFT JOIN license_category lc
		ON lc.license_category_id = plc.license_category_id
	LEFT JOIN license_keycode_type kt
		ON kt.license_keycode_type_id = p.license_keycode_type_id
	LEFT JOIN product_years y
		ON y.product_id = p.product_id
	LEFT JOIN product_seat s
		ON s.product_id = p.product_id
	LEFT JOIN license_attribute_license_value v
		ON v.license_attribute_license_value = i.license_attribute_license_value
	LEFT JOIN cart_order_item_license il
		ON il.cart_order_item_id = i.cart_order_item_id
	LEFT JOIN cart_order_item_json coij
		ON coij.cart_order_item_id = i.cart_order_item_id
	LEFT JOIN (SELECT di.cart_order_item_id, di.cart_order_id, di.line_item, di.cart_item_bundle_id, di.item_hierarchy_id, dlc.license_category_id
				FROM cart_order_item di
				LEFT JOIN product dp
					ON dp.product_id = di.product_id
				LEFT JOIN product_license_category dlc
					ON dlc.product_id = dp.product_id
				WHERE dp.product_type_id IN (1,2)) d
			ON d.cart_order_id = i.cart_order_id AND d.cart_item_bundle_id = i.cart_item_bundle_id AND d.item_hierarchy_id = i.item_hierarchy_id AND (d.license_category_id = plc.license_category_id OR plc.license_category_id IS NULL) AND d.line_item < i.line_item
	OUTER APPLY dbo.fn_locale_to_lang_loc(ISNULL(i.product_locale, @cart_locale)) ll
	OUTER APPLY dbo.fn_cart_select_one_year_products(p.product_id) oyp
	LEFT JOIN dbo.product_pricing pp
		ON pp.product_id = oyp.product_id AND pp.location_code = ll.location_code AND pp.language_code = ll.language_code
	OUTER APPLY fn_cart_select_cart_order_item_json(i.cart_order_item_id) jp
	WHERE i.cart_order_id = @cart_order_id AND
		i.cart_item_bundle_id = CASE WHEN @cart_item_bundle_id IS NULL THEN i.cart_item_bundle_id ELSE @cart_item_bundle_id END AND
		i.item_hierarchy_id = CASE WHEN @item_hierarchy_id IS NULL THEN i.item_hierarchy_id ELSE @item_hierarchy_id END AND
		i.line_item = CASE WHEN @line_item IS NULL THEN i.line_item ELSE @line_item END
		--order by i.cart_item_bundle_id, i.line_item

END TRY

BEGIN CATCH

	SET @response_code = -200
	SET @message = CASE WHEN @message IS NULL THEN 'Could not select cart_order for vendor_order_code: ' + @vendor_order_code
						ELSE @message END

	DECLARE @DBName NVARCHAR(128)
	SET @DBName = DB_NAME()
	EXEC usp_LogError @ErrorDB = @DBName

END CATCH;

