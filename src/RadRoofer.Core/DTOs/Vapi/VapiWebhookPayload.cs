using System.Text.Json.Serialization;

namespace RadRoofer.Core.DTOs.Vapi;

public record VapiWebhookPayload
{
    [JsonPropertyName("message")]
    public required VapiMessage Message { get; init; }
}

public record VapiMessage
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("endedReason")]
    public string? EndedReason { get; init; }

    [JsonPropertyName("call")]
    public VapiCall? Call { get; init; }

    [JsonPropertyName("analysis")]
    public VapiAnalysis? Analysis { get; init; }

    [JsonPropertyName("artifact")]
    public VapiArtifact? Artifact { get; init; }
}

public record VapiCall
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("customer")]
    public VapiCustomer? Customer { get; init; }

    [JsonPropertyName("startedAt")]
    public DateTimeOffset? StartedAt { get; init; }

    [JsonPropertyName("endedAt")]
    public DateTimeOffset? EndedAt { get; init; }
}

public record VapiCustomer
{
    [JsonPropertyName("number")]
    public string? Number { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public record VapiAnalysis
{
    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("structuredData")]
    public VapiStructuredData? StructuredData { get; init; }
}

public record VapiStructuredData
{
    [JsonPropertyName("callerName")]
    public string? CallerName { get; init; }

    [JsonPropertyName("callerPhone")]
    public string? CallerPhone { get; init; }

    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("reasonForCall")]
    public string? ReasonForCall { get; init; }
}

public record VapiArtifact
{
    [JsonPropertyName("transcript")]
    public string? Transcript { get; init; }
}
