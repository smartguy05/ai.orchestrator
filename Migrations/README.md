# Database Migrations

This directory contains SQL migration scripts for the AI Orchestrator database schema.

## Migration Files

### 001_AddPerAgentConfigurationFields.sql
**Date:** 2024-10-24
**Status:** Pending

**Description:**
Adds per-agent configuration fields to the Agents table to support the new per-agent service architecture.

**Changes:**
- **ADD** `ConfirmationPlugin` (VARCHAR 200) - Plugin for confirmation requests
- **ADD** `ConfirmationExpirationMinutes` (INT, DEFAULT 30) - Confirmation timeout
- **ADD** `NotificationTimeoutHours` (INT, DEFAULT 24) - Notification timeout
- **ADD** `LoggingPlugins` (VARCHAR 1000) - Comma-separated logging plugin names
- **DROP** `ActivePlugins` - Deprecated field (replaced by PluginConfigurations table)

**Rollback:** Use `001_AddPerAgentConfigurationFields_Rollback.sql`

## How to Run Migrations

### Using Entity Framework Core (Recommended)

If you prefer to use EF Core migrations instead of manual SQL:

```bash
# Navigate to the Models project
cd Ai.Orchestrator.Models

# Create a new migration
dotnet ef migrations add AddPerAgentConfigurationFields --startup-project ../Ai.Orchestrator

# Apply the migration
dotnet ef database update --startup-project ../Ai.Orchestrator

# View migration SQL without applying
dotnet ef migrations script --startup-project ../Ai.Orchestrator
```

### Using Manual SQL Scripts

If you prefer to run the SQL scripts directly:

#### PostgreSQL (Production)

```bash
# Connect to your database
psql -h localhost -U your_username -d your_database_name

# Run the migration
\i Migrations/001_AddPerAgentConfigurationFields.sql

# Verify the changes
\d "Agents"
```

Or use a single command:
```bash
psql -h localhost -U your_username -d your_database_name -f Migrations/001_AddPerAgentConfigurationFields.sql
```

#### Docker/Compose Setup

If using Docker Compose:

```bash
# Copy the migration file into the container
docker cp Migrations/001_AddPerAgentConfigurationFields.sql postgres_container:/tmp/

# Execute the migration
docker exec -it postgres_container psql -U your_username -d your_database_name -f /tmp/001_AddPerAgentConfigurationFields.sql
```

### Environment Variables

Make sure your connection string is configured in one of these places:
- `launchSettings.json` - `PostgresConnectionString` environment variable
- Environment variable: `PostgresConnectionString`
- Default: `Host=localhost;Database=orchestrator;Username=postgres;Password=postgres`

## Rollback Instructions

If you need to rollback a migration:

```bash
# Using SQL script
psql -h localhost -U your_username -d your_database_name -f Migrations/001_AddPerAgentConfigurationFields_Rollback.sql

# Using EF Core
dotnet ef database update PreviousMigrationName --startup-project ../Ai.Orchestrator
```

## Verification

After running the migration, verify the changes:

```sql
-- Check that new columns exist
SELECT column_name, data_type, column_default
FROM information_schema.columns
WHERE table_name = 'Agents'
  AND column_name IN (
    'ConfirmationPlugin',
    'ConfirmationExpirationMinutes',
    'NotificationTimeoutHours',
    'LoggingPlugins'
  );

-- Verify ActivePlugins column is gone
SELECT column_name
FROM information_schema.columns
WHERE table_name = 'Agents'
  AND column_name = 'ActivePlugins';
-- Should return 0 rows

-- Check existing agent data
SELECT
    "Id",
    "Name",
    "ConfirmationPlugin",
    "ConfirmationExpirationMinutes",
    "NotificationTimeoutHours",
    "LoggingPlugins"
FROM "Agents";
```

## Migration History

| Migration | Date | Status | Description |
|-----------|------|--------|-------------|
| 001_AddPerAgentConfigurationFields | 2024-10-24 | Pending | Add per-agent config fields |

## Notes

- Always backup your database before running migrations
- Test migrations in a development environment first
- The `ActivePlugins` field has been deprecated and removed
- Plugin configuration now comes from the `PluginConfigurations` table
- Each agent can have different confirmation, logging, and notification settings
