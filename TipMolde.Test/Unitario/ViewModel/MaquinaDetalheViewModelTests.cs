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
    private List<IndustrialEventoDto> _eventosPendentes = null!;
    private IndustrialSessaoAtivaDto? _sessaoAtiva;
    private bool _contextoCompleto;
    private bool _producaoConcluida;

    [SetUp]
    public void SetUp()
    {
        _dialogService = new Mock<IDialogService>();
        _dialogService
            .Setup(service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _eventosPendentes =
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
        ];

        _sessaoAtiva = new IndustrialSessaoAtivaDto
        {
            SessaoMaquinaIndustrial_id = 501,
            Maquina_id = 9,
            Operador_id = 7,
            OperadorNome = "Gestor Producao",
            Peca_id = 101,
            NumeroPeca = "P-101",
            DesignacaoPeca = "Base 101",
            Molde_id = 9,
            NumeroMolde = "M-901",
            Fase_id = 3,
            FaseNome = "MONTAGEM",
            ProximaFasePlaneada_id = 4,
            ProximaFasePlaneadaNome = "EROSAO",
            EstadoSessao = "ATIVA",
            UltimoEstadoMaquina = "RUNNING",
            StartedAt = new DateTime(2026, 7, 7, 5, 50, 0, DateTimeKind.Utc),
            LastSeenAt = new DateTime(2026, 7, 7, 6, 0, 0, DateTimeKind.Utc)
        };

        _contextoCompleto = false;
        _producaoConcluida = false;

        var httpClient = CreateHttpClient(HandleRequest, out _requests);
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(userId: 7));

        _sut = new MaquinaDetalheViewModel(
            new FasesProducaoService(httpClient),
            new MaquinasService(httpClient),
            new IndustrialProducaoService(httpClient),
            new PecasService(httpClient),
            new SessaoPersistidaService(httpClient),
            new UtilizadoresService(httpClient),
            _dialogService.Object);
    }

    [Test(Description = "T1MDET - O detalhe deve priorizar STOPPED pendente, mostrar a sessao ativa e filtrar pecas pela fase dedicada.")]
    [Category("Smoke")]
    public async Task LoadAsync_Should_PrioritizeStoppedPendingAndLoadActiveSession()
    {
        await _sut.LoadAsync(9);

        _sut.Maquina.Should().NotBeNull();
        _sut.Maquina!.Maquina_id.Should().Be(9);
        _sut.UtilizadorAtualId.Should().Be(7);
        _sut.UtilizadorAtualNome.Should().Be("Gestor Producao");
        _sut.EventoPendente.Should().NotBeNull();
        _sut.EventoPendente!.EventoMaquinaIndustrial_id.Should().Be(94);
        _sut.HasEventoStoppedPendente.Should().BeTrue();
        _sut.HasEventoRunningPendente.Should().BeFalse();
        _sut.NeedsContextSelection.Should().BeFalse();
        _sut.SelectedProximaFase.Should().NotBeNull();
        _sut.SelectedProximaFase!.FasesProducao_id.Should().Be(4);
        _sut.SessaoAtiva.Should().NotBeNull();
        _sut.SessaoAtiva!.Peca_id.Should().Be(101);
        _sut.PecasEncontradas.Should().BeEmpty();
        _requests.Should().NotContain(request =>
            request.Method == HttpMethod.Get &&
            request.Path == "/api/pecas/fila-trabalho?page=1&pageSize=5&searchMode=Peca&faseId=3");
    }

    [Test(Description = "T2MDET - Ao completar contexto RUNNING, o frontend deve enviar gestor, peca e fase e recarregar a maquina como EM_USO.")]
    public async Task CompletarContextoCommand_Should_PostSelectedPieceAndReloadMachineState()
    {
        _eventosPendentes =
        [
            new IndustrialEventoDto
            {
                EventoMaquinaIndustrial_id = 91,
                Maquina_id = 9,
                EstadoMaquina = "RUNNING",
                OccurredAt = new DateTime(2026, 7, 7, 8, 0, 0, DateTimeKind.Utc)
            }
        ];
        _sessaoAtiva = null;

        await _sut.LoadAsync(9);
        _sut.NeedsContextSelection.Should().BeTrue();
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
        _sut.Maquina!.Estado.Should().Be("EM_USO");
        _sut.SessaoAtiva.Should().NotBeNull();
        _dialogService.Verify(service => service.ShowSuccessAsync(
            "Contexto registado",
            It.Is<string>(message => message.Contains("901") && message.Contains("P-101"))),
            Times.Once);
    }

    [Test(Description = "T3MDET - Ao confirmar STOPPED como concluido, o frontend deve enviar a decisao e limpar a sessao ativa apos recarga.")]
    public async Task ConfirmarConclusaoCommand_Should_PostDecisionAndClearActiveSession()
    {
        await _sut.LoadAsync(9);

        await _sut.ConfirmarConclusaoCommand.ExecuteAsync(null);

        _requests.Should().ContainSingle(request =>
            request.Method == HttpMethod.Post &&
            request.Path == "/api/industrial/eventos/94/confirmar-paragem");

        var post = _requests.Single(request =>
            request.Method == HttpMethod.Post &&
            request.Path == "/api/industrial/eventos/94/confirmar-paragem");

        post.Body.Should().Contain("\"trabalhoConcluido\":true");
        post.Body.Should().Contain("\"proximaFase_id\":4");
        _sut.EventoPendente.Should().BeNull();
        _sut.SessaoAtiva.Should().BeNull();
        _sut.Maquina!.Estado.Should().Be("DISPONIVEL");
        _dialogService.Verify(service => service.ShowSuccessAsync(
            "Producao concluida",
            "O trabalho ativo da maquina foi concluido."),
            Times.Once);
    }

    private HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/Maquina/9" => CreateJsonResponse(HttpStatusCode.OK, BuildMaquinaResponse()),
            "/api/fases-producao?page=1&pageSize=100" => CreateJsonResponse(HttpStatusCode.OK, BuildFasesResponse()),
            "/api/users/7" => CreateJsonResponse(
                HttpStatusCode.OK,
                new UtilizadorDto
                {
                    User_id = 7,
                    Nome = "Gestor Producao",
                    Email = "gestor@tipmolde.pt",
                    Role = "GESTOR_PRODUCAO"
                }),
            "/api/industrial/maquinas/9/evento-pendente" => BuildEventoPendenteResponse(),
            "/api/industrial/maquinas/9/sessao-ativa" => BuildSessaoAtivaResponse(),
            "/api/pecas/fila-trabalho?page=1&pageSize=5&searchMode=Peca&faseId=3" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreatePecasPage(
                    page: 1,
                    pageSize: 5,
                    totalItems: 7,
                    [101, 102, 103, 104, 105])),
            "/api/pecas/fila-trabalho?page=2&pageSize=5&searchMode=Peca&faseId=3" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreatePecasPage(
                    page: 2,
                    pageSize: 5,
                    totalItems: 7,
                    [106, 107])),
            "/api/pecas/fila-trabalho?page=1&pageSize=5&searchMode=Peca&searchTerm=Base&faseId=3" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreatePecasPage(
                    page: 1,
                    pageSize: 5,
                    totalItems: 2,
                    [101, 106])),
            "/api/industrial/eventos/91/completar-contexto" => BuildCompletarContextoResponse(),
            "/api/industrial/eventos/94/confirmar-paragem" => BuildConfirmarParagemResponse(),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private IReadOnlyList<IndustrialEventoDto> ResolveEventosPendentes()
    {
        if (_contextoCompleto || _producaoConcluida)
            return [];

        return _eventosPendentes;
    }

    private HttpResponseMessage BuildEventoPendenteResponse()
    {
        var evento = ResolveEventoPendente();
        return evento is null
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : CreateJsonResponse(HttpStatusCode.OK, evento);
    }

    private IndustrialEventoDto? ResolveEventoPendente()
    {
        if (_contextoCompleto || _producaoConcluida)
            return null;

        if (_sessaoAtiva is not null &&
            string.Equals(_sessaoAtiva.EstadoSessao, "AGUARDAR_CONFIRMACAO_PARAGEM", StringComparison.OrdinalIgnoreCase))
        {
            return _eventosPendentes
                .Where(evento => evento.Maquina_id == 9)
                .Where(evento => string.Equals(evento.EstadoMaquina, "STOPPED", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(evento => evento.OccurredAt)
                .FirstOrDefault();
        }

        if (_sessaoAtiva is not null)
            return null;

        return _eventosPendentes
            .Where(evento => evento.Maquina_id == 9)
            .Where(evento => string.Equals(evento.EstadoMaquina, "RUNNING", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(evento => evento.OccurredAt)
            .FirstOrDefault();
    }

    private MaquinaItem BuildMaquinaResponse()
    {
        return new MaquinaItem
        {
            Maquina_id = 9,
            Numero = 901,
            NomeModelo = "Maq CNC 01",
            IpAddress = "192.168.0.9",
            Estado = _contextoCompleto ? "EM_USO" : "DISPONIVEL",
            FaseDedicada_id = 3,
            FaseDedicadaNome = "MONTAGEM",
            ProtocoloComunicacao = "OPC-UA"
        };
    }

    private HttpResponseMessage BuildSessaoAtivaResponse()
    {
        if (_producaoConcluida)
            return new HttpResponseMessage(HttpStatusCode.NotFound);

        if (_contextoCompleto)
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                _sessaoAtiva ?? new IndustrialSessaoAtivaDto
                {
                    SessaoMaquinaIndustrial_id = 501,
                    Maquina_id = 9,
                    Operador_id = 7,
                    OperadorNome = "Gestor Producao",
                    Peca_id = 101,
                    NumeroPeca = "P-101",
                    DesignacaoPeca = "Base 101",
                    Molde_id = 9,
                    NumeroMolde = "M-901",
                    Fase_id = 3,
                    FaseNome = "MONTAGEM",
                    ProximaFasePlaneada_id = 4,
                    ProximaFasePlaneadaNome = "EROSAO",
                    EstadoSessao = "ATIVA",
                    UltimoEstadoMaquina = "RUNNING",
                    StartedAt = new DateTime(2026, 7, 7, 8, 0, 0, DateTimeKind.Utc),
                    LastSeenAt = new DateTime(2026, 7, 7, 8, 1, 0, DateTimeKind.Utc)
                });
        }

        return _sessaoAtiva is null
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : CreateJsonResponse(HttpStatusCode.OK, _sessaoAtiva);
    }

    private HttpResponseMessage BuildCompletarContextoResponse()
    {
        _contextoCompleto = true;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
    }

    private HttpResponseMessage BuildConfirmarParagemResponse()
    {
        _producaoConcluida = true;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
    }

    private static PagedResult<FaseProducaoItem> BuildFasesResponse()
    {
        return new PagedResult<FaseProducaoItem>
        {
            Items =
            [
                new FaseProducaoItem { FasesProducao_id = 3, Nome = "MONTAGEM", Descricao = "Montagem" },
                new FaseProducaoItem { FasesProducao_id = 4, Nome = "EROSAO", Descricao = "Erosao" },
                new FaseProducaoItem { FasesProducao_id = 5, Nome = "POLIMENTO", Descricao = "Polimento" }
            ],
            Page = 1,
            PageSize = 100,
            TotalItems = 3
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
                    ProximaFaseId = 3,
                    ProximaFaseNome = "MONTAGEM",
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
