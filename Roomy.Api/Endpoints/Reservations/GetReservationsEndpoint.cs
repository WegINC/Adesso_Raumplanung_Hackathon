using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;
using Roomy.Api.Entities;

namespace Roomy.Api.Endpoints.Reservations;

public class GetReservationsRequest
{
    public int? RoomId { get; set; }
    public int? UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class GetReservationsResponse
{
    public List<ReservationResponse> Reservations { get; set; } = new();
}

public class GetReservationsEndpoint : Endpoint<GetReservationsRequest, GetReservationsResponse>
{
    private readonly RoomyDbContext _dbContext;

    public GetReservationsEndpoint(RoomyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/api/reservations");
        // Require authentication
    }

    public override async Task HandleAsync(GetReservationsRequest req, CancellationToken ct)
    {
        var query = _dbContext.Reservations
            .Include(r => r.Room)
            .Include(r => r.User)
            .Where(r => r.Status == ReservationStatus.Confirmed)
            .AsQueryable();

        if (req.RoomId.HasValue)
        {
            query = query.Where(r => r.RoomId == req.RoomId.Value);
        }

        if (req.UserId.HasValue)
        {
            query = query.Where(r => r.UserId == req.UserId.Value);
        }

        if (req.StartDate.HasValue)
        {
            query = query.Where(r => r.EndTime >= req.StartDate.Value);
        }

        if (req.EndDate.HasValue)
        {
            query = query.Where(r => r.StartTime <= req.EndDate.Value);
        }

        var reservations = await query
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
