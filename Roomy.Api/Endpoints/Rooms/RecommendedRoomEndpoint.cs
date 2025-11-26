using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;
using Roomy.Api.Entities;
using Roomy.Api.Services.LLM;

namespace Roomy.Api.Endpoints.Rooms;

/// <summary>
/// Endpoint für die Empfehlung eines verfügbaren Raums für morgen basierend auf Suchkriterien.
/// Domain: Room-Aggregat mit zeitbasierter Verfügbarkeitsprüfung und LLM-gestützter Empfehlungslogik.
/// </summary>
public class RecommendedRoomEndpoint : Endpoint<RecommendedRoomRequest, RecommendedRoomResponse>
{
    private readonly RoomyDbContext _dbContext;
    private readonly LlmRoomRecommendationService _llmService;

    public RecommendedRoomEndpoint(RoomyDbContext dbContext, LlmRoomRecommendationService llmService)
    {
        _dbContext = dbContext;
        _llmService = llmService;
    }

    public override void Configure()
    {
        Post("/api/rooms/recommended");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Empfiehlt einen verfügbaren Raum für morgen";
            s.Description = "Gibt den Namen eines verfügbaren Raums für morgen basierend auf dem Suchkriterium zurück";
            s.RequestParam(r => r.Criteria, "Suchkriterium (z.B. 'meeting', 'presentation', 'small')");
        });
    }

    public override async Task HandleAsync(RecommendedRoomRequest req, CancellationToken ct)
    {
        // Domain-Logik: Verfügbare Räume für morgen abfragen
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var tomorrowStart = tomorrow;
        var tomorrowEnd = tomorrow.AddDays(1);
        
        // Geschäftsregel: Räume laden mit ihren Reservierungen für morgen
        var allRooms = await _dbContext.Rooms
            .Include(r => r.Reservations)
            .ToListAsync(ct);

        // Geschäftsregel: Filtere Räume, die für morgen KEINE bestätigte Reservierung haben
        // Ein Raum ist verfügbar, wenn er keine Reservierung hat, die sich mit "morgen" überschneidet
        var availableRooms = allRooms
            .Where(r => !r.Reservations.Any(res => 
                res.Status == ReservationStatus.Confirmed &&
                res.StartTime < tomorrowEnd &&
                res.EndTime > tomorrowStart))
            .ToList();

        if (!availableRooms.Any())
        {
            ThrowError("Keine verfügbaren Räume für morgen gefunden", 404);
        }

        // Empfehlungslogik: Zuerst LLM-basierte Empfehlung versuchen
        Room? recommendedRoom = null;
        string? llmRecommendation = null;
        
        var (llmRoomId, llmResponse) = await _llmService.GetRecommendedRoomAsync(availableRooms, req.Criteria, ct);
        
        if (llmRoomId.HasValue)
        {
            // LLM hat eine Empfehlung gegeben
            recommendedRoom = availableRooms.FirstOrDefault(r => r.Id == llmRoomId.Value);
            llmRecommendation = llmResponse;
        }

        // Fallback: Regelbasierte Empfehlung wenn LLM fehlschlägt
        if (recommendedRoom == null)
        {
            recommendedRoom = SelectRecommendedRoom(availableRooms, req.Criteria);
        }

        Response = new RecommendedRoomResponse
        {
            RoomName = recommendedRoom.Name,
            RoomId = recommendedRoom.Id,
            Capacity = recommendedRoom.Capacity,
            Description = recommendedRoom.Description,
            LlmRecommendation = llmRecommendation
        };
    }

    /// <summary>
    /// Domain Service: Empfehlungslogik für die Auswahl des besten Raums.
    /// Geschäftsregeln:
    /// "small" oder "klein": Kleinster verfügbarer Raum (Capacity kleiner gleich 10).
    /// "large" oder "groß": Größter verfügbarer Raum (Capacity größer gleich 20).
    /// "meeting": Raum mit "meeting" oder "konferenz" im Namen/Beschreibung.
    /// Default: Erster verfügbarer Raum.
    /// </summary>
    private Room SelectRecommendedRoom(List<Room> availableRooms, string criteria)
    {
        var lowerCriteria = criteria.ToLowerInvariant().Trim();

        // Geschäftsregel: Beamer/Projektor-Raum (für Präsentationen)
        if (lowerCriteria.Contains("beamer") || lowerCriteria.Contains("projektor"))
        {
            var roomsWithBeamer = availableRooms
                .Where(r => r.Description != null && 
                           (r.Description.ToLowerInvariant().Contains("beamer") || 
                            r.Description.ToLowerInvariant().Contains("projektor")))
                .ToList();

            if (roomsWithBeamer.Any())
            {
                // Wenn Kapazität in der Anfrage erwähnt wird, berücksichtige diese
                var requestedCapacity = ExtractCapacityFromCriteria(lowerCriteria);
                if (requestedCapacity.HasValue)
                {
                    return roomsWithBeamer
                        .Where(r => r.Capacity >= requestedCapacity.Value)
                        .OrderBy(r => r.Capacity)
                        .FirstOrDefault() ?? roomsWithBeamer.First();
                }
                
                return roomsWithBeamer.First();
            }
        }

        // Geschäftsregel: Kleiner Raum
        if (lowerCriteria.Contains("small") || lowerCriteria.Contains("klein"))
        {
            return availableRooms
                .Where(r => r.Capacity <= 10)
                .OrderBy(r => r.Capacity)
                .FirstOrDefault() ?? availableRooms.OrderBy(r => r.Capacity).First();
        }

        // Geschäftsregel: Großer Raum
        if (lowerCriteria.Contains("large") || lowerCriteria.Contains("groß") || lowerCriteria.Contains("gross"))
        {
            return availableRooms
                .Where(r => r.Capacity >= 20)
                .OrderByDescending(r => r.Capacity)
                .FirstOrDefault() ?? availableRooms.OrderByDescending(r => r.Capacity).First();
        }

        // Geschäftsregel: Meeting-Raum
        if (lowerCriteria.Contains("meeting") || lowerCriteria.Contains("konferenz") || lowerCriteria.Contains("besprechung"))
        {
            return availableRooms
                .FirstOrDefault(r => 
                    r.Name.ToLowerInvariant().Contains("meeting") || 
                    r.Name.ToLowerInvariant().Contains("konferenz") ||
                    (r.Description != null && r.Description.ToLowerInvariant().Contains("meeting")) ||
                    (r.Description != null && r.Description.ToLowerInvariant().Contains("konferenz"))
                ) ?? availableRooms.First();
        }

        // Geschäftsregel: Präsentation
        if (lowerCriteria.Contains("presentation") || lowerCriteria.Contains("präsentation") || lowerCriteria.Contains("praesentation"))
        {
            return availableRooms
                .FirstOrDefault(r => 
                    r.Name.ToLowerInvariant().Contains("presentation") || 
                    r.Name.ToLowerInvariant().Contains("präsentation") ||
                    (r.Description != null && (r.Description.ToLowerInvariant().Contains("projektor") || r.Description.ToLowerInvariant().Contains("beamer")))
                ) ?? availableRooms.OrderByDescending(r => r.Capacity).First();
        }

        // Geschäftsregel: Nach Kapazität suchen (z.B. "5", "10", "20")
        if (int.TryParse(lowerCriteria, out int numericCapacity))
        {
            // Finde Raum mit passender oder nächst größerer Kapazität
            return availableRooms
                .Where(r => r.Capacity >= numericCapacity)
                .OrderBy(r => r.Capacity)
                .FirstOrDefault() ?? availableRooms.OrderBy(r => Math.Abs(r.Capacity - numericCapacity)).First();
        }

        // Default: Erster verfügbarer Raum
        return availableRooms.First();
    }

    /// <summary>
    /// Extrahiert Kapazitätszahlen aus natürlichsprachlichen Anfragen.
    /// Sucht nach Mustern wie "für 5 Leute", "5 Personen", "10 Menschen", etc.
    /// </summary>
    private int? ExtractCapacityFromCriteria(string lowerCriteria)
    {
        // Suche nach Zahlen gefolgt von "leute", "personen", "menschen", "teilnehmer"
        var patterns = new[]
        {
            @"für\s+(\d+)\s+(leute|personen|menschen|teilnehmer)",
            @"(\d+)\s+(leute|personen|menschen|teilnehmer)",
            @"(\d+)er\s+gruppe",
            @"gruppe\s+von\s+(\d+)"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(lowerCriteria, pattern);
            if (match.Success)
            {
                var numberGroup = match.Groups[1];
                if (int.TryParse(numberGroup.Value, out int capacity))
                {
                    return capacity;
                }
            }
        }

        return null;
    }
}

