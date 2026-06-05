namespace WatchTogether.Application.DTOs.Room;

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "public";
    public string? Password { get; set; }
    public int MaxUsers { get; set; } = 10;
    
    // Video source
    public string? VideoUrl { get; set; } // URL for external video (YouTube, Yandex, etc.)
    public string? VideoType { get; set; } = "url"; // "url" or "file"
}
