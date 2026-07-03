using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class UtilizadorDto
{
    [JsonPropertyName("user_id")]
    public int User_id { get; set; }

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}