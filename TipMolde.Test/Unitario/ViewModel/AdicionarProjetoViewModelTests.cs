using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

/// <summary>
/// Testes unitarios do ViewModel de criacao de projeto do frontend.
/// </summary>
/// <remarks>
/// Valida o carregamento dos moldes e a pesquisa local na selecao do molde.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class AdicionarProjetoViewModelTests
{
    private AdicionarProjetoViewModel _sut = null!;
    private List<RecordedRequest> _requests = null!;

    [SetUp]
    public void SetUp()
    {
        var httpClient = CreateHttpClient(HandleRequest, out _requests);

        _sut = new AdicionarProjetoViewModel(
            new MoldesService(httpClient),
            new ProjetosService(httpClient),
            new AuthorizationService(new SessaoPersistidaService(httpClient), new UtilizadoresService(httpClient)),
            new DialogServiceStub());
    }

    [Test(Description = "T1FRT - A criacao de projeto deve carregar todos os moldes e selecionar o primeiro por defeito.")]
    public async Task LoadAsync_Should_PopulateFilteredMoldesAndSelectFirst_When_AdminOpensForm()
    {
        // ACT
        await _sut.LoadAsync();

        // ASSERT
        _sut.IsAdmin.Should().BeTrue();
        _sut.Moldes.Should().HaveCount(3);
        _sut.MoldesFiltrados.Should().HaveCount(3);
        _sut.SelectedMolde.Should().NotBeNull();
        _sut.SelectedMolde!.MoldeId.Should().Be(1);
        _requests.Should().ContainSingle(request => request.Path == "/api/moldes?page=1&pageSize=100");
    }

    [Test(Description = "T2FRT - A pesquisa local deve filtrar a lista de moldes sem perder a selecao correspondente.")]
    public async Task MoldeSearchTerm_Should_FilterMoldes_When_UserSearches()
    {
        // ARRANGE
        await _sut.LoadAsync();

        // ACT
        _sut.MoldeSearchTerm = "Tampa";

        // ASSERT
        _sut.MoldesFiltrados.Should().HaveCount(1);
        _sut.MoldesFiltrados.Single().MoldeId.Should().Be(2);
        _sut.SelectedMolde.Should().NotBeNull();
        _sut.SelectedMolde!.MoldeId.Should().Be(2);
    }

    [Test(Description = "T2.1FRT - A pesquisa local deve aceitar termos alfanumericos sem erros.")]
    public async Task MoldeSearchTerm_Should_HandleAlphanumericSearch_When_UserTypesMixedText()
    {
        // ARRANGE
        await _sut.LoadAsync();

        // ACT
        _sut.MoldeSearchTerm = "M001";

        // ASSERT
        _sut.MoldesFiltrados.Should().ContainSingle(item => item.MoldeId == 1);
        _sut.SelectedMolde.Should().NotBeNull();
        _sut.SelectedMolde!.MoldeId.Should().Be(1);
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
                    Nome = "Administrador",
                    Email = "admin@tipmolde.pt",
                    Role = "ADMIN"
                }),
            "/api/moldes?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<MoldeDto>
                {
                    Items =
                    [
                        new MoldeDto
                        {
                            MoldeId = 1,
                            Numero = "M-001",
                            Nome = "Molde Base",
                            NumeroMoldeCliente = "C-001",
                            TipoPedido = "Normal"
                        },
                        new MoldeDto
                        {
                            MoldeId = 2,
                            Numero = "M-002",
                            Nome = "Molde Tampa",
                            NumeroMoldeCliente = "C-002",
                            TipoPedido = "Normal"
                        },
                        new MoldeDto
                        {
                            MoldeId = 3,
                            Numero = "M-003",
                            Nome = "Molde Filtro",
                            NumeroMoldeCliente = "C-003",
                            TipoPedido = "Normal"
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 3
                }),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        out List<RecordedRequest> requests)
    {
        var handler = new RecordingHttpMessageHandler(responder);
        requests = handler.Requests;

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1, "ADMIN"));

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

    private static string CreateJwtToken(int userId, string role)
    {
        var header = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncode(JsonSerializer.Serialize(new
        {
            sub = userId.ToString(),
            role
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

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new RecordedRequest(request.Method, request.RequestUri?.PathAndQuery ?? string.Empty, body));
            return _responder(request);
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Path, string Body);

    private sealed class DialogServiceStub : IDialogService
    {
        public Task<string> ShowOptionsAsync(string message, string action) => Task.FromResult(string.Empty);
        public Task<string?> ShowSelectionAsync(string title, string cancel, params string[] options) => Task.FromResult<string?>(null);
        public Task<string?> PromptAsync(string title, string message, PromptDialogOptions? options = null) => Task.FromResult<string?>(null);
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Task.FromResult(false);
        public Task<bool> ConfirmDeleteAsync(string message) => Task.FromResult(false);
        public Task ShowSuccessAsync(string title, string message) => Task.CompletedTask;
        public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;
        public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    }
}
