using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class MaquinaItem
{
    [JsonPropertyName("maquina_id")]
    public int Maquina_id { get; set; }

    [JsonPropertyName("numero")]
    public int Numero { get; set; }

    [JsonPropertyName("nomeModelo")]
    public string NomeModelo { get; set; } = string.Empty;

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("protocoloComunicacao")]
    public string? ProtocoloComunicacao { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("faseDedicada_id")]
    public int FaseDedicada_id { get; set; }

    public string FaseDedicadaNome { get; set; } = string.Empty;

    [JsonIgnore]
    public string NumeroDisplay => Numero <= 0 ? "Sem numero" : Numero.ToString();

    [JsonIgnore]
    public string DisplayName => NomeModeloDisplay;

    [JsonIgnore]
    public string NomeModeloDisplay => string.IsNullOrWhiteSpace(NomeModelo) ? "Maquina sem nome" : NomeModelo;

    [JsonIgnore]
    public string EstadoDisplay => string.IsNullOrWhiteSpace(Estado) ? "Sem estado" : Estado.Replace('_', ' ');

    [JsonIgnore]
    public string FaseDedicadaDisplay => string.IsNullOrWhiteSpace(FaseDedicadaNome)
        ? "Fase dedicada não definida"
        : FaseDedicadaNome.Replace('_', ' ');

    [JsonIgnore]
    public bool HasIpAddress => !string.IsNullOrWhiteSpace(IpAddress);

    [JsonIgnore]
    public string IpAddressDisplay => HasIpAddress ? IpAddress : "Sem conexao configurada";

    [JsonIgnore]
    public string ProtocoloComunicacaoDisplay => string.IsNullOrWhiteSpace(ProtocoloComunicacao)
        ? "Protocolo não detetado"
        : ProtocoloComunicacao;

    [JsonIgnore]
    public bool EmUtilizacao => string.Equals(Estado, "EM_USO", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool EmManutencao => string.Equals(Estado, "MANUTENCAO", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string UtilizacaoDisplay
    {
        get
        {
            if (EmUtilizacao)
                return "Em utilizacao";

            if (EmManutencao)
                return "Em manutencao";

            return "Disponivel";
        }
    }

    [JsonIgnore]
    public bool Disponivel => string.Equals(Estado, "DISPONIVEL", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string LigacaoDisplay => HasIpAddress ? "Conexao configurada" : "Sem conexao";

    [JsonIgnore]
    public string EstadoBadgeBackground => Estado switch
    {
        "EM_USO" => "#DBEAFE",
        "MANUTENCAO" => "#FEF3C7",
        _ => "#ECFDF5"
    };

    [JsonIgnore]
    public string EstadoBadgeForeground => Estado switch
    {
        "EM_USO" => "#1D4ED8",
        "MANUTENCAO" => "#B45309",
        _ => "#166534"
    };

    [JsonIgnore]
    public string LigacaoBadgeBackground => HasIpAddress ? "#EFF6FF" : "#F1F5F9";

    [JsonIgnore]
    public string LigacaoBadgeForeground => HasIpAddress ? "#1D4ED8" : "#475569";
}
