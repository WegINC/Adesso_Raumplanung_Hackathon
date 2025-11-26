# LLM-Integration für Raumempfehlungen

## Übersicht

Die Roomy.Api nutzt einen Large Language Model (LLM) Service für intelligente Raumempfehlungen basierend auf natürlichsprachlichen Anfragen.

## Architektur

### Domain-Driven Design Prinzipien

**Domain Service**: `LlmRoomRecommendationService`
- Verantwortlich für die Kommunikation mit der externen LLM-API
- Übersetzt Geschäftslogik (Raumeigenschaften) in strukturierte Prompts
- Validiert LLM-Antworten gegen Geschäftsregeln

**Geschäftsregeln**:
1. Nur verfügbare Räume werden dem LLM präsentiert
2. LLM-Empfehlung muss ein verfügbarer Raum sein (Validierung)
3. Fallback auf regelbasierte Logik bei LLM-Fehlern (Resilienz)

### SOLID-Prinzipien

- **Single Responsibility**: Service hat eine klare Aufgabe - LLM-Kommunikation
- **Dependency Inversion**: HttpClient wird via DI injiziert
- **Open/Closed**: Endpoint kann weitere Empfehlungsstrategien hinzufügen

## API-Endpunkt

### POST /api/rooms/recommended

Empfiehlt einen verfügbaren Raum für morgen basierend auf natürlichsprachlicher Anfrage.

**Request:**
```json
{
  "criteria": "Ich möchte einen Raum mit Beamer für 5 Leute"
}
```

**Response:**
```json
{
  "roomName": "Präsentationsraum A",
  "roomId": 2,
  "capacity": 8,
  "description": "Moderner Raum mit Beamer und Whiteboard"
}
```

## LLM-Integration Details

### API-Konfiguration

- **Endpoint**: `https://adesso-ai-hub.3asabc.de/v1/chat/completions`
- **Modell**: `gpt-4o-mini`
- **Authentifizierung**: Bearer Token (hardcoded für Demo)
- **Temperature**: 0.3 (niedrig für konsistente Ergebnisse)

### Prompt-Engineering

Der Service sendet zwei Nachrichten an das LLM:

1. **System Prompt**: Definiert die Rolle und Antwortformat
   - Assistent für Raumbuchungen
   - Muss nur mit Raum-ID antworten

2. **User Prompt**: Enthält strukturierte Informationen
   - Liste aller verfügbaren Räume (ID, Name, Kapazität, Beschreibung)
   - Benutzeranfrage
   - Klare Anweisung zur Antwort

**Beispiel-Prompt:**
```
Verfügbare Räume für morgen:
ID: 1
  Name: Kleiner Besprechungsraum
  Kapazität: 4 Personen
  Beschreibung: Einfacher Raum ohne Equipment

ID: 2
  Name: Präsentationsraum A
  Kapazität: 8 Personen
  Beschreibung: Moderner Raum mit Beamer und Whiteboard

Benutzeranfrage: "Ich möchte einen Raum mit Beamer für 5 Leute"

Welcher Raum passt am besten? Antworte nur mit der Raum-ID (Zahl).
```

### Fehlerbehandlung & Resilienz

**Mehrschichtige Fehlerbehandlung:**

1. **LLM-API-Fehler**: Bei HTTP-Fehler wird null zurückgegeben
2. **Parse-Fehler**: Wenn LLM-Antwort keine gültige ID ist
3. **Validierung**: Wenn ID nicht in verfügbaren Räumen
4. **Fallback**: Regelbasierte Logik übernimmt

**Resilienz-Pattern:**
```csharp
// LLM-Empfehlung versuchen
var llmRoomId = await _llmService.GetRecommendedRoomIdAsync(...);

if (llmRoomId.HasValue)
{
    recommendedRoom = availableRooms.FirstOrDefault(r => r.Id == llmRoomId.Value);
}

// Fallback auf Regeln
if (recommendedRoom == null)
{
    recommendedRoom = SelectRecommendedRoom(availableRooms, criteria);
}
```

## Vorteile der Hybrid-Architektur

### LLM-basierte Empfehlung

**Vorteile:**
- Versteht natürlichsprachliche Anfragen
- Kontextuelle Analyse (z.B. "Beamer für 5 Leute")
- Kann implizite Anforderungen erkennen
- Flexibel bei unerwarteten Anfragen

**Nachteile:**
- Externe Abhängigkeit
- Latenz (API-Call)
- Kosten pro Anfrage
- Potenzielle Fehler

### Regelbasierte Fallback-Logik

**Vorteile:**
- Deterministisch und vorhersehbar
- Keine externen Abhängigkeiten
- Sofortige Antwort
- Keine zusätzlichen Kosten

**Nachteile:**
- Begrenzte Flexibilität
- Manuell zu wartende Regeln
- Weniger intelligent bei komplexen Anfragen

## Sicherheit & Best Practices

### Aktuelle Implementierung (Demo)

⚠️ **Achtung**: API-Key ist aktuell **hardcoded** für Demo-Zwecke!

```csharp
private const string ApiKey = "sk-efZkF-Kq4AlUXCvBJCV82Q";
```

### Production-Ready Empfehlungen

**1. API-Key in Konfiguration auslagern:**
```csharp
// appsettings.json
{
  "LlmSettings": {
    "ApiUrl": "https://adesso-ai-hub.3asabc.de/v1/chat/completions",
    "ApiKey": "sk-..."
  }
}

// Service
public LlmRoomRecommendationService(HttpClient httpClient, IOptions<LlmSettings> settings)
{
    _httpClient = httpClient;
    _apiUrl = settings.Value.ApiUrl;
    _apiKey = settings.Value.ApiKey;
}
```

**2. Secret Management:**
- Verwende Azure Key Vault / AWS Secrets Manager
- Umgebungsvariablen für Container-Deployments
- User Secrets für lokale Entwicklung

**3. Rate Limiting:**
```csharp
services.AddHttpClient<LlmRoomRecommendationService>()
    .AddPolicyHandler(Policy.RateLimitAsync(10, TimeSpan.FromMinutes(1)));
```

**4. Caching:**
- Cache häufige Anfragen
- TTL basierend auf Verfügbarkeit
- Reduziert API-Calls und Kosten

**5. Monitoring & Logging:**
```csharp
// Strukturiertes Logging
_logger.LogInformation("LLM recommendation requested", 
    new { UserQuery = userQuery, AvailableRooms = availableRooms.Count });

_logger.LogWarning("LLM API failed, using fallback", 
    new { StatusCode = response.StatusCode, Error = errorContent });
```

## Performance-Überlegungen

### Latenz

- **LLM-Call**: ~500-2000ms
- **Regelbasiert**: <10ms
- **Empfehlung**: Asynchrone Verarbeitung minimiert Blocking

### Optimierungen

1. **Prompt-Optimierung**: Nur relevante Rauminformationen senden
2. **Max Tokens**: Niedrig halten (50 Tokens ausreichend für ID)
3. **Temperature**: 0.3 für konsistente, schnelle Antworten
4. **Parallel Processing**: Bei Batch-Anfragen möglich

## Testing

### Unit Tests

```csharp
[Fact]
public async Task GetRecommendedRoomIdAsync_WithValidResponse_ReturnsRoomId()
{
    // Arrange: Mock HttpClient
    var mockHandler = new MockHttpMessageHandler();
    mockHandler.SetupResponse(new LlmChatResponse { ... });
    
    var service = new LlmRoomRecommendationService(new HttpClient(mockHandler));
    
    // Act
    var roomId = await service.GetRecommendedRoomIdAsync(rooms, "beamer", ct);
    
    // Assert
    roomId.Should().Be(expectedId);
}
```

### Integration Tests

```csharp
[Fact]
public async Task RecommendedRoomEndpoint_WithLlmFailure_UsesRuleBasedFallback()
{
    // Simulate LLM failure and verify fallback logic works
}
```

## Zukünftige Erweiterungen

1. **Feedback-Loop**: Benutzer-Feedback für LLM-Verbesserung
2. **Fine-Tuning**: Spezifisches Modell für Raumempfehlungen
3. **Multi-Criteria**: Komplexe Anfragen mit mehreren Kriterien
4. **Zeitslots**: Spezifische Zeitfenster berücksichtigen
5. **Präferenzen**: Benutzer-Präferenzen speichern und nutzen

## Dokumentation & Support

- **API-Dokumentation**: Siehe Swagger UI
- **LLM-Provider**: Adesso AI Hub
- **Modell-Dokumentation**: GPT-4o-mini Spezifikation

