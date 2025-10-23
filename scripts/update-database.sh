#!/bin/bash

# Script to apply EF Core migrations to the database
# Usage: ./scripts/update-database.sh [MigrationName]

set -e

MIGRATION_NAME=${1:-""}

echo "Applying database migrations"
echo "=========================================="

# Check if PostgreSQL is running
if ! docker ps | grep -q postgres; then
    echo "Error: PostgreSQL container is not running"
    echo "Start it with: cd dependencies && docker-compose up -d postgres"
    exit 1
fi

cd Ai.Orchestrator.Services

if [ -z "$MIGRATION_NAME" ]; then
    echo "Applying all pending migrations..."
    dotnet ef database update \
      --context OrchestratorDbContext \
      --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
else
    echo "Applying migrations up to: $MIGRATION_NAME"
    dotnet ef database update "$MIGRATION_NAME" \
      --context OrchestratorDbContext \
      --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
fi

echo ""
echo "Database updated successfully!"
echo ""
echo "To verify, connect to PostgreSQL:"
echo "  psql -h localhost -p 5432 -U orchestrator -d orchestrator"
