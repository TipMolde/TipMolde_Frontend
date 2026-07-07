using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Globalization;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Encapsula os pedidos HTTP de exportacao e consulta de relatorios.
/// </summary>
/// <remarks>
/// Suporta preview, geracao final, garantia de fichas e exportacao
/// da FOP geral para armazenamento local no frontend.
/// </remarks>
public sealed class RelatoriosService : ApiServiceBase
{
    /// <summary>
    /// Construtor do servico de relatorios.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado com o endpoint base da API.</param>
    public RelatoriosService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    /// <summary>
    /// Lista fichas de producao associadas a um molde.
    /// </summary>
    /// <param name="moldeId">Identificador do molde.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com fichas do molde ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<FichaProducaoResumoDto>?> GetFichasByMoldeIdAsync(int moldeId, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/fichas-producao/by-molde?moldeId={moldeId}&page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<FichaProducaoResumoDto>>(response);
    }

    /// <summary>
    /// Gera uma preview local de um relatorio.
    /// </summary>
    /// <param name="request">Contexto funcional do relatorio a gerar.</param>
    /// <returns>Resultado com o ficheiro gerado em cache.</returns>
    public Task<RelatorioFileResult> PreviewAsync(RelatorioExportRequest request)
    {
        var directory = Path.Combine(FileSystem.Current.CacheDirectory, "relatorios-preview");
        return DownloadAsync(request, directory, "preview");
    }

    /// <summary>
    /// Gera a versao final de um relatorio num diretorio de destino.
    /// </summary>
    /// <param name="request">Contexto funcional do relatorio a gerar.</param>
    /// <param name="directory">Diretorio opcional de destino.</param>
    /// <returns>Resultado com o ficheiro gerado.</returns>
    public Task<RelatorioFileResult> GenerateAsync(RelatorioExportRequest request, string? directory = null)
    {
        var targetDirectory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(FileSystem.Current.AppDataDirectory, "relatorios-gerados")
            : directory;

        return DownloadAsync(request, targetDirectory, "gerado");
    }

    /// <summary>
    /// Garante que existe uma ficha de producao para o contexto pedido.
    /// </summary>
    /// <param name="tipoRelatorio">Tipo funcional da ficha pretendida.</param>
    /// <param name="encomendaMoldeId">Associacao encomenda-molde usada como contexto.</param>
    /// <returns>Resumo da ficha garantida ou criada.</returns>
    public async Task<FichaProducaoResumoDto?> EnsureFichaAsync(string tipoRelatorio, int encomendaMoldeId)
    {
        var payload = new
        {
            Tipo = tipoRelatorio.ToUpperInvariant(),
            EncomendaMolde_id = encomendaMoldeId
        };

        using var response = await HttpClient.PostAsJsonAsync("api/fichas-producao/ensure", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel garantir a ficha {tipoRelatorio}.");

        return await DeserializeAsync<FichaProducaoResumoDto>(response);
    }

    /// <summary>
    /// Lista linhas da FOP geral dentro de um intervalo temporal.
    /// </summary>
    /// <param name="dataInicio">Data inicial do filtro.</param>
    /// <param name="dataFim">Data final do filtro.</param>
    /// <param name="page">Pagina atual a consultar.</param>
    /// <param name="pageSize">Quantidade de itens por pagina.</param>
    /// <returns>Resultado paginado com linhas da FOP geral ou nulo quando a API nao devolve sucesso.</returns>
    public async Task<PagedResult<FopGeralLinhaDto>?> GetFopGeralAsync(DateTime dataInicio, DateTime dataFim, int page, int pageSize)
    {
        var inicio = Uri.EscapeDataString(dataInicio.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        var fim = Uri.EscapeDataString(dataFim.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        using var response = await HttpClient.GetAsync(
            $"api/fichas-producao/fop-geral?dataInicio={inicio}&dataFim={fim}&page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<FopGeralLinhaDto>>(response);
    }

    /// <summary>
    /// Gera o ficheiro exportado da FOP geral.
    /// </summary>
    /// <param name="dataInicio">Data inicial do filtro.</param>
    /// <param name="dataFim">Data final do filtro.</param>
    /// <param name="directory">Diretorio opcional de destino.</param>
    /// <returns>Resultado com o ficheiro exportado.</returns>
    public async Task<RelatorioFileResult> GenerateFopGeralAsync(DateTime dataInicio, DateTime dataFim, string? directory = null)
    {
        var targetDirectory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(FileSystem.Current.AppDataDirectory, "relatorios-gerados")
            : directory;

        var inicio = Uri.EscapeDataString(dataInicio.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        var fim = Uri.EscapeDataString(dataFim.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        var endpoint = $"api/fichas-producao/fop-geral/export?dataInicio={inicio}&dataFim={fim}";

        Directory.CreateDirectory(targetDirectory);

        using var response = await HttpClient.GetAsync(endpoint);
        await EnsureSuccessAsync(response, "Nao foi possivel gerar o relatorio FOP geral.");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var fallbackRequest = new RelatorioExportRequest
        {
            TipoRelatorio = "FOP",
            NumeroMolde = "geral"
        };

        var fileName = ResolveFileName(response.Content.Headers.ContentDisposition, fallbackRequest, "gerado");
        var filePath = Path.Combine(targetDirectory, fileName);

        await File.WriteAllBytesAsync(filePath, bytes);

        return new RelatorioFileResult
        {
            FileName = fileName,
            FilePath = filePath
        };
    }

    private async Task<RelatorioFileResult> DownloadAsync(
        RelatorioExportRequest request,
        string directory,
        string prefix)
    {
        var endpoint = BuildEndpoint(request);

        Directory.CreateDirectory(directory);

        using var response = await HttpClient.GetAsync(endpoint);
        await EnsureSuccessAsync(response, $"Nao foi possivel gerar o relatorio {request.TipoRelatorio}.");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var fileName = ResolveFileName(response.Content.Headers.ContentDisposition, request, prefix);
        var filePath = Path.Combine(directory, fileName);

        await File.WriteAllBytesAsync(filePath, bytes);

        return new RelatorioFileResult
        {
            FileName = fileName,
            FilePath = filePath
        };
    }

    private static string BuildEndpoint(RelatorioExportRequest request)
    {
        return request.TipoRelatorio.ToUpperInvariant() switch
        {
            "FLT" => $"api/fichas-producao/encomendas-moldes/{request.EncomendaMoldeId}/export/flt",
            "FRE" when request.FichaProducaoId.HasValue => $"api/fichas-producao/{request.FichaProducaoId.Value}/export/fre",
            "FRM" when request.FichaProducaoId.HasValue => $"api/fichas-producao/{request.FichaProducaoId.Value}/export/frm",
            "FRA" when request.FichaProducaoId.HasValue => $"api/fichas-producao/{request.FichaProducaoId.Value}/export/fra",
            "FOP" when request.FichaProducaoId.HasValue => $"api/fichas-producao/{request.FichaProducaoId.Value}/export/fop",
            _ => throw new InvalidOperationException("O pedido do relatorio nao contem contexto suficiente para exportacao.")
        };
    }

    private static string ResolveFileName(
        ContentDispositionHeaderValue? contentDisposition,
        RelatorioExportRequest request,
        string prefix)
    {
        var rawFileName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName;
        var cleanFileName = string.IsNullOrWhiteSpace(rawFileName)
            ? $"{prefix}-{request.TipoRelatorio.ToLowerInvariant()}-{Sanitize(request.NumeroMolde)}.xlsx"
            : rawFileName.Trim().Trim('"');

        var extension = Path.GetExtension(cleanFileName);
        var baseName = Path.GetFileNameWithoutExtension(cleanFileName);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        return $"{prefix}-{timestamp}-{Sanitize(baseName)}{(string.IsNullOrWhiteSpace(extension) ? ".xlsx" : extension)}";
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "relatorio";

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Where(character => !invalidChars.Contains(character)).ToArray()).Trim();

        return string.IsNullOrWhiteSpace(sanitized)
            ? "relatorio"
            : sanitized;
    }
}
