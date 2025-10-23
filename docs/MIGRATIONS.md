# Database Migrations Guide

This guide explains how to create and run Entity Framework Core migrations for the PostgreSQL database.

## Prerequisites

1. **PostgreSQL running** via Docker Compose:
```bash
cd dependencies
docker-compose up -d postgres
```

2. **EF Core CLI tools** installed globally:
```bash
dotnet tool install --global dotnet-ef
# Or update existing:
dotnet tool update --global dotnet-ef
```

3. **Environment variable** for connection string:
```bash
export PostgresConnectionString="Host=localhost;Port=5432;Database=orchestrator;Username=orchestrator;Password=orchestrator_dev_password"
```

## Creating Migrations

### Initial Migration (First Time Only)

From the **Ai.Orchestrator.Services** directory:

```bash
cd Ai.Orchestrator.Services

dotnet ef migrations add InitialCreate \
  --context OrchestratorDbContext \
  --output-dir Data/Migrations \
  --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
```

This will create the initial migration with all entities:
- Users
- Roles
- Permissions
- UserRoles
- RolePermissions
- Agents
- PluginConfigurations
- AgentTools

### Subsequent Migrations

After making changes to entities or DbContext:

```bash
cd Ai.Orchestrator.Services

dotnet ef migrations add <MigrationName> \
  --context OrchestratorDbContext \
  --output-dir Data/Migrations \
  --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
```

Example migration names:
- `AddUserEmailIndex`
- `AddAgentEncryptedApiKey`
- `UpdatePluginConfigJsonType`

## Applying Migrations

### Apply to Database

```bash
cd Ai.Orchestrator.Services

dotnet ef database update \
  --context OrchestratorDbContext \
  --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
```

### Apply Specific Migration

```bash
dotnet ef database update <MigrationName> \
  --context OrchestratorDbContext \
  --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
```

### Revert to Previous Migration

```bash
dotnet ef database update <PreviousMigrationName> \
  --context OrchestratorDbContext \
  --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
```

### Revert All Migrations (Drop Database)

```bash
dotnet ef database update 0 \
  --context OrchestratorDbContext \
  --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
```

## Viewing Migrations

### List All Migrations

```bash
cd Ai.Orchestrator.Services

dotnet ef migrations list \
  --context OrchestratorDbContext \
  --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
```

### Generate SQL Script

```bash
dotnet ef migrations script \
  --context OrchestratorDbContext \
  --output migration.sql \
  --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
```

### Generate SQL for Specific Migration Range

```bash
dotnet ef migrations script <FromMigration> <ToMigration> \
  --context OrchestratorDbContext \
  --output migration.sql \
  --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
```

## Removing Migrations

### Remove Last Migration (if not applied)

```bash
cd Ai.Orchestrator.Services

dotnet ef migrations remove \
  --context OrchestratorDbContext \
  --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
```

**WARNING**: This only works if the migration hasn't been applied to the database.

## Verifying Database Schema

### Connect to PostgreSQL

```bash
psql -h localhost -p 5432 -U orchestrator -d orchestrator
```

### List All Tables

```sql
\dt
```

Expected tables:
- `Users`
- `Roles`
- `Permissions`
- `UserRoles`
- `RolePermissions`
- `Agents`
- `PluginConfigurations`
- `AgentTools`
- `__EFMigrationsHistory` (EF Core internal)

### Describe Table Structure

```sql
\d Users
\d Agents
\d PluginConfigurations
```

### Check Seeded Data

```sql
-- Check roles
SELECT * FROM "Roles";

-- Check permissions
SELECT * FROM "Permissions";

-- Check role-permission mappings
SELECT r."Name" as Role, p."Name" as Permission
FROM "RolePermissions" rp
JOIN "Roles" r ON rp."RoleId" = r."Id"
JOIN "Permissions" p ON rp."PermissionId" = p."Id"
ORDER BY r."Name", p."Name";

-- Check if admin user exists
SELECT "Id", "Username", "Email", "IsActive" FROM "Users";
```

## Troubleshooting

### Error: "No executable found matching command dotnet-ef"

**Solution**: Install EF Core CLI tools:
```bash
dotnet tool install --global dotnet-ef
```

### Error: "Your startup project doesn't reference Microsoft.EntityFrameworkCore.Design"

**Solution**: Ensure the design package is installed:
```bash
cd Ai.Orchestrator.Services
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### Error: "Unable to create an object of type 'OrchestratorDbContext'"

**Solution**: Check that:
1. `PostgresConnectionString` environment variable is set
2. `OrchestratorDbContextFactory` exists in `Ai.Orchestrator.Models/Data/`
3. PostgreSQL is running: `docker-compose ps postgres`

### Error: "Connection refused" or "Network unreachable"

**Solution**: Ensure PostgreSQL is running and accessible:
```bash
# Check PostgreSQL status
docker-compose ps postgres

# Check PostgreSQL logs
docker-compose logs postgres

# Test connection
psql -h localhost -p 5432 -U orchestrator -d orchestrator
```

### Error: "Database already exists"

**Solution**: Either:
1. Drop and recreate:
```bash
dotnet ef database drop --force
dotnet ef database update
```

Or via psql:
```sql
DROP DATABASE orchestrator;
CREATE DATABASE orchestrator;
```

### Error: "Migration has already been applied"

**Solution**: Check applied migrations:
```bash
dotnet ef migrations list
```

Then either:
- Remove the migration file if it hasn't been applied
- Create a new migration with a different name

## Best Practices

### 1. Always Review Generated Migrations

After creating a migration, review the generated code in:
`Ai.Orchestrator.Services/Data/Migrations/`

Ensure it matches your intentions.

### 2. Test Migrations on Development Database First

Never run migrations directly on production without testing.

### 3. Backup Before Migrations

```bash
# Backup database
docker exec postgres pg_dump -U orchestrator orchestrator > backup_before_migration.sql
```

### 4. Use Idempotent Scripts for Production

Generate SQL scripts and review before applying:
```bash
dotnet ef migrations script > production_migration.sql
# Review the SQL
cat production_migration.sql
# Apply manually in production
```

### 5. Version Control Migration Files

Always commit migration files to git:
```bash
git add Ai.Orchestrator.Services/Data/Migrations/
git commit -m "feat: add InitialCreate migration"
```

### 6. Don't Modify Applied Migrations

Once a migration is applied and committed, don't modify it. Create a new migration instead.

## Production Deployment

For production, use this workflow:

1. **Generate SQL script**:
```bash
dotnet ef migrations script --idempotent > production_migration.sql
```

2. **Review SQL script** thoroughly

3. **Backup production database**

4. **Apply during maintenance window**:
```bash
psql -h production-host -U orchestrator -d orchestrator < production_migration.sql
```

5. **Verify deployment**:
```sql
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;
```

## Automatic Migration on Startup

To apply migrations automatically when the application starts, add this to `Program.cs`:

```csharp
// Apply migrations on startup (development only!)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
    dbContext.Database.Migrate();
}
```

**WARNING**: Only use automatic migrations in development. In production, apply migrations manually during deployment.

## Quick Reference Commands

```bash
# Create initial migration
dotnet ef migrations add InitialCreate --context OrchestratorDbContext --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj

# Apply migrations
dotnet ef database update --context OrchestratorDbContext --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj

# List migrations
dotnet ef migrations list --context OrchestratorDbContext --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj

# Generate SQL script
dotnet ef migrations script --context OrchestratorDbContext --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj

# Remove last migration (if not applied)
dotnet ef migrations remove --context OrchestratorDbContext --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj

# Drop database
dotnet ef database drop --force --context OrchestratorDbContext --startup-project ../Ai.Orchestrator/Ai.Orchestrator.csproj
```

## Next Steps

After running migrations:

1. Verify database schema
2. Check seeded data (roles, permissions)
3. Run the application to seed admin user
4. Test user registration and login
5. Create test agents and plugin configurations
