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
/// Testes unitarios do carregamento da pagina de producao do frontend.
/// </summary>
/// <remarks>
/// Garante que o carregamento inicial nao requisita maquinas antes de serem necessarias.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class ProducaoViewModelLoadTests
{
    private ProducaoViewModel _sut = null!;
    private List<RecordedRequest> _requests = null!;

    [SetUp]
    public void SetUp()
    {
        var httpClient = CreateHttpClient(HandleRequest, out _requests);

        var dependencies = new ProducaoViewModelDependencies(
            new EncomendasService(httpClient),
            new PecasService(httpClient),
            new FasesProducaoService(httpClient),
            new MaquinasService(httpClient),
            new RegistosProducaoService(httpClient));

        _sut = new ProducaoViewModel(
            dependencies,
            new SessaoPersistidaService(httpClient),
            new UtilizadoresService(httpClient),
            new DialogServiceStub());
    }

    [Test(Description = "T1FRT - O carregamento inicial da pagina de producao nao deve pedir maquinas.")]
    public async Task LoadAsync_Should_NotRequestMachines_When_OpeningProductionPage()
    {
        // ACT
        await _sut.LoadAsync();

        // ASSERT
        _sut.PecasDisponiveis.Should().NotBeEmpty();
        _requests.Should().NotContain(request => request.Path.StartsWith("/api/Maquina", StringComparison.OrdinalIgnoreCase));
    }

    private HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/encomenda-moldes/fila-global?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<FilaGlobalMoldeItemDto>
                {
                    Items =
                    [
                        new FilaGlobalMoldeItemDto
                        {
                            EncomendaMoldeId = 1,
                            EncomendaId = 10,
                            MoldeId = 1,
                            Prioridade = 1,
                            DataEntregaPrevista = new DateTime(2026, 6, 18),
                            Quantidade = 100,
                            NumeroEncomendaCliente = "ENC-001",
                            NomeCliente = "Cliente Teste",
                            NumeroMolde = "M-001",
                            NomeMolde = "Molde Teste",
                            EstadoEncomenda = "CONFIRMADA"
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 1
                }),
            "/api/fases-producao?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<FaseProducaoItem>
                {
                    Items =
                    [
                        new FaseProducaoItem
                        {
                            FasesProducao_id = 1,
                            Nome = "MONTAGEM",
                            Descricao = "Montagem final"
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 1
                }),
            "/api/users/7" => CreateJsonResponse(
                HttpStatusCode.OK,
                new UtilizadorDto
                {
                    User_id = 7,
                    Nome = "Gestor Teste",
                    Role = "ADMIN"
                }),
            "/api/pecas/por-molde/1?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items =
                    [
                        new PecaDto
                        {
                            PecaId = 11,
                            NumeroPeca = "P-011",
                            Designacao = "Peca Teste",
                            Quantidade = 4,
                            Molde_id = 1,
                            MaterialRecebido = true,
                            ProximaFase_id = 1
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 1
                }),
            "/api/RegistosProducao/ultimo?faseId=1&pecaId=11" => new HttpResponseMessage(HttpStatusCode.NotFound),
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
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(7, "ADMIN"));

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
        public Page GetCurrentPage() => new ContentPage();
        public Task<string> ShowOptionsAsync(string message, string action) => Task.FromResult(string.Empty);
        public Task<string?> ShowSelectionAsync(string title, string cancel, params string[] options) => Task.FromResult<string?>(null);
        public Task<string?> PromptAsync(string title, string message, PromptDialogOptions? options = null) => Task.FromResult<string?>(null);
        public Task<bool> ConfirmDeleteAsync(string message) => Task.FromResult(false);
        public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;
        public Task ShowSuccessAsync(string title, string message) => Task.CompletedTask;
        public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    }
}
