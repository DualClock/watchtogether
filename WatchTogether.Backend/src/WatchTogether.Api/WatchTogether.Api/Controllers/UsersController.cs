using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTogether.Application.DTOs.User;
using WatchTogether.Application.Services;

namespace WatchTogether.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile()
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();

        var profile = await _userService.GetProfileAsync(userId.Value);
        if (profile == null) return NotFound();

        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var profile = await _userService.UpdateProfileAsync(userId.Value, request);
            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("me/avatar")]
    public async Task<ActionResult> UpdateAvatar(IFormFile file)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "File size must be less than 5MB" });

        try
        {
            using var stream = file.OpenReadStream();
            var avatarUrl = await _userService.UpdateAvatarAsync(userId.Value, stream, file.FileName);
            return Ok(new { avatarUrl });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{username}")]
    [AllowAnonymous]
    public async Task<ActionResult<UserProfileResponse>> GetProfileByUsername(string username)
    {
        var profile = await _userService.GetProfileByUsernameAsync(username);
        if (profile == null) return NotFound();

        return Ok(profile);
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();

        await _userService.DeleteAccountAsync(userId.Value);
        return Ok(new { message = "Account deleted successfully" });
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }
}
