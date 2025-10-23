# Database Schema Design - User Configuration System

## Overview
This document describes the PostgreSQL database schema for the multi-user configuration system.

## Entity Relationship Diagram

```
Users (1) ----< (M) Agents
Users (1) ----< (M) UserRoles ----< (M) Roles
Roles (1) ----< (M) RolePermissions ----< (M) Permissions
Agents (1) ----< (M) PluginConfigurations
Agents (1) ----< (M) AgentTools
```

## Tables

### Users
Stores user accounts with hashed passwords.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique user identifier |
| Username | VARCHAR(100) | UNIQUE, NOT NULL | Login username |
| Email | VARCHAR(255) | UNIQUE, NOT NULL | User email address |
| PasswordHash | VARCHAR(255) | NOT NULL | BCrypt hashed password |
| IsActive | BOOLEAN | DEFAULT TRUE | Account active status |
| CreatedAt | TIMESTAMP | NOT NULL | Account creation timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last update timestamp |
| CreatedById | UUID | FOREIGN KEY | User who created this account |

**Indexes**:
- `idx_users_username` on Username
- `idx_users_email` on Email
- `idx_users_isactive` on IsActive

### Roles
Predefined roles for authorization.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INT | PRIMARY KEY | Role identifier |
| Name | VARCHAR(50) | UNIQUE, NOT NULL | Role name |
| Description | VARCHAR(500) | NULL | Role description |

**Default Roles**:
- `Admin` - Full system access, can reset passwords, manage all users
- `AgentManager` - Can create and manage agents and configurations
- `User` - Basic access, can create own agents
- `ReadOnly` - View-only access

### Permissions
Granular permissions for access control.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INT | PRIMARY KEY | Permission identifier |
| Name | VARCHAR(100) | UNIQUE, NOT NULL | Permission name |
| Description | VARCHAR(500) | NULL | Permission description |

**Default Permissions**:
- `ManageUsers` - Create, update, delete users
- `ResetPasswords` - Reset user passwords
- `CreateAgents` - Create new agents
- `UpdateAgents` - Modify agent configurations
- `DeleteAgents` - Delete agents
- `ViewAgents` - View agent configurations
- `ManagePluginConfigs` - Manage plugin configurations
- `ViewPluginConfigs` - View plugin configurations

### UserRoles
Many-to-many relationship between Users and Roles.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| UserId | UUID | FOREIGN KEY, NOT NULL | Reference to Users.Id |
| RoleId | INT | FOREIGN KEY, NOT NULL | Reference to Roles.Id |

**Composite Primary Key**: (UserId, RoleId)

### RolePermissions
Many-to-many relationship between Roles and Permissions.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| RoleId | INT | FOREIGN KEY, NOT NULL | Reference to Roles.Id |
| PermissionId | INT | FOREIGN KEY, NOT NULL | Reference to Permissions.Id |

**Composite Primary Key**: (RoleId, PermissionId)

### Agents
Agent (mini-agent) configurations.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Unique agent identifier |
| Name | VARCHAR(100) | NOT NULL | Agent name |
| Description | TEXT | NULL | Agent description |
| OwnerId | UUID | FOREIGN KEY, NOT NULL | Reference to Users.Id |
| IsDefault | BOOLEAN | DEFAULT FALSE | Is this the default agent for the owner |
| IsActive | BOOLEAN | DEFAULT TRUE | Agent active status |
| Model | VARCHAR(200) | NULL | AI model identifier |
| ApiKey | VARCHAR(500) | NULL | Encrypted API key |
| ApiUrl | VARCHAR(500) | NULL | API endpoint URL |
| DefaultSystemPrompt | TEXT | NULL | Default system prompt |
| ToolsEnabled | BOOLEAN | DEFAULT TRUE | Enable tool calling |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last update timestamp |

**Indexes**:
- `idx_agents_ownerid` on OwnerId
- `idx_agents_name_ownerid` on (Name, OwnerId) UNIQUE
- `idx_agents_isdefault` on IsDefault
- `idx_agents_isactive` on IsActive

### PluginConfigurations
Per-agent plugin configurations (or global defaults if AgentId is NULL).

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Configuration identifier |
| AgentId | UUID | FOREIGN KEY, NULL | Reference to Agents.Id (NULL = default) |
| PluginName | VARCHAR(200) | NOT NULL | Plugin identifier |
| ConfigurationJson | JSONB | NOT NULL | Plugin config as JSON |
| IsActive | BOOLEAN | DEFAULT TRUE | Configuration active status |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |
| UpdatedAt | TIMESTAMP | NOT NULL | Last update timestamp |

**Indexes**:
- `idx_pluginconfigs_agentid` on AgentId
- `idx_pluginconfigs_pluginname` on PluginName
- `idx_pluginconfigs_agentid_pluginname` on (AgentId, PluginName) UNIQUE

**Note**: When AgentId is NULL, this represents a default/global configuration that applies when no agent-specific config exists.

### AgentTools
Tools/functions enabled for each agent.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | UUID | PRIMARY KEY | Record identifier |
| AgentId | UUID | FOREIGN KEY, NOT NULL | Reference to Agents.Id |
| ToolName | VARCHAR(200) | NOT NULL | Tool/function name |
| IsEnabled | BOOLEAN | DEFAULT TRUE | Tool enabled status |
| CreatedAt | TIMESTAMP | NOT NULL | Creation timestamp |

**Indexes**:
- `idx_agenttools_agentid` on AgentId
- `idx_agenttools_agentid_toolname` on (AgentId, ToolName) UNIQUE

## Configuration Fallback Logic

When loading configuration for an agent:

1. **Agent-specific plugin config**: Check `PluginConfigurations` where `AgentId = {agentId}` AND `PluginName = {pluginName}`
2. **Default plugin config**: If not found, check `PluginConfigurations` where `AgentId IS NULL` AND `PluginName = {pluginName}`
3. **JSON file fallback** (migration phase): If not found in DB, fall back to JSON file in `Configs/` directory

## Security Considerations

1. **Password Storage**:
   - NEVER store plain text passwords
   - Use BCrypt with salt (work factor: 12+)
   - Store only the hash in `PasswordHash` column

2. **API Keys**:
   - Encrypt API keys at rest in `Agents.ApiKey`
   - Use environment-based encryption key
   - Never expose keys in API responses

3. **Admin User**:
   - Create from environment variable `ADMIN_USERNAME` and `ADMIN_PASSWORD`
   - Seed during application startup if not exists
   - Automatically assign Admin role

4. **Authorization**:
   - Users can only view/modify their own agents
   - Admin role can view/modify all agents
   - Permission checks before all mutation operations

## Migration Path

### Phase 1: Database Setup
1. Create all entities and DbContext
2. Add EF Core migrations
3. Seed default roles and permissions
4. Seed admin user from environment

### Phase 2: Backward Compatibility
1. Keep JSON file loading working
2. Check database first, fall back to JSON
3. Add migration tool to import JSON to database

### Phase 3: Full Migration
1. Migrate all configs to database
2. Deprecate JSON file loading
3. Remove file-based configuration code

## Example Queries

### Get agent configuration with plugin configs
```sql
SELECT a.*, pc.PluginName, pc.ConfigurationJson
FROM Agents a
LEFT JOIN PluginConfigurations pc ON pc.AgentId = a.Id
WHERE a.Id = '{agentId}' AND a.IsActive = TRUE AND pc.IsActive = TRUE;
```

### Get default configuration for a plugin
```sql
SELECT ConfigurationJson
FROM PluginConfigurations
WHERE AgentId IS NULL
  AND PluginName = '{pluginName}'
  AND IsActive = TRUE;
```

### Get all tools for an agent
```sql
SELECT at.ToolName, at.IsEnabled
FROM AgentTools at
WHERE at.AgentId = '{agentId}' AND at.IsEnabled = TRUE;
```

### Check user permissions
```sql
SELECT DISTINCT p.Name
FROM Users u
INNER JOIN UserRoles ur ON ur.UserId = u.Id
INNER JOIN RolePermissions rp ON rp.RoleId = ur.RoleId
INNER JOIN Permissions p ON p.Id = rp.PermissionId
WHERE u.Id = '{userId}' AND u.IsActive = TRUE;
```
