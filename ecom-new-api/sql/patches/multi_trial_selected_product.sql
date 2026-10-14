DECLARE @originalNoCount bit = CASE WHEN (@@OPTIONS & 512) = 512 THEN 1 ELSE 0 END;
DECLARE @originalXactAbort bit = CASE WHEN (@@OPTIONS & 16384) = 16384 THEN 1 ELSE 0 END;

IF DB_NAME() COLLATE Latin1_General_100_BIN2 <> N'ecommerce_VH14'
    OR CONVERT(nvarchar(128), @@SERVERNAME) COLLATE Latin1_General_100_BIN2 <> N'QADENECOM6'
BEGIN
    ;THROW 50001, 'This patch is restricted to QADENECOM6.ecommerce_VH14.', 1;
END;

IF @@TRANCOUNT <> 0
BEGIN
    ;THROW 50001, 'Run this patch without an ambient transaction.', 1;
END;

DECLARE @procedureId int = OBJECT_ID(N'dbo.usp_trial_insert_trial_registration', N'P');
DECLARE @definition nvarchar(max);
DECLARE @normalizedDefinition nvarchar(max);
DECLARE @originalUsesAnsiNulls bit;
DECLARE @originalUsesQuotedIdentifier bit;
DECLARE @hasParameter bit = 0;
DECLARE @parameterIsInt bit = 0;
DECLARE @parameterDeclaration nvarchar(100) = N'@SELECTED_PRODUCT_ID INT = NULL';
DECLARE @originalParameterTail nvarchar(200) = N'@TRIAL_REGISTRATION_JSON VARCHAR(MAX) = NULL, @OPT_IN INT = NULL';
DECLARE @patchedParameterTail nvarchar(240) = N'@TRIAL_REGISTRATION_JSON VARCHAR(MAX) = NULL, @OPT_IN INT = NULL, @SELECTED_PRODUCT_ID INT = NULL';
DECLARE @partnerLookup nvarchar(200) = N'WHERE PP.PARTNER_PRODUCT_ID = @PARTNER_PRODUCT_ID AND P.PARTNER_KEY = @PARTNER_KEY';
DECLARE @fallbackLogic nvarchar(150) = N'IF @PRODUCT_ID IS NULL AND @SELECTED_PRODUCT_ID IS NOT NULL SELECT @PRODUCT_ID = @SELECTED_PRODUCT_ID';
DECLARE @productSelectionBranch nvarchar(100) = N'IF @PRODUCT_ID IS NULL BEGIN';
DECLARE @parameterPosition int;
DECLARE @selectionPosition int;
DECLARE @partnerLookupPosition int;
DECLARE @fallbackPosition int;
DECLARE @productSelectionPosition int;
DECLARE @originalParameterCount int;
DECLARE @trialRegistrationJsonOrdinal int;
DECLARE @optInParameterOrdinal int;
DECLARE @selectedParameterOrdinal int;
DECLARE @expectedParameters TABLE
(
    parameter_id int NOT NULL PRIMARY KEY,
    parameter_name sysname NOT NULL,
    system_type_id int NOT NULL,
    max_length smallint NOT NULL
);

INSERT INTO @expectedParameters (parameter_id, parameter_name, system_type_id, max_length)
VALUES
    (1, N'@first_name', 231, 450),
    (2, N'@last_name', 231, 450),
    (3, N'@company_name', 231, 510),
    (4, N'@company_type_name', 167, 50),
    (5, N'@phone_number', 167, 64),
    (6, N'@customer_email', 167, 100),
    (7, N'@address_1', 231, 510),
    (8, N'@address_2', 231, 510),
    (9, N'@city', 231, 260),
    (10, N'@state', 231, 4),
    (11, N'@postal_code', 231, 64),
    (12, N'@country', 167, 75),
    (13, N'@language_code', 167, 2),
    (14, N'@location_code', 167, 3),
    (15, N'@license_category_name', 167, 10),
    (16, N'@license_seats', 56, 4),
    (17, N'@years', 62, 8),
    (18, N'@trial_days', 56, 4),
    (19, N'@license_keycode_type_id', 56, 4),
    (20, N'@partner_key', 167, 36),
    (21, N'@account_user_name', 167, 100),
    (22, N'@partner_account_code', 167, 50),
    (23, N'@sfdc_lead_id', 167, 18),
    (24, N'@sfdc_opportunity_id', 167, 18),
    (25, N'@sfdc_trial_id', 167, 18),
    (26, N'@salesforce_campaign_id', 167, 18),
    (27, N'@partner_product_id', 167, 20),
    (28, N'@sfdc_distributor_id', 167, 18),
    (29, N'@sfdc_reseller_id', 167, 18),
    (30, N'@eloqua_parameters', 231, -1),
    (31, N'@license_distribution_method_code', 175, 4),
    (32, N'@salesforce_license_id', 167, 18),
    (33, N'@trial_registration_json', 167, -1),
    (34, N'@opt_in', 56, 4),
    (35, N'@selected_product_id', 56, 4);

BEGIN TRY
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    IF @procedureId IS NULL
    BEGIN
        ;THROW 50001, 'Cannot find dbo.usp_trial_insert_trial_registration.', 1;
    END;

    SET @definition = OBJECT_DEFINITION(@procedureId);
    IF @definition IS NULL
    BEGIN
        ;THROW 50001, 'Cannot read dbo.usp_trial_insert_trial_registration definition.', 1;
    END;

    SELECT @originalUsesAnsiNulls = uses_ansi_nulls,
           @originalUsesQuotedIdentifier = uses_quoted_identifier
    FROM sys.sql_modules
    WHERE object_id = @procedureId;

    IF @originalUsesAnsiNulls IS NULL OR @originalUsesQuotedIdentifier IS NULL
    BEGIN
        ;THROW 50001, 'Cannot read the registration procedure SET options.', 1;
    END;

SELECT @hasParameter = 1,
       @parameterIsInt = CASE WHEN system_type_id = 56 AND user_type_id = 56 THEN 1 ELSE 0 END
FROM sys.parameters
WHERE object_id = @procedureId AND name = N'@selected_product_id';

SELECT @originalParameterCount = COUNT(*)
FROM sys.parameters
WHERE object_id = @procedureId AND parameter_id > 0;

SELECT @optInParameterOrdinal = parameter_id
FROM sys.parameters
WHERE object_id = @procedureId AND name = N'@opt_in';

SELECT @trialRegistrationJsonOrdinal = parameter_id
FROM sys.parameters
WHERE object_id = @procedureId AND name = N'@trial_registration_json';

IF @optInParameterOrdinal IS NULL OR @trialRegistrationJsonOrdinal IS NULL
BEGIN
    ;THROW 50001, 'Cannot verify the registration procedure parameter list.', 1;
END;

SET @normalizedDefinition = UPPER(REPLACE(REPLACE(REPLACE(@definition, CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
WHILE CHARINDEX(N'  ', @normalizedDefinition) > 0
    SET @normalizedDefinition = REPLACE(@normalizedDefinition, N'  ', N' ');
SET @normalizedDefinition = @normalizedDefinition COLLATE Latin1_General_100_BIN2;

SET @partnerLookupPosition = CHARINDEX(@partnerLookup, @normalizedDefinition);
SET @fallbackPosition = CHARINDEX(@fallbackLogic, @normalizedDefinition);
SET @productSelectionPosition = CHARINDEX(@productSelectionBranch, @normalizedDefinition);

IF @hasParameter = 1
BEGIN
    IF @parameterIsInt = 0
    BEGIN
        ;THROW 50001, 'dbo.usp_trial_insert_trial_registration.@selected_product_id exists with a non-int SQL type.', 1;
    END;
    SELECT @selectedParameterOrdinal = parameter_id
    FROM sys.parameters
    WHERE object_id = @procedureId AND name = N'@selected_product_id';
    SET @parameterPosition = CHARINDEX(@patchedParameterTail, @normalizedDefinition);
    IF CHARINDEX(@parameterDeclaration, @normalizedDefinition) = 0
        OR CHARINDEX(@parameterDeclaration, @normalizedDefinition, CHARINDEX(@parameterDeclaration, @normalizedDefinition) + LEN(@parameterDeclaration)) > 0
        OR @parameterPosition = 0
        OR CHARINDEX(@patchedParameterTail, @normalizedDefinition, @parameterPosition + LEN(@patchedParameterTail)) > 0
        OR @partnerLookupPosition = 0
        OR CHARINDEX(@partnerLookup, @normalizedDefinition, @partnerLookupPosition + LEN(@partnerLookup)) > 0
        OR @fallbackPosition = 0
        OR CHARINDEX(@fallbackLogic, @normalizedDefinition, @fallbackPosition + LEN(@fallbackLogic)) > 0
        OR @productSelectionPosition = 0
        OR CHARINDEX(@productSelectionBranch, @normalizedDefinition, @productSelectionPosition + LEN(@productSelectionBranch)) > 0
        OR @fallbackPosition <= @partnerLookupPosition
        OR @fallbackPosition >= @productSelectionPosition
        OR @originalParameterCount <> 35
        OR @trialRegistrationJsonOrdinal <> 33
        OR @optInParameterOrdinal <> 34
        OR @selectedParameterOrdinal <> @optInParameterOrdinal + 1
        OR @selectedParameterOrdinal <> 35
        OR EXISTS (
            SELECT 1
            FROM @expectedParameters AS expected
            LEFT JOIN sys.parameters AS actual
            ON actual.object_id = @procedureId
               AND actual.parameter_id = expected.parameter_id
            WHERE expected.parameter_id <= 35
              AND (actual.parameter_id IS NULL
               OR actual.name <> expected.parameter_name
               OR actual.system_type_id <> expected.system_type_id
               OR actual.max_length <> expected.max_length))
    BEGIN
        ;THROW 50001, 'dbo.usp_trial_insert_trial_registration has an incomplete or incompatible selected-product patch.', 1;
    END;

    PRINT 'Selected-product patch is already installed and verified.';
END
ELSE
BEGIN
    IF CHARINDEX(N'@SELECTED_PRODUCT_ID', @normalizedDefinition) > 0
    BEGIN
        ;THROW 50001, 'dbo.usp_trial_insert_trial_registration contains a partial selected-product patch; review before deployment.', 1;
    END;

    DECLARE @parameterAnchor nvarchar(100) = N'@opt_in int = null';
    DECLARE @selectionAnchor nvarchar(200) = N'where pp.partner_product_id = @partner_product_id and p.partner_key = @partner_key';
    SET @parameterPosition = CHARINDEX(@parameterAnchor, @definition COLLATE Latin1_General_100_BIN2);
    SET @selectionPosition = CHARINDEX(@selectionAnchor, @definition COLLATE Latin1_General_100_BIN2);

    IF @parameterPosition = 0 OR @selectionPosition = 0 OR @partnerLookupPosition = 0 OR @productSelectionPosition = 0
        OR @originalParameterCount <> 34
        OR @trialRegistrationJsonOrdinal <> 33
        OR @optInParameterOrdinal <> 34
        OR EXISTS (
            SELECT 1
            FROM @expectedParameters AS expected
            LEFT JOIN sys.parameters AS actual
                ON actual.object_id = @procedureId
               AND actual.parameter_id = expected.parameter_id
            WHERE expected.parameter_id <= 34
              AND (actual.parameter_id IS NULL
                   OR actual.name <> expected.parameter_name
                   OR actual.system_type_id <> expected.system_type_id
                   OR actual.max_length <> expected.max_length))
        OR NOT EXISTS (
            SELECT 1 FROM sys.parameters
            WHERE object_id = @procedureId AND name = N'@trial_registration_json'
              AND parameter_id = 33 AND system_type_id = 167 AND max_length = -1)
        OR NOT EXISTS (
            SELECT 1 FROM sys.parameters
            WHERE object_id = @procedureId AND name = N'@opt_in'
              AND parameter_id = 34 AND system_type_id = 56)
        OR CHARINDEX(@originalParameterTail, @normalizedDefinition) = 0
        OR CHARINDEX(@originalParameterTail, @normalizedDefinition,
            CHARINDEX(@originalParameterTail, @normalizedDefinition) + LEN(@originalParameterTail)) > 0
        OR CHARINDEX(@selectionAnchor, @definition COLLATE Latin1_General_100_BIN2,
            @selectionPosition + LEN(@selectionAnchor)) > 0
        OR CHARINDEX(@productSelectionBranch, @normalizedDefinition,
            @productSelectionPosition + LEN(@productSelectionBranch)) > 0
        OR @partnerLookupPosition >= @productSelectionPosition
    BEGIN
        ;THROW 50001, 'Registration procedure differs from the reviewed source; review patch anchors before deployment.', 1;
    END;
    IF CHARINDEX(@parameterAnchor, @definition COLLATE Latin1_General_100_BIN2,
            @parameterPosition + LEN(@parameterAnchor)) > 0
        OR CHARINDEX(@selectionAnchor, @definition COLLATE Latin1_General_100_BIN2,
            @selectionPosition + LEN(@selectionAnchor)) > 0
    BEGIN
        ;THROW 50001, 'Registration procedure patch anchors are ambiguous; review before deployment.', 1;
    END;

    SET @definition = REPLACE(@definition, @parameterAnchor,
        @parameterAnchor + N', @selected_product_id int = null');
    SET @definition = REPLACE(@definition, @selectionAnchor,
        @selectionAnchor + N'
    if @product_id is null and @selected_product_id is not null
        select @product_id = @selected_product_id
');

    DECLARE @declarationPosition int = CHARINDEX(N'CREATE OR ALTER PROCEDURE', UPPER(@definition));
    IF @declarationPosition > 0
        SET @definition = STUFF(@definition, @declarationPosition, LEN(N'CREATE OR ALTER'), N'ALTER');
    ELSE
    BEGIN
        SET @declarationPosition = CHARINDEX(N'CREATE PROCEDURE', UPPER(@definition));
        IF @declarationPosition > 0
            SET @definition = STUFF(@definition, @declarationPosition, LEN(N'CREATE'), N'ALTER');
        ELSE IF CHARINDEX(N'ALTER PROCEDURE', UPPER(@definition)) = 0
        BEGIN
            ;THROW 50001, 'Cannot locate the reviewed registration procedure declaration.', 1;
        END;
    END;

    DECLARE @alterBatch nvarchar(max) =
        N'SET ANSI_NULLS ' + CASE WHEN @originalUsesAnsiNulls = 1 THEN N'ON' ELSE N'OFF' END + N'; ' +
        N'SET QUOTED_IDENTIFIER ' + CASE WHEN @originalUsesQuotedIdentifier = 1 THEN N'ON' ELSE N'OFF' END + N'; ' +
        N'EXEC sys.sp_executesql @procedureDefinition;';
    EXEC sys.sp_executesql
        @alterBatch,
        N'@procedureDefinition nvarchar(max)',
        @procedureDefinition = @definition;
END;

SET @definition = OBJECT_DEFINITION(@procedureId);
IF @definition IS NULL
BEGIN
    ;THROW 50001, 'Cannot read the altered registration procedure definition.', 1;
END;
SET @normalizedDefinition = UPPER(REPLACE(REPLACE(REPLACE(@definition, CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
WHILE CHARINDEX(N'  ', @normalizedDefinition) > 0
    SET @normalizedDefinition = REPLACE(@normalizedDefinition, N'  ', N' ');
SET @normalizedDefinition = @normalizedDefinition COLLATE Latin1_General_100_BIN2;

SET @parameterPosition = CHARINDEX(@patchedParameterTail, @normalizedDefinition);
SET @partnerLookupPosition = CHARINDEX(@partnerLookup, @normalizedDefinition);
SET @fallbackPosition = CHARINDEX(@fallbackLogic, @normalizedDefinition);
SET @productSelectionPosition = CHARINDEX(@productSelectionBranch, @normalizedDefinition);

IF NOT EXISTS (
    SELECT 1 FROM sys.parameters
    WHERE object_id = @procedureId AND name = N'@selected_product_id'
      AND system_type_id = 56 AND user_type_id = 56)
    OR @parameterPosition = 0
    OR CHARINDEX(@patchedParameterTail, @normalizedDefinition,
        @parameterPosition + LEN(@patchedParameterTail)) > 0
    OR @partnerLookupPosition = 0
    OR CHARINDEX(@partnerLookup, @normalizedDefinition,
        @partnerLookupPosition + LEN(@partnerLookup)) > 0
    OR @fallbackPosition = 0
    OR CHARINDEX(@fallbackLogic, @normalizedDefinition,
        @fallbackPosition + LEN(@fallbackLogic)) > 0
    OR @productSelectionPosition = 0
    OR CHARINDEX(@productSelectionBranch, @normalizedDefinition,
        @productSelectionPosition + LEN(@productSelectionBranch)) > 0
    OR @fallbackPosition <= @partnerLookupPosition
    OR @fallbackPosition >= @productSelectionPosition
    OR (SELECT COUNT(*) FROM sys.parameters
        WHERE object_id = @procedureId AND parameter_id > 0) <> 35
    OR NOT EXISTS (
        SELECT 1 FROM sys.parameters
        WHERE object_id = @procedureId AND name = N'@trial_registration_json'
          AND parameter_id = 33 AND system_type_id = 167 AND max_length = -1)
    OR NOT EXISTS (
        SELECT 1 FROM sys.parameters
        WHERE object_id = @procedureId AND name = N'@opt_in'
          AND parameter_id = 34 AND system_type_id = 56)
    OR NOT EXISTS (
        SELECT 1
        FROM sys.parameters AS selectedParameter
        JOIN sys.parameters AS optInParameter
            ON optInParameter.object_id = selectedParameter.object_id
           AND optInParameter.parameter_id + 1 = selectedParameter.parameter_id
        WHERE selectedParameter.object_id = @procedureId
          AND selectedParameter.name = N'@selected_product_id'
                    AND selectedParameter.parameter_id = 35
                    AND selectedParameter.system_type_id = 56
          AND optInParameter.name = N'@opt_in')
        OR EXISTS (
                SELECT 1
                FROM @expectedParameters AS expected
                LEFT JOIN sys.parameters AS actual
                        ON actual.object_id = @procedureId
                     AND actual.parameter_id = expected.parameter_id
                WHERE expected.parameter_id <= 35
                    AND (actual.parameter_id IS NULL
                             OR actual.name <> expected.parameter_name
                             OR actual.system_type_id <> expected.system_type_id
                             OR actual.max_length <> expected.max_length))
BEGIN
    ;THROW 50001, 'Selected-product patch postcondition failed; review the procedure before use.', 1;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.sql_modules
    WHERE object_id = @procedureId
      AND uses_ansi_nulls = @originalUsesAnsiNulls
      AND uses_quoted_identifier = @originalUsesQuotedIdentifier)
BEGIN
    ;THROW 50001, 'Registration procedure ANSI_NULLS or QUOTED_IDENTIFIER settings changed.', 1;
END;

COMMIT TRANSACTION;
IF @originalXactAbort = 0
    SET XACT_ABORT OFF;
IF @originalNoCount = 0
    SET NOCOUNT OFF;

PRINT 'Selected-product patch installed and verified.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    IF @originalXactAbort = 0
        SET XACT_ABORT OFF;
    IF @originalNoCount = 0
        SET NOCOUNT OFF;

    ;THROW;
END CATCH;
GO