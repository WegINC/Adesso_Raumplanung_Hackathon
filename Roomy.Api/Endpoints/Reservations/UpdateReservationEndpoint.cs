using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;
using Roomy.Api.Entities;
using Roomy.Api.Services;
using System.Security.Claims;

namespace Roomy.Api.Endpoints.Reservations;

public class UpdateReservationEndpoint : Endpoint<UpdateReservationRequest, ReservationResponse>
{
    private readonly RoomyDbContext _dbContext;
    private readonly IReservationService _reservationService;

    public UpdateReservationEndpoint(RoomyDbContext dbContext, IReservationService reservationService)
    {
        _dbContext = dbContext;
        _reservationService = reservationService;
    }

    public override void Configure()
    {
        Put("/api/reservations/{Id}");
    }

    public override async Task HandleAsync(UpdateReservationRequest req, CancellationToken ct)
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
            .Include(r => r.Room)
            .Include(r => r.User)
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
            ThrowError("You don't have permission to update this reservation", 403);
            return;
        }

        // Check if reservation is already cancelled
        if (reservation.Status == ReservationStatus.Cancelled)
        {
            ThrowError("Cannot update a cancelled reservation", 400);
            return;
        }

        // Update fields
        var hasTimeChange = false;

        if (!string.IsNullOrWhiteSpace(req.Title))
        {
            reservation.Title = req.Title;
        }

        if (req.Description != null)
        {
            reservation.Description = req.Description;
        }

        if (req.StartTime.HasValue || req.EndTime.HasValue)
        {
            var newStartTime = req.StartTime ?? reservation.StartTime;
            var newEndTime = req.EndTime ?? reservation.EndTime;

            // Validate time range
            if (newEndTime <= newStartTime)
            {
                ThrowError("End time must be after start time", 400);
                return;
            }

            if (newStartTime < DateTime.UtcNow)
            {
                ThrowError("Cannot reschedule reservation to the past", 400);
                return;
            }

            // Check for conflicts (exclude current reservation)
            if (await _reservationService.HasConflictAsync(reservation.RoomId, newStartTime, newEndTime, reservation.Id, ct))
            {
                ThrowError("Room is already booked for the selected time slot", 409);
                return;
            }

            reservation.StartTime = newStartTime;
            reservation.EndTime = newEndTime;
            hasTimeChange = true;
        }

        if (hasTimeChange || req.Title != null || req.Description != null)
        {
            reservation.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
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
