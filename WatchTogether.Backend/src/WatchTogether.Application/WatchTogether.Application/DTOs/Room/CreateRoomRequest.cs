namespace WatchTogether.Application.DTOs.Room;

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "public";
    public string? Password { get; set; }
    public int MaxUsers { get; set; } = 10;
    
    // Room purpose
    public string Purpose { get; set; } = "cinema"; // "cinema" or "call"
    
    // Video source (for cinema rooms)
    public string? VideoUrl { get; set; }
    public string? VideoType { get; set; } = "url";
}
