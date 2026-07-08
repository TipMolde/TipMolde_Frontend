using System.Net;
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
public class EncomendaDetalheViewModelTests
{
    private Mock<IDialogService> _dialogService = null!;
    private Mock<INavigationService> _navigationService = null!;
    private EncomendaDetalheViewModel _sut = null!;
    private ScenarioState _state = null!;

    [SetUp]
    public void SetUp()
    {
        _state = new ScenarioState();
        _dialogService = new Mock<IDialogService>();
        _navigationService = new Mock<INavigationService>();

        _dialogService.Setup(service => service.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _dialogService.Setup(service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var httpClient = CreateHttpClient(request => HandleRequest(request, _state));
        var encomendasService = new EncomendasService(httpClient);

        _sut = new EncomendaDetalheViewModel(
            encomendasService,
            new MoldesService(httpClient),
            new ClientesService(httpClient),
            new GlobalMoldePriorityService(encomendasService),
            _dialogService.Object,
            _navigationService.Object);
    }

    [Test(Description = "T1ENCDET - O detalhe da encomenda deve preencher o nome do cliente por fallback e ordenar moldes por prioridade.")]
    [Category("Smoke")]
    public async Task LoadAsync_Should_FillCustomerFallbackAndOrderedMoldes()
    {
        await _sut.LoadAsync(40);

        _sut.NomeCliente.Should().Be("Cliente Fallback");
        _sut.Moldes.Should().HaveCount(2);
        _sut.Moldes.Select(item => item.EncomendaMoldeId).Should().ContainInOrder(401, 400);
        _sut.QuantidadeTotalPrevista.Should().Be(8);
        _sut.CanCancelEncomenda.Should().BeTrue();
        _sut.CanGerirMoldes.Should().BeTrue();
    }

    [Test(Description = "T2ENCDET - Cancelar uma encomenda deve atualizar o estado, rebalancear prioridades e refletir o bloqueio de gestao.")]
    public async Task CancelarEncomendaCommand_Should_CancelAndDisableMoldeManagement()
    {
        await _sut.LoadAsync(40);

        await _sut.CancelarEncomendaCommand.ExecuteAsync(null);

        _sut.Estado.Should().Be("CANCELADA");
        _sut.CanCancelEncomenda.Should().BeFalse();
        _sut.CanGerirMoldes.Should().BeFalse();
        _sut.Moldes.All(item => item.CanGerirMolde == false).Should().BeTrue();
        _state.CancelStatePatched.Should().BeTrue();
        _dialogService.Verify(service => service.ShowSuccessAsync(
            "Sucesso",
            "A encomenda ENC-040 foi cancelada com sucesso."),
            Times.Once);
    }

    [Test(Description = "T3ENCDET - Colocar um molde em producao deve atualizar o estado e recarregar o detalhe.")]
    public async Task IniciarProducaoMoldeCommand_Should_UpdateStateAndReload()
    {
        await _sut.LoadAsync(40);
        var molde = _sut.Moldes.Single(item => item.EncomendaMoldeId == 400);

        await _sut.IniciarProducaoMoldeCommand.ExecuteAsync(molde);

        _state.Molde400State.Should().Be("EM_PRODUCAO");
        _sut.Moldes.Single(item => item.EncomendaMoldeId == 400).Estado.Should().Be("EM_PRODUCAO");
        _dialogService.Verify(service => service.ShowSuccessAsync(
            "Inicio de producao",
            "O molde M-400 foi colocado em producao."),
            Times.Once);
    }

    [Test(Description = "T4ENCDET - Guardar o novo prazo do molde deve recalcular prioridades e recarregar o detalhe.")]
    public async Task GuardarPrazoMoldeCommand_Should_UpdateDateRebalanceAndReload()
    {
        await _sut.LoadAsync(40);
        var molde = _sut.Moldes.Single(item => item.EncomendaMoldeId == 400);
        molde.DataEntregaPrevista = new DateTime(2026, 7, 20);

        await _sut.GuardarPrazoMoldeCommand.ExecuteAsync(molde);

        _state.UpdatePrazoCalls.Should().Be(1);
        _state.RebalanceUpdates.Should().BeGreaterThan(0);
        _dialogService.Verify(service => service.ShowSuccessAsync(
            "Sucesso",
            "A entrega prevista do molde M-400 foi atualizada."),
            Times.Once);
    }

    [Test(Description = "T5ENCDET - Concluir um molde deve atualizar o estado e manter o detalhe sincronizado.")]
    public async Task ConcluirMoldeCommand_Should_UpdateStateAndReload()
    {
        await _sut.LoadAsync(40);
        var molde = _sut.Moldes.Single(item => item.EncomendaMoldeId == 401);

        await _sut.ConcluirMoldeCommand.ExecuteAsync(molde);

        _state.Molde401State.Should().Be("CONCLUIDO");
        _sut.Moldes.Single(item => item.EncomendaMoldeId == 401).Estado.Should().Be("CONCLUIDO");
        _dialogService.Verify(service => service.ShowSuccessAsync(
            "Conclusao do molde",
            "O molde M-401 foi marcado como concluido."),
            Times.Once);
    }

    [Test(Description = "T6ENCDET - Abrir um molde deve navegar para o detalhe correto.")]
    public async Task AbrirMoldeCommand_Should_NavigateToMoldeDetail()
    {
        await _sut.LoadAsync(40);
        var molde = _sut.Moldes.Single(item => item.MoldeId == 401);

        await _sut.AbrirMoldeCommand.ExecuteAsync(molde);

        _navigationService.Verify(
            service => service.GoToAsync("MoldeDetalhePage?molde_id=401"),
            Times.Once);
    }

    [Test(Description = "T7ENCDET - Quando a encomenda nao existe, o detalhe deve apresentar erro e limpar os moldes.")]
    public async Task LoadAsync_Should_SetError_When_EncomendaIsNotFound()
    {
        var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var encomendasService = new EncomendasService(httpClient);
        var sut = new EncomendaDetalheViewModel(
            encomendasService,
            new MoldesService(httpClient),
            new ClientesService(httpClient),
            new GlobalMoldePriorityService(encomendasService),
            _dialogService.Object,
            _navigationService.Object);

        await sut.LoadAsync(404);

        sut.ErrorMessage.Should().Be("Nao foi possivel carregar o detalhe da encomenda.");
        sut.Moldes.Should().BeEmpty();
        sut.HasNoMoldes.Should().BeTrue();
    }

    [Test(Description = "T8ENCDET - Uma encomenda concluida deve ficar em modo de consulta, sem operacoes de cancelamento ou gestao.")]
    public async Task LoadAsync_Should_DisableActions_When_EncomendaIsConcluded()
    {
        _state.OrderState = "CONCLUIDA";

        await _sut.LoadAsync(40);

        _sut.CanCancelEncomenda.Should().BeFalse();
        _sut.CanGerirMoldes.Should().BeFalse();
        _sut.Moldes.Should().OnlyContain(item => item.CanGerirMolde == false);
    }

    [Test(Description = "T9ENCDET - Se o utilizador desistir da confirmacao, a encomenda nao deve ser cancelada nem alterada.")]
    public async Task CancelarEncomendaCommand_Should_NotChangeState_When_UserDoesNotConfirm()
    {
        _dialogService.Setup(service => service.ConfirmAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(false);

        await _sut.LoadAsync(40);

        await _sut.CancelarEncomendaCommand.ExecuteAsync(null);

        _sut.Estado.Should().Be("EM_PRODUCAO");
        _state.CancelStatePatched.Should().BeFalse();
        _dialogService.Verify(service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test(Description = "T10ENCDET - Uma falha ao atualizar o prazo do molde deve mostrar erro e evitar o sucesso visual.")]
    public async Task GuardarPrazoMoldeCommand_Should_SetError_When_BackendFails()
    {
        var state = new ScenarioState();
        var httpClient = CreateHttpClient(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/encomenda-moldes/400" &&
                request.Method == HttpMethod.Put)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        """{"detail":"Nao foi possivel atualizar o prazo do molde."}""",
                        Encoding.UTF8,
                        "application/json")
                };
            }

            return HandleRequest(request, state);
        });

        var encomendasService = new EncomendasService(httpClient);
        var sut = new EncomendaDetalheViewModel(
            encomendasService,
            new MoldesService(httpClient),
            new ClientesService(httpClient),
            new GlobalMoldePriorityService(encomendasService),
            _dialogService.Object,
            _navigationService.Object);

        await sut.LoadAsync(40);
        var molde = sut.Moldes.Single(item => item.EncomendaMoldeId == 400);
        molde.DataEntregaPrevista = new DateTime(2026, 7, 22);

        await sut.GuardarPrazoMoldeCommand.ExecuteAsync(molde);

        sut.ErrorMessage.Should().Be("Nao foi possivel atualizar o prazo do molde.");
        _dialogService.Verify(service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static HttpResponseMessage HandleRequest(HttpRequestMessage request, ScenarioState state)
    {
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;

        if (path == "/api/encomendas/40")
        {
            return CreateJsonResponse(HttpStatusCode.OK, new EncomendaResumoDto
            {
                Encomenda_id = 40,
                NumeroEncomendaCliente = "ENC-040",
                Cliente_id = 5,
                NomeCliente = string.Empty,
                NomeServicoCliente = "Servico Critico",
                NomeResponsavelCliente = "Ana",
                NumeroProjetoCliente = "PRJ-40",
                Estado = state.OrderState,
                DataRegisto = new DateTime(2026, 7, 1)
            });
        }

        if (path == "/api/clientes/5")
        {
            return CreateJsonResponse(HttpStatusCode.OK, new ClienteDto
            {
                Cliente_id = 5,
                Nome = "Cliente Fallback",
                Sigla = "CF"
            });
        }

        if (path == "/api/encomenda-moldes/por-encomenda/40?page=1&pageSize=100")
        {
            return CreateJsonResponse(HttpStatusCode.OK, new PagedResult<EncomendaMoldeDto>
            {
                Items =
                [
                    new EncomendaMoldeDto
                    {
                        EncomendaMolde_id = 400,
                        Encomenda_id = 40,
                        Molde_id = 400,
                        Quantidade = 5,
                        Prioridade = 2,
                        DataEntregaPrevista = new DateTime(2026, 7, 12),
                        Estado = state.Molde400State,
                        NumeroMolde = "M-400"
                    },
                    new EncomendaMoldeDto
                    {
                        EncomendaMolde_id = 401,
                        Encomenda_id = 40,
                        Molde_id = 401,
                        Quantidade = 3,
                        Prioridade = 1,
                        DataEntregaPrevista = new DateTime(2026, 7, 10),
                        Estado = state.Molde401State,
                        NumeroMolde = "M-401"
                    }
                ],
                Page = 1,
                PageSize = 100,
                TotalItems = 2
            });
        }

        if (path == "/api/moldes/por-encomenda/40?page=1&pageSize=100")
        {
            return CreateJsonResponse(HttpStatusCode.OK, new PagedResult<MoldeDto>
            {
                Items =
                [
                    new MoldeDto { MoldeId = 400, Numero = "M-400", Nome = "Molde 400", Descricao = "Descricao 400", Numero_cavidades = 2 },
                    new MoldeDto { MoldeId = 401, Numero = "M-401", Nome = "Molde 401", Descricao = "Descricao 401", Numero_cavidades = 4 }
                ],
                Page = 1,
                PageSize = 100,
                TotalItems = 2
            });
        }

        if (path == "/api/encomendas/40/estado" && request.Method == HttpMethod.Patch)
        {
            state.CancelStatePatched = true;
            state.OrderState = "CANCELADA";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }

        if (path == "/api/encomendas/em-producao?page=1&pageSize=100")
        {
            var openOrders = state.OrderState == "CANCELADA"
                ? new List<EncomendaResumoDto>
                {
                    new() { Encomenda_id = 41, NumeroEncomendaCliente = "ENC-041", Estado = "EM_PRODUCAO" }
                }
                : new List<EncomendaResumoDto>
                {
                    new() { Encomenda_id = 40, NumeroEncomendaCliente = "ENC-040", Estado = "EM_PRODUCAO" },
                    new() { Encomenda_id = 41, NumeroEncomendaCliente = "ENC-041", Estado = "EM_PRODUCAO" }
                };

            return CreateJsonResponse(HttpStatusCode.OK, new PagedResult<EncomendaResumoDto>
            {
                Items = openOrders,
                Page = 1,
                PageSize = 100,
                TotalItems = openOrders.Count
            });
        }

        if (path == "/api/encomenda-moldes/por-encomenda/41?page=1&pageSize=100")
        {
            return CreateJsonResponse(HttpStatusCode.OK, new PagedResult<EncomendaMoldeDto>
            {
                Items =
                [
                    new EncomendaMoldeDto
                    {
                        EncomendaMolde_id = 410,
                        Encomenda_id = 41,
                        Molde_id = 410,
                        Quantidade = 2,
                        Prioridade = 1,
                        DataEntregaPrevista = new DateTime(2026, 7, 8),
                        Estado = "PENDENTE",
                        NumeroMolde = "M-410"
                    }
                ],
                Page = 1,
                PageSize = 100,
                TotalItems = 1
            });
        }

        if (path == "/api/encomenda-moldes/400/estado" && request.Method == HttpMethod.Patch)
        {
            state.Molde400State = "EM_PRODUCAO";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }

        if (path == "/api/encomenda-moldes/401/estado" && request.Method == HttpMethod.Patch)
        {
            state.Molde401State = "CONCLUIDO";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }

        if (path == "/api/encomenda-moldes/400" && request.Method == HttpMethod.Put)
        {
            var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
            if (body.Contains("\"dataEntregaPrevista\":", StringComparison.Ordinal) &&
                !body.Contains("\"dataEntregaPrevista\":null", StringComparison.Ordinal))
            {
                state.UpdatePrazoCalls++;
            }

            if (body.Contains("\"prioridade\":", StringComparison.Ordinal) &&
                !body.Contains("\"prioridade\":null", StringComparison.Ordinal))
            {
                state.RebalanceUpdates++;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }

        if (path == "/api/encomenda-moldes/401" && request.Method == HttpMethod.Put)
        {
            state.RebalanceUpdates++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        return new HttpClient(new StubHttpMessageHandler(responder))
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

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }

    private sealed class ScenarioState
    {
        public string OrderState { get; set; } = "EM_PRODUCAO";
        public string Molde400State { get; set; } = "PENDENTE";
        public string Molde401State { get; set; } = "EM_PRODUCAO";
        public bool CancelStatePatched { get; set; }
        public int RebalanceUpdates { get; set; }
        public int UpdatePrazoCalls { get; set; }
    }
}
