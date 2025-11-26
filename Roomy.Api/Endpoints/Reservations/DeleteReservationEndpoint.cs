using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;
using Roomy.Api.Entities;
using System.Security.Claims;

namespace Roomy.Api.Endpoints.Reservations;

public class DeleteReservationRequest
{
    public int Id { get; set; }
}

public class DeleteReservationResponse
{
    public string Message { get; set; } = string.Empty;
}

public class DeleteReservationEndpoint : Endpoint<DeleteReservationRequest, DeleteReservationResponse>
{
    private readonly RoomyDbContext _dbContext;

    public DeleteReservationEndpoint(RoomyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Delete("/api/reservations/{Id}");
    }

    public override async Task HandleAsync(DeleteReservationRequest req, CancellationToken ct)
    {
        // Get current user ID
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Unauthorized", 401);
            return;
        }

        // Get existing reservation
        var reservation = await _dbContext.Reservations
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct);

        if (reservation == null)
        {
            ThrowError("Reservation not found", 404);
            return;
        }

        // Check if user is owner or admin
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (reservation.UserId != userId && userRole != "Administrator")
        {
            ThrowError("You don't have permission to cancel this reservation", 403);
            return;
        }

        // Check if already cancelled
        if (reservation.Status == ReservationStatus.Cancelled)
        {
            ThrowError("Reservation is already cancelled", 400);
            return;
        }

        // Cancel the reservation (soft delete)
        reservation.Status = ReservationStatus.Cancelled;
        reservation.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        Response = new DeleteReservationResponse
        {
            Message = "Reservation cancelled successfully"
        };
    }
}
