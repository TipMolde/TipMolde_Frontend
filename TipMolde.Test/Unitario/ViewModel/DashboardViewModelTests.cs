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
/// Testes unitarios do dashboard do frontend.
/// </summary>
/// <remarks>
/// Garante que a area de registo de chegada de material apenas expõe moldes com pedido de material ativo.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class DashboardViewModelTests
{
    private DashboardViewModel _sut = null!;
    private List<RecordedRequest> _requests = null!;

    [SetUp]
    public void SetUp()
    {
        var httpClient = CreateHttpClient(HandleRequest, out _requests);

        _sut = new DashboardViewModel(
            new EncomendasService(httpClient),
            new MoldesService(httpClient),
            new PecasService(httpClient),
            new AuthorizationService(new SessaoPersistidaService(httpClient), new UtilizadoresService(httpClient)),
            new DialogServiceStub());
    }

    [Test(Description = "T1FRT - O dashboard deve mostrar apenas moldes com pedido de material ativo na rececao.")]
    public async Task LoadAsync_Should_FilterRececaoMoldesByActiveMaterialOrder_When_DashboardOpens()
    {
        // ACT
        await _sut.LoadAsync();

        // ASSERT
        _sut.MoldesRececaoDisponiveis.Should().ContainSingle();
        _sut.MoldesRececaoDisponiveis.Single().MoldeId.Should().Be(1);
        _sut.SelectedMoldeRececao.Should().NotBeNull();
        _sut.SelectedMoldeRececao!.MoldeId.Should().Be(1);
        _requests.Should().Contain(request =>
            request.Path == "/api/pecas/por-molde/1/pendentes-rececao-material?page=1&pageSize=1");
        _requests.Should().Contain(request =>
            request.Path == "/api/pecas/por-molde/2/pendentes-rececao-material?page=1&pageSize=1");
    }

    private static HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/encomendas/em-producao?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<EncomendaResumoDto>
                {
                    Items =
                    [
                        new EncomendaResumoDto
                        {
                            Encomenda_id = 1,
                            NumeroEncomendaCliente = "ENC-001",
                            NomeCliente = "Cliente Exemplo",
                            Estado = "EM_PRODUCAO",
                            DataRegisto = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
                            Cliente_id = 10
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 1
                }),
            "/api/encomendas?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<EncomendaResumoDto>
                {
                    Items =
                    [
                        new EncomendaResumoDto
                        {
                            Encomenda_id = 1,
                            NumeroEncomendaCliente = "ENC-001",
                            NomeCliente = "Cliente Exemplo",
                            Estado = "EM_PRODUCAO",
                            DataRegisto = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
                            Cliente_id = 10
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 1
                }),
            "/api/encomenda-moldes/fila-global?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<FilaGlobalMoldeItemDto>
                {
                    Items =
                    [
                        new FilaGlobalMoldeItemDto
                        {
                            EncomendaMoldeId = 100,
                            EncomendaId = 1,
                            MoldeId = 1,
                            Prioridade = 1,
                            DataEntregaPrevista = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc),
                            Quantidade = 10,
                            NumeroEncomendaCliente = "ENC-001",
                            NomeCliente = "Cliente Exemplo",
                            NumeroMolde = "M-001",
                            NomeMolde = "Molde Ativo",
                            EstadoEncomenda = "EM_PRODUCAO"
                        },
                        new FilaGlobalMoldeItemDto
                        {
                            EncomendaMoldeId = 101,
                            EncomendaId = 1,
                            MoldeId = 2,
                            Prioridade = 2,
                            DataEntregaPrevista = new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc),
                            Quantidade = 12,
                            NumeroEncomendaCliente = "ENC-001",
                            NomeCliente = "Cliente Exemplo",
                            NumeroMolde = "M-002",
                            NomeMolde = "Molde Sem Pedido",
                            EstadoEncomenda = "EM_PRODUCAO"
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 2
                }),
            "/api/moldes/1" => CreateJsonResponse(
                HttpStatusCode.OK,
                new MoldeDto
                {
                    MoldeId = 1,
                    Numero = "M-001",
                    Nome = "Molde Ativo"
                }),
            "/api/moldes/1/dashboard-ciclo-vida" => CreateJsonResponse(
                HttpStatusCode.OK,
                new MoldeCicloVidaDashboardDto
                {
                    MoldeId = 1,
                    NumeroMolde = "M-001",
                    TotalPecas = 2,
                    MaterialPendente = 1,
                    PercentagemConclusao = 50m
                }),
            "/api/pecas/por-molde/1/pendentes-rececao-material?page=1&pageSize=1" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items =
                    [
                        new PecaDto
                        {
                            PecaId = 11,
                            NumeroPeca = "P-001",
                            Designacao = "Peca 1",
                            Prioridade = 1,
                            Quantidade = 5,
                            MaterialRecebido = false,
                            Molde_id = 1
                        }
                    ],
                    Page = 1,
                    PageSize = 1,
                    TotalItems = 1
                }),
            "/api/pecas/por-molde/1/pendentes-rececao-material?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items =
                    [
                        new PecaDto
                        {
                            PecaId = 11,
                            NumeroPeca = "P-001",
                            Designacao = "Peca 1",
                            Prioridade = 1,
                            Quantidade = 5,
                            MaterialRecebido = false,
                            Molde_id = 1
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 1
                }),
            "/api/pecas/por-molde/2/pendentes-rececao-material?page=1&pageSize=1" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items = [],
                    Page = 1,
                    PageSize = 1,
                    TotalItems = 0
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
        public Page GetCurrentPage() => new ContentPage();
        public Task<string> ShowOptionsAsync(string message, string action) => Task.FromResult(string.Empty);
        public Task<string?> ShowSelectionAsync(string title, string cancel, params string[] options) => Task.FromResult<string?>(null);
        public Task<string?> PromptAsync(string title, string message, PromptDialogOptions? options = null) => Task.FromResult<string?>(null);
        public Task<bool> ConfirmDeleteAsync(string message) => Task.FromResult(false);
        public Task ShowSuccessAsync(string title, string message) => Task.CompletedTask;
        public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;
        public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    }
}
