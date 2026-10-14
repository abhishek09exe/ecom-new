SET NOCOUNT ON;
GO
DECLARE @prepareProcedureId int = OBJECT_ID(N'dbo.usp_trial_prepare_license_service', N'P');
DECLARE @prepareDefinition nvarchar(max);
DECLARE @normalizedPrepareDefinition nvarchar(max);
IF @prepareProcedureId IS NULL
BEGIN
    DECLARE @prepareSql nvarchar(max) = N'
CREATE PROCEDURE dbo.usp_trial_prepare_license_service
    @trial_registration_id int,
    @keycode varchar(40) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @trial_registration_json nvarchar(max), @distribution_id int,
            @response_code int, @message varchar(100);
    SELECT @trial_registration_json = trial_registration_json
    FROM dbo.trial_registration_json WHERE trial_registration_id = @trial_registration_id;
    IF ISNULL(ISJSON(@trial_registration_json), 0) <> 1
        THROW 50001, ''Multi Trial requires valid persisted trial registration JSON.'', 1;
    IF @keycode IS NULL OR @keycode = ''''
    BEGIN
        SELECT @distribution_id = license_distribution_method_id
        FROM dbo.trial_registration WHERE trial_registration_id = @trial_registration_id;
        EXEC dbo.usp_license_generate_keycode_cart 152200130, @distribution_id,
            @keycode OUTPUT, @response_code OUTPUT, @message OUTPUT;
        IF ISNULL(@response_code, -200) <> 0
            THROW 50002, ''Legacy keycode generation failed; trial license preparation stopped.'', 1;
        SET @trial_registration_json = JSON_MODIFY(@trial_registration_json, ''$.ecom_create_license'', CAST(1 AS tinyint));
    END;
    IF @keycode IS NULL
        OR NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@keycode,
            CHAR(9), ''''), CHAR(10), ''''), CHAR(11), ''''), CHAR(12), ''''), CHAR(13), ''''))), '''') IS NULL
        THROW 50003, ''Trial keycode preparation did not produce a non-whitespace keycode.'', 1;
    SET @trial_registration_json = JSON_MODIFY(@trial_registration_json, ''$.keycode'', @keycode);
    UPDATE dbo.trial_registration_json SET trial_registration_json = @trial_registration_json
    WHERE trial_registration_id = @trial_registration_id;
    UPDATE dbo.trial_registration SET keycode = @keycode
    WHERE trial_registration_id = @trial_registration_id;
    SELECT @keycode AS keycode, @trial_registration_json AS service_json;
END;';
    EXEC sys.sp_executesql @prepareSql;
END
ELSE
BEGIN
    PRINT 'Existing trial preparation procedure found; verifying below.';
END;

SET @prepareProcedureId = OBJECT_ID(N'dbo.usp_trial_prepare_license_service', N'P');
IF @prepareProcedureId IS NULL
    THROW 50001, 'dbo.usp_trial_prepare_license_service was not created.', 1;
SET @prepareDefinition = OBJECT_DEFINITION(@prepareProcedureId);
IF @prepareDefinition IS NULL
    THROW 50001, 'Cannot read dbo.usp_trial_prepare_license_service definition.', 1;
IF (SELECT COUNT(*) FROM sys.parameters WHERE object_id = @prepareProcedureId AND parameter_id > 0) <> 2
    OR NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @prepareProcedureId
        AND name = N'@trial_registration_id' AND system_type_id = 56 AND user_type_id = 56)
    OR NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @prepareProcedureId
        AND name = N'@keycode' AND system_type_id = 167 AND user_type_id = 167 AND max_length = 40)
    THROW 50001, 'dbo.usp_trial_prepare_license_service has incompatible parameter metadata; review before deployment.', 1;

SET @normalizedPrepareDefinition = UPPER(REPLACE(REPLACE(REPLACE(@prepareDefinition, CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
WHILE CHARINDEX(N'  ', @normalizedPrepareDefinition) > 0
    SET @normalizedPrepareDefinition = REPLACE(@normalizedPrepareDefinition, N'  ', N' ');
SET @normalizedPrepareDefinition = @normalizedPrepareDefinition COLLATE Latin1_General_100_BIN2;

IF CHARINDEX(N'@TRIAL_REGISTRATION_ID INT, @KEYCODE VARCHAR(40) = NULL', @normalizedPrepareDefinition) = 0
    THROW 50001, 'dbo.usp_trial_prepare_license_service has an incompatible textual signature; review before deployment.', 1;

IF CHARINDEX(N'EXEC DBO.USP_LICENSE_GENERATE_KEYCODE_CART 152200130, @DISTRIBUTION_ID, @KEYCODE OUTPUT, @RESPONSE_CODE OUTPUT, @MESSAGE OUTPUT', @normalizedPrepareDefinition) = 0
    OR CHARINDEX(N'IF ISNULL(@RESPONSE_CODE, -200) <> 0', @normalizedPrepareDefinition) = 0
    OR CHARINDEX(N'IF @KEYCODE IS NULL OR @KEYCODE = ''''', @normalizedPrepareDefinition) = 0
    OR CHARINDEX(N'CHAR(9)', @normalizedPrepareDefinition) = 0
    OR CHARINDEX(N'CHAR(10)', @normalizedPrepareDefinition) = 0
    OR CHARINDEX(N'CHAR(11)', @normalizedPrepareDefinition) = 0
    OR CHARINDEX(N'CHAR(12)', @normalizedPrepareDefinition) = 0
    OR CHARINDEX(N'CHAR(13)', @normalizedPrepareDefinition) = 0
    OR CHARINDEX(N'NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@KEYCODE', @normalizedPrepareDefinition) = 0
    OR CHARINDEX(N'CHAR(13)', @normalizedPrepareDefinition) > CHARINDEX(N'$.KEYCODE', @normalizedPrepareDefinition)
    OR CHARINDEX(N'$.KEYCODE', @normalizedPrepareDefinition) = 0
    OR CHARINDEX(N'AS KEYCODE, @TRIAL_REGISTRATION_JSON AS SERVICE_JSON', @normalizedPrepareDefinition) = 0
    OR CHARINDEX(N'IF ISNULL(@RESPONSE_CODE, -200) <> 0', @normalizedPrepareDefinition)
        > CHARINDEX(N'$.ECOM_CREATE_LICENSE', @normalizedPrepareDefinition)
    OR CHARINDEX(N'CHAR(13)', @normalizedPrepareDefinition)
        > CHARINDEX(N'$.KEYCODE', @normalizedPrepareDefinition)
    THROW 50001, 'dbo.usp_trial_prepare_license_service is incomplete or incompatible; review before deployment.', 1;
PRINT 'Trial preparation procedure is present and verified.';
GO
DECLARE @trialProcedureId int = OBJECT_ID(N'dbo.usp_trial_insert_trial', N'P');
DECLARE @definition nvarchar(max);
DECLARE @normalizedDefinition nvarchar(max);
DECLARE @hasParameter bit = 0;
DECLARE @parameterIsInt bit = 0;
DECLARE @expectedDispatch nvarchar(max) = N'IF @staged_license_service_id IS NULL EXEC usp_license_service_insert_service @license_service_type_name, @trial_registration_json, @return_license_service_id ELSE BEGIN IF NOT EXISTS ( SELECT 1 FROM dbo.license_service s JOIN dbo.license_service_type t ON t.license_service_type_id = s.license_service_type_id JOIN dbo.license_service_status st ON st.license_service_status_id = s.license_service_status_id JOIN dbo.license_service_json j ON j.license_service_id = s.license_service_id WHERE s.license_service_id = @staged_license_service_id AND t.license_service_type_id = 20 AND t.license_service_type_name = ''license_create'' AND t.process_type = ''synchronous'' AND st.license_service_status_name COLLATE Latin1_General_100_CI_AS = ''Pending'' COLLATE Latin1_General_100_CI_AS AND DATALENGTH(j.license_service_json) = DATALENGTH(@trial_registration_json) AND j.license_service_json COLLATE Latin1_General_100_BIN2 = @trial_registration_json COLLATE Latin1_General_100_BIN2) THROW 50001, ''Staged service does not match the prepared trial registration JSON.'', 1; EXEC dbo.usp_license_service_insert_license @license_service_id = @staged_license_service_id; END';
IF @trialProcedureId IS NULL
    THROW 50001, 'Cannot find dbo.usp_trial_insert_trial.', 1;

SET @definition = OBJECT_DEFINITION(@trialProcedureId);
IF @definition IS NULL
    THROW 50001, 'Cannot read dbo.usp_trial_insert_trial definition.', 1;

SELECT @hasParameter = 1,
       @parameterIsInt = CASE WHEN system_type_id = 56 AND user_type_id = 56 THEN 1 ELSE 0 END
FROM sys.parameters
WHERE object_id = @trialProcedureId AND name = N'@staged_license_service_id';

SET @normalizedDefinition = UPPER(REPLACE(REPLACE(REPLACE(@definition, CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
WHILE CHARINDEX(N'  ', @normalizedDefinition) > 0
    SET @normalizedDefinition = REPLACE(@normalizedDefinition, N'  ', N' ');
SET @normalizedDefinition = @normalizedDefinition COLLATE Latin1_General_100_BIN2;

IF @hasParameter = 1
BEGIN
    IF @parameterIsInt = 0
            OR (SELECT COUNT(*) FROM sys.parameters WHERE object_id = @trialProcedureId AND parameter_id > 0) <> 4
        OR NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
            AND name = N'@trial_registration_id' AND system_type_id = 56 AND user_type_id = 56)
        OR NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
            AND name = N'@keycode' AND system_type_id = 167 AND user_type_id = 167 AND max_length = 40)
        OR NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
            AND name = N'@account_order_json' AND system_type_id = 167 AND user_type_id = 167 AND max_length = -1)
        OR CHARINDEX(N'@ACCOUNT_ORDER_JSON VARCHAR(MAX) = NULL', @normalizedDefinition) = 0
        THROW 50001, 'dbo.usp_trial_insert_trial has incompatible staged-service parameters.', 1;
    IF CHARINDEX(N'@STAGED_LICENSE_SERVICE_ID INT = NULL', @normalizedDefinition) = 0
        OR CHARINDEX(N'@ACCOUNT_ORDER_JSON VARCHAR(MAX) = NULL', @normalizedDefinition) = 0
        OR CHARINDEX(UPPER(@expectedDispatch), @normalizedDefinition) = 0
        OR CHARINDEX(UPPER(@expectedDispatch), @normalizedDefinition,
            CHARINDEX(UPPER(@expectedDispatch), @normalizedDefinition) + LEN(UPPER(@expectedDispatch))) > 0
        THROW 50001, 'dbo.usp_trial_insert_trial has an incomplete or incompatible staged-service patch.', 1;
    PRINT 'Staged license-service patch is already installed and verified.';
END
ELSE
BEGIN
    IF CHARINDEX(N'@STAGED_LICENSE_SERVICE_ID', @normalizedDefinition) > 0
        THROW 50001, 'dbo.usp_trial_insert_trial contains a partial staged-service patch; review before deployment.', 1;

    DECLARE @parameter nvarchar(200) = N'@account_order_json VARCHAR(MAX) = NULL';
    DECLARE @dispatch nvarchar(500) = N'EXEC usp_license_service_insert_service @license_service_type_name, @trial_registration_json, @return_license_service_id';
    DECLARE @parameterPosition int = CHARINDEX(@parameter, @definition COLLATE Latin1_General_100_BIN2);
    DECLARE @dispatchPosition int = CHARINDEX(@dispatch, @definition COLLATE Latin1_General_100_BIN2);
    IF (SELECT COUNT(*) FROM sys.parameters WHERE object_id = @trialProcedureId AND parameter_id > 0) <> 3
        OR NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
            AND name = N'@trial_registration_id' AND system_type_id = 56 AND user_type_id = 56)
        OR NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
            AND name = N'@keycode' AND system_type_id = 167 AND user_type_id = 167 AND max_length = 40)
        OR NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
            AND name = N'@account_order_json' AND system_type_id = 167 AND user_type_id = 167 AND max_length = -1)
        THROW 50001, 'Base dbo.usp_trial_insert_trial signature is incompatible; review before deployment.', 1;
    IF @parameterPosition = 0 OR @dispatchPosition = 0
        THROW 50001, 'Trial procedure differs from the reviewed source; review bridge anchors before deployment.', 1;
    IF CHARINDEX(@parameter, @definition COLLATE Latin1_General_100_BIN2, @parameterPosition + LEN(@parameter)) > 0
        OR CHARINDEX(@dispatch, @definition COLLATE Latin1_General_100_BIN2, @dispatchPosition + LEN(@dispatch)) > 0
        THROW 50001, 'Trial procedure patch anchors are ambiguous; review before deployment.', 1;

    SET @definition = REPLACE(@definition, @parameter,
        @parameter + N', @staged_license_service_id int = NULL');
    SET @definition = REPLACE(@definition, @dispatch, N'
    IF @staged_license_service_id IS NULL
        EXEC usp_license_service_insert_service @license_service_type_name, @trial_registration_json, @return_license_service_id
    ELSE
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM dbo.license_service s
            JOIN dbo.license_service_type t ON t.license_service_type_id = s.license_service_type_id
            JOIN dbo.license_service_status st ON st.license_service_status_id = s.license_service_status_id
            JOIN dbo.license_service_json j ON j.license_service_id = s.license_service_id
            WHERE s.license_service_id = @staged_license_service_id
              AND t.license_service_type_id = 20 AND t.license_service_type_name = ''license_create''
                            AND t.process_type = ''synchronous''
                            AND st.license_service_status_name COLLATE Latin1_General_100_CI_AS = ''Pending'' COLLATE Latin1_General_100_CI_AS
              AND DATALENGTH(j.license_service_json) = DATALENGTH(@trial_registration_json)
              AND j.license_service_json COLLATE Latin1_General_100_BIN2 = @trial_registration_json COLLATE Latin1_General_100_BIN2)
            THROW 50001, ''Staged service does not match the prepared trial registration JSON.'', 1;
        EXEC dbo.usp_license_service_insert_license @license_service_id = @staged_license_service_id;
    END');
    DECLARE @createPosition int = CHARINDEX('CREATE', UPPER(@definition));
    IF @createPosition = 0
        THROW 50001, 'Cannot locate procedure declaration.', 1;
    IF SUBSTRING(UPPER(@definition), @createPosition, 15) = 'CREATE OR ALTER'
        SET @definition = STUFF(@definition, @createPosition, 15, 'ALTER');
    ELSE
        SET @definition = STUFF(@definition, @createPosition, 6, 'ALTER');
    EXEC sys.sp_executesql @definition;

    SET @definition = OBJECT_DEFINITION(@trialProcedureId);
    SET @normalizedDefinition = UPPER(REPLACE(REPLACE(REPLACE(@definition, CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
    WHILE CHARINDEX(N'  ', @normalizedDefinition) > 0
        SET @normalizedDefinition = REPLACE(@normalizedDefinition, N'  ', N' ');
    SET @normalizedDefinition = @normalizedDefinition COLLATE Latin1_General_100_BIN2;
        IF (SELECT COUNT(*) FROM sys.parameters WHERE object_id = @trialProcedureId AND parameter_id > 0) <> 4
        OR NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
            AND name = N'@trial_registration_id' AND system_type_id = 56 AND user_type_id = 56)
        OR NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
            AND name = N'@keycode' AND system_type_id = 167 AND user_type_id = 167 AND max_length = 40)
        OR NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
            AND name = N'@account_order_json' AND system_type_id = 167 AND user_type_id = 167 AND max_length = -1)
        OR NOT EXISTS (SELECT 1 FROM sys.parameters WHERE object_id = @trialProcedureId
            AND name = N'@staged_license_service_id' AND system_type_id = 56 AND user_type_id = 56)
        OR CHARINDEX(N'@STAGED_LICENSE_SERVICE_ID INT = NULL', @normalizedDefinition) = 0
        OR CHARINDEX(N'@ACCOUNT_ORDER_JSON VARCHAR(MAX) = NULL', @normalizedDefinition) = 0
        OR CHARINDEX(UPPER(@expectedDispatch), @normalizedDefinition) = 0
        OR CHARINDEX(UPPER(@expectedDispatch), @normalizedDefinition,
            CHARINDEX(UPPER(@expectedDispatch), @normalizedDefinition) + LEN(UPPER(@expectedDispatch))) > 0
            OR CHARINDEX(UPPER(@dispatch), @normalizedDefinition,
                CHARINDEX(UPPER(@dispatch), @normalizedDefinition) + LEN(UPPER(@dispatch))) > 0
        OR CHARINDEX(UPPER(@dispatch), @normalizedDefinition,
            CHARINDEX(UPPER(@dispatch), @normalizedDefinition) + LEN(UPPER(@dispatch))) > 0
        THROW 50001, 'Staged license-service patch postcondition failed; review the procedure before use.', 1;
END;
GO