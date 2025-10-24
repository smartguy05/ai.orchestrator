using Ai.Orchestrator.Models.DTOs.Users;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ai.Orchestrator.Controllers;

/// <summary>
/// Controller for user management operations
/// Handles CRUD operations and role assignment for users
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    /// <summary>
    /// Get current user's ID from JWT claims
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim);
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving users", error = ex.Message });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"User with ID '{id}' not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the user", error = ex.Message });
        }
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _userService.UpdateUserAsync(id, request);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            // Check if it's a "not found" error or "already exists" error
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            // Validation errors (invalid email, etc.)
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while updating the user", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete user
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            await _userService.DeleteUserAsync(id);
            return Ok(new { message = $"User with ID '{id}' deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the user", error = ex.Message });
        }
    }

    /// <summary>
    /// Add role to user
    /// </summary>
    [HttpPost("{userId}/roles/{roleId}")]
    public async Task<IActionResult> AddRoleToUser(Guid userId, Guid roleId)
    {
        try
        {
            await _userService.AddRoleToUserAsync(userId, roleId);
            return Ok(new { message = $"Role added successfully to user" });
        }
        catch (InvalidOperationException ex)
        {
            // Check if it's a "not found" error or "already has" error
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while adding role to user", error = ex.Message });
        }
    }

    /// <summary>
    /// Remove role from user
    /// </summary>
    [HttpDelete("{userId}/roles/{roleId}")]
    public async Task<IActionResult> RemoveRoleFromUser(Guid userId, Guid roleId)
    {
        try
        {
            await _userService.RemoveRoleFromUserAsync(userId, roleId);
            return Ok(new { message = $"Role removed successfully from user" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while removing role from user", error = ex.Message });
        }
    }

    /// <summary>
    /// Get users by role
    /// </summary>
    [HttpGet("by-role/{roleId}")]
    public async Task<IActionResult> GetUsersByRole(Guid roleId)
    {
        try
        {
            var users = await _userService.GetUsersByRoleAsync(roleId);
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving users by role", error = ex.Message });
        }
    }
}
