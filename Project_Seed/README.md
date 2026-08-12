# Project Seed Assets

This folder contains SQL exports and helper scripts used for migration analysis and local data inspection.

## File map

- `ecomtables.sql`: schema-oriented table definitions/export.
- `allecom_stored_procs.sql`: stored procedure export.
- `All_Tables_20_ROWS.sql`: sample data dump (up to 20 rows per table).
- `insertstatements.sql`: insert statement export.
- `extract_all_stored_procedures.sql`: helper query/script to extract procedures.
- `extract_license_options_bundle_pricing_table_samples.sql`: helper query/script for targeted sample extraction.
- `TABLES_REFERENCE.md`: table usage mapping per major stored procedure.

## Notes

- Keep large SQL exports as source-of-truth snapshots.
- Use targeted extraction scripts when validating specific endpoints.
