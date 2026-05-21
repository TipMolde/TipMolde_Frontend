using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class LoginDto
{
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [JsonPropertyName("password")]
    public required string Password { get; set; }
}
