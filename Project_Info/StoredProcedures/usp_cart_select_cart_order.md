USE [ecommerce_VH14]
GO
/****** Object:  StoredProcedure [dbo].[usp_cart_select_cart_order]    Script Date: 15-07-2026 12:22:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[usp_cart_select_cart_order]

(
		@vendor_order_code varchar(100)
)

/*	
	DATE		AUTHOR		REMARKS
	2017-07-25	esmart		Initial creation.
	2018-01-23	esmart		Add currency_code

	DESCRIPTION
*/

AS
	set nocount on				
	begin try

	declare @response_code int,
			@message varchar(100)
			
	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	-- 1.) select
	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	select co.cart_order_id, vendor_order_code,site_id,offer_amount,total_amount,sub_total_amount,tax_amount,sales_order_date,locale,insert_date,insert_by,modified_date,modified_by,cart_order_status_id,cu.currency_id,cu.currency_code,user_ip,
			partner_key = convert(varchar(36),p.partner_key),
			j.cart_json
	from cart_order co
	left join cart_order_partner cp
		on cp.cart_order_id = co.cart_order_id
	left join partner p
		on p.partner_id = cp.partner_id
	left join currency cu
		on cu.currency_id = co.currency_id
	left join cart_json j
		on j.cart_order_id = co.cart_order_id
	where co.vendor_order_code = @vendor_order_code

end try

begin catch

	set @response_code = -200
	set @message = case when @message is null then 'Could not select cart_order for vendor_order_code: ' + @vendor_order_code
						else @message end

	declare @DBName nvarchar(128)
	set @DBName = db_name()
	exec usp_LogError @ErrorDB = @DBName

end catch; 



