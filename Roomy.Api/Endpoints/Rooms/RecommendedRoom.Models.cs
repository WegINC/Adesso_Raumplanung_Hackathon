namespace Roomy.Api.Endpoints.Rooms;

/// <summary>
/// Request für die Raumempfehlung mit Suchkriterium.
/// </summary>
public class RecommendedRoomRequest
{
    /// <summary>
    /// Suchkriterium für die Raumempfehlung (z.B. "meeting", "presentation", "small").
    /// </summary>
    public string Criteria { get; set; } = string.Empty;
}

/// <summary>
/// Response mit dem Namen des empfohlenen Raums.
/// </summary>
public class RecommendedRoomResponse
{
    /// <summary>
    /// Name des empfohlenen Raums.
    /// </summary>
    public string RoomName { get; set; } = string.Empty;
    
    /// <summary>
    /// ID des empfohlenen Raums.
    /// </summary>
    public int RoomId { get; set; }
    
    /// <summary>
    /// Kapazität des empfohlenen Raums.
    /// </summary>
    public int Capacity { get; set; }
    
    /// <summary>
    /// Beschreibung des empfohlenen Raums (optional).
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Ausführliche Empfehlung vom LLM mit Ausstattung und Begründung (optional).
    /// </summary>
    public string? LlmRecommendation { get; set; }
}

