-- Source: QA DB via sp_helptext. Referenced in doc 04 (04_SKYRISE_KEYCODE_GENERATION.md), Section 3.1
-- and doc 01 (01_ENDPOINT_PROTECTION_TRIAL_MIGRATION.md), Section 7.1.
-- CRITICAL: on the SUCCESS path, the result set has NO response_code column (null >= 0 = true in PHP).
-- Only duplicate (-110) and error (-200) paths return a response_code. Do not default it to 0 in C#.
-- Duplicate path is NOT read-only: it writes an sfdc_trial_audit row + updates two tables before
-- returning -110 if the incoming sfdc_trial_id differs from what's stored.

CREATE PROCEDURE [dbo].[usp_trial_insert_trial_registration]

(
 @first_name nvarchar(225) = null,
 @last_name nvarchar(225) = null,
 @company_name nvarchar(255) = null,
 @company_type_name varchar(50) = null,
 @phone_number varchar(64) = null,
 @customer_email varchar(100) = null,
 @address_1 nvarchar(255) = null,
 @address_2 nvarchar(255) = null,
 @city nvarchar(130) = null,
 @state nvarchar(2) = null,
 @postal_code nvarchar(32) = null,
 @country varchar(75) = null,
 @language_code varchar(2) = null,
 @location_code varchar(3) = null,
 @license_category_name varchar(10) = null,
 @license_seats int = null,
 @years float = null,
 @trial_days int = null,
 @license_keycode_type_id int = null,
 @partner_key varchar(36) = null,
 @account_user_name varchar(100) = null,
 @partner_account_code varchar(50) = null,
 @sfdc_lead_id varchar(18) = null,
 @sfdc_opportunity_id varchar(18) = null,
 @sfdc_trial_id varchar(18) = null,
 @salesforce_campaign_id varchar(18) = null,
 @partner_product_id varchar(20) = null,
 @sfdc_distributor_id varchar(18) = null,
 @sfdc_reseller_id varchar(18) = null,
 @eloqua_parameters nvarchar(max) = null,
 @license_distribution_method_code char(4) = null,
 @salesforce_license_id varchar(18) = null,
 @trial_registration_json varchar(max) = NULL,
 @opt_in int = null
)

AS
 /*
  DATE       AUTHOR    REMARKS
  2014-02-10 esmart    Initial creation.
  2014-11-17 esmart    Add eloqua_guid
  2015-08-05 esmart    Move @license_distribution_method_code to input
  2015-09-11 acoffel   Added NFR disti support
  2016-04-15 esmart    Add @salesforce_license_id
  2017-02-02 esmart    Add @trial_registration_json
  2017-09-08 esmart    Add product_line_id to result set
  2018-07-09 cnovac    Added code to not reset @years when creating WIFI trial
  2018-10-04 esmart    Add company_type_name
  2020-01-24 cnovac    Added @product_years to write to license_registration so that for WIFI we can tell if monthly or annual
  2020-03-05 esmart    Refactor to use @trial_registration_json
  2020-06-28 abelenkiy AXIOM-1297: set @opt_in variable so value passed into usp_customer_insert_customer_service is no longer null
  2020-07-23 wbarton   Retiring the restriction for .ru and yandex. The webservers for api.webroot block repeat-requests, which was why this code was put in place.
  2020-08-28 abelenkiy AXIOM-1764: added handling of customer_type to get company_type_name from company_type_id in json and properly write to trial_registration table

  DESCRIPTION
  insert trial registration
 */

 set nocount on

 declare @trial_registration_id int,
   @request_type varchar(20),
   @product_line_id int,
   @license_category_id tinyint,
   @license_type varchar(50),
   @license_type_id int,
   @product_years float = @years,
   @license_distribution_method_id int,
   @license_module_list varchar(max),
   @storage_gb int,
   @cart_discount_id int,
   @cart_type_id int,
   @capability_id int,
   @capability_type_id int,
   @capability_activation_days int,
   @product_id int,
   @trial tinyint,
   @partner_account_key varchar(36),
   @current_sfdc_trial_id varchar(18),
   @sfdc_update_type_id tinyint,
   @sfdc_template_id tinyint,
   @trial_id int,
   @company_type_id int,
   @response_code int,
   @message varchar(100)

 declare @license_category_table table (license_category_name varchar(10),license_seats int,days int,start_date datetime,expiration_date datetime,category_type_name varchar(50),item_hierarchy_id tinyint)

 begin try

 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 -- 0.) json
 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

 if isjson(@trial_registration_json) = 1
 begin
 declare @keycode varchar(40),
   @license_category nvarchar(max),
   @customer nvarchar(max),
   @partner nvarchar(max),
   @sfdc nvarchar(max)

 -- 0.1) parse json
  select @keycode = keycode,
    @license_distribution_method_code = case when license_distribution_method_code = '' then null else license_distribution_method_code end,
    @language_code = language_code,
    @location_code = location_code,
    @license_keycode_type_id = license_keycode_type_id,
    @license_category = license_category,
    @partner = partner,
    @customer = customer,
    @sfdc = sfdc,
    @eloqua_parameters = eloqua_parameters
  from openjson(@trial_registration_json)
  with (keycode varchar(40) '$.keycode',
   language_code varchar(2) '$.language_code',
   location_code varchar(40) '$.location_code',
   license_distribution_method_code char(4) '$.license_distribution_method_code',
   license_keycode_type_id int '$.license_keycode_type_id',
   license_category nvarchar(max) as json,
   customer nvarchar(max) as json,
   partner nvarchar(max) as json,
   sfdc nvarchar(max) as json,
   eloqua_parameters nvarchar(max) as json)

 -- @license_category
 insert into @license_category_table(license_category_name,license_seats,days,start_date,expiration_date,category_type_name,item_hierarchy_id)
 select license_category_name,license_seats,days,start_date,expiration_date,category_type_name,item_hierarchy_id
 from openjson(@license_category)
 with (license_category_name varchar(10) '$.license_category_name',
    license_seats int '$.license_seats',
    days int '$.days',
    start_date datetime '$.start_date',
    expiration_date datetime '$.expiration_date',
    category_type_name varchar(50) '$.category_type_name',
    item_hierarchy_id tinyint '$.item_hierarchy_id')
 where license_category_name is not null

 select @license_category_name = t.license_category_name, @license_seats = t.license_seats, @years = 0, @trial_days = t.days
 from @license_category_table t
 where t.item_hierarchy_id = 1

 -- @customer
 select @customer_email = j.customer_email,
   @first_name = j.first_name,
   @last_name = j.last_name,
   @company_name = j.company_name,
   @company_type_name = ct.company_type_name,
   @phone_number = j.phone_number,
   @address_1 = j.address_1,
   @address_2 = j.address_2,
   @city = j.city,
   @state = j.state,
   @postal_code = j.postal_code,
   @country = j.country,
   @opt_in = j.opt_in
 from openjson(@customer)
 with (customer_email varchar(100) '$.customer_email',
   first_name nvarchar(225) '$.first_name',
   last_name nvarchar(225) '$.last_name',
   company_name nvarchar(255) '$.company_name',
   company_type_name varchar(20) '$.company_type_name',
   company_type_id int '$.company_type_id',
   phone_number varchar(64) '$.phone_number',
   address_1 nvarchar(255) '$.address_1',
   address_2 nvarchar(255) '$.address_2',
   city nvarchar(130) '$.city',
   state nvarchar(2) '$.state',
   postal_code nvarchar(32) '$.postal_code',
   country varchar(75) '$.country',
   opt_in int '$.opt_in') j
 left join company_type ct
  on ct.company_type_id = j.company_type_id

 -- @partner
 select @partner_key = partner_key,
   @account_user_name = account_user_name,
   @partner_account_code = partner_account_code,
   @partner_product_id = partner_product_id
 from openjson(@partner)
 with (partner_key varchar(36) '$.partner_key',
   account_user_name varchar(36) '$.account_user_name',
   partner_account_code varchar(36) '$.partner_account_code',
   partner_product_id varchar(36) '$.partner_product_id')

 -- @sfdc
 select @sfdc_lead_id = sfdc_lead_id,
   @sfdc_opportunity_id = sfdc_opportunity_id,
   @sfdc_trial_id = sfdc_trial_id,
   @salesforce_campaign_id = salesforce_campaign_id,
   @sfdc_distributor_id = sfdc_distributor_id,
   @sfdc_reseller_id = sfdc_reseller_id,
   @salesforce_license_id = salesforce_license_id
 from openjson(@sfdc)
 with (sfdc_lead_id varchar(18) '$.sfdc_lead_id',
  sfdc_opportunity_id varchar(18) '$.sfdc_opportunity_id',
  sfdc_trial_id varchar(18) '$.sfdc_trial_id',
  salesforce_campaign_id varchar(18) '$.salesforce_campaign_id',
  sfdc_distributor_id varchar(18) '$.sfdc_distributor_id',
  sfdc_reseller_id varchar(18) '$.sfdc_reseller_id',
  salesforce_license_id varchar(18) '$.salesforce_license_id')
 end

 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 -- 1.) select
 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

 if @license_keycode_type_id is null
  begin
  select @license_keycode_type_id = 1
  end

 -- 1.1) check for duplicate
 select @trial_registration_id = tr.trial_registration_id, @current_sfdc_trial_id = sfdc_trial_id, @trial_id = trial_id
 from trial_registration tr
 left join trial t
  on tr.trial_registration_id = t.trial_registration_id
 where tr.customer_email = @customer_email and tr.license_category_name = @license_category_name and tr.license_keycode_type_id = @license_keycode_type_id

 -- 1.1.1) @sfdc_trial_id
 if @trial_registration_id is null
  begin
  select @trial_registration_id = tr.trial_registration_id
  from trial_registration tr
  where tr.sfdc_trial_id = @sfdc_trial_id
  end

 -- 1.1.2) update trial
 if @trial_registration_id is not null
  begin
  if @sfdc_trial_id is not null and @sfdc_trial_id <> @current_sfdc_trial_id
   begin

   -- 1.1.2.1) insert audit
   insert into sfdc_trial_audit(sfdc_trial_id, trial_id, salesforce_lead_id, salesforce_opportunity_id, salesforce_trial_id)
   select sfdc_trial_id, trial_id, salesforce_lead_id, salesforce_opportunity_id, salesforce_trial_id
   from sfdc_trial
   where salesforce_trial_id = @current_sfdc_trial_id

   -- 1.1.2.2) update trial_registration
   update trial_registration
   set sfdc_trial_id = @sfdc_trial_id
   where trial_registration_id = @trial_registration_id

   -- 1.1.2.3) update sfdc_trial
   update sfdc_trial
   set salesforce_trial_id = @sfdc_trial_id
   where salesforce_trial_id = @current_sfdc_trial_id

   end
    select response_code = -110, message='Duplicate registration', trial_registration_id = @trial_registration_id
  return
  end

 -- 1.2) @partner_account_key
 select @partner_account_key = pa.partner_account_key
 from partner_account pa
 inner join account a
  on pa.account_id = a.account_id
 where a.account_user_name = @account_user_name

 -- 1.3) @partner_product_id
 select @product_id = pp.product_id, @product_line_id = pl.product_line_id
 from partner_service_product pp
 inner join partner p
  on pp.partner_id = p.partner_id
 inner join product_line_product pl
  on pp.product_id = pl.product_id
 where pp.partner_product_id = @partner_product_id and p.partner_key = @partner_key

 -- 1.4) @years
 if @trial_days is not null and @trial_days > 0
  begin
  select @years = 0
  end

 -- 1.5 language/location
 if len(@location_code) = 2
  begin
  select @location_code = iso3 from lu_countries where iso = @location_code
  end

  -- 1.5.1) @language_code
  if @language_code is null or @language_code = ''
   begin
   select @language_code = 'EN'
   end

  if @location_code is null or @location_code = ''
   begin
   select @location_code = 'USA'
   end

 -- 1.6) @salesforce_campaign_id
 if len(@salesforce_campaign_id) = 15 and @salesforce_campaign_id is not null
  begin
  select @salesforce_campaign_id = dbo.fn_ConvertSalesforceID_15to18(@salesforce_campaign_id)
  end

 -- 1.7) @license_distribution_method_code
 if @license_distribution_method_code is not null
  begin
   select @license_distribution_method_id = license_distribution_method_id
   from license_distribution_method
   where license_distribution_method_code = @license_distribution_method_code

   if @license_distribution_method_id is null
    begin
    select @license_distribution_method_code = null
    end
  end

 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 -- 2.) product
 -- --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

 if @product_id is null
  begin

  -- 2.2) @product_line_id
  select @product_line_id = product_line_id
  from license_category_product_line pl
  inner join license_category c
   on pl.license_category_id = c.license_category_id
  where c.license_category_name = @license_category_name and
   language_code = @language_code and
   location_code = @location_code

  -- 2.1) select @product_set
  declare @product_set table(line_item int identity(1,1), product_id int, product_description varchar(100), quantity int default 1, retail_price money, upgrade_price money, discount money, standard_discount money, net_price money, product_type_id int, license_category_name varchar(10), seats int, years float, p_type varchar(20), cart_item_class_id tinyint, cart_item_bundle_id int, keycode varchar(40), currency_id tinyint, currency_code char(3), currency_description varchar(20), symbol_html varchar(10), symbol_utf8 nvarchar(10), symbol_text varchar(10), cart_discount_id int)

  if @product_line_id in (55,300)
   begin
   -- 2.1.1) enterprise
    insert into @product_set(product_id,product_description,retail_price,upgrade_price,discount,standard_discount,net_price,product_type_id,license_category_name,seats,years,p_type, currency_id, currency_code, currency_description, symbol_html, symbol_utf8, symbol_text, cart_discount_id)
    exec usp_cart_select_new_product_set_ENT @license_category_name,@license_seats,@storage_gb,@years,@cart_discount_id,@language_code,@location_code,@cart_type_id,@trial_days,@license_keycode_type_id
   end
  else
   begin
   -- 2.1.2) consumer
    insert into @product_set(product_id,product_description,retail_price,upgrade_price,discount,standard_discount,net_price,product_type_id,license_category_name,seats,years,p_type, currency_id, currency_code, currency_description, symbol_html, symbol_utf8, symbol_text, cart_discount_id)
    exec usp_cart_select_new_product_set @license_category_name,@license_seats,@storage_gb,@years,@cart_discount_id,@language_code,@location_code,@cart_type_id,@trial_days,@license_keycode_type_id
   end

  -- 2.3) license type
  select @product_id = p.product_id, @license_category_id = lc.license_category_id, @product_line_id = pl.product_line_id, @capability_type_id = c.capability_type_id, @capability_activation_days = c.capability_activation_days, @storage_gb = s.storage_gb, @license_keycode_type_id = pr.license_keycode_type_id
  from @product_set p
  inner join product pr
   on p.product_id = pr.product_id
  inner join product_line_product pl
   on p.product_id = pl.product_id
  inner join product_license_category plc
   on p.product_id = plc.product_id
  inner join license_category lc
   on plc.license_category_id = lc.license_category_id
  inner join product_capability c
   on p.product_id = c.product_id and c.capability_id = lc.base_capability_id and c.capability_id = lc.base_capability_id
  left join product_storage s
   on p.product_id = s.product_id

  end
 else
  begin
  select @product_id = p.product_id, @license_category_id = lc.license_category_id, @product_line_id = pl.product_line_id, @capability_type_id = c.capability_type_id, @capability_activation_days = c.capability_activation_days, @storage_gb = s.storage_gb, @license_keycode_type_id = p.license_keycode_type_id, @license_category_name = lc.license_category_name
  from product p
  inner join product_line_product pl
   on p.product_id = pl.product_id
  inner join product_license_category plc
   on p.product_id = plc.product_id
  inner join license_category lc
   on plc.license_category_id = lc.license_category_id
  inner join product_capability c
   on p.product_id = c.product_id and c.capability_id = lc.base_capability_id and c.capability_id = lc.base_capability_id
  left join product_storage s
   on p.product_id = s.product_id
  where p.product_id = @product_id
  end

 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 -- 3.) set values
 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

 select @license_type_id = case
        when @product_line_id in (300,55) and @capability_type_id = 1  then 4
        when @product_line_id in (300,55) and @capability_type_id <> 1  then 104
        when @product_line_id not in (300,55) and @capability_type_id = 1  then 1
        when @product_line_id not in (300,55) and @capability_type_id <> 1  then 101
       end

 select @license_type = case
        when @product_line_id in (300,55) and @capability_type_id = 1 then 'enterprise,full'
        when @product_line_id in (300,55) and @capability_type_id <> 1 then 'enterprise,trial'
        when @product_line_id not in (300,55) and @capability_type_id = 1 then 'consumer,full'
        when @product_line_id not in (300,55) and @capability_type_id <> 1 then 'consumer,trial'
       end

 -- 2.4) request type
 select @request_type = case
        when @license_keycode_type_id = 3 then 'create_master'
        else 'create_key'
       end

 -- 2.5) license_module_list
 select @license_module_list = coalesce(@license_module_list+',','')+ cast(lm.license_module_code+'='+convert(varchar(20),t.license_module_type_name) as varchar(100))
 from license_category_module m
 inner join license_module lm
  on m.license_module_id = lm.license_module_id
 inner join license_module_type t
  on m.license_module_type_id = t.license_module_type_id
 inner join license_category c
  on m.license_category_id = c.license_category_id
 where c.license_category_name = @license_category_name

 -- 2.6) @license_distribution_method_code
 if @license_distribution_method_code <> 'NFRS' or @license_distribution_method_code is NULL
  begin
   select @license_distribution_method_code = m.license_distribution_method_code, @license_distribution_method_id = m.license_distribution_method_id
   from partner p
   inner join partner_license_distribution_method pm
    on p.partner_id = pm.partner_id
   inner join license_distribution_method m
    on pm.license_distribution_method_id = m.license_distribution_method_id
   where p.partner_key = @partner_key
  end

 if @license_distribution_method_code is null
  begin
   print 'new disti'
   select @trial = case when @trial_days > 0 then 1 else 0 end

   exec usp_product_select_license_distribution_method @product_line_id,@license_keycode_type_id,@trial,@license_category_id,@license_distribution_method_id OUTPUT
   select @license_distribution_method_code = license_distribution_method_code from license_distribution_method where license_distribution_method_id =  @license_distribution_method_id
  end

 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 -- 3.) results
 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

 -- 3.1) insert registration
 insert into trial_registration(first_name, last_name, company_name, company_type_name, phone_number, customer_email, address_1, address_2, city, state, postal_code, country, language_code,
   location_code, license_category_name, license_seats, years, trial_days, license_keycode_type_id, partner_key, partner_account_key, partner_account_code, sfdc_lead_id, sfdc_opportunity_id,
   product_id, license_distribution_method_id, request_type, sfdc_trial_id, salesforce_campaign_id, sfdc_distributor_id, sfdc_reseller_id, eloqua_parameters, salesforce_license_id, opt_in)
 values (@first_name, @last_name, @company_name, @company_type_name, @phone_number, @customer_email, @address_1, @address_2, @city, @state, @postal_code, @country, @language_code, @location_code,
   @license_category_name, @license_seats, @product_years, @trial_days, @license_keycode_type_id, @partner_key, @partner_account_key, @partner_account_code, @sfdc_lead_id, @sfdc_opportunity_id,
   @product_id, @license_distribution_method_id, @request_type, @sfdc_trial_id, @salesforce_campaign_id, @sfdc_distributor_id, @sfdc_reseller_id, @eloqua_parameters, @salesforce_license_id, @opt_in)

 select @trial_registration_id = scope_identity()

 -- 3.2) @trial_registration_json
 if isjson(@trial_registration_json) = 1
  begin
  insert into trial_registration_json (trial_registration_id,trial_registration_json)
  values(@trial_registration_id,@trial_registration_json)
  end

 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 -- 4.) results  (NOTE: no response_code column on the success path -- see the C# porting constraint above)
 -- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

 select trial_registration_id = @trial_registration_id,
   request_type = @request_type,

   first_name = @first_name,
   last_name = @last_name,
   customer_email = @customer_email,

   product_line_id = @product_line_id,
   license_category_name = @license_category_name,
   license_category_id=@license_category_id,
   license_module_list = @license_module_list,
   license_type = @license_type,
   license_type_id = @license_type_id,
   capability_activation_days = @capability_activation_days,
   license_seats = @license_seats,
   storage_gb = @storage_gb,
   license_distribution_method_code = @license_distribution_method_code

 end try

 begin catch

  set @response_code = -200
  set @message = case when @message is null then 'select failed'
       else @message end

  declare @DBName nvarchar(128)
  set @DBName = db_name()
  exec usp_LogError @ErrorDB = @DBName

 end catch;
