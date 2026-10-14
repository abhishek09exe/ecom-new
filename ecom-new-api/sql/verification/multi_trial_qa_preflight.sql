/*
Read-only ECOM Multi Trial QA preflight.
Run this script in the intended QA database before deploying or testing Multi Trial.
It reads system catalogs, stored-procedure definitions, schema-backed reference tables,
and product configuration only. It does not execute any stored procedure or modify data.

The fallback product ID is intentionally unchanged at 152200130. A PASS confirms the
configuration relationships queried below exist; the database owner must still confirm
that this is the approved QA fallback product and its keycode/distribution setup is intended.
*/
SET NOCOUNT ON;

SELECT @@SERVERNAME AS target_server,
       DB_NAME() AS target_database,
       compatibility_level
FROM sys.databases
WHERE database_id = DB_ID();

DECLARE @registrationProcedureId int = OBJECT_ID(N'dbo.usp_trial_insert_trial_registration', N'P');
DECLARE @trialProcedureId int = OBJECT_ID(N'dbo.usp_trial_insert_trial', N'P');
DECLARE @serviceInsertProcedureId int = OBJECT_ID(N'dbo.usp_license_service_insert_service', N'P');
DECLARE @licenseServiceInsertProcedureId int = OBJECT_ID(N'dbo.usp_license_service_insert_license', N'P');
DECLARE @generatorProcedureId int = OBJECT_ID(N'dbo.usp_license_generate_keycode_cart', N'P');
DECLARE @registrationDefinition nvarchar(max) = OBJECT_DEFINITION(@registrationProcedureId);
DECLARE @trialDefinition nvarchar(max) = OBJECT_DEFINITION(@trialProcedureId);
DECLARE @serviceInsertDefinition nvarchar(max) = OBJECT_DEFINITION(@serviceInsertProcedureId);
DECLARE @licenseServiceInsertDefinition nvarchar(max) = OBJECT_DEFINITION(@licenseServiceInsertProcedureId);
DECLARE @generatorDefinition nvarchar(max) = OBJECT_DEFINITION(@generatorProcedureId);
DECLARE @registrationNormalized nvarchar(max);
DECLARE @trialNormalized nvarchar(max);
DECLARE @serviceInsertNormalized nvarchar(max);
DECLARE @licenseServiceInsertNormalized nvarchar(max);
DECLARE @generatorNormalized nvarchar(max);
DECLARE @legacyServiceDispatch nvarchar(160) = N'EXEC USP_LICENSE_SERVICE_INSERT_SERVICE @LICENSE_SERVICE_TYPE_NAME, @TRIAL_REGISTRATION_JSON, @RETURN_LICENSE_SERVICE_ID';
DECLARE @fallbackProductId int = 152200130;
DECLARE @fallbackUsesKeycode int;
DECLARE @fallbackProductFamilyId int;
DECLARE @fallbackRootProductId int;
DECLARE @fallbackKeycodeTypeId int;
DECLARE @fallbackFamilyPrefix char(2);
DECLARE @fallbackCategoryId tinyint;
DECLARE @fallbackCategoryCount int = 0;
DECLARE @fallbackProductLineId int;
DECLARE @fallbackProductLineCount int = 0;
DECLARE @fallbackProductLineProductCount int = 0;
DECLARE @fallbackCategoryDistributionCount int = 0;
DECLARE @fallbackGenericDistributionCount int = 0;
DECLARE @fallbackDistributionCount int = 0;
DECLARE @fallbackDistributionMethodCount int = 0;
DECLARE @fallbackVersionSource nvarchar(100);
DECLARE @compatibilityLevel int = (SELECT compatibility_level FROM sys.databases WHERE database_id = DB_ID());
DECLARE @registrationParameterCount int =
    (SELECT COUNT(*) FROM sys.parameters WHERE object_id = @registrationProcedureId AND parameter_id > 0);

SET @registrationNormalized = UPPER(REPLACE(REPLACE(REPLACE(COALESCE(@registrationDefinition, N''), CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
WHILE CHARINDEX(N'  ', @registrationNormalized) > 0
    SET @registrationNormalized = REPLACE(@registrationNormalized, N'  ', N' ');
SET @registrationNormalized = @registrationNormalized COLLATE Latin1_General_100_BIN2;

SET @trialNormalized = UPPER(REPLACE(REPLACE(REPLACE(COALESCE(@trialDefinition, N''), CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
WHILE CHARINDEX(N'  ', @trialNormalized) > 0
    SET @trialNormalized = REPLACE(@trialNormalized, N'  ', N' ');
SET @trialNormalized = @trialNormalized COLLATE Latin1_General_100_BIN2;

SET @serviceInsertNormalized = UPPER(REPLACE(REPLACE(REPLACE(COALESCE(@serviceInsertDefinition, N''), CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
WHILE CHARINDEX(N'  ', @serviceInsertNormalized) > 0
    SET @serviceInsertNormalized = REPLACE(@serviceInsertNormalized, N'  ', N' ');
SET @serviceInsertNormalized = @serviceInsertNormalized COLLATE Latin1_General_100_BIN2;

SET @licenseServiceInsertNormalized = UPPER(REPLACE(REPLACE(REPLACE(COALESCE(@licenseServiceInsertDefinition, N''), CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
WHILE CHARINDEX(N'  ', @licenseServiceInsertNormalized) > 0
    SET @licenseServiceInsertNormalized = REPLACE(@licenseServiceInsertNormalized, N'  ', N' ');
SET @licenseServiceInsertNormalized = @licenseServiceInsertNormalized COLLATE Latin1_General_100_BIN2;

SET @generatorNormalized = UPPER(REPLACE(REPLACE(REPLACE(COALESCE(@generatorDefinition, N''), CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
WHILE CHARINDEX(N'  ', @generatorNormalized) > 0
    SET @generatorNormalized = REPLACE(@generatorNormalized, N'  ', N' ');
SET @generatorNormalized = @generatorNormalized COLLATE Latin1_General_100_BIN2;

SELECT @fallbackUsesKeycode = p.uses_keycode,
       @fallbackProductFamilyId = p.product_family_id,
       @fallbackRootProductId = p.root_product_id,
       @fallbackKeycodeTypeId = p.license_keycode_type_id,
       @fallbackFamilyPrefix = f.product_family_prefix
FROM dbo.product AS p
LEFT JOIN dbo.product_family AS f ON f.product_family_id = p.product_family_id
WHERE p.product_id = @fallbackProductId;

SELECT @fallbackCategoryCount = COUNT(*),
             @fallbackCategoryId = MIN(plc.license_category_id)
FROM dbo.product_license_category AS plc
JOIN dbo.license_category AS lc ON lc.license_category_id = plc.license_category_id
WHERE plc.product_id = @fallbackProductId
  AND lc.license_category_name = 'SAEP';

SELECT @fallbackProductLineCount = COUNT(*),
             @fallbackProductLineId = MIN(product_line_id)
FROM dbo.license_category_product_line
WHERE license_category_id = @fallbackCategoryId
    AND language_code = 'EN'
    AND location_code = 'USA';

SELECT @fallbackProductLineProductCount = COUNT(*)
 FROM dbo.product_line_product
WHERE product_id = @fallbackProductId
    AND product_line_id = @fallbackProductLineId;

IF @fallbackCategoryCount = 1 AND @fallbackProductLineCount = 1
    AND @fallbackProductLineProductCount = 1 AND @fallbackKeycodeTypeId IS NOT NULL
BEGIN
        SELECT @fallbackCategoryDistributionCount = COUNT(*)
        FROM dbo.product_line_license_distribution_method
        WHERE product_line_id = @fallbackProductLineId
            AND license_keycode_type_id = @fallbackKeycodeTypeId
            AND trial = 1
            AND license_category_id = @fallbackCategoryId;

        IF @fallbackCategoryDistributionCount > 0
        BEGIN
                SET @fallbackDistributionCount = @fallbackCategoryDistributionCount;
                SELECT @fallbackDistributionMethodCount = COUNT(*)
                FROM dbo.product_line_license_distribution_method AS mapping
                JOIN dbo.license_distribution_method AS dm
                    ON dm.license_distribution_method_id = mapping.license_distribution_method_id
                WHERE mapping.product_line_id = @fallbackProductLineId
                    AND mapping.license_keycode_type_id = @fallbackKeycodeTypeId
                    AND mapping.trial = 1
                    AND mapping.license_category_id = @fallbackCategoryId;
        END;
        ELSE
        BEGIN
                SELECT @fallbackGenericDistributionCount = COUNT(*)
                FROM dbo.product_line_license_distribution_method
                WHERE product_line_id = @fallbackProductLineId
                    AND license_keycode_type_id = @fallbackKeycodeTypeId
                    AND trial = 1
                    AND license_category_id IS NULL;
                SET @fallbackDistributionCount = @fallbackGenericDistributionCount;
                SELECT @fallbackDistributionMethodCount = COUNT(*)
                FROM dbo.product_line_license_distribution_method AS mapping
                JOIN dbo.license_distribution_method AS dm
                    ON dm.license_distribution_method_id = mapping.license_distribution_method_id
                WHERE mapping.product_line_id = @fallbackProductLineId
                    AND mapping.license_keycode_type_id = @fallbackKeycodeTypeId
                    AND mapping.trial = 1
                    AND mapping.license_category_id IS NULL;
        END;
END;

SET @fallbackVersionSource = CASE
    WHEN EXISTS (SELECT 1 FROM dbo.product_versions WHERE product_id = @fallbackProductId)
        THEN N'Product version exists on fallback product.'
    WHEN @fallbackRootProductId IS NOT NULL
         AND EXISTS (SELECT 1 FROM dbo.product_versions WHERE product_id = @fallbackRootProductId)
        THEN N'Product version resolves through root_product_id.'
    WHEN CHARINDEX(N'IF @PRODUCT_VERSION IS NULL BEGIN SELECT @PRODUCT_VERSION = ''10'' END', @generatorNormalized) > 0
        THEN N'No direct/root product version; checked-in generator default 10 applies.'
    ELSE N'No direct/root version and generator default was not found.'
END;

;WITH Checks AS
(
    SELECT N'Connected to expected QA server/database' AS check_name,
           CASE WHEN @@SERVERNAME COLLATE Latin1_General_100_BIN2 = N'QADENECOM6'
                      AND DB_NAME() COLLATE Latin1_General_100_BIN2 = N'ecommerce_VH14'
                THEN 'PASS' ELSE 'FAIL' END AS result,
           CONCAT(N'Current server=', COALESCE(CONVERT(nvarchar(128), @@SERVERNAME), N'<unavailable>'),
                  N'; database=', COALESCE(DB_NAME(), N'<unavailable>'),
                  N'; expected QADENECOM6.ecommerce_VH14.') AS details
    UNION ALL
    SELECT N'Database compatibility level supports JSON operations',
           CASE WHEN @compatibilityLevel >= 130 THEN 'PASS' ELSE 'FAIL' END AS result,
           CONCAT(N'Current compatibility_level=', COALESCE(CONVERT(nvarchar(12), @compatibilityLevel), N'<unavailable>'), N'; JSON functions require level 130 or later.') AS details
    UNION ALL
    SELECT N'Registration procedure matches the current 34-parameter application contract',
           CASE WHEN @registrationProcedureId IS NOT NULL
                AND @registrationParameterCount = 34
                AND NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @registrationProcedureId
                    AND name = N'@selected_product_id')
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @registrationProcedureId
                    AND name = N'@trial_registration_json' AND parameter_id = 33
                    AND system_type_id = 167 AND max_length = -1)
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @registrationProcedureId
                    AND name = N'@opt_in' AND parameter_id = 34
                    AND system_type_id = 56)
                AND CHARINDEX(N'@TRIAL_REGISTRATION_JSON VARCHAR(MAX) = NULL, @OPT_IN INT = NULL', @registrationNormalized) > 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'Requires exactly 34 parameters, trial_registration_json at 33, opt_in at 34, and no selected_product_id.'
    UNION ALL
    SELECT N'Registration procedure selects products internally',
           CASE WHEN CHARINDEX(N'IF @PRODUCT_ID IS NULL BEGIN', @registrationNormalized) > 0
                AND CHARINDEX(N'USP_CART_SELECT_NEW_PRODUCT_SET_ENT', @registrationNormalized) > 0
                AND CHARINDEX(N'USP_CART_SELECT_NEW_PRODUCT_SET ', @registrationNormalized) > 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'Requires the existing procedure branches for enterprise and consumer product selection.'
    UNION ALL
    SELECT N'Trial procedure has the legacy three-parameter signature and ordered types',
           CASE WHEN @trialProcedureId IS NOT NULL
                AND (SELECT COUNT(*) FROM sys.parameters WHERE object_id = @trialProcedureId AND parameter_id > 0) = 3
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
                    AND name = N'@trial_registration_id' AND parameter_id = 1
                    AND system_type_id = 56 AND user_type_id = 56)
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
                    AND name = N'@keycode' AND parameter_id = 2
                    AND system_type_id = 167 AND max_length = 40)
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
                    AND name = N'@account_order_json' AND parameter_id = 3
                    AND system_type_id = 167 AND max_length = -1)
                AND CHARINDEX(N'@TRIAL_REGISTRATION_ID INT, @KEYCODE VARCHAR(40) = NULL, @ACCOUNT_ORDER_JSON VARCHAR(MAX) = NULL', @trialNormalized) > 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'Requires exactly the legacy three parameters in order; staged_license_service_id must be absent.'
    UNION ALL
        SELECT N'Trial procedure performs null-keycode fallback and persists keycode JSON',
             CASE WHEN CHARINDEX(N'IF @KEYCODE IS NULL OR @KEYCODE = ', @trialNormalized) > 0
                 AND CHARINDEX(N'SELECT @PRODUCT_ID = 152200130', @trialNormalized) > 0
                 AND CHARINDEX(N'EXEC USP_LICENSE_GENERATE_KEYCODE_CART @PRODUCT_ID', @trialNormalized) > 0
                 AND CHARINDEX(N'JSON_MODIFY(@TRIAL_REGISTRATION_JSON,', @trialNormalized) > 0
                 AND CHARINDEX(N'$.KEYCODE', @trialNormalized) > 0
                 AND CHARINDEX(N'UPDATE TRIAL_REGISTRATION_JSON', @trialNormalized) > 0
                 AND CHARINDEX(N'UPDATE TRIAL_REGISTRATION', @trialNormalized) > 0
                 THEN 'PASS' ELSE 'FAIL' END,
             N'Text check confirms the null-keycode fallback and registration/keycode JSON update; DBA should inspect the returned definition.'
    UNION ALL
    SELECT N'Trial procedure has no staged-service fourth parameter',
           CASE WHEN @trialProcedureId IS NOT NULL
                AND (SELECT COUNT(*) FROM sys.parameters WHERE object_id = @trialProcedureId AND parameter_id > 0) = 3
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId AND name = N'@trial_registration_id' AND system_type_id = 56 AND user_type_id = 56)
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId AND name = N'@keycode' AND system_type_id = 167 AND user_type_id = 167 AND max_length = 40)
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId AND name = N'@account_order_json' AND system_type_id = 167 AND user_type_id = 167 AND max_length = -1)
                THEN 'PASS' ELSE 'FAIL' END,
           N'Requires the original three parameters only; count=3 rejects any staged-service fourth parameter.'
    UNION ALL
        SELECT N'Trial procedure calls legacy license-service insertion',
             CASE WHEN CHARINDEX(@legacyServiceDispatch, @trialNormalized) > 0
                 THEN 'PASS' ELSE 'FAIL' END,
             N'Requires usp_trial_insert_trial to call usp_license_service_insert_service with its license_create JSON.'
    UNION ALL
    SELECT N'Legacy license-service dispatcher supports synchronous license_create',
           CASE WHEN @serviceInsertProcedureId IS NOT NULL
                AND (SELECT COUNT(*) FROM sys.parameters WHERE object_id = @serviceInsertProcedureId AND parameter_id > 0) = 3
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @serviceInsertProcedureId
                    AND name = N'@license_service_type_name' AND parameter_id = 1
                    AND system_type_id = 167 AND max_length = 50)
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @serviceInsertProcedureId
                    AND name = N'@service_json' AND parameter_id = 2
                    AND system_type_id = 231 AND max_length = -1)
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @serviceInsertProcedureId
                    AND name = N'@return_license_service_id' AND parameter_id = 3
                    AND system_type_id = 104)
                AND CHARINDEX(N'IF @PROCESS_TYPE = ''SYNCHRONOUS''', @serviceInsertNormalized) > 0
                AND CHARINDEX(N'IF @LICENSE_SERVICE_TYPE_ID IN (50,20)', @serviceInsertNormalized) > 0
                AND CHARINDEX(N'EXEC USP_LICENSE_SERVICE_INSERT_LICENSE @LICENSE_SERVICE_ID', @serviceInsertNormalized) > 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'Requires the legacy service signature and synchronous type-20 license insertion branch.'
    UNION ALL
    SELECT N'License insertion procedure is present',
           CASE WHEN @licenseServiceInsertProcedureId IS NOT NULL
                AND (SELECT COUNT(*) FROM sys.parameters
                    WHERE object_id = @licenseServiceInsertProcedureId AND parameter_id > 0) = 1
                AND EXISTS (SELECT 1 FROM sys.parameters
                    WHERE object_id = @licenseServiceInsertProcedureId
                      AND name = N'@license_service_id' AND parameter_id = 1
                      AND system_type_id = 56 AND user_type_id = 56)
                THEN 'PASS' ELSE 'FAIL' END,
           N'Requires dbo.usp_license_service_insert_license(@license_service_id int), invoked by the synchronous license_create dispatcher.'
    UNION ALL
    SELECT N'license_create service reference row is present',
           CASE WHEN EXISTS (SELECT 1 FROM dbo.license_service_type
                WHERE license_service_type_id = 20
                  AND license_service_type_name COLLATE Latin1_General_100_BIN2 = 'license_create' COLLATE Latin1_General_100_BIN2
                  AND process_type COLLATE Latin1_General_100_BIN2 = 'synchronous' COLLATE Latin1_General_100_BIN2)
                THEN 'PASS' ELSE 'FAIL' END,
           N'Requires ID 20, exact name license_create and processing type synchronous.'
    UNION ALL
    SELECT N'Pending service status row is present',
           CASE WHEN EXISTS (SELECT 1 FROM dbo.license_service_status
                WHERE license_service_status_name COLLATE Latin1_General_100_CI_AS = 'Pending' COLLATE Latin1_General_100_CI_AS)
                THEN 'PASS' ELSE 'FAIL' END,
           N'Case-independent check using an explicit collation.'
    UNION ALL
    SELECT N'Fallback product exists and is keycode-enabled with SAEP family/category configuration',
           CASE WHEN @fallbackUsesKeycode = 1
                AND NULLIF(LTRIM(RTRIM(@fallbackFamilyPrefix)), '') IS NOT NULL
                 AND @fallbackCategoryCount = 1
                 AND @fallbackProductLineCount = 1
                 AND @fallbackProductLineProductCount = 1
                AND @fallbackKeycodeTypeId = 3
                THEN 'PASS' ELSE 'FAIL' END,
           CONCAT(N'Product ID 152200130; family ID=', COALESCE(CONVERT(nvarchar(20), @fallbackProductFamilyId), N'<missing>'),
                   N'; SAEP category matches=', @fallbackCategoryCount,
                   N'; EN/USA category product-line matches=', @fallbackProductLineCount,
                   N'; product/product-line matches=', @fallbackProductLineProductCount,
                   N'; category ID=', COALESCE(CONVERT(nvarchar(20), @fallbackCategoryId), N'<missing>'),
                  N'; keycode type=', COALESCE(CONVERT(nvarchar(20), @fallbackKeycodeTypeId), N'<missing>'),
                  N'. Owner must confirm this product is approved for QA fallback use.')
    UNION ALL
    SELECT N'Fallback product has an applicable trial distribution method',
            CASE WHEN @fallbackProductLineCount = 1 AND @fallbackDistributionCount = 1 AND @fallbackDistributionMethodCount = 1 THEN 'PASS' ELSE 'FAIL' END,
            CONCAT(N'EN/USA product-line mappings=', @fallbackProductLineCount,
                N'; applicable category-specific-or-generic mapping rows=', @fallbackDistributionCount,
                N'; resolved license_distribution_method rows=', @fallbackDistributionMethodCount,
                N'. Exactly one row is required at each resolution step.')
    UNION ALL
    SELECT N'Fallback product version source is resolvable',
           CASE WHEN @fallbackVersionSource <> N'No direct/root version and generator default was not found.' THEN 'PASS' ELSE 'FAIL' END,
           @fallbackVersionSource
    UNION ALL
    SELECT N'Generator signature and checked-in behavior are compatible',
           CASE WHEN @generatorProcedureId IS NOT NULL
                AND (SELECT COUNT(*) FROM sys.parameters WHERE object_id = @generatorProcedureId AND parameter_id > 0) = 5
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @generatorProcedureId AND name = N'@product_id' AND system_type_id = 56 AND user_type_id = 56 AND is_output = 0)
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @generatorProcedureId AND name = N'@license_distribution_method_id' AND system_type_id = 56 AND user_type_id = 56 AND is_output = 0)
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @generatorProcedureId AND name = N'@keycode' AND system_type_id = 167 AND user_type_id = 167 AND max_length = 40 AND is_output = 1)
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @generatorProcedureId AND name = N'@response_code' AND system_type_id = 56 AND user_type_id = 56 AND is_output = 1)
                AND EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @generatorProcedureId AND name = N'@message' AND system_type_id = 167 AND user_type_id = 167 AND max_length = 100 AND is_output = 1)
                AND CHARINDEX(N'USES_KEYCODE = 1', @generatorNormalized) > 0
                AND CHARINDEX(N'THEN -103 ELSE 0 END', @generatorNormalized) > 0
                AND CHARINDEX(N'THEN -107 ELSE 0 END', @generatorNormalized) > 0
                AND CHARINDEX(N'@RESPONSE_CODE < 0', @generatorNormalized) > 0
                THEN 'PASS' ELSE 'FAIL' END,
           N'Requires generator parameters/output types and the checked-in keycoded-product/error-code logic.'
)
SELECT check_name,
       result,
       details,
      CASE WHEN MAX(CASE WHEN result = 'FAIL' THEN 1 ELSE 0 END) OVER () = 0 THEN 'PASS' ELSE 'FAIL' END AS automated_checks_result,
      CASE WHEN check_name = N'Fallback product exists and is keycode-enabled with SAEP family/category configuration'
          THEN 'REQUIRED: database owner must confirm approved QA fallback product'
          ELSE 'No separate product-owner confirmation flagged by this check' END AS owner_review
FROM Checks
ORDER BY CASE result WHEN 'FAIL' THEN 0 WHEN 'REVIEW' THEN 1 ELSE 2 END, check_name;
