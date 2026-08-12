-- =============================================================================
-- EXPORT STORED PROCEDURES
-- =============================================================================
-- Purpose:
--   Extract all user stored procedures from the current database and output
--   their definitions as a script-like result (similar to ecomtables.sql).
--
-- How to use:
--   1) Set the target database in USE below.
--   2) Run the script.
--   3) Copy results from the first result set, or read message output.
--
-- Notes:
--   - Encrypted procedures cannot be scripted (definition is unavailable).
--   - PRINT output is chunked to avoid SQL Server message length limits.
-- =============================================================================

USE ecom_cart_dev;
GO

SET NOCOUNT ON;

DECLARE @SchemaFilter SYSNAME = NULL;   -- Example: 'dbo' (NULL = all schemas)
DECLARE @PrintToMessages BIT = 1;       -- 1 = also PRINT each procedure script

;WITH ProcedureDefinitions AS (
	SELECT
		s.name AS schema_name,
		p.name AS procedure_name,
		p.object_id,
		m.definition
	FROM sys.procedures AS p
	INNER JOIN sys.schemas AS s
		ON s.schema_id = p.schema_id
	LEFT JOIN sys.sql_modules AS m
		ON m.object_id = p.object_id
	WHERE p.is_ms_shipped = 0
	  AND (@SchemaFilter IS NULL OR s.name = @SchemaFilter)
)
SELECT
	'-- =============================================================================' + CHAR(13) + CHAR(10) +
	'-- PROCEDURE: [' + schema_name + '].[' + procedure_name + ']' + CHAR(13) + CHAR(10) +
	'-- =============================================================================' + CHAR(13) + CHAR(10) +
	ISNULL(definition, '-- Definition unavailable (possibly encrypted).') + CHAR(13) + CHAR(10) +
	'GO' + CHAR(13) + CHAR(10) AS procedure_script
FROM ProcedureDefinitions
ORDER BY schema_name, procedure_name;

IF @PrintToMessages = 1
BEGIN
	DECLARE @ProcScript NVARCHAR(MAX);
	DECLARE @Chunk NVARCHAR(4000);

	DECLARE procedure_cursor CURSOR FAST_FORWARD FOR
	SELECT
		'-- =============================================================================' + CHAR(13) + CHAR(10) +
		'-- PROCEDURE: [' + s.name + '].[' + p.name + ']' + CHAR(13) + CHAR(10) +
		'-- =============================================================================' + CHAR(13) + CHAR(10) +
		ISNULL(m.definition, '-- Definition unavailable (possibly encrypted).') + CHAR(13) + CHAR(10) +
		'GO' + CHAR(13) + CHAR(10)
	FROM sys.procedures AS p
	INNER JOIN sys.schemas AS s
		ON s.schema_id = p.schema_id
	LEFT JOIN sys.sql_modules AS m
		ON m.object_id = p.object_id
	WHERE p.is_ms_shipped = 0
	  AND (@SchemaFilter IS NULL OR s.name = @SchemaFilter)
	ORDER BY s.name, p.name;

	OPEN procedure_cursor;
	FETCH NEXT FROM procedure_cursor INTO @ProcScript;

	WHILE @@FETCH_STATUS = 0
	BEGIN
		WHILE LEN(@ProcScript) > 0
		BEGIN
			SET @Chunk = LEFT(@ProcScript, 4000);
			PRINT @Chunk;
			SET @ProcScript = SUBSTRING(@ProcScript, 4001, LEN(@ProcScript));
		END;

		FETCH NEXT FROM procedure_cursor INTO @ProcScript;
	END;

	CLOSE procedure_cursor;
	DEALLOCATE procedure_cursor;
END;
GO
