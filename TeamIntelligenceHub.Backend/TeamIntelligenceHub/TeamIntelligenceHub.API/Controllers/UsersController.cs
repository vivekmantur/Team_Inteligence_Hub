// 1. Get all users
// 2. Get the signed-in user
// 3. Get one user by ID
// 4. Update a user's app role

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>Lists users, returns the signed-in user, and updates a user's AppRole.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="userService">The service that reads, provisions, and updates users.</param>
    /// <param name="logger">The logger used to record provisioning failures.</param>
    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <returns>200 OK with the users.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();

        return Ok(users);
    }

    /// <summary>
    /// Gets the signed-in user, creating the row on first sign-in and refreshing the profile and last-login stamp on every call after that.
    /// </summary>
    /// <returns>200 OK with the signed-in user, or 401 Unauthorized when the token lacks the claims needed to provision the user.</returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var user = await _userService.GetCurrentUserAsync();

            return Ok(user);
        }
        catch (UnauthorizedAccessException ex)
        {
            // The token authenticated but lacks a claim we provision from. Name the
            // claims that did arrive, otherwise this is indistinguishable from a
            // rejected token at the browser.
            _logger.LogWarning(
                "Provisioning failed: {Reason} Claims on the token: {Claims}",
                ex.Message,
                string.Join(", ", User.Claims.Select(c => c.Type)));

            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the user with the given ID.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <returns>200 OK with the user, or 404 Not Found when it does not exist.</returns>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Sets a user's AppRole, the free-text role or title shown on the Team page. Only the signed-in user may edit their own; the ID in the route keeps the client's existing request shape and does not let anyone target someone else.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="request">The new AppRole value.</param>
    /// <returns>200 OK with the updated user, 400 Bad Request when validation fails, or 401 Unauthorized when the caller is not the user being edited.</returns>
    [HttpPut("{id:int}/app-role")]
    public async Task<IActionResult> UpdateAppRole(
        int id, [FromBody] UpdateAppRoleRequestDto request)
    {
        try
        {
            var updated = await _userService.UpdateAppRoleAsync(id, request);

            return Ok(updated);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
