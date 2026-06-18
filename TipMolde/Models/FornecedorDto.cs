using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class FornecedorDto
{
    [JsonPropertyName("fornecedorId")]
    public int FornecedorId { get; set; }

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("nif")]
    public string NIF { get; set; } = string.Empty;

    [JsonPropertyName("morada")]
    public string? Morada { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("telefone")]
    public string? Telefone { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(Nome)
        ? $"Fornecedor #{FornecedorId}"
        : string.IsNullOrWhiteSpace(NIF)
            ? Nome
            : $"{Nome} ({NIF})";
}
