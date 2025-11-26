using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;

namespace Roomy.Api.Endpoints.Rooms;

public class UpdateRoomEndpoint : Endpoint<UpdateRoomRequest, RoomDto>
{
    private readonly RoomyDbContext _dbContext;

    public UpdateRoomEndpoint(RoomyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Put("/api/rooms/{id}");
        Roles("Administrator");
    }

    public override async Task HandleAsync(UpdateRoomRequest req, CancellationToken ct)
    {
        var room = await _dbContext.Rooms.FindAsync(new object[] { req.Id }, ct);
        
        if (room == null)
        {
            ThrowError("Room not found", 404);
            return;
        }

        room.Name = req.Name;
        room.Description = req.Description;
        room.Capacity = req.Capacity;
        room.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        Response = new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            Description = room.Description,
            Capacity = room.Capacity,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt
        };
    }
}
