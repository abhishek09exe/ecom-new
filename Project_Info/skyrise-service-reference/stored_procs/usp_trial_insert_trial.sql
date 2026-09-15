-- Source: QA DB via sp_helptext. Referenced in doc 04 (04_SKYRISE_KEYCODE_GENERATION.md), Section 4.
-- Contains the DB-side keycode fallback call (usp_license_generate_keycode_cart) and the
-- MSP/WorldAuth registration sync hook (usp_sky_insert_update) -- see doc 04 Section 4/5.

CREATE PROCEDURE [dbo].[usp_trial_insert_trial]

(
 @trial_registration_id INT,
 @keycode VARCHAR(40) = NULL,
 @account_order_json VARCHAR(MAX) = NULL
)

AS
      /*
            DATE          AUTHOR       REMARKS
            2014-02-10    esmart       Initial creation.
            2014-03-26    esmart       Fix partner_account_code
            2014-04-09    esmart       Remove license_feature_license
            2014-11-17    esmart       Add eloqua_guid
            2015-03-05    cnovac       Added @company_type_name = null to usp_customer_insert_customer_service call
            2015-05-06    esmart       Remove partner_id from restriction on 6.2
            2015-08-24    acoffel      License channel update
            2016-04-15    esmart       Add @salesforce_license_id
            2016-07-12    acoffel      6.2 Extend additional process into update_eloqua_trial template
            2016-08-11    acoffel      fix sfdc op update process timing
            2017-02-02    esmart       trial_registration_json
            2017-06-21    esmart       Move MSP registration up to 3.6
            2017-07-05    esmart       Fix bug that is creating multiple license for consumer trials with seats > 1
            2017-07-06    esmart       sky licensing support
            2017-09-05    esmart       restrict interface to eloqua for partner trials
            2018-04-30    rvinoche     remove insert into sky_api_license as it's done in sub routine
            2018-06-13    acoffel      remove -- 6.2) @eloqua_guid
            2018-07-10    cnovac       added code for WIFI at bottom and @account_order_json
            2018-10-11    cnovac       Updated call to usp_account_order_finalize
            2019-06-21    esmart       add @company_id_input
            2019-12-03    esmart       Remove buffer on sky insert
            2020-02-13    esmart       Modify GSM registration insert to only include SAEP (3.6)
            2020-03-05    esmart       Refactor to use @trial_registration_json and vault
            2020-04-20    cnovac       Fix to set @license_category_name and @account_order_staging_id for WIFI
   2020-07-23    wbarton  Retiring the restriction for .ru and yandex. The webservers for api.webroot block repeat-requests, which was why this code was put in place.
   2020-09-16    abelenkiy More accurate hadling of json and removed unused vars
   2025-10-27    psatish  D-5045328 Set the variable @ecom_create_license to 1 if a license generates through ecom

            DESCRIPTION
            insert trial
      */

      set nocount on

      declare @first_name nvarchar(225),
               @last_name nvarchar(225),
               @company_name nvarchar(255),
   @company_type_name varchar(50),
               @phone_number varchar(64),
               @customer_email varchar(100),
               @address_1 nvarchar(255),
               @address_2 nvarchar(255),
               @city nvarchar(130),
               @state nvarchar(2),
               @postal_code nvarchar(32),
               @country varchar(75),
               @license_category_name varchar(10),
               @license_seats int,
               @trial_days int,
               @partner_key varchar(36),
               @partner_account_key varchar(36),
               @sfdc_lead_id varchar(18),
               @sfdc_opportunity_id varchar(18),
               @sfdc_trial_id varchar(18),
               @partner_id int,
               @partner_account_id int,
               @product_id int,
               @license_distribution_method_id int,
               @start_date datetime,
               @end_date datetime,
               @months int,
               @license_id int,
               @trial_id int,
               @customer_id int,
               @customer_id_input int,
               @request_type varchar(20),
   @vendor_id int,
               @vendor_customer_code varchar(100),
               @opt_in int,
               @salesforce_account_id varchar(18),
               @salesforce_contact_id varchar(18),
               @salesforce_license_id varchar(18),
               @transaction_user varchar(20) = 'trial_registration',
               @process_date datetime,
               @product_line_id int,
               @trial_registration_json nvarchar(max),
               @module_seats int,
               @license_change_reason_id int,
               @order_item_id int,
               @module_product_id int,
               @account_order_staging_id int,
   @company_id_input int,
   @license_service_type_name varchar(50),
   @return_license_service_id int,
               @response_code int,
               @message varchar(100),
   @ecom_create_license TINYINT


      BEGIN TRY


      -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
      -- 0.) json
      -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

  -- 0.1) @trial_registration_json
  SELECT @trial_registration_json = j.trial_registration_json
  FROM trial_registration_json j
  WHERE j.trial_registration_id = @trial_registration_id

      -- 0.2) select values
    IF ISJSON(@trial_registration_json) = 1
   BEGIN

   SELECT @license_service_type_name = 'license_create'

   -- 0.2.1) update json with keycode
    IF @keycode IS NULL OR @keycode = ''
    BEGIN
     SELECT @product_id = 152200130

     SELECT @license_distribution_method_id = license_distribution_method_id
     FROM trial_registration t
     WHERE t.trial_registration_id = @trial_registration_id


     EXEC usp_license_generate_keycode_cart @product_id,
                 @license_distribution_method_id,
                 @keycode OUTPUT,
                 @response_code OUTPUT,
                 @message OUTPUT
     SET @ecom_create_license = 1
     SELECT @trial_registration_json=JSON_MODIFY(@trial_registration_json,'$.ecom_create_license',@ecom_create_license)
    END

    -- modify json
    SELECT @trial_registration_json = JSON_MODIFY(@trial_registration_json,'$.keycode',@keycode)

    -- trial_registration_json
    UPDATE trial_registration_json
    SET trial_registration_json = @trial_registration_json
    WHERE trial_registration_id = @trial_registration_id

    -- trial_registration
    UPDATE trial_registration
    SET keycode = @keycode
    WHERE trial_registration_id = @trial_registration_id

   -- 0.2.2) license_create
   EXEC usp_license_service_insert_service @license_service_type_name, @trial_registration_json, @return_license_service_id

   -- 0.2.3) @license_id
   SELECT @license_id = l.license_id, @customer_id = l.customer_id
   FROM license l
   WHERE l.keycode = @keycode

   -- 0.2.4) partner
   SELECT @partner_key = JSON_VALUE(@trial_registration_json,'$.partner.partner_key')

   SELECT @partner_id = p.partner_id
   FROM partner p
   WHERE p.partner_key = @partner_key

   -- return
   END
  ELSE --'else' code is obsolete because now php always passes json into [usp_trial_insert_trial_registration]; code can be removed once json standardization across systems is complete
   BEGIN

     -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
     -- 1.) select
     -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

     -- 1.1) select values
     select @first_name = first_name,
    @last_name = last_name,
    @company_name = company_name,
    @company_type_name = company_type_name,
    @phone_number = phone_number,
    @customer_email = customer_email,
    @address_1 = address_1,
    @address_2 = address_2,
    @city = city,
    @state = case when state is null then '' else state end,
    @postal_code = postal_code,
    @country = country,
    @partner_key = partner_key,
    @partner_account_key = partner_account_key,
    @product_id = product_id,
    @license_category_name = license_category_name,
    @license_distribution_method_id = license_distribution_method_id,
    @license_seats = license_seats,
    @request_type = request_type,
    @sfdc_lead_id = sfdc_lead_id,
    @sfdc_opportunity_id = sfdc_opportunity_id,
    @sfdc_trial_id = sfdc_trial_id,
    @vendor_customer_code = partner_account_code,
    @salesforce_license_id = salesforce_license_id,
    @trial_days = trial_days,
    @opt_in = opt_in
     from trial_registration
     where trial_registration_id = @trial_registration_id

   select @account_order_staging_id = json_value(@account_order_json, '$.account_order_staging_id')

     -- 1.2) check for duplicate
     select @trial_id = trial_id
     from trial
     where trial_registration_id = @trial_registration_id

     if @trial_id is not null
     begin
     select trial_id = @trial_id

     select @license_id = license_id from trial where trial_id = @trial_id
     goto insert_sfdc_opportunity_update
     return
     end

     -- 1.3) update keycode
     if @keycode is not null
   begin
    update trial_registration
    set keycode = @keycode
    where trial_registration_id = @trial_registration_id
   end

     -- 1.4) @partner_id
     select @partner_id = partner_id
     from partner
     where partner_key = @partner_key

     -- 1.5) @partner_account_id
     select @partner_account_id = partner_account_id
     from partner_account
     where partner_account_key = @partner_account_key

     -- 1.6) @product_line_id
     select @product_line_id = product_line_id
     from product_line_product
     where product_id = @product_id

     if @product_line_id not in (300,55) and @license_category_name != 'WIFI'
   begin
    select @license_seats = 1
   end

     -- 1.7) handle iso country in place of description
     if not exists (select country_id from lu_countries where description = @country)
   begin
    select @country = description
    from lu_countries
    where iso = @country
   end

   -- 1.8) correct invalid company_type_name
   if @company_type_name is not null
    begin
    select @company_type_name = case
            when @company_type_name = 'Managed Service Provider' then 'MSP'
            when @company_type_name = 'MSP/VAR (Hybrid)' then 'MSP/Reseller Hybrid'
            when @company_type_name = 'Value Added Reseller' then 'Reseller'
            when @company_type_name = '' then 'Business'
            else @company_type_name
           end
    end

     -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
     -- 2.) customer
     -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

     -- 2.1) select customer match
     exec usp_customer_insert_customer_service @customer_email, @first_name, @last_name, @company_name, @company_type_name, @phone_number, @address_1, @address_2, @city, @state, @postal_code, @country, @vendor_id, @vendor_customer_code, @partner_id, @opt_in, @salesforce_account_id, @salesforce_contact_id, @transaction_user, @customer_id_input, @company_id_input, @customer_id output

     -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
     -- 3.) license
     -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

     -- 3.1) license
     declare @keycodes table (keycode varchar(40))

     -- 3.2) insert license
     insert into @keycodes(keycode)
     exec usp_license_insert_license_service @product_id,@license_distribution_method_id,@license_seats,@start_date,@end_date,@months,@keycode,null,@trial_registration_id

     -- 3.3) select @license_id
     select @license_id = l.license_id
     from @keycodes k
     inner join license l
     on k.keycode = l.keycode

     -- 3.4) update license.customer_id
     update license set customer_id = @customer_id where license_id = @license_id

     -- 3.5) @salesforce_license_id
     update license_key set salesforce_license_id = @salesforce_license_id where license_id = @license_id

     -- 3.6) MSP registration
     if @request_type = 'create_master' and @license_category_name = 'SAEP'
   begin
    declare @sky_template_id tinyint
    select @sky_template_id = sky_template_id from sky_template where sky_template_name = 'msp_registration'

    select @process_date = getdate()

    exec [usp_sky_insert_update] @license_id, @sky_template_id, @process_date
   end

     -- 3.7) @trial_registration_json
     select @trial_registration_json = trial_registration_json
     from trial_registration_json
     where trial_registration_id = @trial_registration_id

     if @trial_registration_json is not null
   begin
    select @license_change_reason_id = 3

    declare @json_module_data table (license_module_code varchar(50), days int,     module_seats int)
    declare @module_products table (module_id int identity(1,1), module_product_id int, module_seats int)

    declare @module_id int, @modules int

    -- 2.1) parse json
    insert into @json_module_data (license_module_code, days, module_seats)
    select license_module_code, days, module_seats
    from openjson(@trial_registration_json)
    with (license_module_code varchar(50) '$.license_module_code',days int '$.days',module_seats int '$.module_seats')

    -- 2.2) get products
    insert into @module_products (module_product_id, module_seats)
    select p.product_id, t.module_seats
    from @json_module_data t
    inner join license_module lm
      on lm.license_module_code = t.license_module_code
    inner join product_license_module pm
      on pm.license_module_id = lm.license_module_id and pm.days = t.days
    inner join product p
      on p.product_id = pm.product_id and p.product_type_id = 4

    select @modules = max(module_id), @module_id = 1 from @module_products

    -- 2.3) add modules
    while @modules >= @module_id
    begin
     select @module_product_id = module_product_id, @module_seats = module_seats
     from @module_products
     where module_id = @module_id

     exec usp_license_update_license_module_license @license_id, @module_product_id, @license_change_reason_id, @order_item_id, @module_seats

     select @module_id = @module_id + 1
    end

   end
  end
      -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
      -- 4.) trial
      -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

      -- 4.1) insert trial
      insert into trial(trial_registration_id, license_id, customer_id)
      values(@trial_registration_id, @license_id, @customer_id)

      select @trial_id = scope_identity()

      -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
      -- 5.) post process
      -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

      -- 5.3) insert partner_trial
      if @partner_id is not null
            begin
            insert into partner_trial(partner_id, trial_id, partner_account_id)
            values(@partner_id, @trial_id, @partner_account_id)
            end

      -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
      -- 6.0) sfdc update
      -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

      insert_sfdc_opportunity_update:

      declare @sfdc_update_type_id tinyint, @sfdc_template_id tinyint

      -- 6.1) insert sfdc_trial
      if @sfdc_opportunity_id is not null or @sfdc_lead_id is not null or @sfdc_trial_id is not null or @salesforce_license_id is not null
            begin

            -- 6.1.2) @sfdc_template_id
            select @sfdc_template_id = sfdc_template_id
            from sfdc_template
            where sfdc_template_name = 'update_license'

            -- 6.1.3)
            select @sfdc_update_type_id = sfdc_update_type_id
            from sfdc_template_update_type
            where sfdc_template_update_type_id = (select min(sfdc_template_update_type_id)
                                                                        from sfdc_template_update_type
                                                                        where sfdc_template_id = @sfdc_template_id)

            -- 6.1.4) insert sfdc_opportunity_update
            insert into sfdc_opportunity_update(license_id, trial_id, sfdc_template_id, sfdc_update_type_id, process_date)
            values(@license_id, @trial_id, @sfdc_template_id, @sfdc_update_type_id, getdate())

            end

      -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
      -- 7.0)  WIFI specific code to log which account order staging id is associated to this trial
      -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

  select @license_category_name = license_category_name
  from trial_registration
  where trial_registration_id = @trial_registration_id

  select @account_order_staging_id = json_value(@account_order_json, '$.account_order_staging_id')

  if @license_category_name = 'WIFI' and @account_order_staging_id is not null
  begin
   set @account_order_json = (select @account_order_staging_id as account_order_staging_id, @trial_id as trial_id, @trial_days as days for json path,without_array_wrapper)
   exec usp_account_order_finalize @license_id = @license_id, @account_order_json = @account_order_json
  end

      select trial_id = @trial_id

      end try

      begin catch

            set @response_code = -200
            set @message = case when @message is null then 'select failed'
                                              else @message end

            declare @DBName nvarchar(128)
            set @DBName = db_name()
            exec usp_LogError @ErrorDB = @DBName

      end catch;
