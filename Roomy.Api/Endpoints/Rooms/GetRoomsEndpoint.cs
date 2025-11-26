using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;

namespace Roomy.Api.Endpoints.Rooms;

public class GetRoomsEndpoint : EndpointWithoutRequest<GetRoomsResponse>
{
    private readonly RoomyDbContext _dbContext;

    public GetRoomsEndpoint(RoomyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/api/rooms");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var rooms = await _dbContext.Rooms
            .Select(r => new RoomDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Capacity = r.Capacity,
                IsAvailable = r.IsAvailable,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync(ct);

        Response = new GetRoomsResponse { Rooms = rooms };
    }
}
