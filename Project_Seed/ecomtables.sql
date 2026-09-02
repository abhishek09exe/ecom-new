-- ===============================================================================
-- CREATE TABLE SCRIPTS - ALL TABLES
-- ===============================================================================

CREATE TABLE [dbo].[account] (
    [account_id] int IDENTITY(1,1) NOT NULL,
    [account_user_name] varchar(100) NOT NULL,
    [account_password] varchar(64) NOT NULL,
    [password_hint] varchar(50),
    [account_type_id] tinyint NOT NULL,
    [account_status_id] tinyint NOT NULL,
    [parent_account_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [WARN_opt_in] tinyint NOT NULL,
    [email_opt_in] tinyint,
    [password_reset_date] datetime NOT NULL
,
    PRIMARY KEY ([account_id])
);
CREATE TABLE [dbo].[account_audit] (
    [account_audit_id] int IDENTITY(1,1) NOT NULL,
    [account_id] int NOT NULL,
    [account_user_name] varchar(100) NOT NULL,
    [account_password] varchar(64) NOT NULL,
    [password_hint] varchar(50),
    [account_type_id] tinyint NOT NULL,
    [account_status_id] tinyint NOT NULL,
    [parent_account_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([account_audit_id])
);
CREATE TABLE [dbo].[account_creation_status] (
    [account_creation_status_id] int NOT NULL,
    [account_creation_status_description] varchar(100) NOT NULL
,
    PRIMARY KEY ([account_creation_status_id])
);
CREATE TABLE [dbo].[account_customer] (
    [account_id] int NOT NULL,
    [customer_id] int NOT NULL
,
    PRIMARY KEY ([account_id], [customer_id])
);
CREATE TABLE [dbo].[account_device] (
    [account_device_id] int IDENTITY(1,1) NOT NULL,
    [account_id] int NOT NULL,
    [license_id] int NOT NULL,
    [phone_number] varchar(64),
    [device_guid] varchar(50) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([account_device_id])
);
CREATE TABLE [dbo].[account_device_audit] (
    [account_device_audit_id] int IDENTITY(1,1) NOT NULL,
    [account_device_id] int NOT NULL,
    [account_id] int NOT NULL,
    [license_id] int NOT NULL,
    [phone_number] varchar(64),
    [device_guid] varchar(50) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [audit_date] datetime NOT NULL,
    [audit_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([account_device_audit_id])
);
CREATE TABLE [dbo].[account_eula] (
    [account_eula_id] int IDENTITY(1,1) NOT NULL,
    [account_hash_id] varchar(65) NOT NULL,
    [license_id] int,
    [eula_accepted] bit NOT NULL,
    [modified_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([account_eula_id])
);
CREATE TABLE [dbo].[account_ext] (
    [account_ext_id] int IDENTITY(1,1) NOT NULL,
    [account_id] int NOT NULL,
    [encryption_key_hash] varchar(128) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([account_ext_id])
);
CREATE TABLE [dbo].[account_lastpass] (
    [account_lastpass_id] int NOT NULL,
    [account_id] int NOT NULL,
    [account_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([account_lastpass_id])
);
CREATE TABLE [dbo].[account_license] (
    [account_license_id] int IDENTITY(1,1) NOT NULL,
    [account_id] int NOT NULL,
    [license_id] int NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([account_license_id])
);
CREATE TABLE [dbo].[account_license_audit] (
    [account_license_audit_id] int IDENTITY(1,1) NOT NULL,
    [account_license_id] int NOT NULL,
    [account_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_change_reason_id] int NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [audit_date] datetime NOT NULL,
    [audit_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([account_license_audit_id])
);
CREATE TABLE [dbo].[account_license_storage] (
    [account_id] int NOT NULL,
    [license_storage_id] int NOT NULL
,
    PRIMARY KEY ([account_id], [license_storage_id])
);
CREATE TABLE [dbo].[account_message] (
    [account_message_id] int IDENTITY(1,1) NOT NULL,
    [account_id] int NOT NULL,
    [account_message_type_id] tinyint NOT NULL,
    [account_message_name] varchar(20) NOT NULL,
    [image_url] varchar(200),
    [bold_text_value] varchar(500),
    [text_value] varchar(500) NOT NULL,
    [button_text] varchar(100),
    [button_url] varchar(200),
    [sort_order] tinyint,
    [start_date] datetime,
    [end_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([account_message_id])
);
CREATE TABLE [dbo].[account_order] (
    [account_order_id] int IDENTITY(1,1) NOT NULL,
    [account_order_staging_id] int NOT NULL,
    [original_account_order_staging_id] int,
    [trial_id] int,
    [order_header_id] int,
    [license_id] int NOT NULL,
    [license_message_id] int,
    [is_trial_to_full_conversion] bit,
    [modified_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([account_order_id])
);
CREATE TABLE [dbo].[account_order_api_reference] (
    [account_order_api_reference_id] tinyint IDENTITY(1,1) NOT NULL,
    [account_order_api_reference_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([account_order_api_reference_id])
);
CREATE TABLE [dbo].[account_order_bulk_load] (
    [account_order_bulk_load_id] int IDENTITY(1,1) NOT NULL,
    [license_bulk_load_id] int,
    [license_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [account_registration_URL] nvarchar(MAX) NOT NULL
,
    PRIMARY KEY ([account_order_bulk_load_id])
);
CREATE TABLE [dbo].[account_order_history] (
    [account_order_history_id] int IDENTITY(1,1) NOT NULL,
    [account_order_id] int NOT NULL,
    [account_order_staging_id] int NOT NULL,
    [original_account_order_staging_id] int,
    [trial_id] int,
    [order_header_id] int,
    [license_id] int NOT NULL,
    [license_message_id] int,
    [is_trial_to_full_conversion] bit,
    [modified_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([account_order_history_id])
);
CREATE TABLE [dbo].[account_order_staging] (
    [account_order_staging_id] int IDENTITY(1,1) NOT NULL,
    [skytell_notification_id] int,
    [apple_notification_id] int,
    [google_notification_id] int,
    [pending_account_id] varchar(65),
    [account_hash_id] varchar(65),
    [account_id] bigint,
    [console_id] bigint,
    [payment_merchant_id] tinyint NOT NULL,
    [email] varchar(100) NOT NULL,
    [opt_in] bit,
    [merchant_product_id] varchar(100),
    [order_token] varchar(MAX) NOT NULL,
    [order_receipt] varchar(100),
    [vendor_order_date] datetime,
    [vendor_expiration_date] datetime,
    [modified_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([account_order_staging_id])
);
CREATE TABLE [dbo].[account_order_staging_history] (
    [account_order_staging_history_id] int IDENTITY(1,1) NOT NULL,
    [account_order_staging_id] int NOT NULL,
    [skytell_notification_id] int,
    [apple_notification_id] int,
    [google_notification_id] int,
    [pending_account_id] varchar(65),
    [account_hash_id] varchar(65),
    [account_id] bigint,
    [console_id] bigint,
    [payment_merchant_id] tinyint NOT NULL,
    [email] varchar(100) NOT NULL,
    [opt_in] bit,
    [merchant_product_id] varchar(100),
    [order_token] varchar(MAX) NOT NULL,
    [order_receipt] varchar(100),
    [vendor_order_date] datetime,
    [vendor_expiration_date] datetime,
    [modified_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([account_order_staging_history_id])
);
CREATE TABLE [dbo].[account_order_staging_log] (
    [account_order_staging_log_id] int IDENTITY(1,1) NOT NULL,
    [account_order_staging_id] int NOT NULL,
    [account_order_api_reference_id] tinyint NOT NULL,
    [account_order_api_status] int NOT NULL,
    [account_order_api_message] varchar(500) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([account_order_staging_log_id])
);
CREATE TABLE [dbo].[account_order_staging_log_history] (
    [account_order_staging_log_history_id] int IDENTITY(1,1) NOT NULL,
    [account_order_staging_log_id] int NOT NULL,
    [account_order_staging_id] int NOT NULL,
    [account_order_api_reference_id] tinyint NOT NULL,
    [account_order_api_status] int NOT NULL,
    [account_order_api_message] varchar(500) NOT NULL,
    [insert_date] datetime NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([account_order_staging_log_history_id])
);
CREATE TABLE [dbo].[account_reconciliation] (
    [account_reconciliation_id] int IDENTITY(1,1) NOT NULL,
    [account_user_name] varchar(100) NOT NULL,
    [reconciliation_type] varchar(20) NOT NULL,
    [status] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([account_reconciliation_id])
);
CREATE TABLE [dbo].[account_status] (
    [account_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [account_status] varchar(20) NOT NULL
,
    PRIMARY KEY ([account_status_id])
);
CREATE TABLE [dbo].[account_type] (
    [account_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [account_type] varchar(10) NOT NULL,
    [account_type_description] varchar(20) NOT NULL
,
    PRIMARY KEY ([account_type_id])
);
CREATE TABLE [dbo].[account_update_license] (
    [license_id] int NOT NULL
);
CREATE TABLE [dbo].[address_class] (
    [address_class_id] tinyint IDENTITY(1,1) NOT NULL,
    [address_class] varchar(50) NOT NULL
,
    PRIMARY KEY ([address_class_id])
);
CREATE TABLE [dbo].[address_component] (
    [address_component_id] int IDENTITY(1,1) NOT NULL,
    [address_component_name] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([address_component_id])
);
CREATE TABLE [dbo].[address_status] (
    [address_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [address_status] varchar(20) NOT NULL
,
    PRIMARY KEY ([address_status_id])
);
CREATE TABLE [dbo].[address_type] (
    [address_type_id] int IDENTITY(1,1) NOT NULL,
    [address_type_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [address_type_name] varchar(50)
,
    PRIMARY KEY ([address_type_id])
);
CREATE TABLE [dbo].[address_validation_method] (
    [address_validation_method_id] int IDENTITY(1,1) NOT NULL,
    [address_validation_method_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([address_validation_method_id])
);
CREATE TABLE [dbo].[affiliate_build] (
    [build_id] int IDENTITY(1,1) NOT NULL,
    [reference_id] int NOT NULL,
    [alert_product_version_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([build_id])
);
CREATE TABLE [dbo].[affiliate_category_members] (
    [affiliate_code] varchar(16) NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [category_id] smallint NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([affiliate_code], [start_date])
);
CREATE TABLE [dbo].[affiliate_order_history] (
    [affiliate_order_history_id] int IDENTITY(1,1) NOT NULL,
    [affiliate_code] varchar(16) NOT NULL,
    [p_rc] varchar(20),
    [category_id] smallint NOT NULL,
    [product_id] int NOT NULL,
    [order_date] datetime NOT NULL,
    [units_sold] int NOT NULL,
    [units_returned] int NOT NULL,
    [dollars_sold] decimal(11,4),
    [dollars_returned] decimal(11,4) NOT NULL,
    [insert_date] datetime NOT NULL,
    [promotion_code] varchar(32)
,
    PRIMARY KEY ([affiliate_order_history_id])
);
CREATE TABLE [dbo].[affiliate_sales_history] (
    [ash_id] int IDENTITY(1,1) NOT NULL,
    [affiliate_code] varchar(16) NOT NULL,
    [affiliate_subcode] varchar(32),
    [category_id] smallint NOT NULL,
    [product_id] int NOT NULL,
    [invoice_day] datetime NOT NULL,
    [units_sold] int NOT NULL,
    [units_returned] int NOT NULL,
    [dollars_sold] decimal(11,4),
    [dollars_returned] decimal(11,4) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([ash_id])
);
CREATE TABLE [dbo].[affiliate_urls] (
    [affiliate_code] varchar(16) NOT NULL,
    [url] varchar(64) NOT NULL,
    [site_category_id] smallint NOT NULL,
    [unique_visitors_month] int NOT NULL,
    [page_views_month] int NOT NULL,
    [is_main] tinyint NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([url])
);
CREATE TABLE [dbo].[affiliates] (
    [affiliate_code] varchar(16) NOT NULL,
    [assigned_num] int,
    [assigned_num_str] varchar(12),
    [first_name] varchar(32) NOT NULL,
    [last_name] varchar(32) NOT NULL,
    [title] varchar(32),
    [company1] varchar(48),
    [company2] varchar(48),
    [address1] varchar(48) NOT NULL,
    [address2] varchar(48),
    [city] varchar(48) NOT NULL,
    [state_id] char(2) NOT NULL,
    [postal_code] varchar(10) NOT NULL,
    [country_id] smallint NOT NULL,
    [telephone] varchar(20) NOT NULL,
    [fax] varchar(20),
    [email] varchar(64) NOT NULL,
    [description] text,
    [referral_source_id] smallint NOT NULL,
    [referral_name] varchar(50),
    [affiliate_status_id] smallint NOT NULL,
    [tax_id] varchar(32),
    [business_type_id] smallint NOT NULL,
    [commissionable] tinyint,
    [min_check_amount] int NOT NULL,
    [payable_to] varchar(48),
    [applied_date] datetime NOT NULL,
    [approved_date] datetime,
    [notified_date] datetime,
    [terminated_date] datetime,
    [termination_code] smallint NOT NULL,
    [userid] varchar(48) NOT NULL,
    [password] varchar(16) NOT NULL,
    [password_clue_id] smallint,
    [password_clue] varchar(50),
    [fiscal_month] tinyint NOT NULL,
    [fiscal_day] tinyint NOT NULL,
    [trial_availability] tinyint,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([affiliate_code])
);
CREATE TABLE [dbo].[agile_log_json] (
    [agile_log_json_id] int IDENTITY(1,1) NOT NULL,
    [agile_id] varchar(20),
    [agile_json] varchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([agile_log_json_id])
);
CREATE TABLE [dbo].[alert_build] (
    [alert_build_id] int IDENTITY(1,1) NOT NULL,
    [alert_id] int NOT NULL,
    [build_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([alert_build_id])
);
CREATE TABLE [dbo].[alert_location] (
    [alert_location_id] int IDENTITY(1,1) NOT NULL,
    [alert_id] int NOT NULL,
    [country_id] smallint NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([alert_location_id])
);
CREATE TABLE [dbo].[alert_product_version] (
    [alert_product_version_id] int IDENTITY(1,1) NOT NULL,
    [product_version_id] int NOT NULL,
    [product_group_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([alert_product_version_id])
);
CREATE TABLE [dbo].[allstate_registration_status] (
    [allstate_registration_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [allstate_registration_status] varchar(20) NOT NULL
,
    PRIMARY KEY ([allstate_registration_status_id])
);
CREATE TABLE [dbo].[amazon_account] (
    [amazon_account_id] int IDENTITY(1,1) NOT NULL,
    [amazon_account_key] uniqueidentifier NOT NULL,
    [customer_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([amazon_account_id])
);
CREATE TABLE [dbo].[amazon_subscription] (
    [amazon_subscription_id] int IDENTITY(1,1) NOT NULL,
    [amazon_subscription_key] varchar(50) NOT NULL,
    [amazon_account_id] int NOT NULL,
    [license_id] int NOT NULL,
    [amazon_subscription_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([amazon_subscription_id])
);
CREATE TABLE [dbo].[amazon_subscription_audit] (
    [amazon_subscription_audit_id] int IDENTITY(1,1) NOT NULL,
    [amazon_subscription_id] int NOT NULL,
    [amazon_subscription_key] varchar(50) NOT NULL,
    [amazon_account_id] int NOT NULL,
    [license_id] int NOT NULL,
    [amazon_subscription_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [amazon_period] varchar(30),
    [amazon_reason] varchar(30),
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([amazon_subscription_audit_id])
);
CREATE TABLE [dbo].[amazon_subscription_status] (
    [amazon_subscription_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [amazon_subscription_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([amazon_subscription_status_id])
);
CREATE TABLE [dbo].[api] (
    [api_id] int IDENTITY(1,1) NOT NULL,
    [api_description] varchar(100) NOT NULL,
    [api_queue_name] varchar(100) NOT NULL,
    [record_threshold] int NOT NULL,
    [record_threshold_minutes] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([api_id])
);
CREATE TABLE [dbo].[api_alerts] (
    [api_alert_id] int IDENTITY(1,1) NOT NULL,
    [queue_name] varchar(100),
    [api_count] int,
    [alert_date] datetime
,
    PRIMARY KEY ([api_alert_id])
);
CREATE TABLE [dbo].[api_monitor] (
    [api_monitor_id] int IDENTITY(1,1) NOT NULL,
    [api_id] tinyint NOT NULL,
    [api_template_id] tinyint,
    [api_update_type_id] tinyint,
    [api_record_count] int NOT NULL,
    [api_monitor_date] datetime NOT NULL
,
    PRIMARY KEY ([api_monitor_id])
);
CREATE TABLE [dbo].[app_config] (
    [app_config_id] int IDENTITY(1,1) NOT NULL,
    [app_config_name] nvarchar(50) NOT NULL,
    [app_config_description] nvarchar(MAX) NOT NULL,
    [app_config_group_id] int NOT NULL,
    [app_config_type_id] int NOT NULL,
    [app_config_key_value_json] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] nvarchar(50) NOT NULL
,
    PRIMARY KEY ([app_config_id])
);
CREATE TABLE [dbo].[app_config_group] (
    [app_config_group_id] int IDENTITY(1,1) NOT NULL,
    [group_name] nvarchar(50) NOT NULL,
    [group_description] nvarchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] nvarchar(50) NOT NULL
,
    PRIMARY KEY ([app_config_group_id])
);
CREATE TABLE [dbo].[app_config_type] (
    [app_config_type_id] int IDENTITY(1,1) NOT NULL,
    [type_description] nvarchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] nvarchar(50) NOT NULL
,
    PRIMARY KEY ([app_config_type_id])
);
CREATE TABLE [dbo].[apple_notifications] (
    [apple_notification_id] int IDENTITY(1,1) NOT NULL,
    [license_message_id] int,
    [notification_string] varchar(MAX) NOT NULL,
    [latest_receipt] varchar(MAX),
    [latest_receipt_info] varchar(MAX),
    [original_transaction_id] varchar(100),
    [auto_renew_product_id] varchar(100),
    [product_id] varchar(100),
    [auto_renew_status] bit,
    [expiration_intent] tinyint,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([apple_notification_id])
);
CREATE TABLE [dbo].[apple_notifications_by_receipt] (
    [apple_notifications_by_receipt_id] int IDENTITY(1,1) NOT NULL,
    [apple_notification_id] int NOT NULL,
    [transaction_id] varchar(100) NOT NULL,
    [original_transaction_id] varchar(100) NOT NULL,
    [product_id] varchar(100) NOT NULL,
    [cancellation_reason] tinyint,
    [cancellation_date] datetime,
    [purchase_date] datetime NOT NULL,
    [expires_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([apple_notifications_by_receipt_id])
);
CREATE TABLE [dbo].[apple_notifications_by_receipt_history] (
    [apple_notifications_by_receipt_history_id] int IDENTITY(1,1) NOT NULL,
    [apple_notifications_by_receipt_id] int NOT NULL,
    [apple_notification_id] int NOT NULL,
    [transaction_id] varchar(100) NOT NULL,
    [original_transaction_id] varchar(100) NOT NULL,
    [product_id] varchar(100) NOT NULL,
    [cancellation_reason] tinyint,
    [cancellation_date] datetime,
    [purchase_date] datetime NOT NULL,
    [expires_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([apple_notifications_by_receipt_history_id])
);
CREATE TABLE [dbo].[apple_notifications_history] (
    [apple_notification_history_id] int IDENTITY(1,1) NOT NULL,
    [apple_notification_id] int NOT NULL,
    [license_message_id] int,
    [notification_string] varchar(MAX) NOT NULL,
    [latest_receipt] varchar(MAX),
    [latest_receipt_info] varchar(MAX),
    [original_transaction_id] varchar(100),
    [auto_renew_product_id] varchar(100),
    [product_id] varchar(100),
    [auto_renew_status] bit,
    [expiration_intent] tinyint,
    [insert_date] datetime NOT NULL,
    [history_date] datetime,
    [history_by] varchar(200)
,
    PRIMARY KEY ([apple_notification_history_id])
);
CREATE TABLE [dbo].[apple_notifications_temp] (
    [apple_notificaton_id] int IDENTITY(1,1) NOT NULL,
    [notificaton_string] varchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([apple_notificaton_id])
);
CREATE TABLE [dbo].[authnet_transactions] (
    [last_name] varchar(32),
    [first_name] varchar(32),
    [address] varchar(32),
    [city] varchar(32),
    [state] varchar(32),
    [postal_code] varchar(32),
    [country_id] smallint,
    [telephone] varchar(32),
    [email] varchar(64),
    [resp_code] smallint,
    [reason_code] smallint,
    [transaction_date] datetime,
    [remote_host] varchar(32),
    [num_returned_fields] smallint,
    [cc_num] varchar(40),
    [exp_date] varchar(20),
    [amount] money,
    [order_description] varchar(200),
    [process_manually] tinyint,
    [gw] varchar(4),
    [invoice_id] varchar(20),
    [tax_total] decimal(15,10),
    [authnet_transactions_id] int IDENTITY(1,1) NOT NULL
,
    PRIMARY KEY ([authnet_transactions_id])
);
CREATE TABLE [dbo].[auto_renewal_feed] (
    [auto_renewal_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_id] bigint NOT NULL,
    [keycode] nvarchar(40),
    [product_id] nvarchar(40),
    [line_item_id] nvarchar(40),
    [vendor_order_date] datetime,
    [enroll_date] datetime,
    [unenroll_date] datetime,
    [auto_renewal_description] varchar(20)
,
    PRIMARY KEY ([auto_renewal_id])
);
CREATE TABLE [dbo].[auto_renewal_master_stage] (
    [auto_renewal_master_stage_id] int IDENTITY(1,1) NOT NULL,
    [auto_renewal_trigger_run_date] datetime NOT NULL,
    [license_id] int NOT NULL,
    [order_header_id] int,
    [cybs_customer_profile_id] nchar(10),
    [customer_profile_token] varchar(255),
    [customer_id] int,
    [customer_email] varchar(100),
    [keycode] varchar(40),
    [enroll_date] datetime,
    [capability_id] int,
    [license_category_id] int,
    [category_prefix] varchar(10),
    [license_seats] tinyint,
    [capability_expiration_date] datetime,
    [capability_activation_days] int,
    [root_product_id] int,
    [original_product_id] int,
    [auto_renewal_product_mapping] int,
    [renewal_product_id] int,
    [renewal_product_description] varchar(255),
    [price] money,
    [auto_renewal_status_id] tinyint,
    [auto_renewal_trigger_id] tinyint,
    [trigger_days] int,
    [trigger_days_description] varchar(255),
    [trigger_type_description] varchar(20),
    [insert_date] datetime,
    [insert_by] varchar(50),
    [modified_date] datetime,
    [modified_by] varchar(50)
,
    PRIMARY KEY ([auto_renewal_master_stage_id])
);
CREATE TABLE [dbo].[auto_renewal_message] (
    [auto_renewal_message_id] int IDENTITY(1,1) NOT NULL,
    [auto_renewal_message_auto_renewal_type_id] int,
    [license_id] int NOT NULL,
    [license_category_id] int,
    [salesforce_renewal_opportunity_id] varchar(50),
    [customer_id] int,
    [customer_email] varchar(150) NOT NULL,
    [cart_discount_id] int,
    [exact_target_master_id] int NOT NULL,
    [auto_renewal_message_status_id] int NOT NULL,
    [mail_date] datetime,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([auto_renewal_message_id])
);
CREATE TABLE [dbo].[auto_renewal_message_auto_renewal_type] (
    [auto_renewal_message_auto_renewal_type_id] int IDENTITY(1,1) NOT NULL,
    [auto_renewal_message_auto_renewal_type_description] varchar(50)
,
    PRIMARY KEY ([auto_renewal_message_auto_renewal_type_id])
);
CREATE TABLE [dbo].[autorenewal_discount_type] (
    [autorenewal_discount_type_id] int IDENTITY(1,1) NOT NULL,
    [autorenewal_discount_type_description] varchar(50) NOT NULL,
    [autorenewal_discount] float NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([autorenewal_discount_type_id])
);
CREATE TABLE [dbo].[bazaar_voice_email] (
    [bazaar_voice_email_id] int IDENTITY(1,1) NOT NULL,
    [customer_email] varchar(100),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [csi_user_id] varchar(50),
    [wr_customer_id] int
,
    PRIMARY KEY ([bazaar_voice_email_id])
);
CREATE TABLE [dbo].[beta_participant_max] (
    [beta_participant_max_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [product_version] varchar(5) NOT NULL,
    [participant_max_cnt] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [operating_system] varchar(20)
);
CREATE TABLE [dbo].[beta_registration] (
    [beta_registration_id] int IDENTITY(1,1) NOT NULL,
    [first_name] nvarchar(32) NOT NULL,
    [last_name] nvarchar(32) NOT NULL,
    [email] varchar(64) NOT NULL,
    [operating_system] varchar(20) NOT NULL,
    [opt_in] tinyint NOT NULL,
    [language] varchar(3) NOT NULL,
    [location] varchar(3) NOT NULL,
    [sessionid] bigint NOT NULL,
    [insert_date] datetime NOT NULL,
    [new_user] char(1),
    [product_version] varchar(5),
    [product_id] int
,
    PRIMARY KEY ([beta_registration_id])
);
CREATE TABLE [dbo].[beta_tracking] (
    [beta_tracking_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [product_version] varchar(5) NOT NULL,
    [keycode] char(20),
    [sessionid] bigint NOT NULL,
    [insert_date] datetime NOT NULL,
    [operating_system] varchar(20)
,
    PRIMARY KEY ([beta_tracking_id])
);
CREATE TABLE [dbo].[billing_failure_log] (
    [failure_log_id] int IDENTITY(1,1) NOT NULL,
    [failure_month] varchar(20),
    [failure_year] int,
    [source] varchar(10),
    [contract] varchar(20),
    [license_id] int,
    [notes] nvarchar(255),
    [resolution] nvarchar(255),
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([failure_log_id])
);
CREATE TABLE [dbo].[capability] (
    [capability_id] int IDENTITY(1,1) NOT NULL,
    [capability_name] varchar(50),
    [capability_description] varchar(50) NOT NULL,
    [product_line_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [capability_sort_order] tinyint
,
    PRIMARY KEY ([capability_id])
);
CREATE TABLE [dbo].[capability_ext] (
    [capability_ext_id] int IDENTITY(1,1) NOT NULL,
    [capability_id] int,
    [capability_ext_type_id] tinyint
,
    PRIMARY KEY ([capability_ext_id])
);
CREATE TABLE [dbo].[capability_ext_type] (
    [capability_ext_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [capability_ext_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([capability_ext_type_id])
);
CREATE TABLE [dbo].[capability_type] (
    [capability_type_id] int IDENTITY(1,1) NOT NULL,
    [capability_type_description] varchar(20) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([capability_type_id])
);
CREATE TABLE [dbo].[card_type] (
    [card_type_id] tinyint NOT NULL,
    [card_type_description] varchar(100) NOT NULL
,
    PRIMARY KEY ([card_type_id])
);
CREATE TABLE [dbo].[cart_api_log] (
    [cart_api_log_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [partner_account_id] int NOT NULL,
    [vendor_order_code] varchar(100),
    [insert_date] datetime,
    [modified_date] datetime
,
    PRIMARY KEY ([cart_api_log_id])
);
CREATE TABLE [dbo].[cart_api_log_request] (
    [cart_api_log_request_id] int IDENTITY(1,1) NOT NULL,
    [cart_api_log_id] int NOT NULL,
    [cart_api_request] nvarchar(MAX) NOT NULL,
    [cart_api_endpoint] nvarchar(100) NOT NULL,
    [http_request] varchar(20)
,
    PRIMARY KEY ([cart_api_log_request_id])
);
CREATE TABLE [dbo].[cart_api_log_response] (
    [cart_api_log_response_id] int IDENTITY(1,1) NOT NULL,
    [cart_api_log_id] int NOT NULL,
    [response_code] int NOT NULL,
    [cart_api_response] nvarchar(MAX)
,
    PRIMARY KEY ([cart_api_log_response_id])
);
CREATE TABLE [dbo].[cart_customer] (
    [cart_customer_id] int IDENTITY(1,1) NOT NULL,
    [customer_in_process_id] int NOT NULL,
    [external_customer_key] varchar(100),
    [first_name] nvarchar(255),
    [last_name] nvarchar(255),
    [bill_address_1] nvarchar(255),
    [bill_address_2] nvarchar(255),
    [bill_address_3] nvarchar(255),
    [bill_city] nvarchar(130),
    [bill_state] char(2),
    [bill_postal_code] nvarchar(32),
    [bill_country] char(3),
    [ship_address_1] nvarchar(255),
    [ship_address_2] nvarchar(255),
    [ship_address_3] nvarchar(255),
    [ship_city] nvarchar(130),
    [ship_state] char(2),
    [ship_postal_code] nvarchar(32),
    [ship_country] char(3),
    [phone_number] varchar(64),
    [fax_number] varchar(64),
    [company_name] nvarchar(255),
    [customer_email] nvarchar(255),
    [opt_in] bit NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [cart_order_status_id] tinyint NOT NULL,
    [vat_id] varchar(20),
    [customer_type_id] int,
    [cart_order_id] int,
    [company_type_id] tinyint,
    [salesforce_account_id] varchar(20),
    [salesforce_contact_id] varchar(20)
,
    PRIMARY KEY ([cart_customer_id])
);
CREATE TABLE [dbo].[cart_customer_audit] (
    [cart_customer_audit_id] int IDENTITY(1,1) NOT NULL,
    [cart_customer_id] int NOT NULL,
    [external_customer_key] varchar(100),
    [first_name] nvarchar(255),
    [last_name] nvarchar(255),
    [company_name] nvarchar(255),
    [customer_email] nvarchar(255),
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [city] nvarchar(130),
    [state] varchar(3),
    [postal_code] nvarchar(32),
    [country] varchar(2),
    [phone_number] varchar(64),
    [opt_in] bit,
    [vat_id] varchar(20),
    [customer_type_id] int,
    [audit_date] datetime NOT NULL,
    [company_type_id] tinyint,
    [salesforce_account_id] varchar(20),
    [salesforce_contact_id] varchar(20)
,
    PRIMARY KEY ([cart_customer_audit_id])
);
CREATE TABLE [dbo].[cart_customer_in_process] (
    [cart_customer_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_in_process_id] int NOT NULL,
    [first_name] nvarchar(255),
    [last_name] nvarchar(255),
    [company_name] nvarchar(255),
    [address1] nvarchar(255),
    [address2] nvarchar(255),
    [city] nvarchar(130),
    [state_id] char(2),
    [postal_code] varchar(32),
    [country_id] int,
    [phone_number] varchar(64),
    [customer_email] varchar(255),
    [opt_in] bit,
    [modified_date] datetime,
    [insert_date] datetime,
    [vat_id] varchar(20)
,
    PRIMARY KEY ([cart_customer_in_process_id])
);
CREATE TABLE [dbo].[cart_customer_json] (
    [cart_customer_json_id] int IDENTITY(1,1) NOT NULL,
    [cart_customer_id] int NOT NULL,
    [cart_customer_json] nvarchar(MAX) NOT NULL
,
    PRIMARY KEY ([cart_customer_json_id])
);
CREATE TABLE [dbo].[cart_discount] (
    [cart_discount_id] int IDENTITY(1,1) NOT NULL,
    [cart_discount_description] varchar(50) NOT NULL,
    [cart_discount_type_id] tinyint NOT NULL,
    [cart_discount_status_id] tinyint NOT NULL,
    [cart_discount_key] uniqueidentifier NOT NULL,
    [cart_discount_code] varchar(20),
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [cd_default] tinyint,
    [suppress_discount] tinyint
,
    PRIMARY KEY ([cart_discount_id])
);
CREATE TABLE [dbo].[cart_discount_bundle] (
    [cart_discount_bundle_id] int IDENTITY(1,1) NOT NULL,
    [cart_discount_id] int NOT NULL,
    [cart_discount_bundle_description] nvarchar(200) NOT NULL,
    [cart_discount_bundle_details_list] nvarchar(1000)
);
CREATE TABLE [dbo].[cart_discount_gamer_discount] (
    [cart_discount_gamer_discount_id] int IDENTITY(1,1) NOT NULL,
    [cart_discount_id] int NOT NULL,
    [cart_discount_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([cart_discount_gamer_discount_id])
);
CREATE TABLE [dbo].[cart_discount_item] (
    [cart_discount_item_id] int IDENTITY(1,1) NOT NULL,
    [cart_discount_id] int NOT NULL,
    [cart_discount_method_id] tinyint NOT NULL,
    [discount] float NOT NULL,
    [low_range] float,
    [high_range] float,
    [product_type_id] int NOT NULL,
    [product_line_id] int NOT NULL,
    [license_category_id] tinyint,
    [license_category_name] varchar(10),
    [license_seats] int,
    [storage_gb] int,
    [years] float,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [product_id] int,
    [site_display] tinyint,
    [license_attribute_license_value] int,
    [sap_material_number] int
,
    PRIMARY KEY ([cart_discount_item_id])
);
CREATE TABLE [dbo].[cart_discount_item_discount] (
    [cart_discount_item_discount_id] int IDENTITY(1,1) NOT NULL,
    [cart_discount_item_id] int NOT NULL,
    [discount] float NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [currency] varchar(50) NOT NULL,
    [currency_id] tinyint
,
    PRIMARY KEY ([cart_discount_item_discount_id])
);
CREATE TABLE [dbo].[cart_discount_item_discount_history] (
    [cart_discount_item_discount_history_id] int IDENTITY(1,1) NOT NULL,
    [cart_discount_item_discount_id] int NOT NULL,
    [cart_discount_item_id] int NOT NULL,
    [discount] float NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [currency] varchar(50) NOT NULL,
    [currency_id] tinyint,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([cart_discount_item_discount_history_id])
);
CREATE TABLE [dbo].[cart_discount_item_license_module] (
    [cart_discount_item_license_module_id] int IDENTITY(1,1) NOT NULL,
    [cart_discount_item_id] int NOT NULL,
    [license_module_id] tinyint NOT NULL
,
    PRIMARY KEY ([cart_discount_item_license_module_id])
);
CREATE TABLE [dbo].[cart_discount_license_distribution_method] (
    [cart_discount_license_distribution_method_id] int IDENTITY(1,1) NOT NULL,
    [cart_discount_id] int NOT NULL,
    [license_distribution_method_id] int NOT NULL
);
CREATE TABLE [dbo].[cart_discount_message] (
    [cart_discount_message_id] int IDENTITY(1,1) NOT NULL,
    [cart_discount_message_description] varchar(50) NOT NULL,
    [cart_discount_message_key] uniqueidentifier NOT NULL,
    [message_type_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([cart_discount_message_id])
);
CREATE TABLE [dbo].[cart_discount_message_value] (
    [cart_discount_message_value_id] int IDENTITY(1,1) NOT NULL,
    [cart_discount_message_id] int NOT NULL,
    [message_value_type_id] tinyint NOT NULL,
    [value_id] varchar(20) NOT NULL
,
    PRIMARY KEY ([cart_discount_message_value_id])
);
CREATE TABLE [dbo].[cart_discount_method] (
    [cart_discount_method_id] tinyint IDENTITY(1,1) NOT NULL,
    [cart_discount_method_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([cart_discount_method_id])
);
CREATE TABLE [dbo].[cart_discount_specials_mapping] (
    [cart_discount_specials_mapping_id] int IDENTITY(1,1) NOT NULL,
    [cart_discount_id] int NOT NULL,
    [specials_code] varchar(100) NOT NULL
,
    PRIMARY KEY ([cart_discount_specials_mapping_id])
);
CREATE TABLE [dbo].[cart_discount_status] (
    [cart_discount_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [cart_discount_status_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([cart_discount_status_id])
);
CREATE TABLE [dbo].[cart_discount_type] (
    [cart_discount_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [cart_discount_type_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([cart_discount_type_id])
);
CREATE TABLE [dbo].[cart_in_process] (
    [cart_in_process_id] int IDENTITY(1,1) NOT NULL,
    [invoice_code] varchar(16) NOT NULL,
    [customer_code] varchar(16) NOT NULL,
    [affiliate_code] varchar(16) NOT NULL,
    [distribution_channel_code] varchar(32),
    [distribution_role_code] varchar(32),
    [referral_source_id] smallint,
    [user_ip] varchar(16),
    [order_type_id] smallint NOT NULL,
    [amount] decimal(9,2) NOT NULL,
    [purchased_date] datetime NOT NULL,
    [modified_date] datetime,
    [invoice_status_id] smallint NOT NULL,
    [visitorid] int,
    [coupon_amount] money,
    [last_modified] datetime NOT NULL,
    [tax_total] decimal(15,2),
    [p_rc] varchar(12),
    [p_rsc] varchar(64),
    [p_ac] varchar(12),
    [trx_rc] varchar(12),
    [trx_rsc] varchar(64),
    [trx_ac] varchar(12),
    [coupon_code] varchar(7),
    [ein] varchar(20),
    [separate_shipping] int,
    [carttype] varchar(30),
    [carttype_id] int,
    [profile_token] varchar(32),
    [auth_request_id] varchar(32),
    [currency_code] char(3),
    [language_code] varchar(2),
    [location_code] varchar(3)
,
    PRIMARY KEY ([cart_in_process_id])
);
CREATE TABLE [dbo].[cart_item_in_process] (
    [cart_item_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_in_process_id] int NOT NULL,
    [invoice_code] varchar(16),
    [line_item] int,
    [line_item_description] varchar(100),
    [product_id] int,
    [product_version] varchar(20),
    [quantity] int NOT NULL,
    [extended_price] decimal(10,4) NOT NULL,
    [entered_timestamp] datetime NOT NULL,
    [previous_version] varchar(20),
    [is_update] smallint,
    [cross_sell] tinyint,
    [shipping_status] tinyint,
    [shipped_date] datetime,
    [serial_number] varchar(50),
    [refund_reason_code] smallint,
    [auth_trans_id] varchar(40),
    [auth_batch_id] datetime,
    [deferred_income] money,
    [last_modified] datetime NOT NULL,
    [tax_item_amount] decimal(15,2),
    [IsKeycodeValid] bit,
    [full_retail_price] decimal(10,4),
    [special_code] varchar(12),
    [effective_date] datetime,
    [extra_years] int,
    [extra_days] int,
    [override_unit_price] money,
    [cartKey] varchar(50),
    [cart_item_class_name] varchar(50),
    [final_product_id] int,
    [capability] int,
    [parent_line_item] int,
    [tax_offer_line_item] varchar(10)
,
    PRIMARY KEY ([cart_item_in_process_id])
);
CREATE TABLE [dbo].[cart_item_in_process_ext] (
    [cart_item_in_process_ext_id] int IDENTITY(1,1) NOT NULL,
    [cart_item_in_process_id] int NOT NULL,
    [cart_item_in_process_ext_type_id] tinyint NOT NULL,
    [ext_type_value] varchar(20) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([cart_item_in_process_ext_id])
);
CREATE TABLE [dbo].[cart_item_in_process_ext_type] (
    [cart_item_in_process_ext_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [ext_type_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([cart_item_in_process_ext_type_id])
);
CREATE TABLE [dbo].[cart_json] (
    [cart_json_id] int IDENTITY(1,1) NOT NULL,
    [cart_json] nvarchar(MAX) NOT NULL,
    [cart_order_id] int,
    [cart_order_in_process_id] int
);
CREATE TABLE [dbo].[cart_json_audit] (
    [cart_json_audit_id] int IDENTITY(1,1) NOT NULL,
    [cart_json_id] int NOT NULL,
    [cart_json] nvarchar(MAX) NOT NULL,
    [cart_order_id] int,
    [cart_order_in_process_id] int,
    [audit_date] datetime
);
CREATE TABLE [dbo].[cart_license_update] (
    [vendor_order_code] varchar(100) NOT NULL
,
    PRIMARY KEY ([vendor_order_code])
);
CREATE TABLE [dbo].[cart_load] (
    [cart_load_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [load_step_id] tinyint NOT NULL,
    [load_step] varchar(50) NOT NULL,
    [step_complete_date] datetime NOT NULL
,
    PRIMARY KEY ([cart_load_id])
);
CREATE TABLE [dbo].[cart_message] (
    [cart_message_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [message_key] varchar(36) NOT NULL
,
    PRIMARY KEY ([cart_message_id])
);
CREATE TABLE [dbo].[cart_message_in_process] (
    [cart_message_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_in_process_id] int NOT NULL,
    [message_key] uniqueidentifier NOT NULL,
    [license_id] int,
    [cart_discount_id] int,
    [status_id] tinyint NOT NULL
,
    PRIMARY KEY ([cart_message_in_process_id])
);
CREATE TABLE [dbo].[cart_order] (
    [cart_order_id] int IDENTITY(1,1) NOT NULL,
    [cart_customer_id] int NOT NULL,
    [invoice_in_process_id] int NOT NULL,
    [vendor_order_code] varchar(100),
    [order_type] varchar(30) NOT NULL,
    [site_id] varchar(65) NOT NULL,
    [site_url] varchar(1025) NOT NULL,
    [p_rc] varchar(50) NOT NULL,
    [p_rsc] varchar(50),
    [p_ac] varchar(100),
    [trx_rc] varchar(50),
    [trx_rsc] varchar(50),
    [trx_ac] varchar(100),
    [aid] varchar(50),
    [pid] varchar(50),
    [sid] varchar(100),
    [offer_id] varchar(65),
    [offer_amount] money,
    [total_amount] money,
    [sub_total_amount] money NOT NULL,
    [tax_amount] money,
    [payment_method] varchar(255) NOT NULL,
    [exchange_rate] money,
    [session_id] bigint NOT NULL,
    [submission_date] datetime NOT NULL,
    [sales_order_date] datetime,
    [locale] char(5) NOT NULL,
    [subject] varchar(255),
    [comment] varchar(8000),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [cart_order_status_id] tinyint NOT NULL,
    [currency_id] tinyint,
    [customer_profile_token] varchar(24),
    [cart_order_in_process_id] int,
    [user_ip] varchar(16),
    [restriction] varchar(20)
,
    PRIMARY KEY ([cart_order_id])
);
CREATE TABLE [dbo].[cart_order_audit] (
    [cart_order_audit_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [vendor_order_code] varchar(100),
    [order_type] varchar(30) NOT NULL,
    [site_id] varchar(65) NOT NULL,
    [site_url] varchar(1025) NOT NULL,
    [offer_amount] money,
    [total_amount] money NOT NULL,
    [sub_total_amount] money NOT NULL,
    [tax_amount] money,
    [sales_order_date] datetime,
    [locale] char(5) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [cart_order_status_id] tinyint NOT NULL,
    [currency_id] tinyint,
    [user_ip] varchar(16),
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([cart_order_audit_id])
);
CREATE TABLE [dbo].[cart_order_customer] (
    [cart_order_customer_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [customer_type_id] int NOT NULL
,
    PRIMARY KEY ([cart_order_customer_id])
);
CREATE TABLE [dbo].[cart_order_customer_in_process] (
    [cart_order_customer_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_in_process_id] int NOT NULL,
    [first_name] nvarchar(255),
    [last_name] nvarchar(255),
    [company_name] nvarchar(255),
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [city] nvarchar(130),
    [state] nvarchar(2),
    [postal_code] varchar(32),
    [country_id] int,
    [phone_number] varchar(64),
    [customer_email] varchar(255),
    [opt_in] tinyint,
    [last_modified_date] datetime,
    [customer_type_id] int,
    [vat_id] varchar(20),
    [external_customer_key] varchar(100)
,
    PRIMARY KEY ([cart_order_customer_in_process_id])
);
CREATE TABLE [dbo].[cart_order_extension] (
    [cart_order_extension_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [order_extension_type_id] tinyint NOT NULL,
    [order_extension_value] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([cart_order_extension_id])
);
CREATE TABLE [dbo].[cart_order_failure] (
    [card_order_failure_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [failure_error_message] nvarchar(4000),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([card_order_failure_id])
);
CREATE TABLE [dbo].[cart_order_header] (
    [cart_order_header_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [autorenewal_flag] int,
    [order_header_id] int NOT NULL
,
    PRIMARY KEY ([cart_order_header_id])
);
CREATE TABLE [dbo].[cart_order_in_process] (
    [cart_order_in_process_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_code] varchar(100),
    [order_type] varchar(20) NOT NULL,
    [site_id] varchar(20) NOT NULL,
    [site_url] varchar(20) NOT NULL,
    [p_rc] varchar(20) NOT NULL,
    [p_rsc] varchar(36),
    [p_ac] varchar(20),
    [trx_rc] varchar(36),
    [trx_rsc] varchar(30),
    [trx_ac] varchar(20),
    [total_amount] money NOT NULL,
    [sub_total_amount] money NOT NULL,
    [offer_amount] money NOT NULL,
    [tax_amount] money,
    [session_id] bigint NOT NULL,
    [user_ip] varchar(16),
    [submission_date] datetime NOT NULL,
    [locale] char(5) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(50) NOT NULL,
    [cart_order_in_process_status_id] tinyint NOT NULL,
    [currency_id] tinyint,
    [customer_profile_token] varchar(24),
    [language_code] varchar(2),
    [location_code] varchar(3),
    [merchant_id] varchar(50),
    [restriction] varchar(20)
,
    PRIMARY KEY ([cart_order_in_process_id])
);
CREATE TABLE [dbo].[cart_order_item] (
    [cart_order_item_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [invoice_item_in_process_id] int NOT NULL,
    [vendor_id] int NOT NULL,
    [line_item] int NOT NULL,
    [vendor_product_id] int,
    [quantity] int NOT NULL,
    [order_item_offer_code] varchar(65),
    [order_item_offer_amount] money,
    [list_price] money NOT NULL,
    [unit_price] money NOT NULL,
    [tax_item_total] money NOT NULL,
    [tax_exempt] bit NOT NULL,
    [product_id] int NOT NULL,
    [conversion_product_id] int,
    [product_locale] char(5),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [cart_order_status_id] tinyint NOT NULL,
    [cart_order_item_in_process_id] int,
    [cart_item_bundle_id] int,
    [start_date] datetime,
    [expiration_date] datetime,
    [vendor_order_item_code] varchar(36),
    [order_item_update_type_id] tinyint,
    [license_attribute_license_value] int,
    [item_hierarchy_id] tinyint,
    [cart_discount_id] int,
    [cart_discount_method_id] tinyint,
    [discount] float,
    [unit_price_pre_vat] money,
    [usage_price] money,
    [opportunity_line_item_id] varchar(18),
    [sap_material_number] int,
    [storage_gb] int
,
    PRIMARY KEY ([cart_order_item_id])
);
CREATE TABLE [dbo].[cart_order_item_audit] (
    [cart_order_item_audit_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_item_id] int NOT NULL,
    [cart_order_id] int NOT NULL,
    [line_item] int NOT NULL,
    [quantity] int NOT NULL,
    [order_item_offer_amount] money,
    [list_price] money NOT NULL,
    [unit_price] money NOT NULL,
    [tax_item_total] money NOT NULL,
    [product_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [cart_item_bundle_id] int,
    [start_date] datetime,
    [expiration_date] datetime,
    [vendor_order_item_code] varchar(36),
    [order_item_update_type_id] tinyint,
    [license_attribute_license_value] int,
    [item_hierarchy_id] tinyint,
    [audit_date] datetime NOT NULL,
    [cart_discount_id] int,
    [cart_discount_method_id] tinyint,
    [discount] float,
    [unit_price_pre_vat] money,
    [usage_price] money,
    [usage_pricing_model_id] tinyint,
    [opportunity_line_item_id] varchar(18),
    [sap_material_number] int,
    [storage_gb] int
,
    PRIMARY KEY ([cart_order_item_audit_id])
);
CREATE TABLE [dbo].[cart_order_item_failure] (
    [cart_order_item_failure_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int,
    [item_json] nvarchar(MAX),
    [bundle_json] nvarchar(MAX),
    [insert_date] datetime
);
CREATE TABLE [dbo].[cart_order_item_in_process] (
    [cart_order_item_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_in_process_id] int NOT NULL,
    [vendor_id] int NOT NULL,
    [cart_item_class_id] tinyint NOT NULL,
    [cart_item_bundle_id] int,
    [line_item] int NOT NULL,
    [vendor_product_id] int,
    [quantity] int NOT NULL,
    [order_item_offer_code] varchar(65),
    [order_item_offer_amount] money,
    [retail_price] money NOT NULL,
    [net_price] money NOT NULL,
    [tax_item_total] money NOT NULL,
    [tax_exempt] bit NOT NULL,
    [product_id] int NOT NULL,
    [product_locale] char(5),
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(50) NOT NULL,
    [discount] money NOT NULL,
    [standard_discount] money NOT NULL,
    [cart_discount_id] int,
    [pricing_level] varchar(20),
    [order_item_update_type_id] tinyint,
    [license_attribute_license_value] int,
    [start_date] datetime,
    [expiration_date] datetime
,
    PRIMARY KEY ([cart_order_item_in_process_id])
);
CREATE TABLE [dbo].[cart_order_item_json] (
    [cart_order_item_json_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_item_id] int NOT NULL,
    [cart_order_item_json] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([cart_order_item_json_id])
);
CREATE TABLE [dbo].[cart_order_item_json_log] (
    [cart_order_item_json_log_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int,
    [item_json] nvarchar(MAX),
    [bundle_json] nvarchar(MAX),
    [insert_date] datetime
);
CREATE TABLE [dbo].[cart_order_item_license] (
    [cart_order_item_license_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_item_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [cart_order_status_id] tinyint NOT NULL
,
    PRIMARY KEY ([cart_order_item_license_id])
);
CREATE TABLE [dbo].[cart_order_item_license_audit] (
    [cart_order_item_license_audit_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_item_license_id] int NOT NULL,
    [cart_order_item_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([cart_order_item_license_audit_id])
);
CREATE TABLE [dbo].[cart_order_item_license_in_process] (
    [cart_order_item_license_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_item_in_process_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL
,
    PRIMARY KEY ([cart_order_item_license_in_process_id])
);
CREATE TABLE [dbo].[cart_order_item_rebate] (
    [cart_order_item_rebate_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_item_id] int NOT NULL,
    [rebate_id] int NOT NULL
,
    PRIMARY KEY ([cart_order_item_rebate_id])
);
CREATE TABLE [dbo].[cart_order_item_rebate_in_process] (
    [cart_order_item_rebate_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_item_in_process_id] int NOT NULL,
    [rebate_id] int NOT NULL
,
    PRIMARY KEY ([cart_order_item_rebate_in_process_id])
);
CREATE TABLE [dbo].[cart_order_item_tax] (
    [cart_order_item_tax_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_item_id] int NOT NULL,
    [tax_total_city_tax] money NOT NULL,
    [tax_total_county_tax] money NOT NULL,
    [tax_total_district_tax] money NOT NULL,
    [tax_total_state_tax] money NOT NULL,
    [tax_total_tax] money NOT NULL,
    [insert_date] datetime,
    [local_tax] money
,
    PRIMARY KEY ([cart_order_item_tax_id])
);
CREATE TABLE [dbo].[cart_order_item_tax_audit] (
    [cart_order_item_tax_audit_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_item_tax_id] int NOT NULL,
    [cart_order_item_id] int NOT NULL,
    [tax_total_city_tax] money NOT NULL,
    [tax_total_county_tax] money NOT NULL,
    [tax_total_district_tax] money NOT NULL,
    [tax_total_state_tax] money NOT NULL,
    [tax_total_tax] money NOT NULL,
    [insert_date] datetime,
    [audit_date] datetime,
    [local_tax] money
,
    PRIMARY KEY ([cart_order_item_tax_audit_id])
);
CREATE TABLE [dbo].[cart_order_item_tax_in_process] (
    [cart_order_item_tax_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_item_in_process_id] int NOT NULL,
    [tax_total_city_tax] money NOT NULL,
    [tax_total_county_tax] money NOT NULL,
    [tax_total_district_tax] money NOT NULL,
    [tax_total_state_tax] money NOT NULL,
    [tax_total_tax] money NOT NULL,
    [insert_date] datetime
,
    PRIMARY KEY ([cart_order_item_tax_in_process_id])
);
CREATE TABLE [dbo].[cart_order_message] (
    [cart_order_message_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [message_key] uniqueidentifier NOT NULL,
    [license_id] int,
    [cart_discount_id] int,
    [status_id] tinyint NOT NULL,
    [message_campaign_id] int,
    [message_campaign_platform] varchar(50)
,
    PRIMARY KEY ([cart_order_message_id])
);
CREATE TABLE [dbo].[cart_order_message_in_process] (
    [cart_order_message_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_in_process_id] int NOT NULL,
    [message_key] uniqueidentifier NOT NULL,
    [license_id] int,
    [cart_discount_id] int,
    [status_id] tinyint NOT NULL,
    [message_campaign_id] int,
    [message_campaign_platform] varchar(50)
,
    PRIMARY KEY ([cart_order_message_in_process_id])
);
CREATE TABLE [dbo].[cart_order_partner] (
    [cart_order_partner_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [partner_id] int NOT NULL,
    [partner_account_id] int
,
    PRIMARY KEY ([cart_order_partner_id])
);
CREATE TABLE [dbo].[cart_order_partner_in_process] (
    [cart_order_partner_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_in_process_id] int NOT NULL,
    [partner_id] int NOT NULL,
    [partner_account_id] int NOT NULL
,
    PRIMARY KEY ([cart_order_partner_in_process_id])
);
CREATE TABLE [dbo].[cart_order_route] (
    [cart_order_route_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [routing_action] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([cart_order_route_id])
);
CREATE TABLE [dbo].[cart_order_route_in_process] (
    [cart_order_route_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_in_process_id] int NOT NULL,
    [routing_action] varchar(50),
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([cart_order_route_in_process_id])
);
CREATE TABLE [dbo].[cart_order_shipping_in_process] (
    [cart_order_shipping_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_in_process_id] int NOT NULL,
    [company_name] nvarchar(255),
    [first_name] nvarchar(255),
    [last_name] nvarchar(255),
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [city] nvarchar(130),
    [state] varchar(2),
    [postal_code] varchar(32),
    [country_id] int,
    [last_modified] datetime
,
    PRIMARY KEY ([cart_order_shipping_in_process_id])
);
CREATE TABLE [dbo].[cart_order_status] (
    [cart_order_status_id] tinyint NOT NULL,
    [cart_order_status_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([cart_order_status_id])
);
CREATE TABLE [dbo].[cart_order_tax_json] (
    [cart_order_tax_json_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [cart_order_tax_json] varchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([cart_order_tax_json_id])
);
CREATE TABLE [dbo].[cart_order_technical_contact_in_process] (
    [cart_order_technical_contact_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_in_process_id] int NOT NULL,
    [first_name] nvarchar(255) NOT NULL,
    [last_name] nvarchar(255) NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [phone_number] varchar(64) NOT NULL
,
    PRIMARY KEY ([cart_order_technical_contact_in_process_id])
);
CREATE TABLE [dbo].[cart_order_token] (
    [cart_order_id] int NOT NULL,
    [cart_order_token] uniqueidentifier NOT NULL,
    [modified_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[cart_order_token_audit] (
    [cart_order_token_audit_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [cart_order_token] uniqueidentifier NOT NULL,
    [transaction_type] char(1) NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[cart_route] (
    [cart_route_id] int IDENTITY(1,1) NOT NULL,
    [routing_action] varchar(50),
    [message_key] uniqueidentifier,
    [license_id] int,
    [message_campaign_id] int,
    [message_campagn_platform] varchar(50),
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[cart_sector] (
    [cart_sector_id] tinyint IDENTITY(1,1) NOT NULL,
    [cart_sector_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([cart_sector_id])
);
CREATE TABLE [dbo].[cart_shipping] (
    [cart_shipping_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [company_name] nvarchar(255),
    [first_name] nvarchar(255),
    [last_name] nvarchar(255),
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [city] nvarchar(130),
    [state] varchar(2),
    [postal_code] varchar(32),
    [country_id] int,
    [last_modified] datetime
,
    PRIMARY KEY ([cart_shipping_id])
);
CREATE TABLE [dbo].[cart_shipping_in_process] (
    [cart_shipping_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_in_process_id] int NOT NULL,
    [vendor_order_code] varchar(16),
    [first_name] varchar(50),
    [last_name] varchar(50),
    [company] varchar(50),
    [address1] varchar(50),
    [address2] varchar(50),
    [city] varchar(50),
    [state] varchar(50),
    [postal_code] varchar(50),
    [country_id] smallint,
    [telephone] varchar(50),
    [ship_via] varchar(50),
    [purchased_date] smalldatetime,
    [keycode] varchar(50),
    [product_id] int,
    [shipping_status] tinyint,
    [last_modified] datetime,
    [lineitem] int
,
    PRIMARY KEY ([cart_shipping_in_process_id])
);
CREATE TABLE [dbo].[cart_site_id_order_code_prefix] (
    [cart_site_id_order_code_prefix_id] int IDENTITY(1,1) NOT NULL,
    [site_id] varchar(20) NOT NULL,
    [vendor_order_code_prefix] varchar(5) NOT NULL,
    [site_id_description] varchar(50)
,
    PRIMARY KEY ([cart_site_id_order_code_prefix_id])
);
CREATE TABLE [dbo].[cart_technical_contact] (
    [cart_technical_contact_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [cart_in_process_id] int NOT NULL,
    [first_name] nvarchar(255) NOT NULL,
    [last_name] nvarchar(255) NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [phone_number] varchar(64) NOT NULL
,
    PRIMARY KEY ([cart_technical_contact_id])
);
CREATE TABLE [dbo].[cart_technical_contact_in_process] (
    [cart_technical_contact_in_process_id] int IDENTITY(1,1) NOT NULL,
    [cart_in_process_id] int NOT NULL,
    [first_name] nvarchar(255) NOT NULL,
    [last_name] nvarchar(255) NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [phone_number] varchar(64) NOT NULL,
    [active] tinyint
,
    PRIMARY KEY ([cart_technical_contact_in_process_id])
);
CREATE TABLE [dbo].[cart_upsell] (
    [cart_upsell_id] int IDENTITY(1,1) NOT NULL,
    [cart_upsell_type_id] tinyint NOT NULL,
    [cart_upsell_description] varchar(200) NOT NULL,
    [cart_upsell_status_id] tinyint NOT NULL
,
    PRIMARY KEY ([cart_upsell_id])
);
CREATE TABLE [dbo].[cart_upsell_discount] (
    [cart_upsell_discount_id] int IDENTITY(1,1) NOT NULL,
    [cart_upsell_id] int NOT NULL,
    [cart_discount_id] int NOT NULL
,
    PRIMARY KEY ([cart_upsell_discount_id])
);
CREATE TABLE [dbo].[cart_upsell_license_category] (
    [cart_upsell_license_category_id] int IDENTITY(1,1) NOT NULL,
    [cart_upsell_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [upgrade_path] int
,
    PRIMARY KEY ([cart_upsell_license_category_id])
);
CREATE TABLE [dbo].[cart_upsell_license_distribution_method] (
    [cart_upsell_license_distribution_method_id] int IDENTITY(1,1) NOT NULL,
    [cart_upsell_id] int NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [inclusive] tinyint NOT NULL
,
    PRIMARY KEY ([cart_upsell_license_distribution_method_id])
);
CREATE TABLE [dbo].[cart_upsell_product_line] (
    [cart_upsell_product_line_id] int IDENTITY(1,1) NOT NULL,
    [cart_upsell_id] int NOT NULL,
    [product_line_id] int NOT NULL
,
    PRIMARY KEY ([cart_upsell_product_line_id])
);
CREATE TABLE [dbo].[cart_upsell_product_type] (
    [cart_upsell_product_type_id] int IDENTITY(1,1) NOT NULL,
    [cart_upsell_id] int NOT NULL,
    [product_type_id] int NOT NULL
,
    PRIMARY KEY ([cart_upsell_product_type_id])
);
CREATE TABLE [dbo].[cart_upsell_seat] (
    [cart_upsell_seat_id] int IDENTITY(1,1) NOT NULL,
    [cart_upsell_id] int NOT NULL,
    [seats] int NOT NULL
,
    PRIMARY KEY ([cart_upsell_seat_id])
);
CREATE TABLE [dbo].[cart_upsell_sector] (
    [cart_upsell_sector_id] int IDENTITY(1,1) NOT NULL,
    [cart_upsell_id] int NOT NULL,
    [cart_sector_id] tinyint NOT NULL
,
    PRIMARY KEY ([cart_upsell_sector_id])
);
CREATE TABLE [dbo].[cart_upsell_status] (
    [cart_upsell_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [cart_upsell_status_name] varchar(50) NOT NULL
);
CREATE TABLE [dbo].[cart_upsell_storage] (
    [cart_upsell_license_storage_id] int IDENTITY(1,1) NOT NULL,
    [cart_upsell_id] int NOT NULL,
    [storage_gb] int NOT NULL
,
    PRIMARY KEY ([cart_upsell_license_storage_id])
);
CREATE TABLE [dbo].[cart_upsell_template] (
    [cart_upsell_template_id] tinyint IDENTITY(1,1) NOT NULL,
    [cart_upsell_template_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([cart_upsell_template_id])
);
CREATE TABLE [dbo].[cart_upsell_template_mapping] (
    [cart_upsell_template_mapping_id] int IDENTITY(1,1) NOT NULL,
    [cart_upsell_id] int NOT NULL,
    [cart_upsell_template_id] tinyint NOT NULL
,
    PRIMARY KEY ([cart_upsell_template_mapping_id])
);
CREATE TABLE [dbo].[cart_upsell_template_text] (
    [cart_upsell_template_text_id] int IDENTITY(1,1) NOT NULL,
    [cart_upsell_template_id] int NOT NULL,
    [cart_upsell_template_text] nvarchar(200) NOT NULL,
    [cart_upsell_template_supplemental_text] nvarchar(500) NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL
,
    PRIMARY KEY ([cart_upsell_template_text_id])
);
CREATE TABLE [dbo].[cart_upsell_type] (
    [cart_upsell_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [cart_upsell_type_name] varchar(50) NOT NULL,
    [type_sort_order] tinyint NOT NULL,
    [coterm] tinyint
);
CREATE TABLE [dbo].[cart_upsell_years] (
    [cart_upsell_years_id] int IDENTITY(1,1) NOT NULL,
    [cart_upsell_id] int NOT NULL,
    [years] float NOT NULL
,
    PRIMARY KEY ([cart_upsell_years_id])
);
CREATE TABLE [dbo].[cb_client_identification] (
    [cb_client_id] varchar(5) NOT NULL
);
CREATE TABLE [dbo].[cb_notification_status] (
    [cb_notification_status_id] int IDENTITY(1,1) NOT NULL,
    [cb_notification_status_name] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([cb_notification_status_id])
);
CREATE TABLE [dbo].[cb_notification_type] (
    [cb_notification_type_id] int IDENTITY(1,1) NOT NULL,
    [cb_notification_type_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([cb_notification_type_id])
);
CREATE TABLE [dbo].[cb_notifications] (
    [cb_notification_id] int IDENTITY(1,1) NOT NULL,
    [cb_notification_type_id] int,
    [cb_order_header] varchar(MAX),
    [cb_order_body] xml,
    [cb_purchase_code] varchar(50),
    [cart_order_in_process_id] int,
    [cart_order_id] int,
    [order_header_id] int,
    [license_message_key] uniqueidentifier,
    [insert_date] datetime,
    [modified_date] datetime,
    [process_date] datetime,
    [cb_notification_status_id] int
,
    PRIMARY KEY ([cb_notification_id])
);
CREATE TABLE [dbo].[cb_payment_method] (
    [cb_payment_method_id] tinyint IDENTITY(1,1) NOT NULL,
    [cb_payment_method_code] varchar(50) NOT NULL,
    [cb_payment_method_description] varchar(50) NOT NULL,
    [payment_method_id] tinyint
,
    PRIMARY KEY ([cb_payment_method_id])
);
CREATE TABLE [dbo].[cb_product_mapping] (
    [cb_product_mapping_id] int IDENTITY(1,1) NOT NULL,
    [cb_product_id] int NOT NULL,
    [cb_product_name] varchar(300) NOT NULL,
    [product_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [seats] int NOT NULL,
    [years] varchar(100) NOT NULL,
    [price] money NOT NULL,
    [product_type_id] int NOT NULL,
    [is_included_in_url] bit NOT NULL,
    [insert_date] datetime NOT NULL,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([cb_product_mapping_id])
);
CREATE TABLE [dbo].[channel] (
    [channel_id] int IDENTITY(1,1) NOT NULL,
    [channel_name] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [status] varchar(20)
,
    PRIMARY KEY ([channel_id])
);
CREATE TABLE [dbo].[child_update_distribution_method_include] (
    [child_update_distribution_method_id] int IDENTITY(1,1) NOT NULL,
    [license_distribution_method_id] int,
    [insert_date] datetime,
    [insert_by] varchar(50),
    [modified_date] datetime,
    [modified_by] varchar(50)
,
    PRIMARY KEY ([child_update_distribution_method_id])
);
CREATE TABLE [dbo].[cisco_license] (
    [cisco_license_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [oem_id] varchar(100),
    [device_id] varchar(100),
    [url_link] varchar(500),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([cisco_license_id])
);
CREATE TABLE [dbo].[code_type] (
    [code_type_id] int IDENTITY(1,1) NOT NULL,
    [code_type] varchar(50) NOT NULL,
    [code_type_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([code_type_id])
);
CREATE TABLE [dbo].[commissionable_product_ids] (
    [product_id] int,
    [commission_type_id] tinyint
);
CREATE TABLE [dbo].[company] (
    [company_id] int IDENTITY(1,1) NOT NULL,
    [company_name] nvarchar(255) NOT NULL,
    [salesforce_account_id] varchar(20),
    [last_modified_date] datetime NOT NULL,
    [company_type_id] tinyint,
    [company_name_clean] nvarchar(255),
    [oracle_customer_id] int,
    [fid] int
,
    PRIMARY KEY ([company_id])
);
CREATE TABLE [dbo].[company_address] (
    [company_address_id] int IDENTITY(1,1) NOT NULL,
    [company_id] int NOT NULL,
    [address_type_id] int NOT NULL,
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [city] nvarchar(130) NOT NULL,
    [state] nvarchar(3) NOT NULL,
    [postal_code] nvarchar(32) NOT NULL,
    [country_id] smallint NOT NULL,
    [address_class_id] tinyint,
    [address_status_id] tinyint,
    [source] varchar(20) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([company_address_id])
);
CREATE TABLE [dbo].[company_address_geocode_request] (
    [company_address_geocode_request_id] int IDENTITY(1,1) NOT NULL,
    [company_address_id] int NOT NULL,
    [geocode_request_id] int NOT NULL
,
    PRIMARY KEY ([company_address_geocode_request_id])
);
CREATE TABLE [dbo].[company_address_match_logic] (
    [company_address_match_logic_id] int NOT NULL,
    [company_address_match_logic_notes] varchar(1000),
    [parameters] varchar(1000),
    [enabled] bit NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([company_address_match_logic_id])
);
CREATE TABLE [dbo].[company_address_validation] (
    [company_address_validation_id] int IDENTITY(1,1) NOT NULL,
    [company_address_id] int NOT NULL,
    [address_validation_method_id] int NOT NULL,
    [is_valid] bit NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [expiration_date] datetime
,
    PRIMARY KEY ([company_address_validation_id])
);
CREATE TABLE [dbo].[company_audit] (
    [company_audit_id] int IDENTITY(1,1) NOT NULL,
    [company_id] int NOT NULL,
    [company_name] nvarchar(255) NOT NULL,
    [salesforce_account_id] varchar(20),
    [last_modified_date] datetime NOT NULL,
    [company_type_id] tinyint,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([company_audit_id])
);
CREATE TABLE [dbo].[company_configuration] (
    [company_configuration_id] tinyint IDENTITY(1,1) NOT NULL,
    [configuration_name] varchar(50) NOT NULL,
    [status] varchar(10) NOT NULL,
    [oracle_template_name] varchar(50)
,
    PRIMARY KEY ([company_configuration_id])
);
CREATE TABLE [dbo].[company_configuration_company] (
    [company_configuration_company_id] int IDENTITY(1,1) NOT NULL,
    [company_id] int NOT NULL,
    [company_configuration_id] tinyint NOT NULL,
    [configuration_value] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([company_configuration_company_id])
);
CREATE TABLE [dbo].[company_configuration_value] (
    [company_configuration_value_id] int IDENTITY(1,1) NOT NULL,
    [company_configuration_id] tinyint NOT NULL,
    [configuration_value] nvarchar(MAX) NOT NULL
,
    PRIMARY KEY ([company_configuration_value_id])
);
CREATE TABLE [dbo].[company_email_domain] (
    [company_email_domain_id] int IDENTITY(1,1) NOT NULL,
    [company_id] int NOT NULL,
    [email_domain] varchar(100) NOT NULL,
    [email_domain_type_id] tinyint NOT NULL,
    [protected] tinyint NOT NULL,
    [customers] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([company_email_domain_id])
);
CREATE TABLE [dbo].[company_match_exception] (
    [company_match_exception_id] int IDENTITY(1,1) NOT NULL,
    [company_name] nvarchar(255) NOT NULL,
    [company_match_type] varchar(50) NOT NULL,
    [exception_reason] varchar(200) NOT NULL,
    [exception_date] datetime NOT NULL,
    [customer_update_json_id] int,
    [exception_json] nvarchar(MAX)
,
    PRIMARY KEY ([company_match_exception_id])
);
CREATE TABLE [dbo].[company_merge] (
    [company_merge_id] int IDENTITY(1,1) NOT NULL,
    [ecommerce_merge_status_id] tinyint NOT NULL,
    [sfdc_merge_status_id] tinyint NOT NULL,
    [oracle_merge_status_id] tinyint NOT NULL,
    [merge_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(100) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(100) NOT NULL
,
    PRIMARY KEY ([company_merge_id])
);
CREATE TABLE [dbo].[company_merge_company] (
    [company_merge_company_id] int IDENTITY(1,1) NOT NULL,
    [company_merge_id] int NOT NULL,
    [merge_type] varchar(20) NOT NULL,
    [company_id] int NOT NULL,
    [company_name] nvarchar(255) NOT NULL,
    [salesforce_account_id] varchar(18),
    [oracle_customer_id] int
,
    PRIMARY KEY ([company_merge_company_id])
);
CREATE TABLE [dbo].[company_name] (
    [company_name_id] int IDENTITY(1,1) NOT NULL,
    [company_id] int NOT NULL,
    [company_name_match_logic_id] int NOT NULL,
    [company_name_type_id] int NOT NULL,
    [is_romanized] bit NOT NULL,
    [derived_from_company_name_id] int,
    [apex_company_name_id] int,
    [company_name] nvarchar(255) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([company_name_id])
);
CREATE TABLE [dbo].[company_name_invalid] (
    [company_name_invalid_id] int IDENTITY(1,1) NOT NULL,
    [company_name] nvarchar(255) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([company_name_invalid_id])
);
CREATE TABLE [dbo].[company_name_match_logic] (
    [company_name_match_logic_id] int NOT NULL,
    [company_name_type_id] int NOT NULL,
    [is_romanized] bit NOT NULL,
    [derived_from_company_name_match_logic_id] int,
    [parameters] varchar(1000),
    [company_name_match_logic_notes] varchar(1000) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([company_name_match_logic_id])
);
CREATE TABLE [dbo].[company_name_type] (
    [company_name_type_id] int IDENTITY(1,1) NOT NULL,
    [company_name_type] varchar(50) NOT NULL,
    [company_name_type_description] varchar(255) NOT NULL
,
    PRIMARY KEY ([company_name_type_id])
);
CREATE TABLE [dbo].[company_search_facebook] (
    [facebook_search_id] int NOT NULL,
    [company_id] int NOT NULL,
    [company_name] nvarchar(255) NOT NULL,
    [results] nvarchar(MAX),
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[company_search_facebook_analysis] (
    [company_search_facebook_analysis_id] int NOT NULL,
    [facebook_search_id] int NOT NULL,
    [facebook_search_result_index] int NOT NULL,
    [company_id] int NOT NULL,
    [match_index] int NOT NULL,
    [match_score] float NOT NULL,
    [ecom_name] nvarchar(255),
    [fb_name] nvarchar(255),
    [ecom_phone] nvarchar(255),
    [fb_phone] nvarchar(255),
    [ecom_website] nvarchar(255),
    [fb_website] nvarchar(255),
    [ecom_address] nvarchar(255),
    [fb_address] nvarchar(255),
    [ecom_city] nvarchar(255),
    [fb_city] nvarchar(255),
    [ecom_state] nvarchar(255),
    [fb_state] nvarchar(255),
    [ecom_zip] nvarchar(255),
    [fb_zip] nvarchar(255),
    [ecom_country] nvarchar(255),
    [fb_country] nvarchar(255)
);
CREATE TABLE [dbo].[company_search_facebook_detail] (
    [search_id] int IDENTITY(1,1) NOT NULL,
    [company_id] int,
    [company_name] nvarchar(255),
    [address_1] nvarchar(255),
    [city] nvarchar(130),
    [state] varchar(3),
    [postal_code] nvarchar(32),
    [country] varchar(75),
    [phone] varchar(64),
    [website] nvarchar(300),
    [category_list] nvarchar(MAX),
    [company_match] tinyint,
    [insert_date] datetime,
    [company_domain] varchar(100),
    [match_score] float,
    [company_score] float,
    [phone_score] float,
    [website_score] float,
    [street_score] float,
    [city_score] float,
    [state_score] float,
    [zip_score] float,
    [country_score] float
,
    PRIMARY KEY ([search_id])
);
CREATE TABLE [dbo].[company_type] (
    [company_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [company_type_name] varchar(50) NOT NULL,
    [site_display] tinyint
,
    PRIMARY KEY ([company_type_id])
);
CREATE TABLE [dbo].[company_vat] (
    [company_vat_id] int IDENTITY(1,1) NOT NULL,
    [company_id] int NOT NULL,
    [country_id] smallint NOT NULL,
    [vat_id] varchar(20) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([company_vat_id])
);
CREATE TABLE [dbo].[company_vat_audit] (
    [company_vat_audit_id] int IDENTITY(1,1) NOT NULL,
    [company_vat_id] int NOT NULL,
    [company_id] int NOT NULL,
    [country_id] smallint NOT NULL,
    [vat_id] varchar(20) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([company_vat_audit_id])
);
CREATE TABLE [dbo].[console_activity_all_log] (
    [console_activity_all_id] int IDENTITY(1,1) NOT NULL,
    [console_activity_all_guid] uniqueidentifier NOT NULL,
    [console_activity_all_type] char(1) NOT NULL,
    [keycode] varchar(50) NOT NULL,
    [report_type] varchar(50) NOT NULL,
    [report_date] date NOT NULL,
    [site_keycode] varchar(50),
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[console_activity_cbep] (
    [console_activity_cbep_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [companyid] uniqueidentifier NOT NULL,
    [tenantpartnerid] uniqueidentifier,
    [organizationname] nvarchar(1000) NOT NULL,
    [cbepsubscribeddevices] int NOT NULL,
    [cbepcapacity] decimal(18,2) NOT NULL,
    [vaultid] uniqueidentifier NOT NULL,
    [effective_date] date NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([console_activity_cbep_id])
);
CREATE TABLE [dbo].[console_activity_crsb] (
    [console_activity_crsb_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [license_category_id] int,
    [capability_type_id] int,
    [parent_license_id] int,
    [sitename] varchar(1000),
    [sitemarid] int,
    [endpoints_1d] int,
    [endpoints_30d] int,
    [endpoints_60d] int,
    [endpoints_90d] int,
    [standardstorage_1d] decimal(18,2),
    [standardstorage_30d] decimal(18,2),
    [standardstorage_60d] decimal(18,2),
    [standardstorage_90d] decimal(18,2),
    [customstorage_1d] decimal(18,2),
    [customstorage_30d] decimal(18,2),
    [customstorage_60d] decimal(18,2),
    [customstorage_90d] decimal(18,2),
    [effective_date] date,
    [insert_date] datetime,
    [insert_by] varchar(200)
,
    PRIMARY KEY ([console_activity_crsb_id])
);
CREATE TABLE [dbo].[console_activity_exclusions] (
    [license_id] int NOT NULL,
    [license_category_id] int NOT NULL
);
CREATE TABLE [dbo].[console_activity_otsf] (
    [console_activity_otsf_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [tenantid] uniqueidentifier NOT NULL,
    [tenantpartnerid] uniqueidentifier,
    [organizationname] nvarchar(1000) NOT NULL,
    [office365assigneduserseats] int NOT NULL,
    [office365capacity] int NOT NULL,
    [office365assigneduserseatshigh] int NOT NULL,
    [office365capacityhigh] int NOT NULL,
    [effective_date] date NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([console_activity_otsf_id])
);
CREATE TABLE [dbo].[console_activity_pillr] (
    [console_activity_pillr_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [license_category_id] int,
    [capability_type_id] int,
    [parent_license_id] int,
    [pillr_partner_id] varchar(16),
    [pillr_partner_name] varchar(255),
    [pillr_customer_id] varchar(16),
    [pillr_customer_name] varchar(255),
    [high_watermark_endpoints] int,
    [high_watermark_gb] decimal(18,2),
    [effective_date] date,
    [insert_date] datetime,
    [insert_by] varchar(200)
,
    PRIMARY KEY ([console_activity_pillr_id])
);
CREATE TABLE [dbo].[console_activity_saep] (
    [console_activity_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [total_devices] int,
    [active_devices] int,
    [deactivated_devices] int,
    [effective_date] datetime,
    [last_30_day_devices] int,
    [active_last_30_day_devices] int,
    [capability_type_id] int,
    [parent_license_id] int,
    [marname] nvarchar(1000)
,
    PRIMARY KEY ([console_activity_id])
);
CREATE TABLE [dbo].[console_activity_sdns] (
    [console_activity_sdns_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [parent_license_id] int,
    [ipagent] varchar(50) NOT NULL,
    [consolemar] int NOT NULL,
    [marnames] nvarchar(1000),
    [dbseenlast24hours] int NOT NULL,
    [dbseenlast30days] int NOT NULL,
    [dbactivelast24hours] int NOT NULL,
    [dbactivelast30days] int NOT NULL,
    [effective_date] date NOT NULL
,
    PRIMARY KEY ([console_activity_sdns_id])
);
CREATE TABLE [dbo].[console_activity_sdns_202501_deletes] (
    [console_activity_sdns_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [parent_license_id] int,
    [ipagent] varchar(50) NOT NULL,
    [consolemar] int NOT NULL,
    [marnames] nvarchar(1000),
    [dbseenlast24hours] int NOT NULL,
    [dbseenlast30days] int NOT NULL,
    [dbactivelast24hours] int NOT NULL,
    [dbactivelast30days] int NOT NULL,
    [effective_date] date NOT NULL
);
CREATE TABLE [dbo].[console_activity_seca] (
    [console_activity_seca_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [parent_license_id] int,
    [consolemar] int NOT NULL,
    [marnames] nvarchar(1000),
    [dbactivelast24hours] int NOT NULL,
    [dbactivelast30days] int NOT NULL,
    [effective_date] date NOT NULL
,
    PRIMARY KEY ([console_activity_seca_id])
);
CREATE TABLE [dbo].[console_login] (
    [console_login_id] int IDENTITY(1,1) NOT NULL,
    [parent_mar_id] int,
    [license_id] int NOT NULL,
    [login_date] datetime NOT NULL,
    [email] varchar(100)
,
    PRIMARY KEY ([console_login_id])
);
CREATE TABLE [dbo].[console_site] (
    [console_site_id] int IDENTITY(1,1) NOT NULL,
    [console_site_name] nvarchar(255) NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([console_site_id])
);
CREATE TABLE [dbo].[console_site_audit] (
    [console_site_audit_id] int IDENTITY(1,1) NOT NULL,
    [console_site_id] int NOT NULL,
    [console_site_name] nvarchar(255) NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([console_site_audit_id])
);
CREATE TABLE [dbo].[copy_of_partner_company] (
    [partner_company_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [partner_customer_code] varchar(100) NOT NULL,
    [company_name] nvarchar(255),
    [company_id] int,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[country_currency_merchant] (
    [country_currency_merchant_id] int IDENTITY(1,1) NOT NULL,
    [country_id] smallint,
    [currency_id] tinyint NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [payment_merchant_id] tinyint NOT NULL
,
    PRIMARY KEY ([country_currency_merchant_id])
);
CREATE TABLE [dbo].[coupon_usage] (
    [itemid] int IDENTITY(1,1) NOT NULL,
    [coupon_code] varchar(7) NOT NULL,
    [dt_used] datetime NOT NULL,
    [invoice_code] varchar(20) NOT NULL
,
    PRIMARY KEY ([itemid])
);
CREATE TABLE [dbo].[csi_comments] (
    [comment_id] int IDENTITY(1,1) NOT NULL,
    [subject] varchar(255) NOT NULL,
    [comment] varchar(8000) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [customer_id] int,
    [invoice_id] int,
    [license_id] int,
    [refund_reason_code] int,
    [csi_users_id] varchar(50)
,
    PRIMARY KEY ([comment_id])
);
CREATE TABLE [dbo].[csi_user_usage] (
    [csi_user_usage_id] int IDENTITY(1,1) NOT NULL,
    [csi_user_id] varchar(50),
    [insert_date] datetime
);
CREATE TABLE [dbo].[csi_users] (
    [csi_user_rec_id] int IDENTITY(1,1) NOT NULL,
    [csi_users_ID] varchar(50) NOT NULL,
    [first_name] varchar(255) NOT NULL,
    [last_name] varchar(255) NOT NULL,
    [agent_number] int,
    [DefaultLanguage] smallint,
    [permission_data] varchar(2048)
,
    PRIMARY KEY ([csi_user_rec_id])
);
CREATE TABLE [dbo].[currency] (
    [currency_id] tinyint IDENTITY(1,1) NOT NULL,
    [currency_code] char(3),
    [currency_description] varchar(20) NOT NULL,
    [symbol_html] varchar(10),
    [symbol_utf8] nvarchar(10),
    [symbol_text] varchar(10),
    [exchange_rate] float,
    [exchange_multiplier] float,
    [dr_locale] varchar(10),
    [active] tinyint,
    [last_modified_date] datetime,
    [last_modified_by] varchar(200),
    [vat_rate] float
,
    PRIMARY KEY ([currency_id])
);
CREATE TABLE [dbo].[currency_language_location] (
    [currency_language_location_id] int IDENTITY(1,1) NOT NULL,
    [currency_id] tinyint NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [dr_locale] varchar(10),
    [symbol_trailing] tinyint,
    [fraction_unit_separator] varchar(2),
    [whole_unit_separator] varchar(2),
    [show_minor] tinyint
,
    PRIMARY KEY ([currency_language_location_id])
);
CREATE TABLE [dbo].[customer] (
    [customer_id] int IDENTITY(1,1) NOT NULL,
    [first_name] nvarchar(225),
    [middle_name] nvarchar(32),
    [last_name] nvarchar(225),
    [title] nvarchar(50),
    [opt_in] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [salesforce_contact_id] varchar(20),
    [person_id] int
,
    PRIMARY KEY ([customer_id])
);
CREATE TABLE [dbo].[customer_address] (
    [customer_address_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int NOT NULL,
    [address_type_id] int NOT NULL,
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [address_3] nvarchar(255),
    [city] nvarchar(130) NOT NULL,
    [state] nvarchar(2) NOT NULL,
    [postal_code] nvarchar(32) NOT NULL,
    [country_id] smallint NOT NULL,
    [vendor_address_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [address_status_id] tinyint NOT NULL,
    [clean_address] nvarchar(510),
    [clean_city] nvarchar(130),
    [clean_postal_code] nvarchar(32)
,
    PRIMARY KEY ([customer_address_id])
);
CREATE TABLE [dbo].[customer_audit] (
    [customer_audit_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int NOT NULL,
    [first_name] nvarchar(225),
    [middle_name] nvarchar(32),
    [last_name] nvarchar(225),
    [title] nvarchar(50),
    [opt_in] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [audit_date] datetime NOT NULL,
    [audit_by] varchar(200) NOT NULL,
    [salesforce_contact_id] varchar(20)
,
    PRIMARY KEY ([customer_audit_id])
);
CREATE TABLE [dbo].[customer_company] (
    [customer_company_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int NOT NULL,
    [company_name] nvarchar(255) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [company_id] int,
    [customer_type_id] int,
    [company_type_id] tinyint
,
    PRIMARY KEY ([customer_company_id])
);
CREATE TABLE [dbo].[customer_company_audit] (
    [customer_company_audit_id] int IDENTITY(1,1) NOT NULL,
    [customer_company_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [company_name] nvarchar(255) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [company_id] int,
    [customer_type_id] int,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([customer_company_audit_id])
);
CREATE TABLE [dbo].[customer_email] (
    [customer_email_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [email_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [email_domain] varchar(100)
,
    PRIMARY KEY ([customer_email_id])
);
CREATE TABLE [dbo].[customer_email_domain] (
    [customer_email_domain_id] int IDENTITY(1,1) NOT NULL,
    [email_domain] varchar(100) NOT NULL,
    [email_domain_type_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([customer_email_domain_id])
);
CREATE TABLE [dbo].[customer_email_invalid] (
    [customer_email_invalid_id] int IDENTITY(1,1) NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([customer_email_invalid_id])
);
CREATE TABLE [dbo].[customer_email_verification] (
    [customer_email_verification_id] int IDENTITY(1,1) NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [customer_id] int,
    [customer_email_verification_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([customer_email_verification_id])
);
CREATE TABLE [dbo].[customer_email_verification_status] (
    [customer_email_verification_status_id] int IDENTITY(1,1) NOT NULL,
    [vendor_status] varchar(100) NOT NULL,
    [risk] varchar(100) NOT NULL,
    [webroot_status] varchar(100) NOT NULL
,
    PRIMARY KEY ([customer_email_verification_status_id])
);
CREATE TABLE [dbo].[customer_in_process] (
    [customer_in_process_id] int IDENTITY(1,1) NOT NULL,
    [invoice_in_process_id] int NOT NULL,
    [first_name] nvarchar(255),
    [last_name] nvarchar(255),
    [company_name] nvarchar(255),
    [address1] nvarchar(255),
    [address2] nvarchar(255),
    [city] nvarchar(130),
    [state_id] char(2),
    [postal_code] varchar(32),
    [country_id] int,
    [phone_number] varchar(64),
    [customer_email] varchar(255),
    [opt_in] bit,
    [modified_date] datetime,
    [insert_date] datetime
,
    PRIMARY KEY ([customer_in_process_id])
);
CREATE TABLE [dbo].[customer_language] (
    [customer_language_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int NOT NULL,
    [language_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([customer_language_id])
);
CREATE TABLE [dbo].[customer_load] (
    [customer_load_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_id] bigint NOT NULL,
    [vendor_id] int NOT NULL,
    [vendor_customer_id] bigint NOT NULL,
    [vendor_address_id] int,
    [customer_type] varchar(10) NOT NULL,
    [first_name] nvarchar(255),
    [middle_name] nvarchar(32),
    [last_name] nvarchar(255),
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [address_3] nvarchar(255),
    [city] nvarchar(130),
    [state] char(2),
    [postal_code] nvarchar(32),
    [country] char(3),
    [phone_number] varchar(64),
    [fax_number] varchar(64),
    [company_name] nvarchar(255),
    [alternate_phone_number] varchar(64),
    [customer_email] nvarchar(255),
    [opt_in] bit NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [order_notification_status_id] int NOT NULL
,
    PRIMARY KEY ([customer_load_id])
);
CREATE TABLE [dbo].[customer_load_archive] (
    [customer_load_archive_id] int IDENTITY(1,1) NOT NULL,
    [customer_load_id] int NOT NULL,
    [vendor_order_id] bigint NOT NULL,
    [vendor_id] int NOT NULL,
    [vendor_customer_id] bigint NOT NULL,
    [vendor_address_id] int,
    [customer_type] varchar(10) NOT NULL,
    [first_name] nvarchar(255),
    [middle_name] nvarchar(32),
    [last_name] nvarchar(255),
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [address_3] nvarchar(255),
    [city] nvarchar(130),
    [state] char(2),
    [postal_code] nvarchar(32),
    [country] char(3),
    [phone_number] varchar(64),
    [fax_number] varchar(64),
    [company_name] nvarchar(255),
    [alternate_phone_number] varchar(64),
    [customer_email] nvarchar(255),
    [opt_in] bit NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [order_notification_status_id] int NOT NULL
,
    PRIMARY KEY ([customer_load_archive_id])
);
CREATE TABLE [dbo].[customer_locale] (
    [customer_locale_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [customer_locale_source_id] tinyint NOT NULL,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([customer_locale_id])
);
CREATE TABLE [dbo].[customer_locale_source] (
    [customer_locale_source_id] tinyint IDENTITY(1,1) NOT NULL,
    [customer_locale_source] varchar(50) NOT NULL
,
    PRIMARY KEY ([customer_locale_source_id])
);
CREATE TABLE [dbo].[customer_merge] (
    [customer_merge_id] int IDENTITY(1,1) NOT NULL,
    [master_customer_id] int NOT NULL,
    [merge_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([customer_merge_id])
);
CREATE TABLE [dbo].[customer_merge_customer] (
    [customer_merge_id] int NOT NULL,
    [customer_id] int NOT NULL
,
    PRIMARY KEY ([customer_id], [customer_merge_id])
);
CREATE TABLE [dbo].[customer_order] (
    [customer_order_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [customer_type_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([customer_order_id])
);
CREATE TABLE [dbo].[customer_order_address] (
    [customer_order_address_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [customer_address_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([customer_order_address_id])
);
CREATE TABLE [dbo].[customer_order_address_audit] (
    [customer_order_address_audit_id] int IDENTITY(1,1) NOT NULL,
    [customer_order_address_id] int,
    [order_header_id] int NOT NULL,
    [customer_address_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([customer_order_address_audit_id])
);
CREATE TABLE [dbo].[customer_order_audit] (
    [customer_order_audit_id] int IDENTITY(1,1) NOT NULL,
    [customer_order_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [customer_type_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([customer_order_audit_id])
);
CREATE TABLE [dbo].[customer_phone] (
    [customer_phone_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int NOT NULL,
    [phone_type_id] int NOT NULL,
    [phone_number] varchar(64) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([customer_phone_id])
);
CREATE TABLE [dbo].[customer_phone_clean] (
    [unique_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int NOT NULL,
    [phone_number] varchar(64) NOT NULL,
    [phone_number_clean] varchar(64) NOT NULL
);
CREATE TABLE [dbo].[customer_phone_type] (
    [phone_type_id] int IDENTITY(1,1) NOT NULL,
    [phone_type_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([phone_type_id])
);
CREATE TABLE [dbo].[customer_sequence] (
    [customer_sequence_id] bigint IDENTITY(1,1) NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[customer_service] (
    [customer_service_id] int IDENTITY(1,1) NOT NULL,
    [customer_service_type_id] tinyint NOT NULL,
    [customer_service_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime,
    [update_attempts] tinyint NOT NULL
,
    PRIMARY KEY ([customer_service_id])
);
CREATE TABLE [dbo].[customer_service_archive] (
    [customer_service_archive_id] int IDENTITY(1,1) NOT NULL,
    [customer_service_id] int NOT NULL,
    [customer_service_type_id] tinyint NOT NULL,
    [customer_service_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [archive_date] datetime NOT NULL
,
    PRIMARY KEY ([customer_service_archive_id])
);
CREATE TABLE [dbo].[customer_service_failure] (
    [customer_service_failure_id] int IDENTITY(1,1) NOT NULL,
    [customer_service_id] int NOT NULL,
    [customer_service_type_id] tinyint NOT NULL,
    [customer_service_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [failure_date] datetime NOT NULL
,
    PRIMARY KEY ([customer_service_failure_id])
);
CREATE TABLE [dbo].[customer_service_json] (
    [customer_service_json_id] int IDENTITY(1,1) NOT NULL,
    [customer_service_id] int NOT NULL,
    [customer_service_json] nvarchar(MAX) NOT NULL
,
    PRIMARY KEY ([customer_service_json_id])
);
CREATE TABLE [dbo].[customer_service_status] (
    [customer_service_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [customer_service_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([customer_service_status_id])
);
CREATE TABLE [dbo].[customer_service_type] (
    [customer_service_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [customer_service_type_name] varchar(50) NOT NULL,
    [process_type] varchar(20),
    [customer_service_type_description] nvarchar(MAX)
,
    PRIMARY KEY ([customer_service_type_id])
);
CREATE TABLE [dbo].[customer_type] (
    [customer_type_id] int IDENTITY(1,1) NOT NULL,
    [customer_type_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([customer_type_id])
);
CREATE TABLE [dbo].[customer_type_customer] (
    [customer_type_customer_id] int IDENTITY(1,1) NOT NULL,
    [customer_type_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([customer_type_customer_id])
);
CREATE TABLE [dbo].[customer_update_json] (
    [customer_update_json_id] int IDENTITY(1,1) NOT NULL,
    [customer_update_json] nvarchar(MAX) NOT NULL,
    [application_origin] nvarchar(250),
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(50) NOT NULL,
    [complete_date] datetime,
    [output_json] nvarchar(MAX)
,
    PRIMARY KEY ([customer_update_json_id])
);
CREATE TABLE [dbo].[customer_vat] (
    [customer_vat_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int NOT NULL,
    [vat_id] varchar(20) NOT NULL,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([customer_vat_id])
);
CREATE TABLE [dbo].[CustomerCodePrefix] (
    [Prefix] varchar(4) NOT NULL,
    [OriginalDB] varchar(8) NOT NULL,
    [Description] varchar(50)
,
    PRIMARY KEY ([Prefix])
);
CREATE TABLE [dbo].[CustomerEmailHistory] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Date] datetime,
    [Customer_Code] varchar(16),
    [Email] varchar(64)
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[CustomersAudit] (
    [customer_code] varchar(16) NOT NULL,
    [first_name] nvarchar(32),
    [last_name] nvarchar(32),
    [company1] nvarchar(48),
    [company2] nvarchar(48),
    [address1] nvarchar(48),
    [address2] nvarchar(48),
    [city] nvarchar(48),
    [state_id] nchar(2),
    [other_state] nvarchar(32),
    [country_id] smallint,
    [postal_code] nvarchar(10),
    [age] int,
    [sex] nchar(1),
    [marital_status] nchar(1),
    [income_level_id] smallint,
    [email] nvarchar(64),
    [telephone] nvarchar(20),
    [fax] nvarchar(20),
    [wantoffers] smallint,
    [wantupdates] smallint,
    [wantnews] smallint,
    [credit_balance] decimal(9,2),
    [password] nvarchar(16),
    [password_clue_id] smallint,
    [failed_send] tinyint,
    [last_modified] datetime NOT NULL,
    [for_id] nvarchar(150),
    [insert_date] datetime,
    [AuditTimestamp] datetime,
    [AuditSystemUser] varchar(50)
);
CREATE TABLE [dbo].[customize_pricing] (
    [customize_pricing_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [low_range] int,
    [high_range] int,
    [customized_price] money,
    [insert_date] datetime,
    [end_date] datetime
,
    PRIMARY KEY ([customize_pricing_id])
);
CREATE TABLE [dbo].[customize_pricing_license] (
    [customize_pricing_license_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [partner_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [partner_transfer_date] date,
    [category_start_date] date,
    [insert_date] datetime,
    [modified_date] datetime,
    [is_active] bit
,
    PRIMARY KEY ([customize_pricing_license_id])
);
CREATE TABLE [dbo].[customize_pricing_partner] (
    [customize_pricing_partner_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [insert_date] datetime,
    [is_active] bit
,
    PRIMARY KEY ([customize_pricing_partner_id])
);
CREATE TABLE [dbo].[cybs_customer_profile] (
    [cybs_customer_profile_id] int IDENTITY(1,1) NOT NULL,
    [invoice_in_process_id] int NOT NULL,
    [customer_profile_token] varchar(255) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([cybs_customer_profile_id])
);
CREATE TABLE [dbo].[cybs_customer_profile_customer] (
    [cybs_customer_profile_customer_id] int IDENTITY(1,1) NOT NULL,
    [cybs_customer_profile_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([cybs_customer_profile_customer_id])
);
CREATE TABLE [dbo].[cybs_customer_profile_license] (
    [cybs_customer_profile_license_id] int IDENTITY(1,1) NOT NULL,
    [cybs_customer_profile_id] int NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([cybs_customer_profile_license_id])
);
CREATE TABLE [dbo].[cybs_customer_profile_order] (
    [cybs_customer_profile_order_id] int IDENTITY(1,1) NOT NULL,
    [cybs_customer_profile_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([cybs_customer_profile_order_id])
);
CREATE TABLE [dbo].[cybs_error_log] (
    [cybs_error_log_id] int IDENTITY(1,1) NOT NULL,
    [reason_code] int NOT NULL,
    [request_token_type] varchar(20) NOT NULL,
    [request_token] varchar(255),
    [invoice_code] varchar(16),
    [request_id] varchar(255),
    [field_in_question] varchar(255),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[cybs_process_token] (
    [cybs_process_token_id] int IDENTITY(1,1) NOT NULL,
    [invoice_code] varchar(16) NOT NULL,
    [request_id] varchar(255) NOT NULL,
    [request_token] varchar(255) NOT NULL,
    [request_token_type] varchar(20) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([cybs_process_token_id])
);
CREATE TABLE [dbo].[cybs_subscription] (
    [cybs_subscription_id] int IDENTITY(1,1) NOT NULL,
    [credit_card_subscription_id] varchar(24),
    [payment_merchant_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [account_number] varchar(4),
    [expiration_month] tinyint,
    [expiration_year] smallint,
    [card_type_id] tinyint
,
    PRIMARY KEY ([cybs_subscription_id])
);
CREATE TABLE [dbo].[cybs_subscription_customer] (
    [cybs_subscription_customer_id] int IDENTITY(1,1) NOT NULL,
    [cybs_subscription_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [company_id] int,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([cybs_subscription_customer_id])
);
CREATE TABLE [dbo].[cybs_subscription_log] (
    [cybs_subscription_log_id] int IDENTITY(1,1) NOT NULL,
    [cybs_subscription_id] int NOT NULL,
    [log_type] varchar(20) NOT NULL,
    [response_code] varchar(3),
    [response_message] varchar(120),
    [log_text] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([cybs_subscription_log_id])
);
CREATE TABLE [dbo].[cybs_tax_transaction_in_process] (
    [cybs_tax_trans_in_process_id] int IDENTITY(1,1) NOT NULL,
    [invoice_in_process_id] int NOT NULL,
    [currency] varchar(10) NOT NULL,
    [ics_rcode] int NOT NULL,
    [ics_rflag] varchar(50) NOT NULL,
    [ics_rmsg] varchar(255) NOT NULL,
    [invoice_id] varchar(50) NOT NULL,
    [request_id] varchar(26) NOT NULL,
    [tax_city_name] varchar(50),
    [tax_county_name] varchar(50),
    [tax_rcode] int NOT NULL,
    [tax_rflag] varchar(50) NOT NULL,
    [tax_rmsg] varchar(255) NOT NULL,
    [tax_state_name] char(2),
    [tax_total_city_tax] decimal(15,2) NOT NULL,
    [tax_total_county_tax] decimal(15,2) NOT NULL,
    [tax_total_district_tax] decimal(15,2) NOT NULL,
    [tax_total_grand] decimal(15,2) NOT NULL,
    [tax_total_state_tax] decimal(15,2) NOT NULL,
    [tax_total_tax] decimal(15,2) NOT NULL,
    [tax_zip] varchar(20),
    [transaction_date] datetime,
    [tax_country] char(2) NOT NULL
,
    PRIMARY KEY ([cybs_tax_trans_in_process_id], [invoice_in_process_id])
);
CREATE TABLE [dbo].[cybs_tax_transactions] (
    [currency] varchar(10) NOT NULL,
    [ics_rcode] int NOT NULL,
    [ics_rflag] varchar(50) NOT NULL,
    [ics_rmsg] varchar(255) NOT NULL,
    [invoice_id] varchar(50) NOT NULL,
    [request_id] varchar(26) NOT NULL,
    [tax_city_name] varchar(50),
    [tax_county_name] varchar(50),
    [tax_rcode] int NOT NULL,
    [tax_rflag] varchar(50) NOT NULL,
    [tax_rmsg] varchar(255) NOT NULL,
    [tax_state_name] char(2),
    [tax_total_city_tax] decimal(15,2) NOT NULL,
    [tax_total_county_tax] decimal(15,2) NOT NULL,
    [tax_total_district_tax] decimal(15,2) NOT NULL,
    [tax_total_grand] decimal(15,2) NOT NULL,
    [tax_total_state_tax] decimal(15,2) NOT NULL,
    [tax_total_tax] decimal(15,2) NOT NULL,
    [tax_zip] varchar(20),
    [transaction_date] datetime,
    [tax_country] char(2) NOT NULL
);
CREATE TABLE [dbo].[cybs_tax_transactions_soap] (
    [invoice_id] varchar(50) NOT NULL,
    [request_id] varchar(26) NOT NULL,
    [request_token] varchar(255) NOT NULL,
    [currency] varchar(10) NOT NULL,
    [tax_total_amount] decimal(15,2) NOT NULL,
    [grand_total_amount] decimal(15,2) NOT NULL,
    [tax_city_name] varchar(50),
    [tax_state_name] char(2),
    [tax_zip] varchar(20),
    [tax_total_city_amount] decimal(15,2) NOT NULL,
    [tax_total_county_amount] decimal(15,2) NOT NULL,
    [tax_total_district_amount] decimal(15,2) NOT NULL,
    [tax_total_state_amount] decimal(15,2) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[database_connection] (
    [connection_id] int IDENTITY(1,1) NOT NULL,
    [connection_type] varchar(50) NOT NULL,
    [database_name] varchar(50) NOT NULL,
    [link_server_name] varchar(50),
    [acitve_status] tinyint
);
CREATE TABLE [dbo].[DBErrorlog] (
    [Record_ID] int IDENTITY(1,1) NOT NULL,
    [Add_Date] datetime,
    [ErrorNumber] int,
    [ErrorSeverity] int,
    [ErrorState] int,
    [ErrorProcedure] nvarchar(128),
    [ErrorLine] int,
    [ErrorMessage] nvarchar(4000),
    [ErrorServer] nvarchar(128),
    [ErrorDB] nvarchar(128),
    [ErrorUser] nvarchar(128)
,
    PRIMARY KEY ([Record_ID])
);
CREATE TABLE [dbo].[dbprefix] (
    [dbprefix_id] char(1) NOT NULL,
    [server_name] varchar(50)
,
    PRIMARY KEY ([dbprefix_id])
);
CREATE TABLE [dbo].[discount] (
    [discount_id] int IDENTITY(1,1) NOT NULL,
    [discount_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([discount_id])
);
CREATE TABLE [dbo].[discount_range] (
    [discount_range_id] int IDENTITY(1,1) NOT NULL,
    [discount_id] int NOT NULL,
    [low_quantity] int NOT NULL,
    [high_quantity] int NOT NULL,
    [discount_percent] decimal(9,7),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([discount_range_id])
);
CREATE TABLE [dbo].[dish_customer_status] (
    [dish_customer_status_id] int IDENTITY(1,1) NOT NULL,
    [customer_status] varchar(15) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([dish_customer_status_id])
);
CREATE TABLE [dbo].[dish_customer_type] (
    [dish_customer_type_id] int IDENTITY(1,1) NOT NULL,
    [customer_type] varchar(15) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([dish_customer_type_id])
);
CREATE TABLE [dbo].[dish_license_subscription] (
    [dish_license_subscription_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [customer_code] varchar(15),
    [dish_guid] varchar(50) NOT NULL,
    [dish_customer_type_id] int,
    [dish_customer_status_id] int,
    [protection_plan_add_date] datetime,
    [dish_protection_plan_tier_id] int,
    [promotional_period_end_date] datetime,
    [plan_drop_date] datetime,
    [install_date] datetime,
    [last_deactivation_date] datetime,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [last_modified_date] datetime,
    [last_modified_by] varchar(200)
,
    PRIMARY KEY ([dish_license_subscription_id])
);
CREATE TABLE [dbo].[dish_license_subscription_cancel] (
    [license_id] int NOT NULL,
    [cancel_date] datetime,
    [insert_date] datetime,
    [insert_by] varchar(200)
);
CREATE TABLE [dbo].[dish_license_subscription_cancel_archive] (
    [dish_license_subscription_cancel_archive_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [cancel_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200),
    [archive_date] datetime
);
CREATE TABLE [dbo].[dish_license_subscription_history] (
    [dish_license_subscription_history_id] int IDENTITY(1,1) NOT NULL,
    [dish_license_subscription_id] int NOT NULL,
    [license_id] int NOT NULL,
    [customer_code] varchar(15),
    [dish_guid] varchar(50) NOT NULL,
    [dish_customer_type_id] int,
    [dish_customer_status_id] int,
    [protection_plan_add_date] datetime,
    [dish_protection_plan_tier_id] int,
    [promotional_period_end_date] datetime,
    [plan_drop_date] datetime,
    [install_date] datetime,
    [last_deactivation_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([dish_license_subscription_history_id])
);
CREATE TABLE [dbo].[dish_protection_plan_tier] (
    [dish_protection_plan_tier_id] int IDENTITY(1,1) NOT NULL,
    [protection_plan_tier] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([dish_protection_plan_tier_id])
);
CREATE TABLE [dbo].[distribution_account] (
    [distribution_account_id] tinyint IDENTITY(1,1) NOT NULL,
    [distribution_account] varchar(50) NOT NULL,
    [status] varchar(20)
,
    PRIMARY KEY ([distribution_account_id])
);
CREATE TABLE [dbo].[distribution_business_unit] (
    [distribution_business_unit_id] tinyint IDENTITY(1,1) NOT NULL,
    [distribution_business_unit] varchar(50) NOT NULL,
    [status] varchar(20)
,
    PRIMARY KEY ([distribution_business_unit_id])
);
CREATE TABLE [dbo].[distribution_class] (
    [distribution_class_id] tinyint IDENTITY(1,1) NOT NULL,
    [distribution_class] varchar(50) NOT NULL,
    [status] varchar(20)
,
    PRIMARY KEY ([distribution_class_id])
);
CREATE TABLE [dbo].[distribution_geography] (
    [distribution_geography_id] tinyint IDENTITY(1,1) NOT NULL,
    [distribution_geography] varchar(50) NOT NULL,
    [status] varchar(20)
,
    PRIMARY KEY ([distribution_geography_id])
);
CREATE TABLE [dbo].[dr_bundle] (
    [dr_bundle_id] int IDENTITY(1,1) NOT NULL,
    [offer_id] bigint NOT NULL,
    [offer_name] varchar(200) NOT NULL,
    [parent_product_id] int NOT NULL,
    [current_storage_gb] int,
    [storage_gb] int,
    [language] varchar(10),
    [start_months] int,
    [end_months] int,
    [insert_date] datetime
,
    PRIMARY KEY ([dr_bundle_id])
);
CREATE TABLE [dbo].[dr_bundle_product] (
    [dr_bundle_product_id] int IDENTITY(1,1) NOT NULL,
    [dr_bundle_id] int NOT NULL,
    [dr_product_id] int NOT NULL,
    [insert_date] datetime
,
    PRIMARY KEY ([dr_bundle_product_id])
);
CREATE TABLE [dbo].[dr_bundle_products] (
    [dr_bundle_products_id] int IDENTITY(1,1) NOT NULL,
    [dr_bundle_id] int NOT NULL,
    [upgrade_dr_product_id] int,
    [renewal_dr_product_id] int,
    [storage_dr_product_id] int,
    [renewal_storage_dr_product_id] int
,
    PRIMARY KEY ([dr_bundle_products_id])
);
CREATE TABLE [dbo].[dr_cart_customer_in_process] (
    [dr_cart_customer_in_process_id] int IDENTITY(1,1) NOT NULL,
    [first_name] nvarchar(255),
    [last_name] nvarchar(255),
    [bill_address_1] nvarchar(255),
    [bill_address_2] nvarchar(255),
    [bill_address_3] nvarchar(255),
    [bill_city] nvarchar(130),
    [bill_state] char(2),
    [bill_postal_code] nvarchar(32),
    [bill_country] char(3),
    [ship_address_1] nvarchar(255),
    [ship_address_2] nvarchar(255),
    [ship_address_3] nvarchar(255),
    [ship_city] nvarchar(130),
    [ship_state] char(2),
    [ship_postal_code] nvarchar(32),
    [ship_country] char(3),
    [phone_number] varchar(64),
    [fax_number] varchar(64),
    [company_name] nvarchar(255),
    [customer_email] nvarchar(255),
    [opt_in] bit NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [dr_cart_order_in_process_id] int,
    [external_customer_key] varchar(100)
,
    PRIMARY KEY ([dr_cart_customer_in_process_id])
);
CREATE TABLE [dbo].[dr_cart_order_error] (
    [dr_cart_order_error_id] int IDENTITY(1,1) NOT NULL,
    [dr_cart_order_in_process_id] int NOT NULL,
    [dr_order_notification_xml_id] int NOT NULL,
    [vendor_order_code] varchar(100),
    [error_reason] varchar(1000),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([dr_cart_order_error_id])
);
CREATE TABLE [dbo].[dr_cart_order_in_process] (
    [dr_cart_order_in_process_id] int IDENTITY(1,1) NOT NULL,
    [dr_cart_customer_in_process_id] int NOT NULL,
    [dr_order_notification_xml_id] int NOT NULL,
    [vendor_order_code] varchar(100),
    [site_id] varchar(65) NOT NULL,
    [site_url] varchar(1025) NOT NULL,
    [p_rc] varchar(50) NOT NULL,
    [p_ac] varchar(100),
    [trx_rc] varchar(50),
    [trx_ac] varchar(100),
    [total_amount] money NOT NULL,
    [sub_total_amount] money NOT NULL,
    [tax_amount] money,
    [payment_method] varchar(255) NOT NULL,
    [exchange_rate] float,
    [submission_date] datetime NOT NULL,
    [locale] char(5) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([dr_cart_order_in_process_id])
);
CREATE TABLE [dbo].[dr_cart_order_item_in_process] (
    [dr_cart_order_item_in_process_id] int IDENTITY(1,1) NOT NULL,
    [dr_cart_order_in_process_id] int NOT NULL,
    [vendor_id] int,
    [line_item] int,
    [vendor_product_id] int,
    [quantity] int,
    [list_price] money,
    [unit_price] money,
    [tax] money,
    [tax_exempt] bit,
    [product_id] nvarchar(100),
    [prod_year] int,
    [seats] int,
    [product_locale] char(5),
    [transaction_description] char(100),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([dr_cart_order_item_in_process_id])
);
CREATE TABLE [dbo].[dr_cart_order_item_license_in_process] (
    [dr_cart_order_item_license_in_process_id] int IDENTITY(1,1) NOT NULL,
    [dr_cart_order_item_in_process_id] int NOT NULL,
    [keycode] nvarchar(200),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([dr_cart_order_item_license_in_process_id])
);
CREATE TABLE [dbo].[dr_cart_order_refund] (
    [dr_cart_order_refund_id] int IDENTITY(1,1) NOT NULL,
    [dr_cart_order_in_process_id] int NOT NULL,
    [dr_order_notification_xml_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [vendor_order_code] varchar(100)
,
    PRIMARY KEY ([dr_cart_order_refund_id])
);
CREATE TABLE [dbo].[dr_license_subscription] (
    [dr_license_subscription_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [subscription_id] bigint NOT NULL,
    [dr_user_id] bigint NOT NULL,
    [product_id] int NOT NULL,
    [current_expiration_date] datetime,
    [dr_subscription_status_id] tinyint NOT NULL,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([dr_license_subscription_id])
);
CREATE TABLE [dbo].[dr_license_subscription_history] (
    [dr_license_subscription_history_id] int IDENTITY(1,1) NOT NULL,
    [dr_license_subscription_id] int NOT NULL,
    [current_expiration_date] datetime,
    [dr_subscription_status_id] tinyint NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [dr_subscription_change_reason_id] tinyint NOT NULL,
    [history_date] datetime NOT NULL
,
    PRIMARY KEY ([dr_license_subscription_history_id])
);
CREATE TABLE [dbo].[dr_offer] (
    [dr_offer_id] bigint NOT NULL,
    [dr_offer_name] varchar(100) NOT NULL,
    [site_id] varchar(20)
);
CREATE TABLE [dbo].[dr_offer_cart_discount] (
    [dr_offer_id] bigint NOT NULL,
    [cart_discount_id] int NOT NULL
);
CREATE TABLE [dbo].[dr_order_notification_xml] (
    [dr_order_notification_xml_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_code] varchar(100),
    [processed] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [order_notification_xml] xml NOT NULL,
    [attempts] int NOT NULL
,
    PRIMARY KEY ([dr_order_notification_xml_id], [modified_date])
);
CREATE TABLE [dbo].[dr_order_notification_xml_switch] (
    [dr_order_notification_xml_id] int NOT NULL,
    [vendor_order_code] varchar(100),
    [processed] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [order_notification_xml] xml NOT NULL,
    [attempts] int NOT NULL
,
    PRIMARY KEY ([dr_order_notification_xml_id], [modified_date])
);
CREATE TABLE [dbo].[dr_product] (
    [dr_product_id] int IDENTITY(1,1) NOT NULL,
    [dr_base_product_id] int NOT NULL,
    [dr_variation_product_id] int NOT NULL,
    [product_id] int NOT NULL,
    [variation_name] varchar(100) NOT NULL,
    [display_name] varchar(100) NOT NULL,
    [autorenewal] varchar(100) NOT NULL,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([dr_product_id])
);
CREATE TABLE [dbo].[dr_product_price] (
    [dr_product_price_id] int IDENTITY(1,1) NOT NULL,
    [dr_product_id] int NOT NULL,
    [dr_product_name] varchar(150) NOT NULL,
    [dr_base_product_id] int NOT NULL,
    [product_id] int NOT NULL,
    [start_months] smallint,
    [end_months] smallint,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3),
    [retail_price] money NOT NULL,
    [currency] varchar(10) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([dr_product_price_id])
);
CREATE TABLE [dbo].[dr_report] (
    [dr_report_id] int IDENTITY(1,1) NOT NULL,
    [dr_report_name] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([dr_report_id])
);
CREATE TABLE [dbo].[dr_subscription] (
    [dr_subscription_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] bigint NOT NULL,
    [dr_user_id] bigint NOT NULL,
    [vendor_order_code] varchar(100) NOT NULL,
    [dr_product_id] int NOT NULL,
    [product_id] int NOT NULL,
    [locale] varchar(10) NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [xml_type] varchar(50),
    [processed] tinyint,
    [attempts] int NOT NULL
,
    PRIMARY KEY ([dr_subscription_id])
);
CREATE TABLE [dbo].[dr_subscription_action] (
    [dr_subscription_action_id] tinyint IDENTITY(1,1) NOT NULL,
    [dr_subscription_action] varchar(20) NOT NULL
,
    PRIMARY KEY ([dr_subscription_action_id])
);
CREATE TABLE [dbo].[dr_subscription_reprocess] (
    [dr_subscription_id] int NOT NULL,
    [xml_type] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[dr_subscription_update] (
    [dr_subscription_update_id] int IDENTITY(1,1) NOT NULL,
    [dr_license_subscription_id] int NOT NULL,
    [dr_subscription_action_id] tinyint NOT NULL,
    [dr_subscription_date] datetime NOT NULL,
    [dr_subscription_update_status_id] tinyint NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([dr_subscription_update_id])
);
CREATE TABLE [dbo].[dr_subscription_update_status] (
    [dr_subscription_update_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [dr_subscription_update_status] varchar(20) NOT NULL
,
    PRIMARY KEY ([dr_subscription_update_status_id])
);
CREATE TABLE [dbo].[dr_subscription_xml] (
    [dr_subscription_xml_id] int IDENTITY(1,1) NOT NULL,
    [dr_subscription_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [dr_subscription_xml] nvarchar(MAX)
,
    PRIMARY KEY ([dr_subscription_xml_id])
);
CREATE TABLE [dbo].[dr_theme] (
    [dr_theme_id] int IDENTITY(1,1) NOT NULL,
    [dr_theme_description] varchar(100) NOT NULL,
    [dr_theme_value] varchar(30) NOT NULL
,
    PRIMARY KEY ([dr_theme_id])
);
CREATE TABLE [dbo].[dr_theme_license_distribution_method] (
    [dr_theme_license_distribution_method_id] int IDENTITY(1,1) NOT NULL,
    [dr_theme_id] int NOT NULL,
    [license_distribution_method_id] int NOT NULL
,
    PRIMARY KEY ([dr_theme_license_distribution_method_id])
);
CREATE TABLE [dbo].[dtproperties] (
    [id] int IDENTITY(1,1) NOT NULL,
    [objectid] int,
    [property] varchar(64) NOT NULL,
    [value] varchar(255),
    [uvalue] nvarchar(255),
    [lvalue] image,
    [version] int NOT NULL
,
    PRIMARY KEY ([id], [property])
);
CREATE TABLE [dbo].[effective_object] (
    [effective_object_id] tinyint IDENTITY(1,1) NOT NULL,
    [object_name] varchar(50) NOT NULL,
    [object_element_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([effective_object_id])
);
CREATE TABLE [dbo].[effective_object_transaction_type] (
    [effective_object_transaction_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [effective_object_transaction_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([effective_object_transaction_type_id])
);
CREATE TABLE [dbo].[effective_object_value] (
    [effective_object_value_id] int IDENTITY(1,1) NOT NULL,
    [effective_object_id] tinyint NOT NULL,
    [effective_value] int NOT NULL,
    [effective_object_value_name] nvarchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([effective_object_value_id])
);
CREATE TABLE [dbo].[email_cart_order] (
    [email_cart_order_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_in_process_id] int,
    [cart_discount_message_id] int,
    [email_message_id] int,
    [insert_date] datetime NOT NULL,
    [process_date] datetime,
    [sent_count] int,
    [status] int
,
    PRIMARY KEY ([email_cart_order_id])
);
CREATE TABLE [dbo].[email_cart_order_message] (
    [email_cart_order_message_id] int IDENTITY(1,1) NOT NULL,
    [email_cart_order_id] int,
    [email_message_id] int
,
    PRIMARY KEY ([email_cart_order_message_id])
);
CREATE TABLE [dbo].[email_domain_type] (
    [email_domain_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [email_domain_type_name] varchar(50) NOT NULL,
    [email_domain_type_description] varchar(500) NOT NULL
,
    PRIMARY KEY ([email_domain_type_id])
);
CREATE TABLE [dbo].[email_log] (
    [email_log_id] int IDENTITY(1,1) NOT NULL,
    [email_address] varchar(100) NOT NULL,
    [email_type] varchar(50) NOT NULL,
    [email_parameters] varchar(500) NOT NULL,
    [email_body] nvarchar(MAX) NOT NULL,
    [sent_date] datetime NOT NULL,
    [sent_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([email_log_id])
);
CREATE TABLE [dbo].[email_message] (
    [email_message_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [message_campaign_id] smallint NOT NULL,
    [email_message_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime,
    [order_header_id] int
,
    PRIMARY KEY ([email_message_id])
);
CREATE TABLE [dbo].[email_message_html] (
    [email_message_html_id] int IDENTITY(1,1) NOT NULL,
    [email_message_id] int NOT NULL,
    [customer_email] varchar(100),
    [email_message_body] nvarchar(MAX) NOT NULL
,
    PRIMARY KEY ([email_message_html_id])
);
CREATE TABLE [dbo].[email_message_html_load] (
    [email_message_id] int IDENTITY(1,1) NOT NULL,
    [customer_email] varchar(100),
    [email_message_body] nvarchar(MAX) NOT NULL,
    [loaded] bit NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[email_message_status] (
    [email_message_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [email_message_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([email_message_status_id])
);
CREATE TABLE [dbo].[email_message_transactional_value] (
    [email_message_transactional_value_id] int NOT NULL,
    [email_message_transactional_value_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([email_message_transactional_value_id])
);
CREATE TABLE [dbo].[email_registration_insert] (
    [email_registration_insert_id] int IDENTITY(1,1) NOT NULL,
    [keycode] varchar(40),
    [opt_in] int,
    [customer_email] varchar(255),
    [last_modified_date] datetime,
    [license_id] int NOT NULL,
    [customer_id] int,
    [insert_date] datetime,
    [processed] int
,
    PRIMARY KEY ([email_registration_insert_id])
);
CREATE TABLE [dbo].[email_registration_insert_email] (
    [email_registration_insert_email_id] int IDENTITY(1,1) NOT NULL,
    [email_registration_insert_id] int,
    [opt_in] int,
    [customer_email] varchar(255)
);
CREATE TABLE [dbo].[email_unregister] (
    [email_unregister_id] int IDENTITY(1,1) NOT NULL,
    [customer_email] varchar(150),
    [processed] int,
    [insert_date] datetime
);
CREATE TABLE [dbo].[ent_product_product_update] (
    [product_id] int NOT NULL,
    [product_version] varchar(20) NOT NULL,
    [ent_product_update_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([ent_product_update_id], [product_id], [product_version])
);
CREATE TABLE [dbo].[ent_product_update] (
    [ent_product_update_id] int IDENTITY(1,1) NOT NULL,
    [ent_update_type_id] int NOT NULL,
    [update_sequence_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([ent_product_update_id])
);
CREATE TABLE [dbo].[ent_product_update_file] (
    [ent_product_update_file_id] int IDENTITY(1,1) NOT NULL,
    [ent_product_update_id] int NOT NULL,
    [filepath] varchar(1024) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([ent_product_update_file_id])
);
CREATE TABLE [dbo].[ent_product_update_property] (
    [ent_product_update_property_id] int IDENTITY(1,1) NOT NULL,
    [ent_product_update_id] int NOT NULL,
    [ent_property_id] int NOT NULL,
    [property_value] nvarchar(448),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([ent_product_update_property_id])
);
CREATE TABLE [dbo].[ent_property_obsolete] (
    [ent_property_id] int NOT NULL,
    [ent_property_description] varchar(64) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([ent_property_id])
);
CREATE TABLE [dbo].[ent_stopcode] (
    [ent_stopcode_id] int IDENTITY(1,1) NOT NULL,
    [stop_update_sequence_id] int NOT NULL,
    [product_id] int,
    [product_version] varchar(20),
    [stopcode_description] varchar(1024) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([ent_stopcode_id])
);
CREATE TABLE [dbo].[ent_update_type_obsolete] (
    [ent_update_type_id] int NOT NULL,
    [ent_update_type_description] varchar(128) NOT NULL,
    [tag] varchar(16),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([ent_update_type_id])
);
CREATE TABLE [dbo].[enterprise_lead] (
    [id] int IDENTITY(1,1) NOT NULL,
    [first_name] varchar(30) NOT NULL,
    [last_name] varchar(30) NOT NULL,
    [title] varchar(40),
    [company] varchar(40) NOT NULL,
    [address1] varchar(100),
    [address2] varchar(100),
    [country] varchar(100) NOT NULL,
    [state] varchar(50),
    [city] varchar(50),
    [zip] varchar(50) NOT NULL,
    [phone] varchar(20) NOT NULL,
    [email] varchar(100) NOT NULL,
    [num_computers] smallint,
    [applied_date] datetime NOT NULL,
    [trial_requested] numeric(3,0) NOT NULL,
    [comments] text,
    [heard] smallint
,
    PRIMARY KEY ([id])
);
CREATE TABLE [dbo].[environment] (
    [environment_id] int NOT NULL,
    [environment_type] varchar(50) NOT NULL,
    [keycode_modification_string] varchar(4) NOT NULL
,
    PRIMARY KEY ([environment_id])
);
CREATE TABLE [dbo].[epicor_Epicor2WR_OrderQueue] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [row_id] int NOT NULL,
    [invoice_code] varchar(16),
    [order_no] int NOT NULL,
    [ext] int NOT NULL,
    [type] char(1) NOT NULL,
    [cust_code] varchar(13) NOT NULL,
    [date_entered] datetime NOT NULL,
    [cust_po] varchar(20),
    [curr_key] varchar(10),
    [total_amt_order] numeric(20,8),
    [tot_ord_tax] numeric(20,8),
    [tot_ord_disc] numeric(20,8),
    [tot_ord_freight] numeric(20,8),
    [tax_id] varchar(10),
    [line_no] int NOT NULL,
    [part_no] varchar(30),
    [location] varchar(10),
    [orderEd] numeric(20,8),
    [uom] char(2),
    [price] numeric(20,8),
    [discount_pct] numeric(20,8),
    [total_tax] numeric(20,8),
    [tax_code] varchar(10),
    [EmailDownload] varchar(64),
    [EmailReceipt] varchar(64),
    [ShipToName] varchar(40),
    [ShipToAddr1] varchar(40),
    [ShipToAddr2] varchar(40),
    [ShipToCity] varchar(40),
    [ShipToState] varchar(2),
    [ShipToZip] varchar(10),
    [ShipToCountry] varchar(40),
    [contact_name] varchar(40)
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[epicor_Epicor2WR_OrderQueue_bk] (
    [ID] int NOT NULL,
    [row_id] int NOT NULL,
    [invoice_code] varchar(16),
    [order_no] int NOT NULL,
    [ext] int NOT NULL,
    [type] char(1) NOT NULL,
    [cust_code] varchar(13) NOT NULL,
    [date_entered] datetime NOT NULL,
    [cust_po] varchar(20),
    [curr_key] varchar(10),
    [total_amt_order] numeric(20,8),
    [tot_ord_tax] numeric(20,8),
    [tot_ord_disc] numeric(20,8),
    [tot_ord_freight] numeric(20,8),
    [tax_id] varchar(10),
    [line_no] int NOT NULL,
    [part_no] varchar(30),
    [location] varchar(10),
    [orderEd] numeric(20,8),
    [uom] char(2),
    [price] numeric(20,8),
    [discount_pct] numeric(20,8),
    [total_tax] numeric(20,8),
    [tax_code] varchar(10),
    [EmailDownload] varchar(64),
    [EmailReceipt] varchar(64),
    [ShipToName] varchar(40),
    [ShipToAddr1] varchar(40),
    [ShipToAddr2] varchar(40),
    [ShipToCity] varchar(40),
    [ShipToState] varchar(2),
    [ShipToZip] varchar(10),
    [ShipToCountry] varchar(40),
    [contact_name] varchar(40)
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[epicor_Epicor2WR_PaymentQueue] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [row_id] int NOT NULL,
    [invoice_code] varchar(16),
    [order_no] int,
    [ext] int,
    [doc_ctrl_num] varchar(16),
    [payment_code] varchar(8),
    [amt_payment] numeric(20,8),
    [CC] varchar(30),
    [CC_exp] varchar(30),
    [card_name] varchar(30),
    [prompt4_inp] varchar(30)
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[epicor_Epicor2WR_PaymentQueue_bk] (
    [ID] int NOT NULL,
    [row_id] int NOT NULL,
    [invoice_code] varchar(16),
    [order_no] int,
    [ext] int,
    [doc_ctrl_num] varchar(16),
    [payment_code] varchar(8),
    [amt_payment] numeric(20,8),
    [CC] varchar(30),
    [CC_exp] varchar(30),
    [card_name] varchar(30),
    [prompt4_inp] varchar(30)
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[epicor_ProcessFlag] (
    [Value] int NOT NULL,
    [DateInserted] smalldatetime NOT NULL,
    [Description] varchar(100) NOT NULL,
    [UpdatedBy] varchar(50)
,
    PRIMARY KEY ([Value])
);
CREATE TABLE [dbo].[epicor_ProcessFlagNext] (
    [Seq] int,
    [Value] int NOT NULL,
    [NextValue] int NOT NULL,
    [Process] varchar(20),
    [FromDesc] varchar(100),
    [ToDesc] varchar(100),
    [DateInserted] smalldatetime NOT NULL,
    [UpdatedBy] varchar(50)
,
    PRIMARY KEY ([NextValue], [Value])
);
CREATE TABLE [dbo].[epicor_WR2Epicor_Queue] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Invoice_Code] varchar(16) NOT NULL,
    [Line_Item] int NOT NULL,
    [Customer_Code] varchar(16) NOT NULL,
    [Purchased_Date] datetime NOT NULL,
    [Currency] varchar(5) NOT NULL,
    [Payment_Method_ID] smallint NOT NULL,
    [Product_ID] int NOT NULL,
    [Entered_Timestamp] datetime NOT NULL,
    [Quantity] int NOT NULL,
    [Extended_Price] decimal(10,4) NOT NULL,
    [Tax] decimal(9,2) NOT NULL,
    [Auth_Batch_ID] datetime NOT NULL,
    [Process_Flag] int NOT NULL,
    [QueueDate] datetime NOT NULL,
    [effective_date] datetime,
    [license_expiration_date] datetime
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[epicor_WR2Epicor_Queue_bk] (
    [ID] int NOT NULL,
    [Invoice_Code] varchar(16) NOT NULL,
    [Line_Item] int NOT NULL,
    [Customer_Code] varchar(16) NOT NULL,
    [Purchased_Date] datetime NOT NULL,
    [Currency] varchar(5) NOT NULL,
    [Payment_Method_ID] smallint NOT NULL,
    [Product_ID] int NOT NULL,
    [Entered_Timestamp] datetime NOT NULL,
    [Quantity] int NOT NULL,
    [Extended_Price] decimal(10,4) NOT NULL,
    [Tax] decimal(9,2) NOT NULL,
    [Auth_Batch_ID] datetime,
    [QueueDate] datetime NOT NULL,
    [ArchiveDate] datetime NOT NULL,
    [effective_date] datetime,
    [license_expiration_date] datetime
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[exact_target_batch] (
    [exact_target_batch_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [customer_id] int,
    [cart_discount_id] int,
    [exact_target_master_id] int NOT NULL,
    [exact_target_batch_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [mail_date] datetime,
    [email_type] varchar(30),
    [license_category_id] int
,
    PRIMARY KEY ([exact_target_batch_id])
);
CREATE TABLE [dbo].[exact_target_batch_children] (
    [exact_target_batch_children_id] int IDENTITY(1,1) NOT NULL,
    [exact_target_batch_id] int,
    [exact_target_master_id] int NOT NULL,
    [parent_license_id] int NOT NULL,
    [child_license_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_expiration_date] datetime NOT NULL,
    [mail_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([exact_target_batch_children_id])
);
CREATE TABLE [dbo].[exact_target_class] (
    [exact_target_class_id] tinyint IDENTITY(1,1) NOT NULL,
    [exact_target_class] varchar(50) NOT NULL
,
    PRIMARY KEY ([exact_target_class_id])
);
CREATE TABLE [dbo].[exact_target_file_prefix] (
    [exact_target_file_prefix_id] tinyint IDENTITY(1,1) NOT NULL,
    [exact_target_file_prefix] varchar(50) NOT NULL
,
    PRIMARY KEY ([exact_target_file_prefix_id])
);
CREATE TABLE [dbo].[exact_target_invalid_email] (
    [exact_target_invalid_email_id] int IDENTITY(1,1) NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [customer_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([exact_target_invalid_email_id])
);
CREATE TABLE [dbo].[exact_target_license_activation] (
    [exact_target_license_activation_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [capability_activation_date] datetime
);
CREATE TABLE [dbo].[exact_target_license_category_upgrade] (
    [exact_target_license_category_upgrade_id] tinyint IDENTITY(1,1) NOT NULL,
    [license_category_id] int NOT NULL,
    [license_category_upgrade_id] int NOT NULL
,
    PRIMARY KEY ([exact_target_license_category_upgrade_id])
);
CREATE TABLE [dbo].[exact_target_master] (
    [exact_target_master_id] int IDENTITY(1,1) NOT NULL,
    [exact_target_message_campaign_id] int NOT NULL,
    [message_campaign_id] int NOT NULL
,
    PRIMARY KEY ([exact_target_master_id])
);
CREATE TABLE [dbo].[exact_target_message_campaign] (
    [exact_target_message_campaign_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_exact_target_name] varchar(50),
    [exact_target_class_id] int,
    [exact_target_file_prefix_id] int,
    [email_id] int,
    [output_import] decimal(5,4),
    [import_sends] decimal(5,4),
    [message_service_type_id] int
,
    PRIMARY KEY ([exact_target_message_campaign_id])
);
CREATE TABLE [dbo].[exact_target_subscriber] (
    [exact_target_subscriber_id] int IDENTITY(1,1) NOT NULL,
    [member_id] int,
    [subscriber_key] varchar(255),
    [subscriber_id] int,
    [email_id] int,
    [campaign_name] varchar(255),
    [customer_email] varchar(255),
    [transaction_time] datetime,
    [bounce_category] varchar(255),
    [bounce_sub_category] varchar(255),
    [job_id] int,
    [list_id] int,
    [batch_id] int,
    [activity] varchar(50),
    [unsub_reason] varchar(255),
    [held_date] varchar(255),
    [license_id] int
);
CREATE TABLE [dbo].[exact_target_subscriber_archive] (
    [exact_target_subscriber_archive_id] int IDENTITY(1,1) NOT NULL,
    [member_id] int,
    [subscriber_key] varchar(255),
    [subscriber_id] int,
    [email_id] int,
    [campaign_name] varchar(255),
    [customer_email] varchar(255),
    [transaction_time] datetime,
    [bounce_category] varchar(255),
    [bounce_sub_category] varchar(255),
    [job_id] int,
    [list_id] int,
    [batch_id] int,
    [activity] varchar(50),
    [unsub_reason] varchar(255),
    [held_date] varchar(255),
    [license_id] int
);
CREATE TABLE [dbo].[exact_target_subset1] (
    [exact_target_master_id] int NOT NULL,
    [license_id] int NOT NULL,
    [cart_discount_id] int,
    [license_distribution_method_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [product_line_id] int NOT NULL,
    [opt_in] int NOT NULL,
    [autorenewal_opt_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [license_seats] int NOT NULL
);
CREATE TABLE [dbo].[exact_target_subset2] (
    [exact_target_master_id] int NOT NULL,
    [license_id] int NOT NULL,
    [cart_discount_id] int,
    [license_distribution_method_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [product_line_id] int NOT NULL,
    [opt_in] int NOT NULL,
    [autorenewal_opt_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [license_seats] int NOT NULL
);
CREATE TABLE [dbo].[exact_target_subset3] (
    [exact_target_master_id] int NOT NULL,
    [license_id] int NOT NULL,
    [cart_discount_id] int,
    [license_distribution_method_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [product_line_id] int NOT NULL,
    [opt_in] int NOT NULL,
    [autorenewal_opt_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [license_seats] int NOT NULL
);
CREATE TABLE [dbo].[external_order_partner] (
    [external_order_partner_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [insert_date] datetime
,
    PRIMARY KEY ([external_order_partner_id])
);
CREATE TABLE [dbo].[extole_update] (
    [extole_update_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [extole_referral_id] varchar(50),
    [process_date] datetime,
    [update_attempts] tinyint NOT NULL,
    [extole_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([extole_update_id])
);
CREATE TABLE [dbo].[extole_update_archive] (
    [extole_update_archive_id] int IDENTITY(1,1) NOT NULL,
    [extole_update_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [extole_referral_id] varchar(50),
    [process_date] datetime,
    [update_attempts] tinyint NOT NULL,
    [extole_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([extole_update_archive_id])
);
CREATE TABLE [dbo].[extole_update_status_obsolete] (
    [extole_update_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [extole_update_status_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([extole_update_status_id])
);
CREATE TABLE [dbo].[finance_fid] (
    [fid] int,
    [source_table_id] int,
    [source_table_name] nvarchar(100),
    [billing_partner] nvarchar(100),
    [partner_id] nvarchar(150),
    [insert_date] datetime,
    [modified_date] datetime,
    [company_id] int,
    [customer_id] int,
    [vendor_customer_code] varchar(100)
);
CREATE TABLE [dbo].[forecast_calc] (
    [forecast_calc_id] int IDENTITY(1,1) NOT NULL,
    [forecast_core_id] int NOT NULL,
    [forecast_usage_id] int,
    [license_id] int NOT NULL,
    [license_category_id] int,
    [period] int NOT NULL,
    [growth_rate] decimal(18,2) NOT NULL,
    [forecast_start_date] date NOT NULL,
    [forecast_end_date] date NOT NULL,
    [forecast_type] varchar(50) NOT NULL,
    [forecast_usage] decimal(18,4),
    [forescast_usage_delta] decimal(18,4),
    [forecast_sold_quantity_total] decimal(18,4),
    [acv_sold] money,
    [acv_new] money,
    [acv_cancel_new] money,
    [acv_renew] money,
    [acv_cancel_renew] money,
    [acv_total] money,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([forecast_calc_id])
);
CREATE TABLE [dbo].[forecast_calc_archive] (
    [forecast_calc_archive_id] int IDENTITY(1,1) NOT NULL,
    [forecast_calc_id] int NOT NULL,
    [forecast_core_id] int NOT NULL,
    [forecast_usage_id] int,
    [license_id] int NOT NULL,
    [license_category_id] int,
    [period] int NOT NULL,
    [growth_rate] decimal(18,2) NOT NULL,
    [forecast_start_date] date NOT NULL,
    [forecast_end_date] date NOT NULL,
    [forecast_type] varchar(50) NOT NULL,
    [forecast_usage] decimal(18,4),
    [forescast_usage_delta] decimal(18,4),
    [forecast_sold_quantity_total] decimal(18,4),
    [acv_sold] money,
    [acv_new] money,
    [acv_cancel_new] money,
    [acv_renew] money,
    [acv_cancel_renew] money,
    [acv_total] money,
    [insert_date] datetime NOT NULL,
    [archive_date] datetime NOT NULL,
    [archive_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([forecast_calc_archive_id])
);
CREATE TABLE [dbo].[forecast_core] (
    [forecast_core_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] int,
    [license_seat_count] int NOT NULL,
    [sfdc_opportunity_id] varchar(18) NOT NULL,
    [sfdc_opportunity_line_id] varchar(18) NOT NULL,
    [order_item_id] int NOT NULL,
    [order_item_type] varchar(50) NOT NULL,
    [order_item_eff_date] date NOT NULL,
    [order_item_exp_date] date NOT NULL,
    [order_term_in_months] decimal(18,4),
    [order_item_quantity] decimal(18,4) NOT NULL,
    [order_item_total] money NOT NULL,
    [model] varchar(50) NOT NULL,
    [price] money NOT NULL,
    [model_start_date] date NOT NULL,
    [model_end_date] date NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([forecast_core_id])
);
CREATE TABLE [dbo].[forecast_growth_rate] (
    [forecast_growth_rate_id] int IDENTITY(1,1) NOT NULL,
    [growth_rate] decimal(18,2) NOT NULL,
    [growth_rate_active] bit NOT NULL
,
    PRIMARY KEY ([forecast_growth_rate_id])
);
CREATE TABLE [dbo].[forecast_order_exclusions] (
    [sfdc_oppline_id] varchar(50) NOT NULL,
    [sfdc_opp_id] varchar(50) NOT NULL,
    [order_item_id] int
);
CREATE TABLE [dbo].[forecast_usage] (
    [forecast_usage_id] int IDENTITY(1,1) NOT NULL,
    [forecast_core_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] int,
    [order_license_usage_id] int,
    [usage_start_date] date NOT NULL,
    [usage_end_date] date NOT NULL,
    [price] money NOT NULL,
    [model] varchar(50) NOT NULL,
    [usage] decimal(18,4) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([forecast_usage_id])
);
CREATE TABLE [dbo].[forecast_usage_archive] (
    [forecast_usage_archive_id] int IDENTITY(1,1) NOT NULL,
    [forecast_usage_id] int NOT NULL,
    [forecast_core_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] int,
    [order_license_usage_id] int,
    [usage_start_date] date NOT NULL,
    [usage_end_date] date NOT NULL,
    [price] money NOT NULL,
    [model] varchar(50) NOT NULL,
    [usage] decimal(18,4) NOT NULL,
    [insert_date] datetime NOT NULL,
    [archive_date] datetime NOT NULL,
    [archive_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([forecast_usage_archive_id])
);
CREATE TABLE [dbo].[form] (
    [form_id] int IDENTITY(1,1) NOT NULL,
    [form_name] varchar(50) NOT NULL,
    [form_type_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([form_id])
);
CREATE TABLE [dbo].[form_field] (
    [form_field_id] int IDENTITY(1,1) NOT NULL,
    [field_name] varchar(50) NOT NULL,
    [field_data_type] varchar(20),
    [field_name_alias] varchar(50)
,
    PRIMARY KEY ([form_field_id])
);
CREATE TABLE [dbo].[form_field_form] (
    [form_field_form_id] int IDENTITY(1,1) NOT NULL,
    [form_id] int NOT NULL,
    [form_field_id] int NOT NULL,
    [language_code] varchar(2),
    [required] varchar(10)
,
    PRIMARY KEY ([form_field_form_id])
);
CREATE TABLE [dbo].[form_response] (
    [form_response_id] int IDENTITY(1,1) NOT NULL,
    [form_submit_id] int NOT NULL,
    [form_response_key] uniqueidentifier NOT NULL,
    [form_response] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([form_response_id])
);
CREATE TABLE [dbo].[form_submit] (
    [form_submit_id] int IDENTITY(1,1) NOT NULL,
    [form_id] int NOT NULL,
    [ip_address] varchar(24) NOT NULL,
    [form_url] varchar(500) NOT NULL,
    [form_json] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([form_submit_id])
);
CREATE TABLE [dbo].[form_submit_customer] (
    [form_submit_customer_id] int IDENTITY(1,1) NOT NULL,
    [form_submit_id] int NOT NULL,
    [customer_id] int NOT NULL
,
    PRIMARY KEY ([form_submit_customer_id])
);
CREATE TABLE [dbo].[form_submit_value] (
    [form_submit_value_id] int IDENTITY(1,1) NOT NULL,
    [form_submit_id] int NOT NULL,
    [form_field_id] int NOT NULL,
    [field_value] nvarchar(500)
,
    PRIMARY KEY ([form_submit_value_id])
);
CREATE TABLE [dbo].[form_type] (
    [form_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [form_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([form_type_id])
);
CREATE TABLE [dbo].[fraud] (
    [id] smallint IDENTITY(1,1) NOT NULL,
    [last_name] varchar(32),
    [email] varchar(64),
    [ipaddr] varchar(50),
    [city] varchar(32),
    [state_id] char(2),
    [country_id] int,
    [fraudid] int
,
    PRIMARY KEY ([id])
);
CREATE TABLE [dbo].[g_temp_Tid] (
    [row_id] int IDENTITY(1,1) NOT NULL,
    [pymnt_hrd_id] int,
    [json_resp_msg] varchar(MAX)
);
CREATE TABLE [dbo].[geocode_request] (
    [geocode_request_id] int IDENTITY(1,1) NOT NULL,
    [geocode_type_id] int NOT NULL,
    [request] nvarchar(2000) NOT NULL,
    [request_date] datetime NOT NULL,
    [response] nvarchar(MAX),
    [response_date] datetime,
    [response_expiration_date] datetime,
    [response_cleared_date] datetime,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([geocode_request_id])
);
CREATE TABLE [dbo].[geocode_result] (
    [geocode_result_id] int IDENTITY(1,1) NOT NULL,
    [geocode_request_id] int NOT NULL,
    [formatted_address] nvarchar(1000),
    [location] geography,
    [location_type] varchar(50),
    [partial_match] bit,
    [google_place_id] varchar(60),
    [plus_code] varchar(10),
    [expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [result_cleared_date] datetime,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([geocode_result_id])
);
CREATE TABLE [dbo].[geocode_result_address_component] (
    [geocode_result_address_component_id] int IDENTITY(1,1) NOT NULL,
    [geocode_result_id] int NOT NULL,
    [address_component_id] int NOT NULL,
    [short_value] nvarchar(255),
    [long_value] nvarchar(255)
,
    PRIMARY KEY ([geocode_result_address_component_id])
);
CREATE TABLE [dbo].[geocode_type] (
    [geocode_type_id] int IDENTITY(1,1) NOT NULL,
    [geocode_type_name] varchar(50) NOT NULL,
    [expiration_days] int,
    [clear_expired_response] bit NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([geocode_type_id])
);
CREATE TABLE [dbo].[google_continue_token] (
    [google_continue_token] varchar(50) NOT NULL,
    [last_modified_date] datetime NOT NULL
);
CREATE TABLE [dbo].[google_notification_type_obsolete] (
    [google_notification_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [google_notification_type_name] varchar(100) NOT NULL
,
    PRIMARY KEY ([google_notification_type_id])
);
CREATE TABLE [dbo].[google_notifications] (
    [google_notification_id] int IDENTITY(1,1) NOT NULL,
    [license_message_id] int,
    [notification_string] varchar(MAX) NOT NULL,
    [transaction_id] varchar(100),
    [purchase_date_utc] datetime,
    [expiration_date_utc] datetime,
    [cancellation_date_utc] datetime,
    [cancel_reason] tinyint,
    [auto_renewing] bit,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([google_notification_id])
);
CREATE TABLE [dbo].[google_notifications_history] (
    [google_notification_history_id] int IDENTITY(1,1) NOT NULL,
    [google_notification_id] int NOT NULL,
    [license_message_id] int,
    [notification_string] varchar(MAX) NOT NULL,
    [transaction_id] varchar(100),
    [purchase_date_utc] datetime,
    [expiration_date_utc] datetime,
    [cancellation_date_utc] datetime,
    [cancel_reason] tinyint,
    [auto_renewing] bit,
    [insert_date] datetime NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([google_notification_history_id])
);
CREATE TABLE [dbo].[google_order] (
    [google_order_id] int IDENTITY(1,1) NOT NULL,
    [google_order_number] varchar(100) NOT NULL,
    [keycode] varchar(40),
    [guid] varchar(50),
    [order_status_id] int,
    [product] varchar(50),
    [order_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL,
    [processed] int
,
    PRIMARY KEY ([google_order_id])
);
CREATE TABLE [dbo].[google_order_notification] (
    [google_order_notification_id] int IDENTITY(1,1) NOT NULL,
    [order_notification_xml] varchar(MAX) NOT NULL,
    [google_notification_type_id] tinyint NOT NULL,
    [google_order_number] varchar(100),
    [process_date] datetime NOT NULL,
    [processed] int,
    [Serial_number] varchar(40),
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([google_order_notification_id])
);
CREATE TABLE [dbo].[google_order_notification_parking] (
    [google_order_notification_parking_id] int IDENTITY(1,1) NOT NULL,
    [order_notification_xml] varchar(MAX) NOT NULL,
    [google_notification_type_id] tinyint NOT NULL,
    [google_order_number] varchar(100),
    [process_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([google_order_notification_parking_id])
);
CREATE TABLE [dbo].[google_product] (
    [google_product_id] int IDENTITY(1,1) NOT NULL,
    [google_product] varchar(50) NOT NULL,
    [product_id] int NOT NULL
,
    PRIMARY KEY ([google_product_id])
);
CREATE TABLE [dbo].[goognec_order] (
    [google_order_id] bigint,
    [order_creation_date] varchar(100),
    [product_name] varchar(100),
    [financial_status] varchar(100),
    [fulfillment_status] varchar(50),
    [sale_currency] varchar(50),
    [price] decimal(12,2),
    [tax] decimal(12,2),
    [order_amount] decimal(12,2),
    [amount_refunded] decimal(12,2),
    [amount_charged_back] decimal(12,2),
    [chargeback_protection] varchar(50),
    [country] varchar(50),
    [merchant_order_id] varchar(100)
);
CREATE TABLE [dbo].[gsmc_migration] (
    [gsmc_migration_id] int IDENTITY(1,1) NOT NULL,
    [marid] int,
    [marname] varchar(MAX),
    [keycode] varchar(255),
    [lic_id] int,
    [lic_type] varchar(255),
    [uber_id] int,
    [user_email] varchar(255),
    [domain] varchar(50),
    [last_login] datetime,
    [active_non_mobile] int,
    [active_mobile] int,
    [license_id] int,
    [gsmc_status_id] tinyint,
    [gsmc_status_reason] varchar(255),
    [batch_number] tinyint,
    [sfdc_account_id] varchar(20),
    [sfdc_account_name] varchar(255),
    [sfdc_geo] varchar(10),
    [reseller_id] varchar(24),
    [distributor_id] varchar(24),
    [insert_date] datetime,
    [last_modified_date] datetime
);
CREATE TABLE [dbo].[gsmc_migration_error] (
    [gsmc_migration_error_id] int IDENTITY(1,1) NOT NULL,
    [keycode] varchar(24),
    [product_id] int,
    [seats] int,
    [start_date] datetime,
    [end_date] datetime,
    [customer_email] varchar(100),
    [license_distribution_method_id] int,
    [insert_date] datetime
);
CREATE TABLE [dbo].[gsmc_status] (
    [gsmc_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [gsmc_status_description] varchar(100) NOT NULL
,
    PRIMARY KEY ([gsmc_status_id])
);
CREATE TABLE [dbo].[ids] (
    [id_type] int NOT NULL,
    [next_id] int NOT NULL,
    [description] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([id_type])
);
CREATE TABLE [dbo].[incomm_license_status] (
    [incomm_license_status_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [serial_number] varchar(20),
    [barcode] varchar(20),
    [merchant_name] varchar(50),
    [incomm_product_status_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime,
    [modified_by] varchar(200),
    [update_attempts] tinyint,
    [process_date] datetime
,
    PRIMARY KEY ([incomm_license_status_id])
);
CREATE TABLE [dbo].[incomm_license_status_history] (
    [incomm_license_status_history_id] int IDENTITY(1,1) NOT NULL,
    [incomm_license_status_id] int NOT NULL,
    [license_id] int NOT NULL,
    [serial_number] varchar(20),
    [barcode] varchar(20),
    [merchant_name] varchar(50),
    [incomm_product_status_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime,
    [modified_by] varchar(200),
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL,
    [update_attempts] tinyint,
    [process_date] datetime
,
    PRIMARY KEY ([incomm_license_status_history_id])
);
CREATE TABLE [dbo].[incomm_log] (
    [incomm_log_id] int IDENTITY(1,1) NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [transaction_reference] varchar(64) NOT NULL,
    [merchant_name] varchar(50),
    [request_action] varchar(50),
    [message] varchar(200),
    [source] varchar(32),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [redeem_timeout] tinyint
,
    PRIMARY KEY ([incomm_log_id])
);
CREATE TABLE [dbo].[incomm_product_status] (
    [incomm_product_status_id] int NOT NULL,
    [incomm_product_status_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([incomm_product_status_id])
);
CREATE TABLE [dbo].[incomm_response] (
    [incomm_response_id] int IDENTITY(1,1) NOT NULL,
    [response_incomm_code] int,
    [response_message] varchar(50),
    [face_value] money,
    [upc] varchar(30),
    [store_number] varchar(20),
    [merchant] varchar(50),
    [pin] varchar(20),
    [partner_name] varchar(20),
    [serial_number] varchar(20),
    [authorization_code] varchar(20),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[invoice_in_process] (
    [invoice_in_process_id] int IDENTITY(1,1) NOT NULL,
    [invoice_code] varchar(16) NOT NULL,
    [customer_code] varchar(16) NOT NULL,
    [affiliate_code] varchar(16) NOT NULL,
    [affiliate_subcode] varchar(32),
    [master_override_affiliate_num] int,
    [employee_id] int,
    [promo_code] varchar(32),
    [trial_product_id] int,
    [wcode1] varchar(32),
    [wcode2] varchar(32),
    [wcode3] varchar(32),
    [distribution_channel_code] varchar(32),
    [distribution_role_code] varchar(32),
    [cd_code] varchar(32),
    [pop_under] varchar(32),
    [page_design_1] varchar(32),
    [page_design_2] varchar(32),
    [page_design_3] varchar(32),
    [referral_source_id] smallint,
    [user_ip] varchar(16),
    [order_type_id] smallint NOT NULL,
    [amount] decimal(9,2) NOT NULL,
    [purchased_date] datetime NOT NULL,
    [modified_date] datetime,
    [payment_method_id] smallint NOT NULL,
    [payment_authorization_id] smallint,
    [invoice_status_id] smallint NOT NULL,
    [completed_ship_date] datetime,
    [alt_invoice_code] varchar(24),
    [visitorid] int,
    [coupon_amount] money,
    [last_modified] datetime NOT NULL,
    [tax_total] decimal(15,2),
    [p_rc] varchar(12),
    [p_rsc] varchar(64),
    [p_ac] varchar(12),
    [trx_rc] varchar(12),
    [trx_rsc] varchar(64),
    [trx_ac] varchar(12),
    [coupon_code] varchar(7),
    [ein] int,
    [separate_shipping] int,
    [carttype] varchar(30),
    [carttype_id] int
,
    PRIMARY KEY ([invoice_in_process_id])
);
CREATE TABLE [dbo].[invoice_item_in_process] (
    [invoice_item_in_process_id] int IDENTITY(1,1) NOT NULL,
    [invoice_in_process_id] int NOT NULL,
    [invoice_code] varchar(16),
    [line_item] int,
    [product_id] int,
    [product_version] varchar(20),
    [quantity] int NOT NULL,
    [extended_price] decimal(10,4) NOT NULL,
    [entered_timestamp] datetime NOT NULL,
    [previous_version] varchar(20),
    [is_update] smallint,
    [cross_sell] tinyint,
    [shipping_status] tinyint,
    [shipped_date] datetime,
    [serial_number] varchar(50),
    [refund_reason_code] smallint,
    [auth_trans_id] varchar(40),
    [auth_batch_id] datetime,
    [deferred_income] money,
    [last_modified] datetime NOT NULL,
    [tax_item_amount] decimal(15,2),
    [IsKeycodeValid] bit,
    [full_retail_price] decimal(10,4),
    [special_code] varchar(12),
    [effective_date] datetime,
    [extra_years] int,
    [extra_days] int,
    [override_unit_price] money,
    [cartKey] varchar(12),
    [final_product_id] int,
    [capability] int
,
    PRIMARY KEY ([invoice_item_in_process_id])
);
CREATE TABLE [dbo].[Invoice_Items] (
    [invoice_code] varchar(16) NOT NULL,
    [line_item] int NOT NULL,
    [product_id] int NOT NULL,
    [product_version] varchar(20),
    [quantity] int NOT NULL,
    [extended_price] decimal(10,4) NOT NULL,
    [entered_timestamp] datetime NOT NULL,
    [previous_version] varchar(20),
    [is_update] smallint,
    [cross_sell] tinyint,
    [shipping_status] tinyint,
    [shipped_date] datetime,
    [serial_number] varchar(50),
    [refund_reason_code] smallint,
    [auth_trans_id] varchar(40),
    [auth_batch_id] datetime,
    [deferred_income] money,
    [last_modified] datetime NOT NULL,
    [tax_item_amount] decimal(15,2),
    [IsKeycodeValid] bit,
    [full_retail_price] decimal(10,4),
    [special_code] varchar(12),
    [effective_date] datetime
,
    PRIMARY KEY ([invoice_code], [line_item], [product_id])
);
CREATE TABLE [dbo].[invoice_items_deleted] (
    [invoice_code] varchar(16) NOT NULL,
    [line_item] int NOT NULL,
    [product_id] int NOT NULL,
    [product_version] varchar(20),
    [quantity] int NOT NULL,
    [extended_price] decimal(10,4) NOT NULL,
    [entered_timestamp] datetime NOT NULL,
    [previous_version] varchar(20),
    [is_update] smallint,
    [cross_sell] tinyint,
    [shipping_status] tinyint,
    [shipped_date] datetime,
    [serial_number] varchar(50),
    [refund_reason_code] smallint,
    [auth_trans_id] varchar(26),
    [auth_batch_id] datetime,
    [deferred_income] money,
    [last_modified] datetime NOT NULL,
    [tax_item_amount] decimal(15,2)
);
CREATE TABLE [dbo].[invoice_itemsAudit] (
    [invoice_code] varchar(16) NOT NULL,
    [line_item] int NOT NULL,
    [product_id] int NOT NULL,
    [product_version] varchar(20),
    [quantity] int NOT NULL,
    [extended_price] decimal(10,4) NOT NULL,
    [entered_timestamp] datetime NOT NULL,
    [previous_version] varchar(20),
    [is_update] smallint,
    [cross_sell] tinyint,
    [shipping_status] tinyint,
    [shipped_date] datetime,
    [serial_number] varchar(50),
    [refund_reason_code] smallint,
    [auth_trans_id] varchar(30),
    [auth_batch_id] datetime,
    [deferred_income] money,
    [last_modified] datetime NOT NULL,
    [tax_item_amount] decimal(15,2),
    [AuditTimestamp] datetime,
    [AuditSystemUser] varchar(50),
    [IsKeycodeValid] bit,
    [full_retail_price] decimal(10,4),
    [special_code] varchar(12),
    [effective_date] datetime
);
CREATE TABLE [dbo].[invoices] (
    [invoice_code] varchar(16) NOT NULL,
    [customer_code] varchar(16) NOT NULL,
    [affiliate_code] varchar(16) NOT NULL,
    [affiliate_subcode] varchar(32),
    [master_override_affiliate_num] int,
    [employee_id] int,
    [promo_code] varchar(32),
    [trial_product_id] int,
    [wcode1] varchar(32),
    [wcode2] varchar(32),
    [wcode3] varchar(32),
    [distribution_channel_code] varchar(32),
    [distribution_role_code] varchar(32),
    [cd_code] varchar(32),
    [pop_under] varchar(32),
    [page_design_1] varchar(32),
    [page_design_2] varchar(32),
    [page_design_3] varchar(32),
    [referral_source_id] smallint,
    [user_ip] varchar(16),
    [order_type_id] smallint NOT NULL,
    [amount] decimal(9,2) NOT NULL,
    [purchased_date] datetime NOT NULL,
    [modified_date] datetime,
    [payment_method_id] smallint NOT NULL,
    [payment_authorization_id] smallint,
    [invoice_status_id] smallint NOT NULL,
    [completed_ship_date] datetime,
    [alt_invoice_code] varchar(24),
    [visitorid] bigint,
    [coupon_amount] money,
    [last_modified] datetime NOT NULL,
    [tax_total] decimal(15,2),
    [p_rc] varchar(12),
    [p_rsc] varchar(64),
    [p_ac] varchar(12),
    [trx_rc] varchar(12),
    [trx_rsc] varchar(64),
    [trx_ac] varchar(12)
,
    PRIMARY KEY ([invoice_code])
);
CREATE TABLE [dbo].[invoices_deleted] (
    [invoice_code] varchar(16) NOT NULL,
    [customer_code] varchar(16) NOT NULL,
    [affiliate_code] varchar(16) NOT NULL,
    [affiliate_subcode] varchar(32),
    [master_override_affiliate_num] int,
    [employee_id] int,
    [promo_code] varchar(32),
    [trial_product_id] int,
    [wcode1] varchar(32),
    [wcode2] varchar(32),
    [wcode3] varchar(32),
    [distribution_channel_code] varchar(32),
    [distribution_role_code] varchar(32),
    [cd_code] varchar(32),
    [pop_under] varchar(32),
    [page_design_1] varchar(32),
    [page_design_2] varchar(32),
    [page_design_3] varchar(32),
    [referral_source_id] smallint,
    [user_ip] varchar(16),
    [order_type_id] smallint NOT NULL,
    [amount] decimal(9,2) NOT NULL,
    [purchased_date] datetime NOT NULL,
    [modified_date] datetime,
    [payment_method_id] smallint NOT NULL,
    [payment_authorization_id] smallint,
    [invoice_status_id] smallint NOT NULL,
    [completed_ship_date] datetime,
    [alt_invoice_code] varchar(24),
    [visitorid] bigint,
    [coupon_amount] money,
    [last_modified] datetime NOT NULL,
    [tax_total] decimal(15,2)
);
CREATE TABLE [dbo].[Invoices_sequence] (
    [sequence_id] int IDENTITY(1,1) NOT NULL,
    [insert_date] datetime
);
CREATE TABLE [dbo].[invoicesAudit] (
    [invoice_code] varchar(16) NOT NULL,
    [customer_code] varchar(16) NOT NULL,
    [affiliate_code] varchar(16) NOT NULL,
    [affiliate_subcode] varchar(32),
    [master_override_affiliate_num] int,
    [employee_id] int,
    [promo_code] varchar(32),
    [trial_product_id] int,
    [wcode1] varchar(32),
    [wcode2] varchar(32),
    [wcode3] varchar(32),
    [distribution_channel_code] varchar(32),
    [distribution_role_code] varchar(32),
    [cd_code] varchar(32),
    [pop_under] varchar(32),
    [page_design_1] varchar(32),
    [page_design_2] varchar(32),
    [page_design_3] varchar(32),
    [referral_source_id] smallint,
    [user_ip] varchar(16),
    [order_type_id] smallint NOT NULL,
    [amount] decimal(9,2) NOT NULL,
    [purchased_date] datetime NOT NULL,
    [modified_date] datetime,
    [payment_method_id] smallint NOT NULL,
    [payment_authorization_id] smallint,
    [invoice_status_id] smallint NOT NULL,
    [completed_ship_date] datetime,
    [alt_invoice_code] varchar(24),
    [visitorid] bigint,
    [coupon_amount] money,
    [last_modified] datetime NOT NULL,
    [tax_total] decimal(15,2),
    [AuditTimestamp] datetime,
    [AuditSystemUser] varchar(50),
    [p_rc] varchar(12),
    [p_rsc] varchar(64),
    [p_ac] varchar(12),
    [trx_rc] varchar(12),
    [trx_rsc] varchar(64),
    [trx_ac] varchar(12)
);
CREATE TABLE [dbo].[ironport_license_check_results] (
    [keycode] varchar(40),
    [license_type_id] int,
    [license_status_id] int,
    [capability_id] int,
    [capability_name] varchar(50),
    [capability_description] varchar(50),
    [capability_type_id] int,
    [capability_expiration_date] datetime,
    [days_left] int,
    [needs_activation] int,
    [needs_registration] int,
    [auto_renewal] int,
    [insert_date] datetime,
    [row_count] int,
    [error] int
);
CREATE TABLE [dbo].[item_hierarchy] (
    [item_hierarchy_id] int IDENTITY(1,1) NOT NULL,
    [item_hierarchy_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([item_hierarchy_id])
);
CREATE TABLE [dbo].[keycode_invalid] (
    [keycode_invalid_id] int IDENTITY(1,1) NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([keycode_invalid_id])
);
CREATE TABLE [dbo].[keycode_sequence] (
    [keycode_sequence_id] bigint IDENTITY(1,1) NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[keycode_update_policies] (
    [update_key] int NOT NULL,
    [product_id] int NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [last_modified] datetime NOT NULL,
    [rc] varchar(8) NOT NULL
);
CREATE TABLE [dbo].[keycode_update_products] (
    [update_key] int NOT NULL,
    [update_product_id] int NOT NULL,
    [update_days] int NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [last_modified] datetime NOT NULL
);
CREATE TABLE [dbo].[keycodes] (
    [keycode] char(20) NOT NULL,
    [product_id] int NOT NULL,
    [customer_code] varchar(16),
    [activation_counts] smallint NOT NULL,
    [max_allowed_activations] int NOT NULL,
    [activation_date] smalldatetime,
    [status_id] tinyint NOT NULL,
    [type_id] tinyint NOT NULL,
    [assigned_retailer] varchar(50),
    [allow_upgrade_days] smallint NOT NULL,
    [allow_defs_days] smallint NOT NULL,
    [allow_plugins_days] smallint NOT NULL,
    [last_modified] datetime NOT NULL,
    [IsValid] bit,
    [Expiration_Date] datetime
,
    PRIMARY KEY ([keycode])
);
CREATE TABLE [dbo].[KeycodesHistory] (
    [AuditID] int IDENTITY(1,1) NOT NULL,
    [keycode] char(20) NOT NULL,
    [product_id] int NOT NULL,
    [customer_code] varchar(16),
    [activation_counts] smallint NOT NULL,
    [max_allowed_activations] int NOT NULL,
    [activation_date] smalldatetime,
    [status_id] tinyint NOT NULL,
    [type_id] tinyint NOT NULL,
    [assigned_retailer] varchar(50),
    [allow_upgrade_days] smallint NOT NULL,
    [allow_defs_days] smallint NOT NULL,
    [allow_plugins_days] smallint NOT NULL,
    [last_modified] datetime NOT NULL,
    [AuditTimestamp] datetime NOT NULL,
    [AuditSystemUser] varchar(20) NOT NULL
,
    PRIMARY KEY ([AuditID])
);
CREATE TABLE [dbo].[labtech_billing] (
    [labtech_billing_id] int IDENTITY(1,1) NOT NULL,
    [report_date] datetime,
    [vendor_customer_code] varchar(100) NOT NULL,
    [company_name] nvarchar(255) NOT NULL,
    [webroot_entity] varchar(3) NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [license_seats] int NOT NULL,
    [order_header_id] int,
    [vendor_order_date] datetime,
    [product_id] int NOT NULL,
    [keycode_age] int,
    [retail_price] money,
    [extended_amount] money,
    [cap_amount] float,
    [insert_date] datetime NOT NULL,
    [location_code] varchar(3),
    [state] nvarchar(2)
);
CREATE TABLE [dbo].[labtech_gsm_migration] (
    [labtech_gsm_migration_id] int IDENTITY(1,1) NOT NULL,
    [original_keycode] varchar(40),
    [new_keycode] varchar(40),
    [insert_date] datetime
);
CREATE TABLE [dbo].[lastpass_reconciliation] (
    [lastpass_reconciliation_id] int IDENTITY(1,1) NOT NULL,
    [account_id] int NOT NULL,
    [account_user_name] varchar(100) NOT NULL,
    [lastpass_insert_date] datetime NOT NULL,
    [response_code] tinyint,
    [response_text] varchar(20),
    [last_update_date] datetime NOT NULL
,
    PRIMARY KEY ([lastpass_reconciliation_id])
);
CREATE TABLE [dbo].[lastpass_update] (
    [lastpass_update_id] int IDENTITY(1,1) NOT NULL,
    [account_id] int NOT NULL,
    [lastpass_update_type_id] tinyint NOT NULL,
    [lastpass_update_status_id] tinyint NOT NULL,
    [response_code] tinyint,
    [response_text] varchar(20),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime
,
    PRIMARY KEY ([lastpass_update_id])
);
CREATE TABLE [dbo].[lastpass_update_archive] (
    [lastpass_update_archive_id] int IDENTITY(1,1) NOT NULL,
    [lastpass_update_id] int NOT NULL,
    [account_id] int NOT NULL,
    [lastpass_update_type_id] tinyint NOT NULL,
    [lastpass_update_status_id] tinyint NOT NULL,
    [response_code] tinyint,
    [response_text] varchar(20),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime
,
    PRIMARY KEY ([lastpass_update_archive_id])
);
CREATE TABLE [dbo].[lastpass_update_failure] (
    [lastpass_update_failure_id] int IDENTITY(1,1) NOT NULL,
    [lastpass_update_id] int NOT NULL,
    [account_id] int NOT NULL,
    [lastpass_update_type_id] tinyint NOT NULL,
    [lastpass_update_status_id] tinyint NOT NULL,
    [response_code] tinyint,
    [response_text] varchar(20),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime
,
    PRIMARY KEY ([lastpass_update_failure_id])
);
CREATE TABLE [dbo].[lastpass_update_status] (
    [lastpass_update_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [lastpass_update_status_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([lastpass_update_status_id])
);
CREATE TABLE [dbo].[lastpass_update_type] (
    [lastpass_update_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [lastpass_update_type_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([lastpass_update_type_id])
);
CREATE TABLE [dbo].[license] (
    [license_id] int IDENTITY(1,1) NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [customer_id] int,
    [product_line_id] int NOT NULL,
    [license_status_id] int NOT NULL,
    [license_type_id] int NOT NULL,
    [license_distribution_method_id] int,
    [license_keycode_type_id] int,
    [max_daily_activations] int NOT NULL,
    [max_child_licenses] int,
    [license_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([license_id])
);
CREATE TABLE [dbo].[license_activation] (
    [license_activation_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [guid] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200),
    [machine_name] varchar(20)
,
    PRIMARY KEY ([license_activation_id])
);
CREATE TABLE [dbo].[license_activation_temp_insert_db2] (
    [license_activation_id] int NOT NULL,
    [license_id] int NOT NULL,
    [guid] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200)
);
CREATE TABLE [dbo].[license_active_seats] (
    [license_id] int NOT NULL,
    [start_date] datetime2 NOT NULL,
    [end_date] datetime2 NOT NULL,
    [consumed_seats] int NOT NULL
);
CREATE TABLE [dbo].[license_active_seats_failed] (
    [failed_id] int IDENTITY(1,1) NOT NULL,
    [keycode] nvarchar(50),
    [start_date] datetime2,
    [end_date] datetime2,
    [consumed_seats] int
);
CREATE TABLE [dbo].[license_active_seats_history] (
    [license_active_seats_history_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [start_date] datetime2 NOT NULL,
    [end_date] datetime2 NOT NULL,
    [consumed_seats] int NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [modified_by] varchar(100) NOT NULL
,
    PRIMARY KEY ([license_active_seats_history_id])
);
CREATE TABLE [dbo].[license_active_seats_stage] (
    [staging_id] int IDENTITY(1,1) NOT NULL,
    [keycode] nvarchar(50) NOT NULL,
    [start_date] datetime2 NOT NULL,
    [end_date] datetime2 NOT NULL,
    [consumed_seats] int NOT NULL,
    [is_processed] tinyint
,
    PRIMARY KEY ([staging_id])
);
CREATE TABLE [dbo].[license_activity] (
    [license_activity_id] int NOT NULL,
    [license_id] int,
    [keycode] varchar(40) NOT NULL,
    [mjv] smallint,
    [version] varchar(10),
    [grabword] varchar(30),
    [rc] int,
    [operating_system] smallint,
    [platform] int,
    [language_code] varchar(2),
    [location_code] varchar(3),
    [device_mid] varchar(40) NOT NULL,
    [instance_mid] varchar(64) NOT NULL,
    [threats_removed] int,
    [current_threats] smallint,
    [number_scans] int,
    [average_scan_time] int,
    [number_events] bigint,
    [bytes_cleaned] bigint,
    [time_protected] bigint,
    [first_activity_date] datetime,
    [last_activity_date] datetime,
    [max_activity_id] bigint,
    [total_checks] int,
    [max_threats_removed] int,
    [max_current_threats] smallint,
    [hostname] varchar(64),
    [sa_score] tinyint,
    [sa_software_status] char(1),
    [sa_hardware_status] char(1),
    [sa_threat_status] char(1)
,
    PRIMARY KEY ([license_activity_id])
);
CREATE TABLE [dbo].[license_activity_action] (
    [license_activity_action_id] int NOT NULL,
    [license_activity_action_type_id] int NOT NULL,
    [license_activity_action_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([license_activity_action_id])
);
CREATE TABLE [dbo].[license_activity_import] (
    [license_activity_import_id] int IDENTITY(1,1) NOT NULL,
    [dt] datetime NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [guid] varchar(50),
    [pc] int,
    [rc] int,
    [oc] int,
    [mjv] int,
    [mnv] int,
    [bld] int,
    [lang] varchar(2),
    [loc] varchar(3),
    [oid] int,
    [omj] int,
    [omn] int,
    [action] int,
    [data] varchar(255)
,
    PRIMARY KEY ([license_activity_import_id])
);
CREATE TABLE [dbo].[license_activity_operation_code] (
    [license_activity_operation_code_id] int NOT NULL,
    [license_activity_operation_code] varchar(50) NOT NULL
,
    PRIMARY KEY ([license_activity_operation_code_id])
);
CREATE TABLE [dbo].[license_activity_staging] (
    [license_activity_id] int NOT NULL,
    [license_id] int,
    [keycode] varchar(40) NOT NULL,
    [mjv] smallint,
    [version] varchar(10),
    [grabword] varchar(30),
    [rc] int,
    [operating_system] smallint,
    [platform] int,
    [language_code] varchar(2),
    [location_code] varchar(3),
    [device_mid] varchar(40) NOT NULL,
    [instance_mid] varchar(64) NOT NULL,
    [threats_removed] int,
    [current_threats] smallint,
    [number_scans] int,
    [average_scan_time] int,
    [number_events] bigint,
    [bytes_cleaned] bigint,
    [time_protected] bigint,
    [first_activity_date] datetime,
    [last_activity_date] datetime,
    [max_activity_id] bigint,
    [total_checks] int,
    [max_threats_removed] int,
    [max_current_threats] smallint,
    [hostname] varchar(64),
    [sa_score] tinyint,
    [sa_software_status] char(1),
    [sa_hardware_status] char(1),
    [sa_threat_status] char(1),
    [record_type] char(1) NOT NULL
,
    PRIMARY KEY ([license_activity_id])
);
CREATE TABLE [dbo].[license_app_store_status] (
    [license_app_store_status_id] int NOT NULL,
    [license_app_store_status_description] varchar(100) NOT NULL
,
    PRIMARY KEY ([license_app_store_status_id])
);
CREATE TABLE [dbo].[license_arbitration] (
    [license_id] int,
    [license_key] uniqueidentifier,
    [keycode] varchar(40),
    [email] varchar(100),
    [activation_date] date,
    [opt_out] bit NOT NULL,
    [opt_out_date] date NOT NULL
);
CREATE TABLE [dbo].[license_attribute] (
    [license_attribute_id] int IDENTITY(1,1) NOT NULL,
    [license_attribute_description] varchar(100) NOT NULL,
    [license_attribute_tag] varchar(20) NOT NULL,
    [license_attribute_default_value] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [license_module_id] tinyint
,
    PRIMARY KEY ([license_attribute_id])
);
CREATE TABLE [dbo].[license_attribute_license] (
    [license_attribute_license_id] int IDENTITY(1,1) NOT NULL,
    [license_attribute_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_attribute_license_value] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([license_attribute_license_id])
);
CREATE TABLE [dbo].[license_attribute_license_history] (
    [license_attribute_license_history_id] int IDENTITY(1,1) NOT NULL,
    [license_attribute_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_attribute_license_value] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [start_date] datetime,
    [end_date] datetime,
    [license_attribute_license_id] int
,
    PRIMARY KEY ([license_attribute_license_history_id])
);
CREATE TABLE [dbo].[license_attribute_license_value] (
    [license_attribute_license_value] int NOT NULL,
    [license_attribute_id] int NOT NULL,
    [license_attribute_license_value_description] varchar(50) NOT NULL,
    [license_module_type_id] tinyint,
    [autobilling_enabled] bit
,
    PRIMARY KEY ([license_attribute_license_value])
);
CREATE TABLE [dbo].[license_autorenewal_cycle] (
    [license_id] int NOT NULL,
    [message_autorenewal_cycle_id] tinyint NOT NULL,
    [billing_day_of_month] tinyint,
    [license_autorenewal_cycle_id] int IDENTITY(1,1) NOT NULL,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([license_autorenewal_cycle_id])
);
CREATE TABLE [dbo].[license_autorenewal_cycle_audit] (
    [license_autorenewal_cycle_audit_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [message_autorenewal_cycle_id] tinyint NOT NULL,
    [billing_day_of_month] tinyint,
    [license_autorenewal_cycle_id] int NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([license_autorenewal_cycle_audit_id])
);
CREATE TABLE [dbo].[license_autorenewal_discount] (
    [license_autorenewal_discount_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [autorenewal_discount_type_id] int NOT NULL,
    [discount_start_date] datetime NOT NULL,
    [discount_end_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_autorenewal_discount_id])
);
CREATE TABLE [dbo].[license_autorenewal_no_keycode_opt_out] (
    [license_autorenewal_no_keycode_opt_out_id] int IDENTITY(1,1) NOT NULL,
    [first_name] varchar(255),
    [last_name] varchar(255),
    [customer_email] varchar(100),
    [reason_to_cancel] varchar(500),
    [request_uid] varchar(255),
    [request_open_category] varchar(100),
    [request_source] varchar(100),
    [request_ip] varchar(100),
    [insert_date] datetime NOT NULL,
    [license_category_id] int,
    [response_code] int,
    [message] varchar(100)
,
    PRIMARY KEY ([license_autorenewal_no_keycode_opt_out_id])
);
CREATE TABLE [dbo].[license_autorenewal_opt_out] (
    [license_autorenewal_opt_out_id] int IDENTITY(1,1) NOT NULL,
    [requested_keycode] varchar(40),
    [license_id] int,
    [license_category_id] tinyint,
    [license_seats] int,
    [status] varchar(10),
    [message_campaign_id] int,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_autorenewal_opt_out_id])
);
CREATE TABLE [dbo].[license_behavior] (
    [license_behavior_id] int IDENTITY(1,1) NOT NULL,
    [license_behavior_description] varchar(100) NOT NULL,
    [license_behavior_class_id] int NOT NULL,
    [license_behavior_status_id] int NOT NULL,
    [capability_type_id] tinyint,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([license_behavior_id])
);
CREATE TABLE [dbo].[license_behavior_check] (
    [license_behavior_check_id] int IDENTITY(1,1) NOT NULL,
    [license_behavior_id] int NOT NULL,
    [license_behavior_check_value] nvarchar(100),
    [processed] bit,
    [insert_date] datetime
,
    PRIMARY KEY ([license_behavior_check_id])
);
CREATE TABLE [dbo].[license_behavior_check_license] (
    [license_behavior_check_license_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_behavior_id] int NOT NULL,
    [processed] tinyint,
    [insert_date] datetime
,
    PRIMARY KEY ([license_behavior_check_license_id])
);
CREATE TABLE [dbo].[license_behavior_class] (
    [license_behavior_class_id] int IDENTITY(1,1) NOT NULL,
    [license_behavior_class_description] varchar(100) NOT NULL,
    [license_category_id] tinyint,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([license_behavior_class_id])
);
CREATE TABLE [dbo].[license_behavior_license] (
    [license_behavior_license_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_behavior_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [completion_date] datetime,
    [completion_status] varchar(50)
,
    PRIMARY KEY ([license_behavior_license_id])
);
CREATE TABLE [dbo].[license_behavior_license_archive] (
    [license_behavior_license_archive_id] int IDENTITY(1,1) NOT NULL,
    [license_behavior_license_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_behavior_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [completion_date] datetime,
    [archive_date] datetime NOT NULL
,
    PRIMARY KEY ([license_behavior_license_archive_id])
);
CREATE TABLE [dbo].[license_behavior_license_category] (
    [license_behavior_license_category_id] int IDENTITY(1,1) NOT NULL,
    [license_behavior_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [capability_type_id] int NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(100),
    [modified_date] datetime,
    [modified_by] varchar(100)
,
    PRIMARY KEY ([license_behavior_license_category_id])
);
CREATE TABLE [dbo].[license_behavior_license_new] (
    [license_behavior_license_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [license_behavior_id] int NOT NULL,
    [order_header_id] int,
    [behavior_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(150) NOT NULL,
    [modified_date] datetime,
    [modified_by] varchar(150)
,
    PRIMARY KEY ([license_behavior_license_id])
);
CREATE TABLE [dbo].[license_behavior_realationship] (
    [license_behavior_relationship_id] int IDENTITY(1,1) NOT NULL,
    [license_behavior_id] int NOT NULL,
    [relationship_column] varchar(100),
    [relationship_table] varchar(100),
    [license_column] varchar(100),
    [sql_string] nvarchar(MAX),
    [is_query] bit NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200)
,
    PRIMARY KEY ([license_behavior_relationship_id])
);
CREATE TABLE [dbo].[license_behavior_sequence] (
    [license_behavior_sequence_id] int IDENTITY(1,1) NOT NULL,
    [license_behavior_class_id] int NOT NULL,
    [license_behavior_id] int NOT NULL,
    [sequence_number] int,
    [insert_date] datetime,
    [insert_by] varchar(200)
,
    PRIMARY KEY ([license_behavior_sequence_id])
);
CREATE TABLE [dbo].[license_behavior_status] (
    [license_behavior_status_id] int IDENTITY(1,1) NOT NULL,
    [license_behavior_status_description] varchar(50) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200)
,
    PRIMARY KEY ([license_behavior_status_id])
);
CREATE TABLE [dbo].[license_bulk_load] (
    [license_bulk_load_id] int IDENTITY(1,1) NOT NULL,
    [license_bulk_load_description] varchar(100) NOT NULL,
    [quantity] int NOT NULL,
    [product_id] int NOT NULL,
    [product_line_id] int NOT NULL,
    [distribution_rc] int,
    [license_status_id] int NOT NULL,
    [license_type_id] int NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [license_keycode_type_id] int,
    [max_daily_activations] int,
    [max_child_licenses] int,
    [license_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [license_bulk_status_id] int NOT NULL,
    [license_bulk_load_group_id] int,
    [module_product_id] int,
    [enterprise_seats] int,
    [fixed_expiration_OEM] tinyint,
    [days_override] int
,
    PRIMARY KEY ([license_bulk_load_id])
);
CREATE TABLE [dbo].[license_bulk_load_download] (
    [license_bulk_load_download_id] int IDENTITY(1,1) NOT NULL,
    [license_bulk_load_id] int NOT NULL,
    [download_key] uniqueidentifier NOT NULL,
    [insert_date] datetime NOT NULL,
    [expiration_date] datetime NOT NULL,
    [status] varchar(20) NOT NULL
,
    PRIMARY KEY ([license_bulk_load_download_id])
);
CREATE TABLE [dbo].[license_bulk_load_group] (
    [license_bulk_load_group_id] int IDENTITY(1,1) NOT NULL,
    [license_bulk_load_group_description] varchar(100),
    [insert_date] datetime,
    [insert_by] varchar(200)
);
CREATE TABLE [dbo].[license_bulk_load_license] (
    [license_bulk_load_license_id] int IDENTITY(1,1) NOT NULL,
    [license_bulk_load_id] int NOT NULL,
    [keycode] varchar(40),
    [product_id] int,
    [license_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [module_product_id] int
,
    PRIMARY KEY ([license_bulk_load_license_id])
);
CREATE TABLE [dbo].[license_bulk_load_license_update] (
    [license_bulk_load_license_update_type_id] int IDENTITY(1,1) NOT NULL,
    [license_bulk_load_license_update_type] varchar(40) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_bulk_load_license_update_type_id])
);
CREATE TABLE [dbo].[license_bulk_status] (
    [license_bulk_status_id] int IDENTITY(1,1) NOT NULL,
    [license_bulk_status_description] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([license_bulk_status_id])
);
CREATE TABLE [dbo].[license_capability] (
    [license_capability_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [capability_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_activation_days] int,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([license_capability_id])
);
CREATE TABLE [dbo].[license_capability_audit] (
    [license_capability_audit_id] int IDENTITY(1,1) NOT NULL,
    [license_capability_id] int NOT NULL,
    [license_id] int NOT NULL,
    [capability_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_activation_days] int,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [audit_date] datetime NOT NULL,
    [audit_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_capability_audit_id])
);
CREATE TABLE [dbo].[license_capability_history] (
    [license_capability_history_id] int IDENTITY(1,1) NOT NULL,
    [license_capability_id] int NOT NULL,
    [license_id] int NOT NULL,
    [capability_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_activation_days] int,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [capability_change_reason_id] int NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(50) NOT NULL,
    [days_used] smallint,
    [days_remaining] smallint,
    [days_added] smallint,
    [days_new] smallint,
    [days_contiguous] smallint,
    [order_item_id] int
,
    PRIMARY KEY ([license_capability_history_id])
);
CREATE TABLE [dbo].[license_capability_history_temp_insert_db2] (
    [license_capability_history_id] int NOT NULL,
    [license_capability_id] int NOT NULL,
    [license_id] int NOT NULL,
    [capability_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_activation_days] int,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [capability_change_reason_id] int NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL,
    [days_used] smallint,
    [days_remaining] smallint,
    [days_added] smallint,
    [days_new] smallint,
    [days_contiguous] smallint
);
CREATE TABLE [dbo].[license_capability_temp_insert_db2] (
    [license_capability_id] int NOT NULL,
    [license_id] int NOT NULL,
    [capability_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_activation_days] int,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[license_cart_discount] (
    [license_cart_discount_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [cart_discount_id] int NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([license_cart_discount_id])
);
CREATE TABLE [dbo].[license_cart_discount_history] (
    [license_cart_discount_history_id] int IDENTITY(1,1) NOT NULL,
    [license_cart_discount_id] int NOT NULL,
    [license_id] int NOT NULL,
    [cart_discount_id] int NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime,
    [last_modified_date] datetime NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_cart_discount_history_id])
);
CREATE TABLE [dbo].[license_category] (
    [license_category_id] tinyint IDENTITY(1,1) NOT NULL,
    [license_category_name] varchar(10) NOT NULL,
    [license_category_description] varchar(50) NOT NULL,
    [base_capability_id] int,
    [devices_per_seat] tinyint NOT NULL,
    [min_order_quantity] int,
    [max_order_quantity] int,
    [status] tinyint NOT NULL,
    [license_category_brand] varchar(50)
,
    PRIMARY KEY ([license_category_id])
);
CREATE TABLE [dbo].[license_category_activity_headers] (
    [license_category_activity_headers_id] int IDENTITY(1,1) NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [column_name] varchar(50) NOT NULL,
    [column_header] varchar(50) NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([license_category_activity_headers_id])
);
CREATE TABLE [dbo].[license_category_audit_update] (
    [license_category_audit_update_id] int IDENTITY(1,1) NOT NULL,
    [license_category_license_id] int NOT NULL,
    [license_id] int NOT NULL,
    [process_flag] tinyint NOT NULL,
    [corrected] tinyint NOT NULL
);
CREATE TABLE [dbo].[license_category_capability] (
    [license_category_capability_id] int IDENTITY(1,1) NOT NULL,
    [license_category_id] int NOT NULL,
    [capability_id] int NOT NULL
,
    PRIMARY KEY ([license_category_capability_id])
);
CREATE TABLE [dbo].[license_category_discount_model_json] (
    [license_category_discount_model_json_id] int IDENTITY(1,1) NOT NULL,
    [license_category_discount_model_json] nvarchar(MAX),
    [license_category_discount_model_source_caller] varchar(100),
    [insert_date] datetime NOT NULL,
    [db_error_message] varchar(MAX),
    [item_discount_profile_json] nvarchar(MAX)
,
    PRIMARY KEY ([license_category_discount_model_json_id])
);
CREATE TABLE [dbo].[license_category_license] (
    [license_category_license_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [start_date] datetime,
    [end_date] datetime,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200)
,
    PRIMARY KEY ([license_category_license_id])
);
CREATE TABLE [dbo].[license_category_license_history] (
    [license_category_license__historty_id] int IDENTITY(1,1) NOT NULL,
    [license_category_license_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [start_date] datetime,
    [end_date] datetime,
    [order_item_id] int,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [license_change_reason_id] tinyint NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_category_license__historty_id])
);
CREATE TABLE [dbo].[license_category_module] (
    [license_category_module_id] smallint IDENTITY(1,1) NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [license_module_id] tinyint NOT NULL,
    [license_module_type_id] tinyint NOT NULL,
    [license_category_module_end_date] datetime
,
    PRIMARY KEY ([license_category_module_id])
);
CREATE TABLE [dbo].[license_category_product_line] (
    [license_category_product_line_id] int IDENTITY(1,1) NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [product_line_id] int NOT NULL,
    [cart_type_id] tinyint NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [payment_merchant_id] tinyint,
    [site_id] varchar(20),
    [alt_language_code] varchar(2),
    [alt_location_code] varchar(3),
    [legal_entity] varchar(5)
,
    PRIMARY KEY ([license_category_product_line_id])
);
CREATE TABLE [dbo].[license_category_product_line_license_attribute_license_value] (
    [license_category_product_line_license_attribute_license_value_id] int IDENTITY(1,1) NOT NULL,
    [license_category_product_line_id] int NOT NULL,
    [license_attribute_license_value] int NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([license_category_product_line_license_attribute_license_value_id])
);
CREATE TABLE [dbo].[license_category_storage] (
    [license_category_storage_id] int IDENTITY(1,1) NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [product_extension_json] nvarchar(MAX) NOT NULL,
    [storage_TB] decimal(4,1) NOT NULL,
    [quantity] int NOT NULL
,
    PRIMARY KEY ([license_category_storage_id])
);
CREATE TABLE [dbo].[license_change_reason] (
    [license_change_reason_id] int IDENTITY(1,1) NOT NULL,
    [license_change_reason] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_change_reason_id])
);
CREATE TABLE [dbo].[license_channel] (
    [license_channel_id] tinyint IDENTITY(1,1) NOT NULL,
    [license_channel_name] varchar(30) NOT NULL,
    [license_channel_description] varchar(100) NOT NULL
,
    PRIMARY KEY ([license_channel_id])
);
CREATE TABLE [dbo].[license_channel_exemption] (
    [license_channel_exemption_id] int IDENTITY(1,1) NOT NULL,
    [license_channel_id] tinyint NOT NULL,
    [company_id] int NOT NULL
,
    PRIMARY KEY ([license_channel_exemption_id])
);
CREATE TABLE [dbo].[license_channel_license] (
    [license_channel_license_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_channel_id] tinyint NOT NULL,
    [gsm] bit NOT NULL,
    [order_header_id] int,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [license_channel_logic_id] int NOT NULL
,
    PRIMARY KEY ([license_channel_license_id])
);
CREATE TABLE [dbo].[license_channel_license_history] (
    [license_channel_license_history_id] int IDENTITY(1,1) NOT NULL,
    [license_channel_license_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_channel_id] tinyint NOT NULL,
    [gsm] bit NOT NULL,
    [order_header_id] int,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_channel_license_history_id])
);
CREATE TABLE [dbo].[license_channel_logic] (
    [license_channel_logic_id] int NOT NULL,
    [license_channel_logic_name] varchar(50) NOT NULL,
    [license_channel_logic_description] varchar(255) NOT NULL,
    [license_channel_id] tinyint NOT NULL,
    [priority] int NOT NULL,
    [enabled] bit NOT NULL
,
    PRIMARY KEY ([license_channel_logic_id])
);
CREATE TABLE [dbo].[license_check_update] (
    [license_check_update_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [license_history_id] int,
    [license_capability_history_id] int,
    [license_feature_license_id] int,
    [license_attribute_license_id] int,
    [license_attribute_license_history_id] int,
    [license_storage_id] int,
    [license_storage_history_id] int,
    [storage_account_id] int,
    [storage_account_audit_id] int,
    [insert_date] datetime,
    [license_check_update_status] tinyint,
    [processed_date] datetime,
    [license_category_license__historty_id] int,
    [license_category_license_id] int,
    [license_seat_id] int,
    [license_seat_history_id] int,
    [license_message_id] int,
    [license_message_archive_id] int,
    [subscription_history_id] int,
    [subscription_external_id] int,
    [customer_audit_id] int,
    [customer_id] int,
    [license_module_license_id] int,
    [license_module_license_history_id] int,
    [autobilling_effective_object_id] int,
    [license_service_id] int,
    [license_service_archive_id] int
,
    PRIMARY KEY ([license_check_update_id])
);
CREATE TABLE [dbo].[license_check_update_license] (
    [license_id] int NOT NULL,
    [source] tinyint NOT NULL,
    [insertdate] datetime
);
CREATE TABLE [dbo].[license_check_xfer] (
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [license_status_id] int NOT NULL,
    [license_type_id] int NOT NULL,
    [capability_id] int,
    [capability_name] varchar(50),
    [capability_description] varchar(50),
    [capability_type_id] int,
    [capability_activation_days] int,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [needs_registration] bit,
    [auto_renewal] bit,
    [license_category_name] varchar(10),
    [product_line_id] int,
    [license_module_list] varchar(512),
    [license_seats] int
);
CREATE TABLE [dbo].[license_churn] (
    [license_id] int,
    [churn_days_add] int NOT NULL,
    [churn_days_measure] int NOT NULL,
    [insert_date] datetime
);
CREATE TABLE [dbo].[license_churn_archive] (
    [license_churn_archive_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [churn_days_add] int NOT NULL,
    [churn_days_measure] int NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200)
,
    PRIMARY KEY ([license_churn_archive_id])
);
CREATE TABLE [dbo].[license_contract] (
    [license_contract_id] int IDENTITY(1,1) NOT NULL,
    [originating_license_contract_id] int,
    [license_contract_transaction_id] int,
    [license_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [license_category_type_id] int NOT NULL,
    [license_start_date] date NOT NULL,
    [license_end_date] date NOT NULL,
    [license_contract_status_id] tinyint NOT NULL,
    [modified_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_contract_id])
);
CREATE TABLE [dbo].[license_contract_exception_reasons] (
    [license_contract_exception_reason_id] tinyint IDENTITY(1,1) NOT NULL,
    [license_contract_exception_reason_description] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_contract_exception_reason_id])
);
CREATE TABLE [dbo].[license_contract_exceptions] (
    [license_contract_exception_id] int IDENTITY(1,1) NOT NULL,
    [license_contract_exception_reason_id] tinyint NOT NULL,
    [license_contract_json_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_contract_exception_id])
);
CREATE TABLE [dbo].[license_contract_json] (
    [license_contract_json_id] int IDENTITY(1,1) NOT NULL,
    [license_contract_json] varchar(MAX) NOT NULL,
    [license_contract_source_caller] varchar(50),
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_contract_json_id])
);
CREATE TABLE [dbo].[license_contract_status] (
    [license_contract_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [license_contract_status_description] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_contract_status_id])
);
CREATE TABLE [dbo].[license_contract_transaction] (
    [license_contract_transaction_id] int IDENTITY(1,1) NOT NULL,
    [license_contract_json_id] int NOT NULL,
    [contract_transaction_type_index] int NOT NULL,
    [effective_object_transaction_type_id] tinyint NOT NULL,
    [contract_transaction_type_value] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_contract_transaction_id])
);
CREATE TABLE [dbo].[license_customer] (
    [license_customer_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [license_customer_source_id] int NOT NULL,
    [opt_in] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[license_customer_source] (
    [license_customer_source_id] int IDENTITY(1,1) NOT NULL,
    [license_customer_source_description] varchar(50),
    [license_customer_user_type_id] int
);
CREATE TABLE [dbo].[license_customer_user_type] (
    [license_customer_user_type_id] int IDENTITY(1,1) NOT NULL,
    [license_customer_user_type_description] varchar(50)
);
CREATE TABLE [dbo].[license_distribution_method] (
    [license_distribution_method_id] int IDENTITY(1,1) NOT NULL,
    [license_distribution_method_code] char(4) NOT NULL,
    [license_distribution_method_name] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [license_distribution_method_active] tinyint,
    [cd_default] tinyint
,
    PRIMARY KEY ([license_distribution_method_id])
);
CREATE TABLE [dbo].[license_distribution_method_channel] (
    [license_distribution_method_channel_id] int IDENTITY(1,1) NOT NULL,
    [channel_id] int NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [distribution_geography_id] tinyint,
    [distribution_class_id] tinyint,
    [distribution_account_id] tinyint,
    [distribution_business_unit_id] tinyint
,
    PRIMARY KEY ([license_distribution_method_channel_id])
);
CREATE TABLE [dbo].[license_distribution_method_churn] (
    [license_distribution_method_churn_id] int IDENTITY(1,1) NOT NULL,
    [license_distribution_method_id] int,
    [churn_days_add] int NOT NULL,
    [churn_days_measure] int NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200)
,
    PRIMARY KEY ([license_distribution_method_churn_id])
);
CREATE TABLE [dbo].[license_distribution_method_language_location] (
    [license_distribution_method_language_location_id] int IDENTITY(1,1) NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL
,
    PRIMARY KEY ([license_distribution_method_language_location_id])
);
CREATE TABLE [dbo].[license_distribution_method_module] (
    [license_distribution_method_module_id] int IDENTITY(1,1) NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [license_module_id] tinyint NOT NULL,
    [license_module_type_id] tinyint NOT NULL
,
    PRIMARY KEY ([license_distribution_method_module_id])
);
CREATE TABLE [dbo].[license_distribution_method_upd] (
    [license_distribution_method_code] nvarchar(255),
    [license_distribution_method_code1] nvarchar(255),
    [rc] float
);
CREATE TABLE [dbo].[license_effective_object] (
    [license_effective_object_id] int IDENTITY(1,1) NOT NULL,
    [license_effective_object_transaction_id] int,
    [effective_object_id] tinyint NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [effective_value] int NOT NULL,
    [effective_start_date] date NOT NULL,
    [effective_end_date] date,
    [is_dup] bit NOT NULL,
    [modified_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_effective_object_id])
);
CREATE TABLE [dbo].[license_effective_object_json] (
    [license_effective_object_json_id] int IDENTITY(1,1) NOT NULL,
    [license_effective_object_json] varchar(MAX) NOT NULL,
    [license_effective_source_caller] varchar(50),
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_effective_object_json_id])
);
CREATE TABLE [dbo].[license_effective_object_transaction] (
    [license_effective_object_transaction_id] int IDENTITY(1,1) NOT NULL,
    [license_effective_object_json_id] int NOT NULL,
    [effective_object_transaction_type_index] int NOT NULL,
    [effective_object_transaction_type_id] tinyint,
    [effective_object_transaction_type_value] int,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_effective_object_transaction_id])
);
CREATE TABLE [dbo].[license_extension] (
    [license_id] int NOT NULL
,
    PRIMARY KEY ([license_id])
);
CREATE TABLE [dbo].[license_external_reference] (
    [license_external_reference_id] int IDENTITY(1,1) NOT NULL,
    [license_external_reference_vault_id] int,
    [license_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [effective_object_id] tinyint NOT NULL,
    [external_reference_value] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_external_reference_id])
);
CREATE TABLE [dbo].[license_external_reference_vault] (
    [license_external_reference_vault_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [vault_id] int NOT NULL,
    [is_provisioned] bit NOT NULL,
    [is_active] bit NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [billing_type] varchar(20),
    [storage_GB] int,
    [notes] nvarchar(MAX),
    [case_number] varchar(20)
,
    PRIMARY KEY ([license_external_reference_vault_id])
);
CREATE TABLE [dbo].[license_external_reference_vault_json_log] (
    [license_external_reference_vault_json_log_id] int IDENTITY(1,1) NOT NULL,
    [processing_procedure] varchar(50) NOT NULL,
    [input_json] nvarchar(MAX),
    [license_service_type_name] varchar(50),
    [license_external_reference_vault_id] int,
    [is_provisioned] bit,
    [is_active] bit,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_external_reference_vault_json_log_id])
);
CREATE TABLE [dbo].[license_feature] (
    [license_feature_id] int IDENTITY(1,1) NOT NULL,
    [license_feature_name] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_feature_id])
);
CREATE TABLE [dbo].[license_feature_license] (
    [license_feature_id] int NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [license_feature_license_id] int IDENTITY(1,1) NOT NULL,
    [license_feature_value] varchar(20)
,
    PRIMARY KEY ([license_feature_license_id])
);
CREATE TABLE [dbo].[license_feature_license_distribution_method] (
    [license_feature_id] int NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200)
,
    PRIMARY KEY ([license_distribution_method_id], [license_feature_id])
);
CREATE TABLE [dbo].[license_feature_license_history] (
    [license_feature_license_history_id] int IDENTITY(1,1) NOT NULL,
    [license_feature_id] int NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_feature_license_history_id])
);
CREATE TABLE [dbo].[license_gdstest] (
    [license_id] int IDENTITY(1,1) NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [customer_id] int,
    [product_line_id] int,
    [license_status_id] int NOT NULL,
    [license_type_id] int NOT NULL,
    [license_distribution_method_id] int,
    [license_keycode_type_id] int,
    [max_daily_activations] int NOT NULL,
    [max_child_licenses] int,
    [license_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[license_history] (
    [license_history_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [customer_id] int,
    [product_line_id] int NOT NULL,
    [license_status_id] int NOT NULL,
    [license_type_id] int NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [license_keycode_type_id] int NOT NULL,
    [max_daily_activations] int NOT NULL,
    [max_child_licenses] int,
    [license_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [license_change_reason_id] int NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_history_id])
);
CREATE TABLE [dbo].[license_install] (
    [license_install_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [first_install_date] datetime,
    [latest_install_date] datetime,
    [log_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_install_id])
);
CREATE TABLE [dbo].[license_install_old] (
    [license_install_id] int IDENTITY(1,1) NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [first_install_date] datetime,
    [latest_install_date] datetime,
    [log_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[license_install_update] (
    [license_install_update_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [keycode] varchar(40) NOT NULL,
    [first_install_date] datetime,
    [latest_install_date] datetime,
    [log_date] datetime,
    [update] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_install_update_id])
);
CREATE TABLE [dbo].[license_key] (
    [license_id] int NOT NULL,
    [license_key] uniqueidentifier NOT NULL,
    [salesforce_license_id] varchar(18)
,
    PRIMARY KEY ([license_id])
);
CREATE TABLE [dbo].[license_key_on_demand] (
    [license_key_on_demand_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_key] varchar(40)
);
CREATE TABLE [dbo].[license_keycode_alias] (
    [license_keycode_alias_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [keycode_alias] varchar(40) NOT NULL,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([license_keycode_alias_id])
);
CREATE TABLE [dbo].[license_keycode_alias_audit] (
    [license_keycode_alias_audit_id] int IDENTITY(1,1) NOT NULL,
    [license_keycode_alias_id] int NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [keycode_alias] varchar(40) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([license_keycode_alias_audit_id])
);
CREATE TABLE [dbo].[license_keycode_alias_update] (
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[license_keycode_type] (
    [license_keycode_type_id] int IDENTITY(1,1) NOT NULL,
    [license_keycode_type_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_keycode_type_id])
);
CREATE TABLE [dbo].[license_keycode_type_message_action] (
    [license_keycode_type_message_action_id] int IDENTITY(1,1) NOT NULL,
    [message_content_id] int NOT NULL,
    [license_keycode_type_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_keycode_type_message_action_id])
);
CREATE TABLE [dbo].[license_message] (
    [license_message_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [license_message_key] uniqueidentifier NOT NULL,
    [message_type_id] int NOT NULL,
    [message_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime,
    [end_date] datetime,
    [archive_date] datetime,
    [last_modified_date] datetime,
    [message_campaign_id] int,
    [message_campaign_class_id] tinyint,
    [update_attempts] int
,
    PRIMARY KEY ([license_message_id])
);
CREATE TABLE [dbo].[license_message_AR_duplicate] (
    [license_message_AR_duplicate_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [last_AR_order_date] datetime,
    [process_date] datetime,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_message_AR_duplicate_id])
);
CREATE TABLE [dbo].[license_message_archive] (
    [license_message_archive_id] int IDENTITY(1,1) NOT NULL,
    [license_message_id] int NOT NULL,
    [license_id] int,
    [license_message_key] uniqueidentifier NOT NULL,
    [message_type_id] int NOT NULL,
    [message_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime,
    [end_date] datetime,
    [archive_date] datetime,
    [license_message_archive_date] datetime,
    [message_campaign_id] int,
    [message_campaign_class_id] tinyint
,
    PRIMARY KEY ([license_message_archive_id])
);
CREATE TABLE [dbo].[license_message_check] (
    [license_message_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [license_id] int,
    [license_message_key] varchar(36) NOT NULL,
    [process_date] datetime,
    [end_date] datetime,
    [message_campaign_id] int NOT NULL,
    [message_platform_id] tinyint NOT NULL,
    [massage_platform_name] varchar(20) NOT NULL,
    [message_content_id] int NOT NULL,
    [serial_number] varchar(19),
    [contract_id] varchar(10),
    [cart_discount_id] int
);
CREATE TABLE [dbo].[license_message_check_12062019] (
    [license_message_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [license_message_key] uniqueidentifier NOT NULL,
    [message_type_id] int NOT NULL,
    [message_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime,
    [end_date] datetime,
    [archive_date] datetime,
    [last_modified_date] datetime,
    [message_campaign_id] int,
    [message_campaign_class_id] tinyint,
    [update_attempts] tinyint
);
CREATE TABLE [dbo].[license_message_check_archive] (
    [license_message_check_archive_id] int IDENTITY(1,1) NOT NULL,
    [license_message_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [license_id] int,
    [license_message_key] varchar(36) NOT NULL,
    [process_date] datetime,
    [end_date] datetime,
    [message_campaign_id] int NOT NULL,
    [message_platform_id] tinyint NOT NULL,
    [massage_platform_name] varchar(20) NOT NULL,
    [message_content_id] int NOT NULL,
    [serial_number] varchar(19),
    [contract_id] varchar(10),
    [cart_discount_id] int,
    [archive_date] datetime NOT NULL,
    [archive_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_message_check_archive_id])
);
CREATE TABLE [dbo].[license_message_check_update_license] (
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[license_message_failure] (
    [license_message_failure_id] int IDENTITY(1,1) NOT NULL,
    [license_message_id] int NOT NULL,
    [message_response_code] varchar(50) NOT NULL,
    [message_response_text] varchar(255),
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_message_failure_id])
);
CREATE TABLE [dbo].[license_message_update_license] (
    [license_id] int NOT NULL,
    [bulk_update] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [message_campaign_id] int,
    [license_message_value_str] varchar(MAX)
);
CREATE TABLE [dbo].[license_message_value] (
    [license_message_value_id] int IDENTITY(1,1) NOT NULL,
    [license_message_id] int NOT NULL,
    [message_value_type_id] tinyint NOT NULL,
    [value_id] int NOT NULL
,
    PRIMARY KEY ([license_message_value_id])
);
CREATE TABLE [dbo].[license_message_value_archive] (
    [license_message_value_archive_id] int IDENTITY(1,1) NOT NULL,
    [license_message_value_id] int NOT NULL,
    [license_message_id] int NOT NULL,
    [message_value_type_id] tinyint NOT NULL,
    [value_id] int NOT NULL
,
    PRIMARY KEY ([license_message_value_archive_id])
);
CREATE TABLE [dbo].[license_migration] (
    [license_migration_id] int IDENTITY(1,1) NOT NULL,
    [original_license_id] int NOT NULL,
    [new_license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_migration_id])
);
CREATE TABLE [dbo].[license_module] (
    [license_module_id] tinyint IDENTITY(1,1) NOT NULL,
    [license_module_code] varchar(50) NOT NULL,
    [license_module_name] varchar(50) NOT NULL,
    [license_module_class_id] tinyint NOT NULL,
    [license_module_status] varchar(20) NOT NULL,
    [parent_license_module_id] tinyint
,
    PRIMARY KEY ([license_module_id])
);
CREATE TABLE [dbo].[license_module_category] (
    [license_module_category_id] int IDENTITY(1,1) NOT NULL,
    [license_module_id] tinyint NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [is_module_string_included] bit
,
    PRIMARY KEY ([license_module_category_id])
);
CREATE TABLE [dbo].[license_module_class] (
    [license_module_class_id] tinyint IDENTITY(1,1) NOT NULL,
    [license_module_class_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([license_module_class_id])
);
CREATE TABLE [dbo].[license_module_license] (
    [license_module_license_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_module_id] tinyint NOT NULL,
    [license_module_type_id] tinyint NOT NULL,
    [start_date] datetime,
    [end_date] datetime,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [module_seats] int NOT NULL,
    [license_status_id] int
,
    PRIMARY KEY ([license_module_license_id])
);
CREATE TABLE [dbo].[license_module_license_history] (
    [license_module_license_history_id] int IDENTITY(1,1) NOT NULL,
    [license_module_license_id] int,
    [license_id] int NOT NULL,
    [license_module_id] tinyint,
    [license_module_type_id] tinyint,
    [start_date] datetime,
    [end_date] datetime,
    [last_modified_date] datetime,
    [last_modified_by] varchar(200),
    [order_item_id] int,
    [license_change_reason_id] int NOT NULL,
    [history_date] datetime NOT NULL,
    [module_seats] int,
    [license_status_id] int,
    [history_by] varchar(200)
,
    PRIMARY KEY ([license_module_license_history_id])
);
CREATE TABLE [dbo].[license_module_type] (
    [license_module_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [license_module_type_name] varchar(50) NOT NULL,
    [license_module_value_type_id] tinyint NOT NULL
,
    PRIMARY KEY ([license_module_type_id])
);
CREATE TABLE [dbo].[license_module_value_type] (
    [license_module_value_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [license_module_value_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([license_module_value_type_id])
);
CREATE TABLE [dbo].[license_next_bill_date] (
    [license_next_bill_date_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [next_bill_date] datetime NOT NULL,
    [last_modified_by] varchar(50) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [subscription_payment_status_id] tinyint
,
    PRIMARY KEY ([license_next_bill_date_id])
);
CREATE TABLE [dbo].[license_next_bill_date_audit] (
    [license_next_bill_date_audit_id] int IDENTITY(1,1) NOT NULL,
    [license_next_bill_date_id] int NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [next_bill_date] datetime NOT NULL,
    [last_modified_by] varchar(50) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL,
    [audit_by] varchar(50) NOT NULL,
    [subscription_payment_status_id] tinyint
,
    PRIMARY KEY ([license_next_bill_date_audit_id])
);
CREATE TABLE [dbo].[license_next_bill_date_status] (
    [license_next_bill_date_status_id] int IDENTITY(1,1) NOT NULL,
    [license_next_bill_date_status_description] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
);
CREATE TABLE [dbo].[license_next_bill_date_update] (
    [license_next_bill_date_update_id] int IDENTITY(1,1) NOT NULL,
    [keycode] varchar(40),
    [next_bill_date] datetime,
    [payment_status_name] varchar(20),
    [license_id] int,
    [license_next_bill_date_status_id] int,
    [last_modified_date] datetime,
    [insert_date] datetime,
    [AutoRenew] varchar(20),
    [IsAutoRenewChanged] int,
    [ZuoraUpdatedDate] date
);
CREATE TABLE [dbo].[license_parent] (
    [license_parent_id] int IDENTITY(1,1) NOT NULL,
    [parent_license_id] int NOT NULL,
    [child_license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_parent_id])
);
CREATE TABLE [dbo].[license_parent_audit] (
    [license_parent_audit_id] int IDENTITY(1,1) NOT NULL,
    [license_parent_id] int NOT NULL,
    [parent_license_id] int NOT NULL,
    [child_license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([license_parent_audit_id])
);
CREATE TABLE [dbo].[license_parent_device_status] (
    [license_parent_device_trending_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [parentkeycode] varchar(40) NOT NULL,
    [license_seats] int,
    [salesforce_license_id] varchar(18),
    [salesforce_account_id] varchar(18),
    [account_owner_id] varchar(18),
    [active_last_30_day_devices] int,
    [total_dbactivelast90days] int,
    [percent_drop] float,
    [processed] bit NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[license_parent_dimension] (
    [license_parent_dimension_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [capability_activation_days] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_parent_dimension_id])
);
CREATE TABLE [dbo].[license_parent_temp_insert_db2] (
    [license_parent_id] int NOT NULL,
    [parent_license_id] int NOT NULL,
    [child_license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[license_promo_campaign] (
    [license_promo_campaign_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [source_type] varchar(15) NOT NULL,
    [offer_type] varchar(15) NOT NULL,
    [product_id] int NOT NULL,
    [price] decimal(9,2),
    [days] int,
    [language_id] int NOT NULL,
    [country_id] int NOT NULL,
    [rc] int NOT NULL,
    [insert_date] datetime,
    [expiration_date] datetime,
    [sent_date] datetime,
    [activated_date] datetime
,
    PRIMARY KEY ([license_promo_campaign_id])
);
CREATE TABLE [dbo].[license_registration_invalid] (
    [license_registration_invalid_id] int IDENTITY(1,1) NOT NULL,
    [first_name] nvarchar(255),
    [last_name] nvarchar(255),
    [customer_email] varchar(100),
    [lang] varchar(3),
    [loc] varchar(3),
    [keycode] varchar(40),
    [insert_date] datetime
);
CREATE TABLE [dbo].[license_renewal_status] (
    [license_renewal_status_id] int NOT NULL,
    [license_renewal_status_description] varchar(100) NOT NULL
,
    PRIMARY KEY ([license_renewal_status_id])
);
CREATE TABLE [dbo].[license_renewals] (
    [license_id] int NOT NULL,
    [order_header_id_1] int
,
    PRIMARY KEY ([license_id])
);
CREATE TABLE [dbo].[license_scan_license] (
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [good_scans] bigint,
    [bad_scans] bigint,
    [insert_date] datetime NOT NULL,
    [modified_date] date NOT NULL
,
    PRIMARY KEY ([license_id])
);
CREATE TABLE [dbo].[license_seat] (
    [license_seat_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_seats] int NOT NULL,
    [seats_used] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [start_date] datetime,
    [end_date] datetime
,
    PRIMARY KEY ([license_seat_id])
);
CREATE TABLE [dbo].[license_seat_adjustment] (
    [license_seat_adjustment_id] int IDENTITY(1,1) NOT NULL,
    [license_seat_id] int NOT NULL,
    [added_seats] int NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [order_item_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_seat_adjustment_id])
);
CREATE TABLE [dbo].[license_seat_history] (
    [license_seat_history_id] int IDENTITY(1,1) NOT NULL,
    [license_seat_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_seats] int NOT NULL,
    [seats_used] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [license_change_reason_id] int NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(50) NOT NULL,
    [start_date] datetime,
    [end_date] datetime,
    [order_item_id] int
,
    PRIMARY KEY ([license_seat_history_id])
);
CREATE TABLE [dbo].[license_seat_ramp] (
    [license_seat_ramp_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_seats] nchar(10) NOT NULL,
    [effective_date] datetime NOT NULL,
    [processed] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [processed_date] datetime
,
    PRIMARY KEY ([license_seat_ramp_id])
);
CREATE TABLE [dbo].[license_serial_number] (
    [license_serial_number_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [serial_number] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_serial_number_id])
);
CREATE TABLE [dbo].[license_service] (
    [license_service_id] int IDENTITY(1,1) NOT NULL,
    [license_service_type_id] tinyint NOT NULL,
    [license_service_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime,
    [update_attempts] tinyint NOT NULL
,
    PRIMARY KEY ([license_service_id])
);
CREATE TABLE [dbo].[license_service_archive] (
    [license_service_archive_id] int IDENTITY(1,1) NOT NULL,
    [license_service_id] int NOT NULL,
    [license_service_type_id] tinyint NOT NULL,
    [license_service_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [archive_date] datetime NOT NULL
,
    PRIMARY KEY ([license_service_archive_id])
);
CREATE TABLE [dbo].[license_service_bulk_load_file] (
    [license_service_bulk_load_file_id] int IDENTITY(1,1) NOT NULL,
    [file_name] nvarchar(1000),
    [insert_by] varchar(200),
    [insert_date] datetime,
    [modified_by] varchar(200),
    [modified_date] datetime,
    [load_status] varchar(50)
);
CREATE TABLE [dbo].[license_service_bulk_update] (
    [license_service_bulk_update_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_service_type_id] tinyint NOT NULL,
    [expiration_date] datetime,
    [modified_by] varchar(100) NOT NULL,
    [update_status] varchar(10) NOT NULL,
    [license_service_bulk_load_file_id] int
,
    PRIMARY KEY ([license_service_bulk_update_id])
);
CREATE TABLE [dbo].[license_service_customer] (
    [license_service_customer_id] int IDENTITY(1,1) NOT NULL,
    [license_service_id] int NOT NULL,
    [first_name] nvarchar(225),
    [last_name] nvarchar(225),
    [customer_email] varchar(100),
    [company_name] nvarchar(255)
,
    PRIMARY KEY ([license_service_customer_id])
);
CREATE TABLE [dbo].[license_service_failure] (
    [license_service_failure_id] int IDENTITY(1,1) NOT NULL,
    [license_service_id] int NOT NULL,
    [license_service_type_id] tinyint NOT NULL,
    [license_service_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [failure_date] datetime NOT NULL
,
    PRIMARY KEY ([license_service_failure_id])
);
CREATE TABLE [dbo].[license_service_json] (
    [license_service_json_id] int IDENTITY(1,1) NOT NULL,
    [license_service_id] int NOT NULL,
    [license_service_json] nvarchar(MAX) NOT NULL
,
    PRIMARY KEY ([license_service_json_id])
);
CREATE TABLE [dbo].[license_service_license] (
    [license_service_license_id] int IDENTITY(1,1) NOT NULL,
    [license_service_id] int NOT NULL,
    [license_id] int NOT NULL
,
    PRIMARY KEY ([license_service_license_id])
);
CREATE TABLE [dbo].[license_service_status] (
    [license_service_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [license_service_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([license_service_status_id])
);
CREATE TABLE [dbo].[license_service_type] (
    [license_service_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [license_service_type_name] varchar(50) NOT NULL,
    [process_type] varchar(20),
    [license_service_type_description] nvarchar(MAX)
,
    PRIMARY KEY ([license_service_type_id])
);
CREATE TABLE [dbo].[license_service_value] (
    [license_service_value_id] int IDENTITY(1,1) NOT NULL,
    [license_service_id] int NOT NULL,
    [license_service_value_type_id] int NOT NULL,
    [license_service_value] nvarchar(255)
,
    PRIMARY KEY ([license_service_value_id])
);
CREATE TABLE [dbo].[license_service_value_type] (
    [license_service_value_type_id] int IDENTITY(1,1) NOT NULL,
    [value_type_name] varchar(50) NOT NULL,
    [value_data_type] varchar(20)
,
    PRIMARY KEY ([license_service_value_type_id])
);
CREATE TABLE [dbo].[license_service_value_type_alias] (
    [license_service_value_type_alias_id] int IDENTITY(1,1) NOT NULL,
    [license_service_value_type_id] int NOT NULL,
    [value_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([license_service_value_type_alias_id])
);
CREATE TABLE [dbo].[license_site_transfer_log] (
    [license_site_transfer_log_id] int IDENTITY(1,1) NOT NULL,
    [site_transfer_guid] uniqueidentifier,
    [site_transfer_license_service_id] int,
    [new_parent_license_id] int,
    [license_id] int,
    [keycode] varchar(50),
    [license_category_name] varchar(10),
    [expiration_date] datetime,
    [category_type_name] varchar(50),
    [item_hierarchy_id] tinyint,
    [license_status_id] int,
    [license_service_id] int,
    [license_service_type_name] varchar(50),
    [insert_date] datetime,
    [process_date] datetime
);
CREATE TABLE [dbo].[license_status] (
    [license_status_id] int NOT NULL,
    [license_status_description] varchar(20) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_status_id])
);
CREATE TABLE [dbo].[license_storage] (
    [license_storage_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [storage_gb] int NOT NULL,
    [storage_activation_date] datetime,
    [storage_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50),
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50)
,
    PRIMARY KEY ([license_storage_id])
);
CREATE TABLE [dbo].[license_storage_audit] (
    [license_storage_audit_id] int IDENTITY(1,1) NOT NULL,
    [license_storage_id] int NOT NULL,
    [license_id] int NOT NULL,
    [storage_gb] int NOT NULL,
    [storage_activation_date] datetime,
    [storage_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [audit_date] datetime NOT NULL,
    [audit_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_storage_audit_id])
);
CREATE TABLE [dbo].[license_storage_history] (
    [license_storage_history_id] int IDENTITY(1,1) NOT NULL,
    [license_storage_id] int NOT NULL,
    [license_id] int NOT NULL,
    [storage_gb] int NOT NULL,
    [storage_activation_date] datetime,
    [storage_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [license_change_reason_id] int NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL,
    [order_item_id] int
,
    PRIMARY KEY ([license_storage_history_id])
);
CREATE TABLE [dbo].[license_summary] (
    [license_id] int NOT NULL,
    [license_key] uniqueidentifier NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [license_type_id] tinyint NOT NULL,
    [license_status_id] tinyint NOT NULL,
    [product_line_id] smallint NOT NULL,
    [license_seats] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [opt_in] int NOT NULL,
    [autorenewal_opt_id] tinyint NOT NULL,
    [capability_id] tinyint NOT NULL,
    [capability_type_id] tinyint NOT NULL,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [subscription_id] int,
    [serial_number] varchar(19),
    [contract_id] varchar(10),
    [contract_detail_update] int,
    [subscription_type_id] tinyint,
    [summary_date] datetime,
    [keycode_alias] varchar(40),
    [customer_id] int
,
    PRIMARY KEY ([license_id])
);
CREATE TABLE [dbo].[license_summary_audit] (
    [license_id] int NOT NULL,
    [last_summary_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL,
    [data_update] tinyint
);
CREATE TABLE [dbo].[license_summary_update_license] (
    [license_id] int NOT NULL,
    [summary_date] datetime NOT NULL
);
CREATE TABLE [dbo].[license_summary_working] (
    [license_id] int NOT NULL,
    [license_key] uniqueidentifier NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [license_type_id] tinyint NOT NULL,
    [license_status_id] tinyint NOT NULL,
    [product_line_id] smallint NOT NULL,
    [license_seats] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [opt_in] int NOT NULL,
    [autorenewal_opt_id] tinyint NOT NULL,
    [capability_id] tinyint NOT NULL,
    [capability_type_id] tinyint NOT NULL,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [subscription_id] int,
    [serial_number] varchar(19),
    [contract_id] varchar(10),
    [contract_detail_update] int,
    [subscription_type_id] tinyint,
    [summary_date] datetime,
    [keycode_alias] varchar(40),
    [customer_id] int
);
CREATE TABLE [dbo].[license_swap] (
    [license_swap_id] int IDENTITY(1,1) NOT NULL,
    [original_license_id] int NOT NULL,
    [new_license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [original_days_remaining] int,
    [new_days_remaining] int,
    [modified_date] datetime,
    [license_swap_status_id] int
,
    PRIMARY KEY ([license_swap_id])
);
CREATE TABLE [dbo].[license_swap_history] (
    [license_swap_history_id] int IDENTITY(1,1) NOT NULL,
    [license_swap_id] int NOT NULL,
    [original_license_id] int NOT NULL,
    [new_license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [original_days_remaining] int,
    [new_days_remaining] int,
    [modified_date] datetime,
    [license_swap_status_id] int,
    [history_date] datetime NOT NULL
,
    PRIMARY KEY ([license_swap_history_id])
);
CREATE TABLE [dbo].[license_swap_invalid] (
    [license_swap_invalid_id] int IDENTITY(1,1) NOT NULL,
    [original_license_id] int NOT NULL,
    [new_license_id] int NOT NULL,
    [response_code] int,
    [message] varchar(100),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_swap_invalid_id])
);
CREATE TABLE [dbo].[license_swap_status] (
    [license_swap_status_id] int IDENTITY(1,1) NOT NULL,
    [license_swap_status_description] varchar(30) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_swap_status_id])
);
CREATE TABLE [dbo].[license_temp_insert_db2] (
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [customer_id] int,
    [product_line_id] int NOT NULL,
    [license_status_id] int NOT NULL,
    [license_type_id] int NOT NULL,
    [license_distribution_method_id] int,
    [license_keycode_type_id] int,
    [max_daily_activations] int NOT NULL,
    [max_child_licenses] int,
    [license_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[license_type] (
    [license_type_id] int IDENTITY(1,1) NOT NULL,
    [license_type_description] varchar(20) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([license_type_id])
);
CREATE TABLE [dbo].[license_upgrade_essen_to_complete] (
    [essen_updgrade_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [product_id] int,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[license_usage] (
    [license_id] int,
    [keycode] varchar(40),
    [activity_date] date,
    [devices] int,
    [current_seats] int,
    [product_line_id] int,
    [partition_bit] int
);
CREATE TABLE [dbo].[license_usage_license] (
    [license_id] int NOT NULL,
    [devices] int NOT NULL,
    [activity_date] date NOT NULL,
    [insert_date] datetime NOT NULL,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([license_id])
);
CREATE TABLE [dbo].[license_usage_pricing] (
    [license_usage_pricing_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [order_item_id] int NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime,
    [retail_price] money NOT NULL,
    [currency_id] tinyint NOT NULL,
    [pricing_status] varchar(10) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [license_category_id] tinyint
,
    PRIMARY KEY ([license_usage_pricing_id])
);
CREATE TABLE [dbo].[license_usage_pricing_audit] (
    [license_usage_pricing_audit_id] int IDENTITY(1,1) NOT NULL,
    [license_usage_pricing_id] int NOT NULL,
    [license_id] int NOT NULL,
    [order_item_id] int NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [retail_price] money NOT NULL,
    [currency_id] tinyint NOT NULL,
    [pricing_status] varchar(10) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL,
    [license_category_id] tinyint
,
    PRIMARY KEY ([license_usage_pricing_audit_id])
);
CREATE TABLE [dbo].[licensing_batch_copy] (
    [batch_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL
,
    PRIMARY KEY ([license_id])
);
CREATE TABLE [dbo].[licensing_insert_license_activation] (
    [licensing_insert_license_activation_id] int IDENTITY(1,1) NOT NULL,
    [max_license_activation_id] int NOT NULL,
    [start_date] datetime,
    [completed_date] datetime
);
CREATE TABLE [dbo].[lu_affiliate_categories] (
    [category_id] smallint NOT NULL,
    [description] varchar(50) NOT NULL,
    [last_modified] datetime NOT NULL,
    [assigned_num_min] int,
    [assigned_num_max] int
,
    PRIMARY KEY ([category_id])
);
CREATE TABLE [dbo].[lu_affiliate_status] (
    [affiliate_status_id] smallint NOT NULL,
    [description] varchar(100) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([affiliate_status_id])
);
CREATE TABLE [dbo].[lu_assigned_sales_tracking_codes] (
    [sales_tracking_category] varchar(32) NOT NULL,
    [assigned_code] varchar(32) NOT NULL,
    [description] varchar(64) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([assigned_code], [sales_tracking_category])
);
CREATE TABLE [dbo].[lu_business_types] (
    [business_type_id] smallint NOT NULL,
    [description] varchar(32),
    [last_modified] datetime NOT NULL,
    [business_type] varchar(50)
,
    PRIMARY KEY ([business_type_id])
);
CREATE TABLE [dbo].[lu_commission_types] (
    [commission_type_id] smallint NOT NULL,
    [description] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([commission_type_id])
);
CREATE TABLE [dbo].[lu_countries] (
    [country_id] smallint NOT NULL,
    [description] varchar(75) NOT NULL,
    [last_modified] datetime NOT NULL,
    [iso] varchar(2),
    [iso3] varchar(3),
    [web_display] tinyint,
    [parent_country_iso3] varchar(3),
    [parent_state_id] char(2),
    [iso_short_description] nvarchar(75),
    [vat_rate] float,
    [webroot_entity] varchar(3),
    [country_date_format] nvarchar(50),
    [is_vat_eligible] bit,
    [postal_code_format] varchar(75),
    [is_postal_code_required] bit
,
    PRIMARY KEY ([country_id])
);
CREATE TABLE [dbo].[lu_disti_by_incomm_merchant] (
    [incomm_merchant_id] tinyint IDENTITY(1,1) NOT NULL,
    [incomm_merchant_name] varchar(75) NOT NULL,
    [license_distribution_method_code] char(4) NOT NULL,
    [license_category] varchar(4),
    [product_id] int
);
CREATE TABLE [dbo].[lu_email_process] (
    [email_process_id] int IDENTITY(1,1) NOT NULL,
    [process_description] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[lu_email_recipient] (
    [email_recipient_id] int IDENTITY(1,1) NOT NULL,
    [email_process_id] int NOT NULL,
    [email_address] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[lu_income_levels] (
    [income_level_id] smallint NOT NULL,
    [description] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([income_level_id])
);
CREATE TABLE [dbo].[lu_incomm_upc_to_product_id] (
    [incomm_upc_id] tinyint IDENTITY(1,1) NOT NULL,
    [upc] varchar(20) NOT NULL,
    [product_id] int NOT NULL
);
CREATE TABLE [dbo].[lu_invoice_status] (
    [invoice_status_id] smallint NOT NULL,
    [description] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([invoice_status_id])
);
CREATE TABLE [dbo].[lu_keycode_status] (
    [status_id] tinyint NOT NULL,
    [description] varchar(50) NOT NULL
,
    PRIMARY KEY ([status_id])
);
CREATE TABLE [dbo].[lu_keycode_types] (
    [type_id] tinyint NOT NULL,
    [description] varchar(50) NOT NULL
,
    PRIMARY KEY ([type_id])
);
CREATE TABLE [dbo].[lu_language] (
    [language_id] int IDENTITY(1,1) NOT NULL,
    [language_code] char(2) NOT NULL,
    [description] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([language_id])
);
CREATE TABLE [dbo].[lu_leap_year] (
    [leap_year_id] int IDENTITY(1,1) NOT NULL,
    [leap_year] int NOT NULL,
    [leap_date] date NOT NULL
,
    PRIMARY KEY ([leap_year_id])
);
CREATE TABLE [dbo].[lu_linked_server] (
    [linked_server_id] int IDENTITY(1,1) NOT NULL,
    [linked_server_string] varchar(100) NOT NULL,
    [linked_server_name] varchar(50) NOT NULL,
    [description] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] nvarchar(200) NOT NULL
);
CREATE TABLE [dbo].[lu_modifiers] (
    [modifier_id] smallint NOT NULL,
    [description] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([modifier_id])
);
CREATE TABLE [dbo].[lu_note_types] (
    [note_type_id] smallint NOT NULL,
    [description] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([note_type_id])
);
CREATE TABLE [dbo].[lu_order_types] (
    [order_type_id] smallint NOT NULL,
    [description] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([order_type_id])
);
CREATE TABLE [dbo].[lu_password_clues] (
    [password_clue_id] smallint NOT NULL,
    [description] varchar(48) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([password_clue_id])
);
CREATE TABLE [dbo].[lu_payment_authorization_tablenames] (
    [payment_authorization_id] smallint NOT NULL,
    [description] varchar(48) NOT NULL,
    [table_name] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([payment_authorization_id])
);
CREATE TABLE [dbo].[lu_payment_methods] (
    [payment_method_id] smallint NOT NULL,
    [description] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([payment_method_id])
);
CREATE TABLE [dbo].[lu_Prefectures] (
    [state_id] char(2) NOT NULL,
    [description] nvarchar(50) NOT NULL,
    [last_modified] datetime NOT NULL
);
CREATE TABLE [dbo].[lu_product_groups] (
    [product_group_id] int NOT NULL,
    [description] varchar(48) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([product_group_id])
);
CREATE TABLE [dbo].[lu_qtyordollars] (
    [qtyordollars_id] smallint NOT NULL,
    [description] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([qtyordollars_id])
);
CREATE TABLE [dbo].[lu_referral_sources] (
    [referral_source_id] smallint NOT NULL,
    [description] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL,
    [active] tinyint
,
    PRIMARY KEY ([referral_source_id])
);
CREATE TABLE [dbo].[lu_refund_reasons] (
    [refund_reason_code] int IDENTITY(1,1) NOT NULL,
    [description] varchar(255) NOT NULL
,
    PRIMARY KEY ([refund_reason_code])
);
CREATE TABLE [dbo].[lu_sales_tracking_categories] (
    [sales_tracking_category] varchar(32) NOT NULL,
    [description] varchar(64) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([sales_tracking_category])
);
CREATE TABLE [dbo].[lu_shipping_status] (
    [shipping_status] tinyint NOT NULL,
    [description] varchar(50) NOT NULL
);
CREATE TABLE [dbo].[lu_site_categories] (
    [site_category_id] smallint NOT NULL,
    [description] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([site_category_id])
);
CREATE TABLE [dbo].[lu_spy_categories] (
    [category_id] numeric(10,0) NOT NULL,
    [cat_description] varchar(50)
);
CREATE TABLE [dbo].[lu_spy_threat_assessment] (
    [threat_assessment_id] numeric(10,0) NOT NULL,
    [th_description] varchar(50)
);
CREATE TABLE [dbo].[lu_states] (
    [admin_id] int IDENTITY(1,1) NOT NULL,
    [iso2] char(2) NOT NULL,
    [state_id] char(3) NOT NULL,
    [description] nvarchar(50) NOT NULL,
    [last_modified] datetime NOT NULL,
    [state_iso3] varchar(3),
    [external_description] varchar(50),
    [vat_rate] float
);
CREATE TABLE [dbo].[lu_time_periods] (
    [time_period_id] smallint NOT NULL,
    [description] varchar(32) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([time_period_id])
);
CREATE TABLE [dbo].[lu_TransactionProcess] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Name] varchar(20) NOT NULL,
    [Description] varchar(50),
    [DateInserted] datetime NOT NULL
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[lu_update_limits] (
    [update_limit_id] smallint NOT NULL,
    [description] varchar(100) NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([update_limit_id])
);
CREATE TABLE [dbo].[lu_version_compare] (
    [version_compare_id] int IDENTITY(1,1) NOT NULL,
    [compare_code] varchar(2) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([version_compare_id])
);
CREATE TABLE [dbo].[mar] (
    [mar_id] int NOT NULL,
    [mar_name] varchar(50) NOT NULL,
    [mar_type_id] tinyint NOT NULL,
    [mar_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([mar_id])
);
CREATE TABLE [dbo].[mar_audit] (
    [mar_audit_id] int IDENTITY(1,1) NOT NULL,
    [mar_id] int NOT NULL,
    [mar_name] varchar(50) NOT NULL,
    [mar_type_id] tinyint NOT NULL,
    [mar_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([mar_audit_id])
);
CREATE TABLE [dbo].[mar_license] (
    [mar_license_id] int IDENTITY(1,1) NOT NULL,
    [mar_id] int NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([mar_license_id])
);
CREATE TABLE [dbo].[mar_license_audit] (
    [mar_license_audit_id] int IDENTITY(1,1) NOT NULL,
    [mar_license_id] int NOT NULL,
    [mar_id] int NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([mar_license_audit_id])
);
CREATE TABLE [dbo].[mar_relation] (
    [mar_relation_id] int IDENTITY(1,1) NOT NULL,
    [parent_mar_id] int NOT NULL,
    [child_mar_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([mar_relation_id])
);
CREATE TABLE [dbo].[mar_relation_audit] (
    [mar_relation_audit_id] int IDENTITY(1,1) NOT NULL,
    [mar_relation_id] int NOT NULL,
    [parent_mar_id] int NOT NULL,
    [child_mar_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([mar_relation_audit_id])
);
CREATE TABLE [dbo].[mar_status] (
    [mar_status_id] tinyint NOT NULL,
    [mar_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([mar_status_id])
);
CREATE TABLE [dbo].[mar_type] (
    [mar_type_id] tinyint NOT NULL,
    [mar_type_code] varchar(10) NOT NULL,
    [mar_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([mar_type_id])
);
CREATE TABLE [dbo].[mar_user] (
    [mar_user_id] bigint NOT NULL,
    [user_email] varchar(100) NOT NULL,
    [user_password] varchar(64) NOT NULL,
    [mar_status_id] tinyint NOT NULL,
    [mar_id] int,
    [first_name] nvarchar(225),
    [last_name] nvarchar(225),
    [display_name] nvarchar(225),
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [mar_user_type_id] tinyint
,
    PRIMARY KEY ([mar_user_id])
);
CREATE TABLE [dbo].[mar_user_account] (
    [mar_user_account_id] int IDENTITY(1,1) NOT NULL,
    [mar_user_id] bigint NOT NULL,
    [account_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([mar_user_account_id])
);
CREATE TABLE [dbo].[mar_user_audit] (
    [mar_user_audit_id] int IDENTITY(1,1) NOT NULL,
    [mar_user_id] bigint NOT NULL,
    [user_email] varchar(100) NOT NULL,
    [user_password] varchar(64) NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [mar_status_id] tinyint NOT NULL,
    [insert_mar_user_id] bigint,
    [mar_id] int,
    [first_name] nvarchar(225),
    [last_name] nvarchar(225),
    [display_name] nvarchar(225),
    [audit_date] datetime NOT NULL,
    [mar_user_type_id] tinyint
,
    PRIMARY KEY ([mar_user_audit_id])
);
CREATE TABLE [dbo].[mar_user_ext] (
    [mar_user_ext_id] int IDENTITY(1,1) NOT NULL,
    [mar_user_id] bigint NOT NULL,
    [encryption_key_hash] varchar(128) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([mar_user_ext_id])
);
CREATE TABLE [dbo].[mar_user_license] (
    [mar_user_license_id] int IDENTITY(1,1) NOT NULL,
    [mar_user_id] bigint NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([mar_user_license_id])
);
CREATE TABLE [dbo].[mar_user_license_audit] (
    [mar_user_license_audit_id] int IDENTITY(1,1) NOT NULL,
    [mar_user_license_id] int NOT NULL,
    [mar_user_id] bigint NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([mar_user_license_audit_id])
);
CREATE TABLE [dbo].[mar_user_type] (
    [mar_user_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [mar_user_type_code] varchar(10) NOT NULL,
    [mar_user_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([mar_user_type_id])
);
CREATE TABLE [dbo].[merchant_category] (
    [merchant_category_id] int IDENTITY(1,1) NOT NULL,
    [merchant_category_description] varchar(50) NOT NULL
,
    PRIMARY KEY ([merchant_category_id])
);
CREATE TABLE [dbo].[merchant_category_merchant] (
    [merchant_category_merchant_id] int IDENTITY(1,1) NOT NULL,
    [payment_merchant_id] tinyint NOT NULL,
    [merchant_category_id] int NOT NULL
,
    PRIMARY KEY ([merchant_category_merchant_id])
);
CREATE TABLE [dbo].[merge_status] (
    [merge_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [merge_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([merge_status_id])
);
CREATE TABLE [dbo].[message_action] (
    [message_action_id] tinyint IDENTITY(1,1) NOT NULL,
    [message_action] varchar(50) NOT NULL,
    [message_action_description] varchar(200),
    [message_action_status] varchar(20)
,
    PRIMARY KEY ([message_action_id])
);
CREATE TABLE [dbo].[message_agent_state] (
    [message_agent_state_id] int IDENTITY(1,1) NOT NULL,
    [agent_state] varchar(15)
,
    PRIMARY KEY ([message_agent_state_id])
);
CREATE TABLE [dbo].[message_autorenewal_cycle] (
    [message_autorenewal_cycle_id] tinyint IDENTITY(1,1) NOT NULL,
    [autorenewal_cycle_name] varchar(50) NOT NULL,
    [autorenewal_cycle] float NOT NULL
,
    PRIMARY KEY ([message_autorenewal_cycle_id])
);
CREATE TABLE [dbo].[message_campaign] (
    [message_campaign_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_name] varchar(65),
    [message_campaign_description] varchar(256),
    [message_campaign_class_id] tinyint,
    [message_campaign_enabled] tinyint NOT NULL,
    [message_campaign_start_date] datetime NOT NULL,
    [message_campaign_end_date] datetime,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200),
    [message_campaign_key] uniqueidentifier
,
    PRIMARY KEY ([message_campaign_id])
);
CREATE TABLE [dbo].[message_campaign_account_creation_status] (
    [message_campaign_account_creation_status_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [account_creation_status] varchar(2) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_account_creation_status_id])
);
CREATE TABLE [dbo].[message_campaign_agent_behavior] (
    [message_campaign_agent_behavior_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [tray_state] varchar(15),
    [agent_state] varchar(15),
    [transactional] bit NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_agent_behavior_id])
);
CREATE TABLE [dbo].[message_campaign_allstate] (
    [message_campaign_allstate_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [dimension_type] varchar(50) NOT NULL,
    [dimension_value] int NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_allstate_id])
);
CREATE TABLE [dbo].[message_campaign_app_store_status] (
    [message_campaign_app_store_status_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [app_store_status] tinyint,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_app_store_status_id])
);
CREATE TABLE [dbo].[message_campaign_autorenewal] (
    [message_campaign_autorrenewal_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [autorenewal_opt_id] tinyint NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_autorrenewal_id])
);
CREATE TABLE [dbo].[message_campaign_build_campaign] (
    [message_campaign_build_campaign_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_class_id] tinyint NOT NULL,
    [campaign_dimension_header_name] varchar(50) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_build_campaign_id])
);
CREATE TABLE [dbo].[message_campaign_cart_discount] (
    [message_campaign_cart_discount_int] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [cart_discount_id] int NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_cart_discount_int])
);
CREATE TABLE [dbo].[message_campaign_child_license] (
    [message_campaign_child_license_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [dimension_type] varchar(50) NOT NULL,
    [dimension_value] int NOT NULL
,
    PRIMARY KEY ([message_campaign_child_license_id])
);
CREATE TABLE [dbo].[message_campaign_class] (
    [message_campaign_class_id] tinyint IDENTITY(1,1) NOT NULL,
    [message_campaign_class_name] varchar(50) NOT NULL,
    [trial] tinyint NOT NULL,
    [message_campaign_class_description] varchar(200),
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_class_id])
);
CREATE TABLE [dbo].[message_campaign_configuration] (
    [message_campaign_configuration_id] tinyint IDENTITY(1,1) NOT NULL,
    [configuration_name] varchar(50) NOT NULL,
    [status] varchar(10) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_configuration_id])
);
CREATE TABLE [dbo].[message_campaign_configuration_message_campaign] (
    [message_campaign_configuration_message_campaign_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [message_campaign_configuration_id] tinyint NOT NULL,
    [configuration_value] varchar(500) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_configuration_message_campaign_id])
);
CREATE TABLE [dbo].[message_campaign_configuration_value] (
    [message_campaign_configuration_value_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_configuration_id] tinyint NOT NULL,
    [configuration_value] varchar(500) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_configuration_value_id])
);
CREATE TABLE [dbo].[message_campaign_content] (
    [message_campaign_content_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [message_content_id] int NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_content_id])
);
CREATE TABLE [dbo].[message_campaign_contract_detail] (
    [message_campaign_contract_detail_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [contract_detail_update] varchar(10) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_contract_detail_id])
);
CREATE TABLE [dbo].[message_campaign_email_election] (
    [message_campaign_email_election_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [message_email_opt_id] tinyint NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_email_election_id])
);
CREATE TABLE [dbo].[message_campaign_enabled] (
    [message_campaign_enabled] tinyint NOT NULL,
    [message_campaign_enabled_description] varchar(50) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_enabled])
);
CREATE TABLE [dbo].[message_campaign_extension_json] (
    [message_campaign_extension_json_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [campaign_extension_json] nvarchar(MAX) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_extension_json_id])
);
CREATE TABLE [dbo].[message_campaign_language_location] (
    [message_campaign_language_location_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_language_location_id])
);
CREATE TABLE [dbo].[message_campaign_license_category] (
    [message_campaign_license_category_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [license_category_id] int,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_license_category_id])
);
CREATE TABLE [dbo].[message_campaign_license_channel] (
    [message_campaign_license_channel_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [license_channel_id] tinyint NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_license_channel_id])
);
CREATE TABLE [dbo].[message_campaign_license_distribution_method] (
    [message_campaign_license_distribution_method_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [inclusive] tinyint NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_license_distribution_method_id])
);
CREATE TABLE [dbo].[message_campaign_license_keycode_type] (
    [message_campaign_license_keycode_type_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [license_keycode_type_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_license_keycode_type_id])
);
CREATE TABLE [dbo].[message_campaign_license_status] (
    [message_campaign_license_status_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [license_status_id] varchar(10) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_license_status_id])
);
CREATE TABLE [dbo].[message_campaign_measure_point] (
    [message_campaign_measure_point_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [message_measure_point_id] tinyint NOT NULL,
    [message_measure_days] int NOT NULL,
    [message_measure_end_days] int,
    [message_duration_days] int,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_measure_point_id])
);
CREATE TABLE [dbo].[message_campaign_message_priority] (
    [message_campaign_message_priority_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [message_campaign_priority_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([message_campaign_message_priority_id])
);
CREATE TABLE [dbo].[message_campaign_platform] (
    [message_campaign_platform_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [message_platform_id] tinyint NOT NULL,
    [message_cycle_id] int,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_platform_id])
);
CREATE TABLE [dbo].[message_campaign_priority] (
    [message_campaign_priority_id] int NOT NULL,
    [message_campaign_priority_description] varchar(200) NOT NULL,
    [insert_date] smalldatetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([message_campaign_priority_id])
);
CREATE TABLE [dbo].[message_campaign_product_line] (
    [message_campaign_product_line_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [product_line_id] int NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_product_line_id])
);
CREATE TABLE [dbo].[message_campaign_renewal_status] (
    [message_campaign_renewal_status_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [renewal_status] tinyint,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_renewal_status_id])
);
CREATE TABLE [dbo].[message_campaign_seat] (
    [message_campaign_seat_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [license_seats] int NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_seat_id])
);
CREATE TABLE [dbo].[message_campaign_seat_available] (
    [message_campaign_seat_available_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [seat_available] int NOT NULL
,
    PRIMARY KEY ([message_campaign_seat_available_id])
);
CREATE TABLE [dbo].[message_campaign_seat_count_enforcement] (
    [message_campaign_seat_count_enforcement_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [seatcount_enforcement] bit
,
    PRIMARY KEY ([message_campaign_seat_count_enforcement_id])
);
CREATE TABLE [dbo].[message_campaign_seat_overage] (
    [message_campaign_seat_overage_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [seat_overage] bit
,
    PRIMARY KEY ([message_campaign_seat_overage_id])
);
CREATE TABLE [dbo].[message_campaign_security_exclusion] (
    [message_campaign_security_exclusion_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_security_exclusion_id])
);
CREATE TABLE [dbo].[message_campaign_sequence] (
    [message_campaign_sequence_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [next_message_campaign_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_id])
);
CREATE TABLE [dbo].[message_campaign_sfdc_data_extension] (
    [message_campaign_sfdc_data_extension_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [data_extension] varchar(100),
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_sfdc_data_extension_id])
);
CREATE TABLE [dbo].[message_campaign_subscription_days] (
    [message_campaign_subscription_days_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [subscription_days] tinyint,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_subscription_days_id])
);
CREATE TABLE [dbo].[message_campaign_subscription_type] (
    [message_campaign_subscription_type_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [subscription_type_id] int NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_subscription_type_id])
);
CREATE TABLE [dbo].[message_campaign_trial] (
    [message_campaign_trial_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [trial] tinyint NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_trial_id])
);
CREATE TABLE [dbo].[message_campaign_value] (
    [message_campaign_value_id] int IDENTITY(1,1) NOT NULL,
    [message_campaign_id] int NOT NULL,
    [message_value_type_id] int NOT NULL,
    [value_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([message_campaign_value_id])
);
CREATE TABLE [dbo].[message_content] (
    [message_content_id] int IDENTITY(1,1) NOT NULL,
    [message_content_name] varchar(50),
    [message_action] varchar(50),
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [message_content_image_id] tinyint,
    [message_content_status] varchar(20),
    [message_url] varchar(500),
    [language_code] varchar(2),
    [location_code] varchar(3)
,
    PRIMARY KEY ([message_content_id])
);
CREATE TABLE [dbo].[message_content_display] (
    [message_content_display_id] int IDENTITY(1,1) NOT NULL,
    [message_content_id] int NOT NULL,
    [offer_tagline] nvarchar(120),
    [offer_body] nvarchar(160),
    [offer_button] nvarchar(20),
    [image_url] varchar(500),
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [offer_body_short] nvarchar(90)
,
    PRIMARY KEY ([message_content_display_id])
);
CREATE TABLE [dbo].[message_content_display_content_display_ext] (
    [message_content_display_content_display_ext_id] int IDENTITY(1,1) NOT NULL,
    [message_content_display_id] int NOT NULL,
    [message_content_display_ext_id] int NOT NULL
,
    PRIMARY KEY ([message_content_display_content_display_ext_id])
);
CREATE TABLE [dbo].[message_content_display_ext] (
    [message_content_display_ext_id] int IDENTITY(1,1) NOT NULL,
    [replace_tag] varchar(20) NOT NULL,
    [replace_string] nvarchar(160) NOT NULL,
    [rule_parameter] varchar(20),
    [low_range] float,
    [high_range] float,
    [language_code] varchar(2),
    [location_code] varchar(3)
,
    PRIMARY KEY ([message_content_display_ext_id])
);
CREATE TABLE [dbo].[message_content_image] (
    [message_content_image_id] tinyint IDENTITY(1,1) NOT NULL,
    [image_name] varchar(50) NOT NULL,
    [image_url] varchar(500) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([message_content_image_id])
);
CREATE TABLE [dbo].[message_content_image_measure_point] (
    [message_content_image_measure_point_id] int IDENTITY(1,1) NOT NULL,
    [message_content_image_schedule_id] tinyint NOT NULL,
    [message_measure_point_id] tinyint NOT NULL,
    [message_measure_days] int NOT NULL,
    [message_measure_end_days] int NOT NULL,
    [message_content_image_id] tinyint NOT NULL
,
    PRIMARY KEY ([message_content_image_measure_point_id])
);
CREATE TABLE [dbo].[message_content_image_schedule] (
    [message_content_image_schedule_id] tinyint IDENTITY(1,1) NOT NULL,
    [image_schedule_desription] varchar(50) NOT NULL,
    [autorenewal_opt_id] tinyint NOT NULL
,
    PRIMARY KEY ([message_content_image_schedule_id])
);
CREATE TABLE [dbo].[message_content_image_schedule_content] (
    [message_content_image_schedule_content_id] int IDENTITY(1,1) NOT NULL,
    [message_content_image_schedule_id] tinyint NOT NULL,
    [message_content_id] int NOT NULL
,
    PRIMARY KEY ([message_content_image_schedule_content_id])
);
CREATE TABLE [dbo].[message_cycle] (
    [message_cycle_id] int IDENTITY(1,1) NOT NULL,
    [message_cycle_description] varchar(50) NOT NULL,
    [cycles] int NOT NULL,
    [cycle_duration_days] int NOT NULL,
    [cycle_limit_days] int
,
    PRIMARY KEY ([message_cycle_id])
);
CREATE TABLE [dbo].[message_cycle_line] (
    [message_cycle_line_id] int IDENTITY(1,1) NOT NULL,
    [message_cycle_id] int NOT NULL,
    [line_id] int NOT NULL,
    [cycle_gap_days] int
,
    PRIMARY KEY ([message_cycle_line_id])
);
CREATE TABLE [dbo].[message_measure_point] (
    [message_measure_point_id] tinyint IDENTITY(1,1) NOT NULL,
    [message_measure_point_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([message_measure_point_id])
);
CREATE TABLE [dbo].[message_platform] (
    [message_platform_id] tinyint IDENTITY(1,1) NOT NULL,
    [massage_platform_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([message_platform_id])
);
CREATE TABLE [dbo].[message_response] (
    [message_response_id] tinyint IDENTITY(1,1) NOT NULL,
    [message_response_code] varchar(50),
    [message_response_description] varchar(200) NOT NULL
,
    PRIMARY KEY ([message_response_id])
);
CREATE TABLE [dbo].[message_service] (
    [message_service_id] int IDENTITY(1,1) NOT NULL,
    [message_service_type_id] int NOT NULL,
    [message_service_client_id] int NOT NULL,
    [message_service_status_id] int NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([message_service_id])
);
CREATE TABLE [dbo].[message_service_archive] (
    [message_service_archive_id] int IDENTITY(1,1) NOT NULL,
    [message_service_id] int NOT NULL,
    [message_service_type_id] int NOT NULL,
    [message_service_client_id] int NOT NULL,
    [message_service_status_id] int NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [archive_date] datetime NOT NULL,
    [message_service_vendor_response_status] varchar(100)
,
    PRIMARY KEY ([message_service_archive_id])
);
CREATE TABLE [dbo].[message_service_client] (
    [message_service_client_id] int IDENTITY(1,1) NOT NULL,
    [client_name] nvarchar(255),
    [client_guid] uniqueidentifier NOT NULL,
    [client_email] varchar(100) NOT NULL,
    [message_service_client_status_id] int,
    [look_ups] int NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(20),
    [last_modified_date] datetime,
    [last_modified_by] varchar(20)
,
    PRIMARY KEY ([message_service_client_id])
);
CREATE TABLE [dbo].[message_service_client_log] (
    [message_service_client_id] int,
    [client_name] nvarchar(255),
    [client_guid] uniqueidentifier,
    [client_email] varchar(100),
    [message_service_client_status_id] int,
    [look_ups] int,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [last_modified_date] datetime,
    [last_modified_by] varchar(200)
);
CREATE TABLE [dbo].[message_service_client_status] (
    [message_service_client_status_id] int IDENTITY(1,1) NOT NULL,
    [client_status_name] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([message_service_client_status_id])
);
CREATE TABLE [dbo].[message_service_failure] (
    [message_service_failure_id] int IDENTITY(1,1) NOT NULL,
    [message_service_id] int NOT NULL,
    [message_service_type_id] int NOT NULL,
    [message_service_client_id] int NOT NULL,
    [message_service_status_id] int NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [failure_date] datetime NOT NULL
,
    PRIMARY KEY ([message_service_failure_id])
);
CREATE TABLE [dbo].[message_service_log_json] (
    [message_service_log_json_id] int IDENTITY(1,1) NOT NULL,
    [message_service_id] int NOT NULL,
    [message_service_json] nvarchar(MAX),
    [insert_date] datetime NOT NULL,
    [last_modified_date] datetime,
    [last_modified_by] varchar(200)
,
    PRIMARY KEY ([message_service_log_json_id])
);
CREATE TABLE [dbo].[message_service_platform] (
    [message_service_platform_id] int IDENTITY(1,1) NOT NULL,
    [message_service_platform_name] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([message_service_platform_id])
);
CREATE TABLE [dbo].[message_service_response] (
    [message_service_response_id] int IDENTITY(1,1) NOT NULL,
    [message_service_id] int NOT NULL,
    [message_service_response] varchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL,
    [recipient_send_id] varchar(255)
,
    PRIMARY KEY ([message_service_response_id])
);
CREATE TABLE [dbo].[message_service_status] (
    [message_service_status_id] int IDENTITY(1,1) NOT NULL,
    [message_service_status_name] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([message_service_status_id])
);
CREATE TABLE [dbo].[message_service_type] (
    [message_service_type_id] int IDENTITY(1,1) NOT NULL,
    [message_service_type_name] varchar(50) NOT NULL,
    [process_type] varchar(20) NOT NULL,
    [message_service_type_description] varchar(200),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [message_service_platform_id] int
,
    PRIMARY KEY ([message_service_type_id])
);
CREATE TABLE [dbo].[message_status] (
    [message_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [message_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([message_status_id])
);
CREATE TABLE [dbo].[message_type] (
    [message_type_id] int IDENTITY(1,1) NOT NULL,
    [message_type_name] varchar(50) NOT NULL,
    [message_type_description] varchar(200),
    [license_attribute_id] int
,
    PRIMARY KEY ([message_type_id])
);
CREATE TABLE [dbo].[message_value_type] (
    [message_value_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [message_value_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([message_value_type_id])
);
CREATE TABLE [dbo].[msn_churn_expiration_update] (
    [license_capability_id] int NOT NULL,
    [license_id] int NOT NULL,
    [capability_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_activation_days] int,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [days_used] int,
    [days_remaining] int,
    [days_added] int,
    [days_new] int,
    [days_contiguous] int,
    [update_complete] int
);
CREATE TABLE [dbo].[msn_expiration_extension] (
    [license_capability_id] int NOT NULL,
    [license_id] int NOT NULL,
    [capability_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_activation_days] int,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [days_used] int,
    [days_remaining] int,
    [days_added] int,
    [days_new] int,
    [days_contiguous] int,
    [update_complete] int
);
CREATE TABLE [dbo].[msn_keycode] (
    [msn_keycode_id] int IDENTITY(1,1) NOT NULL,
    [msn_hash_puid] varchar(64),
    [license_id] int,
    [insert_date] datetime NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [msn_keycode_status_id] tinyint NOT NULL
,
    PRIMARY KEY ([msn_keycode_id])
);
CREATE TABLE [dbo].[msn_keycode_sequence] (
    [msn_keycode_id] int IDENTITY(1,1) NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[nec_order] (
    [google_order_id] varchar(16),
    [merchant_order_id] varchar(100),
    [google_order_status_id] int,
    [keycode] varchar(40),
    [insert_date] datetime NOT NULL,
    [processed] int NOT NULL
);
CREATE TABLE [dbo].[nec_order_dup] (
    [google_order_id] varchar(16)
);
CREATE TABLE [dbo].[nec_order_working] (
    [temp_nec_id] int IDENTITY(1,1) NOT NULL,
    [google_order_id] varchar(16),
    [merchant_order_id] varchar(100),
    [google_order_status_id] int,
    [keycode] varchar(40),
    [insert_date] datetime NOT NULL,
    [processed] int NOT NULL
);
CREATE TABLE [dbo].[Netsuite_Contract_Backfill] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Netsuite_Contractid] int,
    [Netsuite_Contract_Lineid] int,
    [Netsuite_Salesorder_Number] varchar(250),
    [Netsuite_Salesorder_Linenumber] varchar(10),
    [Netsuite_Customernumber] varchar(250),
    [Netsuite_Customername] varchar(MAX),
    [Netsuite_Enduser_Customerno] varchar(250),
    [SAP_CustomerNumber] int,
    [SAP_MaterialNumber] int,
    [Billingid] varchar(150),
    [Ecom_Licenseid] int,
    [Ecom_Order_Header_id] int,
    [Ecom_Order_ItemId] int,
    [Ecom_License_Category_id] int,
    [Ecom_License_CategoryName] varchar(100),
    [Sap_Order_number] int,
    [Sap_Orderitem_id] int,
    [Ecom_Vendor_Order_Code] varchar(100),
    [Ecom_License_Keycode] varchar(100),
    [Ecomm_loaddate] datetime,
    [Enduser_SAPCustomerno] varchar(150),
    [EndOfTermAction] varchar(250)
,
    PRIMARY KEY ([Id])
);
CREATE TABLE [dbo].[Netsuite_Contract_Vault_Data] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Netsuite_Contract_Backfill_id] int,
    [Vault_id] int,
    [Vault_Name] varchar(100),
    [Vault_Datacenter_Key] varchar(100),
    [Vault_datacenter_name] varchar(150),
    [Carb_Number] varchar(150),
    [Carb_Status] varchar(50),
    [Carb_VaultPartnerId] varchar(250),
    [Carb_VaultPartner_Name] varchar(250),
    [Carb_IsTrialAccount] bit,
    [Carb_NFR] bit,
    [Carb_Customer_Shortname] varchar(250),
    [Carb_CompanyURL] varchar(250),
    [Carb_Account] varchar(150),
    [Carb_Notes] varchar(MAX),
    [RecordType] varchar(250),
    [Vault_url] varchar(150),
    [Sfdc_Product_account_id] varchar(250)
,
    PRIMARY KEY ([Id])
);
CREATE TABLE [dbo].[new_affiliate] (
    [rec_id] int IDENTITY(1,1) NOT NULL,
    [new_rc_code] varchar(30),
    [affiliate_category_id] int,
    [first_name] varchar(30),
    [last_name] varchar(30)
);
CREATE TABLE [dbo].[Numbers] (
    [Num] int IDENTITY(1,1) NOT NULL
,
    PRIMARY KEY ([Num])
);
CREATE TABLE [dbo].[operating_system] (
    [operating_system_id] int IDENTITY(1,1) NOT NULL,
    [operating_system_platform_id] int NOT NULL,
    [operating_system_major_version] int,
    [operating_system_minor_version] int,
    [operating_system_name] varchar(50) NOT NULL,
    [supported] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([operating_system_id])
);
CREATE TABLE [dbo].[oracle_company] (
    [oracle_company_id] int IDENTITY(1,1) NOT NULL,
    [oracle_customer_id] int NOT NULL,
    [company_name] nvarchar(255) NOT NULL,
    [company_name_clean] nvarchar(255) NOT NULL,
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [city] nvarchar(130),
    [state] varchar(3),
    [postal_code] nvarchar(32),
    [country_id] smallint,
    [salesforce_account_id] varchar(18),
    [insert_date] datetime,
    [insert_by] varchar(200),
    [company_id] int
,
    PRIMARY KEY ([oracle_company_id])
);
CREATE TABLE [dbo].[oracle_contract] (
    [oracle_contract_id] int NOT NULL,
    [oracle_contract_number] varchar(20) NOT NULL,
    [oracle_contract_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([oracle_contract_id])
);
CREATE TABLE [dbo].[oracle_contract_line] (
    [oracle_contract_line_id] int IDENTITY(1,1) NOT NULL,
    [oracle_contract_id] int NOT NULL,
    [oracle_line_id] varchar(40) NOT NULL,
    [oracle_contract_status_id] tinyint NOT NULL,
    [order_item_id] int,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([oracle_contract_line_id])
);
CREATE TABLE [dbo].[oracle_contract_line_audit] (
    [oracle_contract_line_audit_id] int IDENTITY(1,1) NOT NULL,
    [oracle_contract_line_id] int NOT NULL,
    [oracle_contract_id] int NOT NULL,
    [oracle_line_id] varchar(40) NOT NULL,
    [oracle_contract_status_id] tinyint NOT NULL,
    [order_item_id] int,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([oracle_contract_line_audit_id])
);
CREATE TABLE [dbo].[oracle_contract_line_license] (
    [oracle_contract_line_license_id] int IDENTITY(1,1) NOT NULL,
    [oracle_contract_line_id] int NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([oracle_contract_line_license_id])
);
CREATE TABLE [dbo].[oracle_contract_line_license_audit] (
    [oracle_contract_line_license_audit_id] int IDENTITY(1,1) NOT NULL,
    [oracle_contract_line_license_id] int NOT NULL,
    [oracle_contract_line_id] int NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([oracle_contract_line_license_audit_id])
);
CREATE TABLE [dbo].[oracle_contract_order] (
    [oracle_contract_order_id] int IDENTITY(1,1) NOT NULL,
    [oracle_contract_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([oracle_contract_order_id])
);
CREATE TABLE [dbo].[oracle_contract_status] (
    [oracle_contract_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [oracle_contract_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([oracle_contract_status_id])
);
CREATE TABLE [dbo].[oracle_customer_data] (
    [CUSTOMER_ID] numeric(15,0) NOT NULL,
    [CRM_ID] nvarchar(150),
    [account_number] nvarchar(30) NOT NULL,
    [customer_name] nvarchar(360) NOT NULL,
    [status] nvarchar(8),
    [org_id] numeric(15,0),
    [org_name] nvarchar(240) NOT NULL,
    [oracle_site_id] numeric(15,0) NOT NULL,
    [ADDRESS1] nvarchar(240) NOT NULL,
    [ADDRESS2] nvarchar(240),
    [ADDRESS3] nvarchar(240),
    [address4] nvarchar(240),
    [city] nvarchar(60),
    [county] nvarchar(60),
    [state] nvarchar(60),
    [province] nvarchar(60),
    [postal_code] nvarchar(60),
    [country] nvarchar(60) NOT NULL,
    [oracle_contact_id] numeric(15,0),
    [first_name] nvarchar(150),
    [middle_name] nvarchar(60),
    [last_name] nvarchar(150),
    [contact_point_type] nvarchar(30),
    [role_name] nvarchar(30),
    [EMAIL_ADDRESS] nvarchar(2000),
    [phone_number] nvarchar(60)
);
CREATE TABLE [dbo].[oracle_ext_customer_merge_data] (
    [oracle_ext_customer_merge_data_id] int IDENTITY(1,1) NOT NULL,
    [cust_merge_record_id] int NOT NULL,
    [record_creation_date] datetime NOT NULL,
    [cust_account_id] int NOT NULL,
    [account_number] varchar(50) NOT NULL,
    [party_id] int NOT NULL,
    [company_id] int,
    [merge_date] datetime,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[oracle_ext_customer_summary] (
    [oracle_ext_customer_summary_id] int IDENTITY(1,1) NOT NULL,
    [cust_summary_record_id] int NOT NULL,
    [record_creation_date] datetime NOT NULL,
    [cust_account_id] int NOT NULL,
    [account_number] varchar(50) NOT NULL,
    [party_id] int NOT NULL,
    [party_name] nvarchar(200) NOT NULL,
    [company_id] int,
    [currency_code] varchar(50),
    [amount_due_total] money,
    [amount_past_due] money,
    [amount_current] money,
    [amount_credits] money,
    [address_line_1] nvarchar(200),
    [address_line_2] nvarchar(200),
    [address_line_3] nvarchar(200),
    [address_line_4] nvarchar(200),
    [city] nvarchar(200),
    [state] nvarchar(200),
    [zip] nvarchar(200),
    [country] nvarchar(200),
    [insert_date] datetime NOT NULL,
    [tax_information] nvarchar(200)
);
CREATE TABLE [dbo].[oracle_ext_oracle_invoice_order_header] (
    [oracle_ext_oracle_invoice_order_header_id] int IDENTITY(1,1) NOT NULL,
    [oracle_transaction_number] varchar(100) NOT NULL,
    [order_header_id] int,
    [document_date] date,
    [transaction_amount] money,
    [account_number] varchar(50),
    [party_name] nvarchar(200)
,
    PRIMARY KEY ([oracle_ext_oracle_invoice_order_header_id])
);
CREATE TABLE [dbo].[oracle_ext_payment_summary] (
    [oracle_ext_payment_summary_id] int IDENTITY(1,1) NOT NULL,
    [payment_summary_record_id] int NOT NULL,
    [record_creation_date] datetime NOT NULL,
    [cust_account_id] int,
    [company_id] int,
    [payment_date] datetime NOT NULL,
    [payment_receipt_id] int,
    [currency_code] varchar(30),
    [payment_amount] money,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[oracle_ext_trans_balance] (
    [oracle_ext_trans_balance_id] int IDENTITY(1,1) NOT NULL,
    [balance_record_id] int NOT NULL,
    [record_creation_date] datetime NOT NULL,
    [transaction_id] int NOT NULL,
    [currency_code] varchar(50) NOT NULL,
    [transaction_amount] money NOT NULL,
    [remaining_balance] money NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[oracle_ext_trans_balance_audit] (
    [oracle_ext_trans_balance_audit_id] int IDENTITY(1,1) NOT NULL,
    [oracle_ext_trans_balance_id] int NOT NULL,
    [balance_record_id] int NOT NULL,
    [record_creation_date] datetime NOT NULL,
    [transaction_id] int NOT NULL,
    [currency_code] varchar(50) NOT NULL,
    [transaction_amount] money NOT NULL,
    [remaining_balance] money NOT NULL,
    [insert_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL,
    [oracle_update_id] int
,
    PRIMARY KEY ([oracle_ext_trans_balance_audit_id])
);
CREATE TABLE [dbo].[oracle_ext_trans_details] (
    [oracle_ext_trans_details_id] int IDENTITY(1,1) NOT NULL,
    [trans_details_record_id] int NOT NULL,
    [record_creation_date] datetime NOT NULL,
    [transaction_id] int NOT NULL,
    [transaction_line_id] int,
    [product_id] int,
    [product_code] varchar(100),
    [product_description] varchar(250),
    [trans_line_description] varchar(250) NOT NULL,
    [currency_code] varchar(50) NOT NULL,
    [line_number] int NOT NULL,
    [quantity] int,
    [uom] varchar(200),
    [line_amount] money NOT NULL,
    [tax_amount] money,
    [total_line_amount] money NOT NULL,
    [from_date] datetime,
    [to_date] datetime,
    [contract_term] varchar(250),
    [po_number] varchar(150),
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[oracle_ext_trans_summary] (
    [oracle_ext_trans_summary_id] int IDENTITY(1,1) NOT NULL,
    [trans_summary_record_id] int NOT NULL,
    [record_creation_date] datetime NOT NULL,
    [cust_account_id] int NOT NULL,
    [company_id] int,
    [transaction_id] int NOT NULL,
    [transaction_number] varchar(100) NOT NULL,
    [currency_code] varchar(50) NOT NULL,
    [transaction_amount] money NOT NULL,
    [pretax_amount] money NOT NULL,
    [tax_amount] money NOT NULL,
    [transaction_creation_date] datetime NOT NULL,
    [transaction_date] datetime NOT NULL,
    [due_date] datetime NOT NULL,
    [po_number] varchar(150),
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[oracle_invoice_payment_log] (
    [oracle_invoice_payment_log_id] int IDENTITY(1,1) NOT NULL,
    [oracle_invoice_number] int NOT NULL,
    [oracle_invoice_json] varchar(MAX) NOT NULL,
    [oracle_invoice_response_text] varchar(1000),
    [modified_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([oracle_invoice_payment_log_id])
);
CREATE TABLE [dbo].[oracle_template] (
    [oracle_template_id] tinyint IDENTITY(1,1) NOT NULL,
    [oracle_template_name] varchar(50) NOT NULL,
    [oracle_template_description] varchar(500) NOT NULL
,
    PRIMARY KEY ([oracle_template_id])
);
CREATE TABLE [dbo].[oracle_template_surpal] (
    [oracle_template_id] tinyint IDENTITY(1,1) NOT NULL,
    [oracle_template_name] varchar(50) NOT NULL,
    [oracle_template_description] varchar(500) NOT NULL
);
CREATE TABLE [dbo].[oracle_template_update_type] (
    [oracle_template_update_type_id] int IDENTITY(1,1) NOT NULL,
    [oracle_template_id] tinyint NOT NULL,
    [oracle_update_type_id] tinyint NOT NULL,
    [next_update_type_id] tinyint
,
    PRIMARY KEY ([oracle_template_update_type_id])
);
CREATE TABLE [dbo].[oracle_update] (
    [oracle_update_id] int IDENTITY(1,1) NOT NULL,
    [oracle_template_id] tinyint NOT NULL,
    [oracle_update_type_id] tinyint NOT NULL,
    [oracle_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [payment_header_id] int,
    [order_license_usage_id] int,
    [license_id] int
,
    PRIMARY KEY ([oracle_update_id])
);
CREATE TABLE [dbo].[oracle_update_archive] (
    [oracle_update_archive_id] int IDENTITY(1,1) NOT NULL,
    [oracle_update_id] int NOT NULL,
    [oracle_template_id] tinyint NOT NULL,
    [oracle_update_type_id] tinyint NOT NULL,
    [oracle_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [payment_header_id] int,
    [archive_date] datetime NOT NULL,
    [order_license_usage_id] int,
    [license_id] int
,
    PRIMARY KEY ([oracle_update_archive_id])
);
CREATE TABLE [dbo].[oracle_update_failure] (
    [oracle_update_failure_id] int IDENTITY(1,1) NOT NULL,
    [oracle_update_id] int NOT NULL,
    [oracle_template_id] tinyint NOT NULL,
    [oracle_update_type_id] tinyint NOT NULL,
    [oracle_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [payment_header_id] int,
    [failure_description] varchar(255) NOT NULL,
    [failure_date] datetime NOT NULL,
    [order_license_usage_id] int,
    [license_id] int
,
    PRIMARY KEY ([oracle_update_failure_id])
);
CREATE TABLE [dbo].[oracle_update_json] (
    [oracle_update_json_id] int IDENTITY(1,1) NOT NULL,
    [oracle_update_id] int NOT NULL,
    [oracle_update_json] nvarchar(MAX) NOT NULL
,
    PRIMARY KEY ([oracle_update_json_id])
);
CREATE TABLE [dbo].[oracle_update_status] (
    [oracle_update_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [oracle_update_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([oracle_update_status_id])
);
CREATE TABLE [dbo].[oracle_update_type] (
    [oracle_update_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [oracle_update_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([oracle_update_type_id])
);
CREATE TABLE [dbo].[oracle_update_type_surpal] (
    [oracle_update_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [oracle_update_type_name] varchar(50) NOT NULL
);
CREATE TABLE [dbo].[order_code] (
    [order_code_id] int IDENTITY(1,1) NOT NULL,
    [code_type_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [code_value] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_code_id])
);
CREATE TABLE [dbo].[order_company] (
    [order_company_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [company_id] int NOT NULL,
    [order_company_type_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [customer_id] int
,
    PRIMARY KEY ([order_company_id])
);
CREATE TABLE [dbo].[order_company_audit] (
    [order_company_audit_id] int IDENTITY(1,1) NOT NULL,
    [order_company_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [company_id] int NOT NULL,
    [order_company_type_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [customer_id] int,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([order_company_audit_id])
);
CREATE TABLE [dbo].[order_company_type] (
    [order_company_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [order_company_type_name] varchar(50) NOT NULL,
    [order_company_type_description] varchar(50)
,
    PRIMARY KEY ([order_company_type_id])
);
CREATE TABLE [dbo].[order_currency] (
    [order_header_id] int NOT NULL,
    [currency_id] tinyint NOT NULL
,
    PRIMARY KEY ([currency_id], [order_header_id])
);
CREATE TABLE [dbo].[order_currency_audit] (
    [order_currency_audit_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [currency_id] tinyint NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([order_currency_audit_id])
);
CREATE TABLE [dbo].[order_customer_profile_token] (
    [order_customer_profile_token_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [customer_profile_token] varchar(24) NOT NULL
,
    PRIMARY KEY ([order_customer_profile_token_id])
);
CREATE TABLE [dbo].[order_extension] (
    [order_extension_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [order_extension_type_id] tinyint NOT NULL,
    [order_extension_value] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([order_extension_id])
);
CREATE TABLE [dbo].[order_extension_audit] (
    [order_extension_audit_id] int IDENTITY(1,1) NOT NULL,
    [order_extension_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [order_extension_type_id] tinyint NOT NULL,
    [order_extension_value] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([order_extension_audit_id])
);
CREATE TABLE [dbo].[order_extension_default] (
    [order_extension_default_id] int IDENTITY(1,1) NOT NULL,
    [site_id] varchar(20) NOT NULL,
    [product_line_id] int,
    [license_attribute_license_value] int,
    [company_type_id] tinyint,
    [order_extension_type_id] tinyint NOT NULL,
    [order_extension_value] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[order_extension_type] (
    [order_extension_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [order_extension_type_name] varchar(50) NOT NULL,
    [order_extension_type_description] varchar(500) NOT NULL
,
    PRIMARY KEY ([order_extension_type_id])
);
CREATE TABLE [dbo].[order_extension_value] (
    [order_extension_value_id] int IDENTITY(1,1) NOT NULL,
    [order_extension_type_id] tinyint NOT NULL,
    [order_extension_value] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[order_external_load_exception] (
    [order_external_load_exception_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [exception_status] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [resolved_date] datetime,
    [exception_json] nvarchar(MAX),
    [completed_merge_customer] bit,
    [completed_merge_company] bit,
    [completed_primary_company_name_update] bit,
    [completed_secondary_company_name_add] bit,
    [completed_email_domain_type_update] bit,
    [completed_existing_company_association] bit,
    [completed_new_company_create] bit
,
    PRIMARY KEY ([order_external_load_exception_id])
);
CREATE TABLE [dbo].[order_fee] (
    [order_fee_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [payment_fee] money NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_fee_id])
);
CREATE TABLE [dbo].[order_header] (
    [order_header_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_id] int NOT NULL,
    [order_offer_id] varchar(65),
    [order_offer_amount] money,
    [total_amount] money NOT NULL,
    [sub_total_amount] money NOT NULL,
    [tax_total] money NOT NULL,
    [payment_method_id] int NOT NULL,
    [session_id] bigint,
    [vendor_order_date] datetime NOT NULL,
    [locale] char(5),
    [exchange_rate] money,
    [site_id] varchar(65),
    [site_url] varchar(64),
    [order_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [fee_total] money,
    [download_instruction_type_id] tinyint,
    [order_header_token] uniqueidentifier
,
    PRIMARY KEY ([order_header_id])
);
CREATE TABLE [dbo].[order_header_audit] (
    [order_header_audit_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [vendor_order_id] int NOT NULL,
    [order_offer_id] varchar(65),
    [order_offer_amount] money,
    [total_amount] money NOT NULL,
    [sub_total_amount] money NOT NULL,
    [tax_total] money NOT NULL,
    [payment_method_id] int NOT NULL,
    [session_id] bigint,
    [vendor_order_date] datetime NOT NULL,
    [locale] char(5),
    [exchange_rate] money,
    [site_id] varchar(65),
    [site_url] varchar(64),
    [order_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [fee_total] money,
    [download_instruction_type_id] tinyint,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([order_header_audit_id])
);
CREATE TABLE [dbo].[order_history] (
    [order_history_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [vendor_order_id] int NOT NULL,
    [order_offer_id] varchar(65),
    [order_offer_amount] money,
    [total_amount] money NOT NULL,
    [sub_total_amount] money NOT NULL,
    [tax_total] money NOT NULL,
    [payment_method_id] int NOT NULL,
    [session_id] bigint,
    [vendor_order_date] datetime NOT NULL,
    [locale] char(5) NOT NULL,
    [exchange_rate] money,
    [site_id] varchar(65),
    [site_url] varchar(64),
    [order_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_history_id])
);
CREATE TABLE [dbo].[order_item] (
    [order_item_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [line_item] int NOT NULL,
    [product_id] int NOT NULL,
    [quantity] int NOT NULL,
    [list_price] money NOT NULL,
    [unit_price] money NOT NULL,
    [tax] money,
    [order_item_offer_id] varchar(65),
    [order_item_offer_amount] money,
    [item_total] money,
    [tax_exempt] bit,
    [effective_date] datetime NOT NULL,
    [order_item_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [capability_expiration_date] datetime,
    [item_bundle_id] int,
    [order_item_update_type_id] tinyint,
    [item_hierarchy_id] tinyint,
    [sap_material_number] int
,
    PRIMARY KEY ([order_item_id])
);
CREATE TABLE [dbo].[order_item_audit] (
    [order_item_audit_id] int IDENTITY(1,1) NOT NULL,
    [order_item_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [line_item] int NOT NULL,
    [product_id] int NOT NULL,
    [quantity] int NOT NULL,
    [list_price] money NOT NULL,
    [unit_price] money NOT NULL,
    [tax] money,
    [order_item_offer_id] varchar(65),
    [order_item_offer_amount] money,
    [item_total] money,
    [tax_exempt] bit,
    [effective_date] datetime NOT NULL,
    [order_item_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [capability_expiration_date] datetime,
    [item_bundle_id] int,
    [order_item_update_type_id] tinyint,
    [audit_date] datetime NOT NULL,
    [sap_material_number] int
,
    PRIMARY KEY ([order_item_audit_id])
);
CREATE TABLE [dbo].[order_item_comment] (
    [order_item_cmment_id] int IDENTITY(1,1) NOT NULL,
    [order_item_id] int NOT NULL,
    [order_item_comment] varchar(255) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_item_cmment_id])
);
CREATE TABLE [dbo].[order_item_comment_load] (
    [order_item_comment_load_id] int IDENTITY(1,1) NOT NULL,
    [order_item_load_id] int NOT NULL,
    [comment] varchar(255) NOT NULL
,
    PRIMARY KEY ([order_item_comment_load_id])
);
CREATE TABLE [dbo].[order_item_comment_load_archive] (
    [order_item_comment_load_archive_id] int IDENTITY(1,1) NOT NULL,
    [order_item_comment_load_id] int NOT NULL,
    [order_item_load_id] int NOT NULL,
    [comment] varchar(255) NOT NULL
,
    PRIMARY KEY ([order_item_comment_load_archive_id])
);
CREATE TABLE [dbo].[order_item_json] (
    [order_item_json_id] int IDENTITY(1,1) NOT NULL,
    [order_item_id] int NOT NULL,
    [order_item_json] nvarchar(MAX) NOT NULL
,
    PRIMARY KEY ([order_item_json_id])
);
CREATE TABLE [dbo].[order_item_license] (
    [order_item_license_id] int IDENTITY(1,1) NOT NULL,
    [order_item_id] int NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_item_license_id])
);
CREATE TABLE [dbo].[order_item_license_audit] (
    [order_item_license_audit_id] int IDENTITY(1,1) NOT NULL,
    [order_item_license_id] int,
    [order_item_id] int NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([order_item_license_audit_id])
);
CREATE TABLE [dbo].[order_item_license_bulk_load] (
    [order_item_license_bulk_load_id] int IDENTITY(1,1) NOT NULL,
    [order_item_id] int NOT NULL,
    [license_bulk_load_id] int NOT NULL
,
    PRIMARY KEY ([order_item_license_bulk_load_id])
);
CREATE TABLE [dbo].[order_item_license_load] (
    [order_item_license_load_id] int IDENTITY(1,1) NOT NULL,
    [order_item_load_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [order_notification_status_id] int NOT NULL
,
    PRIMARY KEY ([order_item_license_load_id])
);
CREATE TABLE [dbo].[order_item_license_load_archive] (
    [order_item_license_load_archive_id] int IDENTITY(1,1) NOT NULL,
    [order_item_license_load_id] int NOT NULL,
    [order_item_load_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [order_notification_status_id] int NOT NULL
,
    PRIMARY KEY ([order_item_license_load_archive_id])
);
CREATE TABLE [dbo].[order_item_line_item_load] (
    [order_item_line_item_load_id] int IDENTITY(1,1) NOT NULL,
    [order_item_load_id] int NOT NULL,
    [line_item_big] bigint NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [order_notification_status_id] int NOT NULL
,
    PRIMARY KEY ([order_item_line_item_load_id])
);
CREATE TABLE [dbo].[order_item_line_item_load_archive] (
    [order_item_line_item_load_archive_id] int IDENTITY(1,1) NOT NULL,
    [order_item_line_item_load_id] int NOT NULL,
    [order_item_load_id] int NOT NULL,
    [line_item_big] bigint NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [order_notification_status_id] int NOT NULL
,
    PRIMARY KEY ([order_item_line_item_load_archive_id])
);
CREATE TABLE [dbo].[order_item_load] (
    [order_item_load_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_id] bigint NOT NULL,
    [vendor_id] int NOT NULL,
    [line_item] int NOT NULL,
    [vendor_product_id] int,
    [quantity] int NOT NULL,
    [order_item_offer_id] varchar(65),
    [order_item_offer_amount] money,
    [list_price] money NOT NULL,
    [unit_price] money NOT NULL,
    [tax_item_total] money NOT NULL,
    [tax_exempt] bit NOT NULL,
    [product_id] int NOT NULL,
    [conversion_product_id] int,
    [product_locale] char(5) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [order_notification_status_id] int NOT NULL
,
    PRIMARY KEY ([order_item_load_id])
);
CREATE TABLE [dbo].[order_item_load_archive] (
    [order_item_load_archive_id] int IDENTITY(1,1) NOT NULL,
    [order_item_load_id] int NOT NULL,
    [vendor_order_id] bigint NOT NULL,
    [vendor_id] int NOT NULL,
    [line_item] int NOT NULL,
    [vendor_product_id] int,
    [quantity] int NOT NULL,
    [order_item_offer_id] varchar(65),
    [order_item_offer_amount] money,
    [list_price] money NOT NULL,
    [unit_price] money NOT NULL,
    [tax_item_total] money NOT NULL,
    [tax_exempt] bit NOT NULL,
    [product_id] int NOT NULL,
    [conversion_product_id] int,
    [product_locale] char(5) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [order_notification_status_id] int NOT NULL
,
    PRIMARY KEY ([order_item_load_archive_id])
);
CREATE TABLE [dbo].[order_item_opportunity_line_item] (
    [order_item_opportunity_line_item_id] int IDENTITY(1,1) NOT NULL,
    [order_item_id] int NOT NULL,
    [opportunity_line_item_id] varchar(18) NOT NULL
,
    PRIMARY KEY ([order_item_opportunity_line_item_id])
);
CREATE TABLE [dbo].[order_item_opportunity_line_item_audit] (
    [order_item_opportunity_line_item_audit_id] int IDENTITY(1,1) NOT NULL,
    [order_item_opportunity_line_item_id] int,
    [order_item_id] int NOT NULL,
    [opportunity_line_item_id] varchar(18) NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([order_item_opportunity_line_item_audit_id])
);
CREATE TABLE [dbo].[order_item_rebate] (
    [order_item_rebate_id] int IDENTITY(1,1) NOT NULL,
    [order_item_id] int NOT NULL,
    [license_id] int,
    [rebate_id] int NOT NULL,
    [rebate_status_id] tinyint NOT NULL,
    [rebate_delivery_order_item_id] int,
    [rebate_delivery_guid] varchar(50),
    [rebate_alternate_id] int,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_item_rebate_id])
);
CREATE TABLE [dbo].[order_item_rebate_history] (
    [order_item_rebate_history_id] int IDENTITY(1,1) NOT NULL,
    [order_item_rebate_id] int NOT NULL,
    [order_item_id] int NOT NULL,
    [license_id] int,
    [rebate_id] int NOT NULL,
    [rebate_status_id] tinyint NOT NULL,
    [rebate_delivery_order_item_id] int,
    [rebate_delivery_guid] varchar(50),
    [rebate_alternate_id] int,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_item_rebate_history_id])
);
CREATE TABLE [dbo].[order_item_redemption] (
    [order_item_redemption_id] int IDENTITY(1,1) NOT NULL,
    [order_item_id] int NOT NULL,
    [redemption_code_id] int NOT NULL,
    [redemption_id] int NOT NULL,
    [status] tinyint NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_item_redemption_id])
);
CREATE TABLE [dbo].[order_item_status] (
    [order_item_status_id] int IDENTITY(1,1) NOT NULL,
    [order_item_status_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_item_status_id])
);
CREATE TABLE [dbo].[order_item_tax] (
    [order_item_tax_id] int IDENTITY(1,1) NOT NULL,
    [order_item_id] int NOT NULL,
    [tax_total_city_tax] money NOT NULL,
    [tax_total_county_tax] money NOT NULL,
    [tax_total_district_tax] money NOT NULL,
    [tax_total_state_tax] money NOT NULL,
    [tax_total_tax] money NOT NULL,
    [insert_date] datetime
,
    PRIMARY KEY ([order_item_tax_id])
);
CREATE TABLE [dbo].[order_item_update_type] (
    [order_item_update_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [order_item_update_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([order_item_update_type_id])
);
CREATE TABLE [dbo].[order_license_usage] (
    [order_license_usage_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [license_id] int NOT NULL,
    [usage_seats] int NOT NULL,
    [unit_price] money NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [license_message_id] int,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [license_seats] int,
    [license_attribute_license_value] int,
    [license_category_id] tinyint,
    [oracle_update_status_id] tinyint,
    [currency_id] int,
    [usage_pricing_model_id] tinyint,
    [usage] bigint
,
    PRIMARY KEY ([order_license_usage_id])
);
CREATE TABLE [dbo].[order_license_usage_accrual] (
    [order_license_usage_accrual_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [license_id] int NOT NULL,
    [usage_seats] int NOT NULL,
    [unit_price] money NOT NULL,
    [process_date] datetime NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [license_message_id] int,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [license_seats] int,
    [license_attribute_license_value] int,
    [license_category_id] tinyint,
    [usage_pricing_model_id] tinyint
,
    PRIMARY KEY ([order_license_usage_accrual_id])
);
CREATE TABLE [dbo].[order_license_usage_accrual_archive] (
    [order_license_usage_accrual_archive_id] int IDENTITY(1,1) NOT NULL,
    [order_license_usage_accrual_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [license_id] int NOT NULL,
    [usage_seats] int NOT NULL,
    [unit_price] money NOT NULL,
    [process_date] datetime NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [license_message_id] int,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [license_seats] int,
    [license_attribute_license_value] int,
    [license_category_id] tinyint,
    [usage_pricing_model_id] tinyint,
    [archive_date] datetime,
    [archive_by] varchar(200)
,
    PRIMARY KEY ([order_license_usage_accrual_archive_id])
);
CREATE TABLE [dbo].[order_license_usage_accrual_failure] (
    [order_license_usage_accrual_failure_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_message_id] int NOT NULL,
    [process_date] datetime NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [billing_day_of_month] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] nvarchar(200) NOT NULL
,
    PRIMARY KEY ([order_license_usage_accrual_failure_id])
);
CREATE TABLE [dbo].[order_license_usage_archive] (
    [order_license_usage_archive_id] int IDENTITY(1,1) NOT NULL,
    [order_license_usage_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [license_id] int NOT NULL,
    [usage_seats] int NOT NULL,
    [unit_price] money NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [license_message_id] int,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [archive_reason] nvarchar(255) NOT NULL,
    [archive_date] datetime NOT NULL,
    [license_seats] int,
    [license_attribute_license_value] int,
    [license_category_id] tinyint,
    [oracle_update_status_id] tinyint,
    [currency_id] int,
    [usage] bigint
,
    PRIMARY KEY ([order_license_usage_archive_id])
);
CREATE TABLE [dbo].[order_load] (
    [order_load_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_id] bigint NOT NULL,
    [vendor_id] int NOT NULL,
    [vendor_order_code] varchar(100),
    [order_type] varchar(30) NOT NULL,
    [site_id] varchar(65) NOT NULL,
    [site_url] varchar(1025) NOT NULL,
    [p_rc] varchar(50) NOT NULL,
    [p_rsc] varchar(50),
    [p_ac] varchar(100),
    [trx_rc] varchar(50),
    [trx_rsc] varchar(50),
    [trx_ac] varchar(100),
    [aid] varchar(50),
    [pid] varchar(50),
    [sid] varchar(100),
    [offer_id] varchar(65),
    [offer_amount] money,
    [total_amount] money NOT NULL,
    [sub_total_amount] money NOT NULL,
    [tax_amount] money,
    [payment_method] varchar(255) NOT NULL,
    [exchange_rate] money,
    [session_id] bigint NOT NULL,
    [submission_date] datetime NOT NULL,
    [sales_order_date] datetime,
    [locale] char(5) NOT NULL,
    [where_heard] varchar(64) NOT NULL,
    [test_order] bit NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [order_notification_status_id] int NOT NULL
,
    PRIMARY KEY ([order_load_id])
);
CREATE TABLE [dbo].[order_load_archive] (
    [order_load_archive_id] int IDENTITY(1,1) NOT NULL,
    [order_load_id] int NOT NULL,
    [vendor_order_id] bigint NOT NULL,
    [vendor_id] int NOT NULL,
    [vendor_order_code] varchar(100),
    [order_type] varchar(30) NOT NULL,
    [site_id] varchar(65) NOT NULL,
    [site_url] varchar(1025) NOT NULL,
    [p_rc] varchar(50) NOT NULL,
    [p_rsc] varchar(50),
    [p_ac] varchar(100),
    [trx_rc] varchar(50),
    [trx_rsc] varchar(50),
    [trx_ac] varchar(100),
    [aid] varchar(50),
    [pid] varchar(50),
    [sid] varchar(100),
    [offer_id] varchar(65),
    [offer_amount] money,
    [total_amount] money NOT NULL,
    [sub_total_amount] money NOT NULL,
    [tax_amount] money,
    [payment_method] varchar(255) NOT NULL,
    [exchange_rate] money,
    [session_id] bigint NOT NULL,
    [submission_date] datetime NOT NULL,
    [sales_order_date] datetime,
    [locale] char(5) NOT NULL,
    [where_heard] varchar(64) NOT NULL,
    [test_order] bit NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [order_notification_status_id] int NOT NULL
,
    PRIMARY KEY ([order_load_archive_id])
);
CREATE TABLE [dbo].[order_load_audit] (
    [order_load_audit_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_id] bigint NOT NULL,
    [vendor_id] int NOT NULL,
    [response_code] int NOT NULL,
    [message] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_load_audit_id])
);
CREATE TABLE [dbo].[order_load_error_definition] (
    [order_load_error_definition_id] int IDENTITY(1,1) NOT NULL,
    [order_load_error] varchar(50) NOT NULL,
    [order_load_error_description] varchar(100),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_load_error_definition_id])
);
CREATE TABLE [dbo].[order_load_status_definition] (
    [order_load_definition_id] int IDENTITY(1,1) NOT NULL,
    [order_load_status] varchar(50) NOT NULL,
    [order_load_status_description] varchar(100),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_load_definition_id])
);
CREATE TABLE [dbo].[order_message] (
    [order_message_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [message_key] varchar(36) NOT NULL,
    [insert_date] datetime NOT NULL,
    [cart_discount_id] int,
    [license_id] int,
    [message_campaign_id] int,
    [message_campaign_platform] varchar(50)
,
    PRIMARY KEY ([order_message_id])
);
CREATE TABLE [dbo].[order_notification_load] (
    [vendor_order_id] bigint NOT NULL,
    [submission_date] nvarchar(30) NOT NULL,
    [site_id] nvarchar(65),
    [total_amount] money,
    [sub_total_amount] money,
    [tax_total] money,
    [order_offer_amount] money,
    [exchange_rate] float,
    [payment_method] nvarchar(50),
    [bill_to_vendor_id] nvarchar(50),
    [bill_to_last_name] nvarchar(100),
    [bill_to_first_name] nvarchar(50),
    [bill_to_address1] nvarchar(255),
    [bill_to_address2] nvarchar(255),
    [bill_to_address3] nvarchar(255),
    [bill_to_phone_number] nvarchar(15),
    [bill_to_postal_code] varchar(32),
    [bill_to_city] nvarchar(25),
    [bill_to_state] nvarchar(2),
    [bill_to_email] nvarchar(64),
    [bill_to_fax] nvarchar(64),
    [bill_to_company_name] nvarchar(50),
    [bill_to_alt_phone] nvarchar(64),
    [bill_to_country] nvarchar(2),
    [line_item_id] nvarchar(50),
    [transaction_Description] nvarchar(25),
    [quantity] bigint,
    [vendor_product_id] nvarchar(100),
    [product_id] nvarchar(20),
    [product_locale] nvarchar(5),
    [ship_to_vendor_address_id] nvarchar(20),
    [ship_to_city] nvarchar(48),
    [ship_to_country] nvarchar(50),
    [ship_to_address1] nvarchar(255),
    [ship_to_address2] nvarchar(255),
    [ship_to_address3] nvarchar(255),
    [ship_to_last_name] nvarchar(50),
    [ship_to_first_name] nvarchar(50),
    [ship_to_phone_number] nvarchar(64),
    [ship_to_postal_code] nvarchar(32),
    [ship_to_state] nvarchar(2),
    [ship_to_email] nvarchar(64),
    [ship_to_fax] nvarchar(64),
    [ship_to_company_name] nvarchar(64),
    [ship_to_alt_phone] nvarchar(64),
    [line_unit_price] money,
    [line_list_price] money,
    [line_price_per_qty] money,
    [line_tax_amount] money,
    [tax_exempt] nvarchar(50),
    [line_offer_id] nvarchar(50),
    [line_offer_amount] money,
    [key_code] nvarchar(100),
    [Line_value_name] nvarchar(40),
    [line_value] nvarchar(60),
    [Program_id] nvarchar(20),
    [bill_to_customer_id] nvarchar(20),
    [order_offer_id] nvarchar(50),
    [order_locale] nvarchar(10),
    [opt_in] nvarchar(10),
    [where_heard] nvarchar(50),
    [test_order_ind] nvarchar(10),
    [order_value_name] nvarchar(25),
    [order_value] nvarchar(100),
    [order_load_status] nvarchar(50),
    [server_loc] nvarchar(32),
    [ts] datetime,
    [process_flag] bit
);
CREATE TABLE [dbo].[order_notification_load_archive] (
    [vendor_order_id] bigint NOT NULL,
    [submission_date] nvarchar(30) NOT NULL,
    [site_id] nvarchar(65),
    [total_amount] money,
    [sub_total_amount] money,
    [tax_total] money,
    [order_offer_amount] money,
    [exchange_rate] float,
    [payment_method] nvarchar(50),
    [bill_to_vendor_id] nvarchar(50),
    [bill_to_last_name] nvarchar(100),
    [bill_to_first_name] nvarchar(50),
    [bill_to_address1] nvarchar(255),
    [bill_to_address2] nvarchar(255),
    [bill_to_address3] nvarchar(255),
    [bill_to_phone_number] nvarchar(15),
    [bill_to_postal_code] varchar(32),
    [bill_to_city] nvarchar(25),
    [bill_to_state] nvarchar(2),
    [bill_to_email] nvarchar(64),
    [bill_to_fax] nvarchar(64),
    [bill_to_company_name] nvarchar(50),
    [bill_to_alt_phone] nvarchar(64),
    [bill_to_country] nvarchar(2),
    [line_item_id] nvarchar(50),
    [transaction_Description] nvarchar(25),
    [quantity] bigint,
    [vendor_product_id] nvarchar(100),
    [product_id] nvarchar(20),
    [product_locale] nvarchar(5),
    [ship_to_vendor_address_id] nvarchar(20),
    [ship_to_city] nvarchar(48),
    [ship_to_country] nvarchar(50),
    [ship_to_address1] nvarchar(255),
    [ship_to_address2] nvarchar(255),
    [ship_to_address3] nvarchar(255),
    [ship_to_last_name] nvarchar(50),
    [ship_to_first_name] nvarchar(50),
    [ship_to_phone_number] nvarchar(64),
    [ship_to_postal_code] nvarchar(32),
    [ship_to_state] nvarchar(2),
    [ship_to_email] nvarchar(64),
    [ship_to_fax] nvarchar(64),
    [ship_to_company_name] nvarchar(64),
    [ship_to_alt_phone] nvarchar(64),
    [line_unit_price] money,
    [line_list_price] money,
    [line_price_per_qty] money,
    [line_tax_amount] money,
    [tax_exempt] nvarchar(50),
    [line_offer_id] nvarchar(50),
    [line_offer_amount] money,
    [key_code] nvarchar(100),
    [Line_value_name] varchar(40),
    [line_value] nvarchar(60),
    [Program_id] nvarchar(20),
    [bill_to_customer_id] nvarchar(20),
    [order_offer_id] nvarchar(50),
    [order_locale] nvarchar(10),
    [opt_in] nvarchar(10),
    [where_heard] nvarchar(50),
    [test_order_ind] nvarchar(10),
    [order_value_name] nvarchar(25),
    [order_value] nvarchar(100),
    [order_load_status] nvarchar(50),
    [server_loc] nvarchar(32),
    [ts] datetime,
    [process_flag] bit
);
CREATE TABLE [dbo].[order_notification_load_retry] (
    [vendor_order_id] bigint NOT NULL,
    [order_load_status] nvarchar(250)
);
CREATE TABLE [dbo].[order_notification_status] (
    [order_notification_status_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_id] bigint NOT NULL,
    [vendor_id] int NOT NULL,
    [order_load_definition_id] int NOT NULL,
    [order_load_error_definition_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_notification_status_id])
);
CREATE TABLE [dbo].[order_notification_status_archive] (
    [order_notification_status_archive_id] int IDENTITY(1,1) NOT NULL,
    [order_notification_status_id] int NOT NULL,
    [vendor_order_id] bigint NOT NULL,
    [vendor_id] int NOT NULL,
    [order_load_definition_id] int NOT NULL,
    [order_load_error_definition_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[order_opportunity] (
    [order_opportunity_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [salesforce_opportunity_id] varchar(20) NOT NULL,
    [oracle_contract_number] varchar(20),
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200),
    [order_opportunity_type_id] int
,
    PRIMARY KEY ([order_opportunity_id])
);
CREATE TABLE [dbo].[order_opportunity_audit] (
    [order_opportunity_audit_id] int IDENTITY(1,1) NOT NULL,
    [order_opportunity_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [salesforce_opportunity_id] varchar(20) NOT NULL,
    [oracle_contract_number] varchar(20),
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200),
    [audit_by] varchar(200),
    [audit_date] datetime,
    [order_opportunity_type_id] int
,
    PRIMARY KEY ([order_opportunity_audit_id])
);
CREATE TABLE [dbo].[order_opportunity_type] (
    [order_opportunity_type_id] int NOT NULL,
    [order_opportunity_type_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_opportunity_type_id])
);
CREATE TABLE [dbo].[order_payment_header] (
    [order_payment_header_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [payment_header_id] int NOT NULL,
    [oracle_invoice_number] int,
    [oracle_receipt_id] int,
    [sap_invoice_number] varchar(20)
,
    PRIMARY KEY ([order_payment_header_id])
);
CREATE TABLE [dbo].[order_post_process_family] (
    [order_post_process_family] int IDENTITY(1,1) NOT NULL,
    [priority] int NOT NULL,
    [product_family_id] int NOT NULL,
    [procedure_name] varchar(100) NOT NULL,
    [description] varchar(255)
,
    PRIMARY KEY ([order_post_process_family])
);
CREATE TABLE [dbo].[order_refunds_batch_refunds] (
    [order_refund_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_code] varchar(100) NOT NULL,
    [processed] bit NOT NULL
);
CREATE TABLE [dbo].[order_sequence] (
    [order_sequence_id] bigint IDENTITY(1,1) NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[order_status] (
    [order_status_id] int IDENTITY(1,1) NOT NULL,
    [order_status_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_status_id])
);
CREATE TABLE [dbo].[order_technical_contact] (
    [order_technical_contact_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [customer_id] int NOT NULL
,
    PRIMARY KEY ([order_technical_contact_id])
);
CREATE TABLE [dbo].[order_type] (
    [order_type_id] int IDENTITY(1,1) NOT NULL,
    [order_type] varchar(30) NOT NULL,
    [order_type_description] varchar(255),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_type_id])
);
CREATE TABLE [dbo].[order_type_group] (
    [order_type_group_id] int IDENTITY(1,1) NOT NULL,
    [order_type_group] varchar(30) NOT NULL,
    [order_type_group_description] varchar(255),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_type_group_id])
);
CREATE TABLE [dbo].[order_type_group_member] (
    [order_type_group_member_id] int IDENTITY(1,1) NOT NULL,
    [order_type_id] int NOT NULL,
    [order_type_group_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([order_type_group_member_id])
);
CREATE TABLE [dbo].[partner] (
    [partner_id] int IDENTITY(1,1) NOT NULL,
    [partner_name] nvarchar(100) NOT NULL,
    [partner_type_id] tinyint NOT NULL,
    [partner_status_id] tinyint NOT NULL,
    [partner_key] uniqueidentifier NOT NULL,
    [parent_partner_id] int,
    [salesforce_id] varchar(20),
    [oracle_id] varchar(20),
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [account_owner_id] varchar(18)
,
    PRIMARY KEY ([partner_id])
);
CREATE TABLE [dbo].[partner_account] (
    [partner_account_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [account_id] int NOT NULL,
    [partner_account_key] uniqueidentifier NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [shared_account] tinyint NOT NULL,
    [partner_account_status_id] tinyint NOT NULL
,
    PRIMARY KEY ([partner_account_id])
);
CREATE TABLE [dbo].[partner_account_reset] (
    [partner_account_reset_id] int IDENTITY(1,1) NOT NULL,
    [partner_account_reset_key] uniqueidentifier NOT NULL,
    [partner_account_key] uniqueidentifier NOT NULL,
    [expiration_date] datetime NOT NULL,
    [reset_status] tinyint NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_account_reset_id])
);
CREATE TABLE [dbo].[partner_account_status] (
    [partner_account_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [partner_account_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([partner_account_status_id])
);
CREATE TABLE [dbo].[partner_billing] (
    [partner_billing_id] int IDENTITY(1,1) NOT NULL,
    [partner_billing_report_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [customer_code] varchar(64) NOT NULL,
    [company_name] nvarchar(100) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [state] nvarchar(3) NOT NULL,
    [webroot_entity] varchar(3) NOT NULL,
    [reseller_customer_id] int,
    [reseller_customer_code] varchar(64),
    [reseller_company_name] nvarchar(100),
    [reseller_location_code] varchar(3),
    [reseller_state] nvarchar(3),
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [keycode_age] int NOT NULL,
    [order_header_id] int NOT NULL,
    [order_date] datetime NOT NULL,
    [product_id] int,
    [license_category_id] tinyint,
    [license_seats] int NOT NULL,
    [retail_price] money NOT NULL,
    [extended_amount] money NOT NULL,
    [usage_seats] int NOT NULL,
    [usage_price] money NOT NULL,
    [usage_extended_amount] money NOT NULL,
    [total_extended_amount] money NOT NULL,
    [cap_amount] money NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [license_attribute_license_value] int,
    [total_cap_amount] money,
    [currency_id] int,
    [partner_pricing_model_id] tinyint,
    [total_seats] int,
    [company_id] int
,
    PRIMARY KEY ([partner_billing_id])
);
CREATE TABLE [dbo].[partner_billing_audit] (
    [partner_billing_audit_id] int IDENTITY(1,1) NOT NULL,
    [partner_billing_id] int NOT NULL,
    [partner_billing_report_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [customer_code] varchar(64) NOT NULL,
    [company_name] nvarchar(100) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [state] nvarchar(2) NOT NULL,
    [webroot_entity] varchar(3) NOT NULL,
    [reseller_customer_id] int,
    [reseller_customer_code] varchar(64),
    [reseller_company_name] nvarchar(100),
    [reseller_location_code] varchar(3),
    [reseller_state] nvarchar(2),
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [keycode_age] int NOT NULL,
    [order_header_id] int NOT NULL,
    [order_date] datetime NOT NULL,
    [product_id] int,
    [license_category_id] tinyint,
    [license_seats] int NOT NULL,
    [retail_price] money NOT NULL,
    [extended_amount] money NOT NULL,
    [usage_seats] int NOT NULL,
    [usage_price] money NOT NULL,
    [usage_extended_amount] money NOT NULL,
    [total_extended_amount] money NOT NULL,
    [cap_amount] money NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [license_attribute_license_value] int,
    [total_cap_amount] money,
    [currency_id] int,
    [partner_pricing_model_id] tinyint,
    [total_seats] int,
    [audit_date] datetime NOT NULL,
    [company_id] int
,
    PRIMARY KEY ([partner_billing_audit_id])
);
CREATE TABLE [dbo].[partner_billing_configuration] (
    [partner_billing_configuration_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [site_id] varchar(20) NOT NULL,
    [configuration_json] nvarchar(MAX) NOT NULL,
    [configuration_status] varchar(20) NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_billing_configuration_id])
);
CREATE TABLE [dbo].[partner_billing_failure] (
    [partner_billing_failure_id] int IDENTITY(1,1) NOT NULL,
    [partner_billing_report_id] int NOT NULL,
    [customer_id] int,
    [customer_code] varchar(64),
    [company_name] nvarchar(100),
    [location_code] varchar(3),
    [state] nvarchar(2),
    [webroot_entity] varchar(3),
    [reseller_customer_id] int,
    [reseller_customer_code] varchar(64),
    [reseller_company_name] nvarchar(100),
    [reseller_location_code] varchar(3),
    [reseller_state] nvarchar(2),
    [license_id] int,
    [keycode] varchar(40),
    [keycode_age] int,
    [order_header_id] int,
    [order_date] datetime,
    [product_id] int,
    [license_category_id] tinyint,
    [license_seats] int,
    [retail_price] money,
    [extended_amount] money,
    [usage_seats] int,
    [usage_price] money,
    [usage_extended_amount] money,
    [total_extended_amount] money,
    [cap_amount] money,
    [last_modified_date] datetime,
    [failure_date] datetime,
    [failure_reason] varchar(100),
    [license_attribute_license_value] int,
    [total_cap_amount] money,
    [currency_id] int,
    [partner_pricing_model_id] tinyint,
    [total_seats] int,
    [company_id] int
,
    PRIMARY KEY ([partner_billing_failure_id])
);
CREATE TABLE [dbo].[partner_billing_license_attribute] (
    [partner_billing_license_attribute_id] int IDENTITY(1,1) NOT NULL,
    [partner_billing_report_id] int NOT NULL,
    [license_attribute_id] int NOT NULL,
    [license_attribute_license_value] int
,
    PRIMARY KEY ([partner_billing_license_attribute_id])
);
CREATE TABLE [dbo].[partner_billing_order] (
    [partner_billing_order_id] int IDENTITY(1,1) NOT NULL,
    [partner_billing_report_id] int NOT NULL,
    [order_header_id] int NOT NULL
,
    PRIMARY KEY ([partner_billing_order_id])
);
CREATE TABLE [dbo].[partner_billing_report] (
    [partner_billing_report_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [partner_billing_report_name] varchar(100) NOT NULL,
    [partner_billing_type_id] tinyint NOT NULL,
    [partner_billing_status_id] tinyint NOT NULL,
    [billing_date] datetime NOT NULL,
    [partner_billing_report_key] uniqueidentifier NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [site_id] varchar(20)
,
    PRIMARY KEY ([partner_billing_report_id])
);
CREATE TABLE [dbo].[partner_billing_report_override] (
    [partner_billing_report_override_id] int IDENTITY(1,1) NOT NULL,
    [partner_billing_report_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [partner_billing_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_billing_report_override_id])
);
CREATE TABLE [dbo].[partner_billing_revenue_fact] (
    [partner_billing_revenue_fact_id] int IDENTITY(1,1) NOT NULL,
    [report_date] date NOT NULL,
    [partner_id] int NOT NULL,
    [site_id] varchar(20) NOT NULL,
    [currency_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [current_billing_date] date,
    [current_license_seats] int,
    [current_unit_price] decimal(10,2),
    [current_total] decimal(10,2),
    [current_usage_seats] int,
    [current_usage_price] decimal(10,2),
    [current_usage_total] decimal(10,2),
    [current_billing_total] decimal(10,2),
    [prior_billing_date] date,
    [prior_license_seats] int,
    [prior_unit_price] decimal(10,2),
    [prior_total] decimal(10,2),
    [prior_usage_seats] int,
    [prior_usage_price] decimal(10,2),
    [prior_usage_total] decimal(10,2),
    [prior_billing_total] decimal(10,2),
    [period_reports] int,
    [period_billing_cycles] int,
    [period_from] date,
    [period_to] date,
    [average_license_seats] int,
    [average_retail_price] decimal(10,2),
    [average_extended_amount] decimal(10,2),
    [average_usage_seats] int,
    [average_usage_price] decimal(10,2),
    [average_usage_extended_amount] decimal(10,2),
    [average_billing_total] decimal(10,2)
,
    PRIMARY KEY ([partner_billing_revenue_fact_id])
);
CREATE TABLE [dbo].[partner_billing_revenue_order_fact] (
    [partner_billing_revenue_order_fact_id] int IDENTITY(1,1) NOT NULL,
    [report_date] date NOT NULL,
    [partner_billing_order_id] int NOT NULL,
    [partner_billing_report_id] int NOT NULL,
    [partner_id] int NOT NULL,
    [site_id] varchar(20) NOT NULL,
    [currency_id] int,
    [billing_date] date NOT NULL,
    [order_header_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [order_total] decimal(10,2) NOT NULL
,
    PRIMARY KEY ([partner_billing_revenue_order_fact_id])
);
CREATE TABLE [dbo].[partner_billing_status] (
    [partner_billing_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [partner_billing_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([partner_billing_status_id])
);
CREATE TABLE [dbo].[partner_billing_type] (
    [partner_billing_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [partner_billing_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([partner_billing_type_id])
);
CREATE TABLE [dbo].[partner_billing_usage_override] (
    [partner_usage_override_id] int IDENTITY(1,1) NOT NULL,
    [partner_billing_report_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [effective_date] datetime NOT NULL,
    [parent_keycode] varchar(40) NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [site_name] nvarchar(255),
    [category_type_name] varchar(20) NOT NULL,
    [expiration_date] datetime,
    [agent_units] int,
    [agentless_units] int,
    [proxy_units] int,
    [usage_seats] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_usage_override_id])
);
CREATE TABLE [dbo].[partner_campaign] (
    [partner_campaign_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [salesforce_campaign_id] varchar(18) NOT NULL
,
    PRIMARY KEY ([partner_campaign_id])
);
CREATE TABLE [dbo].[partner_cart_discount] (
    [partner_cart_discount_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [cart_discount_id] int NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [site_id] varchar(20)
,
    PRIMARY KEY ([partner_cart_discount_id])
);
CREATE TABLE [dbo].[partner_client] (
    [partner_client_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [client_id] varchar(100),
    [client_secret] nvarchar(200),
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_client_id])
);
CREATE TABLE [dbo].[partner_company] (
    [partner_company_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [partner_customer_code] varchar(100) NOT NULL,
    [company_name] nvarchar(255),
    [company_id] int,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_company_id])
);
CREATE TABLE [dbo].[partner_company_audit] (
    [partner_company_audit_id] int IDENTITY(1,1) NOT NULL,
    [partner_company_id] int NOT NULL,
    [partner_id] int NOT NULL,
    [partner_customer_code] varchar(100) NOT NULL,
    [company_name] nvarchar(255),
    [company_id] int,
    [insert_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
);
CREATE TABLE [dbo].[partner_configuration] (
    [partner_configuration_id] tinyint IDENTITY(1,1) NOT NULL,
    [configuration_name] varchar(50) NOT NULL,
    [status] varchar(10) NOT NULL
,
    PRIMARY KEY ([partner_configuration_id])
);
CREATE TABLE [dbo].[partner_configuration_partner] (
    [partner_configuration_partner_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [partner_configuration_id] tinyint NOT NULL,
    [configuration_value] varchar(500) NOT NULL
,
    PRIMARY KEY ([partner_configuration_partner_id])
);
CREATE TABLE [dbo].[partner_configuration_value] (
    [partner_configuration_value_id] int IDENTITY(1,1) NOT NULL,
    [partner_configuration_id] tinyint NOT NULL,
    [configuration_value] varchar(500) NOT NULL
,
    PRIMARY KEY ([partner_configuration_value_id])
);
CREATE TABLE [dbo].[partner_customer] (
    [partner_customer_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [customer_id] int NOT NULL
,
    PRIMARY KEY ([partner_customer_id])
);
CREATE TABLE [dbo].[partner_language_location] (
    [partner_language_location_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [billing_default] tinyint NOT NULL
,
    PRIMARY KEY ([partner_language_location_id])
);
CREATE TABLE [dbo].[partner_license] (
    [partner_license_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [partner_id] int NOT NULL,
    [partner_license_status_id] tinyint NOT NULL,
    [partner_license_source_id] tinyint NOT NULL,
    [last_modified_user_id] varchar(20) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [insert_date] datetime,
    [start_date] datetime,
    [end_date] datetime,
    [order_header_id] int
,
    PRIMARY KEY ([partner_license_id])
);
CREATE TABLE [dbo].[partner_license_attribute] (
    [partner_license_attribute_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [site_id] varchar(20) NOT NULL,
    [license_category_id] int NOT NULL,
    [license_attribute_id] int NOT NULL,
    [license_attribute_license_value] int NOT NULL,
    [partner_pricing_model_id] int NOT NULL,
    [default_value] tinyint NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [cart_display] smallint NOT NULL
,
    PRIMARY KEY ([partner_license_attribute_id])
);
CREATE TABLE [dbo].[partner_license_audit] (
    [partner_license_audit_id] int IDENTITY(1,1) NOT NULL,
    [partner_license_id] int NOT NULL,
    [license_id] int NOT NULL,
    [partner_id] int NOT NULL,
    [partner_license_status_id] tinyint NOT NULL,
    [partner_license_source_id] tinyint NOT NULL,
    [last_modified_user_id] varchar(20) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [insert_date] datetime,
    [start_date] datetime,
    [end_date] datetime,
    [order_header_id] int,
    [audit_date] datetime
,
    PRIMARY KEY ([partner_license_audit_id])
);
CREATE TABLE [dbo].[partner_license_category_product_line] (
    [partner_license_category_product_line_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [product_line_id] int NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [site_id] varchar(20)
);
CREATE TABLE [dbo].[partner_license_comment] (
    [partner_license_comment_id] int IDENTITY(1,1) NOT NULL,
    [partner_license_id] int NOT NULL,
    [comment] nvarchar(MAX) NOT NULL
,
    PRIMARY KEY ([partner_license_comment_id])
);
CREATE TABLE [dbo].[partner_license_debug] (
    [license_id] int,
    [partner_license_status_id] tinyint,
    [vendor_order_code] varchar(50),
    [case_id] tinyint
);
CREATE TABLE [dbo].[partner_license_distribution_method] (
    [partner_license_distribution_method_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([partner_license_distribution_method_id])
);
CREATE TABLE [dbo].[partner_license_module] (
    [partner_license_module_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [site_id] varchar(20) NOT NULL,
    [license_module_id] int NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([partner_license_module_id])
);
CREATE TABLE [dbo].[partner_license_pricing] (
    [partner_license_pricing_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [partner_pricing_tier_id] int NOT NULL,
    [max_license_seats] int,
    [start_date] datetime,
    [end_date] datetime,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_license_pricing_id])
);
CREATE TABLE [dbo].[partner_license_source] (
    [partner_license_source_id] tinyint IDENTITY(1,1) NOT NULL,
    [partner_license_source_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([partner_license_source_id])
);
CREATE TABLE [dbo].[partner_license_status] (
    [partner_license_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [partner_license_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([partner_license_status_id])
);
CREATE TABLE [dbo].[partner_license_transaction] (
    [partner_license_transaction_id] int IDENTITY(1,1) NOT NULL,
    [partner_license_id] int NOT NULL,
    [partner_license_status_id] tinyint NOT NULL,
    [license_transaction] varchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [partner_license_transaction_procedure_id] tinyint
,
    PRIMARY KEY ([partner_license_transaction_id])
);
CREATE TABLE [dbo].[partner_license_transaction_procedure] (
    [partner_license_transaction_procedure_id] tinyint IDENTITY(1,1) NOT NULL,
    [transaction_procedure_name] varchar(100) NOT NULL,
    [transaction_procedure_description] varchar(MAX) NOT NULL,
    [archive_duplicate] varchar(10) NOT NULL
,
    PRIMARY KEY ([partner_license_transaction_procedure_id])
);
CREATE TABLE [dbo].[partner_license_usage] (
    [partner_license_usage_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int,
    [report_date] datetime,
    [license_id] int,
    [company_name] nvarchar(255),
    [customer_email] varchar(100),
    [license_seats] int,
    [activation_date] datetime,
    [licenses] int,
    [devices] int,
    [devices_30_days] int,
    [devices_30_days_active] int,
    [devices_delta] int,
    [devices_delta_abs] int,
    [devices_delta_active] int,
    [devices_delta_abs_active] int,
    [insert_date] datetime,
    [expiration_date] datetime,
    [customer_state] nvarchar(2),
    [customer_country] nvarchar(75),
    [license_category_id] tinyint,
    [storage_gb] int
,
    PRIMARY KEY ([partner_license_usage_id])
);
CREATE TABLE [dbo].[partner_order] (
    [partner_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [partner_account_id] int,
    [insert_date] datetime,
    [partner_order_id] int IDENTITY(1,1) NOT NULL
,
    PRIMARY KEY ([partner_order_id])
);
CREATE TABLE [dbo].[partner_order_audit] (
    [partner_order_audit_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [partner_account_id] int,
    [audit_date] datetime NOT NULL
);
CREATE TABLE [dbo].[partner_payment_method] (
    [partner_payment_method_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [payment_method_id] tinyint NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([partner_payment_method_id])
);
CREATE TABLE [dbo].[partner_payment_term] (
    [partner_payment_term_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [payment_term_id] tinyint NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([partner_payment_term_id])
);
CREATE TABLE [dbo].[partner_pricing_level] (
    [partner_pricing_level_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [product_pricing_level_id] tinyint NOT NULL
,
    PRIMARY KEY ([partner_pricing_level_id])
);
CREATE TABLE [dbo].[partner_pricing_model] (
    [partner_pricing_model_id] tinyint IDENTITY(1,1) NOT NULL,
    [partner_pricing_model_name] varchar(50) NOT NULL,
    [partner_pricing_model_description] varchar(200) NOT NULL
,
    PRIMARY KEY ([partner_pricing_model_id])
);
CREATE TABLE [dbo].[partner_pricing_tier] (
    [partner_pricing_tier_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [product_id] int NOT NULL,
    [retail_price] money NOT NULL,
    [currency_id] tinyint NOT NULL,
    [low_range] int NOT NULL,
    [high_range] int NOT NULL,
    [pricing_status] varchar(10) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [site_id] varchar(20),
    [license_category_id] tinyint,
    [license_attribute_id] int,
    [pricing_term] varchar(10),
    [years] tinyint
,
    PRIMARY KEY ([partner_pricing_tier_id])
);
CREATE TABLE [dbo].[partner_pricing_tier_audit] (
    [partner_pricing_tier_audit_id] int IDENTITY(1,1) NOT NULL,
    [partner_pricing_tier_id] int,
    [partner_id] int NOT NULL,
    [product_id] int NOT NULL,
    [retail_price] money NOT NULL,
    [currency_id] tinyint NOT NULL,
    [low_range] int NOT NULL,
    [high_range] int NOT NULL,
    [pricing_status] varchar(10) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [site_id] varchar(20),
    [license_category_id] tinyint,
    [license_attribute_id] int,
    [pricing_term] varchar(10),
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_pricing_tier_audit_id])
);
CREATE TABLE [dbo].[partner_pricing_tier_extension] (
    [partner_pricing_tier_extension_id] int IDENTITY(1,1) NOT NULL,
    [partner_pricing_tier_id] int NOT NULL,
    [product_extension_json] nvarchar(MAX) NOT NULL
,
    PRIMARY KEY ([partner_pricing_tier_extension_id])
);
CREATE TABLE [dbo].[partner_pricing_tier_price] (
    [partner_pricing_tier_price_id] int IDENTITY(1,1) NOT NULL,
    [partner_pricing_tier_id] int NOT NULL,
    [product_pricing_level_id] tinyint NOT NULL,
    [unit_price] money NOT NULL
,
    PRIMARY KEY ([partner_pricing_tier_price_id])
);
CREATE TABLE [dbo].[partner_product_platform] (
    [partner_product_platform_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [product_platform_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [site_id] varchar(20)
,
    PRIMARY KEY ([partner_product_platform_id])
);
CREATE TABLE [dbo].[partner_quote] (
    [partner_quote_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [cart_discount_message_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_quote_id])
);
CREATE TABLE [dbo].[partner_retention_model] (
    [partner_retention_model_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [retention_model_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [site_id] varchar(20)
,
    PRIMARY KEY ([partner_retention_model_id])
);
CREATE TABLE [dbo].[partner_service_cancel] (
    [partner_service_cancel_id] int IDENTITY(1,1) NOT NULL,
    [partner_service_log_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL
,
    PRIMARY KEY ([partner_service_cancel_id])
);
CREATE TABLE [dbo].[partner_service_child_license_data_log] (
    [partner_service_child_license_data_log_id] int IDENTITY(1,1) NOT NULL,
    [parent_license_id] int NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [capability_type_description] varchar(20) NOT NULL,
    [company_name] nvarchar(255) NOT NULL,
    [devices] int NOT NULL,
    [total_devices] int NOT NULL,
    [deactivated_devices] int NOT NULL,
    [effective_date] datetime NOT NULL,
    [log_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_service_child_license_data_log_id])
);
CREATE TABLE [dbo].[partner_service_license_parent] (
    [partner_service_license_parent_id] int IDENTITY(1,1) NOT NULL,
    [partner_service_log_id] int NOT NULL,
    [parent_keycode] varchar(40) NOT NULL,
    [license_seats] int NOT NULL,
    [child_keycode] varchar(40)
,
    PRIMARY KEY ([partner_service_license_parent_id])
);
CREATE TABLE [dbo].[partner_service_log] (
    [partner_service_log_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [partner_account_id] int NOT NULL,
    [partner_service_method_id] tinyint NOT NULL,
    [partner_service_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [license_service_id] int
,
    PRIMARY KEY ([partner_service_log_id])
);
CREATE TABLE [dbo].[partner_service_log_audit] (
    [partner_service_log_audit_id] int IDENTITY(1,1) NOT NULL,
    [partner_service_log_id] int NOT NULL,
    [partner_id] int NOT NULL,
    [partner_account_id] int NOT NULL,
    [partner_service_method_id] tinyint NOT NULL,
    [partner_service_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_service_log_audit_id])
);
CREATE TABLE [dbo].[partner_service_log_failure] (
    [partner_service_log_failure_id] int IDENTITY(1,1) NOT NULL,
    [partner_service_log_id] int,
    [partner_id] int,
    [partner_account_id] int,
    [partner_service_method_id] tinyint,
    [partner_service_status_id] tinyint,
    [insert_date] datetime,
    [modified_date] datetime,
    [partner_account_key] varchar(36),
    [response_code] int,
    [message] varchar(100),
    [failure_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_service_log_failure_id])
);
CREATE TABLE [dbo].[partner_service_log_json] (
    [partner_service_log_json_id] int IDENTITY(1,1) NOT NULL,
    [partner_service_log_id] int NOT NULL,
    [partner_json] varchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_service_log_json_id])
);
CREATE TABLE [dbo].[partner_service_method] (
    [partner_service_method_id] tinyint IDENTITY(1,1) NOT NULL,
    [partner_service_method_name] varchar(50) NOT NULL,
    [partner_service_method_description] varchar(250) NOT NULL
,
    PRIMARY KEY ([partner_service_method_id])
);
CREATE TABLE [dbo].[partner_service_method_partner] (
    [partner_service_method_partner_id] int IDENTITY(1,1) NOT NULL,
    [partner_service_method_id] tinyint NOT NULL,
    [partner_id] int NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([partner_service_method_partner_id])
);
CREATE TABLE [dbo].[partner_service_order] (
    [partner_service_order_id] int IDENTITY(1,1) NOT NULL,
    [partner_service_log_id] int NOT NULL,
    [partner_id] int NOT NULL,
    [partner_order_code] varchar(20),
    [partner_order_date] datetime,
    [language_code] varchar(2),
    [location_code] varchar(3),
    [purchase_order] varchar(100),
    [cart_order_id] int,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [order_header_id] int,
    [site_id] varchar(65)
,
    PRIMARY KEY ([partner_service_order_id])
);
CREATE TABLE [dbo].[partner_service_order_customer] (
    [partner_service_order_customer_id] int IDENTITY(1,1) NOT NULL,
    [partner_service_order_id] int NOT NULL,
    [external_account_id] varchar(64) NOT NULL,
    [first_name] nvarchar(255),
    [last_name] nvarchar(255),
    [company_name] nvarchar(255),
    [customer_email] varchar(100),
    [phone_number] varchar(50),
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [city] nvarchar(130),
    [state] nvarchar(3),
    [postal_code] nvarchar(32),
    [country] varchar(75),
    [order_company_type_id] tinyint,
    [customer_id] int
,
    PRIMARY KEY ([partner_service_order_customer_id])
);
CREATE TABLE [dbo].[partner_service_order_item] (
    [partner_service_order_item_id] int IDENTITY(1,1) NOT NULL,
    [partner_service_order_id] int NOT NULL,
    [line_item] tinyint NOT NULL,
    [product_id] int,
    [quantity] int,
    [seats] int,
    [list_price] money,
    [unit_price] money,
    [start_date] datetime,
    [expiration_date] datetime,
    [order_item_id] int,
    [external_item_id] varchar(36),
    [order_item_update_type_id] tinyint,
    [license_attribute_license_value] int,
    [item_bundle_id] tinyint
,
    PRIMARY KEY ([partner_service_order_item_id])
);
CREATE TABLE [dbo].[partner_service_order_item_license] (
    [partner_service_order_item_license_id] int IDENTITY(1,1) NOT NULL,
    [partner_service_order_item_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL
,
    PRIMARY KEY ([partner_service_order_item_license_id])
);
CREATE TABLE [dbo].[partner_service_order_process] (
    [partner_service_order_process_id] tinyint IDENTITY(1,1) NOT NULL,
    [order_process_name] varchar(50) NOT NULL,
    [order_process_description] varchar(500) NOT NULL,
    [sfdc_template_id] tinyint
,
    PRIMARY KEY ([partner_service_order_process_id])
);
CREATE TABLE [dbo].[partner_service_order_process_partner] (
    [partner_service_order_process_partner_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [partner_service_order_process_id] int NOT NULL,
    [site_id] varchar(20),
    [insert_by] varchar(200) NOT NULL,
    [license_attribute_id] int,
    [license_attribute_license_value] int
,
    PRIMARY KEY ([partner_service_order_process_partner_id])
);
CREATE TABLE [dbo].[partner_service_product] (
    [partner_service_product_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [partner_product_id] varchar(20) NOT NULL,
    [product_id] int NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [site_id] varchar(20),
    [license_attribute_license_value] int
,
    PRIMARY KEY ([partner_service_product_id])
);
CREATE TABLE [dbo].[partner_service_status] (
    [partner_service_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [partner_service_status_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([partner_service_status_id])
);
CREATE TABLE [dbo].[partner_sfdc_assignment] (
    [partner_sfdc_assignment_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [sfdc_assignment_type_id] tinyint NOT NULL,
    [sfdc_user_id] int
,
    PRIMARY KEY ([partner_sfdc_assignment_id])
);
CREATE TABLE [dbo].[partner_status] (
    [partner_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [partner_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([partner_status_id])
);
CREATE TABLE [dbo].[partner_trial] (
    [partner_trial_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [trial_id] int NOT NULL,
    [partner_account_id] int
,
    PRIMARY KEY ([partner_trial_id])
);
CREATE TABLE [dbo].[partner_type] (
    [partner_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [partner_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([partner_type_id])
);
CREATE TABLE [dbo].[partner_usage_pricing_model] (
    [partner_usage_pricing_model_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [usage_pricing_model_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [site_id] varchar(20)
,
    PRIMARY KEY ([partner_usage_pricing_model_id])
);
CREATE TABLE [dbo].[partner_verify_failure] (
    [partner_verify_failure_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int,
    [verify_type] varchar(20) NOT NULL,
    [verify_value] varchar(40) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_verify_failure_id])
);
CREATE TABLE [dbo].[partner_wwss_account] (
    [partner_wwss_account_id] int IDENTITY(1,1) NOT NULL,
    [partner_id] int NOT NULL,
    [admin_email] varchar(100) NOT NULL,
    [salesforce_contact_id] varchar(18),
    [wwss_external_id] int,
    [last_modified_date] datetime NOT NULL,
    [oracle_account_id] int
,
    PRIMARY KEY ([partner_wwss_account_id])
);
CREATE TABLE [dbo].[partner_wwss_account_audit] (
    [partner_wwss_account_audit_id] int IDENTITY(1,1) NOT NULL,
    [partner_wwss_account_id] int NOT NULL,
    [partner_id] int NOT NULL,
    [admin_email] varchar(100) NOT NULL,
    [salesforce_contact_id] varchar(18),
    [wwss_external_id] int,
    [oracle_account_id] int,
    [last_modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([partner_wwss_account_audit_id])
);
CREATE TABLE [dbo].[partners] (
    [partnerid] int IDENTITY(1,1) NOT NULL,
    [lastname] varchar(100) NOT NULL,
    [firstname] varchar(100) NOT NULL,
    [title] varchar(100) NOT NULL,
    [phone] varchar(100) NOT NULL,
    [fax] varchar(100) NOT NULL,
    [email] varchar(100) NOT NULL,
    [company] varchar(100) NOT NULL,
    [company2] varchar(100) NOT NULL,
    [address] varchar(100) NOT NULL,
    [address2] varchar(100) NOT NULL,
    [city] varchar(100) NOT NULL,
    [state] varchar(100) NOT NULL,
    [country] varchar(100) NOT NULL,
    [zip] varchar(100) NOT NULL,
    [url] varchar(100) NOT NULL,
    [duns] varchar(100) NOT NULL,
    [taxid] varchar(100),
    [rev_last] varchar(100) NOT NULL,
    [rev_this] varchar(100) NOT NULL,
    [rev_secure] varchar(100) NOT NULL,
    [rev_sales] varchar(100) NOT NULL,
    [employees] varchar(100) NOT NULL,
    [territory] varchar(100) NOT NULL,
    [certs] varchar(100) NOT NULL,
    [fwsec_prod] varchar(100) NOT NULL,
    [service_model] varchar(100) NOT NULL,
    [market_size] varchar(100) NOT NULL,
    [market_vert] varchar(100) NOT NULL,
    [platforms] varchar(100) NOT NULL,
    [officer_ceo_name] varchar(100) NOT NULL,
    [officer_ceo_email] varchar(100) NOT NULL,
    [officer_sales_name] varchar(100) NOT NULL,
    [officer_sales_email] varchar(100) NOT NULL,
    [officer_mktg_name] varchar(100) NOT NULL,
    [officer_mktg_email] varchar(100) NOT NULL,
    [officer_support_name] varchar(100) NOT NULL,
    [officer_support_email] varchar(100) NOT NULL,
    [reg_date] varchar(100) NOT NULL
);
CREATE TABLE [dbo].[payment_account] (
    [payment_account_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [account_number] varchar(50) NOT NULL,
    [expiration_month] tinyint NOT NULL,
    [expiration_year] smallint NOT NULL,
    [card_type_id] tinyint NOT NULL,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([payment_account_id])
);
CREATE TABLE [dbo].[payment_action] (
    [payment_action_id] tinyint IDENTITY(1,1) NOT NULL,
    [payment_action_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([payment_action_id])
);
CREATE TABLE [dbo].[payment_billing_frequency] (
    [payment_billing_frequency_id] tinyint IDENTITY(1,1) NOT NULL,
    [payment_billing_frequency] varchar(50) NOT NULL,
    [frequency_cycle] int NOT NULL
,
    PRIMARY KEY ([payment_billing_frequency_id])
);
CREATE TABLE [dbo].[payment_export_violation] (
    [payment_export_violation_id] int IDENTITY(1,1) NOT NULL,
    [payment_response_id] int NOT NULL,
    [response_name] varchar(50) NOT NULL,
    [response_value] nvarchar(MAX) NOT NULL,
    [payment_export_violation_status_id] int
,
    PRIMARY KEY ([payment_export_violation_id])
);
CREATE TABLE [dbo].[payment_export_violation_status] (
    [payment_export_violation_status_id] int IDENTITY(1,1) NOT NULL,
    [payment_export_violation_status] varchar(50) NOT NULL
,
    PRIMARY KEY ([payment_export_violation_status_id])
);
CREATE TABLE [dbo].[payment_fee] (
    [payment_fee_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int NOT NULL,
    [payment_fee] money NOT NULL
);
CREATE TABLE [dbo].[payment_fraud] (
    [payment_fraud_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([payment_fraud_id])
);
CREATE TABLE [dbo].[payment_fraud_threshold] (
    [payment_fraud_threshold_id] int IDENTITY(1,1) NOT NULL,
    [unique_accounts] tinyint NOT NULL,
    [ip_address_history_hours] smallint NOT NULL,
    [ip_address_unique_accounts] tinyint NOT NULL
,
    PRIMARY KEY ([payment_fraud_threshold_id])
);
CREATE TABLE [dbo].[payment_gateway] (
    [payment_gateway_id] int NOT NULL,
    [description] nvarchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] nvarchar(50) NOT NULL
,
    PRIMARY KEY ([payment_gateway_id])
);
CREATE TABLE [dbo].[payment_header] (
    [payment_header_id] int IDENTITY(1,1) NOT NULL,
    [cart_order_id] int,
    [payment_merchant_id] tinyint,
    [payment_status_id] tinyint NOT NULL,
    [payment_method_id] tinyint,
    [currency_id] tinyint,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([payment_header_id])
);
CREATE TABLE [dbo].[payment_header_audit] (
    [payment_header_audit_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [cart_order_id] int,
    [payment_merchant_id] tinyint,
    [payment_status_id] tinyint NOT NULL,
    [currency_id] tinyint NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL,
    [payment_method_id] int
,
    PRIMARY KEY ([payment_header_audit_id])
);
CREATE TABLE [dbo].[payment_header_cart_in_process] (
    [cart_in_process_id] int NOT NULL,
    [payment_header_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([cart_in_process_id], [payment_header_id])
);
CREATE TABLE [dbo].[payment_header_opportunity] (
    [payment_header_opportunity_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [salesforce_opportunity_id] varchar(18) NOT NULL
,
    PRIMARY KEY ([payment_header_opportunity_id])
);
CREATE TABLE [dbo].[payment_header_paypal] (
    [payment_header_paypal_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [setup_request_id] varchar(24),
    [paypal_token] varchar(24),
    [paypal_payer] varchar(100),
    [paypal_payer_id] varchar(24),
    [last_modified_date] datetime NOT NULL,
    [capture_request_id] varchar(24)
,
    PRIMARY KEY ([payment_header_paypal_id])
);
CREATE TABLE [dbo].[payment_header_paypal_audit] (
    [payment_header_paypal_audit_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_paypal_id] int NOT NULL,
    [payment_header_id] int NOT NULL,
    [setup_request_id] varchar(24),
    [paypal_token] varchar(24),
    [paypal_payer] varchar(100),
    [paypal_payer_id] varchar(24),
    [last_modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL,
    [capture_request_id] varchar(24)
,
    PRIMARY KEY ([payment_header_paypal_audit_id])
);
CREATE TABLE [dbo].[payment_header_purchase_order] (
    [payment_header_purchase_order_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [purchase_order] varchar(100) NOT NULL
,
    PRIMARY KEY ([payment_header_purchase_order_id])
);
CREATE TABLE [dbo].[payment_header_token] (
    [payment_header_token_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [request_id] varchar(24) NOT NULL,
    [request_token] varchar(100) NOT NULL,
    [payment_action_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([payment_header_token_id])
);
CREATE TABLE [dbo].[payment_header_transaction] (
    [payment_header_transaction_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [authorization_transaction_id] varchar(24),
    [settlement_transaction_id] varchar(24),
    [last_modified_date] datetime NOT NULL,
    [payment_network_transaction_string] varchar(30)
,
    PRIMARY KEY ([payment_header_transaction_id])
);
CREATE TABLE [dbo].[payment_header_transaction_audit] (
    [payment_header_transaction_audit_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_transaction_id] int NOT NULL,
    [payment_header_id] int NOT NULL,
    [authorization_transaction_id] varchar(24),
    [settlement_transaction_id] varchar(24),
    [last_modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL,
    [payment_network_transaction_string] varchar(30)
,
    PRIMARY KEY ([payment_header_transaction_audit_id])
);
CREATE TABLE [dbo].[payment_license] (
    [payment_license_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [license_id] int NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([payment_license_id])
);
CREATE TABLE [dbo].[payment_merchant] (
    [payment_merchant_id] tinyint IDENTITY(1,1) NOT NULL,
    [merchant_id] varchar(50) NOT NULL,
    [payment_merchant_name] varchar(40),
    [is_csi_displayed] bit NOT NULL
,
    PRIMARY KEY ([payment_merchant_id])
);
CREATE TABLE [dbo].[payment_merchant_gateway] (
    [payment_merchant_gateway_id] int IDENTITY(1,1) NOT NULL,
    [payment_merchant_id] tinyint NOT NULL,
    [payment_gateway_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] nvarchar(50) NOT NULL
,
    PRIMARY KEY ([payment_merchant_gateway_id])
);
CREATE TABLE [dbo].[payment_method] (
    [payment_method_id] tinyint IDENTITY(1,1) NOT NULL,
    [payment_method_name] varchar(50) NOT NULL,
    [autorenewal_flag] tinyint,
    [active] tinyint
,
    PRIMARY KEY ([payment_method_id])
);
CREATE TABLE [dbo].[payment_method_type] (
    [payment_method_type_id] int IDENTITY(1,1) NOT NULL,
    [payment_method_type] varchar(255) NOT NULL,
    [payment_method_type_description] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([payment_method_type_id])
);
CREATE TABLE [dbo].[payment_request] (
    [payment_request_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [payment_method_id] tinyint,
    [payment_action_id] tinyint NOT NULL,
    [request_text_var] varchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL,
    [request_text] nvarchar(MAX),
    [account_number] varchar(4),
    [expiration_month] char(2),
    [expiration_year] char(4),
    [card_type_id] char(3),
    [cv_number] varchar(4)
,
    PRIMARY KEY ([payment_request_id])
);
CREATE TABLE [dbo].[payment_response] (
    [payment_response_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [response_text_var] varchar(MAX) NOT NULL,
    [response_code] varchar(50) NOT NULL,
    [response_message] varchar(120) NOT NULL,
    [insert_date] datetime NOT NULL,
    [response_text] nvarchar(MAX)
,
    PRIMARY KEY ([payment_response_id])
);
CREATE TABLE [dbo].[payment_response_code] (
    [response_code] nvarchar(50) NOT NULL,
    [description] nvarchar(255),
    [status] nvarchar(50),
    [action] nvarchar(20),
    [retry] int NOT NULL,
    [payment_gateway_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] nvarchar(200) NOT NULL
,
    PRIMARY KEY ([payment_gateway_id], [response_code])
);
CREATE TABLE [dbo].[payment_status] (
    [payment_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [payment_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([payment_status_id])
);
CREATE TABLE [dbo].[payment_status_method_action] (
    [payment_status_method_action_id] int IDENTITY(1,1) NOT NULL,
    [payment_status_id] tinyint NOT NULL,
    [payment_method_id] tinyint NOT NULL,
    [payment_action_id] tinyint NOT NULL
,
    PRIMARY KEY ([payment_status_method_action_id])
);
CREATE TABLE [dbo].[payment_subscription] (
    [payment_subscription_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [subscription_id] varchar(50) NOT NULL,
    [subscription_email] varchar(100),
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([payment_subscription_id])
);
CREATE TABLE [dbo].[payment_subscription_audit] (
    [payment_subscription_audit_id] int IDENTITY(1,1) NOT NULL,
    [payment_subscription_id] int NOT NULL,
    [payment_header_id] int NOT NULL,
    [subscription_id] varchar(50) NOT NULL,
    [subscription_email] varchar(100),
    [last_modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([payment_subscription_audit_id])
);
CREATE TABLE [dbo].[payment_subscription_credit_card] (
    [payment_subscription_credit_card_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [credit_card_subscription_id] varchar(24),
    [mit_id] varchar(15),
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([payment_subscription_credit_card_id])
);
CREATE TABLE [dbo].[payment_subscription_credit_card_audit] (
    [payment_subscription_credit_card_audit_id] int IDENTITY(1,1) NOT NULL,
    [payment_subscription_credit_card_id] int NOT NULL,
    [payment_header_id] int NOT NULL,
    [credit_card_subscription_id] varchar(24),
    [mit_id] varchar(15),
    [last_modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([payment_subscription_credit_card_audit_id])
);
CREATE TABLE [dbo].[payment_subscription_credit_card_batch] (
    [payment_subscription_credit_card_batch_id] int IDENTITY(1,1) NOT NULL,
    [process_date] datetime NOT NULL,
    [merchant_id] varchar(50) NOT NULL,
    [cybs_batch_id] varchar(50),
    [cybs_batch_response] varchar(255),
    [attempts] tinyint,
    [last_modified_date] datetime NOT NULL,
    [payment_subscription_credit_card_batch_status_id] tinyint NOT NULL,
    [insert_date] datetime
,
    PRIMARY KEY ([payment_subscription_credit_card_batch_id])
);
CREATE TABLE [dbo].[payment_subscription_credit_card_batch_archive] (
    [payment_subscription_credit_card_batch_archive_id] int IDENTITY(1,1) NOT NULL,
    [payment_subscription_credit_card_batch_id] int NOT NULL,
    [process_date] datetime NOT NULL,
    [merchant_id] varchar(50),
    [cybs_batch_id] varchar(50),
    [cybs_batch_response] varchar(255),
    [attempts] tinyint,
    [last_modified_date] datetime NOT NULL,
    [archive_date] datetime NOT NULL,
    [payment_subscription_credit_card_batch_status_id] tinyint NOT NULL
,
    PRIMARY KEY ([payment_subscription_credit_card_batch_archive_id])
);
CREATE TABLE [dbo].[payment_subscription_credit_card_batch_status] (
    [payment_subscription_credit_card_batch_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [payment_subscription_credit_card_batch_status_description] varchar(25)
,
    PRIMARY KEY ([payment_subscription_credit_card_batch_status_id])
);
CREATE TABLE [dbo].[payment_subscription_credit_card_update] (
    [payment_subscription_credit_card_update_id] int IDENTITY(1,1) NOT NULL,
    [payment_subscription_credit_card_id] int NOT NULL,
    [license_message_id] int NOT NULL,
    [payment_header_id] int NOT NULL,
    [credit_card_subscription_id] varchar(24) NOT NULL,
    [response_code] varchar(50),
    [reason_code] varchar(10),
    [last_modified_date] datetime NOT NULL,
    [payment_subscription_credit_card_batch_id] int NOT NULL,
    [insert_date] datetime
,
    PRIMARY KEY ([payment_subscription_credit_card_update_id])
);
CREATE TABLE [dbo].[payment_subscription_credit_card_update_audit] (
    [payment_subscription_credit_card_update_audit_id] int IDENTITY(1,1) NOT NULL,
    [payment_subscription_credit_card_update_id] int NOT NULL,
    [payment_subscription_credit_card_id] int NOT NULL,
    [license_message_id] int NOT NULL,
    [payment_header_id] int NOT NULL,
    [credit_card_subscription_id] varchar(24) NOT NULL,
    [response_code] varchar(50),
    [reason_code] varchar(10),
    [last_modified_date] datetime NOT NULL,
    [payment_subscription_credit_card_batch_id] int NOT NULL,
    [insert_date] datetime,
    [audit_date] datetime NOT NULL,
    [audit_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([payment_subscription_credit_card_update_audit_id])
);
CREATE TABLE [dbo].[payment_subscription_credit_card_update_merchant] (
    [payment_subscription_credit_card_update_merchant_id] int IDENTITY(1,1) NOT NULL,
    [merchant_id] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [payment_merchant_id] tinyint NOT NULL
,
    PRIMARY KEY ([payment_subscription_credit_card_update_merchant_id])
);
CREATE TABLE [dbo].[payment_subscription_paypal] (
    [payment_subscription_paypal_id] int IDENTITY(1,1) NOT NULL,
    [payment_header_id] int NOT NULL,
    [billing_agreement_id] varchar(24),
    [paypal_customer_email] varchar(100),
    [last_modified_date] datetime NOT NULL
,
    PRIMARY KEY ([payment_subscription_paypal_id])
);
CREATE TABLE [dbo].[payment_subscription_paypal_audit] (
    [payment_subscription_paypal_audit_id] int IDENTITY(1,1) NOT NULL,
    [payment_subscription_paypal_id] int NOT NULL,
    [payment_header_id] int NOT NULL,
    [billing_agreement_id] varchar(24),
    [paypal_customer_email] varchar(100),
    [last_modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([payment_subscription_paypal_audit_id])
);
CREATE TABLE [dbo].[payment_term] (
    [payment_term_id] tinyint IDENTITY(1,1) NOT NULL,
    [payment_term_name] varchar(50) NOT NULL,
    [payment_term_days] int NOT NULL
,
    PRIMARY KEY ([payment_term_id])
);
CREATE TABLE [dbo].[PaymentMethodGroup] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [SetID] int NOT NULL,
    [Name] varchar(50) NOT NULL,
    [PaymentMethodID] smallint NOT NULL
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[PaymentMethodGroupName] (
    [Name] varchar(50) NOT NULL
,
    PRIMARY KEY ([Name])
);
CREATE TABLE [dbo].[PaymentMethodGroupSet] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Name] varchar(50) NOT NULL
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[paypal_customer_email] (
    [customer_email] varchar(100)
);
CREATE TABLE [dbo].[permission_application] (
    [permission_application_id] tinyint IDENTITY(1,1) NOT NULL,
    [permission_application_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([permission_application_id])
);
CREATE TABLE [dbo].[permission_group] (
    [permission_group_id] tinyint IDENTITY(1,1) NOT NULL,
    [permission_group_name] varchar(50) NOT NULL,
    [permission_group_status] varchar(20) NOT NULL,
    [permission_application_id] tinyint NOT NULL
,
    PRIMARY KEY ([permission_group_id])
);
CREATE TABLE [dbo].[permission_group_membership] (
    [permission_group_membership_id] int IDENTITY(1,1) NOT NULL,
    [permission_application_id] tinyint NOT NULL,
    [permission_group_id] tinyint NOT NULL,
    [user_name] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([permission_group_membership_id])
);
CREATE TABLE [dbo].[permission_group_resource] (
    [permission_group_resource_id] int IDENTITY(1,1) NOT NULL,
    [permission_group_id] tinyint NOT NULL,
    [permission_resource_id] int NOT NULL
,
    PRIMARY KEY ([permission_group_resource_id])
);
CREATE TABLE [dbo].[permission_log] (
    [permission_log_id] int IDENTITY(1,1) NOT NULL,
    [permission_application_id] tinyint NOT NULL,
    [user_name] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([permission_log_id])
);
CREATE TABLE [dbo].[permission_resource] (
    [permission_resource_id] int IDENTITY(1,1) NOT NULL,
    [resource_name] varchar(50) NOT NULL,
    [access_level] varchar(50),
    [permission_application_id] tinyint NOT NULL
,
    PRIMARY KEY ([permission_resource_id])
);
CREATE TABLE [dbo].[prevx_beta_upgrade] (
    [prevx_beta_upgrade_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [first_name] nvarchar(225) NOT NULL,
    [last_name] nvarchar(225) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([prevx_beta_upgrade_id])
);
CREATE TABLE [dbo].[prevx_keycode_update] (
    [prevx_keycode_update_id] int IDENTITY(1,1) NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [prevx_keycode] varchar(40) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([prevx_keycode_update_id])
);
CREATE TABLE [dbo].[prevx_keycode_update_archive] (
    [prevx_keycode_update_archive_id] int IDENTITY(1,1) NOT NULL,
    [prevx_keycode_update_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [prevx_keycode] varchar(40) NOT NULL,
    [insert_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([prevx_keycode_update_archive_id])
);
CREATE TABLE [dbo].[prevx_license_migration_table] (
    [prevx_migration_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [license_id] int,
    [license_seats] int NOT NULL,
    [license_category_id] int NOT NULL,
    [capability_type_id] tinyint NOT NULL,
    [capability_activation_days] int NOT NULL,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [license_distribution_method_code] varchar(4) NOT NULL,
    [insert_date] datetime,
    [license_category_name] varchar(10) NOT NULL,
    [product_line_id] int NOT NULL
);
CREATE TABLE [dbo].[prevx_license_working] (
    [keycode] varchar(40) NOT NULL,
    [product_line_id] int NOT NULL,
    [license_id] int,
    [license_seats] int NOT NULL,
    [capability_type_id] tinyint NOT NULL,
    [license_category_id] int NOT NULL,
    [capability_activation_days] int NOT NULL,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [license_status_id] int NOT NULL,
    [license_type_id] int NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [max_daily_activations] int NOT NULL,
    [max_child_licenses] int,
    [license_expiration_date] int,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(128),
    [modified_date] datetime NOT NULL,
    [modified_by] nvarchar(128)
);
CREATE TABLE [dbo].[prevx_license_working_license_capability] (
    [license_id] int,
    [capability_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_activation_days] int NOT NULL,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(128),
    [modified_date] datetime NOT NULL,
    [modified_by] nvarchar(128)
);
CREATE TABLE [dbo].[prevx_mar_update] (
    [prevx_mar_update_id] int IDENTITY(1,1) NOT NULL,
    [reference_id] int,
    [mar_id] int NOT NULL,
    [mar_type_id] tinyint NOT NULL,
    [mar_name] varchar(50) NOT NULL,
    [parent_mar_id] int,
    [license_id] int,
    [mar_user_id] bigint,
    [user_email] varchar(100),
    [user_password] varchar(64),
    [first_name] nvarchar(225),
    [last_name] nvarchar(225),
    [display_name] nvarchar(225),
    [insert_date] datetime NOT NULL,
    [mar_status_id] tinyint,
    [encryption_key_hash] varchar(128),
    [mar_user_type_id] tinyint
,
    PRIMARY KEY ([prevx_mar_update_id])
);
CREATE TABLE [dbo].[prevx_mar_update_archive] (
    [prevx_mar_update_archive_id] int IDENTITY(1,1) NOT NULL,
    [prevx_mar_update_id] int NOT NULL,
    [reference_id] int,
    [mar_id] int NOT NULL,
    [mar_type_id] tinyint NOT NULL,
    [mar_name] varchar(50) NOT NULL,
    [parent_mar_id] int,
    [license_id] int,
    [mar_user_id] bigint,
    [user_email] varchar(100),
    [user_password] varchar(64),
    [first_name] nvarchar(225),
    [last_name] nvarchar(225),
    [display_name] nvarchar(225),
    [insert_date] datetime NOT NULL,
    [archive_date] datetime NOT NULL,
    [mar_status_id] tinyint,
    [mar_user_type_id] tinyint
,
    PRIMARY KEY ([prevx_mar_update_archive_id])
);
CREATE TABLE [dbo].[prevx_sync_update] (
    [prevx_sync_update_id] int IDENTITY(1,1) NOT NULL,
    [account_user_name] varchar(100),
    [license_module_type_name] varchar(50),
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([prevx_sync_update_id])
);
CREATE TABLE [dbo].[prevx_sync_update_archive] (
    [prevx_sync_update_archive_id] int IDENTITY(1,1) NOT NULL,
    [prevx_sync_update_id] int NOT NULL,
    [account_user_name] varchar(100),
    [license_module_type_name] varchar(50),
    [insert_date] datetime NOT NULL,
    [archive_date] datetime NOT NULL
,
    PRIMARY KEY ([prevx_sync_update_archive_id])
);
CREATE TABLE [dbo].[prevx_update] (
    [prevx_update_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_bulk_load_id] int,
    [prevx_update_type_id] tinyint NOT NULL,
    [prevx_update_status_id] tinyint NOT NULL,
    [response_id] smallint,
    [response_text] varchar(5000),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime,
    [priority] tinyint
,
    PRIMARY KEY ([prevx_update_id])
);
CREATE TABLE [dbo].[prevx_update_archive] (
    [prevx_update_archive_id] int IDENTITY(1,1) NOT NULL,
    [prevx_update_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_bulk_load_id] int,
    [prevx_update_type_id] tinyint NOT NULL,
    [prevx_update_status_id] tinyint NOT NULL,
    [response_id] smallint,
    [response_text] varchar(5000),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime,
    [archive_date] datetime NOT NULL,
    [priority] tinyint
,
    PRIMARY KEY ([prevx_update_archive_id])
);
CREATE TABLE [dbo].[prevx_update_backup] (
    [prevx_update_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_bulk_load_id] int,
    [prevx_update_type_id] tinyint NOT NULL,
    [prevx_update_status_id] tinyint NOT NULL,
    [response_id] smallint,
    [response_text] varchar(5000),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime,
    [priority] tinyint
);
CREATE TABLE [dbo].[prevx_update_failure] (
    [prevx_update_failure_id] int IDENTITY(1,1) NOT NULL,
    [prevx_update_id] int NOT NULL,
    [license_id] int NOT NULL,
    [prevx_update_type_id] tinyint NOT NULL,
    [prevx_update_status_id] tinyint NOT NULL,
    [response_id] smallint,
    [response_text] varchar(5000),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime,
    [failure_date] datetime NOT NULL,
    [priority] tinyint
,
    PRIMARY KEY ([prevx_update_failure_id])
);
CREATE TABLE [dbo].[prevx_update_license] (
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [priority] tinyint
);
CREATE TABLE [dbo].[prevx_update_message] (
    [prevx_update_message_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [prevx_update_status_id] tinyint NOT NULL,
    [response_id] smallint,
    [response_text] varchar(5000),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime
,
    PRIMARY KEY ([prevx_update_message_id])
);
CREATE TABLE [dbo].[prevx_update_message_archive] (
    [prevx_update_message_archive_id] int IDENTITY(1,1) NOT NULL,
    [prevx_update_message_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_bulk_load_id] int,
    [prevx_update_type_id] tinyint NOT NULL,
    [prevx_update_status_id] tinyint NOT NULL,
    [response_id] smallint,
    [response_text] varchar(5000),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime,
    [archive_date] datetime NOT NULL
,
    PRIMARY KEY ([prevx_update_message_archive_id])
);
CREATE TABLE [dbo].[prevx_update_s2] (
    [prevx_update_s2_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_bulk_load_id] int,
    [prevx_update_type_id] tinyint NOT NULL,
    [prevx_update_status_id] tinyint NOT NULL,
    [response_id] smallint,
    [response_text] varchar(5000),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime
,
    PRIMARY KEY ([prevx_update_s2_id])
);
CREATE TABLE [dbo].[prevx_update_s2_archive] (
    [prevx_update_s2_archive_id] int IDENTITY(1,1) NOT NULL,
    [prevx_update_s2_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_bulk_load_id] int,
    [prevx_update_type_id] tinyint NOT NULL,
    [prevx_update_status_id] tinyint NOT NULL,
    [response_id] smallint,
    [response_text] varchar(5000),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime,
    [archive_date] datetime NOT NULL
,
    PRIMARY KEY ([prevx_update_s2_archive_id])
);
CREATE TABLE [dbo].[prevx_update_s2_failure] (
    [prevx_update_s2_failure_id] int IDENTITY(1,1) NOT NULL,
    [prevx_update_s2_id] int NOT NULL,
    [license_id] int NOT NULL,
    [prevx_update_type_id] tinyint NOT NULL,
    [prevx_update_status_id] tinyint NOT NULL,
    [response_id] smallint,
    [response_text] varchar(5000),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime,
    [failure_date] datetime NOT NULL
,
    PRIMARY KEY ([prevx_update_s2_failure_id])
);
CREATE TABLE [dbo].[prevx_update_status] (
    [prevx_update_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [prevx_update_status_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([prevx_update_status_id])
);
CREATE TABLE [dbo].[prevx_update_type] (
    [prevx_update_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [prevx_update_type_name] varchar(20) NOT NULL,
    [update_method] varchar(20) NOT NULL
,
    PRIMARY KEY ([prevx_update_type_id])
);
CREATE TABLE [dbo].[Price_Dates_sequence] (
    [sequence_id] int IDENTITY(1,1) NOT NULL,
    [insert_date] datetime
);
CREATE TABLE [dbo].[Prices_Dates_sequence] (
    [sequence_id] int IDENTITY(1,1) NOT NULL,
    [insert_date] datetime
);
CREATE TABLE [dbo].[Prices_sequence] (
    [sequence_id] int IDENTITY(1,1) NOT NULL,
    [insert_date] datetime
);
CREATE TABLE [dbo].[pricing] (
    [priceid] int IDENTITY(1,1) NOT NULL,
    [upperqty] int,
    [lowerqty] int,
    [product_id] int NOT NULL,
    [discountper] varchar(5)
);
CREATE TABLE [dbo].[product] (
    [product_id] int NOT NULL,
    [product_description] varchar(100) NOT NULL,
    [product_type_id] int NOT NULL,
    [product_family_id] int,
    [product_lifecycle_id] int,
    [license_keycode_type_id] int,
    [root_product_id] int,
    [uses_keycode] int NOT NULL,
    [cd_product_id] int,
    [retail_price] money,
    [pict] varchar(100),
    [basename] varchar(32),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [online_refund_flag] tinyint
,
    PRIMARY KEY ([product_id])
);
CREATE TABLE [dbo].[product_autorenewal_cycle] (
    [product_id] int NOT NULL,
    [message_autorenewal_cycle_id] tinyint NOT NULL
,
    PRIMARY KEY ([message_autorenewal_cycle_id], [product_id])
);
CREATE TABLE [dbo].[product_capability] (
    [product_capability_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [capability_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_activation_days] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([product_capability_id])
);
CREATE TABLE [dbo].[product_capability_working] (
    [product_capability_working_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [capability_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_activation_days] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([product_capability_working_id])
);
CREATE TABLE [dbo].[product_description] (
    [product_description_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [product_description] nvarchar(200) NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [last_modified_date] datetime,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([product_description_id])
);
CREATE TABLE [dbo].[product_extension] (
    [product_extension_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [product_extension_json] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([product_extension_id])
);
CREATE TABLE [dbo].[product_family] (
    [product_family_id] int IDENTITY(1,1) NOT NULL,
    [product_family_description] varchar(50) NOT NULL,
    [product_family_prefix] char(2),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([product_family_id])
);
CREATE TABLE [dbo].[product_family_autorenewal] (
    [product_family_autorenewal_id] int IDENTITY(1,1) NOT NULL,
    [product_family_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([product_family_autorenewal_id])
);
CREATE TABLE [dbo].[product_family_discount] (
    [product_family_discount_id] int IDENTITY(1,1) NOT NULL,
    [product_family_id] int NOT NULL,
    [discount_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([product_family_discount_id])
);
CREATE TABLE [dbo].[product_group_members] (
    [product_group_key] int NOT NULL,
    [product_id] int NOT NULL,
    [product_group_id] int NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([product_group_key])
);
CREATE TABLE [dbo].[product_license_category] (
    [product_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [current_license_category_id] tinyint
,
    PRIMARY KEY ([license_category_id], [product_id])
);
CREATE TABLE [dbo].[product_license_category_keycode_type] (
    [product_license_category_keycode_type_id] int IDENTITY(1,1) NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [license_keycode_type_id] int NOT NULL,
    [site_display] tinyint NOT NULL
,
    PRIMARY KEY ([product_license_category_keycode_type_id])
);
CREATE TABLE [dbo].[product_license_category_license_attribute] (
    [product_license_category_license_attribute_id] int IDENTITY(1,1) NOT NULL,
    [license_category_id] int NOT NULL,
    [product_type_id] int NOT NULL,
    [locale] char(5) NOT NULL,
    [license_attribute_id] int NOT NULL,
    [license_attribute_license_value] int NOT NULL,
    [current_license_attribute_id] int,
    [current_license_attribute_license_value] int,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([product_license_category_license_attribute_id])
);
CREATE TABLE [dbo].[product_license_category_seat] (
    [license_category_id] tinyint NOT NULL,
    [seats] int NOT NULL,
    [product_license_category_seat_id] int IDENTITY(1,1) NOT NULL,
    [site_display] tinyint,
    [configuration_option] tinyint
,
    PRIMARY KEY ([product_license_category_seat_id])
);
CREATE TABLE [dbo].[product_license_category_storage] (
    [license_category_id] tinyint NOT NULL,
    [storage_gb] int NOT NULL,
    [product_license_category_storage_id] int IDENTITY(1,1) NOT NULL
,
    PRIMARY KEY ([product_license_category_storage_id])
);
CREATE TABLE [dbo].[product_license_category_upgrade] (
    [license_category_id] tinyint NOT NULL,
    [upgrade_license_category_id] tinyint NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [product_license_category_upgrade_id] int IDENTITY(1,1) NOT NULL,
    [site_display] tinyint,
    [item_hierarchy_id] tinyint,
    [upgrade_path] int
,
    PRIMARY KEY ([product_license_category_upgrade_id])
);
CREATE TABLE [dbo].[product_license_category_years] (
    [license_category_id] tinyint NOT NULL,
    [years] float NOT NULL,
    [years_description] varchar(20),
    [product_license_category_years_id] int IDENTITY(1,1) NOT NULL,
    [site_display] tinyint
,
    PRIMARY KEY ([product_license_category_years_id])
);
CREATE TABLE [dbo].[product_license_module] (
    [product_license_module_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [license_module_id] tinyint NOT NULL,
    [license_module_type_id] tinyint NOT NULL,
    [days] int
,
    PRIMARY KEY ([product_license_module_id])
);
CREATE TABLE [dbo].[product_license_module_working] (
    [product_license_module_working_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [license_module_id] tinyint NOT NULL,
    [license_module_type_id] tinyint NOT NULL,
    [days] int,
    [module_seats] int
,
    PRIMARY KEY ([product_license_module_working_id])
);
CREATE TABLE [dbo].[product_lifecycle] (
    [product_lifecycle_id] int IDENTITY(1,1) NOT NULL,
    [product_lifecycle_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([product_lifecycle_id])
);
CREATE TABLE [dbo].[product_line] (
    [product_line_id] int IDENTITY(1,1) NOT NULL,
    [product_line_description] varchar(40) NOT NULL,
    [product_line_prefix] char(2) NOT NULL,
    [root_product_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [status] tinyint,
    [product_line_cart_type] varchar(20)
,
    PRIMARY KEY ([product_line_id])
);
CREATE TABLE [dbo].[product_line_license_distribution_method] (
    [product_line_license_distribution_method_id] int IDENTITY(1,1) NOT NULL,
    [product_line_id] int NOT NULL,
    [license_keycode_type_id] int NOT NULL,
    [trial] tinyint NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [license_category_id] int
,
    PRIMARY KEY ([product_line_license_distribution_method_id])
);
CREATE TABLE [dbo].[product_line_license_seat_distribution] (
    [product_line_license_distribution_id] int IDENTITY(1,1) NOT NULL,
    [product_line_id] int NOT NULL,
    [license_distribution_method_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [seat_count_enforcement] tinyint
,
    PRIMARY KEY ([license_category_id], [license_distribution_method_id], [product_line_id])
);
CREATE TABLE [dbo].[product_line_product] (
    [product_line_id] int NOT NULL,
    [product_id] int NOT NULL
,
    PRIMARY KEY ([product_id], [product_line_id])
);
CREATE TABLE [dbo].[product_offer] (
    [offer_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int,
    [offer_product_id] int,
    [offer_enabled] bit NOT NULL,
    [offer_type] int,
    [offer_type_group] int,
    [offer_dest_id] int,
    [product_offer_price] money,
    [offer_code] varchar(10),
    [display_order] int,
    [product_offer_desc] varchar(100),
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([offer_id])
);
CREATE TABLE [dbo].[product_offer_dest] (
    [offer_dest_id] int NOT NULL,
    [offer_destination] varchar(100)
,
    PRIMARY KEY ([offer_dest_id])
);
CREATE TABLE [dbo].[product_offer_history] (
    [product_offer_history_id] int IDENTITY(1,1) NOT NULL,
    [offer_id] int NOT NULL,
    [product_id] int,
    [offer_product_id] int,
    [offer_enabled] bit NOT NULL,
    [offer_type] int,
    [offer_type_group] int,
    [offer_dest_id] int,
    [product_offer_price] money,
    [offer_code] varchar(10),
    [display_order] int,
    [product_offer_desc] varchar(100),
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200),
    [history_date] datetime,
    [history_by] varchar(200),
    [history_reason] varchar(100)
,
    PRIMARY KEY ([product_offer_history_id])
);
CREATE TABLE [dbo].[product_offer_scenario] (
    [offer_scenario_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int,
    [offer_dest_id] int,
    [offer_count] int,
    [scenario_enabled] bit
,
    PRIMARY KEY ([offer_scenario_id])
);
CREATE TABLE [dbo].[product_offer_scenario_history] (
    [product_offer_scenario_history_id] int IDENTITY(1,1) NOT NULL,
    [offer_scenario_id] int NOT NULL,
    [product_id] int,
    [offer_dest_id] int,
    [offer_count] int,
    [scenario_enabled] bit,
    [history_date] datetime,
    [history_by] varchar(200),
    [history_reason] varchar(100)
,
    PRIMARY KEY ([product_offer_scenario_history_id])
);
CREATE TABLE [dbo].[product_offer_type] (
    [offer_type] int NOT NULL,
    [offer_description] varchar(100)
,
    PRIMARY KEY ([offer_type])
);
CREATE TABLE [dbo].[product_platform] (
    [product_platform_id] tinyint IDENTITY(1,1) NOT NULL,
    [product_platform_name] varchar(100) NOT NULL,
    [vault_provisioning] varchar(20)
,
    PRIMARY KEY ([product_platform_id])
);
CREATE TABLE [dbo].[product_pricing] (
    [product_pricing_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [currency_id] int NOT NULL,
    [retail_price] money NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [edu_nfp_price] money,
    [govt_price] money,
    [usage_price] money
,
    PRIMARY KEY ([product_pricing_id])
);
CREATE TABLE [dbo].[product_pricing_history] (
    [product_pricing_history_id] int IDENTITY(1,1) NOT NULL,
    [product_pricing_id] int NOT NULL,
    [product_id] int NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [location_code] varchar(3) NOT NULL,
    [currency_id] int NOT NULL,
    [retail_price] money NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([product_pricing_history_id])
);
CREATE TABLE [dbo].[product_pricing_level] (
    [product_pricing_level_id] tinyint IDENTITY(1,1) NOT NULL,
    [pricing_level] varchar(20) NOT NULL,
    [pricing_level_description] varchar(200) NOT NULL
,
    PRIMARY KEY ([product_pricing_level_id])
);
CREATE TABLE [dbo].[product_rebate] (
    [product_id] int NOT NULL,
    [rebate_id] int NOT NULL
,
    PRIMARY KEY ([product_id], [rebate_id])
);
CREATE TABLE [dbo].[product_redemption] (
    [product_redemption_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [redemption_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([product_redemption_id])
);
CREATE TABLE [dbo].[product_registrations] (
    [customer_code] varchar(16) NOT NULL,
    [product_id] int NOT NULL,
    [product_version] varchar(20),
    [major_version] smallint,
    [minor_version] smallint,
    [initial_install_date] datetime,
    [registration_date] datetime NOT NULL,
    [product_registration_number] varchar(50) NOT NULL,
    [affiliate_code] varchar(16),
    [cd_code] varchar(32),
    [distribution_channel_code] varchar(32),
    [distribution_role_code] varchar(32),
    [master_override_affiliate_num] int,
    [refer_to_product_code] int,
    [program_status] char(1),
    [build] varchar(32),
    [referral_source_id] smallint,
    [visitorid] bigint,
    [last_modified] datetime,
    [prKeycode] varchar(20),
    [ID] int IDENTITY(1,1) NOT NULL,
    [IsKeycodeValid] bit,
    [language_code] char(2),
    [location_code] char(3)
);
CREATE TABLE [dbo].[product_seat] (
    [product_seat_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [seats] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [current_seats] int
,
    PRIMARY KEY ([product_seat_id])
);
CREATE TABLE [dbo].[product_storage] (
    [product_storage_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [storage_gb] int NOT NULL,
    [current_storage_gb] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([product_storage_id])
);
CREATE TABLE [dbo].[product_type] (
    [product_type_id] int IDENTITY(1,1) NOT NULL,
    [product_type_description] varchar(50),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([product_type_id])
);
CREATE TABLE [dbo].[product_update_policies] (
    [product_update_key] int NOT NULL,
    [product_id] int NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [update_limit_id] smallint NOT NULL,
    [update_limit] int,
    [def_limit] smallint,
    [plugin_limit] smallint,
    [day_limit] int,
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([product_update_key])
);
CREATE TABLE [dbo].[product_upgrade_method] (
    [product_upgrade_method_id] int IDENTITY(1,1) NOT NULL,
    [operation_code] int NOT NULL,
    [product_id] int NOT NULL,
    [upgrade_method_description] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([product_upgrade_method_id])
);
CREATE TABLE [dbo].[product_version_operating_system] (
    [product_version_id] int NOT NULL,
    [operating_system_id] int NOT NULL,
    [support] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([operating_system_id], [product_version_id])
);
CREATE TABLE [dbo].[product_version_product_upgrade_method] (
    [product_version_id] int NOT NULL,
    [product_upgrade_method_id] int NOT NULL,
    [support] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([product_upgrade_method_id], [product_version_id])
);
CREATE TABLE [dbo].[product_versions] (
    [product_version_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [product_version] varchar(20) NOT NULL,
    [major_version] int,
    [minor_version] int,
    [release] int,
    [start_date] datetime,
    [end_date] datetime,
    [standard_price] decimal(8,4),
    [tagline] varchar(255),
    [last_modified] datetime,
    [bld] int,
    [releaseStatus] int,
    [installer_exe] varchar(50)
,
    PRIMARY KEY ([product_version_id])
);
CREATE TABLE [dbo].[product_versions_production] (
    [product_id] int NOT NULL,
    [product_version] varchar(20) NOT NULL,
    [major_version] int,
    [minor_version] int,
    [start_date] datetime,
    [end_date] datetime,
    [standard_price] decimal(8,4),
    [tagline] varchar(255),
    [last_modified] datetime,
    [bld] int,
    [releaseStatus] int
);
CREATE TABLE [dbo].[product_years] (
    [product_id] int NOT NULL,
    [years] float NOT NULL,
    [upgrade_months] tinyint,
    [upgrade_days] int
,
    PRIMARY KEY ([product_id], [years])
);
CREATE TABLE [dbo].[ProductFamily] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Name] varchar(50) NOT NULL,
    [InsertDate] datetime NOT NULL,
    [Campaign] bit NOT NULL
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[ProductGroup] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [Name] varchar(50) NOT NULL,
    [NameOld] varchar(50),
    [InsertDate] smalldatetime
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[ProductGroupMember] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [ProductGroupID] int,
    [ProductID] int NOT NULL,
    [Active] bit NOT NULL,
    [ProductDesc] varchar(100),
    [ProductGroup] varchar(50)
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[products] (
    [product_id] int NOT NULL,
    [description] varchar(100) NOT NULL,
    [basename] varchar(32),
    [discontinued_date] datetime,
    [renewal_product_id] int,
    [cd_product_id] int,
    [renewal_spcode] varchar(50),
    [serial_threshold1] int,
    [serial_threshold2] int,
    [shipable] tinyint,
    [isretail] tinyint,
    [internal_use] tinyint,
    [subscription_base] money,
    [subscription_duration] smallint,
    [subscription_deferred_code] char(6),
    [subscription_deferred_amount] money,
    [uses_keycode] tinyint,
    [keycode_prefix] char(2),
    [last_modified] datetime NOT NULL,
    [ProductFamilyID] int,
    [ProductFamily] varchar(50),
    [Trial] bit,
    [Days] int,
    [Subscription] varchar(3) NOT NULL,
    [retail_price] money,
    [pict] varchar(100),
    [downloadable] tinyint,
    [reseller] smallint,
    [is_consumer] tinyint,
    [is_enterprise] tinyint
,
    PRIMARY KEY ([product_id])
);
CREATE TABLE [dbo].[ProductsHistory] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [InsertDate] datetime NOT NULL,
    [product_id] int NOT NULL,
    [description] varchar(100) NOT NULL,
    [basename] varchar(32),
    [discontinued_date] datetime,
    [renewal_product_id] int,
    [cd_product_id] int,
    [renewal_spcode] varchar(50),
    [serial_threshold1] int,
    [serial_threshold2] int,
    [shipable] tinyint,
    [isretail] tinyint NOT NULL,
    [internal_use] tinyint NOT NULL,
    [subscription_base] money,
    [subscription_duration] smallint,
    [subscription_deferred_code] char(6),
    [subscription_deferred_amount] money,
    [uses_keycode] tinyint NOT NULL,
    [keycode_prefix] char(2),
    [last_modified] datetime NOT NULL,
    [ProductFamilyID] int,
    [ProductFamily] varchar(50),
    [Trial] bit NOT NULL,
    [Days] int,
    [Subscription] varchar(3) NOT NULL
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[ProductSubscription] (
    [ProductID] int NOT NULL,
    [Days] smallint NOT NULL,
    [ProductDesc] varchar(100),
    [InsertDate] datetime NOT NULL
,
    PRIMARY KEY ([ProductID])
);
CREATE TABLE [dbo].[purchase_order] (
    [purchase_order_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL,
    [purchase_order] varchar(100),
    [purchase_order_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([purchase_order_id])
);
CREATE TABLE [dbo].[purchase_order_audit] (
    [purchase_order_audit_id] int IDENTITY(1,1) NOT NULL,
    [purchase_order_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [purchase_order] varchar(100),
    [purchase_order_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([purchase_order_audit_id])
);
CREATE TABLE [dbo].[quantity_discount] (
    [low] smallint NOT NULL,
    [high] smallint NOT NULL,
    [discount] float NOT NULL,
    [across_products] tinyint
);
CREATE TABLE [dbo].[rashmi] (
    [ID] int NOT NULL,
    [LastName] varchar(255) NOT NULL,
    [FirstName] varchar(255),
    [Age] int,
    [test] nchar(10)
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[rashmi_may26] (
    [lastwaittype] nchar(32) NOT NULL,
    [dbid] smallint NOT NULL,
    [cpu] int NOT NULL,
    [login_time] datetime NOT NULL,
    [last_batch] datetime NOT NULL,
    [open_tran] smallint NOT NULL,
    [status] nchar(30) NOT NULL,
    [hostname] nchar(128) NOT NULL,
    [program_name] nchar(128) NOT NULL,
    [cmd] nchar(16) NOT NULL,
    [nt_domain] nchar(128) NOT NULL,
    [loginame] nchar(128) NOT NULL
);
CREATE TABLE [dbo].[Realm] (
    [id] numeric(10,0),
    [Realm] varchar(80) NOT NULL,
    [LoginFormUrl] varchar(255) NOT NULL,
    [AuthClass] varchar(45) NOT NULL,
    [AuthTableName] varchar(255) NOT NULL,
    [DefaultStartPage] varchar(255) NOT NULL
);
CREATE TABLE [dbo].[rebate] (
    [rebate_id] int IDENTITY(1,1) NOT NULL,
    [rebate_description] varchar(100) NOT NULL,
    [rebate_code] varchar(20) NOT NULL,
    [rebate_type_id] tinyint NOT NULL,
    [rebate_value] money NOT NULL,
    [active] tinyint NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([rebate_id])
);
CREATE TABLE [dbo].[rebate_alternate] (
    [rebate_alternate_id] int IDENTITY(1,1) NOT NULL,
    [alternate_description] varchar(100) NOT NULL,
    [alternate_code] varchar(20) NOT NULL,
    [alternate_value] money NOT NULL,
    [alternate_expiration_date] datetime NOT NULL,
    [rebate_alternate_type_id] tinyint NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([rebate_alternate_id])
);
CREATE TABLE [dbo].[rebate_alternate_type] (
    [rebate_alternate_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [rebate_alternate_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([rebate_alternate_type_id])
);
CREATE TABLE [dbo].[rebate_status] (
    [rebate_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [rebate_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([rebate_status_id])
);
CREATE TABLE [dbo].[rebate_type] (
    [rebate_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [rebate_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([rebate_type_id])
);
CREATE TABLE [dbo].[redemption] (
    [redemption_id] int IDENTITY(1,1) NOT NULL,
    [redemption_name] varchar(100) NOT NULL,
    [redemption_vendor_id] int NOT NULL,
    [start_date] datetime,
    [end_date] datetime,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([redemption_id])
);
CREATE TABLE [dbo].[redemption_code] (
    [redemption_code_id] int IDENTITY(1,1) NOT NULL,
    [redemption_code] varchar(60) NOT NULL,
    [redemption_id] int NOT NULL,
    [status] tinyint NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([redemption_code_id])
);
CREATE TABLE [dbo].[redemption_product_details] (
    [redemption_product_details_id] int IDENTITY(1,1) NOT NULL,
    [redemption_id] int NOT NULL,
    [license_category_name] varchar(6) NOT NULL,
    [seats] int NOT NULL,
    [redemption_value] int NOT NULL
);
CREATE TABLE [dbo].[redemption_vendor] (
    [redemption_vendor_id] int IDENTITY(1,1) NOT NULL,
    [redemption_vendor_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([redemption_vendor_id])
);
CREATE TABLE [dbo].[refund_items] (
    [invoice_code] varchar(16) NOT NULL,
    [line_item] int NOT NULL,
    [product_id] int NOT NULL,
    [product_version] varchar(20),
    [quantity] int NOT NULL,
    [extended_price] decimal(10,4) NOT NULL,
    [entered_timestamp] datetime NOT NULL,
    [previous_version] varchar(20),
    [is_update] smallint,
    [cross_sell] tinyint,
    [shipping_status] tinyint,
    [shipped_date] datetime,
    [serial_number] varchar(50),
    [refund_reason_code] smallint,
    [auth_trans_id] varchar(40),
    [auth_batch_id] datetime,
    [deferred_income] money,
    [last_modified] datetime NOT NULL,
    [tax_item_amount] decimal(15,2),
    [IsKeycodeValid] bit,
    [full_retail_price] decimal(10,4),
    [special_code] varchar(12),
    [effective_date] datetime
);
CREATE TABLE [dbo].[refund_items_archive] (
    [invoice_code] varchar(16) NOT NULL,
    [line_item] int NOT NULL,
    [product_id] int NOT NULL,
    [product_version] varchar(20),
    [quantity] int NOT NULL,
    [extended_price] decimal(10,4) NOT NULL,
    [entered_timestamp] datetime NOT NULL,
    [previous_version] varchar(20),
    [is_update] smallint,
    [cross_sell] tinyint,
    [shipping_status] tinyint,
    [shipped_date] datetime,
    [serial_number] varchar(50),
    [refund_reason_code] smallint,
    [auth_trans_id] varchar(26),
    [auth_batch_id] datetime,
    [deferred_income] money,
    [last_modified] datetime NOT NULL,
    [tax_item_amount] decimal(15,2)
);
CREATE TABLE [dbo].[rept_KeycodeChannel] (
    [KeycodeChannel] varchar(4) NOT NULL,
    [IsValid] bit NOT NULL,
    [Qty] int NOT NULL,
    [LastRun] smalldatetime NOT NULL
);
CREATE TABLE [dbo].[reseller_license] (
    [reseller_license_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [effective_object_id] tinyint NOT NULL,
    [vault_id] int NOT NULL,
    [company_id] int NOT NULL,
    [license_external_reference_id] int NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(50)
,
    PRIMARY KEY ([reseller_license_id])
);
CREATE TABLE [dbo].[reseller_provisioning] (
    [reseller_provisioning_id] int IDENTITY(1,1) NOT NULL,
    [company_id] int NOT NULL,
    [vault_id] int NOT NULL,
    [cep_partner_id] varchar(100) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(50)
,
    PRIMARY KEY ([reseller_provisioning_id])
);
CREATE TABLE [dbo].[retention_model] (
    [retention_model_id] tinyint IDENTITY(1,1) NOT NULL,
    [retention_model_name] varchar(20) NOT NULL,
    [retention_model_description] nvarchar(MAX) NOT NULL,
    [retention_model_type_id] tinyint,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([retention_model_id])
);
CREATE TABLE [dbo].[retention_model_type] (
    [retention_model_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [retention_model_type_name] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([retention_model_type_id])
);
CREATE TABLE [dbo].[s2_license_redemption_log] (
    [s2_license_redemption_log_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [source] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(100) NOT NULL
,
    PRIMARY KEY ([s2_license_redemption_log_id])
);
CREATE TABLE [dbo].[safe_account_user_customer] (
    [safe_account_user_customer_id] int IDENTITY(1,1) NOT NULL,
    [safe_account_user_guid] uniqueidentifier NOT NULL,
    [customer_id] int NOT NULL,
    [allstate_registration_status_id] int,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [allstate_registration_date] datetime
,
    PRIMARY KEY ([safe_account_user_customer_id])
);
CREATE TABLE [dbo].[safe_account_user_customer_audit] (
    [safe_account_user_customer_audit_id] int IDENTITY(1,1) NOT NULL,
    [safe_account_user_customer_id] int NOT NULL,
    [safe_account_user_guid] uniqueidentifier NOT NULL,
    [customer_id] int NOT NULL,
    [allstate_registration_status_id] int,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [audit_date] datetime NOT NULL,
    [audit_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([safe_account_user_customer_audit_id])
);
CREATE TABLE [dbo].[safe_account_user_customer_email] (
    [safe_account_user_customer_email_id] int IDENTITY(1,1) NOT NULL,
    [safe_account_user_customer_id] int NOT NULL,
    [email_address] varchar(100) NOT NULL,
    [email_status_id] tinyint NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([safe_account_user_customer_email_id])
);
CREATE TABLE [dbo].[safe_account_user_customer_json] (
    [safe_account_user_customer_json_id] int IDENTITY(1,1) NOT NULL,
    [safe_account_user_customer_json] nvarchar(2000) NOT NULL,
    [safe_account_user_customer_json_message_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([safe_account_user_customer_json_id])
);
CREATE TABLE [dbo].[safe_account_user_customer_json_message] (
    [safe_account_user_customer_json_message_id] int IDENTITY(1,1) NOT NULL,
    [safe_account_user_customer_json_message] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([safe_account_user_customer_json_message_id])
);
CREATE TABLE [dbo].[sales_order_load] (
    [vendor_order_ID] bigint NOT NULL,
    [report_begin_date] datetime,
    [report_end_date] datetime,
    [report_run_date] datetime,
    [sale_date] nvarchar(30),
    [order_date] nvarchar(30),
    [site_ID] nvarchar(65),
    [total_amount] money,
    [sub_total_amount] money,
    [tax_total] money,
    [order_offer_amount] money,
    [exchange_rate] float,
    [cust_pd_amt] money,
    [payment_method] nvarchar(50),
    [bill_to_last_name] nvarchar(100),
    [bill_to_first_name] nvarchar(50),
    [bill_to_address1] nvarchar(150),
    [bill_to_address2] nvarchar(100),
    [bill_to_address3] nvarchar(100),
    [bill_to_phone_number] nvarchar(15),
    [bill_to_postal_code] nvarchar(32),
    [bill_to_city] nvarchar(25),
    [bill_to_state] nvarchar(2),
    [bill_to_email] nvarchar(64),
    [bill_to_fax] nvarchar(64),
    [bill_to_company_name] nvarchar(50),
    [bill_to_alt_phone] nvarchar(64),
    [bill_to_country] nvarchar(2),
    [line_item_ID] nvarchar(50),
    [transaction_Description] nvarchar(25),
    [quantity] bigint,
    [vendor_product_ID] nvarchar(100),
    [product_ID] nvarchar(20),
    [key_code] nvarchar(100),
    [ship_to_city] nvarchar(48),
    [ship_to_country] nvarchar(50),
    [ship_to_address1] nvarchar(200),
    [ship_to_address2] nvarchar(100),
    [ship_to_address3] nvarchar(100),
    [ship_to_last_name] nvarchar(50),
    [ship_to_first_name] nvarchar(50),
    [ship_to_phone_number] nvarchar(64),
    [ship_to_postal_code] nvarchar(32),
    [ship_to_state] nvarchar(2),
    [ship_to_email] nvarchar(64),
    [ship_to_fax] nvarchar(64),
    [ship_to_company_name] nvarchar(64),
    [ship_to_alt_phone] nvarchar(64),
    [return_type] varchar(50),
    [return_reason] varchar(255),
    [return_date] nvarchar(30),
    [Return_item_keycode] nvarchar(100),
    [Return_Line_item_id] nvarchar(100),
    [line_unit_price] money,
    [line_list_price] money,
    [line_price_per_qty] money,
    [line_tax_amount] money,
    [line_offer_ID] nvarchar(50),
    [line_offer_amount] money,
    [line_value_name] nvarchar(20),
    [line_value] nvarchar(60),
    [order_offer_ID] nvarchar(50),
    [order_locale] nvarchar(10),
    [opt_in] nvarchar(10),
    [program_id] nvarchar(20),
    [customer_id] nvarchar(20),
    [order_value_name] nvarchar(25),
    [order_value] nvarchar(100),
    [order_load_status] nvarchar(50),
    [ts] datetime
);
CREATE TABLE [dbo].[sales_order_load_archive] (
    [vendor_order_ID] nvarchar(255) NOT NULL,
    [report_begin_date] datetime,
    [report_end_date] datetime,
    [report_run_date] datetime,
    [sale_date] nvarchar(255),
    [order_date] nvarchar(255),
    [site_ID] nvarchar(255),
    [total_amount] money,
    [sub_total_amount] money,
    [tax_total] money,
    [order_offer_amount] money,
    [exchange_rate] nvarchar(255),
    [cust_pd_amt] money,
    [payment_method] nvarchar(255),
    [bill_to_last_name] nvarchar(255),
    [bill_to_first_name] nvarchar(255),
    [bill_to_address1] nvarchar(255),
    [bill_to_address2] nvarchar(255),
    [bill_to_address3] nvarchar(255),
    [bill_to_phone_number] nvarchar(255),
    [bill_to_postal_code] nvarchar(255),
    [bill_to_city] nvarchar(255),
    [bill_to_state] nvarchar(255),
    [bill_to_email] nvarchar(255),
    [bill_to_fax] nvarchar(255),
    [bill_to_company_name] nvarchar(255),
    [bill_to_alt_phone] nvarchar(255),
    [bill_to_country] nvarchar(255),
    [line_item_ID] nvarchar(255),
    [transaction_Description] nvarchar(255),
    [quantity] bigint,
    [vendor_product_ID] nvarchar(255),
    [product_ID] nvarchar(255),
    [key_code] nvarchar(255),
    [ship_to_city] nvarchar(255),
    [ship_to_country] nvarchar(255),
    [ship_to_address1] nvarchar(255),
    [ship_to_address2] nvarchar(255),
    [ship_to_address3] nvarchar(255),
    [ship_to_last_name] nvarchar(255),
    [ship_to_first_name] nvarchar(255),
    [ship_to_phone_number] nvarchar(255),
    [ship_to_postal_code] nvarchar(255),
    [ship_to_state] nvarchar(255),
    [ship_to_email] nvarchar(255),
    [ship_to_fax] nvarchar(255),
    [ship_to_company_name] nvarchar(255),
    [ship_to_alt_phone] nvarchar(255),
    [return_type] varchar(255),
    [return_reason] varchar(255),
    [return_date] datetime,
    [Return_item_keycode] nvarchar(255),
    [Return_Line_item_id] nvarchar(255),
    [line_unit_price] money,
    [line_list_price] money,
    [line_price_per_qty] money,
    [line_tax_amount] money,
    [line_offer_ID] nvarchar(255),
    [line_offer_amount] money,
    [line_value_name] nvarchar(255),
    [line_value] nvarchar(60),
    [order_offer_ID] nvarchar(255),
    [order_locale] nvarchar(255),
    [opt_in] nvarchar(255),
    [program_id] nvarchar(255),
    [customer_id] nvarchar(255),
    [order_value_name] nvarchar(255),
    [order_value] nvarchar(255),
    [order_load_status] nvarchar(50),
    [ts] datetime
);
CREATE TABLE [dbo].[sales_order_load_audit] (
    [sales_order_load_audit_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_id] bigint NOT NULL,
    [vendor_id] int NOT NULL,
    [report_run_date] datetime NOT NULL,
    [response_code] int NOT NULL,
    [message] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[sap_account] (
    [sap_account_id] int IDENTITY(1,1) NOT NULL,
    [sap_account_number] int NOT NULL,
    [sap_account_name] nvarchar(140) NOT NULL,
    [sap_partner_function_json] nvarchar(MAX) NOT NULL,
    [company_id] int,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [city] nvarchar(130),
    [state] nvarchar(3),
    [postal_code] nvarchar(32),
    [country_id] smallint,
    [sap_accountgroup] nvarchar(8),
    [sap_customergroup] varchar(50)
,
    PRIMARY KEY ([sap_account_id])
);
CREATE TABLE [dbo].[sap_account_audit] (
    [sap_account_audit_id] int IDENTITY(1,1) NOT NULL,
    [sap_account_id] int NOT NULL,
    [sap_account_number] int NOT NULL,
    [sap_account_name] nvarchar(140) NOT NULL,
    [sap_partner_function_json] nvarchar(MAX) NOT NULL,
    [company_id] int,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [audit_date] datetime NOT NULL,
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [city] nvarchar(130),
    [state] nvarchar(3),
    [postal_code] nvarchar(32),
    [country_id] smallint,
    [sap_accountgroup] nvarchar(8),
    [sap_customergroup] varchar(50)
,
    PRIMARY KEY ([sap_account_audit_id])
);
CREATE TABLE [dbo].[sap_billing_day] (
    [sap_billing_day_id] tinyint IDENTITY(1,1) NOT NULL,
    [sap_billing_day_description] varchar(100) NOT NULL,
    [sap_billing_day_value] varchar(10) NOT NULL,
    [billing_day_of_month] varchar(50) NOT NULL
,
    PRIMARY KEY ([sap_billing_day_id])
);
CREATE TABLE [dbo].[sap_billing_frequency] (
    [sap_billing_frequency_id] tinyint IDENTITY(1,1) NOT NULL,
    [sap_billing_frequency_description] varchar(100) NOT NULL,
    [sap_billing_frequency_value] varchar(10) NOT NULL,
    [billing_frequency] varchar(50) NOT NULL
,
    PRIMARY KEY ([sap_billing_frequency_id])
);
CREATE TABLE [dbo].[sap_ext_I094_customer_summary] (
    [sap_ext_I094_customer_summary_id] int IDENTITY(1,1) NOT NULL,
    [sap_account_number] varchar(10) NOT NULL,
    [party_name] nvarchar(40) NOT NULL,
    [currency_code] varchar(5) NOT NULL,
    [amount_due_total] varchar(15) NOT NULL,
    [amount_past_due] varchar(15),
    [amount_current] varchar(15),
    [amount_credits] varchar(15),
    [address_line_1] nvarchar(35) NOT NULL,
    [address_line_2] nvarchar(40) NOT NULL,
    [address_line_3] nvarchar(40),
    [city] nvarchar(200) NOT NULL,
    [state] nvarchar(200) NOT NULL,
    [zip] nvarchar(200) NOT NULL,
    [country] nvarchar(200) NOT NULL,
    [tax_information] nvarchar(200),
    [log_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[sap_ext_I097_trans_balance] (
    [sap_ext_I097_trans_balance_id] int IDENTITY(1,1) NOT NULL,
    [transaction_number] varchar(10) NOT NULL,
    [currency_code] varchar(5) NOT NULL,
    [transaction_amount] varchar(15) NOT NULL,
    [remaining_balance] varchar(15),
    [log_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[sap_ext_I098_trans_summary] (
    [sap_ext_I098_trans_summary_id] int IDENTITY(1,1) NOT NULL,
    [sap_account_number] varchar(10) NOT NULL,
    [company_code] varchar(4) NOT NULL,
    [fiscal_year] int NOT NULL,
    [document_id] varchar(10) NOT NULL,
    [transaction_number] varchar(10) NOT NULL,
    [currency_code] varchar(5) NOT NULL,
    [transaction_amount] varchar(15) NOT NULL,
    [pretax_amount] varchar(15) NOT NULL,
    [tax_amount] varchar(15),
    [transaction_creation_date] datetime NOT NULL,
    [transaction_date] datetime NOT NULL,
    [due_date] datetime,
    [po_number] varchar(35),
    [log_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[sap_ext_I099_trans_details] (
    [sap_ext_I099_trans_details] int IDENTITY(1,1) NOT NULL,
    [sap_account_number] varchar(10) NOT NULL,
    [company_code] varchar(4) NOT NULL,
    [fiscal_year] int NOT NULL,
    [transaction_number] varchar(10) NOT NULL,
    [transaction_line_number] int NOT NULL,
    [product_code] bigint NOT NULL,
    [product_description] varchar(40) NOT NULL,
    [trans_line_description] varchar(250),
    [currency_code] varchar(5) NOT NULL,
    [quantity] float NOT NULL,
    [uom] varchar(3) NOT NULL,
    [line_amount] varchar(15) NOT NULL,
    [tax_amount] varchar(15),
    [total_line_amount] varchar(15) NOT NULL,
    [from_date] date,
    [to_date] date,
    [contract_term] varchar(25),
    [po_number] varchar(35),
    [log_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[sap_ext_I100_payment_summary] (
    [sap_ext_I100_ext_payment_summary_id] int IDENTITY(1,1) NOT NULL,
    [sap_account_number] varchar(10),
    [payment_date] date NOT NULL,
    [payment_receipt_id] varchar(30),
    [currency_code] varchar(5),
    [payment_amount] varchar(15),
    [log_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[sap_fulfillment_json] (
    [sap_fulfillment_json_id] int IDENTITY(1,1) NOT NULL,
    [sap_fulfillment_json] nvarchar(MAX) NOT NULL,
    [salesforce_opportunity_id] varchar(18),
    [order_header_id] int,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [delivery_type] varchar(10),
    [insert_by] varchar(200),
    [modified_by] varchar(200)
,
    PRIMARY KEY ([sap_fulfillment_json_id])
);
CREATE TABLE [dbo].[sap_fulfillment_json_audit] (
    [sap_fulfillment_json_audit_id] int IDENTITY(1,1) NOT NULL,
    [sap_fulfillment_json_id] int NOT NULL,
    [sap_fulfillment_json] nvarchar(MAX) NOT NULL,
    [salesforce_opportunity_id] varchar(18),
    [order_header_id] int,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [delivery_type] varchar(10),
    [insert_by] varchar(200),
    [modified_by] varchar(200),
    [audit_by] varchar(200),
    [audit_date] datetime
,
    PRIMARY KEY ([sap_fulfillment_json_audit_id])
);
CREATE TABLE [dbo].[sap_fulfillment_segment_json] (
    [sap_fulfillment_segment_json_id] int IDENTITY(1,1) NOT NULL,
    [sap_fulfillment_json] nvarchar(MAX) NOT NULL,
    [salesforce_opportunity_id] varchar(18),
    [order_header_id] int,
    [delivery_type] varchar(10),
    [sap_order_number] int,
    [delivery_document_number] varchar(20),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200),
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200),
    [sap_fulfillment_json_id] int,
    [sap_fulfillment_segment_status_id] int NOT NULL
,
    PRIMARY KEY ([sap_fulfillment_segment_json_id])
);
CREATE TABLE [dbo].[sap_fulfillment_segment_status] (
    [sap_fulfillment_segment_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [sap_fulfillment_segment_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([sap_fulfillment_segment_status_id])
);
CREATE TABLE [dbo].[sap_legal_entity] (
    [sap_legal_entity_id] int IDENTITY(1,1) NOT NULL,
    [sap_legal_entity] varchar(100) NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [country_id] smallint NOT NULL
,
    PRIMARY KEY ([sap_legal_entity_id])
);
CREATE TABLE [dbo].[sap_line_transaction_type] (
    [sap_line_transaction_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [sap_line_transaction_type_description] varchar(100) NOT NULL,
    [sap_line_transaction_type_value] varchar(10) NOT NULL,
    [product_type_id] int NOT NULL
,
    PRIMARY KEY ([sap_line_transaction_type_id])
);
CREATE TABLE [dbo].[sap_material_product_json] (
    [sap_material_product_json_id] int IDENTITY(1,1) NOT NULL,
    [sap_material_product_mapping_id] int NOT NULL,
    [product_extension_json] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([sap_material_product_json_id])
);
CREATE TABLE [dbo].[sap_material_product_mapping] (
    [sap_material_product_mapping_id] int IDENTITY(1,1) NOT NULL,
    [sap_material_number] int NOT NULL,
    [sap_material_description] nvarchar(250) NOT NULL,
    [sap_placeholder] varchar(50),
    [license_category_id] tinyint NOT NULL,
    [usage_product] bit NOT NULL,
    [root_product] bit NOT NULL,
    [seats] int NOT NULL,
    [years] tinyint,
    [product_id] int,
    [material_status] varchar(20) NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([sap_material_product_mapping_id])
);
CREATE TABLE [dbo].[sap_order] (
    [sap_order_id] int IDENTITY(1,1) NOT NULL,
    [sap_order_number] varchar(20) NOT NULL,
    [order_header_id] int NOT NULL,
    [sap_order_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [delivery_document_number] varchar(20),
    [delivery_type] varchar(2),
    [insert_by] varchar(200),
    [modified_by] varchar(200)
,
    PRIMARY KEY ([sap_order_id])
);
CREATE TABLE [dbo].[sap_order_audit] (
    [sap_order_audit_id] int IDENTITY(1,1) NOT NULL,
    [sap_order_id] int NOT NULL,
    [sap_order_number] varchar(20) NOT NULL,
    [order_header_id] int NOT NULL,
    [sap_order_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [delivery_document_number] varchar(20),
    [delivery_type] varchar(2),
    [insert_by] varchar(200),
    [modified_by] varchar(200),
    [audit_by] varchar(200),
    [audit_date] datetime
,
    PRIMARY KEY ([sap_order_audit_id])
);
CREATE TABLE [dbo].[sap_order_item] (
    [sap_order_item_id] int IDENTITY(1,1) NOT NULL,
    [sap_order_id] int NOT NULL,
    [sap_order_status_id] tinyint NOT NULL,
    [sap_item_number] varchar(20) NOT NULL,
    [order_item_id] int NOT NULL,
    [usage_item] char(1) NOT NULL,
    [start_date] date NOT NULL,
    [end_date] date NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [delivery_item_number] varchar(20),
    [po_line_number] varchar(20),
    [insert_by] varchar(200),
    [modified_by] varchar(200),
    [delivery_document_number] varchar(20)
,
    PRIMARY KEY ([sap_order_item_id])
);
CREATE TABLE [dbo].[sap_order_item_audit] (
    [sap_order_item_audit_id] int IDENTITY(1,1) NOT NULL,
    [sap_order_item_id] int NOT NULL,
    [sap_order_id] int NOT NULL,
    [sap_order_status_id] tinyint NOT NULL,
    [sap_item_number] varchar(20) NOT NULL,
    [order_item_id] int NOT NULL,
    [usage_item] char(1) NOT NULL,
    [start_date] date NOT NULL,
    [end_date] date NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200),
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200),
    [delivery_item_number] varchar(20),
    [po_line_number] varchar(20),
    [audit_date] datetime NOT NULL,
    [audit_by] varchar(200) NOT NULL,
    [delivery_document_number] varchar(20)
,
    PRIMARY KEY ([sap_order_item_audit_id])
);
CREATE TABLE [dbo].[sap_order_item_license] (
    [sap_order_item_license_id] int IDENTITY(1,1) NOT NULL,
    [sap_order_item_id] int NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([sap_order_item_license_id])
);
CREATE TABLE [dbo].[sap_order_item_license_audit] (
    [sap_order_item_license_audit_id] int IDENTITY(1,1) NOT NULL,
    [sap_order_item_license_id] int NOT NULL,
    [sap_order_item_id] int NOT NULL,
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200),
    [audit_by] varchar(200),
    [audit_date] datetime
,
    PRIMARY KEY ([sap_order_item_license_audit_id])
);
CREATE TABLE [dbo].[sap_order_item_rma] (
    [sap_rma_order_item_id] int IDENTITY(1,1) NOT NULL,
    [sap_rma_order_id] int NOT NULL,
    [order_item_id] int NOT NULL,
    [usage_item] char(1) NOT NULL,
    [start_date] date NOT NULL,
    [end_date] date NOT NULL,
    [delivery_item_number] varchar(20),
    [po_line_number] varchar(20),
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [delivery_document_number] varchar(20)
,
    PRIMARY KEY ([sap_rma_order_item_id])
);
CREATE TABLE [dbo].[sap_order_rma] (
    [sap_rma_order_id] int IDENTITY(1,1) NOT NULL,
    [sap_order_number] varchar(20) NOT NULL,
    [order_header_id] int NOT NULL,
    [opportunity_id] varchar(18) NOT NULL,
    [delivery_document_number] varchar(20),
    [delivery_type] varchar(2),
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([sap_rma_order_id])
);
CREATE TABLE [dbo].[sap_order_status] (
    [sap_order_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [sap_order_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([sap_order_status_id])
);
CREATE TABLE [dbo].[sap_partner_function] (
    [sap_partner_function_id] tinyint IDENTITY(1,1) NOT NULL,
    [partner_function_name_german] varchar(10) NOT NULL,
    [partner_function_name_english] varchar(10) NOT NULL,
    [partner_function_description] nvarchar(100) NOT NULL
,
    PRIMARY KEY ([sap_partner_function_id])
);
CREATE TABLE [dbo].[sap_partner_function_order_company_type] (
    [sap_partner_function_order_company_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [partner_function_name_german] varchar(10) NOT NULL,
    [order_company_type_id] tinyint NOT NULL
,
    PRIMARY KEY ([sap_partner_function_order_company_type_id])
);
CREATE TABLE [dbo].[serial_numbers] (
    [product_id] int NOT NULL,
    [serial_number] varchar(50) NOT NULL
,
    PRIMARY KEY ([product_id], [serial_number])
);
CREATE TABLE [dbo].[session] (
    [session_id] bigint IDENTITY(1,1) NOT NULL,
    [session_key] uniqueidentifier NOT NULL,
    [session_json] varchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL,
    [expiration_date] datetime NOT NULL
);
CREATE TABLE [dbo].[session_tracking_relayware_session] (
    [session_id] varchar(32),
    [insert_date] datetime,
    [last_modified_date] datetime NOT NULL
);
CREATE TABLE [dbo].[sessions] (
    [sessionid] decimal(18,0) NOT NULL,
    [referrer_page] varchar(100),
    [site_path] varchar(8000) NOT NULL,
    [date_visited] datetime NOT NULL,
    [rc] int,
    [rsc] varchar(32),
    [ac] varchar(32),
    [mo] varchar(32),
    [w1] varchar(32),
    [w2] varchar(32),
    [w3] varchar(32),
    [pc] varchar(32),
    [dcc] varchar(32),
    [drc] varchar(32),
    [cd] varchar(32),
    [pu] varchar(32),
    [pd1] varchar(32),
    [pd2] varchar(32),
    [pd3] varchar(32),
    [remote_addr] varchar(100)
,
    PRIMARY KEY ([sessionid])
);
CREATE TABLE [dbo].[sfdc_account] (
    [Id] char(18) NOT NULL,
    [IsDeleted] bit,
    [Name] nvarchar(255),
    [Type] nvarchar(40),
    [BillingStreet] nvarchar(255),
    [BillingCity] nvarchar(40),
    [BillingState] nvarchar(80),
    [BillingPostalCode] nvarchar(20),
    [BillingCountry] nvarchar(80),
    [BillingStateCode] nvarchar(10),
    [BillingCountryCode] nvarchar(10),
    [BillingAddress] nvarchar(16),
    [ShippingStreet] nvarchar(255),
    [ShippingCity] nvarchar(40),
    [ShippingState] nvarchar(80),
    [ShippingPostalCode] nvarchar(20),
    [ShippingCountry] nvarchar(80),
    [ShippingStateCode] nvarchar(10),
    [ShippingCountryCode] nvarchar(10),
    [ShippingAddress] nvarchar(16),
    [Phone] nvarchar(40),
    [Website] nvarchar(255),
    [CreatedDate] datetime,
    [CreatedById] char(18),
    [OwnerId] char(18),
    [LastModifiedDate] datetime,
    [LastModifiedById] char(18),
    [Valid_Address__c] bit,
    [Valid_Address_Message__c] nvarchar(255),
    [Contracting_Entity__c] nvarchar(50),
    [Oracle_Account_ID__c] nvarchar(20),
    [Invoicing_Contact__c] char(18),
    [Partner_Order_Confirmation_Contact__c] char(18),
    [Partner_Contract_Payment_Terms__c] nvarchar(255),
    [VAT_Number__c] nvarchar(255),
    [AccountNameClean] nvarchar(255),
    [duplicate_exists] int
);
CREATE TABLE [dbo].[sfdc_account_type_sale_type] (
    [sfdc_account_type_sale_type_id] int IDENTITY(1,1) NOT NULL,
    [account_type_name] nvarchar(40),
    [sfdc_sale_type_id] int NOT NULL
,
    PRIMARY KEY ([sfdc_account_type_sale_type_id])
);
CREATE TABLE [dbo].[sfdc_api_status] (
    [sfdc_api_status_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_http_status] int NOT NULL,
    [sfdc_api_response_description] varchar(2000)
,
    PRIMARY KEY ([sfdc_api_status_id])
);
CREATE TABLE [dbo].[sfdc_assignment] (
    [sfdc_assignment_id] int IDENTITY(1,1) NOT NULL,
    [distribution_geography_id] tinyint NOT NULL,
    [country_id] smallint NOT NULL,
    [state] varchar(3),
    [assignment_user_id] int,
    [sfdc_assignment_type_id] int NOT NULL,
    [product_type_id] int NOT NULL
);
CREATE TABLE [dbo].[sfdc_assignment_type] (
    [sfdc_assignment_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [assignment_type_name] varchar(50)
);
CREATE TABLE [dbo].[sfdc_backfill] (
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [order_header_id] int NOT NULL,
    [partner_name] nvarchar(100) NOT NULL,
    [partner_id] int NOT NULL,
    [vendor_order_code] varchar(100) NOT NULL,
    [insert_date] datetime
);
CREATE TABLE [dbo].[sfdc_campaign] (
    [sfdc_campaign_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_campaign_old_org] varchar(50),
    [sfdc_campaign_new_org] varchar(50),
    [sfdc_campaign_description] nvarchar(100),
    [sfdc_campaign_OT] varchar(50),
    [insert_date] datetime
,
    PRIMARY KEY ([sfdc_campaign_id])
);
CREATE TABLE [dbo].[sfdc_case] (
    [sfdc_case_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_case_type_id] tinyint NOT NULL,
    [license_id] int NOT NULL,
    [salesforce_case_id] varchar(18) NOT NULL,
    [parent_sfdc_case_id] int,
    [case_create_json] nvarchar(MAX),
    [sfdc_case_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([sfdc_case_id])
);
CREATE TABLE [dbo].[sfdc_case_status] (
    [sfdc_case_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [sfdc_case_status_name] varchar(100) NOT NULL
,
    PRIMARY KEY ([sfdc_case_status_id])
);
CREATE TABLE [dbo].[sfdc_case_type] (
    [sfdc_case_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [sfdc_case_type_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([sfdc_case_type_id])
);
CREATE TABLE [dbo].[sfdc_case_type_category_mapping] (
    [sfdc_case_type_category_mapping_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_case_type_id] tinyint NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [product_extention_json] nvarchar(MAX)
,
    PRIMARY KEY ([sfdc_case_type_category_mapping_id])
);
CREATE TABLE [dbo].[sfdc_contact] (
    [Id] char(18) NOT NULL,
    [IsDeleted] bit,
    [AccountId] char(18),
    [LastName] nvarchar(80),
    [FirstName] nvarchar(40),
    [MailingStreet] nvarchar(255),
    [MailingCity] nvarchar(40),
    [MailingState] nvarchar(80),
    [MailingPostalCode] nvarchar(20),
    [MailingCountry] nvarchar(80),
    [MailingStateCode] nvarchar(10),
    [MailingCountryCode] nvarchar(10),
    [MailingLatitude] decimal(20,17),
    [MailingLongitude] decimal(20,17),
    [MailingGeocodeAccuracy] nvarchar(40),
    [MailingAddress] nvarchar(16),
    [Phone] nvarchar(40),
    [Email] nvarchar(80),
    [CreatedDate] datetime,
    [CreatedById] char(18),
    [LastModifiedDate] datetime,
    [LastModifiedById] char(18),
    [duplicate_exists] int
);
CREATE TABLE [dbo].[sfdc_geography] (
    [sfdc_geography_id] tinyint IDENTITY(1,1) NOT NULL,
    [geography_name] varchar(10)
);
CREATE TABLE [dbo].[sfdc_geography_country] (
    [sfdc_geography_id] tinyint,
    [country_id] smallint
);
CREATE TABLE [dbo].[sfdc_lead_contact_type] (
    [sfdc_lead_contact_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [contact_type_name] varchar(100)
);
CREATE TABLE [dbo].[sfdc_lead_order] (
    [sfdc_lead_order_id] int IDENTITY(1,1) NOT NULL,
    [salesforce_lead_id] varchar(18) NOT NULL,
    [order_header_id] int NOT NULL
,
    PRIMARY KEY ([sfdc_lead_order_id])
);
CREATE TABLE [dbo].[sfdc_license_category_mapping] (
    [sfdc_license_category_mapping_id] int IDENTITY(1,1) NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [sfdc_field_name] varchar(50) NOT NULL,
    [sfdc_value] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([sfdc_license_category_mapping_id])
);
CREATE TABLE [dbo].[sfdc_license_vault] (
    [sfdc_license_Vault_id] int IDENTITY(1,1) NOT NULL,
    [salesforce_license_vault_id] varchar(25),
    [license_id] int,
    [license_external_reference_id] int,
    [insert_date] datetime,
    [insert_by] varchar(50),
    [modified_by] varchar(50),
    [modified_date] datetime
,
    PRIMARY KEY ([sfdc_license_Vault_id])
);
CREATE TABLE [dbo].[sfdc_opportunity_load] (
    [sfdc_opportunity_load_id] int IDENTITY(1,1) NOT NULL,
    [opportunity_id] varchar(18),
    [salesforce_trial_id] varchar(18),
    [sfdc_template_id] tinyint NOT NULL,
    [sfdc_update_type_id] tinyint NOT NULL,
    [sfdc_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [load_attempts] tinyint NOT NULL,
    [process_date] datetime NOT NULL,
    [license_update_id] varchar(18)
,
    PRIMARY KEY ([sfdc_opportunity_load_id])
);
CREATE TABLE [dbo].[sfdc_opportunity_load_archive] (
    [sfdc_opportunity_load_archive_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_opportunity_load_id] int NOT NULL,
    [opportunity_id] varchar(18),
    [salesforce_trial_id] varchar(18),
    [sfdc_template_id] tinyint NOT NULL,
    [sfdc_update_type_id] tinyint NOT NULL,
    [sfdc_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [load_attempts] tinyint NOT NULL,
    [process_date] datetime NOT NULL,
    [archive_date] datetime NOT NULL,
    [license_update_id] varchar(18)
,
    PRIMARY KEY ([sfdc_opportunity_load_archive_id])
);
CREATE TABLE [dbo].[sfdc_opportunity_load_failure] (
    [sfdc_opportunity_load_failure_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_opportunity_load_id] int NOT NULL,
    [opportunity_id] varchar(18),
    [salesforce_trial_id] varchar(18),
    [sfdc_template_id] tinyint NOT NULL,
    [sfdc_update_type_id] tinyint NOT NULL,
    [sfdc_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [load_attempts] tinyint NOT NULL,
    [process_date] datetime NOT NULL,
    [failure_description] varchar(255) NOT NULL,
    [failure_date] datetime NOT NULL,
    [license_update_id] varchar(18)
,
    PRIMARY KEY ([sfdc_opportunity_load_failure_id])
);
CREATE TABLE [dbo].[sfdc_opportunity_load_json] (
    [sfdc_opportunity_load_json_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_opportunity_load_id] int NOT NULL,
    [sfdc_load_json] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([sfdc_opportunity_load_json_id])
);
CREATE TABLE [dbo].[sfdc_opportunity_renewal] (
    [sfdc_opportunity_renewal_id] int IDENTITY(1,1) NOT NULL,
    [order_opportunity_id] int NOT NULL,
    [is_autorenewal_enabled] bit NOT NULL,
    [salesforce_renewal_opportunity_id] varchar(18),
    [sfdc_opportunity_renewal_status_id] smallint,
    [process_date] date,
    [modified_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([sfdc_opportunity_renewal_id])
);
CREATE TABLE [dbo].[sfdc_opportunity_renewal_status] (
    [sfdc_opportunity_renewal_status_id] smallint IDENTITY(1,1) NOT NULL,
    [sfdc_opportunity_renewal_status_description] varchar(50) NOT NULL
,
    PRIMARY KEY ([sfdc_opportunity_renewal_status_id])
);
CREATE TABLE [dbo].[sfdc_opportunity_update] (
    [sfdc_opportunity_update_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int,
    [trial_id] int,
    [sfdc_template_id] tinyint NOT NULL,
    [sfdc_update_type_id] tinyint NOT NULL,
    [sfdc_update_status_id] tinyint NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [sfdc_response_code] varchar(50),
    [sfdc_response_message] nvarchar(MAX),
    [insert_date] datetime NOT NULL,
    [process_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [form_submit_id] int,
    [license_id] int
,
    PRIMARY KEY ([sfdc_opportunity_update_id])
);
CREATE TABLE [dbo].[sfdc_opportunity_update_archive] (
    [sfdc_opportunity_update_archive_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_opportunity_update_id] int NOT NULL,
    [order_header_id] int,
    [trial_id] int,
    [sfdc_template_id] tinyint,
    [sfdc_update_type_id] tinyint NOT NULL,
    [sfdc_update_status_id] tinyint NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [sfdc_response_code] varchar(50),
    [sfdc_response_message] nvarchar(MAX),
    [insert_date] datetime NOT NULL,
    [process_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [archive_date] datetime NOT NULL,
    [form_submit_id] int,
    [license_id] int
,
    PRIMARY KEY ([sfdc_opportunity_update_archive_id])
);
CREATE TABLE [dbo].[sfdc_opportunity_update_failure] (
    [sfdc_opportunity_update_failure_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_opportunity_update_id] int NOT NULL,
    [order_header_id] int,
    [trial_id] int,
    [sfdc_template_id] tinyint,
    [sfdc_update_type_id] tinyint NOT NULL,
    [sfdc_update_status_id] tinyint NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [sfdc_response_code] varchar(50),
    [sfdc_response_message] nvarchar(MAX),
    [insert_date] datetime NOT NULL,
    [process_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [failure_date] datetime NOT NULL,
    [form_submit_id] int,
    [license_id] int
,
    PRIMARY KEY ([sfdc_opportunity_update_failure_id])
);
CREATE TABLE [dbo].[sfdc_opportunity_update_json] (
    [sfdc_opportunity_update_json_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_opportunity_update_id] int NOT NULL,
    [sfdc_update_json] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([sfdc_opportunity_update_json_id])
);
CREATE TABLE [dbo].[sfdc_pricebook_product] (
    [sfdc_pricebook_product_id] int IDENTITY(1,1) NOT NULL,
    [PricebookEntryId] varchar(18) NOT NULL,
    [PricebookEntryName] nvarchar(255) NOT NULL,
    [Product2Id] varchar(18) NOT NULL,
    [Product2Name] nvarchar(255) NOT NULL,
    [CurrencyCode] varchar(3) NOT NULL,
    [PriceBookUnitPrice] money NOT NULL
,
    PRIMARY KEY ([sfdc_pricebook_product_id])
);
CREATE TABLE [dbo].[sfdc_pricebook_product_mapping] (
    [sfdc_pricebook_product_mapping_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_pricebook_product_id] int NOT NULL,
    [product_id] int NOT NULL
);
CREATE TABLE [dbo].[sfdc_product_category_mapping] (
    [sfdc_product_category_mapping_id] int IDENTITY(1,1) NOT NULL,
    [Product2Id] varchar(18) NOT NULL,
    [ProductCode] nvarchar(255) NOT NULL,
    [license_category_name] nvarchar(10) NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [usage_pricing_model_id] tinyint,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [seats] int,
    [retention_model_id] tinyint
,
    PRIMARY KEY ([sfdc_product_category_mapping_id])
);
CREATE TABLE [dbo].[sfdc_product_tier] (
    [sfdc_product_tier_id] int IDENTITY(1,1) NOT NULL,
    [product_type_id] int NOT NULL,
    [license_category_id] int NOT NULL,
    [product_pricing_level_id] tinyint NOT NULL,
    [low_range] int NOT NULL,
    [high_range] int NOT NULL,
    [salesforce_product_id] varchar(20) NOT NULL
,
    PRIMARY KEY ([sfdc_product_tier_id])
);
CREATE TABLE [dbo].[sfdc_queue_type] (
    [sfdc_queue_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [sfdc_queue_type_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([sfdc_queue_type_id])
);
CREATE TABLE [dbo].[sfdc_sale_type] (
    [sfdc_sale_type_id] int IDENTITY(1,1) NOT NULL,
    [sale_type_name] nvarchar(20)
,
    PRIMARY KEY ([sfdc_sale_type_id])
);
CREATE TABLE [dbo].[sfdc_segment] (
    [sfdc_segment_id] tinyint IDENTITY(1,1) NOT NULL,
    [sfdc_segment_name] varchar(255),
    [sfdc_sale_type_id] int NOT NULL
,
    PRIMARY KEY ([sfdc_segment_id])
);
CREATE TABLE [dbo].[sfdc_sub_segment] (
    [sfdc_sub_segment_id] tinyint IDENTITY(1,1) NOT NULL,
    [sfdc_sub_segment_name] varchar(255),
    [sfdc_segment_id] tinyint NOT NULL,
    [company_type_id] tinyint
,
    PRIMARY KEY ([sfdc_sub_segment_id])
);
CREATE TABLE [dbo].[sfdc_subscriber] (
    [sfdc_subscriber_id] int IDENTITY(1,1) NOT NULL,
    [subscriber_id] int NOT NULL,
    [subscriber_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
);
CREATE TABLE [dbo].[sfdc_subscriber_customer] (
    [sfdc_subscriber_customer_id] int IDENTITY(1,1) NOT NULL,
    [subscriber_id] int NOT NULL,
    [customer_id] int NOT NULL
);
CREATE TABLE [dbo].[sfdc_subscriber_held] (
    [sfdc_subscriber_held_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_subscriber_id] int NOT NULL,
    [sfdc_subscriber_key] varchar(255) NOT NULL,
    [customer_email] varchar(100) NOT NULL,
    [member_id] int NOT NULL,
    [api_response] varchar(2000),
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [processed] int,
    [processed_date] datetime NOT NULL
);
CREATE TABLE [dbo].[sfdc_subscriber_history] (
    [sfdc_subscriber_id] int NOT NULL,
    [subscriber_id] int NOT NULL,
    [subscriber_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL
);
CREATE TABLE [dbo].[sfdc_subscriber_status] (
    [sfdc_subscriber_status_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_subscriber_status] nvarchar(50) NOT NULL
);
CREATE TABLE [dbo].[sfdc_template] (
    [sfdc_template_id] tinyint IDENTITY(1,1) NOT NULL,
    [sfdc_template_name] varchar(50) NOT NULL,
    [sfdc_template_description] varchar(500) NOT NULL,
    [sfdc_queue_type_id] tinyint,
    [next_sfdc_template_id] tinyint,
    [next_template_delay_seconds] int
);
CREATE TABLE [dbo].[sfdc_template_surpal] (
    [sfdc_template_id] tinyint IDENTITY(1,1) NOT NULL,
    [sfdc_template_name] varchar(50) NOT NULL,
    [sfdc_template_description] varchar(500) NOT NULL
);
CREATE TABLE [dbo].[sfdc_template_update_type] (
    [sfdc_template_update_type_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_template_id] int NOT NULL,
    [sfdc_update_type_id] tinyint NOT NULL,
    [next_update_type_id] tinyint
,
    PRIMARY KEY ([sfdc_template_update_type_id])
);
CREATE TABLE [dbo].[sfdc_template_update_type_surpal] (
    [sfdc_template_update_type_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_template_id] int NOT NULL,
    [sfdc_update_type_id] tinyint NOT NULL,
    [next_update_type_id] tinyint
);
CREATE TABLE [dbo].[sfdc_territories] (
    [sfdc_territories_id] int IDENTITY(1,1) NOT NULL,
    [country] varchar(100) NOT NULL,
    [state_code] varchar(100),
    [geo] varchar(10) NOT NULL,
    [inside_sales_territory] varchar(100),
    [renewals_territory] varchar(100),
    [insert_date] datetime,
    [iso] varchar(3),
    [sfdc_sale_type_id] int
,
    PRIMARY KEY ([sfdc_territories_id])
);
CREATE TABLE [dbo].[sfdc_test_group] (
    [sfdc_test_group_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_test_group] varchar(50)
,
    PRIMARY KEY ([sfdc_test_group_id])
);
CREATE TABLE [dbo].[sfdc_trial] (
    [sfdc_trial_id] int IDENTITY(1,1) NOT NULL,
    [trial_id] int NOT NULL,
    [salesforce_lead_id] varchar(18),
    [salesforce_opportunity_id] varchar(18),
    [salesforce_trial_id] varchar(18),
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([sfdc_trial_id])
);
CREATE TABLE [dbo].[sfdc_trial_audit] (
    [sfdc_trial_audit_id] int IDENTITY(1,1) NOT NULL,
    [sfdc_trial_id] int NOT NULL,
    [trial_id] int NOT NULL,
    [salesforce_lead_id] varchar(18),
    [salesforce_opportunity_id] varchar(18),
    [salesforce_trial_id] varchar(18),
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([sfdc_trial_audit_id])
);
CREATE TABLE [dbo].[sfdc_update_status] (
    [sfdc_update_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [sfdc_update_status_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([sfdc_update_status_id])
);
CREATE TABLE [dbo].[sfdc_update_type] (
    [sfdc_update_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [sfdc_update_type_name] varchar(20) NOT NULL,
    [next_update_type_id] tinyint
,
    PRIMARY KEY ([sfdc_update_type_id])
);
CREATE TABLE [dbo].[sfdc_update_type_surpal] (
    [sfdc_update_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [sfdc_update_type_name] varchar(20) NOT NULL,
    [next_update_type_id] tinyint
);
CREATE TABLE [dbo].[sfdc_user] (
    [sfdc_user_id] int IDENTITY(1,1) NOT NULL,
    [User2Id] varchar(18),
    [Username] varchar(100),
    [LastName] nvarchar(80),
    [FirstName] nvarchar(80),
    [Name] nvarchar(121)
);
CREATE TABLE [dbo].[SFDCAccount] (
    [Id] char(18) NOT NULL,
    [IsDeleted] bit,
    [Name] nvarchar(255),
    [Type] nvarchar(40),
    [BillingStreet] nvarchar(255),
    [BillingCity] nvarchar(40),
    [BillingState] nvarchar(80),
    [BillingPostalCode] nvarchar(20),
    [BillingCountry] nvarchar(80),
    [ShippingStreet] nvarchar(255),
    [ShippingCity] nvarchar(40),
    [ShippingState] nvarchar(80),
    [ShippingPostalCode] nvarchar(20),
    [ShippingCountry] nvarchar(80),
    [Phone] nvarchar(40),
    [OwnerId] char(18),
    [CreatedDate] datetime,
    [CreatedById] char(18),
    [IsPartner] bit,
    [Invoicing_Contact__c] char(18),
    [Oracle_Account_ID__c] nvarchar(20)
,
    PRIMARY KEY ([Id])
);
CREATE TABLE [dbo].[SFDCContact] (
    [Id] char(18) NOT NULL,
    [IsDeleted] bit,
    [MasterRecordId] char(18),
    [AccountId] char(18),
    [LastName] nvarchar(80),
    [FirstName] nvarchar(40),
    [Name] nvarchar(121),
    [MailingStreet] nvarchar(255),
    [MailingCity] nvarchar(40),
    [MailingState] nvarchar(80),
    [MailingPostalCode] nvarchar(20),
    [MailingCountry] nvarchar(80),
    [Phone] nvarchar(40),
    [Email] nvarchar(80),
    [Title] nvarchar(128),
    [customer_id] int
,
    PRIMARY KEY ([Id])
);
CREATE TABLE [dbo].[SFDCOpportunitySummary] (
    [OpportunityID] char(18) NOT NULL,
    [AccountId] char(18),
    [Name] nvarchar(120),
    [Amount] decimal(18,2),
    [Type] nvarchar(40),
    [CurrencyIsoCode] nvarchar(3),
    [CreatedDate] datetime,
    [CloseDate] datetime,
    [OwnerId] char(18),
    [CreatedById] char(18),
    [Bill_To_Account__c] char(18),
    [Bill_To_Type__c] nvarchar(255),
    [Billing_Frequency__c] nvarchar(255),
    [Opportunity_Number__c] nvarchar(7),
    [Purchase_Order__c] nvarchar(51),
    [Distributor__c] char(18),
    [Reseller_Partner_Account__c] char(18),
    [Reseller_ID__c] nvarchar(1300),
    [Distributor_ID__c] nvarchar(1300),
    [Download_Contact__c] char(18),
    [Contract_End_Date__c] datetime,
    [Contract_Start_Date__c] datetime,
    [Contract_Term_in_Months__c] int,
    [Key_Code__c] nvarchar(4000),
    [Endpoint_Keycode__c] nvarchar(4000),
    [Mobile_Keycode__c] nvarchar(4000),
    [User_Protection_Key_Code__c] nvarchar(4000),
    [Desktop_Licenses__c] nvarchar(4000),
    [CSI_Invoice_Nbr__c] nvarchar(50),
    [ASSET_Name] nvarchar(255),
    [Product_Family__c] nvarchar(1300),
    [ASSET_AccountId] char(18),
    [Channel_Manager__c] char(18)
);
CREATE TABLE [dbo].[ship_cd_without_order] (
    [ship_cd_without_order_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int NOT NULL,
    [product_id] int NOT NULL,
    [insert_date] datetime
,
    PRIMARY KEY ([ship_cd_without_order_id])
);
CREATE TABLE [dbo].[ship_to] (
    [invoice_code] varchar(16) NOT NULL,
    [s_first_name] varchar(32) NOT NULL,
    [s_last_name] varchar(32) NOT NULL,
    [s_company1] varchar(48),
    [s_company2] varchar(48),
    [s_address1] varchar(48) NOT NULL,
    [s_address2] varchar(48),
    [s_city] varchar(48) NOT NULL,
    [s_state_id] char(2) NOT NULL,
    [s_other_state] varchar(50),
    [s_country_id] smallint NOT NULL,
    [s_postal_code] varchar(10),
    [last_modified] datetime NOT NULL
,
    PRIMARY KEY ([invoice_code])
);
CREATE TABLE [dbo].[shipping_manifest] (
    [invoice_code] varchar(16) NOT NULL,
    [first_name] varchar(50),
    [last_name] varchar(50),
    [company] varchar(50),
    [address1] varchar(50),
    [address2] varchar(50),
    [city] varchar(50),
    [state] varchar(50),
    [postal_code] varchar(50),
    [country_id] smallint,
    [telephone] varchar(50),
    [ship_via] varchar(50),
    [purchased_date] smalldatetime NOT NULL,
    [keycode] varchar(50),
    [product_id] int NOT NULL,
    [shipping_status] tinyint NOT NULL,
    [last_modified] datetime NOT NULL,
    [lineitem] int IDENTITY(1,1) NOT NULL
);
CREATE TABLE [dbo].[shipping_manifest_in_process] (
    [shipping_manifest_in_process_id] int IDENTITY(1,1) NOT NULL,
    [invoice_in_process_id] int NOT NULL,
    [invoice_code] varchar(16),
    [first_name] varchar(50),
    [last_name] varchar(50),
    [company] varchar(50),
    [address1] varchar(50),
    [address2] varchar(50),
    [city] varchar(50),
    [state] varchar(50),
    [postal_code] varchar(50),
    [country_id] smallint,
    [telephone] varchar(50),
    [ship_via] varchar(50),
    [purchased_date] smalldatetime,
    [keycode] varchar(50),
    [product_id] int,
    [shipping_status] tinyint,
    [last_modified] datetime,
    [lineitem] int
,
    PRIMARY KEY ([shipping_manifest_in_process_id])
);
CREATE TABLE [dbo].[sky_account] (
    [sky_account_id] int IDENTITY(1,1) NOT NULL,
    [uber_id] int NOT NULL,
    [customer_email] varchar(50) NOT NULL,
    [sky_account_json_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([sky_account_id])
);
CREATE TABLE [dbo].[sky_account_customer] (
    [sky_account_customer_id] int IDENTITY(1,1) NOT NULL,
    [sky_account_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [company_id] int,
    [sky_account_customer_json_id] int,
    [sky_account_customer_key] uniqueidentifier NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [sap_account_number] int
,
    PRIMARY KEY ([sky_account_customer_id])
);
CREATE TABLE [dbo].[sky_account_customer_company_verify] (
    [sky_account_customer_company_verify_id] int IDENTITY(1,1) NOT NULL,
    [sky_account_customer_id] int NOT NULL,
    [sky_account_customer_company_verify_key] uniqueidentifier NOT NULL,
    [expiration_date] datetime NOT NULL,
    [verified_date] datetime,
    [disabled_date] datetime,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([sky_account_customer_company_verify_id])
);
CREATE TABLE [dbo].[sky_account_customer_json] (
    [sky_account_customer_json_id] int IDENTITY(1,1) NOT NULL,
    [sky_account_customer_id] int,
    [sky_account_customer_json] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([sky_account_customer_json_id])
);
CREATE TABLE [dbo].[sky_account_json] (
    [sky_account_json_id] int IDENTITY(1,1) NOT NULL,
    [sky_account_id] int,
    [sky_account_json] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([sky_account_json_id])
);
CREATE TABLE [dbo].[sky_active_key_sync_saep] (
    [sky_active_key_sync_saep_id] int IDENTITY(1,1) NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [marname] varchar(1000),
    [parentkeycode] varchar(40),
    [licensecategoryid] int,
    [licensetype] varchar(60),
    [last_DBActiveLast30Days] int,
    [last_log_date] date,
    [last_effective_date] date,
    [insert_date] datetime NOT NULL,
    [last_report_date] datetime NOT NULL,
    [resolution_status] varchar(50),
    [license_service_id] int
,
    PRIMARY KEY ([sky_active_key_sync_saep_id])
);
CREATE TABLE [dbo].[sky_api_license] (
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_id])
);
CREATE TABLE [dbo].[sky_device_enforcement_1day] (
    [license_id] bigint NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [device_count] int,
    [seat_count] int,
    [effective_date] datetime NOT NULL
,
    PRIMARY KEY ([keycode], [license_id])
);
CREATE TABLE [dbo].[sky_device_enforcement_7day] (
    [license_id] bigint NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [device_count] int,
    [seat_count] int,
    [effective_date] datetime NOT NULL
,
    PRIMARY KEY ([keycode], [license_id])
);
CREATE TABLE [dbo].[sky_license] (
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([license_id])
);
CREATE TABLE [dbo].[sky_license_activity] (
    [license_id] int NOT NULL,
    [keycode] nvarchar(40) NOT NULL,
    [license_key] uniqueidentifier,
    [distribution] nvarchar(4),
    [device_count] int,
    [device_pc] int,
    [device_mac] int,
    [device_mobile] int,
    [health_status] int,
    [osfirewall_enabled] int,
    [protection_enabled] int,
    [rootkitshield_enabled] int,
    [webthreatshield_enabled] int,
    [usbshield_enabled] int,
    [offlineshield_enabled] int,
    [firewall_enabled] int,
    [infrared_enabled] int,
    [idshield_enabled] int,
    [phishingshield_enabled] int,
    [sku] nvarchar(60),
    [total_garbage_removal_size] int,
    [last_scan_date] datetime,
    [last_scan_duration] bigint,
    [scan_count] bigint,
    [threats_removed] int,
    [threat_blocked] int,
    [latest_threat] nvarchar(4000),
    [system_analyzer_score] int,
    [hardware_score] nvarchar(1),
    [software_score] nvarchar(1),
    [threats_score] nvarchar(1),
    [gamer_license] bit,
    [bby_license] bit,
    [weak_hosts] bit,
    [avg_scan_duration] bigint,
    [license_activity_devices] int,
    [effective_date] datetime NOT NULL
,
    PRIMARY KEY ([keycode], [license_id])
);
CREATE TABLE [dbo].[sky_license_activity_staging] (
    [license_id] int NOT NULL,
    [keycode] nvarchar(40) NOT NULL,
    [license_key] uniqueidentifier,
    [distribution] nvarchar(4),
    [device_count] int,
    [device_pc] int,
    [device_mac] int,
    [device_mobile] int,
    [health_status] int,
    [osfirewall_enabled] int,
    [protection_enabled] int,
    [rootkitshield_enabled] int,
    [webthreatshield_enabled] int,
    [usbshield_enabled] int,
    [offlineshield_enabled] int,
    [firewall_enabled] int,
    [infrared_enabled] int,
    [idshield_enabled] int,
    [phishingshield_enabled] int,
    [sku] nvarchar(60),
    [total_garbage_removal_size] int,
    [last_scan_date] datetime,
    [last_scan_duration] bigint,
    [scan_count] bigint,
    [threats_removed] int,
    [threat_blocked] int,
    [latest_threat] nvarchar(4000),
    [system_analyzer_score] int,
    [hardware_score] nvarchar(1),
    [software_score] nvarchar(1),
    [threats_score] nvarchar(1),
    [gamer_license] bit,
    [bby_license] bit,
    [weak_hosts] bit,
    [avg_scan_duration] bigint,
    [license_activity_devices] int,
    [effective_date] datetime NOT NULL
,
    PRIMARY KEY ([keycode], [license_id])
);
CREATE TABLE [dbo].[sky_license_feature] (
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [uberkey] varchar(40) NOT NULL,
    [marid] varchar(40) NOT NULL,
    [ubid] varchar(40) NOT NULL,
    [lastpass_enabled] bit,
    [backupsync_enabled] bit,
    [effective_date] datetime NOT NULL
,
    PRIMARY KEY ([keycode], [license_id], [marid], [uberkey], [ubid])
);
CREATE TABLE [dbo].[sky_license_host_feature] (
    [license_id] int NOT NULL,
    [keycode] nvarchar(40) NOT NULL,
    [hostname] nvarchar(40) NOT NULL,
    [host_type] nvarchar(15),
    [protection_enabled] bit,
    [rootkitshield_enabled] bit,
    [webthreatshield_enabled] bit,
    [usbshield_enabled] bit,
    [offlineshield_enabled] bit,
    [firewall_enabled] bit,
    [infrared_enabled] bit,
    [idshield_enabled] bit,
    [phishingshield_enabled] bit,
    [effective_date] datetime NOT NULL
,
    PRIMARY KEY ([hostname], [keycode], [license_id])
);
CREATE TABLE [dbo].[sky_supplemental_template] (
    [sky_supplemental_template_id] int IDENTITY(1,1) NOT NULL,
    [sky_template_id] tinyint NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [product_extension_json] nvarchar(MAX)
,
    PRIMARY KEY ([sky_supplemental_template_id])
);
CREATE TABLE [dbo].[sky_template] (
    [sky_template_id] tinyint IDENTITY(1,1) NOT NULL,
    [sky_template_name] varchar(50) NOT NULL,
    [sky_template_description] varchar(500) NOT NULL
);
CREATE TABLE [dbo].[sky_template_update_type] (
    [sky_template_update_type_id] int IDENTITY(1,1) NOT NULL,
    [sky_template_id] int NOT NULL,
    [sky_update_type_id] tinyint NOT NULL,
    [next_update_type_id] tinyint
,
    PRIMARY KEY ([sky_template_update_type_id])
);
CREATE TABLE [dbo].[sky_update] (
    [sky_update_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [sky_template_id] tinyint NOT NULL,
    [sky_update_type_id] tinyint NOT NULL,
    [sky_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [process_date] datetime NOT NULL
,
    PRIMARY KEY ([sky_update_id])
);
CREATE TABLE [dbo].[sky_update_archive] (
    [sky_update_archive_id] int IDENTITY(1,1) NOT NULL,
    [sky_update_id] int NOT NULL,
    [license_id] int NOT NULL,
    [sky_template_id] tinyint NOT NULL,
    [sky_update_type_id] tinyint NOT NULL,
    [sky_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [process_date] datetime NOT NULL,
    [archive_date] datetime NOT NULL
,
    PRIMARY KEY ([sky_update_archive_id])
);
CREATE TABLE [dbo].[sky_update_backup] (
    [sky_update_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [sky_template_id] tinyint NOT NULL,
    [sky_update_type_id] tinyint NOT NULL,
    [sky_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [process_date] datetime NOT NULL
);
CREATE TABLE [dbo].[sky_update_backup_1127] (
    [sky_update_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [sky_template_id] tinyint NOT NULL,
    [sky_update_type_id] tinyint NOT NULL,
    [sky_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [process_date] datetime NOT NULL
);
CREATE TABLE [dbo].[sky_update_failure] (
    [sky_update_failure_id] int IDENTITY(1,1) NOT NULL,
    [sky_update_id] int NOT NULL,
    [license_id] int NOT NULL,
    [sky_template_id] tinyint NOT NULL,
    [sky_update_type_id] tinyint NOT NULL,
    [sky_update_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [update_attempts] tinyint NOT NULL,
    [process_date] datetime NOT NULL,
    [failure_description] varchar(5000) NOT NULL,
    [failure_date] datetime NOT NULL
,
    PRIMARY KEY ([sky_update_failure_id])
);
CREATE TABLE [dbo].[sky_update_json] (
    [sky_update_json_id] int IDENTITY(1,1) NOT NULL,
    [sky_update_id] int NOT NULL,
    [sky_update_json] nvarchar(MAX) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([sky_update_json_id])
);
CREATE TABLE [dbo].[sky_update_license] (
    [license_id] int NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[sky_update_status] (
    [sky_update_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [sky_update_status_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([sky_update_status_id])
);
CREATE TABLE [dbo].[sky_update_type] (
    [sky_update_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [sky_update_type_name] varchar(30)
,
    PRIMARY KEY ([sky_update_type_id])
);
CREATE TABLE [dbo].[skyrise_license_failure] (
    [skyrise_license_failure_id] int IDENTITY(1,1) NOT NULL,
    [skyrise_request] nvarchar(MAX) NOT NULL,
    [skyrise_failure_message] nvarchar(MAX) NOT NULL,
    [failure_date] datetime NOT NULL
,
    PRIMARY KEY ([skyrise_license_failure_id])
);
CREATE TABLE [dbo].[skytell_notification_json] (
    [skytell_notification_id] int IDENTITY(1,1) NOT NULL,
    [skytell_notification_type_id] tinyint,
    [skytell_notification_correlation_guid] uniqueidentifier NOT NULL,
    [skytell_notification_request_guid] uniqueidentifier NOT NULL,
    [skytell_notification_action] varchar(50) NOT NULL,
    [skytell_notification_json] varchar(MAX) NOT NULL,
    [skytell_notification_status_id] smallint,
    [modified_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([skytell_notification_id])
);
CREATE TABLE [dbo].[skytell_notification_status] (
    [skytell_notification_status_id] smallint NOT NULL,
    [skytell_notification_message] varchar(500) NOT NULL
,
    PRIMARY KEY ([skytell_notification_status_id])
);
CREATE TABLE [dbo].[skytell_notification_type] (
    [skytell_notification_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [skytell_notification_type] varchar(50) NOT NULL
,
    PRIMARY KEY ([skytell_notification_type_id])
);
CREATE TABLE [dbo].[sonian_account] (
    [sonian_account_id] int IDENTITY(1,1) NOT NULL,
    [account_user_name] varchar(100),
    [account_password] varchar(64),
    [first_name] nvarchar(225),
    [last_name] nvarchar(225),
    [email_address] varchar(100),
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [city] nvarchar(130),
    [state] nvarchar(2),
    [zip] nvarchar(32),
    [company_name] nvarchar(225),
    [sub_domain] varchar(64),
    [number_users] int,
    [organization_type_id] int,
    [uses_exchange] tinyint,
    [account_status] tinyint,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
);
CREATE TABLE [dbo].[SOS_method] (
    [SOS_method_id] int IDENTITY(1,1) NOT NULL,
    [SOS_method_name] varchar(20) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([SOS_method_id])
);
CREATE TABLE [dbo].[SOS_processing] (
    [SOS_processing_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [SOS_method_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([SOS_processing_id])
);
CREATE TABLE [dbo].[SOS_product_mapping] (
    [SOS_product_mapping_id] int IDENTITY(1,1) NOT NULL,
    [product_line_id] int NOT NULL,
    [storage_gb] int NOT NULL,
    [payment_plan_id] int NOT NULL,
    [customer_type_code] varchar(20) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([SOS_product_mapping_id])
);
CREATE TABLE [dbo].[specials] (
    [id] int NOT NULL,
    [code] varchar(100) NOT NULL,
    [nochangeqty] smallint,
    [gowhere] smallint,
    [description] varchar(255),
    [last_modified] datetime
);
CREATE TABLE [dbo].[specials_products] (
    [id] int NOT NULL,
    [specials_id] int,
    [product_id] int,
    [quantity] smallint,
    [visible_price] decimal(8,2),
    [stored_price] decimal(8,2),
    [last_modified] datetime
);
CREATE TABLE [dbo].[spy_alert] (
    [alert_id] int IDENTITY(1,1) NOT NULL,
    [title] nvarchar(300) NOT NULL,
    [description] nvarchar(1000) NOT NULL,
    [link] nvarchar(500) NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [sub_status] int,
    [expire_begin] int,
    [expire_end] int,
    [reg_status] int,
    [language_id] int NOT NULL,
    [alert_product_version_id] int NOT NULL,
    [version_compare_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] nvarchar(50) NOT NULL,
    [alert_status] smallint NOT NULL
,
    PRIMARY KEY ([alert_id])
);
CREATE TABLE [dbo].[spy_alert_active] (
    [alert_active_id] int IDENTITY(1,1) NOT NULL,
    [title] nvarchar(300) NOT NULL,
    [short_description] nvarchar(1000) NOT NULL,
    [link] nvarchar(500) NOT NULL,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [language_code] varchar(2) NOT NULL,
    [expire_start] int,
    [expire_end] int,
    [sub_status] int,
    [reg_status] int,
    [alert_status] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(75) NOT NULL
,
    PRIMARY KEY ([alert_active_id])
);
CREATE TABLE [dbo].[spy_alert_active_detail] (
    [alert_active_detail_id] int IDENTITY(1,1) NOT NULL,
    [alert_active_id] int NOT NULL,
    [product_id] int NOT NULL,
    [product_version_str] char(10),
    [reference_id] int,
    [country_code_3] varchar(3),
    [InsertDate] datetime NOT NULL,
    [InsertBy] nvarchar(75) NOT NULL
,
    PRIMARY KEY ([alert_active_detail_id])
);
CREATE TABLE [dbo].[spy_alert_note] (
    [alert_note_id] int IDENTITY(1,1) NOT NULL,
    [alert_id] int NOT NULL,
    [note] nvarchar(4000) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] nvarchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] nvarchar(50) NOT NULL
,
    PRIMARY KEY ([alert_note_id])
);
CREATE TABLE [dbo].[spy_definitions] (
    [id] numeric(10,0),
    [serial_number] varchar(8) NOT NULL,
    [name] varchar(70),
    [version] varchar(32),
    [short_desc] varchar(255),
    [category_id] numeric(10,0),
    [threat_assessment_id] numeric(10,0),
    [characteristics] text,
    [method_of_infection] text,
    [recommended_action] text,
    [author] varchar(75),
    [priority] numeric(3,0) NOT NULL,
    [short_chk] numeric(3,0) NOT NULL,
    [med_chk] numeric(3,0) NOT NULL,
    [long_chk] numeric(3,0) NOT NULL,
    [short_stat] numeric(3,0) NOT NULL,
    [characteristics_stat] numeric(3,0) NOT NULL,
    [method_of_infection_stat] numeric(3,0) NOT NULL,
    [dependencies_stat] numeric(3,0) NOT NULL,
    [additional_stat] numeric(3,0) NOT NULL,
    [privacy_issues_stat] numeric(3,0) NOT NULL,
    [security_issues_stat] numeric(3,0) NOT NULL,
    [performance_issues_stat] numeric(3,0) NOT NULL,
    [recommendations_stat] numeric(3,0) NOT NULL,
    [consequences_stat] numeric(3,0) NOT NULL,
    [auth_web] varchar(200),
    [dependencies] text,
    [privacy_policy] varchar(200),
    [eula] varchar(200),
    [related_links] text,
    [privacy_issues] text,
    [security_issues] text,
    [performance_issues] text,
    [consequences] text,
    [recommendations] text,
    [research] text,
    [last_updated] datetime
);
CREATE TABLE [dbo].[storage_account] (
    [storage_account_id] int IDENTITY(1,1) NOT NULL,
    [storage_account_username] nvarchar(50) NOT NULL,
    [storage_account_password] nvarchar(40),
    [security_question] varchar(100),
    [secret_answer] varchar(100),
    [license_storage_id] int,
    [storage_account_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([storage_account_id])
);
CREATE TABLE [dbo].[storage_account_audit] (
    [storage_account_audit_id] int IDENTITY(1,1) NOT NULL,
    [storage_account_id] int NOT NULL,
    [storage_account_username] nvarchar(50) NOT NULL,
    [storage_account_password] nvarchar(40),
    [security_question] varchar(100),
    [secret_answer] varchar(100),
    [license_storage_id] int,
    [storage_account_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [audit_date] datetime NOT NULL,
    [audit_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([storage_account_audit_id])
);
CREATE TABLE [dbo].[storage_account_status] (
    [storage_account_status_id] int IDENTITY(1,1) NOT NULL,
    [storage_account_status_name] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([storage_account_status_id])
);
CREATE TABLE [dbo].[string_replacement] (
    [string_replacement_id] int IDENTITY(1,1) NOT NULL,
    [string_replacement_category_id] int NOT NULL,
    [string_replacement_type_id] int NOT NULL,
    [search_string] nvarchar(100) NOT NULL,
    [replace_string] nvarchar(100) NOT NULL,
    [priority] int NOT NULL
,
    PRIMARY KEY ([string_replacement_id])
);
CREATE TABLE [dbo].[string_replacement_category] (
    [string_replacement_category_id] int IDENTITY(1,1) NOT NULL,
    [name] varchar(100) NOT NULL
,
    PRIMARY KEY ([string_replacement_category_id])
);
CREATE TABLE [dbo].[string_replacement_type] (
    [string_replacement_type_id] int IDENTITY(1,1) NOT NULL,
    [name] varchar(50) NOT NULL
,
    PRIMARY KEY ([string_replacement_type_id])
);
CREATE TABLE [dbo].[subscription] (
    [subscription_id] int IDENTITY(1,1) NOT NULL,
    [subscription_type_id] int,
    [license_id] int NOT NULL,
    [serial_number] varchar(19),
    [start_date] smalldatetime,
    [cancel_reason] varchar(40),
    [cancel_date] smalldatetime,
    [insert_date] smalldatetime,
    [expiration_date] smalldatetime,
    [last_modified_date] datetime,
    [last_modified_by] varchar(200),
    [product_sku] varchar(8),
    [plan_sku] varchar(8),
    [contract_id] varchar(10),
    [contract_detail_update] int,
    [opt_status] int,
    [renewal_sku] varchar(8),
    [subscription_partner_product_id] int
,
    PRIMARY KEY ([subscription_id])
);
CREATE TABLE [dbo].[subscription_cancel_error_log] (
    [subscription_cancel_error_log_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] int,
    [cancel_date] datetime,
    [cancel_reason] varchar(40),
    [response_code] int,
    [message] varchar(200),
    [insert_date] datetime
,
    PRIMARY KEY ([subscription_cancel_error_log_id])
);
CREATE TABLE [dbo].[subscription_cancel_reason] (
    [cancel_reason] varchar(40) NOT NULL,
    [cancel_reason_description] varchar(100) NOT NULL
,
    PRIMARY KEY ([cancel_reason])
);
CREATE TABLE [dbo].[subscription_change_reason] (
    [subscription_change_reason_id] int IDENTITY(1,1) NOT NULL,
    [subscription_change_reason_name] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [process_type] varchar(20)
,
    PRIMARY KEY ([subscription_change_reason_id])
);
CREATE TABLE [dbo].[subscription_contract_detail_update] (
    [contract_detail_update] int NOT NULL,
    [contract_detail_update_description] varchar(100) NOT NULL
,
    PRIMARY KEY ([contract_detail_update])
);
CREATE TABLE [dbo].[subscription_conversion] (
    [subscription_conversion_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] int NOT NULL,
    [conversion_serial_number] varchar(19) NOT NULL,
    [conversion_contract_id] varchar(10) NOT NULL,
    [subscription_conversion_status_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([subscription_conversion_id])
);
CREATE TABLE [dbo].[subscription_conversion_status] (
    [subscription_conversion_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [subscription_conversion_status] varchar(20) NOT NULL
,
    PRIMARY KEY ([subscription_conversion_status_id])
);
CREATE TABLE [dbo].[subscription_customer] (
    [subscription_customer_id] int IDENTITY(1,1) NOT NULL,
    [bby_customer_id] bigint NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([subscription_customer_id])
);
CREATE TABLE [dbo].[subscription_customer_subscription] (
    [subscription_customer_subscription_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] int NOT NULL,
    [subscription_customer_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([subscription_customer_subscription_id])
);
CREATE TABLE [dbo].[subscription_distribution_method] (
    [subscription_distribution_method_id] int IDENTITY(1,1) NOT NULL,
    [license_distribution_method_id] int,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
,
    PRIMARY KEY ([subscription_distribution_method_id])
);
CREATE TABLE [dbo].[subscription_external] (
    [subscription_external_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] int,
    [subscription_customer_id] int NOT NULL,
    [subscription_type_id] int NOT NULL,
    [serial_number] varchar(19) NOT NULL,
    [contract_id] varchar(10),
    [start_date] datetime NOT NULL,
    [expiration_date] datetime,
    [last_usage_date] datetime,
    [last_modified_date] datetime,
    [last_modified_by] varchar(200)
,
    PRIMARY KEY ([subscription_external_id])
);
CREATE TABLE [dbo].[subscription_external_cancel] (
    [serial_number] varchar(19) NOT NULL,
    [expiration_date] datetime NOT NULL
);
CREATE TABLE [dbo].[subscription_external_history] (
    [subscription_external_history_id] int IDENTITY(1,1) NOT NULL,
    [subscription_external_id] int NOT NULL,
    [subscription_id] int,
    [subscription_customer_id] int NOT NULL,
    [subscription_type_id] int NOT NULL,
    [serial_number] varchar(19) NOT NULL,
    [contract_id] varchar(10),
    [start_date] datetime NOT NULL,
    [expiration_date] datetime NOT NULL,
    [last_usage_date] datetime,
    [last_modified_date] datetime,
    [last_modified_by] varchar(200),
    [history_by] datetime
,
    PRIMARY KEY ([subscription_external_history_id])
);
CREATE TABLE [dbo].[subscription_external_message_update] (
    [subscription_external_message_update_id] int IDENTITY(1,1) NOT NULL,
    [serial_number] varchar(19) NOT NULL,
    [subscription_id] int,
    [offer_bundle] varchar(20),
    [creation_date] datetime,
    [offer_bundle_request] varchar(20),
    [vendor_id] varchar(5) NOT NULL,
    [transaction_id] varchar(50) NOT NULL,
    [offer_bundle_details] varchar(20),
    [contract_id] varchar(10),
    [message_type] varchar(2) NOT NULL,
    [association_code] varchar(10),
    [total_regular_price] money,
    [total_renewal_price] money,
    [process_response_code] varchar(20),
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_flag] bit
);
CREATE TABLE [dbo].[subscription_history] (
    [subscription_history_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] int NOT NULL,
    [subscription_type_id] int,
    [license_id] int NOT NULL,
    [serial_number] varchar(19),
    [start_date] smalldatetime,
    [cancel_reason] varchar(40),
    [cancel_date] smalldatetime,
    [subscription_change_reason_id] int,
    [insert_date] smalldatetime,
    [expiration_date] smalldatetime,
    [history_date] datetime NOT NULL,
    [history_by] varchar(20) NOT NULL,
    [product_sku] varchar(8),
    [plan_sku] varchar(8),
    [contract_id] varchar(10),
    [contract_detail_update] int,
    [opt_status] int
,
    PRIMARY KEY ([subscription_history_id])
);
CREATE TABLE [dbo].[subscription_license_category_module_configuration] (
    [subscription_license_category_module_configuration_id] int IDENTITY(1,1) NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [license_module_id] tinyint,
    [product_id] int,
    [license_seats] int,
    [is_default] bit,
    [insert_date] datetime NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([subscription_license_category_module_configuration_id])
);
CREATE TABLE [dbo].[subscription_message] (
    [subscription_message_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] int NOT NULL,
    [contract_id] varchar(50),
    [promo_sku] varchar(10),
    [promo_price] money,
    [promo_start_date] datetime,
    [promo_end_date] datetime,
    [promo_type] smallint,
    [last_modified_date] datetime,
    [subscription_external_id] int
,
    PRIMARY KEY ([subscription_message_id])
);
CREATE TABLE [dbo].[subscription_message_archive] (
    [subscription_message_archive_id] int IDENTITY(1,1) NOT NULL,
    [subscription_message_id] int NOT NULL,
    [subscription_id] int NOT NULL,
    [contract_id] varchar(50),
    [promo_sku] varchar(10),
    [promo_price] money,
    [promo_start_date] datetime,
    [promo_end_date] datetime,
    [promo_type] smallint,
    [last_modified_date] datetime,
    [archive_date] datetime
,
    PRIMARY KEY ([subscription_message_archive_id])
);
CREATE TABLE [dbo].[subscription_message_log] (
    [subscription_message_log_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] int NOT NULL,
    [contract_id] varchar(50),
    [promo_sku] varchar(10),
    [promo_price] money,
    [promo_start_date] datetime,
    [promo_end_date] datetime,
    [promo_type] smallint,
    [log_date] datetime
,
    PRIMARY KEY ([subscription_message_log_id])
);
CREATE TABLE [dbo].[subscription_message_promo_campaign] (
    [subscription_message_promo_campaign_id] int IDENTITY(1,1) NOT NULL,
    [promo_type] smallint NOT NULL,
    [message_campaign_id] int NOT NULL,
    [status] varchar(10) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([subscription_message_promo_campaign_id])
);
CREATE TABLE [dbo].[subscription_message_promo_discount] (
    [subscription_message_promo_discount_id] int IDENTITY(1,1) NOT NULL,
    [promo_sku] varchar(10) NOT NULL,
    [cart_discount_id] int NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([subscription_message_promo_discount_id])
);
CREATE TABLE [dbo].[subscription_opt_status] (
    [opt_status] int NOT NULL,
    [opt_status_description] varchar(50) NOT NULL,
    [license_module_id] tinyint,
    [license_module_type_id] tinyint
,
    PRIMARY KEY ([opt_status])
);
CREATE TABLE [dbo].[subscription_partner_product] (
    [subscription_partner_product_id] int IDENTITY(1,1) NOT NULL,
    [subscription_partner_product_desc] varchar(50) NOT NULL,
    [subscription_partner_product_key] varchar(30) NOT NULL,
    [product_id] int NOT NULL,
    [license_distribution_method_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([subscription_partner_product_id])
);
CREATE TABLE [dbo].[subscription_payment_status] (
    [subscription_payment_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [subscription_payment_status_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([subscription_payment_status_id])
);
CREATE TABLE [dbo].[subscription_plan_sku] (
    [plan_sku] varchar(8) NOT NULL,
    [plan_sku_description] varchar(50) NOT NULL,
    [activate_message] varchar(3) NOT NULL,
    [status] varchar(10) NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [seats] int NOT NULL,
    [subscription_type_id] int NOT NULL,
    [subscription_days] int NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200),
    [plan_sku_opt_out] tinyint,
    [product_sku] varchar(8),
    [subscription_sku_status_id] tinyint
,
    PRIMARY KEY ([plan_sku])
);
CREATE TABLE [dbo].[subscription_product_sku] (
    [product_sku] varchar(8) NOT NULL,
    [product_sku_description] varchar(50) NOT NULL,
    [product_sku_type] varchar(10) NOT NULL,
    [activate_message] varchar(3) NOT NULL,
    [status] varchar(10) NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [seats] int NOT NULL,
    [subscription_type_id] int NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [product_sku_opt_out] tinyint,
    [subscription_sku_status_id] tinyint
,
    PRIMARY KEY ([product_sku])
);
CREATE TABLE [dbo].[subscription_promo_sku] (
    [promo_sku] varchar(10) NOT NULL,
    [subscription_promo_sku_description] varchar(100) NOT NULL,
    [promo_amount] int
,
    PRIMARY KEY ([promo_sku])
);
CREATE TABLE [dbo].[subscription_promo_type] (
    [promo_type] smallint NOT NULL,
    [subscription_promo_type_description] varchar(100) NOT NULL
,
    PRIMARY KEY ([promo_type])
);
CREATE TABLE [dbo].[subscription_sku_status] (
    [subscription_sku_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [subscription_sku_status_desc] varchar(20) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([subscription_sku_status_id])
);
CREATE TABLE [dbo].[subscription_summary] (
    [subscription_summary_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] int NOT NULL,
    [subscription_type_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [license_seats] int NOT NULL,
    [subscription_days] int NOT NULL,
    [start_date] datetime NOT NULL,
    [expiration_date] datetime NOT NULL,
    [summary_date] datetime NOT NULL,
    [subscription_change_reason_id] int NOT NULL,
    [product_sku] varchar(8),
    [plan_sku] varchar(8),
    [contract_id] varchar(10),
    [summary_status_id] tinyint NOT NULL
,
    PRIMARY KEY ([subscription_summary_id])
);
CREATE TABLE [dbo].[subscription_type] (
    [subscription_type_id] int IDENTITY(1,1) NOT NULL,
    [description] varchar(200) NOT NULL,
    [subscription_type] nvarchar(2) NOT NULL,
    [insert_date] smalldatetime
,
    PRIMARY KEY ([subscription_type_id])
);
CREATE TABLE [dbo].[subscription_update] (
    [subscription_update_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] int,
    [subscription_change_reason_id] int NOT NULL,
    [subscription_update_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [process_date] datetime NOT NULL,
    [update_attempts] tinyint NOT NULL
,
    PRIMARY KEY ([subscription_update_id])
);
CREATE TABLE [dbo].[subscription_update_archive] (
    [subscription_update_archive_id] int IDENTITY(1,1) NOT NULL,
    [subscription_update_id] int NOT NULL,
    [subscription_id] int,
    [subscription_change_reason_id] int NOT NULL,
    [subscription_update_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [archive_date] datetime NOT NULL
,
    PRIMARY KEY ([subscription_update_archive_id])
);
CREATE TABLE [dbo].[subscription_update_error_log] (
    [subscription_update_error_log_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] int,
    [start_date] datetime,
    [subscription_type_id] int,
    [expiration_date] datetime,
    [subscription_change_reason_id] int,
    [product_sku] varchar(8),
    [plan_sku] varchar(8),
    [contract_id] varchar(10),
    [response_code] int,
    [message] varchar(200),
    [insert_date] datetime
,
    PRIMARY KEY ([subscription_update_error_log_id])
);
CREATE TABLE [dbo].[subscription_update_failure] (
    [subscription_update_failure_id] int IDENTITY(1,1) NOT NULL,
    [subscription_update_id] int NOT NULL,
    [subscription_id] int,
    [subscription_change_reason_id] int NOT NULL,
    [subscription_update_status_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [failure_date] datetime NOT NULL,
    [failure_reason] varchar(500)
,
    PRIMARY KEY ([subscription_update_failure_id])
);
CREATE TABLE [dbo].[subscription_update_json] (
    [subscription_update_json_id] int IDENTITY(1,1) NOT NULL,
    [subscription_update_id] int NOT NULL,
    [subscription_update_json] nvarchar(MAX) NOT NULL
);
CREATE TABLE [dbo].[subscription_update_old] (
    [subscription_update_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] int NOT NULL,
    [subscription_history_id] int,
    [subscription_update_type_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [processed] tinyint NOT NULL
);
CREATE TABLE [dbo].[subscription_update_status] (
    [subscription_update_status_id] int IDENTITY(1,1) NOT NULL,
    [subscription_update_status_name] varchar(50) NOT NULL
,
    PRIMARY KEY ([subscription_update_status_id])
);
CREATE TABLE [dbo].[subscription_update_type] (
    [subscription_update_type_id] tinyint NOT NULL,
    [subscription_update_type_description] varchar(20) NOT NULL
,
    PRIMARY KEY ([subscription_update_type_id])
);
CREATE TABLE [dbo].[sugarsync] (
    [id] int IDENTITY(1,1) NOT NULL,
    [account_user_name] varchar(256) NOT NULL,
    [used_storage] bigint NOT NULL,
    [avail_storage] int NOT NULL,
    [account_creation_date] smalldatetime NOT NULL,
    [active] char(1) NOT NULL,
    [filedate] smalldatetime,
    [end_date] smalldatetime
,
    PRIMARY KEY ([id])
);
CREATE TABLE [dbo].[sugarsync_reconciliation] (
    [sugarsync_reconciliation_id] int IDENTITY(1,1) NOT NULL,
    [account_id] int,
    [account_user_name] varchar(100) NOT NULL,
    [plan_code] varchar(50),
    [status] varchar(20),
    [create_date] datetime,
    [used_storage] bigint,
    [total_storage] bigint,
    [last_update_date] datetime NOT NULL
,
    PRIMARY KEY ([sugarsync_reconciliation_id])
);
CREATE TABLE [dbo].[sugarsync_update] (
    [sugarsync_update_id] int IDENTITY(1,1) NOT NULL,
    [account_id] int NOT NULL,
    [sugarsync_update_type_id] tinyint NOT NULL,
    [sugarsync_update_status_id] tinyint NOT NULL,
    [plan_code] varchar(10),
    [status_code] varchar(25),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime
,
    PRIMARY KEY ([sugarsync_update_id])
);
CREATE TABLE [dbo].[sugarsync_update_archive] (
    [sugarsync_update_archive_id] int IDENTITY(1,1) NOT NULL,
    [sugarsync_update_id] int NOT NULL,
    [account_id] int NOT NULL,
    [sugarsync_update_type_id] tinyint NOT NULL,
    [sugarsync_update_status_id] tinyint NOT NULL,
    [plan_code] varchar(10),
    [status_code] varchar(25),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime
,
    PRIMARY KEY ([sugarsync_update_archive_id])
);
CREATE TABLE [dbo].[sugarsync_update_failure] (
    [sugarsync_update_failure_id] int IDENTITY(1,1) NOT NULL,
    [sugarsync_update_id] int NOT NULL,
    [account_id] int NOT NULL,
    [sugarsync_update_type_id] tinyint NOT NULL,
    [sugarsync_update_status_id] tinyint NOT NULL,
    [plan_code] varchar(10),
    [status_code] varchar(25),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime
,
    PRIMARY KEY ([sugarsync_update_failure_id])
);
CREATE TABLE [dbo].[sugarsync_update_status] (
    [sugarsync_update_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [sugarsync_update_status_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([sugarsync_update_status_id])
);
CREATE TABLE [dbo].[sugarsync_update_type] (
    [sugarsync_update_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [sugarsync_update_type_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([sugarsync_update_type_id])
);
CREATE TABLE [dbo].[sweepstakes] (
    [sweepstakes_id] int IDENTITY(1,1) NOT NULL,
    [sweepstakes_name] varchar(50) NOT NULL,
    [insert_date] datetime NOT NULL,
    [status_id] tinyint NOT NULL
,
    PRIMARY KEY ([sweepstakes_id])
);
CREATE TABLE [dbo].[sweepstakes_registration] (
    [sweepstakes_registration_id] int IDENTITY(1,1) NOT NULL,
    [sweepstakes_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [dob] datetime NOT NULL,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[temp_adjust_users_activate] (
    [temp_adjust_users_id] int IDENTITY(1,1) NOT NULL,
    [update_keycode] varchar(40) NOT NULL,
    [license_id] int NOT NULL,
    [prevx_capability_expiration_date] datetime,
    [days] int,
    [tewm_process_status_id] tinyint NOT NULL
);
CREATE TABLE [dbo].[temp_adjust_users_temw_days] (
    [temp_adjust_users_id] int IDENTITY(1,1) NOT NULL,
    [update_keycode] varchar(40) NOT NULL,
    [license_id] int NOT NULL,
    [prevx_capability_expiration_date] datetime,
    [days] int,
    [tewm_process_status_id] tinyint NOT NULL
);
CREATE TABLE [dbo].[temp_all_essen] (
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [license_category_name] varchar(10) NOT NULL,
    [iDay] varchar(8),
    [seats] int,
    [Product_id] int
);
CREATE TABLE [dbo].[temp_ar_remove] (
    [rec_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_attribute_license_value] int NOT NULL,
    [license_attribute_tag] varchar(11) NOT NULL,
    [license_attribute_id] int NOT NULL
);
CREATE TABLE [dbo].[temp_asp_update] (
    [license_capability_id] int NOT NULL,
    [license_id] int NOT NULL,
    [capability_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_activation_days] int,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [days_used] int,
    [days_remaining] int,
    [days_added] int,
    [days_new] int,
    [days_contiguous] int,
    [last_days_contiguous] int,
    [new_activation_date] datetime
);
CREATE TABLE [dbo].[temp_bb_license_message] (
    [license_id] int,
    [message_type_id] int NOT NULL,
    [process_date] datetime,
    [end_date] datetime,
    [archive_date] datetime
);
CREATE TABLE [dbo].[temp_bby_ts_subscription] (
    [Message_Type] varchar(50),
    [BBY_Customer_ID] varchar(50),
    [S2_Serial_Number] varchar(50),
    [S2_Contract_ID] varchar(50),
    [External_Contract_Type] varchar(50),
    [Hardware_Description] varchar(50),
    [External_Serial_Number] varchar(50),
    [External_Contract_ID] varchar(50),
    [External_Expiration_Date] varchar(50),
    [External_Promo_Sku] varchar(50),
    [External_Promo_Type_Code] varchar(50),
    [External_Last_Usage_Date] varchar(50)
);
CREATE TABLE [dbo].[temp_bestbuy_serial_number] (
    [Keycode] varchar(40),
    [Change to (printedoncard)] varchar(50),
    [Is (in Ecomm)] varchar(50)
);
CREATE TABLE [dbo].[temp_beta_feedback] (
    [temp_beta_feedback_id] int IDENTITY(1,1) NOT NULL,
    [customer_email] varchar(100),
    [customer_id] int,
    [account_id] int,
    [license_id] int,
    [account_license_id] int,
    [update_license_id] int,
    [processed] int
);
CREATE TABLE [dbo].[temp_beta_live_date] (
    [beta_live_date] datetime NOT NULL,
    [beta_number] int NOT NULL
);
CREATE TABLE [dbo].[temp_beta_update] (
    [license_capability_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [capability_id] int NOT NULL,
    [capability_type_id] int NOT NULL,
    [capability_activation_days] int,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [days_used] int,
    [days_remaining] int,
    [days_added] int,
    [days_new] int,
    [days_contiguous] int,
    [last_days_contiguous] int,
    [new_activation_date] datetime,
    [update_complete] int
);
CREATE TABLE [dbo].[temp_cat_update] (
    [rec_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [capability_activation_days] int,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [product_id] int,
    [processed] int NOT NULL
);
CREATE TABLE [dbo].[temp_company_search_facebook_detail] (
    [search_id] int NOT NULL,
    [company_id] int,
    [company_match] tinyint,
    [match_score] float,
    [company_score] float,
    [phone_score] float,
    [website_score] float,
    [street_score] float,
    [city_score] float,
    [state_score] float,
    [zip_score] float,
    [country_score] float
);
CREATE TABLE [dbo].[temp_console_activity_all_log] (
    [report_type] varchar(50),
    [report_date] date,
    [license_id] int,
    [keycode] varchar(50),
    [license_keycode_type] varchar(50),
    [categories] varchar(50),
    [license_type] varchar(50),
    [expiration_date] date,
    [total_sites_with_full] int,
    [total_sites_with_trial] int,
    [unit_type] varchar(50),
    [total_units_with_full] int,
    [total_units_with_trial] int,
    [site_license_id] int,
    [site_keycode] varchar(50),
    [site_categories] varchar(50),
    [site_expiration_date] date,
    [site_license_type] varchar(50),
    [site_unit_type] varchar(50),
    [site_total_units] int
);
CREATE TABLE [dbo].[temp_contract_subscription_setup] (
    [temp_id] int IDENTITY(1,1) NOT NULL,
    [use_case] varchar(250) NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [license_seats] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [license_status_id] tinyint NOT NULL,
    [license_type_id] tinyint NOT NULL,
    [capability_activation_date] datetime,
    [item_effective_date] datetime NOT NULL,
    [capability_expiration_date] datetime,
    [contract_end_date] datetime,
    [StageName] nvarchar(40),
    [item_expiration_date] datetime,
    [salesforce_account_id] varchar(20),
    [company_name] nvarchar(255) NOT NULL,
    [order_header_id] int NOT NULL,
    [vendor_order_date] datetime NOT NULL,
    [total_amount] money NOT NULL,
    [salesforce_opportunity_id] varchar(20) NOT NULL,
    [order_item_id] int NOT NULL,
    [opportunity_line_item_id] varchar(18) NOT NULL,
    [product_id] int NOT NULL,
    [quantity] int NOT NULL,
    [product_description] varchar(100) NOT NULL,
    [product_type_id] int NOT NULL,
    [product_type_description] varchar(50),
    [non_sfdc_orders] int,
    [last_non_sfdc_order_header_id] int,
    [notes] varchar(250)
,
    PRIMARY KEY ([temp_id])
);
CREATE TABLE [dbo].[temp_contract_subscription_setup_new] (
    [temp_id] int IDENTITY(1,1) NOT NULL,
    [use_case] varchar(250) NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [license_seats] int NOT NULL,
    [license_category_id] tinyint NOT NULL,
    [license_status_id] tinyint NOT NULL,
    [license_type_id] tinyint NOT NULL,
    [capability_activation_date] datetime,
    [item_effective_date] datetime NOT NULL,
    [capability_expiration_date] datetime,
    [contract_end_date] datetime,
    [StageName] nvarchar(40),
    [item_expiration_date] datetime,
    [salesforce_account_id] varchar(20),
    [company_name] nvarchar(255) NOT NULL,
    [order_header_id] int NOT NULL,
    [vendor_order_date] datetime NOT NULL,
    [total_amount] money NOT NULL,
    [salesforce_opportunity_id] varchar(20) NOT NULL,
    [order_item_id] int NOT NULL,
    [opportunity_line_item_id] varchar(18) NOT NULL,
    [product_id] int NOT NULL,
    [quantity] int NOT NULL,
    [product_description] varchar(100) NOT NULL,
    [product_type_id] int NOT NULL,
    [product_type_description] varchar(50),
    [non_sfdc_orders] int,
    [last_non_sfdc_order_header_id] int,
    [notes] varchar(250)
,
    PRIMARY KEY ([temp_id])
);
CREATE TABLE [dbo].[temp_cybs_customer_profile] (
    [license_id] int,
    [cybs_customer_profile_id] int NOT NULL
);
CREATE TABLE [dbo].[temp_cybs_process_token] (
    [vendor_order_code] varchar(16) NOT NULL,
    [cybs_process_token_id] int
);
CREATE TABLE [dbo].[temp_dr_bundle_back] (
    [dr_bundle_id] int NOT NULL,
    [offer_id] int NOT NULL,
    [offer_name] varchar(200) NOT NULL,
    [parent_product_id] int NOT NULL,
    [current_storage_gb] int,
    [storage_gb] int,
    [language] varchar(10),
    [start_months] int,
    [end_months] int
);
CREATE TABLE [dbo].[temp_dr_bundle_fix] (
    [dr_bundle_id] int,
    [offer_id] bigint,
    [offer_name] varchar(200),
    [parent_product_id] int,
    [current_storage_gb] varchar(20),
    [storage_gb] varchar(20),
    [language] varchar(20),
    [start_months] varchar(20),
    [end_months] varchar(20),
    [insert_date] datetime
);
CREATE TABLE [dbo].[temp_dr_bundle_product_back] (
    [dr_bundle_product_id] int NOT NULL,
    [dr_bundle_id] int NOT NULL,
    [dr_product_id] int NOT NULL
);
CREATE TABLE [dbo].[temp_dr_fix] (
    [dr_fix_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int,
    [vendor_order_code] varchar(200),
    [Items] int,
    [processed] bit,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([dr_fix_id])
);
CREATE TABLE [dbo].[temp_dr_fix_keycode] (
    [dr_fix_keycode_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_code] varchar(200),
    [order_header_id] int,
    [product_id] int,
    [order_item_id] int,
    [processed] bit,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([dr_fix_keycode_id])
);
CREATE TABLE [dbo].[temp_dr_product_price_fix] (
    [dr_product_price_id] int,
    [dr_product_id] int,
    [dr_product_name] varchar(150),
    [dr_base_product_id] int,
    [product_id] int,
    [start_months] varchar(20),
    [end_months] varchar(20),
    [language_code] varchar(20),
    [location_code] varchar(20),
    [retail_price] money,
    [currency] varchar(20),
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200)
);
CREATE TABLE [dbo].[temp_dr_subscription_reconciliation] (
    [subscription_id] bigint NOT NULL,
    [dr_user_id] bigint NOT NULL,
    [vendor_order_code] varchar(100) NOT NULL,
    [dr_product_id] bigint NOT NULL,
    [product_id] int NOT NULL,
    [locale] varchar(10) NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [xml_type] varchar(50),
    [processed] int NOT NULL
);
CREATE TABLE [dbo].[temp_dr_subscription_reprocess] (
    [temp_id] int IDENTITY(1,1) NOT NULL,
    [dr_subscription_id] int NOT NULL,
    [exec_string] varchar(200) NOT NULL,
    [processed] int NOT NULL
);
CREATE TABLE [dbo].[temp_duplicate_customer] (
    [temp_id] int IDENTITY(1,1) NOT NULL,
    [first_name] nvarchar(225),
    [last_name] nvarchar(225),
    [customer_email] varchar(100),
    [last_insert_date] datetime,
    [max_customer_id] int,
    [min_customer_id] int
,
    PRIMARY KEY ([temp_id])
);
CREATE TABLE [dbo].[temp_duplicate_dr_license_subscription] (
    [temp_id] int IDENTITY(1,1) NOT NULL,
    [subscription_id] int NOT NULL,
    [dr_license_subscription_id] int NOT NULL,
    [dr_subscription_update_id] int
);
CREATE TABLE [dbo].[temp_essen_to_update] (
    [rec_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [license_category_name] varchar(10) NOT NULL,
    [iDay] varchar(8),
    [seats] int,
    [Product_id] int,
    [processed] int NOT NULL
);
CREATE TABLE [dbo].[temp_extend_beta_users] (
    [temp_extend_beta_users_id] int IDENTITY(1,1) NOT NULL,
    [update_keycode] varchar(40),
    [days] int,
    [processed] int
);
CREATE TABLE [dbo].[temp_extend_child_users] (
    [temp_extend_child_users_id] int IDENTITY(1,1) NOT NULL,
    [update_keycode] varchar(40) NOT NULL,
    [days] int,
    [processed] int NOT NULL
);
CREATE TABLE [dbo].[temp_JA_price_new] (
    [dr_base_product_id] int,
    [retail_price] money
);
CREATE TABLE [dbo].[temp_license_category_license_fix] (
    [license_id] int,
    [license_category_id] tinyint
);
CREATE TABLE [dbo].[temp_license_message_fix] (
    [license_message_id] int,
    [message_type_id] int,
    [message_status_id] tinyint,
    [correct_message_status_id] tinyint
);
CREATE TABLE [dbo].[temp_license_orders] (
    [license_id] int,
    [order_header_id] int
);
CREATE TABLE [dbo].[temp_license_seat_fix] (
    [license_id] int NOT NULL,
    [license_seats] int,
    [product_seats] int
);
CREATE TABLE [dbo].[temp_mobile_update] (
    [rec_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [capability_activation_days] int,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime,
    [product_id] int,
    [processed] int NOT NULL
);
CREATE TABLE [dbo].[temp_new_customer] (
    [new_id] int IDENTITY(1,1) NOT NULL,
    [firstName] nvarchar(40),
    [lastName] nvarchar(80),
    [email] varchar(100),
    [company_id] int,
    [company_name] nvarchar(255),
    [salesforce_account_id] varchar(20),
    [salesforce_contact_id] varchar(20),
    [customer_id] int,
    [processed] int NOT NULL
);
CREATE TABLE [dbo].[temp_ninja_billing] (
    [billing_date] datetime,
    [customer_code] nvarchar(255),
    [company_name] nvarchar(255),
    [reseller_customer_code] nvarchar(255),
    [reseller_company_name] nvarchar(255),
    [webroot_entity] nvarchar(255),
    [location_code] nvarchar(255),
    [state] nvarchar(255),
    [keycode] nvarchar(255),
    [license_type_description] nvarchar(255),
    [license_seats] float,
    [usage_seats] float,
    [total_seats] float,
    [order_date] datetime,
    [license_category_name] nvarchar(255),
    [keycode_age] float,
    [retail_price] money,
    [total_extended_amount] money,
    [cap_amount] money,
    [total_cap_amount] money
);
CREATE TABLE [dbo].[temp_oracle_contract] (
    [ID] int,
    [CONTRACT_NUMBER] nvarchar(255),
    [STS_CODE] nvarchar(255),
    [CREATION_DATE] datetime,
    [LAST_UPDATE_DATE] datetime,
    [START_DATE] datetime,
    [END_DATE] datetime
);
CREATE TABLE [dbo].[temp_oracle_contract_line_20190213] (
    [oracle_contract_line_id] int NOT NULL,
    [oracle_contract_id] int NOT NULL,
    [oracle_line_id] varchar(40) NOT NULL,
    [oracle_contract_status_id] tinyint NOT NULL,
    [order_item_id] int,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL,
    [last_modified_date] datetime NOT NULL
);
CREATE TABLE [dbo].[temp_oracle_contract_line2] (
    [oracle_contract_line_id] int NOT NULL,
    [oracle_contract_id] int NOT NULL,
    [oracle_line_id] varchar(40) NOT NULL,
    [oracle_contract_status_id] tinyint NOT NULL,
    [order_item_id] int,
    [start_date] datetime NOT NULL,
    [end_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL,
    [last_modified_date] datetime NOT NULL
);
CREATE TABLE [dbo].[temp_oracle_convert_usage] (
    [OKC_CONVERT_USAGE_ID] nvarchar(384),
    [OPPORTUNITY_TYPE] nvarchar(100),
    [OPPORTUNITY_NUMBER] nvarchar(100),
    [OPPORTUNITY_ID] nvarchar(100),
    [CURRENCY_CODE] nvarchar(100),
    [CONTRACTING_ENTITY] nvarchar(100),
    [CONTRACT_START_DATE] nvarchar(100),
    [CONTRACT_END_DATE] nvarchar(100),
    [BOOK_DATE] nvarchar(100),
    [PO_NUMBER] nvarchar(100),
    [END_USER_ACCOUNT_ID] nvarchar(100),
    [END_USE_ACCOUNT_NAME] nvarchar(250),
    [RESELLER_ACCOUNT_ID] nvarchar(100),
    [RESELLER_ACCOUNT_NAME] nvarchar(100),
    [DISTRIBUTOR_ACCOUNT_ID] nvarchar(100),
    [DISTRIBUTOR_ACCOUNT_NAME] nvarchar(100),
    [BILL_TO_ACCOUNT_ID] nvarchar(100),
    [BILL_TO_ACCOUNT_NAME] nvarchar(250),
    [BILL_TO_TYPE] nvarchar(100),
    [BILLING_FREQUENCY] nvarchar(100),
    [BILLING_PERIOD] nvarchar(100),
    [BILLING_DAY_OF_MONTH] nvarchar(100),
    [PRODUCT_CODE] nvarchar(100),
    [OPPORTUNITY_LINE_ID] nvarchar(100),
    [QUANTITY] nvarchar(100),
    [MONTHLY_EXCESS_USAGE_TYPE] nvarchar(100),
    [MONTHLY_EXCESS_USAGE_FEE] nvarchar(384),
    [ORACLE_ACCOUNT_ID] nvarchar(384),
    [ORACLE_BILL_TO_ID] nvarchar(384),
    [ORACLE_END_USER_SHIP_TO_ID] nvarchar(384),
    [ORACLE_CONTRACT_NUMBER] nvarchar(100),
    [ORACLE_CONTRACT_ID] nvarchar(384),
    [ORACLE_CONTRACT_LINE_ID] nvarchar(384),
    [SALESREP_NUMBER] nvarchar(100),
    [STATUS] nvarchar(1),
    [ERROR_MESSAGE] nvarchar(1000)
);
CREATE TABLE [dbo].[temp_oracle_customer_data] (
    [PARTY_ID] int,
    [PARTY_NUMBER] int,
    [CUSTOMER_NAME] nvarchar(255),
    [CUSTOMER_NUMBER] varchar(20),
    [CUST_ACCOUNT_ID] int,
    [SFDC_ACCOUNT_ID] varchar(18),
    [ORG_ID] int,
    [OPERATING_UNIT] varchar(50),
    [LOCATION] nvarchar(255),
    [LOCATION_ID] int,
    [ADDRESS] nvarchar(255),
    [SITE_USE_CODE] varchar(20),
    [PRIMARY_SITE_FLAG] char(1),
    [CONTACT_NAME] nvarchar(100),
    [CONTACT_POINT_TYPE] varchar(20),
    [EMAIL_ADDRESS] varchar(100)
);
CREATE TABLE [dbo].[temp_oracle_fix] (
    [vendor_order_code] varchar(50)
);
CREATE TABLE [dbo].[temp_oracle_fix2] (
    [vendor_order_code] varchar(50)
);
CREATE TABLE [dbo].[temp_oracle_line_usage] (
    [CONTRACT_NUMBER] varchar(20),
    [CONTRACT_ID] int,
    [CONTRACT_START_DATE] datetime,
    [CONTRACT_END_DATE] datetime,
    [CONTRACT_STATUS_CODE] varchar(50),
    [LINE_ID] varchar(40),
    [LINE_START_DATE] datetime,
    [LINE_END_DATE] datetime,
    [LINE_STATUS_CODE] varchar(50),
    [USAGE_LINE_ID] varchar(40),
    [USAGE_SUBLINE_ID] varchar(40),
    [USAGE_TYPE] varchar(20),
    [USAGE_START_DATE] datetime,
    [USAGE_END_DATE] datetime,
    [USAGE_STATUS_CODE] varchar(20),
    [COUNTER_ID] int
);
CREATE TABLE [dbo].[temp_order_customer_profile_token] (
    [license_id] int,
    [order_customer_profile_token_id] int NOT NULL
);
CREATE TABLE [dbo].[temp_order_license] (
    [order_header_id] int NOT NULL,
    [license_id] int
);
CREATE TABLE [dbo].[temp_partner_customer_code] (
    [id] varchar(20),
    [guid] varchar(50)
);
CREATE TABLE [dbo].[temp_payment_header_backfill] (
    [temp_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_code] varchar(50),
    [order_header_id] int,
    [cart_order_id] int,
    [cart_in_process_id] int,
    [license_id] int,
    [credit_card_subscription_id] varchar(24),
    [settlement_transaction_id] varchar(24),
    [processed] tinyint NOT NULL
,
    PRIMARY KEY ([temp_id])
);
CREATE TABLE [dbo].[temp_prevx_update_bulk] (
    [license_id] int NOT NULL
);
CREATE TABLE [dbo].[temp_pricebook_mapping] (
    [PricebookEntryName] nvarchar(255),
    [license_category_name] varchar(10),
    [seats] int,
    [years] float
);
CREATE TABLE [dbo].[temp_product_review] (
    [product_family_id] int,
    [product_type_id] int NOT NULL,
    [product_id] int NOT NULL,
    [product_description] varchar(100) NOT NULL,
    [retail_price] money,
    [license_category_id] tinyint,
    [license_category_name] varchar(10),
    [current_license_category_id] tinyint,
    [years] tinyint,
    [seats] int,
    [current_seats] int,
    [storage_gb] int NOT NULL,
    [current_storage_gb] int NOT NULL
);
CREATE TABLE [dbo].[temp_products] (
    [products_id] int IDENTITY(1,1) NOT NULL,
    [product_family_id] int,
    [product_id] int,
    [product_description] varchar(200),
    [retail_price] money,
    [product_type_id] int,
    [current_license_category_id] int,
    [license_category_id] int,
    [current_storage_gb] int,
    [storage_gb] int,
    [current_seats] int,
    [seats] int,
    [years] float
);
CREATE TABLE [dbo].[temp_redownload_attempts] (
    [attempt_id] int IDENTITY(1,1) NOT NULL,
    [customer_email] varchar(100),
    [last_name] nvarchar(225),
    [product_line_prefix] char(2),
    [keycode] varchar(40),
    [total_keycodes] int,
    [insert_date] datetime
,
    PRIMARY KEY ([attempt_id])
);
CREATE TABLE [dbo].[temp_refund_order_headers] (
    [order_header_id] int,
    [rows_inserted] int,
    [processed_date] datetime
);
CREATE TABLE [dbo].[temp_seat_update] (
    [license_seat_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_seats] int NOT NULL,
    [seats_used] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [start_date] datetime,
    [end_date] datetime,
    [keycode] varchar(40) NOT NULL,
    [capability_activation_date] datetime,
    [capability_expiration_date] datetime
);
CREATE TABLE [dbo].[temp_sfdc_assignment] (
    [User2Id] nvarchar(255),
    [Username] nvarchar(255),
    [FirstName] nvarchar(255),
    [LastName] nvarchar(255),
    [assignment_type_name] nvarchar(255),
    [product_type_description] nvarchar(255),
    [description] nvarchar(255),
    [state] nvarchar(255),
    [distribution_geography] nvarchar(255),
    [RepName] nvarchar(255),
    [UserID] nvarchar(255)
);
CREATE TABLE [dbo].[temp_sfdc_channel_assignment] (
    [GEO] nvarchar(255),
    [RegionType] nvarchar(255),
    [Country] nvarchar(255),
    [State] nvarchar(255),
    [TerritoryMgr] nvarchar(255),
    [LDR] nvarchar(255),
    [ChannelMgr] nvarchar(255)
);
CREATE TABLE [dbo].[temp_sfdc_license] (
    [license_id] int NOT NULL,
    [Name] varchar(40) NOT NULL,
    [Account_Id__c] varchar(20),
    [Activation_Date__c] datetime,
    [Capability_Type__c] varchar(20) NOT NULL,
    [Ecomm_License_Id__c] int NOT NULL,
    [Ecomm_License_Key__c] uniqueidentifier NOT NULL,
    [Expiration_Date__c] datetime,
    [License_Attribute_Value__c] varchar(50),
    [License_Attribute__c] varchar(100),
    [License_Category_Id__c] char(18) NOT NULL,
    [License_Distribution_Method_Id__c] char(18) NOT NULL,
    [Parent_License_Id__c] int,
    [Product_Line__c] varchar(40) NOT NULL,
    [Seats__c] int NOT NULL,
    [Status__c] varchar(20) NOT NULL,
    [Type__c] varchar(20) NOT NULL
);
CREATE TABLE [dbo].[temp_sfdc_trial] (
    [license_id] int NOT NULL,
    [Name] varchar(40) NOT NULL,
    [Account_Id__c] varchar(20),
    [Lead__c] varchar(20),
    [Opportunity__c] varchar(20),
    [Activation_Date__c] datetime,
    [Capability_Type__c] varchar(20) NOT NULL,
    [Ecomm_License_Id__c] int NOT NULL,
    [Ecomm_License_Key__c] uniqueidentifier NOT NULL,
    [Expiration_Date__c] datetime,
    [License_Attribute_Value__c] varchar(50),
    [License_Attribute__c] varchar(100),
    [License_Category_Id__c] char(18) NOT NULL,
    [License_Distribution_Method_Id__c] char(18) NOT NULL,
    [Parent_License_Id__c] int,
    [Product_Line__c] varchar(40) NOT NULL,
    [Seats__c] int NOT NULL,
    [Status__c] varchar(20) NOT NULL,
    [Type__c] varchar(20) NOT NULL
);
CREATE TABLE [dbo].[temp_sony_product_line_update] (
    [license_id] nchar(10),
    [processed] tinyint
);
CREATE TABLE [dbo].[temp_trial_licenses] (
    [license_id] int,
    [capability_expiration_date] datetime,
    [license_seats] int
);
CREATE TABLE [dbo].[temp_web_ip] (
    [web_ip_id] int NOT NULL,
    [ip_address] varchar(24) NOT NULL,
    [company_name] nvarchar(255),
    [country_id] smallint,
    [state] nvarchar(2),
    [city] nvarchar(130),
    [postal_code] nvarchar(32),
    [insert_date] datetime NOT NULL,
    [region] nvarchar(100),
    [exclude] tinyint
,
    PRIMARY KEY ([web_ip_id])
);
CREATE TABLE [dbo].[temp_yamada_prevx_load] (
    [temp_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [prevx_update_type_id] tinyint NOT NULL,
    [process_date] datetime,
    [processed] tinyint NOT NULL
);
CREATE TABLE [dbo].[test_object_framework] (
    [test_object_framework_id] int IDENTITY(1,1) NOT NULL,
    [parent_id] int NOT NULL
,
    PRIMARY KEY ([test_object_framework_id])
);
CREATE TABLE [dbo].[test_replication] (
    [test_replication_id] int NOT NULL,
    [test_id] int NOT NULL,
    [test_priority_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([test_replication_id])
);
CREATE TABLE [dbo].[time_zone] (
    [time_zone_id] int NOT NULL,
    [time_zone_description] varchar(50),
    [location] varchar(50),
    [region] varchar(50)
);
CREATE TABLE [dbo].[time_zones] (
    [time_zone_id] int NOT NULL,
    [time_zone_description] varchar(50)
);
CREATE TABLE [dbo].[tmp_cart_refund] (
    [rec_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int,
    [processed] int NOT NULL
);
CREATE TABLE [dbo].[tmp_cart_refund_tracking] (
    [vendor_order_code] varchar(100) NOT NULL,
    [order_header_id] int NOT NULL,
    [total_amount] money NOT NULL,
    [sub_total_amount] money NOT NULL,
    [tax_total] money NOT NULL,
    [license_id] int NOT NULL,
    [keycode] varchar(40) NOT NULL,
    [capability_expiration_date] datetime,
    [insert_date] datetime NOT NULL
);
CREATE TABLE [dbo].[tmp_prevx_activate] (
    [activate_id] int IDENTITY(1,1) NOT NULL,
    [keycode] varchar(40) NOT NULL
);
CREATE TABLE [dbo].[tmp_prevx_update_license_monitor] (
    [license_id] int,
    [date_inserted] datetime
);
CREATE TABLE [dbo].[tmp_reprocess_order_header_id] (
    [rec_id] int IDENTITY(1,1) NOT NULL,
    [order_header_id] int NOT NULL
);
CREATE TABLE [dbo].[tmp_space_license_ids] (
    [rec_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int NOT NULL,
    [license_attribute_license_value] int NOT NULL,
    [license_attribute_tag] varchar(11) NOT NULL,
    [license_attribute_id] int NOT NULL
);
CREATE TABLE [dbo].[tmp_zomax_export] (
    [rowid] int IDENTITY(1,1) NOT NULL,
    [invoice_code] varchar(100),
    [Filler] varchar(3),
    [keycode] varchar(100),
    [fullfiller_code] varchar(20),
    [product_description] varchar(30),
    [quantity] int,
    [purchased_date] datetime,
    [somecode] char(3),
    [shipping_name] varchar(100),
    [company] varchar(100),
    [address1] varchar(100),
    [address2] varchar(100),
    [city] varchar(100),
    [state] char(2),
    [postal_code] varchar(32),
    [country] varchar(50),
    [customer_email] varchar(100),
    [price] decimal(10,2),
    [sm_product_id] varchar(10)
,
    PRIMARY KEY ([rowid])
);
CREATE TABLE [dbo].[trial] (
    [trial_id] int IDENTITY(1,1) NOT NULL,
    [trial_registration_id] int,
    [license_id] int NOT NULL,
    [customer_id] int,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([trial_id])
);
CREATE TABLE [dbo].[trial_external_registration] (
    [trial_external_registration_id] int IDENTITY(1,1) NOT NULL,
    [registration_key_value] nvarchar(2000) NOT NULL,
    [status] varchar(10) NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL
,
    PRIMARY KEY ([trial_external_registration_id])
);
CREATE TABLE [dbo].[trial_registration] (
    [trial_registration_id] int IDENTITY(1,1) NOT NULL,
    [first_name] nvarchar(225),
    [last_name] nvarchar(225),
    [company_name] nvarchar(255),
    [phone_number] varchar(64),
    [customer_email] varchar(100),
    [address_1] nvarchar(255),
    [address_2] nvarchar(255),
    [city] nvarchar(130),
    [state] nvarchar(2),
    [postal_code] nvarchar(32),
    [country] varchar(75),
    [language_code] varchar(2),
    [location_code] varchar(3),
    [license_category_name] varchar(10),
    [license_seats] int,
    [years] float,
    [trial_days] int,
    [license_keycode_type_id] int,
    [partner_key] varchar(36),
    [partner_account_key] varchar(36),
    [partner_account_code] varchar(50),
    [product_id] int,
    [license_distribution_method_id] int,
    [keycode] varchar(40),
    [request_type] varchar(20),
    [sfdc_lead_id] varchar(18),
    [sfdc_opportunity_id] varchar(18),
    [insert_date] datetime NOT NULL,
    [sfdc_trial_id] varchar(18),
    [salesforce_campaign_id] varchar(18),
    [sfdc_distributor_id] varchar(18),
    [sfdc_reseller_id] varchar(18),
    [eloqua_parameters] varchar(MAX),
    [salesforce_license_id] varchar(18),
    [company_type_name] varchar(50),
    [opt_in] int
,
    PRIMARY KEY ([trial_registration_id])
);
CREATE TABLE [dbo].[trial_registration_json] (
    [trial_registration_json_id] int IDENTITY(1,1) NOT NULL,
    [trial_registration_id] int NOT NULL,
    [trial_registration_json] nvarchar(MAX) NOT NULL
,
    PRIMARY KEY ([trial_registration_json_id])
);
CREATE TABLE [dbo].[trial_registration_keycode] (
    [trial_registration_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int,
    [keycode] varchar(40),
    [product_id] int,
    [location_code] varchar(3),
    [language_code] varchar(2),
    [rc] varchar(50),
    [look_up_count] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [BBY_Employee_Number] varchar(50),
    [license_message_id] int
,
    PRIMARY KEY ([trial_registration_id])
);
CREATE TABLE [dbo].[trial_registration_keycode_history] (
    [trial_registration_keycode_history_id] int IDENTITY(1,1) NOT NULL,
    [trial_registration_id] int NOT NULL,
    [customer_id] int,
    [keycode] varchar(40),
    [product_id] int,
    [location_code] varchar(3),
    [language_code] varchar(2),
    [rc] varchar(50),
    [look_up_count] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [BBY_Employee_Number] varchar(50),
    [license_message_id] int,
    [history_date] datetime NOT NULL,
    [history_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([trial_registration_keycode_history_id])
);
CREATE TABLE [dbo].[unity_token] (
    [unity_token_id] int IDENTITY(1,1) NOT NULL,
    [unity_login] varchar(100) NOT NULL,
    [refresh_token] varchar(32) NOT NULL,
    [token_expiration] datetime NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([unity_token_id])
);
CREATE TABLE [dbo].[UPDATE_Requests_sequence] (
    [sequence_id] int IDENTITY(1,1) NOT NULL,
    [insert_date] datetime
);
CREATE TABLE [dbo].[upgrade_method] (
    [upgrade_method_id] int IDENTITY(1,1) NOT NULL,
    [operation_code] int NOT NULL,
    [upgrade_method_description] varchar(50) NOT NULL
,
    PRIMARY KEY ([upgrade_method_id])
);
CREATE TABLE [dbo].[usage_pricing_model] (
    [usage_pricing_model_id] tinyint IDENTITY(1,1) NOT NULL,
    [usage_pricing_model_name] varchar(50) NOT NULL,
    [usage_pricing_model_description] nvarchar(MAX) NOT NULL,
    [usage_pricing_storageGB] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([usage_pricing_model_id])
);
CREATE TABLE [dbo].[user_emails] (
    [product_id] decimal(10,0) NOT NULL,
    [date_received] datetime,
    [rc] decimal(10,0),
    [email] varchar(255),
    [sessionid] bigint,
    [language] char(2),
    [locale] char(3),
    [session_id] bigint,
    [opt_in] int
);
CREATE TABLE [dbo].[vanity_url] (
    [vanity_url_id] int IDENTITY(1,1) NOT NULL,
    [vanity_url] nvarchar(100) NOT NULL,
    [vanity_url_description] nvarchar(100) NOT NULL,
    [host] nvarchar(100) NOT NULL,
    [target_url] nvarchar(200) NOT NULL,
    [default_url] nvarchar(200) NOT NULL,
    [activation_date] datetime NOT NULL,
    [expiration_date] datetime,
    [status] varchar(10) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([vanity_url_id])
);
CREATE TABLE [dbo].[vanity_url_audit] (
    [vanity_url_audit_id] int IDENTITY(1,1) NOT NULL,
    [vanity_url_id] int NOT NULL,
    [vanity_url] nvarchar(100) NOT NULL,
    [vanity_url_description] nvarchar(100) NOT NULL,
    [host] nvarchar(100) NOT NULL,
    [target_url] nvarchar(200) NOT NULL,
    [default_url] nvarchar(200) NOT NULL,
    [activation_date] datetime NOT NULL,
    [expiration_date] datetime,
    [status] varchar(10) NOT NULL,
    [last_modified_date] datetime NOT NULL,
    [last_modified_by] varchar(200) NOT NULL,
    [audit_date] datetime
,
    PRIMARY KEY ([vanity_url_audit_id])
);
CREATE TABLE [dbo].[vanity_url_log] (
    [vanity_url_log_id] int IDENTITY(1,1) NOT NULL,
    [vanity_url_id] int NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([vanity_url_log_id])
);
CREATE TABLE [dbo].[vault] (
    [vault_id] int IDENTITY(1,1) NOT NULL,
    [product_platform_id] tinyint,
    [vault_name] nvarchar(100) NOT NULL,
    [vault_url] nvarchar(255) NOT NULL,
    [vault_datacenter_key] nvarchar(100),
    [vault_datacenter_name] nvarchar(100),
    [vault_status] varchar(50),
    [insert_date] datetime NOT NULL,
    [billing_type] varchar(20),
    [hostname] varchar(100),
    [salesforce_vault_id] varchar(18)
,
    PRIMARY KEY ([vault_id])
);
CREATE TABLE [dbo].[vault_country] (
    [vault_country_id] int IDENTITY(1,1) NOT NULL,
    [vault_id] int,
    [country_id] smallint NOT NULL
,
    PRIMARY KEY ([vault_country_id])
);
CREATE TABLE [dbo].[vault_license_category] (
    [vault_license_category_id] int IDENTITY(1,1) NOT NULL,
    [vault_id] int,
    [license_category_id] tinyint NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([vault_license_category_id])
);
CREATE TABLE [dbo].[vendor] (
    [vendor_id] int IDENTITY(1,1) NOT NULL,
    [vendor_name] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL
,
    PRIMARY KEY ([vendor_id])
);
CREATE TABLE [dbo].[vendor_customer] (
    [vendor_customer_id] int IDENTITY(1,1) NOT NULL,
    [vendor_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [vendor_customer_code] varchar(100) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(50),
    [modified_date] datetime,
    [modified_by] varchar(50),
    [partner_id] int
,
    PRIMARY KEY ([vendor_customer_id])
);
CREATE TABLE [dbo].[vendor_customer_audit] (
    [vendor_customer_audit_id] int IDENTITY(1,1) NOT NULL,
    [vendor_customer_id] int NOT NULL,
    [vendor_id] int NOT NULL,
    [customer_id] int NOT NULL,
    [vendor_customer_code] varchar(100) NOT NULL,
    [insert_date] datetime,
    [insert_by] varchar(50),
    [modified_date] datetime,
    [modified_by] varchar(50),
    [partner_id] int,
    [audit_date] datetime
,
    PRIMARY KEY ([vendor_customer_audit_id])
);
CREATE TABLE [dbo].[vendor_order] (
    [vendor_order_id] int IDENTITY(1,1) NOT NULL,
    [vendor_id] int NOT NULL,
    [vendor_order_code] varchar(100) NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([vendor_order_id])
);
CREATE TABLE [dbo].[vendor_order_item] (
    [vendor_order_item_id] int IDENTITY(1,1) NOT NULL,
    [order_item_id] int NOT NULL,
    [order_header_id] int NOT NULL,
    [transaction_date] datetime NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL,
    [vendor_order_item_code] varchar(36)
,
    PRIMARY KEY ([vendor_order_item_id])
);
CREATE TABLE [dbo].[vendor_order_type] (
    [vendor_id] int NOT NULL,
    [order_type_id] int NOT NULL
,
    PRIMARY KEY ([order_type_id], [vendor_id])
);
CREATE TABLE [dbo].[vendor_order_updated_vendor_order] (
    [vendor_order_updated_vendor_order_id] int IDENTITY(1,1) NOT NULL,
    [vendor_order_code] varchar(100) NOT NULL,
    [vendor_id_old] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([vendor_order_updated_vendor_order_id])
);
CREATE TABLE [dbo].[vendor_product] (
    [vendor_product_id] int IDENTITY(1,1) NOT NULL,
    [vendor_id] int NOT NULL,
    [vendor_product_code] varchar(100) NOT NULL,
    [product_id] int NOT NULL,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(200) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(200) NOT NULL
,
    PRIMARY KEY ([vendor_product_id])
);
CREATE TABLE [dbo].[web_ip] (
    [web_ip_id] int IDENTITY(1,1) NOT NULL,
    [ip_address] varchar(24) NOT NULL,
    [company_name] nvarchar(255),
    [country_id] smallint,
    [state] nvarchar(2),
    [city] nvarchar(130),
    [postal_code] nvarchar(32),
    [insert_date] datetime NOT NULL,
    [region] nvarchar(100),
    [exclude] tinyint
,
    PRIMARY KEY ([web_ip_id])
);
CREATE TABLE [dbo].[web_ip_activity] (
    [web_ip_activity] int IDENTITY(1,1) NOT NULL,
    [web_ip_id] int NOT NULL,
    [session_id] varchar(20) NOT NULL,
    [page_url] varchar(120) NOT NULL,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([web_ip_activity])
);
CREATE TABLE [dbo].[websales_Accounting] (
    [account_id] int IDENTITY(1,1) NOT NULL,
    [invoice_code] varchar(20),
    [product_id] int NOT NULL,
    [quantity] int NOT NULL,
    [extended_price] decimal(15,2) NOT NULL,
    [tax_item_amount] decimal(15,2) NOT NULL,
    [auth_batch_id] datetime NOT NULL,
    [payment_method] smallint NOT NULL,
    [TranProcessID] int NOT NULL,
    [effective_date] datetime,
    [order_item_id] int,
    [vendor_order_date] datetime,
    [capability_expiration_date] datetime,
    [processed] tinyint,
    [finance_push_date] datetime,
    [insert_date] datetime,
    [insert_by] varchar(50),
    [modified_date] datetime,
    [modified_by] varchar(50)
,
    PRIMARY KEY ([account_id])
);
CREATE TABLE [dbo].[websales_availability] (
    [ID] int IDENTITY(1,1) NOT NULL,
    [TranProcessID] int NOT NULL,
    [Batch_Date] datetime NOT NULL,
    [ProcessDate] datetime NOT NULL,
    [PushedToEpicorDate] datetime,
    [NumRows] int
,
    PRIMARY KEY ([ID])
);
CREATE TABLE [dbo].[websales_cb_validation] (
    [Invoice Code] int,
    [Merchant] varchar(50),
    [Order Date] date,
    [Effective Date] date,
    [Expiration Date] date,
    [Item ID] int,
    [Quantity] int,
    [Price] float,
    [Tax] float,
    [Currency] varchar(20),
    [Order Item Id] int,
    [country_of_origin] varchar(20),
    [contracting_entity] varchar(20),
    [Order Type] varchar(50),
    [validation_issues] varchar(MAX)
);
CREATE TABLE [dbo].[websales_check] (
    [invoice_Code] varchar(16) NOT NULL,
    [line_item] int NOT NULL,
    [product_id] int NOT NULL,
    [qty] int,
    [price] decimal(38,4)
);
CREATE TABLE [dbo].[wise_beta] (
    [wise_beta_id] int IDENTITY(1,1) NOT NULL,
    [first_name] nvarchar(255),
    [last_name] nvarchar(255),
    [email_address] varchar(100),
    [keycode] varchar(50),
    [product_id] int,
    [insert_date] datetime NOT NULL,
    [processed] int
,
    PRIMARY KEY ([wise_beta_id])
);
CREATE TABLE [dbo].[ww_64_upgrade] (
    [ww_64_upgrade_id] int IDENTITY(1,1) NOT NULL,
    [customer_id] int,
    [keycode] varchar(40),
    [capability_activation_days] int,
    [source] varchar(20),
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([ww_64_upgrade_id])
);
CREATE TABLE [dbo].[wwss_account] (
    [wwss_account_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_seats] int NOT NULL,
    [wwss_domain] varchar(100),
    [wwss_ip_range] varchar(MAX),
    [time_zone_id] int,
    [wwss_external_id] int,
    [wwss_account_user_name] varchar(100),
    [partner_id] int,
    [distributor_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [wwss_guid] uniqueidentifier,
    [customer_id] int
,
    PRIMARY KEY ([wwss_account_id])
);
CREATE TABLE [dbo].[wwss_account_archive] (
    [wwss_account_archive_id] int IDENTITY(1,1) NOT NULL,
    [wwss_account_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_seats] int NOT NULL,
    [wwss_domain] varchar(100),
    [wwss_ip_range] varchar(MAX),
    [time_zone_id] int,
    [wwss_external_id] int,
    [wwss_account_user_name] varchar(100),
    [partner_id] int,
    [distributor_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [wwss_guid] uniqueidentifier,
    [archive_date] datetime NOT NULL
,
    PRIMARY KEY ([wwss_account_archive_id])
);
CREATE TABLE [dbo].[wwss_account_audit] (
    [wwss_account_audit_id] int IDENTITY(1,1) NOT NULL,
    [wwss_account_id] int NOT NULL,
    [license_id] int NOT NULL,
    [license_seats] int NOT NULL,
    [wwss_domain] varchar(100),
    [wwss_ip_range] varchar(MAX),
    [time_zone_id] int,
    [wwss_external_id] int,
    [wwss_account_user_name] varchar(100),
    [partner_id] int,
    [distributor_id] int,
    [insert_date] datetime NOT NULL,
    [insert_by] varchar(50) NOT NULL,
    [modified_date] datetime NOT NULL,
    [modified_by] varchar(50) NOT NULL,
    [audit_date] datetime NOT NULL
,
    PRIMARY KEY ([wwss_account_audit_id])
);
CREATE TABLE [dbo].[wwss_template] (
    [wwss_template_id] tinyint IDENTITY(1,1) NOT NULL,
    [wwss_template_name] varchar(50) NOT NULL,
    [wwss_template_description] varchar(500) NOT NULL,
    [process_date_basis] varchar(50),
    [process_offset_days] int
,
    PRIMARY KEY ([wwss_template_id])
);
CREATE TABLE [dbo].[wwss_template_update_type] (
    [wwss_template_update_type_id] int IDENTITY(1,1) NOT NULL,
    [wwss_template_id] int NOT NULL,
    [wwss_update_type_id] tinyint NOT NULL,
    [next_update_type_id] tinyint,
    [next_wwss_template_id] tinyint
,
    PRIMARY KEY ([wwss_template_update_type_id])
);
CREATE TABLE [dbo].[wwss_update] (
    [wwss_update_id] int IDENTITY(1,1) NOT NULL,
    [wwss_account_id] int NOT NULL,
    [wwss_template_id] tinyint NOT NULL,
    [wwss_update_type_id] tinyint NOT NULL,
    [wwss_update_status_id] tinyint NOT NULL,
    [response_code] tinyint,
    [response_text] varchar(100),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime
,
    PRIMARY KEY ([wwss_update_id])
);
CREATE TABLE [dbo].[wwss_update_archive] (
    [wwss_update_archive_id] int IDENTITY(1,1) NOT NULL,
    [wwss_update_id] int NOT NULL,
    [wwss_account_id] int NOT NULL,
    [wwss_template_id] tinyint NOT NULL,
    [wwss_update_type_id] tinyint NOT NULL,
    [wwss_update_status_id] tinyint NOT NULL,
    [response_code] tinyint,
    [response_text] varchar(100),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime,
    [archive_date] datetime NOT NULL
,
    PRIMARY KEY ([wwss_update_archive_id])
);
CREATE TABLE [dbo].[wwss_update_failure] (
    [wwss_update_failure_id] int IDENTITY(1,1) NOT NULL,
    [wwss_update_id] int NOT NULL,
    [wwss_account_id] int NOT NULL,
    [wwss_template_id] tinyint NOT NULL,
    [wwss_update_type_id] tinyint NOT NULL,
    [wwss_update_status_id] tinyint NOT NULL,
    [response_code] tinyint,
    [response_text] varchar(100),
    [update_attempts] tinyint NOT NULL,
    [insert_date] datetime NOT NULL,
    [modified_date] datetime NOT NULL,
    [process_date] datetime,
    [failure_date] datetime NOT NULL
,
    PRIMARY KEY ([wwss_update_failure_id])
);
CREATE TABLE [dbo].[wwss_update_license] (
    [license_id] int NOT NULL,
    [insert_date] datetime
);
CREATE TABLE [dbo].[wwss_update_status] (
    [wwss_update_status_id] tinyint IDENTITY(1,1) NOT NULL,
    [wwss_update_status_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([wwss_update_status_id])
);
CREATE TABLE [dbo].[wwss_update_type] (
    [wwss_update_type_id] tinyint IDENTITY(1,1) NOT NULL,
    [wwss_update_type_name] varchar(20) NOT NULL
,
    PRIMARY KEY ([wwss_update_type_id])
);
CREATE TABLE [dbo].[ymada_registration_license] (
    [ymada_registration_id] int IDENTITY(1,1) NOT NULL,
    [license_id] int,
    [insert_date] datetime NOT NULL
,
    PRIMARY KEY ([ymada_registration_id])
);
CREATE TABLE [dbo].[zipcodes] (
    [zipcode] char(5) NOT NULL,
    [state_name] varchar(50) NOT NULL,
    [state_id] char(2) NOT NULL,
    [areacode] char(3),
    [city] varchar(30),
    [citytype] char(1),
    [latitude] float,
    [longitude] float,
    [state] varchar(75),
    [statecode] char(2),
    [zipcodetype] char(1)
,
    PRIMARY KEY ([zipcode])
);
CREATE TABLE [dbo].[zuora_product_pricing] (
    [zuora_product_pricing_id] int IDENTITY(1,1) NOT NULL,
    [campaign_id] int,
    [rate_plan_id] varchar(50),
    [license_category_id] int,
    [retail_price] money,
    [renewal_price] money,
    [discount] money,
    [product_id] int,
    [product_type_id] int,
    [seats] int,
    [identities] int,
    [years] int,
    [is_upgrade] bit,
    [preupgrade_license_category_id] int,
    [preupgrade_license_seats] int,
    [max_remaining_days_rate_validity] int,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200),
    [sku] varchar(200)
,
    PRIMARY KEY ([zuora_product_pricing_id])
);
CREATE TABLE [dbo].[zuora_product_pricing_history] (
    [zuora_product_pricing_history_id] int IDENTITY(1,1) NOT NULL,
    [zuora_product_pricing_id] int NOT NULL,
    [campaign_id] int,
    [rate_plan_id] varchar(50),
    [license_category_id] int,
    [retail_price] money,
    [renewal_price] money,
    [discount] money,
    [product_id] int,
    [product_type_id] int,
    [seats] int,
    [identities] int,
    [years] int,
    [is_upgrade] bit,
    [preupgrade_license_category_id] int,
    [preupgrade_license_seats] int,
    [max_remaining_days_rate_validity] int,
    [insert_date] datetime,
    [insert_by] varchar(200),
    [modified_date] datetime,
    [modified_by] varchar(200),
    [sku] varchar(200),
    [history_date] datetime,
    [history_by] varchar(200)
,
    PRIMARY KEY ([zuora_product_pricing_history_id])
);
