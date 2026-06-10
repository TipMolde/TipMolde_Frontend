using Microsoft.Maui.Storage;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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

    public int? TryGetCurrentUserId()
    {
        var token = _httpClient.DefaultRequestHeaders.Authorization?.Parameter;

        if (string.IsNullOrWhiteSpace(token))
            return null;

        var subject = TryGetClaimFromToken(token, "sub");

        return int.TryParse(subject, out var userId) ? userId : null;
    }

    public string? TryGetCurrentUserRole()
    {
        var token = _httpClient.DefaultRequestHeaders.Authorization?.Parameter;

        if (string.IsNullOrWhiteSpace(token))
            return null;

        return TryGetClaimFromToken(token, "role")
            ?? TryGetClaimFromToken(token, "roles")
            ?? TryGetClaimFromToken(token, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
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
