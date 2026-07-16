using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class IndustrialSessaoAtivaDto
{
    private static readonly TimeSpan TelemetriaStaleAfter = TimeSpan.FromMinutes(10);

    [JsonPropertyName("sessaoMaquinaIndustrial_id")]
    public int SessaoMaquinaIndustrial_id { get; set; }

    [JsonPropertyName("maquina_id")]
    public int Maquina_id { get; set; }

    [JsonPropertyName("operador_id")]
    public int Operador_id { get; set; }

    [JsonPropertyName("operadorNome")]
    public string OperadorNome { get; set; } = string.Empty;

    [JsonPropertyName("peca_id")]
    public int Peca_id { get; set; }

    [JsonPropertyName("numeroPeca")]
    public string NumeroPeca { get; set; } = string.Empty;

    [JsonPropertyName("designacaoPeca")]
    public string DesignacaoPeca { get; set; } = string.Empty;

    [JsonPropertyName("molde_id")]
    public int Molde_id { get; set; }

    [JsonPropertyName("numeroMolde")]
    public string NumeroMolde { get; set; } = string.Empty;

    [JsonPropertyName("fase_id")]
    public int Fase_id { get; set; }

    [JsonPropertyName("faseNome")]
    public string FaseNome { get; set; } = string.Empty;

    [JsonPropertyName("proximaFasePlaneada_id")]
    public int? ProximaFasePlaneada_id { get; set; }

    [JsonPropertyName("proximaFasePlaneadaNome")]
    public string ProximaFasePlaneadaNome { get; set; } = string.Empty;

    [JsonPropertyName("estadoSessao")]
    public string EstadoSessao { get; set; } = string.Empty;

    [JsonPropertyName("ultimoEstadoMaquina")]
    public string UltimoEstadoMaquina { get; set; } = string.Empty;

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("lastSeenAt")]
    public DateTime LastSeenAt { get; set; }

    public bool TelemetriaDesatualizada
    {
        get
        {
            if (LastSeenAt <= DateTime.MinValue)
                return false;

            var lastSeenUtc = LastSeenAt.Kind switch
            {
                DateTimeKind.Utc => LastSeenAt,
                DateTimeKind.Local => LastSeenAt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(LastSeenAt, DateTimeKind.Utc)
            };

            return DateTime.UtcNow - lastSeenUtc > TelemetriaStaleAfter;
        }
    }

    public string OperadorDisplay => string.IsNullOrWhiteSpace(OperadorNome)
        ? $"Operador {Operador_id}"
        : OperadorNome;

    public string ContextoTituloDisplay => TelemetriaDesatualizada ? "Ultimo contexto conhecido" : "Peca ativa";
    public string NumeroPecaDisplay => string.IsNullOrWhiteSpace(NumeroPeca) ? "Sem numero" : NumeroPeca;
    public string DesignacaoPecaDisplay => string.IsNullOrWhiteSpace(DesignacaoPeca) ? "Peca sem designacao" : DesignacaoPeca;
    public string PecaResumoDisplay => $"{NumeroPecaDisplay} - {DesignacaoPecaDisplay}";
    public string NumeroMoldeDisplay => string.IsNullOrWhiteSpace(NumeroMolde) ? "Molde sem numero" : NumeroMolde;
    public string FaseDisplay => string.IsNullOrWhiteSpace(FaseNome) ? "Sem fase" : FaseNome.Replace('_', ' ');
    public string ProximaFasePlaneadaDisplay => string.IsNullOrWhiteSpace(ProximaFasePlaneadaNome)
        ? "Sem fase planeada"
        : ProximaFasePlaneadaNome.Replace('_', ' ');
    public string EstadoSessaoDisplay => string.IsNullOrWhiteSpace(EstadoSessao) ? "Sem sessao" : EstadoSessao.Replace('_', ' ');
    public string UltimoEstadoBaseDisplay => string.IsNullOrWhiteSpace(UltimoEstadoMaquina) ? "Sem estado" : UltimoEstadoMaquina.Replace('_', ' ');
    public string UltimoEstadoMaquinaDisplay => TelemetriaDesatualizada
        ? $"Sem telemetria recente (ultimo estado conhecido: {UltimoEstadoBaseDisplay})"
        : UltimoEstadoBaseDisplay;
    public string StartedAtDisplay => StartedAt > DateTime.MinValue ? StartedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "Sem data";
    public string LastSeenAtDisplay => LastSeenAt > DateTime.MinValue ? LastSeenAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "Sem data";
    public string TelemetriaAvisoDisplay => TelemetriaDesatualizada
        ? $"Sem atualizacoes da maquina ha mais de {TelemetriaStaleAfter.TotalMinutes:0} minutos. O estado mostrado e apenas o ultimo conhecido."
        : string.Empty;
}
