Text
  
  
CREATE PROCEDURE [dbo].[usp_message_select_message_campaign_cart_discount]  
  
(  
 @message_campaign_id int = null,  
    @message_campaign_key uniqueidentifier = null,  
 @license_category_name varchar(10) = NULL,  
 @license_seats int = NULL  
)  
  
/*  
 DATE  AUTHOR  REMARKS  
 2012-11-30 esmart  Initial creation.  
 2019-06-12 wbarton  Adding optional parameters so the cart can select discounts based on license_category_name and license_seats  
 2020-02-28 jnavarra Allow message_campaign_key parameter, simplify logic  
  
 DESCRIPTION  
 select  
*/  
  
as  
 set nocount on      
  
 declare @response_code int,  
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