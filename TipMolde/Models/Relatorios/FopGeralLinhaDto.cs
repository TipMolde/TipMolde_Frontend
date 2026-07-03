namespace TipMolde.Models;

public sealed class FopGeralLinhaDto
{
    public int FichaFopLinha_id { get; set; }
    public int FichaFop_id { get; set; }
    public int EncomendaMolde_id { get; set; }
    public DateTime Data { get; set; }
    public string Ocorrencia { get; set; } = string.Empty;
    public string? Correcao { get; set; }
    public string ResponsavelNome { get; set; } = string.Empty;
    public int? Peca_id { get; set; }
    public string? PecaNumero { get; set; }
    public string? PecaDesignacao { get; set; }
    public int? Molde_id { get; set; }
    public string? MoldeNumero { get; set; }
    public string? MoldeNome { get; set; }

    public string DataDisplay => Data.ToString("dd/MM/yyyy HH:mm");
    public string PecaDisplay => string.IsNullOrWhiteSpace(PecaNumero)
        ? "Peca sem numero"
        : string.IsNullOrWhiteSpace(PecaDesignacao)
            ? PecaNumero
            : $"{PecaNumero} - {PecaDesignacao}";
    public string MoldeDisplay => string.IsNullOrWhiteSpace(MoldeNumero)
        ? "Molde sem numero"
        : string.IsNullOrWhiteSpace(MoldeNome)
            ? MoldeNumero
            : $"{MoldeNumero} - {MoldeNome}";
}
