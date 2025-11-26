using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;

namespace Roomy.Api.Endpoints.Rooms;

public class DeleteRoomEndpoint : Endpoint<DeleteRoomRequest, EmptyResponse>
{
    private readonly RoomyDbContext _dbContext;

    public DeleteRoomEndpoint(RoomyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Delete("/api/rooms/{id}");
        Roles("Administrator");
    }

    public override async Task HandleAsync(DeleteRoomRequest req, CancellationToken ct)
    {
        var room = await _dbContext.Rooms
            .Include(r => r.Reservations.Where(res => res.Status == Entities.ReservationStatus.Confirmed))
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct);
        
        if (room == null)
        {
            ThrowError("Room not found", 404);
            return;
        }

        // Check if room has any confirmed reservations
        if (room.Reservations.Any())
        {
            ThrowError("Cannot delete room with existing confirmed reservations", 409);
            return;
        }

        _dbContext.Rooms.Remove(room);
        await _dbContext.SaveChangesAsync(ct);

        HttpContext.Response.StatusCode = 204;
    }
}

public class DeleteRoomRequest
{
    public int Id { get; set; }
}
