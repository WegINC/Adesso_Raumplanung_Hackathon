using System.ComponentModel.DataAnnotations;
using FastEndpoints;
using Roomy.Api.Services;

namespace Roomy.Api.Endpoints.Reservations;

public class CheckAvailabilityRequest
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }
}

public class CheckAvailabilityResponse
{
    public bool IsAvailable { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ConflictingReservation>? Conflicts { get; set; }
}

public class ConflictingReservation
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class CheckAvailabilityEndpoint : Endpoint<CheckAvailabilityRequest, CheckAvailabilityResponse>
{
    private readonly IReservationService _reservationService;

    public CheckAvailabilityEndpoint(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public override void Configure()
    {
        Post("/api/reservations/check-availability");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CheckAvailabilityRequest req, CancellationToken ct)
    {
        // Validate time range
        if (req.EndTime <= req.StartTime)
        {
            ThrowError("End time must be after start time", 400);
            return;
        }

        var isAvailable = await _reservationService.IsRoomAvailableAsync(req.RoomId, req.StartTime, req.EndTime, null, ct);

        if (isAvailable)
        {
            Response = new CheckAvailabilityResponse
            {
                IsAvailable = true,
                Message = "Room is available for the selected time slot"
            };
        }
        else
        {
            var conflicts = await _reservationService.GetConflictingReservationsAsync(req.RoomId, req.StartTime, req.EndTime, null, ct);

            Response = new CheckAvailabilityResponse
            {
                IsAvailable = false,
                Message = "Room is not available for the selected time slot",
                Conflicts = conflicts.Select(c => new ConflictingReservation
                {
                    Id = c.Id,
                    Title = c.Title,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime
                }).ToList()
            };
        }
    }
}
