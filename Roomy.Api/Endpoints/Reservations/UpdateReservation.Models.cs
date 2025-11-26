using System.ComponentModel.DataAnnotations;

namespace Roomy.Api.Endpoints.Reservations;

public class UpdateReservationRequest
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }
}
