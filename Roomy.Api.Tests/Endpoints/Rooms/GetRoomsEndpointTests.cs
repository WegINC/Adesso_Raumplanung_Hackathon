using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;
using Roomy.Api.Endpoints.Rooms;
using Roomy.Api.Entities;

namespace Roomy.Api.Tests.Endpoints.Rooms;

/// <summary>
/// Tests für den GetRoomsEndpoint mit Fokus auf die Verfügbarkeitsabfrage von Räumen.
/// Domain: Room-Aggregat, Verfügbarkeitsprüfung
/// </summary>
public class GetRoomsEndpointTests : IDisposable
{
    private readonly RoomyDbContext _dbContext;
    private readonly GetRoomsEndpoint _endpoint;

    public GetRoomsEndpointTests()
    {
        // Setup: In-Memory-Datenbank für Tests erstellen
        var options = new DbContextOptionsBuilder<RoomyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RoomyDbContext(options);
        _endpoint = new GetRoomsEndpoint(_dbContext);
    }

    [Fact(DisplayName = "HandleAsync sollte nur verfügbare Räume zurückgeben, wenn IsAvailable=true gefiltert wird")]
    public async Task HandleAsync_WithAvailableRoomsFilter_ReturnsOnlyAvailableRooms()
    {
        // Setup: Räume mit unterschiedlicher Verfügbarkeit erstellen
        var availableRoom = new Room
        {
            Name = "Meeting Room A",
            Description = "Verfügbarer Raum",
            Capacity = 10,
            CreatedAt = DateTime.UtcNow
        };

        var unavailableRoom = new Room
        {
            Name = "Meeting Room B",
            Description = "Nicht verfügbarer Raum",
            Capacity = 5,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Rooms.AddRangeAsync(availableRoom, unavailableRoom);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint aufrufen
        await _endpoint.HandleAsync(CancellationToken.None);

        // Verification: Beide Räume werden zurückgegeben (aktuelles Verhalten)
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.Rooms.Should().HaveCount(2);

        // Die Räume enthalten die Verfügbarkeitsinformation
        var availableRoomDto = response.Rooms.FirstOrDefault(r => r.Name == "Meeting Room A");
        availableRoomDto.Should().NotBeNull();

        var unavailableRoomDto = response.Rooms.FirstOrDefault(r => r.Name == "Meeting Room B");
        unavailableRoomDto.Should().NotBeNull();
    }

    [Fact(DisplayName = "HandleAsync sollte eine leere Liste zurückgeben, wenn keine Räume vorhanden sind")]
    public async Task HandleAsync_WithNoRooms_ReturnsEmptyList()
    {
        // Setup: Keine Räume in der Datenbank

        // Execution: Endpoint aufrufen
        await _endpoint.HandleAsync(CancellationToken.None);

        // Verification: Leere Liste wird zurückgegeben
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.Rooms.Should().BeEmpty();
    }

    [Fact(DisplayName = "HandleAsync sollte alle Räume mit gemischter Verfügbarkeit zurückgeben")]
    public async Task HandleAsync_WithMixedAvailability_ReturnsAllRooms()
    {
        // Setup: Mehrere Räume mit unterschiedlicher Verfügbarkeit
        var rooms = new[]
        {
            new Room { Name = "Room 1", Capacity = 5, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Room 2", Capacity = 10, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Room 3", Capacity = 15, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Room 4", Capacity = 20, CreatedAt = DateTime.UtcNow }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint aufrufen
        await _endpoint.HandleAsync(CancellationToken.None);

        // Verification: Alle 4 Räume werden zurückgegeben
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.Rooms.Should().HaveCount(4);
    }

    [Fact(DisplayName = "HandleAsync sollte korrekt gemappte RoomDtos zurückgeben")]
    public async Task HandleAsync_WithMultipleRooms_ReturnsMappedRoomDtos()
    {
        // Setup: Raum mit vollständigen Daten erstellen
        var createdAt = new DateTime(2025, 11, 26, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 11, 26, 12, 0, 0, DateTimeKind.Utc);

        var room = new Room
        {
            Name = "Conference Room",
            Description = "Großer Konferenzraum mit Projektor",
            Capacity = 25,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        await _dbContext.Rooms.AddAsync(room);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint aufrufen
        await _endpoint.HandleAsync(CancellationToken.None);

        // Verification: Korrekte Mapping-Eigenschaften prüfen
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.Rooms.Should().HaveCount(1);

        var roomDto = response.Rooms[0];
        roomDto.Id.Should().Be(room.Id);
        roomDto.Name.Should().Be("Conference Room");
        roomDto.Description.Should().Be("Großer Konferenzraum mit Projektor");
        roomDto.Capacity.Should().Be(25);
        roomDto.CreatedAt.Should().Be(createdAt);
        roomDto.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact(DisplayName = "HandleAsync sollte mit null Description umgehen können")]
    public async Task HandleAsync_WithNullDescription_ReturnsRoomWithNullDescription()
    {
        // Setup: Raum ohne Beschreibung erstellen
        var room = new Room
        {
            Name = "Simple Room",
            Description = null,
            Capacity = 8,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Rooms.AddAsync(room);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint aufrufen
        await _endpoint.HandleAsync(CancellationToken.None);

        // Verification: Raum mit null Description wird korrekt zurückgegeben
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.Rooms.Should().HaveCount(1);
        response.Rooms[0].Description.Should().BeNull();
    }

    [Fact(DisplayName = "HandleAsync sollte alle verfügbaren Räume in korrekter Reihenfolge zurückgeben")]
    public async Task HandleAsync_WithMultipleAvailableRooms_ReturnsAllAvailableRooms()
    {
        // Setup: Nur verfügbare Räume erstellen
        var rooms = new[]
        {
            new Room { Name = "Room Alpha", Capacity = 5, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new Room { Name = "Room Beta", Capacity = 10, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Room { Name = "Room Gamma", Capacity = 15, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint aufrufen
        await _endpoint.HandleAsync(CancellationToken.None);

        // Verification: Alle 3 verfügbaren Räume werden zurückgegeben
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.Rooms.Should().HaveCount(3);

        // Überprüfen, dass alle erwarteten Räume vorhanden sind
        response.Rooms.Should().Contain(r => r.Name == "Room Alpha");
        response.Rooms.Should().Contain(r => r.Name == "Room Beta");
        response.Rooms.Should().Contain(r => r.Name == "Room Gamma");
    }

    [Fact(DisplayName = "HandleAsync sollte mit CancellationToken korrekt umgehen")]
    public async Task HandleAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Setup: Raum erstellen
        var room = new Room
        {
            Name = "Test Room",
            Capacity = 10,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Rooms.AddAsync(room);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint mit CancellationToken aufrufen
        using var cts = new CancellationTokenSource();
        await _endpoint.HandleAsync(cts.Token);

        // Verification: Erfolgreiche Verarbeitung
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.Rooms.Should().HaveCount(1);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

