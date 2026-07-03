using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class ResponseLoginDto
{
    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; init; }
}
