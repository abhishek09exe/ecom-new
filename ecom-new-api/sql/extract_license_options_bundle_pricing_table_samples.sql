-- =============================================================================
-- Export top-N rows as INSERT statements for all user tables in the database.
-- =============================================================================
-- Why this script:
--   Use this when backup/restore is not possible and you need representative
--   pre-prod data copied into local.
--
-- What it does:
--   1) Builds a table list (all user tables by default).
--   2) Generates INSERT statements for TOP(@TopN) rows per table.
--
-- Output:
--   - Result set #1: resolved table list
--   - Result set #2: line-by-line runnable SQL script (copy and run in local DB)
--
-- Notes:
--   - Uses sys.sql_expression_dependencies (dynamic SQL refs may not resolve).
--   - Rowversion/timestamp columns are excluded (non-insertable).
--   - Identity columns are included with SET IDENTITY_INSERT ON/OFF per table.
-- =============================================================================

SET NOCOUNT ON;

DECLARE @TopN INT = 20;
DECLARE @UseAllTables BIT = 1;
DECLARE @MaxTextChars INT = 4000;

IF OBJECT_ID('tempdb..#TargetProcedures') IS NOT NULL DROP TABLE #TargetProcedures;
CREATE TABLE #TargetProcedures (
    procedure_schema SYSNAME NOT NULL,
    procedure_name SYSNAME NOT NULL,
    include_flag BIT NOT NULL
);

-- Core procedures for GET /license-options and GET /bundle-pricing.
INSERT INTO #TargetProcedures (procedure_schema, procedure_name, include_flag)
VALUES
    ('dbo', 'usp_cart_select_message_key', 1),
    ('dbo', 'usp_license_select_license_by_id', 1),
    ('dbo', 'usp_cart_select_license_profile', 1),
    ('dbo', 'usp_product_select_license_category_upgrade', 1),
    ('dbo', 'usp_cart_select_license_billing_model', 1),
    ('dbo', 'usp_cart_select_license_configurator_pricing', 1);

-- Optional side-path procedures used by message campaign/discount logic.
-- Set include_flag = 1 if you want these included too.
INSERT INTO #TargetProcedures (procedure_schema, procedure_name, include_flag)
VALUES
    ('dbo', 'usp_message_select_message_campaign_cart_discount', 0),
    ('dbo', 'usp_cart_select_new_product_discount', 0),
    ('dbo', 'usp_cart_select_cart_discount', 0),
    ('dbo', 'usp_cart_select_cart_discount_item', 0);

IF OBJECT_ID('tempdb..#SeedProcedures') IS NOT NULL DROP TABLE #SeedProcedures;
CREATE TABLE #SeedProcedures (
    root_object_id INT NOT NULL PRIMARY KEY,
    root_procedure NVARCHAR(300) NOT NULL
);

INSERT INTO #SeedProcedures (root_object_id, root_procedure)
SELECT
    p.object_id,
    QUOTENAME(s.name) + '.' + QUOTENAME(p.name) AS root_procedure
FROM #TargetProcedures tp
JOIN sys.schemas s
    ON s.name = tp.procedure_schema
JOIN sys.procedures p
    ON p.schema_id = s.schema_id
   AND p.name = tp.procedure_name
WHERE tp.include_flag = 1;

IF @UseAllTables = 0
AND NOT EXISTS (SELECT 1 FROM #SeedProcedures)
BEGIN
    RAISERROR('No target procedures found in current database.', 16, 1);
    RETURN;
END;

IF OBJECT_ID('tempdb..#DependencyWalk') IS NOT NULL DROP TABLE #DependencyWalk;
CREATE TABLE #DependencyWalk (
    root_object_id INT NOT NULL,
    referenced_id INT NOT NULL,
    depth INT NOT NULL
);

;WITH deps AS (
    SELECT
        sp.root_object_id,
        sed.referenced_id,
        1 AS depth
    FROM #SeedProcedures sp
    JOIN sys.sql_expression_dependencies sed
        ON sed.referencing_id = sp.root_object_id
    WHERE sed.referenced_id IS NOT NULL

    UNION ALL

    SELECT
        d.root_object_id,
        sed.referenced_id,
        d.depth + 1
    FROM deps d
    JOIN sys.sql_expression_dependencies sed
        ON sed.referencing_id = d.referenced_id
    WHERE sed.referenced_id IS NOT NULL
      AND d.depth < 12
)
INSERT INTO #DependencyWalk (root_object_id, referenced_id, depth)
SELECT DISTINCT root_object_id, referenced_id, depth
FROM deps
OPTION (MAXRECURSION 200);

IF OBJECT_ID('tempdb..#ResolvedObjects') IS NOT NULL DROP TABLE #ResolvedObjects;
CREATE TABLE #ResolvedObjects (
    root_procedure NVARCHAR(300) NOT NULL,
    object_schema SYSNAME NOT NULL,
    object_name SYSNAME NOT NULL,
    object_type CHAR(2) NOT NULL,
    object_type_desc NVARCHAR(60) NOT NULL,
    depth INT NOT NULL,
    object_id INT NOT NULL
);

INSERT INTO #ResolvedObjects (root_procedure, object_schema, object_name, object_type, object_type_desc, depth, object_id)
SELECT DISTINCT
    sp.root_procedure,
    s.name,
    o.name,
    o.type,
    o.type_desc,
    dw.depth,
    o.object_id
FROM #DependencyWalk dw
JOIN #SeedProcedures sp
    ON sp.root_object_id = dw.root_object_id
JOIN sys.objects o
    ON o.object_id = dw.referenced_id
JOIN sys.schemas s
    ON s.schema_id = o.schema_id;

IF OBJECT_ID('tempdb..#TargetTables') IS NOT NULL DROP TABLE #TargetTables;
CREATE TABLE #TargetTables (
    table_schema SYSNAME NOT NULL,
    table_name SYSNAME NOT NULL,
    object_id INT NOT NULL,
    PRIMARY KEY (table_schema, table_name)
);

IF @UseAllTables = 1
BEGIN
    INSERT INTO #TargetTables (table_schema, table_name, object_id)
    SELECT
        s.name,
        t.name,
        t.object_id
    FROM sys.tables t
    JOIN sys.schemas s
        ON s.schema_id = t.schema_id
    WHERE t.is_ms_shipped = 0;
END
ELSE
BEGIN
    INSERT INTO #TargetTables (table_schema, table_name, object_id)
    SELECT DISTINCT object_schema, object_name, object_id
    FROM #ResolvedObjects
    WHERE object_type = 'U';
END;

-- Table inventory to review what will be exported.
SELECT
    tt.table_schema,
    tt.table_name,
    SUM(ps.row_count) AS approx_row_count
FROM #TargetTables tt
LEFT JOIN sys.dm_db_partition_stats ps
    ON ps.object_id = tt.object_id
   AND ps.index_id IN (0, 1)
GROUP BY tt.table_schema, tt.table_name
ORDER BY tt.table_schema, tt.table_name;

IF OBJECT_ID('tempdb..#ExportLines') IS NOT NULL DROP TABLE #ExportLines;
CREATE TABLE #ExportLines (
    line_no INT IDENTITY(1,1) PRIMARY KEY,
    table_schema SYSNAME NULL,
    table_name SYSNAME NULL,
    line_text NVARCHAR(MAX) NOT NULL
);

INSERT INTO #ExportLines (line_text)
VALUES
    ('-- Generated at ' + CONVERT(VARCHAR(33), SYSDATETIME(), 126)),
    ('-- NOTE: Long text values are truncated to ' + CONVERT(VARCHAR(20), @MaxTextChars) + ' characters to avoid client/output truncation limits.'),
    ('SET NOCOUNT ON;'),
    ('SET XACT_ABORT ON;'),
    ('BEGIN TRAN;'),
    ('');

DECLARE @TableSchema SYSNAME;
DECLARE @TableName SYSNAME;
DECLARE @ObjectId INT;
DECLARE @QualifiedTable NVARCHAR(600);
DECLARE @ColumnList NVARCHAR(MAX);
DECLARE @ValueExpr NVARCHAR(MAX);
DECLARE @HasIdentity BIT;
DECLARE @Sql NVARCHAR(MAX);

DECLARE table_cursor CURSOR FAST_FORWARD FOR
SELECT table_schema, table_name, object_id
FROM #TargetTables
ORDER BY table_schema, table_name;

OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @TableSchema, @TableName, @ObjectId;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @QualifiedTable = QUOTENAME(@TableSchema) + N'.' + QUOTENAME(@TableName);

    SELECT @HasIdentity = CASE WHEN EXISTS (
        SELECT 1
        FROM sys.columns c
        WHERE c.object_id = @ObjectId
          AND c.is_identity = 1
          AND c.is_computed = 0
          AND c.system_type_id <> 189
    ) THEN 1 ELSE 0 END;

    SELECT @ColumnList = STUFF((
        SELECT N', ' + QUOTENAME(c.name)
        FROM sys.columns c
        WHERE c.object_id = @ObjectId
          AND c.is_computed = 0
          AND c.system_type_id <> 189
        ORDER BY c.column_id
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 2, '');

    SELECT @ValueExpr = STUFF((
        SELECT N' + '','' + ' +
            CASE
                WHEN t.name IN ('char','varchar','text','nchar','nvarchar','ntext','xml','uniqueidentifier') THEN
                    N'CASE WHEN src.' + QUOTENAME(c.name) + N' IS NULL THEN ''NULL'' ELSE CHAR(39) + REPLACE(LEFT(CONVERT(NVARCHAR(MAX), src.' + QUOTENAME(c.name) + N'), @pMaxTextChars), CHAR(39), CHAR(39) + CHAR(39)) + CHAR(39) END'
                WHEN t.name IN ('date','datetime','datetime2','smalldatetime','datetimeoffset','time') THEN
                    N'CASE WHEN src.' + QUOTENAME(c.name) + N' IS NULL THEN ''NULL'' ELSE CHAR(39) + CONVERT(VARCHAR(33), src.' + QUOTENAME(c.name) + N', 126) + CHAR(39) END'
                WHEN t.name IN ('binary','varbinary','image') THEN
                    N'CASE WHEN src.' + QUOTENAME(c.name) + N' IS NULL THEN ''NULL'' ELSE master.sys.fn_varbintohexstr(src.' + QUOTENAME(c.name) + N') END'
                WHEN t.name = 'bit' THEN
                    N'CASE WHEN src.' + QUOTENAME(c.name) + N' IS NULL THEN ''NULL'' WHEN src.' + QUOTENAME(c.name) + N' = 1 THEN ''1'' ELSE ''0'' END'
                ELSE
                    N'CASE WHEN src.' + QUOTENAME(c.name) + N' IS NULL THEN ''NULL'' ELSE CONVERT(VARCHAR(MAX), src.' + QUOTENAME(c.name) + N') END'
            END
        FROM sys.columns c
        JOIN sys.types t
          ON c.user_type_id = t.user_type_id
        WHERE c.object_id = @ObjectId
          AND c.is_computed = 0
          AND c.system_type_id <> 189
        ORDER BY c.column_id
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 8, '');

    INSERT INTO #ExportLines (table_schema, table_name, line_text)
    VALUES (@TableSchema, @TableName, N'-- ' + @QualifiedTable);

    IF @HasIdentity = 1
    BEGIN
        INSERT INTO #ExportLines (table_schema, table_name, line_text)
        VALUES (@TableSchema, @TableName, N'SET IDENTITY_INSERT ' + @QualifiedTable + N' ON;');
    END;

    SET @Sql = N'
INSERT INTO #ExportLines (table_schema, table_name, line_text)
SELECT
    @pSchema,
    @pTable,
    CONCAT(''INSERT INTO ' + @QualifiedTable + N' (' + @ColumnList + N') VALUES ('', ' + @ValueExpr + N', '');'')
FROM (
    SELECT TOP (@pTopN) *
    FROM ' + @QualifiedTable + N'
    ORDER BY (SELECT NULL)
) AS src;';

    EXEC sp_executesql
        @Sql,
        N'@pSchema SYSNAME, @pTable SYSNAME, @pTopN INT, @pMaxTextChars INT',
        @pSchema = @TableSchema,
        @pTable = @TableName,
        @pTopN = @TopN,
        @pMaxTextChars = @MaxTextChars;

    IF @HasIdentity = 1
    BEGIN
        INSERT INTO #ExportLines (table_schema, table_name, line_text)
        VALUES (@TableSchema, @TableName, N'SET IDENTITY_INSERT ' + @QualifiedTable + N' OFF;');
    END;

    INSERT INTO #ExportLines (table_schema, table_name, line_text)
    VALUES (@TableSchema, @TableName, N'');

    FETCH NEXT FROM table_cursor INTO @TableSchema, @TableName, @ObjectId;
END

CLOSE table_cursor;
DEALLOCATE table_cursor;

INSERT INTO #ExportLines (line_text)
VALUES
    ('COMMIT;'),
    ('-- END GENERATED SCRIPT');

-- Copy this result set and run it in your local DB.
SELECT line_no, line_text
FROM #ExportLines
ORDER BY line_no;

-- Unresolved direct references (for visibility only when dependency mode is used).
IF @UseAllTables = 0
BEGIN
    SELECT DISTINCT
        sp.root_procedure,
        sed.referenced_server_name,
        sed.referenced_database_name,
        sed.referenced_schema_name,
        sed.referenced_entity_name
    FROM #SeedProcedures sp
    JOIN sys.sql_expression_dependencies sed
        ON sed.referencing_id = sp.root_object_id
    WHERE sed.referenced_id IS NULL
    ORDER BY sp.root_procedure, sed.referenced_schema_name, sed.referenced_entity_name;
END;
