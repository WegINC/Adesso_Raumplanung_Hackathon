using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;
using Roomy.Api.Entities;

namespace Roomy.Api.Services;

public interface IReservationService
{
    Task<bool> HasConflictAsync(int roomId, DateTime startTime, DateTime endTime, int? excludeReservationId = null, CancellationToken ct = default);
    Task<List<Reservation>> GetConflictingReservationsAsync(int roomId, DateTime startTime, DateTime endTime, int? excludeReservationId = null, CancellationToken ct = default);
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime startTime, DateTime endTime, int? excludeReservationId = null, CancellationToken ct = default);
    Task<List<AlternativeRoom>> FindAlternativeRoomsAsync(int requestedRoomId, DateTime requestedStartTime, DateTime requestedEndTime, CancellationToken ct = default);
}

public class ReservationService : IReservationService
{
    private readonly RoomyDbContext _dbContext;

    public ReservationService(RoomyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasConflictAsync(int roomId, DateTime startTime, DateTime endTime, int? excludeReservationId = null, CancellationToken ct = default)
    {
        var conflictingReservations = await GetConflictingReservationsAsync(roomId, startTime, endTime, excludeReservationId, ct);
        return conflictingReservations.Any();
    }

    public async Task<List<Reservation>> GetConflictingReservationsAsync(int roomId, DateTime startTime, DateTime endTime, int? excludeReservationId = null, CancellationToken ct = default)
    {
        var query = _dbContext.Reservations
            .Where(r => r.RoomId == roomId 
                && r.Status == ReservationStatus.Confirmed
                && r.StartTime < endTime 
                && r.EndTime > startTime);

        if (excludeReservationId.HasValue)
        {
            query = query.Where(r => r.Id != excludeReservationId.Value);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime startTime, DateTime endTime, int? excludeReservationId = null, CancellationToken ct = default)
    {
        return !await HasConflictAsync(roomId, startTime, endTime, excludeReservationId, ct);
    }

    public async Task<List<AlternativeRoom>> FindAlternativeRoomsAsync(int requestedRoomId, DateTime requestedStartTime, DateTime requestedEndTime, CancellationToken ct = default)
    {
        var alternatives = new List<AlternativeRoom>();
        
        // Get the requested room to compare capacity
        var requestedRoom = await _dbContext.Rooms.FindAsync(new object[] { requestedRoomId }, ct);
        if (requestedRoom == null)
        {
            return alternatives;
        }
        
        // Get all rooms except the requested one
        var allRooms = await _dbContext.Rooms
            .Where(r => r.Id != requestedRoomId)
            .OrderBy(r => Math.Abs(r.Capacity - requestedRoom.Capacity)) // Sort by similar capacity
            .ToListAsync(ct);
        
        // Check which rooms are available at the exact requested time
        foreach (var room in allRooms)
        {
            var isAvailable = await IsRoomAvailableAsync(room.Id, requestedStartTime, requestedEndTime, null, ct);
            
            if (isAvailable)
            {
                alternatives.Add(new AlternativeRoom
                {
                    RoomId = room.Id,
                    RoomName = room.Name,
                    Capacity = room.Capacity,
                    Description = room.Description,
                    AvailableStartTime = requestedStartTime,
                    AvailableEndTime = requestedEndTime,
                    IsExactMatch = true
                });
            }
        }
        
        // If we found exact matches, return them (up to 5)
        if (alternatives.Any())
        {
            return alternatives.Take(5).ToList();
        }
        
        // If no rooms available at exact time, find nearby time slots for other rooms
        var timeWindow = TimeSpan.FromHours(2); // Search within 2 hours before/after
        var duration = requestedEndTime - requestedStartTime;
        
        foreach (var room in allRooms)
        {
            // Check slots around the requested time
            var searchTimes = new List<DateTime>
            {
                requestedStartTime.AddMinutes(-30),
                requestedStartTime.AddMinutes(-60),
                requestedStartTime.AddMinutes(30),
                requestedStartTime.AddMinutes(60),
                requestedStartTime.AddMinutes(-90),
                requestedStartTime.AddMinutes(90)
            };
            
            foreach (var searchStart in searchTimes)
            {
                var searchEnd = searchStart.Add(duration);
                
                // Skip if outside reasonable time window
                if (Math.Abs((searchStart - requestedStartTime).TotalHours) > 2)
                {
                    continue;
                }
                
                // Skip if outside business hours (8 AM - 6 PM)
                if (searchStart.Hour < 8 || searchEnd.Hour > 18 || (searchEnd.Hour == 18 && searchEnd.Minute > 0))
                {
                    continue;
                }
                
                var isAvailable = await IsRoomAvailableAsync(room.Id, searchStart, searchEnd, null, ct);
                
                if (isAvailable)
                {
                    alternatives.Add(new AlternativeRoom
                    {
                        RoomId = room.Id,
                        RoomName = room.Name,
                        Capacity = room.Capacity,
                        Description = room.Description,
                        AvailableStartTime = searchStart,
                        AvailableEndTime = searchEnd,
                        IsExactMatch = false
                    });
                    
                    break; // Found one slot for this room, move to next room
                }
            }
            
            // Limit results
            if (alternatives.Count >= 5)
            {
                break;
            }
        }
        
        return alternatives;
    }
}

public class AlternativeRoom
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public DateTime AvailableStartTime { get; set; }
    public DateTime AvailableEndTime { get; set; }
    public bool IsExactMatch { get; set; }
}
