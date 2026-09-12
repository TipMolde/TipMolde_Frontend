using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

/// <summary>
/// Testes unitarios do carregamento da pagina de producao do frontend.
/// </summary>
/// <remarks>
/// Garante que o carregamento inicial carrega os dados base e preserva o contexto pedido.
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

        _sut = new ProducaoViewModel(
            new FasesProducaoService(httpClient),
            new PecasService(httpClient),
            new SessaoPersistidaService(httpClient),
            new UtilizadoresService(httpClient),
            new RegistosProducaoService(httpClient),
            new DialogServiceStub(),
            Mock.Of<INavigationService>());
    }

    [Test(Description = "T1FRT - O carregamento inicial da pagina de producao deve carregar dados base e o contexto pedido.")]
    public async Task LoadAsync_Should_LoadReferenceDataAndPecasQueue()
    {
        // ACT
        await _sut.LoadAsync();

        // ASSERT
        _sut.GestorProducaoId.Should().Be(7);
        _sut.GestorProducaoNome.Should().Be("Gestor Teste");
        _sut.HasPecasDisponiveis.Should().BeTrue();
        _sut.PecasDisponiveis.Should().ContainSingle();
        _sut.PecasDisponiveis[0].PecaId.Should().Be(11);
        _sut.PecasDisponiveis[0].NumeroMolde.Should().Be("M-001");
        _requests.Should().Contain(request => request.Path == "/api/users/7");
        _requests.Should().Contain(request => request.Path == "/api/pecas/fila-trabalho?page=1&pageSize=10&searchMode=Molde");
    }

    [TestCase(-1)]
    [TestCase(3)]
    public async Task LoadAsync_WithInvalidPickerIndex_ShouldUseDefaultSearch(int index)
    {
        _sut.SelectedSearchModeIndex = index;
        await _sut.LoadAsync();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.PecasDisponiveis.Should().ContainSingle();
    }

    [Test]
    public async Task LoadAsync_ShouldPublishCompletePageWithoutMutatingDisplayedCollection()
    {
        await _sut.LoadAsync();
        var displayedPage = _sut.PecasDisponiveis;
        var collectionChanges = 0;
        var publishedPages = new List<int[]>();
        displayedPage.CollectionChanged += (_, _) => collectionChanges++;
        _sut.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ProducaoViewModel.PecasDisponiveis))
                publishedPages.Add(_sut.PecasDisponiveis.Select(item => item.PecaId).ToArray());
        };

        await _sut.LoadAsync();

        collectionChanges.Should().Be(0, "the bound Windows list must not receive intermediate Clear/Add events");
        publishedPages.Should().ContainSingle();
        publishedPages[0].Should().Equal(11);
        _sut.PecasDisponiveis.Should().NotBeSameAs(displayedPage);
        _sut.ErrorMessage.Should().BeEmpty();
    }

    private HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
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
            "/api/Maquina?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<MaquinaItem>
                {
                    Items =
                    [
                        new MaquinaItem
                        {
                            Maquina_id = 1,
                            Numero = 100,
                            NomeModelo = "Maq Teste",
                            Estado = "OPERACIONAL",
                            FaseDedicada_id = 1
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 1
                }),
            "/api/RegistosProducao?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<RegistoProducaoDto>
                {
                    Items = [],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 0
                }),
            "/api/users/7" => CreateJsonResponse(
                HttpStatusCode.OK,
                new UtilizadorDto
                {
                    User_id = 7,
                    Nome = "Gestor Teste",
                    Role = "ADMIN"
                }),
            "/api/pecas/fila-trabalho?page=1&pageSize=10&searchMode=Molde" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<ProducaoPecaDisponivelItem>
                {
                    Items =
                    [
                        new ProducaoPecaDisponivelItem
                        {
                            MoldeId = 1,
                            PecaId = 11,
                            PrioridadeMolde = 1,
                            PrioridadePeca = 2,
                            Quantidade = 4,
                            NumeroMolde = "M-001",
                            NomeMolde = "Molde Teste",
                            NumeroEncomendaCliente = "ENC-001",
                            NomeCliente = "Cliente Teste",
                            Designacao = "Peca Teste",
                            NumeroPeca = "P-011",
                            DataEntregaPrevista = new DateTime(2026, 6, 18),
                            UltimoEstadoGlobal = "EM_PRODUCAO",
                            UltimaFaseGlobal = "MONTAGEM",
                            ProximaFaseId = 1,
                            ProximaFaseNome = "MONTAGEM",
                            FaseTrabalho = "MONTAGEM",
                            ProximoPasso = "Continuar montagem"
                        }
                    ],
                    Page = 1,
                    PageSize = 10,
                    TotalItems = 1
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
        public Task<string> ShowOptionsAsync(string message, string action) => Task.FromResult(string.Empty);
        public Task<string?> ShowSelectionAsync(string title, string cancel, params string[] options) => Task.FromResult<string?>(null);
        public Task<string?> PromptAsync(string title, string message, PromptDialogOptions? options = null) => Task.FromResult<string?>(null);
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Task.FromResult(false);
        public Task<bool> ConfirmDeleteAsync(string message) => Task.FromResult(false);
        public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;
        public Task ShowSuccessAsync(string title, string message) => Task.CompletedTask;
        public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    }
}
