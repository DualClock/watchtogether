using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTogether.Application.DTOs.Room;
using WatchTogether.Application.Services;

namespace WatchTogether.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<RoomResponse>>> GetPublicRooms([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var rooms = await _roomService.GetPublicRoomsAsync(page, pageSize);
        return Ok(rooms);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<RoomResponse>>> SearchRooms([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { error = "Query is required" });

        var rooms = await _roomService.SearchRoomsAsync(query, page, pageSize);
        return Ok(rooms);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<RoomDetailResponse>> GetRoom(Guid id)
    {
        var room = await _roomService.GetRoomByIdAsync(id);
        if (room == null) return NotFound();

        return Ok(room);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<RoomResponse>> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var room = await _roomService.CreateRoomAsync(request, userId.Value);
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/join")]
    [Authorize]
    public async Task<ActionResult<RoomDetailResponse>> JoinRoom(Guid id, [FromBody] JoinRoomRequest? request)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var room = await _roomService.JoinRoomAsync(id, userId.Value, request?.Password);
            if (room == null) return NotFound();

            return Ok(room);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/leave")]
    [Authorize]
    public async Task<IActionResult> LeaveRoom(Guid id)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();

        await _roomService.LeaveRoomAsync(id, userId.Value);
        return Ok(new { message = "Left room successfully" });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> CloseRoom(Guid id)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            await _roomService.CloseRoomAsync(id, userId.Value);
            return Ok(new { message = "Room closed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/kick/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> KickUser(Guid id, Guid userId)
    {
        var requesterId = GetUserId();
        if (!requesterId.HasValue) return Unauthorized();

        try
        {
            await _roomService.KickUserAsync(id, userId, requesterId.Value);
            return Ok(new { message = "User kicked successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }
}

public class JoinRoomRequest
{
    public string? Password { get; set; }
}
