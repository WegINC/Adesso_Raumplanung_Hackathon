using System.Text.Json.Serialization;

namespace Roomy.Api.Services.LLM;

/// <summary>
/// Request-Model für LLM Chat Completions API.
/// </summary>
public class LlmChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-4o-mini";
    
    [JsonPropertyName("messages")]
    public List<LlmMessage> Messages { get; set; } = new();
    
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 150;
    
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;
}

/// <summary>
/// Nachricht für LLM Chat.
/// </summary>
public class LlmMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Response-Model für LLM Chat Completions API.
/// </summary>
public class LlmChatResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
    
    [JsonPropertyName("created")]
    public long Created { get; set; }
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("choices")]
    public List<LlmChoice> Choices { get; set; } = new();
    
    [JsonPropertyName("usage")]
    public LlmUsage? Usage { get; set; }
}

/// <summary>
/// Choice aus LLM Response.
/// </summary>
public class LlmChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("message")]
    public LlmMessage? Message { get; set; }
    
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// Token-Usage Statistik.
/// </summary>
public class LlmUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

