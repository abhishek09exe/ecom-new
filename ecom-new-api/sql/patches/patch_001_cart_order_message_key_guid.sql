-- =============================================================================
-- PATCH 001 — cart_order_message.message_key: VARCHAR(36) → UNIQUEIDENTIFIER
-- =============================================================================
-- Run this against ecom_cart_dev if the database already exists and was created
-- BEFORE local_dev_setup.sql was updated to use UNIQUEIDENTIFIER for message_key.
--
-- Safe to run multiple times (checks column type before altering).
-- =============================================================================

USE ecom_cart_dev;
GO

-- Only patch if the column is still the old VARCHAR(36) type
IF EXISTS (
	SELECT 1
	FROM   INFORMATION_SCHEMA.COLUMNS
	WHERE  TABLE_SCHEMA = 'dbo'
	  AND  TABLE_NAME   = 'cart_order_message'
	  AND  COLUMN_NAME  = 'message_key'
	  AND  DATA_TYPE    = 'varchar'
)
BEGIN
	PRINT 'Patching cart_order_message.message_key: VARCHAR(36) → UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID()';

	-- Step 1: Drop the old column
	ALTER TABLE dbo.cart_order_message
		DROP COLUMN message_key;

	-- Step 2: Add the new column with the correct type
	ALTER TABLE dbo.cart_order_message
		ADD message_key UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();

	PRINT 'Patch applied successfully.';
END
ELSE
BEGIN
	PRINT 'Patch not needed — message_key is already UNIQUEIDENTIFIER or column does not exist.';
END
GO
