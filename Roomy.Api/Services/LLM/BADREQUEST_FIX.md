# ✅ BadRequest 400 Fehler behoben

## Problem

Der LLM API Call schlug mit einem 400 BadRequest Fehler fehl:

```
LLM API Fehler: StatusCode=BadRequest, 
Error={
  "error": {
    "message": "litellm.BadRequestError: Unrecognized request argument supplied: maxTokens"
  }
}
```

### Ursache

Die API erwartet **snake_case** Property-Namen (`max_tokens`), aber unsere Serialisierung verwendete **camelCase** (`maxTokens`).

**Problem im Code:**
```csharp
var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase  // ❌ Falsch!
});
```

**Generiertes JSON (falsch):**
```json
{
  "model": "gpt-4o-mini",
  "messages": [...],
  "maxTokens": 150,      // ❌ API erkennt "maxTokens" nicht
  "temperature": 0.7
}
```

**Erwartetes JSON (korrekt):**
```json
{
  "model": "gpt-4o-mini",
  "messages": [...],
  "max_tokens": 150,     // ✅ API erwartet "max_tokens"
  "temperature": 0.7
}
```

## Lösung

### 1. JsonPropertyName Attribute hinzugefügt

**Vorher:**
```csharp
public class LlmChatRequest
{
    public string Model { get; set; } = "gpt-4o-mini";
    public List<LlmMessage> Messages { get; set; } = new();
    public int MaxTokens { get; set; } = 150;  // Wird zu "maxTokens"
    public double Temperature { get; set; } = 0.7;
}
```

**Nachher:**
```csharp
using System.Text.Json.Serialization;

public class LlmChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-4o-mini";
    
    [JsonPropertyName("messages")]
    public List<LlmMessage> Messages { get; set; } = new();
    
    [JsonPropertyName("max_tokens")]  // ✅ Explizit snake_case
    public int MaxTokens { get; set; } = 150;
    
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;
}
```

### 2. Alle Models aktualisiert

**LlmMessage:**
```csharp
public class LlmMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
```

**LlmChatResponse:**
```csharp
public class LlmChatResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("choices")]
    public List<LlmChoice> Choices { get; set; } = new();
    
    // ... weitere Properties mit JsonPropertyName
}
```

**LlmChoice:**
```csharp
public class LlmChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("message")]
    public LlmMessage? Message { get; set; }
    
    [JsonPropertyName("finish_reason")]  // ✅ Snake_case
    public string? FinishReason { get; set; }
}
```

**LlmUsage:**
```csharp
public class LlmUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
```

### 3. Serialisierung vereinfacht

**Vorher:**
```csharp
var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});
```

**Nachher:**
```csharp
var jsonContent = JsonSerializer.Serialize(request);  // ✅ Einfacher, nutzt Attribute
```

## Vergleich

### Altes JSON (Fehler):
```json
{
  "model": "gpt-4o-mini",
  "messages": [
    {
      "role": "user",
      "content": "Hello"
    }
  ],
  "maxTokens": 150,        // ❌ Nicht erkannt
  "temperature": 0.7
}
```

### Neues JSON (korrekt):
```json
{
  "model": "gpt-4o-mini",
  "messages": [
    {
      "role": "user",
      "content": "Hello"
    }
  ],
  "max_tokens": 150,       // ✅ Korrekt
  "temperature": 0.7
}
```

## Testing

### Unit-Tests hinzugefügt

```csharp
[Fact]
public void LlmChatRequest_Serialization_UsesSnakeCase()
{
    var request = new LlmChatRequest
    {
        Model = "gpt-4o-mini",
        Messages = new List<LlmMessage>
        {
            new() { Role = "user", Content = "Hello" }
        },
        MaxTokens = 150,
        Temperature = 0.7
    };

    var json = JsonSerializer.Serialize(request);

    // Verification
    json.Should().Contain("\"max_tokens\"");  // ✅
    json.Should().NotContain("\"maxTokens\"");  // ✅
}
```

### Manuelle Tests

**Test mit curl:**
```bash
curl -X POST "http://localhost:5000/api/rooms/recommended" \
  -H "Content-Type: application/json" \
  -d '{
    "criteria": "Ich möchte einen Raum mit Beamer für 5 Leute"
  }'
```

**Erwartetes Log:**
```
[INFO] === LLM Raumempfehlung gestartet ===
[INFO] Benutzeranfrage: Ich möchte einen Raum mit Beamer für 5 Leute
[INFO] Anzahl verfügbarer Räume: 3
[INFO] Sende Request an LLM API: https://adesso-ai-hub.3asabc.de/v1/chat/completions
[DEBUG] Request Payload: {"model":"gpt-4o-mini","messages":[...],"max_tokens":5000,...}
[INFO] LLM API Response Status: 200  // ✅ Nicht mehr 400!
[INFO] LLM Empfehlung (roh): 2
[INFO] ✓ Raum-ID 2 ist verfügbar: Präsentationsraum A
[INFO] === LLM Empfehlung erfolgreich ===
```

## Vorteile der Lösung

✅ **Explizit**: Property-Namen sind klar definiert
✅ **Unabhängig**: Nicht abhängig von globalen Serialisierungseinstellungen
✅ **Wartbar**: Änderungen an C# Properties beeinflussen API-Contract nicht
✅ **Dokumentiert**: JsonPropertyName zeigt den API-Contract
✅ **Testbar**: Serialisierung kann einfach getestet werden

## Wichtige Erkenntnisse

### API-Konvention
Die **Adesso AI Hub API** verwendet **snake_case** (wie OpenAI):
- `max_tokens` ✅
- `finish_reason` ✅
- `prompt_tokens` ✅

### .NET Konvention
.NET verwendet **PascalCase** für Properties:
- `MaxTokens` ✅
- `FinishReason` ✅
- `PromptTokens` ✅

### Lösung
**JsonPropertyName** Attribute überbrücken die Konventionen:
```csharp
[JsonPropertyName("max_tokens")]  // API-Name
public int MaxTokens { get; set; }  // C#-Name
```

## Änderungsliste

### Geänderte Dateien:
1. ✅ `LlmModels.cs` - JsonPropertyName Attribute hinzugefügt
2. ✅ `LlmRoomRecommendationService.cs` - Serialisierung vereinfacht
3. ✅ `LlmModelsSerializationTests.cs` - Tests für korrekte Serialisierung

### Nächste Schritte:
1. ⏳ API neu starten
2. ⏳ Endpoint testen
3. ⏳ Logs überprüfen für erfolgreichen 200 Response

## Status

✅ **Problem**: BadRequest 400 wegen `maxTokens`
✅ **Ursache**: Falsche Property-Namen (camelCase statt snake_case)
✅ **Lösung**: JsonPropertyName Attribute hinzugefügt
✅ **Tests**: Serialisierungstests erstellt
⏳ **Verifizierung**: Manueller Test ausstehend

Der Fehler sollte jetzt behoben sein! 🎉

