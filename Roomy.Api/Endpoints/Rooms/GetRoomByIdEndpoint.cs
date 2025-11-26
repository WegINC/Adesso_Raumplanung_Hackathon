using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;

namespace Roomy.Api.Endpoints.Rooms;

public class GetRoomByIdEndpoint : Endpoint<GetRoomByIdRequest, RoomDto>
{
    private readonly RoomyDbContext _dbContext;

    public GetRoomByIdEndpoint(RoomyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/api/rooms/{id}");
        // Require authentication
    }

    public override async Task HandleAsync(GetRoomByIdRequest req, CancellationToken ct)
    {
        var room = await _dbContext.Rooms
            .Where(r => r.Id == req.Id)
            .Select(r => new RoomDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Capacity = r.Capacity,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (room == null)
        {
            ThrowError("Room not found", 404);
            return;
        }

        Response = room;
    }
}

public class GetRoomByIdRequest
{
    public int Id { get; set; }
}
