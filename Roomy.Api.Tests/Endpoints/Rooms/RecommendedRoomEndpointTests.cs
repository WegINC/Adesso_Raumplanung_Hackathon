using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;
using Roomy.Api.Endpoints.Rooms;
using Roomy.Api.Entities;
using Xunit;

namespace Roomy.Api.Tests.Endpoints.Rooms;

/// <summary>
/// Tests für den RecommendedRoomEndpoint mit Fokus auf die Empfehlungslogik für verfügbare Räume.
/// Domain: Room-Aggregat, Verfügbarkeitsprüfung, Empfehlungsalgorithmus
/// </summary>
public class RecommendedRoomEndpointTests : IDisposable
{
    private readonly RoomyDbContext _dbContext;
    private readonly RecommendedRoomEndpoint _endpoint;

    public RecommendedRoomEndpointTests()
    {
        // Setup: In-Memory-Datenbank für Tests erstellen
        var options = new DbContextOptionsBuilder<RoomyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RoomyDbContext(options);
        _endpoint = new RecommendedRoomEndpoint(_dbContext);
    }

    [Fact(DisplayName = "HandleAsync sollte kleinen Raum empfehlen wenn 'small' als Kriterium angegeben wird")]
    public async Task HandleAsync_WithSmallCriteria_ReturnsSmallestRoom()
    {
        // Setup: Räume mit unterschiedlicher Größe erstellen
        var rooms = new[]
        {
            new Room { Name = "Large Room", Capacity = 25, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Medium Room", Capacity = 15, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Small Room", Capacity = 5, IsAvailable = true, CreatedAt = DateTime.UtcNow }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint mit "small" Kriterium aufrufen
        var request = new RecommendedRoomRequest { Criteria = "small" };
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Verification: Kleinster Raum wird empfohlen
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.RoomName.Should().Be("Small Room");
        response.Capacity.Should().Be(5);
    }

    [Fact(DisplayName = "HandleAsync sollte großen Raum empfehlen wenn 'large' als Kriterium angegeben wird")]
    public async Task HandleAsync_WithLargeCriteria_ReturnsLargestRoom()
    {
        // Setup: Räume mit unterschiedlicher Größe erstellen
        var rooms = new[]
        {
            new Room { Name = "Small Room", Capacity = 5, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Large Conference", Capacity = 30, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Medium Room", Capacity = 15, IsAvailable = true, CreatedAt = DateTime.UtcNow }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint mit "large" Kriterium aufrufen
        var request = new RecommendedRoomRequest { Criteria = "large" };
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Verification: Größter Raum wird empfohlen
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.RoomName.Should().Be("Large Conference");
        response.Capacity.Should().Be(30);
    }

    [Fact(DisplayName = "HandleAsync sollte Meeting-Raum empfehlen wenn 'meeting' als Kriterium angegeben wird")]
    public async Task HandleAsync_WithMeetingCriteria_ReturnsMeetingRoom()
    {
        // Setup: Räume mit unterschiedlichen Namen erstellen
        var rooms = new[]
        {
            new Room { Name = "Storage Room", Capacity = 5, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Meeting Room A", Capacity = 10, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Workshop Space", Capacity = 20, IsAvailable = true, CreatedAt = DateTime.UtcNow }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint mit "meeting" Kriterium aufrufen
        var request = new RecommendedRoomRequest { Criteria = "meeting" };
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Verification: Meeting-Raum wird empfohlen
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.RoomName.Should().Be("Meeting Room A");
    }

    [Fact(DisplayName = "HandleAsync sollte Exception werfen wenn keine verfügbaren Räume existieren")]
    public async Task HandleAsync_WithNoAvailableRooms_ThrowsValidationException()
    {
        // Setup: Nur nicht verfügbare Räume erstellen
        var rooms = new[]
        {
            new Room { Name = "Room 1", Capacity = 10, IsAvailable = false, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Room 2", Capacity = 15, IsAvailable = false, CreatedAt = DateTime.UtcNow }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution & Verification: Exception wird geworfen
        var request = new RecommendedRoomRequest { Criteria = "any" };
        var exception = await Assert.ThrowsAsync<FastEndpoints.ValidationFailureException>(
            async () => await _endpoint.HandleAsync(request, CancellationToken.None)
        );

        // Verification: Fehlermeldung enthält erwarteten Text
        exception.Message.Should().Contain("Keine verfügbaren Räume");
    }

    [Fact(DisplayName = "HandleAsync sollte Raum basierend auf Kapazitätszahl empfehlen")]
    public async Task HandleAsync_WithNumericCapacityCriteria_ReturnsRoomWithMatchingCapacity()
    {
        // Setup: Räume mit unterschiedlicher Kapazität
        var rooms = new[]
        {
            new Room { Name = "Room 5", Capacity = 5, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Room 10", Capacity = 10, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Room 15", Capacity = 15, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Room 20", Capacity = 20, IsAvailable = true, CreatedAt = DateTime.UtcNow }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint mit Kapazität "12" aufrufen
        var request = new RecommendedRoomRequest { Criteria = "12" };
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Verification: Nächst größerer Raum (15) wird empfohlen
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.RoomName.Should().Be("Room 15");
        response.Capacity.Should().Be(15);
    }

    [Fact(DisplayName = "HandleAsync sollte deutschen Begriff 'klein' für kleine Räume verstehen")]
    public async Task HandleAsync_WithGermanSmallCriteria_ReturnsSmallestRoom()
    {
        // Setup: Räume erstellen
        var rooms = new[]
        {
            new Room { Name = "Großer Raum", Capacity = 30, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Kleiner Raum", Capacity = 6, IsAvailable = true, CreatedAt = DateTime.UtcNow }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint mit deutschem Kriterium "klein" aufrufen
        var request = new RecommendedRoomRequest { Criteria = "klein" };
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Verification: Kleiner Raum wird empfohlen
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.RoomName.Should().Be("Kleiner Raum");
        response.Capacity.Should().Be(6);
    }

    [Fact(DisplayName = "HandleAsync sollte Präsentationsraum empfehlen wenn 'presentation' als Kriterium angegeben wird")]
    public async Task HandleAsync_WithPresentationCriteria_ReturnsPresentationRoom()
    {
        // Setup: Räume mit Beschreibungen erstellen
        var rooms = new[]
        {
            new Room { Name = "Standard Room", Capacity = 10, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room 
            { 
                Name = "Presentation Hall", 
                Description = "Raum mit Projektor und Beamer",
                Capacity = 25, 
                IsAvailable = true, 
                CreatedAt = DateTime.UtcNow 
            }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint mit "presentation" Kriterium aufrufen
        var request = new RecommendedRoomRequest { Criteria = "presentation" };
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Verification: Präsentationsraum wird empfohlen
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.RoomName.Should().Be("Presentation Hall");
        response.Description.Should().Contain("Projektor");
    }

    [Fact(DisplayName = "HandleAsync sollte ersten verfügbaren Raum zurückgeben wenn Kriterium nicht erkannt wird")]
    public async Task HandleAsync_WithUnknownCriteria_ReturnsFirstAvailableRoom()
    {
        // Setup: Mehrere verfügbare Räume
        var rooms = new[]
        {
            new Room { Name = "First Room", Capacity = 10, IsAvailable = true, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Room { Name = "Second Room", Capacity = 15, IsAvailable = true, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint mit unbekanntem Kriterium aufrufen
        var request = new RecommendedRoomRequest { Criteria = "xyz123" };
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Verification: Erster Raum wird empfohlen
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.RoomName.Should().NotBeEmpty();
        response.Capacity.Should().BeGreaterThan(0);
    }

    [Fact(DisplayName = "HandleAsync sollte nur verfügbare Räume berücksichtigen")]
    public async Task HandleAsync_WithMixedAvailability_ReturnsOnlyAvailableRoom()
    {
        // Setup: Verfügbare und nicht verfügbare Räume
        var rooms = new[]
        {
            new Room { Name = "Unavailable Small", Capacity = 5, IsAvailable = false, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Available Medium", Capacity = 10, IsAvailable = true, CreatedAt = DateTime.UtcNow }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint mit "small" Kriterium aufrufen
        var request = new RecommendedRoomRequest { Criteria = "small" };
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Verification: Nur verfügbarer Raum wird empfohlen (auch wenn nicht der kleinste)
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.RoomName.Should().Be("Available Medium");
        response.Capacity.Should().Be(10);
    }

    [Fact(DisplayName = "HandleAsync sollte Konferenzraum mit deutschem Begriff 'konferenz' finden")]
    public async Task HandleAsync_WithGermanKonferenzCriteria_ReturnsConferenceRoom()
    {
        // Setup: Räume mit deutschen Namen
        var rooms = new[]
        {
            new Room { Name = "Lagerraum", Capacity = 5, IsAvailable = true, CreatedAt = DateTime.UtcNow },
            new Room { Name = "Konferenzraum A", Capacity = 20, IsAvailable = true, CreatedAt = DateTime.UtcNow }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint mit "konferenz" Kriterium aufrufen
        var request = new RecommendedRoomRequest { Criteria = "konferenz" };
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Verification: Konferenzraum wird empfohlen
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.RoomName.Should().Be("Konferenzraum A");
    }

    [Fact(DisplayName = "HandleAsync sollte Response mit allen RoomDto-Properties zurückgeben")]
    public async Task HandleAsync_WithValidRequest_ReturnsCompleteRoomDto()
    {
        // Setup: Raum mit allen Properties erstellen
        var room = new Room
        {
            Name = "Test Conference Room",
            Description = "Ein vollständig ausgestatteter Konferenzraum",
            Capacity = 15,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Rooms.AddAsync(room);
        await _dbContext.SaveChangesAsync();

        // Execution: Endpoint aufrufen
        var request = new RecommendedRoomRequest { Criteria = "any" };
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Verification: Alle Properties sind korrekt gemappt
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.RoomId.Should().Be(room.Id);
        response.RoomName.Should().Be("Test Conference Room");
        response.Description.Should().Be("Ein vollständig ausgestatteter Konferenzraum");
        response.Capacity.Should().Be(15);
    }

    [Fact(DisplayName = "HandleAsync sollte Raum mit Beamer für natürlichsprachliche Anfrage empfehlen")]
    public async Task HandleAsync_WithNaturalLanguageBeamerRequest_ReturnsRoomWithBeamer()
    {
        // Setup: Verschiedene Räume mit unterschiedlichen Ausstattungen erstellen
        var rooms = new[]
        {
            new Room 
            { 
                Name = "Kleiner Besprechungsraum",
                Description = "Einfacher Raum ohne Equipment",
                Capacity = 4,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow 
            },
            new Room 
            { 
                Name = "Präsentationsraum A",
                Description = "Moderner Raum mit Beamer und Whiteboard",
                Capacity = 8,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow 
            },
            new Room 
            { 
                Name = "Großer Konferenzraum",
                Description = "Raum mit Projektor, Sound-System und Video-Konferenz",
                Capacity = 20,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow 
            },
            new Room 
            { 
                Name = "Meeting Room B",
                Description = "Standard Meeting-Raum",
                Capacity = 6,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow 
            }
        };

        await _dbContext.Rooms.AddRangeAsync(rooms);
        await _dbContext.SaveChangesAsync();

        // Execution: Natürlichsprachliche Anfrage "Ich möchte einen Raum mit Beamer für 5 Leute"
        var request = new RecommendedRoomRequest { Criteria = "Ich möchte einen Raum mit Beamer für 5 Leute" };
        await _endpoint.HandleAsync(request, CancellationToken.None);

        // Verification: Raum mit Beamer und ausreichender Kapazität wird empfohlen
        var response = _endpoint.Response;
        response.Should().NotBeNull();
        response.RoomName.Should().NotBeEmpty();
        
        // Der empfohlene Raum sollte "Beamer" oder "Projektor" in der Beschreibung haben
        // und mindestens Kapazität für 5 Personen
        response.Capacity.Should().BeGreaterThanOrEqualTo(5);
        
        // Prüfe, dass es einer der Räume mit Beamer/Projektor ist
        var roomsWithBeamer = new[] { "Präsentationsraum A", "Großer Konferenzraum" };
        roomsWithBeamer.Should().Contain(response.RoomName, 
            "weil die Anfrage explizit nach einem Raum mit Beamer fragt");
        
        // Zusätzliche Validierung: Description sollte Equipment erwähnen
        response.Description.Should().NotBeNull();
        var hasBeamerOrProjector = response.Description!.ToLowerInvariant().Contains("beamer") || 
                                    response.Description!.ToLowerInvariant().Contains("projektor");
        hasBeamerOrProjector.Should().BeTrue("weil der Raum einen Beamer haben soll");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

