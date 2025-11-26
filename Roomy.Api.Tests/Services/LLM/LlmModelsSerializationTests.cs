using System.Text.Json;
using FluentAssertions;
using Roomy.Api.Services.LLM;
using Xunit;

namespace Roomy.Api.Tests.Services.LLM;

/// <summary>
/// Tests für korrekte JSON-Serialisierung der LLM Models.
/// </summary>
public class LlmModelsSerializationTests
{
    [Fact(DisplayName = "LlmChatRequest sollte mit snake_case serialisiert werden")]
    public void LlmChatRequest_Serialization_UsesSnakeCase()
    {
        // Setup: Request erstellen
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

        // Execution: Serialisieren
        var json = JsonSerializer.Serialize(request);

        // Verification: Sollte snake_case verwenden
        json.Should().Contain("\"max_tokens\"");
        json.Should().Contain("\"model\"");
        json.Should().Contain("\"messages\"");
        json.Should().Contain("\"temperature\"");
        json.Should().Contain("\"role\"");
        json.Should().Contain("\"content\"");
        
        // Sollte NICHT camelCase verwenden
        json.Should().NotContain("\"maxTokens\"");
        json.Should().NotContain("\"Max_Tokens\"");
    }

    [Fact(DisplayName = "Serialisiertes JSON sollte dem erwarteten Format entsprechen")]
    public void LlmChatRequest_Serialization_MatchesExpectedFormat()
    {
        // Setup: Request wie im curl-Beispiel
        var request = new LlmChatRequest
        {
            Model = "gpt-4o-mini",
            Messages = new List<LlmMessage>
            {
                new() { Role = "user", Content = "Hello, how are you?" }
            },
            MaxTokens = 150,
            Temperature = 0.7
        };

        // Execution: Serialisieren mit Formatierung
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        // Verification: Format prüfen
        var expectedJson = @"{
  ""model"": ""gpt-4o-mini"",
  ""messages"": [
    {
      ""role"": ""user"",
      ""content"": ""Hello, how are you?""
    }
  ],
  ""max_tokens"": 150,
  ""temperature"": 0.7
}";

        json.Should().Be(expectedJson);
    }
}

