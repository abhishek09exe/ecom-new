usp_cart_select_cart_discount:

Text
  CREATE PROCEDURE [dbo].[usp_cart_select_cart_discount]  (  @cart_discount_id int )  /*   DATE  AUTHOR  REMARKS  2012-10-15 esmart  Initial creation.  2014-08-27 esmart  Add license_distribution_method to the output.   DESCRIPTION  select cart discount *
/  AS  set nocount on       declare @cart_discount_specials_code_list varchar(max),    @cart_link varchar(200),    @response_code int,    @message varchar(100)   begin try   -- ------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------------  -- 1.) select  -- ----------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------   -- 1.1) @cart_discount_specials_code_list   select @cart_discount_specials_code_list = coalesce(@cart_discount_specials_code_list+', ','')+ cast(convert(varchar(20),specials_c
ode) as varchar(100))    from cart_discount_specials_mapping    where cart_discount_id = @cart_discount_id    -- 1.2) @cart_link  if (select count(*) from cart_discount_item where product_type_id = 1 and license_category_id is not null and license_seats i
s not null and years is not null and cart_discount_id = @cart_discount_id) > 0   begin    select @cart_link = 'https://www.webroot.com/us/en/cart/update?key='+ convert(varchar(36),cart_discount_key)    from cart_discount    where cart_discount_id = @cart_
discount_id   end  else   begin    select @cart_link = ''   end   -- ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
---------  -- 2.) result  -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------   -- 2.1) result   select d.ca
rt_discount_id, cart_discount_description, cart_discount_type_id, cart_discount_status_id, cart_discount_key = convert(varchar(36),cart_discount_key), cart_discount_code, last_modified_date, last_modified_by,    m.license_distribution_method_id,    cart_d
iscount_specials_code_list = @cart_discount_specials_code_list,    cart_link = @cart_link   from cart_discount d   left join cart_discount_license_distribution_method m    on d.cart_discount_id = m.cart_discount_id   where d.cart_discount_id = @cart_disco
unt_id    end try   begin catch    set @response_code = -200   set @message = case when @message is null then 'select failed'        else @message end    declare @DBName nvarchar(128)   set @DBName = db_name()   exec usp_LogError @ErrorDB = @DBName     en
d catch;      



usp_cart_select_cart_discount_item:

Text
  CREATE PROCEDURE [dbo].[usp_cart_select_cart_discount_item]  (  @cart_discount_id int )  /*   DATE  AUTHOR  REMARKS  2012-10-15 esmart  Initial creation.  2014-08-04 esmart  Add license module   DESCRIPTION  select  */  AS  set nocount on       declare 
@response_code int,    @message varchar(100)   begin try   -- -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--  -- 1.) select  -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------   select i.cart_discount_item_id, i.c
art_discount_id, i.cart_discount_method_id, i.discount, i.low_range, i.high_range, i.product_type_id, i.product_line_id, i.license_category_id, i.license_category_name, i.license_seats, i.storage_gb, i.years, i.last_modified_date, i.last_modified_by, i.pr
oduct_id,   m.license_module_id, m.license_module_code, m.license_module_name  from cart_discount_item i  left join cart_discount_item_license_module im   on i.cart_discount_item_id = im.cart_discount_item_id  left join license_module m   on im.license_mo
dule_id = m.license_module_id  where cart_discount_id = @cart_discount_id    end try   begin catch    set @response_code = -200   set @message = case when @message is null then 'select failed'        else @message end    declare @DBName nvarchar(128)   se
t @DBName = db_name()   exec usp_LogError @ErrorDB = @DBName     end catch;     