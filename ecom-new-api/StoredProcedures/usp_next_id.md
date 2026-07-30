USE [ecommerce_VH14]
GO

/****** Object:  StoredProcedure [dbo].[usp_next_id]    Script Date: 29-07-2026 17:47:04 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER PROC [dbo].[usp_next_id] @Type int
WITH EXECUTE AS CALLER
AS
BEGIN
	declare @sqlStmt varchar(1024)
	declare @sequenceName varchar(1024)
	declare @activated int

	if ( @Type <1) OR (@Type > 12) OR (@Type in (1, 2, 12))
	begin
		select -1
		return
	end
	--------------------------------------------------------
	-- Table schema 'dbo' should not be added 
	-- Check against information schema will fail.
	--------------------------------------------------------
	set @sequenceName = CASE @Type
				WHEN 3 THEN 'Invoices'
				WHEN 4 THEN 'Affiliate_Assigned_Number'
				WHEN 5 THEN 'Prices'
				WHEN 6 THEN 'Price_Dates'
				WHEN 7 THEN 'Authorizations'
				WHEN 8 THEN 'Commissions'
				WHEN 9 THEN 'Comm_Dates'
				WHEN 10 THEN 'Groups'
				WHEN 11 THEN 'UPDATE_Requests'
			    END + '_sequence'
	
	set nocount on
	begin transaction
	if  not exists( select 1 from information_schema.tables
			where table_type= 'BASE TABLE'
			and table_name= @sequenceName)
	begin
		UPDATE ids SET 
			next_id = next_id + 1, 
			last_modified = GetDate()
		WHERE id_type = @Type

		SELECT next_id
		FROM ids
		WHERE id_type = @Type
	end
	else begin
		create table #rows (
			rows int
		)
		exec ('insert into #rows select top 1 SEQUENCE_ID from '+ @sequenceName)
		select @activated = rows from #rows
		drop table #rows

		if (@activated is NULL)
		begin
			set nocount on
			UPDATE ids SET 
				next_id = next_id + 1, 
				last_modified = GetDate()
			WHERE id_type = @Type

			SELECT next_id
			FROM ids
			WHERE id_type = @Type
		end
		else begin
			set 	@sqlStmt = 'insert into ' + 
				@sequenceName + 
				' (insert_date) values (getdate())
				select scope_identity() as next_id'
			exec (@sqlStmt)

		end
	end
	set nocount off
	Commit transaction
END
GO


