using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.DTOs.Auth;
using Ai.Orchestrator.Models.DTOs.Users;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Ai.Orchestrator.Services.Users;

/// <summary>
/// Service for user management operations
/// </summary>
public class UserService : IUserService
{
    private readonly OrchestratorDbContext _context;

    public UserService(OrchestratorDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<UserDto> RegisterUserAsync(RegisterRequest request)
    {
        // Validate email format
        if (!IsValidEmail(request.Email))
        {
            throw new ArgumentException("Invalid email format", nameof(request.Email));
        }

        // Check for duplicate username
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new InvalidOperationException($"Username '{request.Username}' is already taken");
        }

        // Check for duplicate email
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new InvalidOperationException($"Email '{request.Email}' is already registered");
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.SetPassword(request.Password);

        _context.Users.Add(user);

        // Assign default "User" role
        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.User);
        if (userRole != null)
        {
            _context.Set<UserRole>().Add(new UserRole
            {
                UserId = user.Id,
                RoleId = userRole.Id
            });
        }

        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(user.Id);
    }

    public async Task<UserDto> AuthenticateAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !user.IsActive)
        {
            return null;
        }

        if (!user.VerifyPassword(request.Password))
        {
            return null;
        }

        return MapToDto(user);
    }

    public async Task<UserDto> GetUserByIdAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto> GetUserByUsernameAsync(string username)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username);

        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto> GetUserByEmailAsync(string email)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        return user != null ? MapToDto(user) : null;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive)
            .ToListAsync();

        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{userId}' not found");
        }

        // Update email if provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            // Validate email format
            if (!IsValidEmail(request.Email))
            {
                throw new ArgumentException("Invalid email format", nameof(request.Email));
            }

            // Check for duplicate email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != userId);

            if (existingUser != null)
            {
                throw new InvalidOperationException($"Email '{request.Email}' is already registered");
            }

            user.Email = request.Email;
        }

        // Update IsActive if provided
        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(userId);
    }

    public async Task<UserDto> UpdateUserEmailAsync(Guid userId, string newEmail)
    {
        // Validate email format
        if (!IsValidEmail(newEmail))
        {
            throw new ArgumentException("Invalid email format", nameof(newEmail));
        }

        // Check for duplicate email
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == newEmail && u.Id != userId);

        if (existingUser != null)
        {
            throw new InvalidOperationException($"Email '{newEmail}' is already registered");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{userId}' not found");
        }

        user.Email = newEmail;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(userId);
    }

    public async Task DeactivateUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{userId}' not found");
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task ActivateUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{userId}' not found");
        }

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{request.UserId}' not found");
        }

        user.SetPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task AssignRoleAsync(Guid userId, string roleName)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{userId}' not found");
        }

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
        {
            throw new InvalidOperationException($"Role '{roleName}' not found");
        }

        // Check if user already has this role
        var existingUserRole = await _context.Set<UserRole>()
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

        if (existingUserRole == null)
        {
            _context.Set<UserRole>().Add(new UserRole
            {
                UserId = userId,
                RoleId = role.Id
            });

            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveRoleAsync(Guid userId, string roleName)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
        {
            throw new InvalidOperationException($"Role '{roleName}' not found");
        }

        var userRole = await _context.Set<UserRole>()
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

        if (userRole != null)
        {
            _context.Set<UserRole>().Remove(userRole);
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddRoleToUserAsync(Guid userId, Guid roleId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{userId}' not found");
        }

        var role = await _context.Roles.FindAsync(roleId);
        if (role == null)
        {
            throw new InvalidOperationException($"Role with ID '{roleId}' not found");
        }

        // Check if user already has this role
        var existingUserRole = await _context.Set<UserRole>()
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (existingUserRole != null)
        {
            throw new InvalidOperationException($"User already has this role");
        }

        _context.Set<UserRole>().Add(new UserRole
        {
            UserId = userId,
            RoleId = roleId
        });

        await _context.SaveChangesAsync();
    }

    public async Task RemoveRoleFromUserAsync(Guid userId, Guid roleId)
    {
        var userRole = await _context.Set<UserRole>()
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole == null)
        {
            throw new InvalidOperationException($"User does not have this role");
        }

        _context.Set<UserRole>().Remove(userRole);
        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return new List<string>();
        }

        return user.UserRoles.Select(ur => ur.Role.Name).ToList();
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{userId}' not found");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UserExistsAsync(Guid userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId);
    }

    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        return !await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> IsUsernameAvailableAsync(string username)
    {
        return !await _context.Users.AnyAsync(u => u.Username == username);
    }

    // Helper methods

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
        };
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var emailValidator = new EmailAddressAttribute();
            return emailValidator.IsValid(email);
        }
        catch
        {
            return false;
        }
    }
}
