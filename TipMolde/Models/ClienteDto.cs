using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class ClienteDto
{
    [JsonPropertyName("cliente_id")]
    public int Cliente_id { get; set; }

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("nif")]
    public string NIF { get; set; } = string.Empty;

    [JsonPropertyName("sigla")]
    public string Sigla { get; set; } = string.Empty;

    [JsonPropertyName("pais")]
    public string Pais { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("telefone")]
    public string Telefone { get; set; } = string.Empty;
}
