#!/bin/bash

# Script to create EF Core migrations
# Usage: ./scripts/create-migration.sh <MigrationName>

set -e

if [ -z "$1" ]; then
    echo "Error: Migration name required"
    echo "Usage: ./scripts/create-migration.sh <MigrationName>"
    echo "Example: ./scripts/create-migration.sh InitialCreate"
    exit 1
fi

MIGRATION_NAME=$1

echo "Creating migration: $MIGRATION_NAME"
echo "=========================================="

cd Ai.Orchestrator.Services

dotnet ef migrations add "$MIGRATION_NAME" \
  --context OrchestratorDbContext \
  --output-dir Data/Migrations \
  --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj

echo ""
echo "Migration created successfully!"
echo "Files created in: Ai.Orchestrator.Services/Data/Migrations/"
echo ""
echo "To apply this migration, run:"
echo "  ./scripts/update-database.sh"
