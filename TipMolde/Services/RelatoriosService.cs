using System.Net.Http.Headers;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class RelatoriosService : ApiServiceBase
{
    public RelatoriosService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<PagedResult<FichaProducaoResumoDto>?> GetFichasByMoldeIdAsync(int moldeId, int page, int pageSize)
    {
        using var response = await HttpClient.GetAsync(
            $"api/fichas-producao/by-molde?moldeId={moldeId}&page={page}&pageSize={pageSize}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await DeserializeAsync<PagedResult<FichaProducaoResumoDto>>(response);
    }

    public Task<RelatorioFileResult> PreviewAsync(RelatorioExportRequest request)
    {
        var directory = Path.Combine(FileSystem.Current.CacheDirectory, "relatorios-preview");
        return DownloadAsync(request, directory, "preview");
    }

    public Task<RelatorioFileResult> GenerateAsync(RelatorioExportRequest request, string? directory = null)
    {
        var targetDirectory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(FileSystem.Current.AppDataDirectory, "relatorios-gerados")
            : directory;

        return DownloadAsync(request, targetDirectory, "gerado");
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
