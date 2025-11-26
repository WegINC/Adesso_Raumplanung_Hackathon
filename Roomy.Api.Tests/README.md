# Roomy.Api.Tests

## Übersicht

Dieses Projekt enthält Unit- und Integrationstests für die Roomy.Api, mit besonderem Fokus auf die Room-Endpoints und die Verfügbarkeitsabfrage.

## Test-Struktur

### GetRoomsEndpointTests

Tests für den `GetRoomsEndpoint` mit Fokus auf die Abfrage verfügbarer Räume:

### RecommendedRoomEndpointTests

Tests für den `RecommendedRoomEndpoint` mit Fokus auf die intelligente Raumempfehlung basierend auf Kriterien:

#### Testmethoden

1. **HandleAsync_WithAvailableRoomsFilter_ReturnsOnlyAvailableRooms**
   - Testet die Rückgabe von Räumen mit unterschiedlicher Verfügbarkeit
   - Überprüft, dass beide verfügbare und nicht verfügbare Räume korrekt zurückgegeben werden
   - Validiert, dass das IsAvailable-Flag korrekt gemappt wird

2. **HandleAsync_WithNoRooms_ReturnsEmptyList**
   - Testet das Verhalten, wenn keine Räume in der Datenbank vorhanden sind
   - Erwartet eine leere Liste als Rückgabewert

3. **HandleAsync_WithMixedAvailability_ReturnsAllRooms**
   - Testet die Rückgabe von mehreren Räumen mit gemischter Verfügbarkeit
   - Validiert, dass alle Räume zurückgegeben werden (verfügbare und nicht verfügbare)
   - Überprüft die Anzahl verfügbarer vs. nicht verfügbarer Räume

4. **HandleAsync_WithMultipleRooms_ReturnsMappedRoomDtos**
   - Testet das korrekte Mapping von Room-Entitäten zu RoomDto-Objekten
   - Überprüft alle Properties: Id, Name, Description, Capacity, IsAvailable, CreatedAt, UpdatedAt
   - Validiert Datumswerte

5. **HandleAsync_WithNullDescription_ReturnsRoomWithNullDescription**
   - Testet den Umgang mit optionalen Properties (Description kann null sein)
   - Überprüft, dass null-Werte korrekt verarbeitet werden

6. **HandleAsync_WithMultipleAvailableRooms_ReturnsAllAvailableRooms**
   - Testet die Rückgabe mehrerer verfügbarer Räume
   - Validiert, dass alle erwarteten Räume vorhanden sind
   - Überprüft die Namen der zurückgegebenen Räume

7. **HandleAsync_WithCancellationToken_CompletesSuccessfully**
   - Testet die korrekte Verarbeitung des CancellationToken-Parameters
   - Validiert asynchrone Verarbeitung

#### RecommendedRoomEndpoint Testmethoden

1. **HandleAsync_WithSmallCriteria_ReturnsSmallestRoom**
   - Testet die Empfehlung des kleinsten Raums bei "small" Kriterium
   - Validiert die Geschäftslogik für kleine Räume (Capacity <= 10)

2. **HandleAsync_WithLargeCriteria_ReturnsLargestRoom**
   - Testet die Empfehlung des größten Raums bei "large" Kriterium
   - Validiert die Geschäftslogik für große Räume (Capacity >= 20)

3. **HandleAsync_WithMeetingCriteria_ReturnsMeetingRoom**
   - Testet die Suche nach Meeting-Räumen im Namen
   - Validiert die Textsuche in Name und Description

4. **HandleAsync_WithNoAvailableRooms_ThrowsValidationException**
   - Testet das Fehlerverhalten wenn keine verfügbaren Räume existieren
   - Erwartet eine ValidationFailureException

5. **HandleAsync_WithNumericCapacityCriteria_ReturnsRoomWithMatchingCapacity**
   - Testet die Kapazitätsbasierte Suche (z.B. "12" findet Raum mit 15er Kapazität)
   - Validiert die Logik für nächst größere Kapazität

6. **HandleAsync_WithGermanSmallCriteria_ReturnsSmallestRoom**
   - Testet die Unterstützung deutscher Begriffe ("klein")
   - Validiert mehrsprachige Empfehlungslogik

7. **HandleAsync_WithPresentationCriteria_ReturnsPresentationRoom**
   - Testet die Suche nach Präsentationsräumen
   - Validiert die Suche nach Equipment-Keywords (Projektor, Beamer)

8. **HandleAsync_WithUnknownCriteria_ReturnsFirstAvailableRoom**
   - Testet das Fallback-Verhalten bei unbekannten Kriterien
   - Validiert die Default-Geschäftsregel

9. **HandleAsync_WithMixedAvailability_ReturnsOnlyAvailableRoom**
   - Testet dass nur verfügbare Räume berücksichtigt werden
   - Validiert die Verfügbarkeitsfilterung

10. **HandleAsync_WithGermanKonferenzCriteria_ReturnsConferenceRoom**
    - Testet deutsche Konferenzraum-Suche
    - Validiert mehrsprachige Suchbegriffe

11. **HandleAsync_WithValidRequest_ReturnsCompleteRoomDto**
    - Testet das vollständige DTO-Mapping
    - Validiert alle Response-Properties

## Test-Technologien

- **xUnit**: Test-Framework
- **FluentAssertions**: Für lesbare und aussagekräftige Assertions
- **EntityFrameworkCore.InMemory**: In-Memory-Datenbank für isolierte Tests

## Tests ausführen

### Voraussetzung
Stellen Sie sicher, dass die Roomy.Api nicht läuft, bevor Sie die Tests ausführen, oder führen Sie die Tests wie folgt aus:

```powershell
# Alle Tests ausführen
cd Roomy.Api.Tests
dotnet test

# Tests mit detaillierter Ausgabe
dotnet test --verbosity normal

# Spezifische Testklasse ausführen
dotnet test --filter "FullyQualifiedName~GetRoomsEndpointTests"

# Einzelnen Test ausführen
dotnet test --filter "FullyQualifiedName~HandleAsync_WithNoRooms_ReturnsEmptyList"
```

### Alternative: Tests aus dem Solution-Verzeichnis ausführen

```powershell
cd C:\Users\petranek\projects\ai\Adesso_Raumplanung_Hackathon
dotnet test Roomy.Api.Tests/Roomy.Api.Tests.csproj
```

## DDD-Prinzipien in Tests

### Domain-Validierung
Die Tests folgen dem **MethodName_Condition_ExpectedResult()**-Namensschema gemäß den DDD-Richtlinien.

### Aggregate-Boundaries
Die Tests respektieren die Aggregate-Grenzen:
- **Room**: Aggregat-Root mit eigener Identität und Lebenszyklus
- Tests validieren Geschäftsregeln auf Aggregat-Ebene

### Ubiquitous Language
Die Tests verwenden die Domänensprache:
- Room (Raum)
- Availability (Verfügbarkeit)
- Capacity (Kapazität)

### Test-Kategorien

**Integration Tests**: Die GetRoomsEndpointTests sind Integrationstests, da sie:
- Den vollständigen Endpoint testen
- Die Datenbankschicht einbeziehen (In-Memory)
- Die Entity-zu-DTO-Transformation validieren

## Wichtige Hinweise

### Verfügbarkeitsfilterung
⚠️ **Beobachtung**: Der aktuelle `GetRoomsEndpoint` gibt **alle** Räume zurück, nicht nur die verfügbaren. Die Tests validieren dieses Verhalten. Wenn nur verfügbare Räume zurückgegeben werden sollen, muss der Endpoint wie folgt angepasst werden:

```csharp
var rooms = await _dbContext.Rooms
    .Where(r => r.IsAvailable)  // Filter hinzufügen
    .Select(r => new RoomDto { ... })
    .ToListAsync(ct);
```

### Test-Isolation
Jeder Test verwendet eine separate In-Memory-Datenbank (via `Guid.NewGuid().ToString()`), um vollständige Isolation zu gewährleisten.

### Dispose Pattern
Die Testklasse implementiert `IDisposable`, um sicherzustellen, dass der DbContext nach jedem Test ordnungsgemäß bereinigt wird.

## Erweiterungsmöglichkeiten

Zukünftige Tests könnten umfassen:
- **Filterung nach Verfügbarkeit**: Tests für einen Endpoint, der nur verfügbare Räume zurückgibt
- **Sortierung**: Tests für die Sortierung nach verschiedenen Kriterien
- **Pagination**: Tests für große Datenmengen
- **Fehlerbehandlung**: Tests für Datenbankfehler und Exception-Handling
- **Performance**: Tests für die Abfrageperformance bei vielen Räumen

