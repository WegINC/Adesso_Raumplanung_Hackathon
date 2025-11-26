using System.ComponentModel.DataAnnotations;

namespace Roomy.Api.Endpoints.Reservations;

public class CreateReservationRequest
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }
}

public class ReservationResponse
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ReservationConflictResponse
{
    public string Message { get; set; } = string.Empty;
    public List<AlternativeRoomSuggestion> Alternatives { get; set; } = new();
}

public class AlternativeRoomSuggestion
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public DateTime AvailableStartTime { get; set; }
    public DateTime AvailableEndTime { get; set; }
    public bool IsExactMatch { get; set; }
}
