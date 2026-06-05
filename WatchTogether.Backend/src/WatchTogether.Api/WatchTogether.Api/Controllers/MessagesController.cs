using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTogether.Application.DTOs.Message;
using WatchTogether.Application.Services;

namespace WatchTogether.Api.Controllers;

[ApiController]
[Route("api/rooms/{roomId:guid}/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageResponse>>> GetMessages(Guid roomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var messages = await _messageService.GetMessagesAsync(roomId, page, pageSize);
        return Ok(messages);
    }

    [HttpPost]
    public async Task<ActionResult<MessageResponse>> SendMessage(Guid roomId, [FromBody] SendMessageRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var message = await _messageService.SendMessageAsync(roomId, userId.Value, request);
            return Ok(message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{messageId:guid}")]
    public async Task<ActionResult<MessageResponse>> EditMessage(Guid roomId, Guid messageId, [FromBody] EditMessageRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var message = await _messageService.EditMessageAsync(messageId, userId.Value, request.Content);
            if (message == null) return NotFound();

            return Ok(message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid roomId, Guid messageId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            await _messageService.DeleteMessageAsync(messageId, userId.Value);
            return Ok(new { message = "Message deleted successfully" });
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

public class EditMessageRequest
{
    public string Content { get; set; } = string.Empty;
}
