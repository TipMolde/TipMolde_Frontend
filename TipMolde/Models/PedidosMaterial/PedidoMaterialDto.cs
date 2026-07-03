using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class PedidoMaterialDto
{
    [JsonPropertyName("pedidoMaterialId")]
    public int PedidoMaterialId { get; set; }

    [JsonPropertyName("dataPedido")]
    public DateTime DataPedido { get; set; }

    [JsonPropertyName("dataRececao")]
    public DateTime? DataRececao { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("fornecedorId")]
    public int FornecedorId { get; set; }

    [JsonPropertyName("userConferenteId")]
    public int? UserConferenteId { get; set; }

    [JsonPropertyName("itens")]
    public List<PedidoMaterialItemDto> Itens { get; set; } = [];
}

public sealed class PedidoMaterialItemDto
{
    [JsonPropertyName("itemId")]
    public int ItemId { get; set; }

    [JsonPropertyName("pecaId")]
    public int PecaId { get; set; }

    [JsonPropertyName("quantidade")]
    public int Quantidade { get; set; }
}

public sealed class CreatePedidoMaterialRequest
{
    public int Fornecedor_id { get; set; }

    public List<CreatePedidoMaterialItemRequest> Itens { get; set; } = [];
}

public sealed class CreatePedidoMaterialItemRequest
{
    public int Peca_id { get; set; }

    public int Quantidade { get; set; }
}
