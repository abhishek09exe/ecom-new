-- Source: QA DB via sp_helptext. Referenced in doc 04 (04_SKYRISE_KEYCODE_GENERATION.md), Section 5.
-- IMPORTANT: despite the "sky_" naming, this is a GENERIC external-integration queue inserter,
-- NOT Skyrise-specific. The msp_registration template it's used for from usp_trial_insert_trial
-- routes to the legacy WorldAuth system, not Skyrise. See doc 04 Section 5 for the full breakdown.

CREATE   PROCEDURE [dbo].[usp_sky_insert_update] (
 @license_id int,
 @sky_template_id tinyint,
 @process_date datetime = null,
 @reregister tinyint = NULL,
 @sky_update_json NVARCHAR(MAX) = NULL
)

AS
/*
 DATE       AUTHOR       REMARKS
 2014-02-10 esmart       Initial creation.
 2015-06-05 esmart       Enable reregistration
 2024-05-09 gblandford   https://jira.opentext.com/browse/ECOM-4416 - extended sky_update insert to allow additional JSON block in request. This is to support
             additional parameters that may be required by the API endpoint.

 DESCRIPTION
 insert sky_update record and any attached JSON

 DEPENDENCIES
 dbo.usp_cart_update_order_license
 dbo.usp_license_insert_child_license
 dbo.usp_license_insert_license_migration
 dbo.usp_license_service_insert_license
 dbo.usp_license_update_license_module_license
 dbo.usp_message_update_license_message_deprovision
 dbo.usp_trial_insert_trial
 dbo.usp_partner_update_partner_license_transaction

*/

SET NOCOUNT ON

DECLARE @sky_update_type_id TINYINT,
  @sky_update_archive_id INT,
  @sky_update_id INT,
  @response_code INT,
  @message VARCHAR(100)

BEGIN TRY

-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 1.) select
-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

-- 1.1) @process_date
IF @process_date IS NULL
 BEGIN
 SELECT @process_date = GETDATE()
 end

-- 1.2) @sky_update_type_id
select @sky_update_type_id = s.sky_update_type_id
from sky_template_update_type s
inner join (select sky_template_update_type_id = min(sky_template_update_type_id)
   from sky_template_update_type
   where sky_template_id = @sky_template_id) t
  on s.sky_template_update_type_id = t.sky_template_update_type_id

-- 1.3)
if @reregister = 1
 begin
  update sky_update_archive
  set sky_update_status_id = 4
  where license_id = @license_id and sky_update_type_id = @sky_update_type_id and sky_update_status_id = 1
 end

-- 1.3) do not duplicate msp_registration for the same license
if @sky_update_type_id = 1
 begin
 select @sky_update_archive_id = sky_update_archive_id
 from sky_update_archive
 where license_id = @license_id and sky_update_type_id = @sky_update_type_id and sky_update_status_id = 1

 if @sky_update_archive_id is not null
  begin
  return
  end
 end

-- 1.4) do not duplicate msp_registration for the same license
if @sky_update_type_id = 1
 begin
 select @sky_update_id = sky_update_id
 from sky_update
 where license_id = @license_id and @sky_update_type_id = @sky_update_type_id

 if @sky_update_id is not null
  begin
  return
  end
 end

-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
-- 2.) insert
-- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

-- 2.1) insert update
insert into sky_update(license_id, sky_template_id, sky_update_type_id,process_date)
values(@license_id, @sky_template_id, @sky_update_type_id,@process_date)

-- 2.1) insert sky_update_json
SELECT @sky_update_id = SCOPE_IDENTITY();

IF ISNULL(@sky_update_json, '' ) > '' BEGIN

  INSERT INTO dbo.sky_update_json (
   sky_update_id,
   sky_update_json
  ) VALUES (
   @sky_update_id,
   @sky_update_json
  )

 END

end try

begin catch

 set @response_code = -200
 set @message = case when @message is null then 'select failed'
      else @message end

 declare @DBName nvarchar(128)
 set @DBName = db_name()
 exec usp_LogError @ErrorDB = @DBName

end catch;
