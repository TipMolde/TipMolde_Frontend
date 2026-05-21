using System.Net.Http.Headers;
using Microsoft.Maui.Storage;

namespace TipMolde.Services;

public sealed class SessaoPersistidaService
{
    private const string RememberSessionKey = "remember_session";
    private const string AuthTokenKey = "auth_token";
    private const string AuthTokenExpiresAtKey = "auth_token_expires_at";

    private readonly HttpClient _httpClient;

    public SessaoPersistidaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool ShouldRememberSession => Preferences.Default.Get(RememberSessionKey, false);

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
            !DateTimeOffset.TryParse(expiresAtRaw, out var expiresAt) ||
            expiresAt <= DateTimeOffset.UtcNow)
        {
            await ClearSessionAsync();
            return false;
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return true;
    }

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
}
