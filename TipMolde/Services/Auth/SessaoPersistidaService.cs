using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TipMolde.Services;

/// <summary>
/// Gere a persistencia local da sessao autenticada do frontend.
/// </summary>
/// <remarks>
/// Sincroniza preferencias locais, secure storage e cabecalho Authorization
/// do cliente HTTP partilhado pela aplicacao.
/// </remarks>
public sealed class SessaoPersistidaService
{
    private const string RememberSessionKey = "remember_session";
    private const string AuthTokenKey = "auth_token";
    private const string AuthTokenExpiresAtKey = "auth_token_expires_at";

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Construtor do servico de sessao persistida.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP cuja autenticacao deve acompanhar o estado da sessao.</param>
    public SessaoPersistidaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Indica se o utilizador escolheu manter a sessao entre arranques da aplicacao.
    /// </summary>
    public static bool ShouldRememberSession => Preferences.Default.Get(RememberSessionKey, false);

    /// <summary>
    /// Guarda a sessao autenticada e atualiza o cliente HTTP ativo.
    /// </summary>
    /// <param name="token">JWT devolvido pela API apos autenticacao.</param>
    /// <param name="expiresAt">Instante UTC de expiracao do token.</param>
    /// <param name="rememberSession">Indica se a sessao deve sobreviver ao fecho da app.</param>
    /// <returns>Tarefa assincrona que representa a gravacao da sessao.</returns>
    public async Task SaveSessionAsync(string token, DateTimeOffset expiresAt, bool rememberSession)
    {
        Preferences.Default.Set(RememberSessionKey, rememberSession);

        if (!rememberSession)
        {
            ClearHttpAuthorization();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            SecureStorage.Default.Remove(AuthTokenKey);
            Preferences.Default.Remove(AuthTokenExpiresAtKey);
            return;
        }

        await SecureStorage.Default.SetAsync(AuthTokenKey, token);
        Preferences.Default.Set(AuthTokenExpiresAtKey, expiresAt.UtcDateTime.ToString("O"));

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Tenta restaurar a sessao persistida no arranque da aplicacao.
    /// </summary>
    /// <returns>True quando a sessao foi restaurada e o cliente HTTP ficou autenticado; false caso contrario.</returns>
    public async Task<bool> TryRestoreSessionAsync()
    {
        if (!ShouldRememberSession)
        {
            ClearHttpAuthorization();
            return false;
        }

        var token = await SecureStorage.Default.GetAsync(AuthTokenKey);
        var expiresAtRaw = Preferences.Default.Get(AuthTokenExpiresAtKey, string.Empty);

        if (string.IsNullOrWhiteSpace(token) ||
            !DateTimeOffset.TryParseExact(
                expiresAtRaw,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expiresAt) ||
            expiresAt <= DateTimeOffset.UtcNow)
        {
            await ClearSessionAsync();
            return false;
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return true;
    }

    /// <summary>
    /// Remove a sessao local e limpa a autenticacao aplicada ao cliente HTTP.
    /// </summary>
    /// <returns>Tarefa concluida quando o estado local foi limpo.</returns>
    public Task ClearSessionAsync()
    {
        ClearHttpAuthorization();

        SecureStorage.Default.Remove(AuthTokenKey);
        Preferences.Default.Remove(AuthTokenExpiresAtKey);
        Preferences.Default.Remove(RememberSessionKey);

        return Task.CompletedTask;
    }

    private void ClearHttpAuthorization()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Tenta obter o identificador do utilizador atual a partir do token em memoria.
    /// </summary>
    /// <returns>ID do utilizador quando o token existe e contem o claim esperado; nulo caso contrario.</returns>
    public int? TryGetCurrentUserId()
    {
        var token = _httpClient.DefaultRequestHeaders.Authorization?.Parameter;

        if (string.IsNullOrWhiteSpace(token))
            return null;

        var subject = TryGetClaimFromToken(token, "sub");

        return int.TryParse(subject, out var userId) ? userId : null;
    }

    private static string? TryGetClaimFromToken(string token, string claimName)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
                return null;

            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            switch (payload.Length % 4)
            {
                case 2:
                    payload += "==";
                    break;
                case 3:
                    payload += "=";
                    break;
            }

            var bytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(bytes);

            using var document = JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty(claimName, out var property))
                return property.GetString();

            return null;
        }
        catch
        {
            return null;
        }
    }
}
