using System.Text.Json.Serialization;

namespace TipMolde.Models;

public sealed class PagedResult<T>
{
    private List<T>? _legacyItems;

    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = [];

    [JsonPropertyName("itens")]
    public List<T>? LegacyItems
    {
        get => _legacyItems;
        set
        {
            _legacyItems = value;

            if (value is { Count: > 0 })
                Items = value;
        }
    }

    [JsonPropertyName("currentPage")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalItems { get; set; }

    [JsonIgnore]
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);
}
