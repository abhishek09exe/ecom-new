```
USE [ecommerce_VH13]
GO
/****** Object:  StoredProcedure [dbo].[usp_cart_insert_cart_order]    Script Date: 10-07-2026 13:14:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



ALTER PROCEDURE [dbo].[usp_cart_insert_cart_order]

(
		@site_id varchar(65),
		@locale char(5),
		@user_ip varchar(16),
		@cart_extension_json nvarchar(max) = null,
		@response_code int output,
		@message varchar(100) output	
)

/*	
	DATE		AUTHOR		REMARKS
	2009-02-23	esmart		Initial creation.
	2011-03-22	esmart		payment header updates
	2011-12-19	esmart		cart technical contact
	2013-04-17	esmart		cart_customer.order_header_id
	2017-07-13	esmart		cart refactor
	2018-03-29	esmart		cart_json insert
	2018-08-29	esmart		correct message_key in json
	2019-12-11	esmart		use partner_configuration to set currency

	DESCRIPTION
	insert cart order
*/

AS
	set nocount on
	
begin try

	declare @cart_order_status_id tinyint,
			@language_code varchar(2),
			@location_code varchar(3),
			@vendor_order_code_prefix varchar(5), 
			@sales_order_date datetime,
			@cart_order_id int,
			@currency_code varchar(3),
			@currency_id tinyint,
			@partner_key varchar(36), 
			@partner_id int,
			@account_user_name varchar(100),
			@partner_account_id int,
			@vendor_order_code varchar(100),
			@routing_action varchar(50),
			@message_campaign_id int,
			@message_campaign_platform varchar(50),
			@message_key varchar(36),
			@license_id int,
			@cart_discount_id int,
			@insert_date datetime
	
	declare @next_id table (invoice_code_int int)

	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	-- 1.) select
	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		
		select @insert_date = getdate()

		-- 1.1) parse @cart_extension_json
		select @sales_order_date = case when sales_order_date = '' then null else sales_order_date end, 
				@currency_code = currency_code, 
				@vendor_order_code = vendor_order_code, 
				@partner_key = partner_key, 
				@account_user_name = account_user_name, 
				@routing_action = routing_action,
				@message_campaign_id = message_campaign_id,
				@message_campaign_platform = message_campaign_platform,
				@message_key = case when message_key = '' then null else message_key end,
				@cart_discount_id = cart_discount_id
		from openjson(@cart_extension_json)
		with (sales_order_date datetime '$.sales_order_date', 
				currency_code varchar(3) '$.currency_code', 
				vendor_order_code varchar(100) '$.vendor_order_code',
				partner_key varchar(36) '$.partner_key',
				account_user_name varchar(100) '$.account_user_name',
				routing_action varchar(36) '$.routing_action',
				message_campaign_id int '$.message_campaign_id',
				message_campaign_platform varchar(50) '$.message_campaign_platform',
				message_key varchar(36) '$.key',
				cart_discount_id int '$.cart_discount_id')
		
		-- 1.2) variables
		if @sales_order_date is null
			begin
			select @sales_order_date = convert(date,getdate())
			end

		-- 1.3) @partner_id
		if @partner_key is not null and @partner_key <> ''
			begin
			select @partner_id = partner_id from partner where partner_key = @partner_key
			end

		-- 1.3) currency		
		
			-- 1.3.1) @currency_code
			if @currency_code is not null
				begin
				select @currency_id = currency_id from currency where currency_code = @currency_code
				end

			-- 1.3.2) partner cusrrency
			if @currency_id is null and @partner_id is not null
				begin 
				select @currency_code = c.currency_code, @currency_id = c.currency_id
				from partner_configuration_partner cp
				inner join currency c
					on cp.configuration_value = c.currency_code
				where cp.partner_id = @partner_id and cp.partner_configuration_id = 15
				end

			-- 1.3.3) default
			if @currency_id is null
				begin
				select @currency_id = 1
				end

	-------------------------------------------------------------------------------------------------------------------------------------------------------------------
	-- 2.) insert
	-------------------------------------------------------------------------------------------------------------------------------------------------------------------

		-- 2.1) @vendor_order_code
		if @vendor_order_code is null or @vendor_order_code = ''
			begin
				insert into @next_id(invoice_code_int)
				exec usp_next_id @Type=3

				select @vendor_order_code_prefix = vendor_order_code_prefix from cart_site_id_order_code_prefix where site_id = @site_id
				select @vendor_order_code = @vendor_order_code_prefix + convert(varchar(8),invoice_code_int) from @next_id
			end

		-- 2.2) insert cart_order
		insert into cart_order(vendor_order_code,order_type,site_id,site_url,sales_order_date,submission_date,locale,user_ip,currency_id,insert_date)
		select @vendor_order_code,@site_id,@site_id,@site_id,@sales_order_date,@insert_date,@locale,@user_ip,@currency_id,@insert_date
		
		select @cart_order_id = scope_identity()

		-- 2.3) insert cart_order_partner
		if @partner_id is not null
			begin

			select @partner_account_id = p.partner_account_id
			from partner_account p
			inner join account a
				on p.account_id = a.account_id
			where p.partner_id = @partner_id and a.account_user_name = @account_user_name

			insert into cart_order_partner (cart_order_id, partner_id, partner_account_id)
			select @cart_order_id, @partner_id, @partner_account_id
			end

		-- 2.4) @routing_action
		if @routing_action is not null and @routing_action <> ''
			begin
			insert into cart_order_route (cart_order_id, routing_action, insert_date)
			select @cart_order_id, @routing_action, @insert_date
			end

		-- 2.5) cart_order_message
		if @message_key is not null
			begin 
			select @license_id = license_id
			from license_key
			where license_key = @message_key

			insert into cart_order_message (cart_order_id, message_key, message_campaign_id, message_campaign_platform, cart_discount_id, license_id)
			select @cart_order_id, @message_key, @message_campaign_id, @message_campaign_platform, @cart_discount_id, @license_id
			end

		-- 2.6) cart_json
		if @cart_extension_json is not null
			begin
				insert into cart_json (cart_json, cart_order_id)
				values(@cart_extension_json, @cart_order_id)
			end

	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	-- 3.) return
	-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	
		select @response_code = 0, @message = 'Success'

		select cart_order_id, vendor_order_code,site_id,offer_amount,total_amount,sub_total_amount,tax_amount,sales_order_date,locale,insert_date,insert_by,modified_date,modified_by,cart_order_status_id,currency_id,user_ip
		from cart_order
		where cart_order_id = @cart_order_id

end try
begin catch

	set @response_code = -200
	set @message = case when @message is null then 'insert cart_order failed'
						else @message end

	declare @DBName nvarchar(128)
	set @DBName = db_name()
	exec usp_LogError @ErrorDB = @DBName
	
end catch;

```