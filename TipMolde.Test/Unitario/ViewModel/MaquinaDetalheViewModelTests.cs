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

[TestFixture]
[Category("Unit")]
public class MaquinaDetalheViewModelTests
{
    private Mock<IDialogService> _dialogService = null!;
    private MaquinaDetalheViewModel _sut = null!;
    private List<RecordedRequest> _requests = null!;

    [SetUp]
    public void SetUp()
    {
        _dialogService = new Mock<IDialogService>();
        _dialogService
            .Setup(service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var httpClient = CreateHttpClient(HandleRequest, out _requests);
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(userId: 7));

        _sut = new MaquinaDetalheViewModel(
            new MaquinasService(httpClient),
            new IndustrialProducaoService(httpClient),
            new PecasService(httpClient),
            new SessaoPersistidaService(httpClient),
            new UtilizadoresService(httpClient),
            _dialogService.Object);
    }

    [Test(Description = "T1MDET - O detalhe da maquina deve carregar cinco pecas por pagina e resolver o RUNNING pendente mais antigo.")]
    [Category("Smoke")]
    public async Task LoadAsync_Should_LoadFirstFivePiecesAndOldestPendingRunningEvent()
    {
        await _sut.LoadAsync(9);

        _sut.Maquina.Should().NotBeNull();
        _sut.Maquina!.Maquina_id.Should().Be(9);
        _sut.UtilizadorAtualId.Should().Be(7);
        _sut.UtilizadorAtualNome.Should().Be("Gestor Producao");
        _sut.EventoPendente.Should().NotBeNull();
        _sut.EventoPendente!.EventoMaquinaIndustrial_id.Should().Be(91);
        _sut.PecasEncontradas.Should().HaveCount(5);
        _sut.PecasEncontradas.First().PecaId.Should().Be(101);
        _sut.Page.Should().Be(1);
        _sut.PageSize.Should().Be(5);
        _sut.TotalPages.Should().Be(2);
        _sut.CanGoNext.Should().BeTrue();
    }

    [Test(Description = "T2MDET - Ao mudar de pagina, a selecao deve ser limpa quando a peca escolhida deixa de estar visivel.")]
    public async Task NextPageCommand_Should_ClearSelection_When_SelectedPieceLeavesVisiblePage()
    {
        await _sut.LoadAsync(9);
        _sut.SelecionarPecaCommand.Execute(_sut.PecasEncontradas.First());

        await _sut.NextPageCommand.ExecuteAsync(null);

        _sut.Page.Should().Be(2);
        _sut.PecasEncontradas.Should().HaveCount(2);
        _sut.PecasEncontradas.Select(item => item.PecaId).Should().ContainInOrder(106, 107);
        _sut.SelectedPeca.Should().BeNull();
        _sut.HasSelectedPeca.Should().BeFalse();
    }

    [Test(Description = "T3MDET - A pesquisa de pecas deve repor a pagina e aplicar o filtro no backend.")]
    public async Task PesquisarPecasCommand_Should_ResetToFirstPageAndApplyFilter()
    {
        await _sut.LoadAsync(9);
        await _sut.NextPageCommand.ExecuteAsync(null);

        _sut.PesquisaPeca = "Base";
        await _sut.PesquisarPecasCommand.ExecuteAsync(null);

        _sut.Page.Should().Be(1);
        _sut.TotalPages.Should().Be(1);
        _sut.PecasEncontradas.Should().HaveCount(2);
        _sut.PecasEncontradas.Select(item => item.PecaId).Should().ContainInOrder(101, 106);
    }

    [Test(Description = "T4MDET - Ao completar contexto, o frontend deve enviar gestor, peca e fase e mostrar confirmacao.")]
    public async Task CompletarContextoCommand_Should_PostSelectedPieceAndShowSuccess()
    {
        await _sut.LoadAsync(9);
        _sut.SelecionarPecaCommand.Execute(_sut.PecasEncontradas.First());

        await _sut.CompletarContextoCommand.ExecuteAsync(null);

        _requests.Should().ContainSingle(request =>
            request.Method == HttpMethod.Post &&
            request.Path == "/api/industrial/eventos/91/completar-contexto");

        var post = _requests.Single(request =>
            request.Method == HttpMethod.Post &&
            request.Path == "/api/industrial/eventos/91/completar-contexto");

        post.Body.Should().Contain("\"operador_id\":7");
        post.Body.Should().Contain("\"peca_id\":101");
        post.Body.Should().Contain("\"fase_id\":3");
        _sut.SelectedPeca.Should().BeNull();
        _dialogService.Verify(service => service.ShowSuccessAsync(
            "Contexto registado",
            It.Is<string>(message => message.Contains("901") && message.Contains("P-101"))),
            Times.Once);
    }

    private static HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/Maquina/9" => CreateJsonResponse(
                HttpStatusCode.OK,
                new MaquinaItem
                {
                    Maquina_id = 9,
                    Numero = 901,
                    NomeModelo = "Maq CNC 01",
                    IpAddress = "192.168.0.9",
                    Estado = "DISPONIVEL",
                    FaseDedicada_id = 3,
                    FaseDedicadaNome = "MONTAGEM",
                    ProtocoloComunicacao = "OPC-UA"
                }),
            "/api/users/7" => CreateJsonResponse(
                HttpStatusCode.OK,
                new UtilizadorDto
                {
                    User_id = 7,
                    Nome = "Gestor Producao",
                    Email = "gestor@tipmolde.pt",
                    Role = "GESTOR_PRODUCAO"
                }),
            "/api/industrial/eventos/pendentes?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<IndustrialEventoDto>
                {
                    Items =
                    [
                        new IndustrialEventoDto
                        {
                            EventoMaquinaIndustrial_id = 91,
                            Maquina_id = 9,
                            EstadoMaquina = "RUNNING",
                            OccurredAt = new DateTime(2026, 7, 7, 8, 0, 0, DateTimeKind.Utc)
                        },
                        new IndustrialEventoDto
                        {
                            EventoMaquinaIndustrial_id = 92,
                            Maquina_id = 9,
                            EstadoMaquina = "RUNNING",
                            OccurredAt = new DateTime(2026, 7, 7, 9, 0, 0, DateTimeKind.Utc)
                        },
                        new IndustrialEventoDto
                        {
                            EventoMaquinaIndustrial_id = 93,
                            Maquina_id = 8,
                            EstadoMaquina = "RUNNING",
                            OccurredAt = new DateTime(2026, 7, 7, 7, 0, 0, DateTimeKind.Utc)
                        },
                        new IndustrialEventoDto
                        {
                            EventoMaquinaIndustrial_id = 94,
                            Maquina_id = 9,
                            EstadoMaquina = "STOPPED",
                            OccurredAt = new DateTime(2026, 7, 7, 6, 0, 0, DateTimeKind.Utc)
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 4
                }),
            "/api/pecas/fila-trabalho?page=1&pageSize=5&searchMode=Peca" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreatePecasPage(
                    page: 1,
                    pageSize: 5,
                    totalItems: 7,
                    [101, 102, 103, 104, 105])),
            "/api/pecas/fila-trabalho?page=2&pageSize=5&searchMode=Peca" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreatePecasPage(
                    page: 2,
                    pageSize: 5,
                    totalItems: 7,
                    [106, 107])),
            "/api/pecas/fila-trabalho?page=1&pageSize=5&searchMode=Peca&searchTerm=Base" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreatePecasPage(
                    page: 1,
                    pageSize: 5,
                    totalItems: 2,
                    [101, 106])),
            "/api/industrial/eventos/91/completar-contexto" => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            },
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private static PagedResult<ProducaoPecaDisponivelItem> CreatePecasPage(
        int page,
        int pageSize,
        int totalItems,
        IReadOnlyList<int> pieceIds)
    {
        return new PagedResult<ProducaoPecaDisponivelItem>
        {
            Items = pieceIds
                .Select(pieceId => new ProducaoPecaDisponivelItem
                {
                    PecaId = pieceId,
                    MoldeId = 9,
                    NumeroMolde = "M-901",
                    NomeMolde = "Molde CNC",
                    NumeroPeca = $"P-{pieceId}",
                    Designacao = pieceId is 101 or 106 ? $"Base {pieceId}" : $"Componente {pieceId}",
                    FaseTrabalho = "MONTAGEM"
                })
                .ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    private static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        out List<RecordedRequest> requests)
    {
        var handler = new StubHttpMessageHandler(responder);
        requests = handler.Requests;
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://tipmolde.test/")
        };
    }

    private static HttpResponseMessage CreateJsonResponse<T>(HttpStatusCode statusCode, T payload)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private static string CreateJwtToken(int userId)
    {
        var header = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncode($"{{\"sub\":\"{userId}\"}}");
        return $"{header}.{payload}.";
    }

    private static string Base64UrlEncode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new RecordedRequest(request.Method, request.RequestUri?.PathAndQuery ?? string.Empty, body));
            return responder(request);
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Path, string Body);
}
