using Ai.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ai.Orchestrator.Models.Data;

/// <summary>
/// Database context for the Orchestrator application
/// Manages all database entities and their relationships
/// </summary>
public class OrchestratorDbContext : DbContext
{
    public OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options)
        : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Agent> Agents { get; set; }
    public DbSet<PluginConfiguration> PluginConfigurations { get; set; }
    public DbSet<AgentTool> AgentTools { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureRole(modelBuilder);
        ConfigurePermission(modelBuilder);
        ConfigureUserRole(modelBuilder);
        ConfigureRolePermission(modelBuilder);
        ConfigureAgent(modelBuilder);
        ConfigurePluginConfiguration(modelBuilder);
        ConfigureAgentTool(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureRefreshToken(modelBuilder);
        SeedDefaultData(modelBuilder);
    }

    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // Indexes
            entity.HasIndex(e => e.Username)
                .IsUnique()
                .HasDatabaseName("idx_users_username");

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("idx_users_email");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("idx_users_isactive");

            // Self-referential relationship for CreatedBy
            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship to Agents
            entity.HasMany(e => e.Agents)
                .WithOne(a => a.Owner)
                .HasForeignKey(a => a.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship to UserRoles
            entity.HasMany(e => e.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            // Indexes
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("idx_roles_name");

            // Relationship to RolePermissions
            entity.HasMany(e => e.RolePermissions)
                .WithOne(rp => rp.Role)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship to UserRoles
            entity.HasMany(e => e.UserRoles)
                .WithOne(ur => ur.Role)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigurePermission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            // Indexes
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("idx_permissions_name");

            // Relationship to RolePermissions
            entity.HasMany(e => e.RolePermissions)
                .WithOne(rp => rp.Permission)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureUserRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureRolePermission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });

            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAgent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Model)
                .HasMaxLength(200);

            entity.Property(e => e.ApiKey)
                .HasMaxLength(500);

            entity.Property(e => e.ApiUrl)
                .HasMaxLength(500);

            entity.Property(e => e.DefaultSystemPrompt)
                .HasMaxLength(50000);

            entity.Property(e => e.IsDefault)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.ToolsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.ActivePlugins)
                .HasMaxLength(1000);

            entity.Property(e => e.ConfirmationPlugin)
                .HasMaxLength(200);

            entity.Property(e => e.ConfirmationExpirationMinutes)
                .HasDefaultValue(30);

            entity.Property(e => e.NotificationTimeoutHours)
                .HasDefaultValue(24);

            entity.Property(e => e.LoggingPlugins)
                .HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // Indexes
            entity.HasIndex(e => e.OwnerId)
                .HasDatabaseName("idx_agents_ownerid");

            entity.HasIndex(e => new { e.Name, e.OwnerId })
                .IsUnique()
                .HasDatabaseName("idx_agents_name_ownerid");

            entity.HasIndex(e => e.IsDefault)
                .HasDatabaseName("idx_agents_isdefault");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("idx_agents_isactive");

            // Relationship to User
            entity.HasOne(e => e.Owner)
                .WithMany(u => u.Agents)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship to PluginConfigurations
            entity.HasMany(e => e.PluginConfigurations)
                .WithOne(pc => pc.Agent)
                .HasForeignKey(pc => pc.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship to AgentTools
            entity.HasMany(e => e.AgentTools)
                .WithOne(at => at.Agent)
                .HasForeignKey(at => at.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigurePluginConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PluginConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PluginName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ConfigurationJson)
                .IsRequired()
                .HasColumnType("text");  // Use text for PostgreSQL JSONB

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // Indexes
            entity.HasIndex(e => e.AgentId)
                .HasDatabaseName("idx_pluginconfigs_agentid");

            entity.HasIndex(e => e.PluginName)
                .HasDatabaseName("idx_pluginconfigs_pluginname");

            entity.HasIndex(e => new { e.AgentId, e.PluginName })
                .IsUnique()
                .HasDatabaseName("idx_pluginconfigs_agentid_pluginname");

            // Relationship to Agent (nullable for default configs)
            entity.HasOne(e => e.Agent)
                .WithMany(a => a.PluginConfigurations)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        });
    }

    private void ConfigureAgentTool(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentTool>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ToolName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.IsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Indexes
            entity.HasIndex(e => e.AgentId)
                .HasDatabaseName("idx_agenttools_agentid");

            entity.HasIndex(e => new { e.AgentId, e.ToolName })
                .IsUnique()
                .HasDatabaseName("idx_agenttools_agentid_toolname");

            // Relationship to Agent
            entity.HasOne(e => e.Agent)
                .WithMany(a => a.AgentTools)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                .HasMaxLength(100);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.EntityType)
                .HasMaxLength(100);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(50);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            entity.Property(e => e.Details)
                .HasMaxLength(10000);

            entity.Property(e => e.Outcome)
                .HasMaxLength(50);

            entity.Property(e => e.Timestamp)
                .IsRequired();

            entity.Property(e => e.HttpMethod)
                .HasMaxLength(10);

            entity.Property(e => e.RequestPath)
                .HasMaxLength(500);

            // Indexes for common queries
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_auditlogs_userid");

            entity.HasIndex(e => e.Action)
                .HasDatabaseName("idx_auditlogs_action");

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("idx_auditlogs_timestamp");

            entity.HasIndex(e => new { e.EntityType, e.EntityId })
                .HasDatabaseName("idx_auditlogs_entity");

            entity.HasIndex(e => e.Outcome)
                .HasDatabaseName("idx_auditlogs_outcome");

            // Relationship to User (optional, audit logs can exist without user)
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureRefreshToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.JwtId)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(50);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            entity.Property(e => e.RevokedReason)
                .HasMaxLength(500);

            entity.Property(e => e.IsUsed)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.IsRevoked)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .IsRequired();

            // Indexes for common queries
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_refreshtokens_userid");

            entity.HasIndex(e => e.Token)
                .IsUnique()
                .HasDatabaseName("idx_refreshtokens_token");

            entity.HasIndex(e => e.JwtId)
                .HasDatabaseName("idx_refreshtokens_jwtid");

            entity.HasIndex(e => e.ExpiresAt)
                .HasDatabaseName("idx_refreshtokens_expiresat");

            entity.HasIndex(e => new { e.IsRevoked, e.IsUsed, e.ExpiresAt })
                .HasDatabaseName("idx_refreshtokens_validity");

            // Relationship to User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Self-referencing relationship for token rotation chain
            entity.HasOne(e => e.ReplacedByToken)
                .WithMany()
                .HasForeignKey(e => e.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void SeedDefaultData(ModelBuilder modelBuilder)
    {
        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = Entities.Roles.Admin, Description = "Full system access, can reset passwords, manage all users" },
            new Role { Id = 2, Name = Entities.Roles.AgentManager, Description = "Can create and manage agents and configurations" },
            new Role { Id = 3, Name = Entities.Roles.User, Description = "Basic access, can create own agents" },
            new Role { Id = 4, Name = Entities.Roles.ReadOnly, Description = "View-only access" }
        );

        // Seed default permissions
        modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = 1, Name = Entities.Permissions.ManageUsers, Description = "Create, update, delete users" },
            new Permission { Id = 2, Name = Entities.Permissions.ResetPasswords, Description = "Reset user passwords" },
            new Permission { Id = 3, Name = Entities.Permissions.CreateAgents, Description = "Create new agents" },
            new Permission { Id = 4, Name = Entities.Permissions.UpdateAgents, Description = "Modify agent configurations" },
            new Permission { Id = 5, Name = Entities.Permissions.DeleteAgents, Description = "Delete agents" },
            new Permission { Id = 6, Name = Entities.Permissions.ViewAgents, Description = "View agent configurations" },
            new Permission { Id = 7, Name = Entities.Permissions.ManagePluginConfigs, Description = "Manage plugin configurations" },
            new Permission { Id = 8, Name = Entities.Permissions.ViewPluginConfigs, Description = "View plugin configurations" }
        );

        // Seed default role-permission mappings
        // Admin has all permissions
        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = 1, PermissionId = 1 },  // Admin -> ManageUsers
            new RolePermission { RoleId = 1, PermissionId = 2 },  // Admin -> ResetPasswords
            new RolePermission { RoleId = 1, PermissionId = 3 },  // Admin -> CreateAgents
            new RolePermission { RoleId = 1, PermissionId = 4 },  // Admin -> UpdateAgents
            new RolePermission { RoleId = 1, PermissionId = 5 },  // Admin -> DeleteAgents
            new RolePermission { RoleId = 1, PermissionId = 6 },  // Admin -> ViewAgents
            new RolePermission { RoleId = 1, PermissionId = 7 },  // Admin -> ManagePluginConfigs
            new RolePermission { RoleId = 1, PermissionId = 8 },  // Admin -> ViewPluginConfigs

            // AgentManager permissions
            new RolePermission { RoleId = 2, PermissionId = 3 },  // AgentManager -> CreateAgents
            new RolePermission { RoleId = 2, PermissionId = 4 },  // AgentManager -> UpdateAgents
            new RolePermission { RoleId = 2, PermissionId = 5 },  // AgentManager -> DeleteAgents
            new RolePermission { RoleId = 2, PermissionId = 6 },  // AgentManager -> ViewAgents
            new RolePermission { RoleId = 2, PermissionId = 7 },  // AgentManager -> ManagePluginConfigs
            new RolePermission { RoleId = 2, PermissionId = 8 },  // AgentManager -> ViewPluginConfigs

            // User permissions
            new RolePermission { RoleId = 3, PermissionId = 3 },  // User -> CreateAgents
            new RolePermission { RoleId = 3, PermissionId = 4 },  // User -> UpdateAgents
            new RolePermission { RoleId = 3, PermissionId = 6 },  // User -> ViewAgents
            new RolePermission { RoleId = 3, PermissionId = 7 },  // User -> ManagePluginConfigs
            new RolePermission { RoleId = 3, PermissionId = 8 },  // User -> ViewPluginConfigs

            // ReadOnly permissions
            new RolePermission { RoleId = 4, PermissionId = 6 },  // ReadOnly -> ViewAgents
            new RolePermission { RoleId = 4, PermissionId = 8 }   // ReadOnly -> ViewPluginConfigs
        );
    }
}
