using FastEndpoints;
using Roomy.Api.Data;
using Roomy.Api.Entities;

namespace Roomy.Api.Endpoints.Rooms;

public class CreateRoomEndpoint : Endpoint<CreateRoomRequest, RoomDto>
{
    private readonly RoomyDbContext _dbContext;

    public CreateRoomEndpoint(RoomyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Post("/api/rooms");
        Roles("Administrator");
    }

    public override async Task HandleAsync(CreateRoomRequest req, CancellationToken ct)
    {
        var room = new Room
        {
            Name = req.Name,
            Description = req.Description,
            Capacity = req.Capacity
        };

        _dbContext.Rooms.Add(room);
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
