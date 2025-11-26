using System.ComponentModel.DataAnnotations;

namespace Roomy.Api.Endpoints.Rooms;

public class UpdateRoomRequest
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(1, 1000)]
    public int Capacity { get; set; }
}
