using Ai.Orchestrator.Models.DTOs.Auth;
using Ai.Orchestrator.Models.DTOs.Users;

namespace Ai.Orchestrator.Models.Interfaces;

/// <summary>
/// Service interface for user management
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Registers a new user
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Created user DTO</returns>
    Task<UserDto> RegisterUserAsync(RegisterRequest request);

    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    /// <param name="request">Login request</param>
    /// <returns>User DTO if successful, null otherwise</returns>
    Task<UserDto> AuthenticateAsync(LoginRequest request);

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>User DTO or null if not found</returns>
    Task<UserDto> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Gets a user by username
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>User DTO or null if not found</returns>
    Task<UserDto> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Gets a user by email
    /// </summary>
    /// <param name="email">Email address</param>
    /// <returns>User DTO or null if not found</returns>
    Task<UserDto> GetUserByEmailAsync(string email);

    /// <summary>
    /// Gets all active users
    /// </summary>
    /// <returns>List of user DTOs</returns>
    Task<List<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Updates a user's information
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="request">Update request with email and/or isActive</param>
    /// <returns>Updated user DTO</returns>
    Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request);

    /// <summary>
    /// Updates a user's email address
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="newEmail">New email address</param>
    /// <returns>Updated user DTO</returns>
    Task<UserDto> UpdateUserEmailAsync(Guid userId, string newEmail);

    /// <summary>
    /// Deactivates a user account
    /// </summary>
    /// <param name="userId">User identifier</param>
    Task DeactivateUserAsync(Guid userId);

    /// <summary>
    /// Activates a user account
    /// </summary>
    /// <param name="userId">User identifier</param>
    Task ActivateUserAsync(Guid userId);

    /// <summary>
    /// Resets a user's password (admin only)
    /// </summary>
    /// <param name="request">Reset password request</param>
    Task ResetPasswordAsync(ResetPasswordRequest request);

    /// <summary>
    /// Assigns a role to a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="roleName">Role name</param>
    Task AssignRoleAsync(Guid userId, string roleName);

    /// <summary>
    /// Removes a role from a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="roleName">Role name</param>
    Task RemoveRoleAsync(Guid userId, string roleName);

    /// <summary>
    /// Adds a role to a user by role ID
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="roleId">Role identifier</param>
    Task AddRoleToUserAsync(Guid userId, int roleId);

    /// <summary>
    /// Removes a role from a user by role ID
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="roleId">Role identifier</param>
    Task RemoveRoleFromUserAsync(Guid userId, int roleId);

    /// <summary>
    /// Gets all roles for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>List of role names</returns>
    Task<List<string>> GetUserRolesAsync(Guid userId);

    /// <summary>
    /// Gets all users with a specific role
    /// </summary>
    /// <param name="roleId">Role identifier</param>
    /// <returns>List of user DTOs</returns>
    Task<List<UserDto>> GetUsersByRoleAsync(int roleId);

    /// <summary>
    /// Deletes a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    Task DeleteUserAsync(Guid userId);

    /// <summary>
    /// Checks if a user exists
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>True if user exists, false otherwise</returns>
    Task<bool> UserExistsAsync(Guid userId);

    /// <summary>
    /// Checks if an email is available
    /// </summary>
    /// <param name="email">Email address</param>
    /// <returns>True if available, false if already used</returns>
    Task<bool> IsEmailAvailableAsync(string email);

    /// <summary>
    /// Checks if a username is available
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>True if available, false if already used</returns>
    Task<bool> IsUsernameAvailableAsync(string username);
}
