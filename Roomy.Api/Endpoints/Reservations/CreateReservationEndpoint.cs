using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;
using Roomy.Api.Entities;
using Roomy.Api.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Roomy.Api.Endpoints.Reservations;

public class CreateReservationEndpoint : Endpoint<CreateReservationRequest, ReservationResponse>
{
    private readonly RoomyDbContext _dbContext;
    private readonly IReservationService _reservationService;

    public CreateReservationEndpoint(RoomyDbContext dbContext, IReservationService reservationService)
    {
        _dbContext = dbContext;
        _reservationService = reservationService;
    }

    public override void Configure()
    {
        Post("/api/reservations");
    }

    public override async Task HandleAsync(CreateReservationRequest req, CancellationToken ct)
    {
        // Get current user ID
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Unauthorized", 401);
            return;
        }

        // Validate time range
        if (req.EndTime <= req.StartTime)
        {
            ThrowError("End time must be after start time", 400);
            return;
        }

        if (req.StartTime < DateTime.UtcNow)
        {
            ThrowError("Cannot create reservation in the past", 400);
            return;
        }

        // Check if room exists
        var room = await _dbContext.Rooms.FindAsync(new object[] { req.RoomId }, ct);
        if (room == null)
        {
            ThrowError("Room not found", 404);
            return;
        }

        // Check for conflicts
        if (await _reservationService.HasConflictAsync(req.RoomId, req.StartTime, req.EndTime, null, ct))
        {
            // Find alternative rooms that are available
            var alternatives = await _reservationService.FindAlternativeRoomsAsync(
                req.RoomId, 
                req.StartTime, 
                req.EndTime, 
                ct);

            var conflictResponse = new ReservationConflictResponse
            {
                Message = "Room is already booked for the selected time slot",
                Alternatives = alternatives.Select(a => new AlternativeRoomSuggestion
                {
                    RoomId = a.RoomId,
                    RoomName = a.RoomName,
                    Capacity = a.Capacity,
                    Description = a.Description,
                    AvailableStartTime = a.AvailableStartTime,
                    AvailableEndTime = a.AvailableEndTime,
                    IsExactMatch = a.IsExactMatch
                }).ToList()
            };

            HttpContext.Response.StatusCode = 409;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsync(JsonSerializer.Serialize(conflictResponse), ct);
            return;
        }

        // Create reservation
        var reservation = new Reservation
        {
            RoomId = req.RoomId,
            UserId = userId,
            Title = req.Title,
            Description = req.Description,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            Status = ReservationStatus.Confirmed
        };

        _dbContext.Reservations.Add(reservation);
        await _dbContext.SaveChangesAsync(ct);

        // Load navigation properties
        await _dbContext.Entry(reservation).Reference(r => r.Room).LoadAsync(ct);
        await _dbContext.Entry(reservation).Reference(r => r.User).LoadAsync(ct);

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
