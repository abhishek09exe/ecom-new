-- =============================================================================
-- PATCH 002 — Clean up duplicate vendor_order_code rows and add UNIQUE constraint
-- =============================================================================
-- Run this against ecom_cart_dev to:
--   1. Remove duplicate cart_order rows left from failed test runs
--      (keeps the most recent row per vendor_order_code)
--   2. Add a UNIQUE constraint on vendor_order_code (nullable-safe: only one NULL allowed)
--
-- Safe to run multiple times.
-- =============================================================================

USE ecom_cart_dev;
GO

-- Step 1: Delete older duplicate rows, keeping only the highest cart_order_id per code
DELETE FROM dbo.cart_order
WHERE cart_order_id NOT IN (
	SELECT MAX(cart_order_id)
	FROM   dbo.cart_order
	WHERE  vendor_order_code IS NOT NULL
	GROUP  BY vendor_order_code
)
AND vendor_order_code IS NOT NULL;

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' duplicate row(s) removed.';
GO

-- Step 2: Add unique constraint if not already present
IF NOT EXISTS (
	SELECT 1 FROM sys.indexes
	WHERE  name   = 'UQ_cart_order_vendor_order_code'
	  AND  object_id = OBJECT_ID('dbo.cart_order')
)
BEGIN
	ALTER TABLE dbo.cart_order
		ADD CONSTRAINT UQ_cart_order_vendor_order_code
			UNIQUE (vendor_order_code);
	PRINT 'Unique constraint added.';
END
ELSE
BEGIN
	PRINT 'Unique constraint already exists — skipped.';
END
GO
