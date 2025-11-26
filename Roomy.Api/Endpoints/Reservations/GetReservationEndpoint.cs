using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;

namespace Roomy.Api.Endpoints.Reservations;

public class GetReservationRequest
{
    public int Id { get; set; }
}

public class GetReservationEndpoint : Endpoint<GetReservationRequest, ReservationResponse>
{
    private readonly RoomyDbContext _dbContext;

    public GetReservationEndpoint(RoomyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/api/reservations/{Id}");
        // Require authentication
    }

    public override async Task HandleAsync(GetReservationRequest req, CancellationToken ct)
    {
        var reservation = await _dbContext.Reservations
            .Include(r => r.Room)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct);

        if (reservation == null)
        {
            ThrowError("Reservation not found", 404);
            return;
        }

        Response = new ReservationResponse
        {
            Id = reservation.Id,
            RoomId = reservation.RoomId,
            RoomName = reservation.Room.Name,
            UserId = reservation.UserId,
            UserName = $"{reservation.User.FirstName} {reservation.User.LastName}",
            Title = reservation.Title,
            Description = reservation.Description,
            StartTime = reservation.StartTime,
            EndTime = reservation.EndTime,
            Status = reservation.Status.ToString(),
            CreatedAt = reservation.CreatedAt,
            UpdatedAt = reservation.UpdatedAt
        };
    }
}
