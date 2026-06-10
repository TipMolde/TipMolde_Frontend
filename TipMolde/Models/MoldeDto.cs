using System.Text.Json.Serialization;
using TipMolde.Helper;

namespace TipMolde.Models;

public sealed class MoldeDto
{
    [JsonPropertyName("moldeId")]
    public int MoldeId { get; set; }

    [JsonPropertyName("numero")]
    public string Numero { get; set; } = string.Empty;

    [JsonPropertyName("numeroMoldeCliente")]
    public string NumeroMoldeCliente { get; set; } = string.Empty;

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("imagemCapaPath")]
    public string ImagemCapaPath { get; set; } = string.Empty;

    [JsonPropertyName("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [JsonPropertyName("numero_cavidades")]
    public int Numero_cavidades { get; set; }

    [JsonPropertyName("tipoPedido")]
    public string TipoPedido { get; set; } = string.Empty;

    public string ImagemCapaSource => MoldeImageSourceHelper.Resolve(ImagemCapaPath);
}
