namespace Roomy.Api.Endpoints.Rooms;

public class GetRoomsResponse
{
    public List<RoomDto> Rooms { get; set; } = new();
}

public class RoomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
