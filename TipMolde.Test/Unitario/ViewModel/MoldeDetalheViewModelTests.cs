using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using TipMolde.Domain.Enums;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

[TestFixture]
[Category("Unit")]
public class MoldeDetalheViewModelTests
{
    private Mock<IDialogService> _dialogService = null!;
    private Mock<INavigationService> _navigationService = null!;
    private MoldeDetalheViewModel _sut = null!;
    private ScenarioState _state = null!;
    private List<RecordedRequest> _requests = null!;

    [SetUp]
    public void SetUp()
    {
        _dialogService = new Mock<IDialogService>();
        _dialogService.Setup(service => service.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _dialogService.Setup(service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _dialogService.Setup(service => service.ShowErrorAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _dialogService.Setup(service => service.ConfirmDeleteAsync(It.IsAny<string>())).ReturnsAsync(true);

        _navigationService = new Mock<INavigationService>();
        _state = new ScenarioState();

        _sut = CreateSut(_state, out _requests);
    }

    [Test(Description = "TMOLDET1 - O detalhe do molde deve carregar a primeira pagina de pecas, contexto de projeto e tempos para um gestor de desenho.")]
    [Category("Smoke")]
    public async Task LoadAsync_Should_LoadFirstPageAndProjectContext_When_UserCanManagePieces()
    {
        await _sut.LoadAsync(55);

        _sut.CanManagePieces.Should().BeTrue();
        _sut.IsAdmin.Should().BeFalse();
        _sut.Pecas.Should().HaveCount(8);
        _sut.TotalPages.Should().Be(2);
        _sut.Pecas.Select(item => item.PecaId).Should().ContainInOrder(507, 502, 504, 501, 506, 503, 508, 505);
        _sut.Projetos.Should().HaveCount(2);
        _sut.SelectedProjeto.Should().NotBeNull();
        _sut.SelectedProjeto!.Projeto_id.Should().Be(902);
        _sut.Revisoes.Should().ContainSingle();
        _sut.Revisoes.Single().NumRevisao.Should().Be(3);
        _sut.CanAccessTempoProjeto.Should().BeTrue();
        _sut.CanAddPeca.Should().BeTrue();
        _sut.TempoRegistadoTotal.Should().Be(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(30)));
        _sut.TempoTotalPecas.Should().Be(TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(15)));
        _requests.Should().Contain(request => request.Path == "/api/registos-tempo-projeto?projetoId=902&autorId=7&page=1&pageSize=100");
    }

    [Test(Description = "TMOLDET2 - Ao avancar a paginacao, o detalhe do molde deve carregar a peca remanescente da segunda pagina.")]
    public async Task NextPageCommand_Should_LoadRemainingPecas_When_PageAdvances()
    {
        await _sut.LoadAsync(55);

        await _sut.NextPageCommand.ExecuteAsync(null);

        _sut.Page.Should().Be(2);
        _sut.Pecas.Should().ContainSingle();
        _sut.Pecas.Single().PecaId.Should().Be(509);
        _sut.CanGoNext.Should().BeFalse();
        _sut.CanGoPrevious.Should().BeTrue();
    }

    [Test(Description = "TMOLDET3 - Um utilizador sem permissao de desenho deve ver o detalhe em modo de consulta e sem historico temporal.")]
    public async Task LoadAsync_Should_KeepReadOnlyMode_When_UserCannotManagePieces()
    {
        _state.Role = "GESTOR_COMERCIAL";

        var sut = CreateSut(_state, out var requests);

        await sut.LoadAsync(55);

        sut.CanManagePieces.Should().BeFalse();
        sut.CanAccessTempoProjeto.Should().BeFalse();
        sut.CanAddPeca.Should().BeFalse();
        sut.PecasSectionDescription.Should().Be("Consulta as pecas registadas para este molde.");
        sut.EmptyTempoMessage.Should().Be("Apenas o gestor de desenho ou o administrador pode consultar o historico de tempo.");
        requests.Should().NotContain(request => request.Path.Contains("/api/registos-tempo-projeto?", StringComparison.Ordinal));
    }

    [Test(Description = "TMOLDET4 - Quando a ultima revisao ja foi aprovada, o frontend deve bloquear a criacao de novas pecas nesse projeto.")]
    public async Task AdicionarPecaCommand_Should_ShowInfoAndNotNavigate_When_LatestRevisionIsApproved()
    {
        _state.ApprovedLatestRevision = true;

        var sut = CreateSut(_state, out _);
        await sut.LoadAsync(55);

        await sut.AdicionarPecaCommand.ExecuteAsync(null);

        sut.CanManagePieces.Should().BeTrue();
        sut.CanAddPeca.Should().BeFalse();
        _dialogService.Verify(
            service => service.ShowInfoAsync(
                "Pecas bloqueadas",
                "So e possivel adicionar pecas quando o projeto do molde tiver a ultima revisao aprovada pelo cliente."),
            Times.Once);
        _navigationService.Verify(service => service.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    [Test(Description = "TMOLDET5 - Quando o molde nao existe, o detalhe deve apresentar erro e limpar o contexto associado.")]
    public async Task LoadAsync_Should_SetError_When_MoldeCannotBeLoaded()
    {
        _state.MoldeExists = false;

        var sut = CreateSut(_state, out _);

        await sut.LoadAsync(55);

        sut.ErrorMessage.Should().Be("Nao foi possivel carregar o detalhe do molde.");
        sut.Dashboard.Should().BeNull();
        sut.Pecas.Should().BeEmpty();
        sut.Projetos.Should().BeEmpty();
        sut.Revisoes.Should().BeEmpty();
        sut.RegistosTempo.Should().BeEmpty();
        sut.SelectedProjeto.Should().BeNull();
    }

    [Test]
    public void CalcularTempoTotalPecas_Should_Somar_Todos_Os_Intervalos_Das_Pecas_Do_Molde()
    {
        var pecaIds = new[] { 10, 11 };
        var registos = new List<RegistoProducaoDto>
        {
            new() { PecaId = 10, EstadoProducao = "PREPARACAO", DataHora = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc) },
            new() { PecaId = 10, EstadoProducao = "PAUSADO", DataHora = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc) },
            new() { PecaId = 10, EstadoProducao = "EM_CURSO", DataHora = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc) },
            new() { PecaId = 10, EstadoProducao = "CONCLUIDO", DataHora = new DateTime(2026, 6, 1, 10, 45, 0, DateTimeKind.Utc) },
            new() { PecaId = 11, EstadoProducao = "PREPARACAO", DataHora = new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc) },
            new() { PecaId = 11, EstadoProducao = "CONCLUIDO", DataHora = new DateTime(2026, 6, 1, 12, 30, 0, DateTimeKind.Utc) },
            new() { PecaId = 99, EstadoProducao = "PREPARACAO", DataHora = new DateTime(2026, 6, 1, 13, 0, 0, DateTimeKind.Utc) },
            new() { PecaId = 99, EstadoProducao = "CONCLUIDO", DataHora = new DateTime(2026, 6, 1, 15, 0, 0, DateTimeKind.Utc) }
        };

        var resultado = InvokeCalculoTempoTotal(pecaIds, registos);

        resultado.Should().Be(TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(15)));
    }

    private MoldeDetalheViewModel CreateSut(ScenarioState state, out List<RecordedRequest> requests)
    {
        var httpClient = CreateHttpClient(request => HandleRequest(request, state), out requests);
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(7));

        var sessaoPersistidaService = new SessaoPersistidaService(httpClient);
        var authorizationService = new AuthorizationService(sessaoPersistidaService, new UtilizadoresService(httpClient));

        return new MoldeDetalheViewModel(
            new MoldesService(httpClient),
            new ProjetosService(httpClient),
            new RevisoesService(httpClient),
            new RegistosTempoProjetoService(httpClient),
            new RegistosProducaoService(httpClient),
            new PecasService(httpClient),
            authorizationService,
            sessaoPersistidaService,
            Mock.Of<IDestinationFolderPickerService>(),
            _dialogService.Object,
            Mock.Of<IFilePickerService>(),
            _navigationService.Object);
    }

    private static HttpResponseMessage HandleRequest(HttpRequestMessage request, ScenarioState state)
    {
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;

        if (path == "/api/users/me")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new UtilizadorDto
                {
                    User_id = 7,
                    Nome = "Gestor Desenho",
                    Email = "desenho@tipmolde.pt",
                    Role = state.Role
                });
        }

        if (path == "/api/moldes/55")
        {
            if (!state.MoldeExists)
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            return CreateJsonResponse(
                HttpStatusCode.OK,
                new MoldeDto
                {
                    MoldeId = 55,
                    Numero = "M-055",
                    NumeroMoldeCliente = "CL-055",
                    Nome = "Molde Critico",
                    Descricao = "Molde de validacao",
                    Numero_cavidades = 2,
                    TipoPedido = "NORMAL",
                    Cor = CorMolde.MONOCOLOR
                });
        }

        if (path == "/api/moldes/55/dashboard-ciclo-vida")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new MoldeCicloVidaDashboardDto
                {
                    MoldeId = 55,
                    NumeroMolde = "M-055",
                    TotalPecas = 9,
                    Maquinacao = 3,
                    Erosao = 2,
                    Montagem = 1,
                    EmEspera = 1,
                    EmTrabalho = 1,
                    Concluidas = 2,
                    MaterialPendente = 0,
                    PercentagemConclusao = 55.5m
                });
        }

        if (path == "/api/projetos/por-molde/55?page=1&pageSize=100")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<ProjetoDto>
                {
                    Items =
                    [
                        new ProjetoDto
                        {
                            Projeto_id = 901,
                            Molde_id = 55,
                            NomeProjeto = "Projeto Base",
                            TipoProjeto = "ALTERACAO",
                            SoftwareUtilizado = "SolidWorks",
                            CaminhoPastaServidor = @"\\srv\proj\901",
                            NumeroMolde = "M-055"
                        },
                        new ProjetoDto
                        {
                            Projeto_id = 902,
                            Molde_id = 55,
                            NomeProjeto = "Projeto Atual",
                            TipoProjeto = "NOVO",
                            SoftwareUtilizado = "NX",
                            CaminhoPastaServidor = @"\\srv\proj\902",
                            NumeroMolde = "M-055"
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 2
                });
        }

        if (path == "/api/projetos/902/com-revisoes")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new ProjetoComRevisoesDto
                {
                    Projeto_id = 902,
                    Molde_id = 55,
                    NomeProjeto = "Projeto Atual",
                    TipoProjeto = "NOVO",
                    SoftwareUtilizado = "NX",
                    CaminhoPastaServidor = @"\\srv\proj\902",
                    NumeroMolde = "M-055",
                    Revisoes = new List<RevisaoDto>
                    {
                        new()
                        {
                            Revisao_id = 9201,
                            Projeto_id = 902,
                            NumRevisao = 3,
                            DescricaoAlteracoes = "Ultima revisao",
                            DataEnvioCliente = new DateTime(2026, 7, 1, 10, 0, 0),
                            Aprovado = state.ApprovedLatestRevision ? true : false,
                            DataResposta = new DateTime(2026, 7, 2, 11, 0, 0)
                        }
                    }
                });
        }

        if (path == "/api/pecas/por-molde/55?page=1&pageSize=8")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items = BuildPecas().Take(8).ToList(),
                    Page = 1,
                    PageSize = 8,
                    TotalItems = 9
                });
        }

        if (path == "/api/pecas/por-molde/55?page=2&pageSize=8")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items = BuildPecas().Skip(8).Take(8).ToList(),
                    Page = 2,
                    PageSize = 8,
                    TotalItems = 9
                });
        }

        if (path == "/api/pecas/por-molde/55?page=1&pageSize=100")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items = BuildPecas(),
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 9
                });
        }

        if (path == "/api/RegistosProducao?page=1&pageSize=100")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<RegistoProducaoDto>
                {
                    Items = new List<RegistoProducaoDto>
                    {
                        new() { PecaId = 501, EstadoProducao = "PREPARACAO", DataHora = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc) },
                        new() { PecaId = 501, EstadoProducao = "CONCLUIDO", DataHora = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc) },
                        new() { PecaId = 502, EstadoProducao = "PREPARACAO", DataHora = new DateTime(2026, 7, 1, 9, 15, 0, DateTimeKind.Utc) },
                        new() { PecaId = 502, EstadoProducao = "PAUSADO", DataHora = new DateTime(2026, 7, 1, 10, 30, 0, DateTimeKind.Utc) }
                    },
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 4
                });
        }

        if (path == "/api/registos-tempo-projeto?projetoId=902&autorId=7&page=1&pageSize=100")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<RegistoTempoProjetoDto>
                {
                    Items = new List<RegistoTempoProjetoDto>
                    {
                        new()
                        {
                            Registo_Tempo_Projeto_id = 1,
                            Projeto_id = 902,
                            Autor_id = 7,
                            Estado_tempo = "INICIADO",
                            Data_hora = new DateTime(2026, 7, 3, 8, 0, 0, DateTimeKind.Utc)
                        },
                        new()
                        {
                            Registo_Tempo_Projeto_id = 2,
                            Projeto_id = 902,
                            Autor_id = 7,
                            Estado_tempo = "PAUSADO",
                            Data_hora = new DateTime(2026, 7, 3, 9, 30, 0, DateTimeKind.Utc)
                        }
                    },
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 2
                });
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static List<PecaDto> BuildPecas()
    {
        return
        [
            new() { PecaId = 501, Molde_id = 55, NumeroPeca = "P-05", Designacao = "Peca 05", Prioridade = 2, Quantidade = 1 },
            new() { PecaId = 502, Molde_id = 55, NumeroPeca = "P-02", Designacao = "Peca 02", Prioridade = 1, Quantidade = 1 },
            new() { PecaId = 503, Molde_id = 55, NumeroPeca = "P-07", Designacao = "Peca 07", Prioridade = 3, Quantidade = 1 },
            new() { PecaId = 504, Molde_id = 55, NumeroPeca = "P-03", Designacao = "Peca 03", Prioridade = 1, Quantidade = 1 },
            new() { PecaId = 505, Molde_id = 55, NumeroPeca = "P-09", Designacao = "Peca 09", Prioridade = 4, Quantidade = 1 },
            new() { PecaId = 506, Molde_id = 55, NumeroPeca = "P-06", Designacao = "Peca 06", Prioridade = 2, Quantidade = 1 },
            new() { PecaId = 507, Molde_id = 55, NumeroPeca = "P-01", Designacao = "Peca 01", Prioridade = 1, Quantidade = 1 },
            new() { PecaId = 508, Molde_id = 55, NumeroPeca = "P-08", Designacao = "Peca 08", Prioridade = 3, Quantidade = 1 },
            new() { PecaId = 509, Molde_id = 55, NumeroPeca = "P-10", Designacao = "Peca 10", Prioridade = 5, Quantidade = 1 }
        ];
    }

    private static TimeSpan InvokeCalculoTempoTotal(IEnumerable<int> pecaIds, IEnumerable<RegistoProducaoDto> registos)
    {
        var metodo = typeof(MoldeDetalheViewModel).GetMethod(
            "CalcularTempoTotalPecas",
            BindingFlags.Static | BindingFlags.NonPublic);

        metodo.Should().NotBeNull();

        return (TimeSpan)metodo!.Invoke(null, [pecaIds, registos])!;
    }

    private static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        out List<RecordedRequest> requests)
    {
        var handler = new RecordingHttpMessageHandler(responder);
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
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
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

    private sealed class RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
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

    private sealed class ScenarioState
    {
        public string Role { get; set; } = "GESTOR_DESENHO";
        public bool MoldeExists { get; set; } = true;
        public bool ApprovedLatestRevision { get; set; }
    }
}
