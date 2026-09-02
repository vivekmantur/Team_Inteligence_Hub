using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();

        return Ok(users);
    }

    /// <summary>
    /// Returns the signed-in user, creating the row on first sign-in and
    /// refreshing the profile and last-login stamp on every call after that.
    /// </summary>
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
}
