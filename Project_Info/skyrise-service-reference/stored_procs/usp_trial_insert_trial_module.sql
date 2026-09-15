-- Source: QA DB via sp_helptext. Referenced in doc 04 (04_SKYRISE_KEYCODE_GENERATION.md), Section 4.
-- Uses legacy fn_parse_json (not OPENJSON) -- predates the refactor applied to the other two trial procs.
-- Overlaps with the embedded module block in usp_trial_insert_trial.sql -- a third mechanism
-- (ModuleTrialController -> License Service dispatch) also exists; see doc 02 Section 3.7.

CREATE PROCEDURE [dbo].[usp_trial_insert_trial_module]
(
 @keycode varchar(40),
 @trial_registration_json varchar(max)
)
/*
 DATE       AUTHOR  REMARKS
 2017-02-03 esmart  Initial creation

 DESCRIPTION
 insert trial license_module_license
*/
as
set nocount on

declare @license_module_code varchar(50),
 @days int,
 @module_seats int,
 @license_change_reason_id int,
 @order_item_id int,
 @module_product_id int,
 @license_id int,
 @response_code int,
 @message varchar(100)

begin try
 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 -- 1.) select
 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 select @license_change_reason_id = 3

 declare @parameters table (parameter_name varchar(50), input_value nvarchar(100))

 -- 1.1) parse json
 insert into @parameters(parameter_name,input_value)
 select parameter_name = NAME, input_value = case when StringValue = '' then null else StringValue end
 from dbo.fn_parse_json(@trial_registration_json)
 where Object_ID is null and StringValue is not null

 -- 1.2) select out values
 select @license_module_code = input_value from @parameters where parameter_name = 'license_module_code'
 select @days = input_value from @parameters where parameter_name = 'days'
 select @module_seats = input_value from @parameters where parameter_name = 'module_seats'

 -- 1.3) find module product
 select @module_product_id = m.product_id
 from product_license_module m
 inner join product p
  on p.product_id = m.product_id
 inner join license_module lm
  on lm.license_module_id = m.license_module_id
 where p.product_type_id = 4 and lm.license_module_code = @license_module_code and m.days = @days

 -- 1.4) @license_id
 select @license_id = license_id
 from license
 where keycode = @keycode

 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 -- 2.) insert
 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 exec usp_license_update_license_module_license @license_id, @module_product_id, @license_change_reason_id, @order_item_id, @module_seats

end try

begin catch
 set @response_code = -200
 set @message = case when @message is null then 'insert license_module_license failed'
      else @message end

 declare @DBName nvarchar(128)
 set @DBName = db_name()
 exec usp_LogError @ErrorDB = @DBName

end catch;
