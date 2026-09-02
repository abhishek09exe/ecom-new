# Small image bundling the Project_Seed .sql scripts + sqlcmd, used by the
# mssql chart's seed Job to populate a fresh database. Built and used purely
# locally (Docker Desktop's Kubernetes sees locally-built images directly,
# no registry push required).
FROM mcr.microsoft.com/mssql-tools:latest

WORKDIR /seed
# Build context is the repo root (see build instructions), since the .sql
# files live in Project_Seed/ at the top level, not next to this Dockerfile.
# insertstatements.sql is intentionally left out - All_Tables_20_ROWS.sql is
# the scoped sample dataset we seed with instead. api_stored_procs.sql
# replaces allecom_stored_procs.sql - it contains only the procedures the
# API actually calls (see its header comment for details on why).
COPY Project_Seed/ecomtables.sql Project_Seed/All_Tables_20_ROWS.sql Project_Seed/api_stored_procs.sql /seed/
COPY deploy/charts/mssql/seed-image/run-seed.sh /seed/run-seed.sh

ENTRYPOINT ["/bin/bash", "/seed/run-seed.sh"]
