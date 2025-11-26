using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;
using Roomy.Api.Entities;
using System.Security.Claims;

namespace Roomy.Api.Endpoints.Reservations;

public class GetMyReservationsEndpoint : EndpointWithoutRequest<GetReservationsResponse>
{
    private readonly RoomyDbContext _dbContext;

    public GetMyReservationsEndpoint(RoomyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/api/reservations/my");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Get current user ID
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Unauthorized", 401);
            return;
        }

        var reservations = await _dbContext.Reservations
            .Include(r => r.Room)
            .Include(r => r.User)
            .Where(r => r.UserId == userId && r.Status == ReservationStatus.Confirmed)
            .OrderBy(r => r.StartTime)
            .Select(r => new ReservationResponse
            {
                Id = r.Id,
                RoomId = r.RoomId,
                RoomName = r.Room.Name,
                UserId = r.UserId,
                UserName = $"{r.User.FirstName} {r.User.LastName}",
                Title = r.Title,
                Description = r.Description,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync(ct);

        Response = new GetReservationsResponse { Reservations = reservations };
    }
}
