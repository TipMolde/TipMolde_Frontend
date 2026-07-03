using System.Net.Http.Headers;
using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class RevisoesService : ApiServiceBase
{
    public RevisoesService(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public async Task<RevisaoDto?> CreateAsync(int projetoId, string descricaoAlteracoes)
    {
        var payload = new
        {
            DescricaoAlteracoes = descricaoAlteracoes,
            Projeto_id = projetoId
        };

        using var response = await HttpClient.PostAsJsonAsync("api/revisoes", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel criar a revisao do projeto {projetoId}. Estado: {(int)response.StatusCode}");

        return await DeserializeAsync<RevisaoDto>(response);
    }

    public async Task UpdateRespostaClienteAsync(
        int revisaoId,
        bool aprovado,
        string? feedbackTexto,
        string? feedbackImagemPath)
    {
        var payload = new
        {
            Aprovado = aprovado,
            FeedbackTexto = feedbackTexto,
            FeedbackImagemPath = feedbackImagemPath
        };

        using var response = await HttpClient.PutAsJsonAsync($"api/revisoes/{revisaoId}/resposta-cliente", payload);
        await EnsureSuccessAsync(response, $"Nao foi possivel registar a resposta da revisao {revisaoId}. Estado: {(int)response.StatusCode}");
    }

    public async Task UpdateRespostaClienteComAnexoAsync(
        int revisaoId,
        bool aprovado,
        string? feedbackTexto,
        string attachmentPath)
    {
        if (aprovado)
            throw new ArgumentException("O anexo da revisao so pode ser enviado quando a revisao e reprovada.", nameof(aprovado));

        if (string.IsNullOrWhiteSpace(attachmentPath))
            throw new ArgumentException("O caminho do anexo e obrigatorio.", nameof(attachmentPath));

        if (!File.Exists(attachmentPath))
            throw new FileNotFoundException("O anexo selecionado nao foi encontrado.", attachmentPath);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(aprovado.ToString().ToLowerInvariant()), "Aprovado");

        if (!string.IsNullOrWhiteSpace(feedbackTexto))
            content.Add(new StringContent(feedbackTexto), "FeedbackTexto");

        await using var stream = File.OpenRead(attachmentPath);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(attachmentPath));
        content.Add(fileContent, "Anexo", Path.GetFileName(attachmentPath));

        using var response = await HttpClient.PutAsync($"api/revisoes/{revisaoId}/resposta-cliente", content);
        await EnsureSuccessAsync(response, $"Nao foi possivel registar a resposta da revisao {revisaoId}. Estado: {(int)response.StatusCode}");
    }

    public async Task<string> DownloadAnexoAsync(string attachmentPath)
    {
        if (string.IsNullOrWhiteSpace(attachmentPath))
            throw new ArgumentException("O caminho do anexo e obrigatorio.", nameof(attachmentPath));

        var normalizedPath = attachmentPath.TrimStart('/');
        if (!normalizedPath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            normalizedPath = $"uploads/{normalizedPath}";

        using var response = await HttpClient.GetAsync(normalizedPath);
        await EnsureSuccessAsync(response, $"Nao foi possivel descarregar o anexo {Path.GetFileName(normalizedPath)}.");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var fileName = Path.GetFileName(normalizedPath);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = $"anexo-{DateTime.Now:yyyyMMdd-HHmmss}.bin";

        var targetDirectory = ResolveDownloadsDirectory();
        Directory.CreateDirectory(targetDirectory);

        var targetPath = Path.Combine(targetDirectory, fileName);
        await File.WriteAllBytesAsync(targetPath, bytes);

        return targetPath;
    }

    private static string GetContentType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }

    private static string ResolveDownloadsDirectory()
    {
        try
        {
            var downloads = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            if (Directory.Exists(downloads))
                return downloads;
        }
        catch
        {
            // Fallback below.
        }

        try
        {
            return Path.Combine(FileSystem.Current.AppDataDirectory, "downloads");
        }
        catch
        {
            return Path.Combine(Path.GetTempPath(), "downloads");
        }
    }
}
