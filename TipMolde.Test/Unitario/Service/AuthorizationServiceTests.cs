using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.Test.Unitario.Service;

/// <summary>
/// Testes unitarios do servico de autorizacao do frontend.
/// </summary>
/// <remarks>
/// Valida a obtencao da role pelo backend e as regras de permissao expostas na interface.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class AuthorizationServiceTests
{
    private AuthorizationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var httpClient = CreateHttpClient(HandleRequest);

        var sessaoPersistidaService = new SessaoPersistidaService(httpClient);
        var utilizadoresService = new UtilizadoresService(httpClient);

        _sut = new AuthorizationService(sessaoPersistidaService, utilizadoresService);
    }

    /// <summary>
    /// Define a role em cache para simular a sessao ativa do utilizador.
    /// </summary>
    /// <param name="role">Role a injetar no cache interno do servico.</param>
    private void SetCachedRole(string? role)
    {
        typeof(AuthorizationService)
            .GetField("_cachedRole", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_sut, role);
    }

    /// <summary>
    /// Define o identificador em cache para simular a associacao ao utilizador atual.
    /// </summary>
    /// <param name="userId">Identificador a injetar no cache interno do servico.</param>
    private void SetCachedUserId(int? userId)
    {
        typeof(AuthorizationService)
            .GetField("_cachedUserId", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_sut, userId);
    }

    [Test(Description = "T0FRT - O servico deve obter a role atual a partir do endpoint /api/users/me.")]
    public async Task GetCurrentRoleAsync_Should_ReturnRoleFromBackend_When_UserIsAuthenticated()
    {
        // ACT
        var result = await _sut.GetCurrentRoleAsync();

        // ASSERT
        result.Should().Be("GESTOR_PRODUCAO");
    }

    [Test(Description = "T1FRT - O administrador deve poder criar maquinas.")]
    public void CanCreateMachines_Should_ReturnTrue_When_RoleIsAdmin()
    {
        // ARRANGE
        SetCachedRole("ADMIN");
        SetCachedUserId(1);

        // ACT
        var result = _sut.CanCreateMachines();

        // ASSERT
        result.Should().BeTrue();
    }

    [Test(Description = "T2FRT - O gestor de comercial nao deve poder criar maquinas.")]
    public void CanCreateMachines_Should_ReturnFalse_When_RoleIsCommercialManager()
    {
        // ARRANGE
        SetCachedRole("GESTOR_COMERCIAL");
        SetCachedUserId(1);

        // ACT
        var result = _sut.CanCreateMachines();

        // ASSERT
        result.Should().BeFalse();
    }

    [Test(Description = "T3FRT - O gestor de producao deve poder alterar o estado das maquinas.")]
    public void CanEditMachineState_Should_ReturnTrue_When_RoleIsProductionManager()
    {
        // ARRANGE
        SetCachedRole("GESTOR_PRODUCAO");
        SetCachedUserId(1);

        // ACT
        var result = _sut.CanEditMachineState();

        // ASSERT
        result.Should().BeTrue();
    }

    [Test(Description = "T4FRT - O gestor de desenho deve poder gerir pecas.")]
    public void CanManagePieces_Should_ReturnTrue_When_RoleIsDesignManager()
    {
        // ARRANGE
        SetCachedRole("GESTOR_DESENHO");
        SetCachedUserId(1);

        // ACT
        var result = _sut.CanManagePieces();

        // ASSERT
        result.Should().BeTrue();
    }

    [Test(Description = "T5FRT - O servico deve limpar o cache interno quando pedido.")]
    public void Clear_Should_ResetCachedState()
    {
        // ARRANGE
        SetCachedRole("ADMIN");
        SetCachedUserId(1);

        // ACT
        _sut.Clear();

        // ASSERT
        typeof(AuthorizationService)
            .GetField("_cachedRole", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_sut)
            .Should()
            .BeNull();

        typeof(AuthorizationService)
            .GetField("_cachedUserId", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_sut)
            .Should()
            .BeNull();
    }

    private static HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/users/me" => CreateJsonResponse(
                HttpStatusCode.OK,
                new UtilizadorDto
                {
                    User_id = 1,
                    Nome = "Gestor Producao",
                    Email = "gestor@tipmolde.pt",
                    Role = "GESTOR_PRODUCAO"
                }),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new RecordingHttpMessageHandler(responder);

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1));

        return httpClient;
    }

    private static HttpResponseMessage CreateJsonResponse<T>(HttpStatusCode statusCode, T value)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(value),
                Encoding.UTF8,
                "application/json")
        };
    }

    private static string CreateJwtToken(int userId)
    {
        var header = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncode(JsonSerializer.Serialize(new
        {
            sub = userId.ToString()
        }));

        return $"{header}.{payload}.signature";
    }

    private static string Base64UrlEncode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
}
