using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class AutenticacaoService
{
    private readonly HttpClient _httpClient;

    public AutenticacaoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ResponseLoginDto> LoginAsync(string email, string password)
    {
        var loginRequest = new LoginDto
        {
            Email = email,
            Password = password
        };

        using var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Acesso não autorizado");

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Email ou palavra-passe incorretos.");

        var authResponse = await response.Content.ReadFromJsonAsync<ResponseLoginDto>();

        if (authResponse is null || string.IsNullOrWhiteSpace(authResponse.Token))
            throw new InvalidOperationException("A API nao devolveu um token valido.");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse.Token);

        return authResponse;
    }
}
