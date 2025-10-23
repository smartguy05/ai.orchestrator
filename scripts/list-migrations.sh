#!/bin/bash

# Script to list all EF Core migrations
# Usage: ./scripts/list-migrations.sh

set -e

echo "Listing all migrations"
echo "=========================================="

cd Ai.Orchestrator.Services

dotnet ef migrations list \
  --context OrchestratorDbContext \
  --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
