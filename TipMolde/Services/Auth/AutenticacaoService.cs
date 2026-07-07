using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TipMolde.Models;

namespace TipMolde.Services;

/// <summary>
/// Executa o fluxo de autenticacao contra a API do backend.
/// </summary>
/// <remarks>
/// Centraliza a chamada de login e atualiza o cabecalho Authorization
/// do cliente HTTP quando a API devolve um token valido.
/// </remarks>
public sealed class AutenticacaoService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Construtor do servico de autenticacao.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado para comunicar com a API.</param>
    public AutenticacaoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Autentica o utilizador com email e palavra-passe.
    /// </summary>
    /// <param name="email">Email usado como identificador de login.</param>
    /// <param name="password">Palavra-passe fornecida pelo utilizador.</param>
    /// <returns>Resposta de autenticacao com token e metadados de sessao.</returns>
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

