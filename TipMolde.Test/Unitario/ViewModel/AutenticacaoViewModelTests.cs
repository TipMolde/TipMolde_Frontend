using FluentAssertions;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

[TestFixture]
[Category("Unit")]
public class AutenticacaoViewModelTests
{
    [Test(Description = "TAUTHVM1 - O login deve falhar localmente quando as credenciais nao sao preenchidas.")]
    public async Task LoginCommand_Should_ShowValidationError_When_CredentialsAreMissing()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var navigationService = new Mock<INavigationService>();
        var sut = CreateSut(httpClient, navigationService.Object);

        // ACT
        await sut.LoginCommand.ExecuteAsync(null);

        // ASSERT
        sut.ErrorTitle.Should().Be("Credenciais em falta");
        sut.ErrorMessage.Should().Be("Indique um email valido e a respetiva palavra-passe antes de continuar.");
        sut.HasError.Should().BeTrue();
        navigationService.Verify(service => service.NavigateToDashboardAfterLoginAsync(), Times.Never);
    }

    [Test(Description = "TAUTHVM2 - O login deve expor erro funcional quando a autenticacao falha no backend.")]
    public async Task LoginCommand_Should_ShowFunctionalError_When_LoginFails()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/auth/login")
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var navigationService = new Mock<INavigationService>();
        var sut = CreateSut(httpClient, navigationService.Object);
        sut.Email = "admin@tipmolde.pt";
        sut.Password = "Errada123!";

        // ACT
        await sut.LoginCommand.ExecuteAsync(null);

        // ASSERT
        sut.ErrorTitle.Should().Be("Falha no login");
        sut.ErrorMessage.Should().Contain("Acesso");
        sut.IsLoggingIn.Should().BeFalse();
        navigationService.Verify(service => service.NavigateToDashboardAfterLoginAsync(), Times.Never);
    }

    [Test(Description = "TAUTHVM3 - O teste de ligacao deve mostrar o endpoint quando a API responde sem sucesso.")]
    public async Task TestConnectionCommand_Should_ShowEndpointError_When_HealthCheckFails()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/health")
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { status = "down" }),
                        Encoding.UTF8,
                        "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var sut = CreateSut(httpClient, new Mock<INavigationService>().Object);

        // ACT
        await sut.TestConnectionCommand.ExecuteAsync(null);

        // ASSERT
        sut.ErrorTitle.Should().Be("Sem ligacao a API");
        sut.ErrorMessage.Should().Contain("Endpoint:");
        sut.ErrorMessage.Should().Contain("https://localhost/");
        sut.IsCheckingConnection.Should().BeFalse();
    }

    private static AutenticacaoViewModel CreateSut(HttpClient httpClient, INavigationService navigationService)
    {
        var sessaoPersistidaService = new SessaoPersistidaService(
            httpClient,
            new InMemoryPreferences(),
            new InMemorySecureStorage());
        var utilizadoresService = new UtilizadoresService(httpClient);

        return new AutenticacaoViewModel(
            new ApiConnectivityService(httpClient),
            new AutenticacaoService(httpClient),
            new AuthorizationService(sessaoPersistidaService, utilizadoresService),
            sessaoPersistidaService,
            navigationService);
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        return new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://localhost/")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    private sealed class InMemoryPreferences : IPreferences
    {
        private readonly Dictionary<string, object?> _values = new();

        public bool ContainsKey(string key, string? sharedName = null)
        {
            return _values.ContainsKey(BuildKey(key, sharedName));
        }

        public void Remove(string key, string? sharedName = null)
        {
            _values.Remove(BuildKey(key, sharedName));
        }

        public void Clear(string? sharedName = null)
        {
            var prefix = $"{sharedName ?? string.Empty}::";
            var keys = _values.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToList();

            foreach (var key in keys)
                _values.Remove(key);
        }

        public void Set<T>(string key, T value, string? sharedName = null)
        {
            _values[BuildKey(key, sharedName)] = value;
        }

        public T Get<T>(string key, T defaultValue, string? sharedName = null)
        {
            return _values.TryGetValue(BuildKey(key, sharedName), out var value) && value is T typed
                ? typed
                : defaultValue;
        }

        private static string BuildKey(string key, string? sharedName)
        {
            return $"{sharedName ?? string.Empty}::{key}";
        }
    }

    private sealed class InMemorySecureStorage : ISecureStorage
    {
        private readonly Dictionary<string, string> _values = new();

        public Task SetAsync(string key, string value)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key)
        {
            _values.TryGetValue(key, out var value);
            return Task.FromResult<string?>(value);
        }

        public bool Remove(string key)
        {
            return _values.Remove(key);
        }

        public void RemoveAll()
        {
            _values.Clear();
        }
    }
}
