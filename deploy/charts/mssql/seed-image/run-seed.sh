#!/bin/bash
set -euo pipefail

export PATH="/opt/mssql-tools/bin:$PATH"

# Required env vars: MSSQL_HOST, SA_PASSWORD, TARGET_DB
: "${MSSQL_HOST:?MSSQL_HOST is required}"
: "${SA_PASSWORD:?SA_PASSWORD is required}"
: "${TARGET_DB:?TARGET_DB is required}"

run_sqlcmd() {
  # -b makes sqlcmd exit non-zero on any batch error, so `set -e` catches it.
  # -x disables sqlcmd's $(var) substitution syntax, since some legacy data
  # values contain literal "$(" sequences that would otherwise be misparsed.
  sqlcmd -S "$MSSQL_HOST" -U sa -P "$SA_PASSWORD" -C -b -x "$@"
}

echo "Checking whether database '$TARGET_DB' already exists..."
EXISTS=$(run_sqlcmd -h -1 \
  -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = '$TARGET_DB'" | tr -d '[:space:]')

if [ "$EXISTS" = "1" ]; then
  echo "Database '$TARGET_DB' already exists, skipping seed (idempotent)."
  exit 0
fi

echo "Creating database '$TARGET_DB'..."
run_sqlcmd -Q "CREATE DATABASE [$TARGET_DB]"

echo "Applying schema (ecomtables.sql)..."
run_sqlcmd -d "$TARGET_DB" -i /seed/ecomtables.sql

echo "Applying stored procedures (api_stored_procs.sql)..."
run_sqlcmd -d "$TARGET_DB" -i /seed/api_stored_procs.sql

echo "Loading sample data (All_Tables_20_ROWS.sql)..."
run_sqlcmd -d "$TARGET_DB" -i /seed/All_Tables_20_ROWS.sql

echo "Seed complete."
