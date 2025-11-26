using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Roomy.Api.Entities;

namespace Roomy.Api.Services.LLM;

/// <summary>
/// Service für LLM-basierte Raumempfehlungen.
/// Domain Service: Nutzt externe KI für intelligente Entscheidungsfindung.
/// </summary>
public class LlmRoomRecommendationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LlmRoomRecommendationService> _logger;
    private const string ApiUrl = "https://adesso-ai-hub.3asabc.de/v1/chat/completions";
    private const string ApiKey = "sk-efZkF-Kq4AlUXCvBJCV82Q";

    public LlmRoomRecommendationService(HttpClient httpClient, ILogger<LlmRoomRecommendationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        
        _logger.LogInformation("LlmRoomRecommendationService initialisiert mit API URL: {ApiUrl}", ApiUrl);
    }

    /// <summary>
    /// Empfiehlt einen Raum basierend auf Benutzeranfrage und verfügbaren Räumen via LLM.
    /// Geschäftslogik: LLM analysiert natürlichsprachliche Anfrage und Raumeigenschaften.
    /// </summary>
    public async Task<int?> GetRecommendedRoomIdAsync(List<Room> availableRooms, string userQuery, CancellationToken ct)
    {
        _logger.LogInformation("=== LLM Raumempfehlung gestartet ===");
        _logger.LogInformation("Benutzeranfrage: {UserQuery}", userQuery);
        _logger.LogInformation("Anzahl verfügbarer Räume: {RoomCount}", availableRooms.Count);
        
        try
        {
            // Erstelle Prompt mit verfügbaren Räumen
            var roomsInfo = BuildRoomsInformation(availableRooms);
            _logger.LogDebug("Raumliste für LLM erstellt: {RoomsInfo}", roomsInfo);
            var systemPrompt = @"Du bist ein intelligenter Assistent für Raumbuchungen. 
Deine Aufgabe ist es, basierend auf der Anfrage des Benutzers den am besten passenden Raum zu empfehlen.
Analysiere die Anforderungen (Kapazität, Ausstattung, Typ) und wähle den optimalen Raum aus.
Antworte mit dem Namen und der Ausstattung des empfohlenen Raums und sage uns, warum Du genau diesen Raum für optimal hälst.";

            var userPrompt = $@"Verfügbare Räume für morgen:
{roomsInfo}

Benutzeranfrage: ""{userQuery}""
Antworte mit dem Namen und der Ausstattung des empfohlenen Raums und sage uns, warum Du genau diesen Raum für optimal hälst";
// Welcher Raum passt am besten? Antworte nur mit der Raum-ID (Zahl).";

            var request = new LlmChatRequest
            {
                Model = "gpt-4o-mini",
                Messages = new List<LlmMessage>
                {
                    new() { Role = "system", Content = systemPrompt },
                    new() { Role = "user", Content = userPrompt }
                },
                MaxTokens = 5000,
                Temperature = 0.3 // Niedrige Temperature für konsistente Ergebnisse
            };

            var jsonContent = JsonSerializer.Serialize(request);

            _logger.LogInformation("Sende Request an LLM API: {ApiUrl}", ApiUrl);
            _logger.LogDebug("Request Payload: {RequestPayload}", jsonContent);

            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(ApiUrl, httpContent, ct);

            _logger.LogInformation("LLM API Response Status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("LLM API Fehler: StatusCode={StatusCode}, Error={ErrorContent}", 
                    response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug("LLM API Response Content: {ResponseContent}", responseContent);
            
            var llmResponse = JsonSerializer.Deserialize<LlmChatResponse>(responseContent);

            if (llmResponse?.Choices == null || llmResponse.Choices.Count == 0)
            {
                _logger.LogWarning("LLM Response enthält keine Choices");
                return null;
            }

            var llmRecommendation = llmResponse.Choices[0]?.Message?.Content?.Trim();
            _logger.LogInformation("LLM Empfehlung (roh): {LlmRecommendation}", llmRecommendation);
            
            // Extrahiere Raum-ID aus LLM-Antwort
            if (int.TryParse(llmRecommendation, out int roomId))
            {
                _logger.LogInformation("LLM hat Raum-ID empfohlen: {RoomId}", roomId);
                
                // Validiere, dass die ID in der verfügbaren Liste ist
                if (availableRooms.Any(r => r.Id == roomId))
                {
                    var recommendedRoom = availableRooms.First(r => r.Id == roomId);
                    _logger.LogInformation("✓ Raum-ID {RoomId} ist verfügbar: {RoomName}", 
                        roomId, recommendedRoom.Name);
                    _logger.LogInformation("=== LLM Empfehlung erfolgreich ===");
                    return roomId;
                }
                else
                {
                    _logger.LogWarning("✗ Raum-ID {RoomId} ist nicht in der verfügbaren Liste", roomId);
                }
            }
            else
            {
                _logger.LogWarning("Konnte Raum-ID nicht aus LLM-Antwort parsen: {LlmRecommendation}", 
                    llmRecommendation);
            }

            _logger.LogInformation("=== LLM Empfehlung fehlgeschlagen, Fallback wird verwendet ===");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim LLM Service Call: {ErrorMessage}", ex.Message);
            _logger.LogInformation("=== LLM Empfehlung mit Exception beendet, Fallback wird verwendet ===");
            return null;
        }
    }

    /// <summary>
    /// Erstellt strukturierte Information über verfügbare Räume für den LLM-Prompt.
    /// </summary>
    private string BuildRoomsInformation(List<Room> rooms)
    {
        var sb = new StringBuilder();
        foreach (var room in rooms)
        {
            sb.AppendLine($"ID: {room.Id}");
            sb.AppendLine($"  Name: {room.Name}");
            sb.AppendLine($"  Kapazität: {room.Capacity} Personen");
            
            if (!string.IsNullOrWhiteSpace(room.Description))
            {
                sb.AppendLine($"  Beschreibung: {room.Description}");
            }
            
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

