using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WatchTogether.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VideosController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public VideosController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadVideo(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        // Validate file type
        var allowedTypes = new[] { "video/mp4", "video/webm", "video/ogg", "video/x-matroska" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { error = "Invalid file type. Only video files are allowed." });

        // Max 500MB
        if (file.Length > 500 * 1024 * 1024)
            return BadRequest(new { error = "File too large. Max 500MB." });

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "videos");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"/uploads/videos/{fileName}";
        return Ok(new { url, fileName });
    }
}
