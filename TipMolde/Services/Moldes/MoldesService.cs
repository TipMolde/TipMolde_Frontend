using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Globalization;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula os pedidos HTTP da feature de moldes no frontend.
/// </summary>
/// <remarks>
/// Centraliza consulta, criacao, atualizacao e enriquecimento de moldes,
/// incluindo imagem de capa e dados de dashboard do ciclo de vida.
/// </remarks>
public sealed class MoldesService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de moldes.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public MoldesService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Obtem um molde pelo identificador.
    /// </summary>
    /// <param name="moldeId">Identificador do molde.</param>
    /// <returns>DTO do molde ou nulo quando nao e encontrado.</returns>
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

    /// <summary>
    /// Lista moldes de forma paginada.
    /// </summary>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com moldes ou nulo quando a API nao devolve sucesso.</returns>
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

    /// <summary>
    /// Lista moldes que ja estao associados a encomendas.
    /// </summary>
    /// <param name="searchTerm">Termo opcional para filtrar moldes associados.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com moldes associados a encomendas ou nulo quando a API falha.</returns>
    public async Task<PagedResult<MoldeDto>?> GetComEncomendaAsync(string? searchTerm, int page, int pageSize)
    {
        var endpoint = $"api/moldes/com-encomenda?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var termoNormalizado = Uri.EscapeDataString(searchTerm.Trim());
            endpoint = $"api/moldes/com-encomenda?searchTerm={termoNormalizado}&page={page}&pageSize={pageSize}";
        }

        using var response = await HttpClient.GetAsync(endpoint);

        await ThrowIfAuthorizationFailureAsync(
            response,
            "Nao tens permissao para consultar os moldes associados a encomendas.");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<MoldeDto>>(response);
    }

    /// <summary>
    /// Lista moldes pertencentes a uma encomenda.
    /// </summary>
    /// <param name="encomendaId">Identificador da encomenda.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com moldes da encomenda ou nulo quando a API falha.</returns>
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

    /// <summary>
    /// Obtem o resumo do dashboard de ciclo de vida de um molde.
    /// </summary>
    /// <param name="moldeId">Identificador do molde.</param>
    /// <returns>DTO do dashboard do molde ou nulo quando a API nao devolve sucesso.</returns>
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
    /// <param name="numero">Numero funcional do molde.</param>
    /// <param name="numeroMoldeCliente">Numero do molde no contexto do cliente.</param>
    /// <param name="nome">Nome do molde.</param>
    /// <param name="descricao">Descricao funcional do molde.</param>
    /// <param name="numeroCavidades">Numero de cavidades.</param>
    /// <param name="tipoPedido">Tipo de pedido associado ao molde.</param>
    /// <param name="largura">Largura tecnica do molde.</param>
    /// <param name="comprimento">Comprimento tecnico do molde.</param>
    /// <param name="altura">Altura tecnica do molde.</param>
    /// <param name="pesoEstimado">Peso estimado do molde.</param>
    /// <param name="tipoInjecao">Tipo de injecao configurado.</param>
    /// <param name="sistemaInjecao">Sistema de injecao configurado.</param>
    /// <param name="contracao">Valor de contracao configurado.</param>
    /// <param name="acabamentoPeca">Acabamento previsto para a peca.</param>
    /// <param name="cor">Cor funcional do molde.</param>
    /// <param name="materialMacho">Material do macho.</param>
    /// <param name="materialCavidade">Material da cavidade.</param>
    /// <param name="materialMovimentos">Material dos movimentos.</param>
    /// <param name="materialInjecao">Material de injecao.</param>
    /// <param name="imagemCapaPath">Caminho local opcional da imagem de capa.</param>
    /// <returns>DTO do molde criado.</returns>
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

    /// <summary>
    /// Atualiza os dados editaveis de um molde existente.
    /// </summary>
    /// <param name="moldeId">Identificador do molde a atualizar.</param>
    /// <param name="numero">Novo numero funcional do molde.</param>
    /// <param name="numeroMoldeCliente">Novo numero no contexto do cliente.</param>
    /// <param name="nome">Novo nome do molde.</param>
    /// <param name="imagemCapaPath">Novo caminho de imagem de capa persistido no backend.</param>
    /// <param name="descricao">Nova descricao funcional.</param>
    /// <param name="numeroCavidades">Novo numero de cavidades.</param>
    /// <param name="tipoPedido">Novo tipo de pedido.</param>
    /// <param name="largura">Nova largura tecnica.</param>
    /// <param name="comprimento">Novo comprimento tecnico.</param>
    /// <param name="altura">Nova altura tecnica.</param>
    /// <param name="pesoEstimado">Novo peso estimado.</param>
    /// <param name="tipoInjecao">Novo tipo de injecao.</param>
    /// <param name="sistemaInjecao">Novo sistema de injecao.</param>
    /// <param name="contracao">Novo valor de contracao.</param>
    /// <param name="acabamentoPeca">Novo acabamento da peca.</param>
    /// <param name="cor">Nova cor funcional.</param>
    /// <param name="materialMacho">Novo material do macho.</param>
    /// <param name="materialCavidade">Novo material da cavidade.</param>
    /// <param name="materialMovimentos">Novo material dos movimentos.</param>
    /// <param name="materialInjecao">Novo material de injecao.</param>
    /// <returns>Tarefa assincrona da atualizacao.</returns>
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

    /// <summary>
    /// Atualiza a imagem de capa de um molde.
    /// </summary>
    /// <param name="moldeId">Identificador do molde.</param>
    /// <param name="imagemCapaPath">Caminho absoluto da nova imagem de capa.</param>
    /// <returns>DTO do molde atualizado com a nova imagem.</returns>
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
