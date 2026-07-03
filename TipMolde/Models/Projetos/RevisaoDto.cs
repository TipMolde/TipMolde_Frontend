using System.Text.Json.Serialization;
using System.IO;

namespace TipMolde.Models;

public sealed class RevisaoDto
{
    [JsonPropertyName("revisao_id")]
    public int Revisao_id { get; set; }

    [JsonPropertyName("numRevisao")]
    public int NumRevisao { get; set; }

    [JsonPropertyName("descricaoAlteracoes")]
    public string DescricaoAlteracoes { get; set; } = string.Empty;

    [JsonPropertyName("dataEnvioCliente")]
    public DateTime DataEnvioCliente { get; set; }

    [JsonPropertyName("aprovado")]
    public bool? Aprovado { get; set; }

    [JsonPropertyName("dataResposta")]
    public DateTime? DataResposta { get; set; }

    [JsonPropertyName("feedbackTexto")]
    public string? FeedbackTexto { get; set; }

    [JsonPropertyName("feedbackImagemPath")]
    public string? FeedbackImagemPath { get; set; }

    [JsonPropertyName("projeto_id")]
    public int Projeto_id { get; set; }

    public string NumRevisaoDisplay => $"Revisao {NumRevisao}";
    public string DataEnvioDisplay => DataEnvioCliente == default ? "Sem data" : DataEnvioCliente.ToString("dd/MM/yyyy HH:mm");
    public string DataRespostaDisplay => DataResposta.HasValue ? DataResposta.Value.ToString("dd/MM/yyyy HH:mm") : "Sem resposta";
    public string EstadoDisplay => Aprovado switch
    {
        true => "Aprovada",
        false => "Rejeitada",
        null => "A aguardar resposta"
    };
    public bool PodeResponder => !Aprovado.HasValue;
    public bool TemResposta => Aprovado.HasValue;
    public bool TemAnexo => !string.IsNullOrWhiteSpace(FeedbackImagemPath);
    public string FeedbackAnexoFileName => string.IsNullOrWhiteSpace(FeedbackImagemPath)
        ? string.Empty
        : Path.GetFileName(FeedbackImagemPath.Trim());
    public string ResumoFeedbackDisplay => string.IsNullOrWhiteSpace(FeedbackTexto) && string.IsNullOrWhiteSpace(FeedbackImagemPath)
        ? "Sem feedback registado."
        : string.Join(" | ", new[] { FeedbackTexto?.Trim(), FeedbackAnexoFileName }.Where(value => !string.IsNullOrWhiteSpace(value)));
}
