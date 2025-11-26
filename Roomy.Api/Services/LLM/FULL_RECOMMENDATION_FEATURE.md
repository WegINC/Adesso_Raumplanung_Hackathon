# ✅ LLM gibt jetzt vollständige Empfehlung mit Begründung zurück

## Änderungen

### 1. Service-Signatur geändert

**Vorher:**
```csharp
public async Task<int?> GetRecommendedRoomIdAsync(...)
```
Gab nur die Raum-ID zurück.

**Nachher:**
```csharp
public async Task<(int? roomId, string? recommendation)> GetRecommendedRoomAsync(...)
```
Gibt Tupel mit Raum-ID **und** vollständiger LLM-Empfehlung zurück.

### 2. Prompt Engineering überarbeitet

**System Prompt:**
```
Du bist ein intelligenter Assistent für Raumbuchungen. 
Deine Aufgabe ist es, basierend auf der Anfrage des Benutzers den am besten passenden Raum zu empfehlen.
Analysiere die Anforderungen (Kapazität, Ausstattung, Typ) und wähle den optimalen Raum aus.

WICHTIG: Deine Antwort muss folgendes Format haben:
1. Erste Zeile: Nur der exakte Raumname (genau wie in der Liste angegeben)
2. Danach: Eine ausführliche Empfehlung mit Ausstattung und Begründung

Beispiel:
Meeting Room A
Ich empfehle den Meeting Room A.
**Ausstattung:** [Beschreibe die Ausstattung]
**Begründung:** [Erkläre warum dieser Raum optimal ist]
```

**User Prompt:**
```
Verfügbare Räume für morgen:
[Raumliste]

Benutzeranfrage: "[Anfrage]"

Welcher Raum passt am besten? Gib zuerst den exakten Raumnamen an, 
dann eine ausführliche Empfehlung mit Ausstattung und Begründung.
```

### 3. Parsing-Logik

**Vorher:** Parse Integer (Raum-ID)
```csharp
if (int.TryParse(llmRecommendation, out int roomId))
{
    // Suche Raum nach ID
}
```

**Nachher:** Parse Raumname aus erster Zeile
```csharp
var lines = llmRecommendation.Split('\n', StringSplitOptions.RemoveEmptyEntries);
var recommendedRoomName = lines[0].Trim();

// Suche Raum nach Name (case-insensitive)
var recommendedRoom = availableRooms.FirstOrDefault(r => 
    r.Name.Equals(recommendedRoomName, StringComparison.OrdinalIgnoreCase));

if (recommendedRoom != null)
{
    return (recommendedRoom.Id, llmRecommendation); // Komplette Empfehlung
}
```

### 4. Response Model erweitert

**RecommendedRoomResponse:**
```csharp
public class RecommendedRoomResponse
{
    public string RoomName { get; set; }
    public int RoomId { get; set; }
    public int Capacity { get; set; }
    public string? Description { get; set; }
    
    // NEU: Vollständige LLM-Empfehlung mit Ausstattung und Begründung
    public string? LlmRecommendation { get; set; }
}
```

### 5. Endpoint aktualisiert

```csharp
var (llmRoomId, llmResponse) = await _llmService.GetRecommendedRoomAsync(
    availableRooms, req.Criteria, ct);

if (llmRoomId.HasValue)
{
    recommendedRoom = availableRooms.FirstOrDefault(r => r.Id == llmRoomId.Value);
    llmRecommendation = llmResponse; // Speichere komplette Empfehlung
}

Response = new RecommendedRoomResponse
{
    RoomName = recommendedRoom.Name,
    RoomId = recommendedRoom.Id,
    Capacity = recommendedRoom.Capacity,
    Description = recommendedRoom.Description,
    LlmRecommendation = llmRecommendation // Wird in Response zurückgegeben
};
```

## Beispiel-Response

### Request:
```json
POST /api/rooms/recommended
{
  "criteria": "Ich möchte einen Raum für 5 Leute mit Kaffeemaschine"
}
```

### Response:
```json
{
  "roomName": "Huddle Space",
  "roomId": 3,
  "capacity": 4,
  "description": "Kleiner Raum für informelle Diskussionen",
  "llmRecommendation": "Huddle Space\nIch empfehle den **Huddle Space**.\n\n**Ausstattung:** Der Huddle Space ist ein kleiner Raum, der für bis zu 4 Personen ausgelegt ist und ideal für informelle Diskussionen ist. Leider ist die Kaffeemaschine nicht in der Beschreibung erwähnt, aber er ist der am besten geeignete Raum für eine kleine Gruppe.\n\n**Begründung:** Obwohl der Huddle Space nur für 4 Personen ausgelegt ist, ist er der einzige verfügbare Raum, der für eine kleine Gruppe von 5 Personen am nächsten kommt. Für eine Gruppe von 5 Personen könnte man eventuell eine Person etwas enger setzen oder den Raum für eine informelle Besprechung nutzen. Leider gibt es keinen Raum, der genau für 5 Personen geeignet ist und eine Kaffeemaschine bietet. Wenn die Kaffeemaschine ein Muss ist, wäre es sinnvoll, dies bei der Buchung zu klären oder nach einem anderen Raum zu suchen, der diese Ausstattung hat."
}
```

## LLM-Antwort Format

Das LLM gibt jetzt eine strukturierte Antwort zurück:

```
[Raumname]
Ich empfehle den **[Raumname]**.

**Ausstattung:** [Detaillierte Beschreibung der Ausstattung]

**Begründung:** [Ausführliche Erklärung warum dieser Raum optimal ist]
```

### Beispiel:
```
Huddle Space
Ich empfehle den **Huddle Space**.

**Ausstattung:** Der Huddle Space ist ein kleiner Raum, der für bis zu 4 Personen 
ausgelegt ist und ideal für informelle Diskussionen ist. Leider ist die Kaffeemaschine 
nicht in der Beschreibung erwähnt, aber er ist der am besten geeignete Raum für eine 
kleine Gruppe.

**Begründung:** Obwohl der Huddle Space nur für 4 Personen ausgelegt ist, ist er der 
einzige verfügbare Raum, der für eine kleine Gruppe von 5 Personen am nächsten kommt. 
Für eine Gruppe von 5 Personen könnte man eventuell eine Person etwas enger setzen oder 
den Raum für eine informelle Besprechung nutzen...
```

## Vorteile

✅ **Transparenz**: Benutzer sieht die Begründung des LLM
✅ **Vertrauen**: Nachvollziehbare Entscheidungen
✅ **Kontext**: Informationen über Ausstattung und Einschränkungen
✅ **Intelligenz**: LLM kann komplexe Abwägungen erklären
✅ **Flexibilität**: Auch wenn kein perfekter Match, erklärt LLM Alternativen

## Logging

Das Logging zeigt jetzt:
```
[INFO] LLM Empfehlung (roh): Huddle Space
Ich empfehle den **Huddle Space**...
[INFO] LLM hat Raum empfohlen: Huddle Space
[INFO] ✓ Raum 'Huddle Space' (ID: 3) gefunden
[INFO] === LLM Empfehlung erfolgreich ===
```

## Frontend-Integration

Das Frontend kann jetzt die `llmRecommendation` anzeigen:

```typescript
interface RecommendedRoomResponse {
  roomName: string;
  roomId: number;
  capacity: number;
  description?: string;
  llmRecommendation?: string; // NEU: Markdown-formatierter Text
}
```

Anzeige mit Markdown-Rendering:
```tsx
{response.llmRecommendation && (
  <div className="llm-recommendation">
    <h3>KI-Empfehlung</h3>
    <ReactMarkdown>{response.llmRecommendation}</ReactMarkdown>
  </div>
)}
```

## Testing

### Manuelle Tests

**Test 1: Spezifische Anforderungen**
```bash
POST /api/rooms/recommended
{
  "criteria": "Ich möchte einen Raum für 5 Leute mit Kaffeemaschine"
}
```

**Test 2: Natürlichsprachlich**
```bash
POST /api/rooms/recommended
{
  "criteria": "Wir brauchen einen Raum mit Beamer für eine Präsentation, ca. 8 Personen"
}
```

**Test 3: Komplexe Anforderungen**
```bash
POST /api/rooms/recommended
{
  "criteria": "Großer Konferenzraum mit Videokonferenz-Equipment für 20 Leute"
}
```

## Status

✅ **Service**: Gibt Tupel (ID, Empfehlung) zurück
✅ **Prompt**: Strukturierte Antwort mit Raumname + Begründung
✅ **Parsing**: Extrahiert Raumname aus erster Zeile
✅ **Response**: Enthält vollständige LLM-Empfehlung
✅ **Logging**: Zeigt Raumname und komplette Empfehlung
✅ **Fallback**: Regelbasierte Logik wenn LLM fehlschlägt

Die LLM-Integration gibt jetzt aussagekräftige, begründete Empfehlungen zurück! 🎉

