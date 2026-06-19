using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Globalization;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class MoldesService : ApiServiceBase
{
    public MoldesService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<MoldeDto?> GetByIdAsync(int moldeId)
    {
        using var response = await HttpClient.GetAsync($"api/moldes/{moldeId}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar o molde.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<MoldeDto>(response);
    }

    public async Task<PagedResult<MoldeDto>?> GetAllAsync(int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync($"api/moldes?page={page}&pageSize={pageSize}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar os moldes.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<MoldeDto>>(response);
    }

    public async Task<PagedResult<MoldeDto>?> GetByEncomendaIdAsync(int encomendaId, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/moldes/por-encomenda/{encomendaId}?page={page}&pageSize={pageSize}");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar os moldes da encomenda.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<MoldeDto>>(response);
    }

    public async Task<MoldeCicloVidaDashboardDto?> GetDashboardCicloVidaAsync(int moldeId)
    {
        using var response = await HttpClient.GetAsync($"api/moldes/{moldeId}/dashboard-ciclo-vida");

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar o dashboard do molde.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<MoldeCicloVidaDashboardDto>(response);
    }

    /// <summary>
    /// Cria um novo molde com especificacoes tecnicas e imagem opcional.
    /// </summary>
    public async Task<MoldeDto?> CreateAsync(
        string numero,
        string? numeroMoldeCliente,
        string nome,
        string? descricao,
        int numeroCavidades,
        string tipoPedido,
        decimal? largura,
        decimal? comprimento,
        decimal? altura,
        decimal? pesoEstimado,
        string? tipoInjecao,
        string? sistemaInjecao,
        decimal? contracao,
        string? acabamentoPeca,
        TipMolde.Domain.Enums.CorMolde? cor,
        string? materialMacho,
        string? materialCavidade,
        string? materialMovimentos,
        string? materialInjecao,
        string? imagemCapaPath = null)
    {
        using var content = new MultipartFormDataContent();
        AddStringContent(content, "Numero", numero);
        AddStringContent(content, "NumeroMoldeCliente", numeroMoldeCliente);
        AddStringContent(content, "Nome", nome);
        AddStringContent(content, "Descricao", descricao);
        AddStringContent(content, "Numero_cavidades", numeroCavidades.ToString(CultureInfo.CurrentCulture));
        AddStringContent(content, "TipoPedido", tipoPedido);
        AddStringContent(content, "Largura", FormatOptionalDecimal(largura));
        AddStringContent(content, "Comprimento", FormatOptionalDecimal(comprimento));
        AddStringContent(content, "Altura", FormatOptionalDecimal(altura));
        AddStringContent(content, "PesoEstimado", FormatOptionalDecimal(pesoEstimado));
        AddStringContent(content, "TipoInjecao", tipoInjecao);
        AddStringContent(content, "SistemaInjecao", sistemaInjecao);
        AddStringContent(content, "Contracao", FormatOptionalDecimal(contracao));
        AddStringContent(content, "AcabamentoPeca", acabamentoPeca);
        AddStringContent(content, "Cor", cor?.ToString());
        AddStringContent(content, "MaterialMacho", materialMacho);
        AddStringContent(content, "MaterialCavidade", materialCavidade);
        AddStringContent(content, "MaterialMovimentos", materialMovimentos);
        AddStringContent(content, "MaterialInjecao", materialInjecao);

        if (!string.IsNullOrWhiteSpace(imagemCapaPath))
        {
            if (!File.Exists(imagemCapaPath))
                throw new FileNotFoundException("A imagem selecionada nao foi encontrada.", imagemCapaPath);

            var imagemFileName = Path.GetFileName(imagemCapaPath);
            await using var stream = File.OpenRead(imagemCapaPath);
            await using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);

            var fileContent = new ByteArrayContent(memory.ToArray());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(imagemCapaPath));
            content.Add(fileContent, "imagemCapa", imagemFileName);
        }

        using var response = await HttpClient.PostAsync("api/moldes", content);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar o molde {numero}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<MoldeDto>(response);
    }

    public async Task UpdateAsync(
        int moldeId,
        string? numero,
        string? numeroMoldeCliente,
        string? nome,
        string? imagemCapaPath,
        string? descricao,
        int? numeroCavidades,
        string? tipoPedido,
        decimal? largura,
        decimal? comprimento,
        decimal? altura,
        decimal? pesoEstimado,
        string? tipoInjecao,
        string? sistemaInjecao,
        decimal? contracao,
        string? acabamentoPeca,
        TipMolde.Domain.Enums.CorMolde? cor,
        string? materialMacho,
        string? materialCavidade,
        string? materialMovimentos,
        string? materialInjecao)
    {
        var payload = new
        {
            Numero = numero,
            NumeroMoldeCliente = numeroMoldeCliente,
            Nome = nome,
            ImagemCapaPath = imagemCapaPath,
            Descricao = descricao,
            Numero_cavidades = numeroCavidades,
            TipoPedido = tipoPedido,
            Largura = largura,
            Comprimento = comprimento,
            Altura = altura,
            PesoEstimado = pesoEstimado,
            TipoInjecao = tipoInjecao,
            SistemaInjecao = sistemaInjecao,
            Contracao = contracao,
            AcabamentoPeca = acabamentoPeca,
            Cor = cor?.ToString(),
            MaterialMacho = materialMacho,
            MaterialCavidade = materialCavidade,
            MaterialMovimentos = materialMovimentos,
            MaterialInjecao = materialInjecao
        };

        using var response = await HttpClient.PutAsJsonAsync($"api/moldes/{moldeId}", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar o molde {moldeId}. Estado: {(int)response.StatusCode}");
    }

    public async Task<MoldeDto?> UpdateImagemCapaAsync(int moldeId, string imagemCapaPath)
    {
        if (string.IsNullOrWhiteSpace(imagemCapaPath))
            throw new ArgumentException("O caminho da imagem e obrigatorio.", nameof(imagemCapaPath));

        if (!File.Exists(imagemCapaPath))
            throw new FileNotFoundException("A imagem selecionada nao foi encontrada.", imagemCapaPath);

        using var content = new MultipartFormDataContent();
        await using var stream = File.OpenRead(imagemCapaPath);
        await using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);

        var fileContent = new ByteArrayContent(memory.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(imagemCapaPath));
        content.Add(fileContent, "file", Path.GetFileName(imagemCapaPath));

        using var response = await HttpClient.PostAsync($"api/moldes/{moldeId}/imagem-capa", content);
        await EnsureSuccessAsync(response, $"Nao foi possivel atualizar a imagem do molde {moldeId}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<MoldeDto>(response);
    }

    private static string GetContentType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }

    private static string? FormatOptionalDecimal(decimal? value)
    {
        return value.HasValue
            ? value.Value.ToString(CultureInfo.CurrentCulture)
            : null;
    }

    private static void AddStringContent(MultipartFormDataContent content, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        content.Add(new StringContent(value), name);
    }
}
