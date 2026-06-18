using System.Text.Json.Serialization;
using TipMolde.Domain.Enums;
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

    [JsonPropertyName("largura")]
    public decimal? Largura { get; set; }

    [JsonPropertyName("comprimento")]
    public decimal? Comprimento { get; set; }

    [JsonPropertyName("altura")]
    public decimal? Altura { get; set; }

    [JsonPropertyName("pesoEstimado")]
    public decimal? PesoEstimado { get; set; }

    [JsonPropertyName("tipoInjecao")]
    public string? TipoInjecao { get; set; }

    [JsonPropertyName("sistemaInjecao")]
    public string? SistemaInjecao { get; set; }

    [JsonPropertyName("contracao")]
    public decimal? Contracao { get; set; }

    [JsonPropertyName("acabamentoPeca")]
    public string? AcabamentoPeca { get; set; }

    [JsonPropertyName("cor")]
    public CorMolde? Cor { get; set; }

    [JsonPropertyName("materialMacho")]
    public string? MaterialMacho { get; set; }

    [JsonPropertyName("materialCavidade")]
    public string? MaterialCavidade { get; set; }

    [JsonPropertyName("materialMovimentos")]
    public string? MaterialMovimentos { get; set; }

    [JsonPropertyName("materialInjecao")]
    public string? MaterialInjecao { get; set; }

    public string ImagemCapaSource => MoldeImageSourceHelper.Resolve(ImagemCapaPath);
    public string DisplayName => string.IsNullOrWhiteSpace(Numero)
        ? $"Molde #{MoldeId}"
        : string.IsNullOrWhiteSpace(Nome)
            ? Numero
            : $"{Numero} - {Nome}";
}
