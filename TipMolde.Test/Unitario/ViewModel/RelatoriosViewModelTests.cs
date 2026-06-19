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
/// Testes unitarios do ViewModel de relatorios do frontend.
/// </summary>
/// <remarks>
/// Valida o carregamento do catalogo e a filtragem dos moldes com associacao Encomenda-Molde.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class RelatoriosViewModelTests
{
    private Mock<IDialogService> _dialogService = null!;
    private RelatoriosViewModel _sut = null!;
    private List<HttpRequestMessage> _requests = null!;

    [SetUp]
    public void SetUp()
    {
        _dialogService = new Mock<IDialogService>();

        var httpClient = CreateHttpClient(
            request => HandleRequest(request),
            out _requests);

        _sut = new RelatoriosViewModel(
            new MoldesService(httpClient),
            new EncomendasService(httpClient),
            new RelatoriosService(httpClient),
            _dialogService.Object);
    }

    [Test(Description = "T1FRT - A lista de moldes deve mostrar apenas moldes com associacao Encomenda-Molde.")]
    public async Task LoadAsync_Should_ShowOnlyMoldesWithEncomendaMolde_When_CatalogContainsMixedMoldes()
    {
        // ACT
        await _sut.LoadAsync();

        // ASSERT
        _sut.Moldes.Should().HaveCount(1);
        _sut.Moldes[0].MoldeId.Should().Be(1);
        _sut.Moldes[0].Numero.Should().Be("M-001");
        _sut.Moldes[0].Nome.Should().Be("Molde com contexto");
    }

    [Test(Description = "T2FRT - Quando nao existe ficha FRM, o frontend deve cria-la no backend antes de exportar.")]
    public async Task GenerateAsync_Should_CreateMissingFrmFicha_BeforeExporting()
    {
        // ARRANGE
        await _sut.LoadAsync();
        _sut.SelectedDestinationFolder = Path.Combine(Path.GetTempPath(), "tipmolde-relatorios-tests", Guid.NewGuid().ToString("N"));
        _sut.SelectedTipoRelatorio = "FRM";

        await _sut.SelecionarMoldeCommand.ExecuteAsync(_sut.Moldes[0]);

        // ACT
        await _sut.GenerateCommand.ExecuteAsync(null);

        // ASSERT
        _requests.Any(request =>
                request.Method == HttpMethod.Post &&
                request.RequestUri?.PathAndQuery == "/api/fichas-producao/ensure")
            .Should()
            .BeTrue();

        _requests.Any(request =>
                request.Method == HttpMethod.Get &&
                request.RequestUri?.PathAndQuery == "/api/fichas-producao/42/export/frm")
            .Should()
            .BeTrue();

        _sut.IsReportAvailable.Should().BeTrue();
        Directory.Exists(_sut.SelectedDestinationFolder).Should().BeTrue();
        Directory.GetFiles(_sut.SelectedDestinationFolder).Should().NotBeEmpty();
        _dialogService.Verify(
            dialog => dialog.ShowSuccessAsync(
                "Relatorio gerado",
                It.Is<string>(message => message.Contains("FRM"))),
            Times.Once);
    }

    private HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;

        if (request.Method == HttpMethod.Post && path == "/api/fichas-producao/ensure")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new
                {
                    fichaProducao_id = 42,
                    tipo = "FRM",
                    dataCriacao = new DateTime(2026, 6, 19, 10, 30, 0, DateTimeKind.Utc),
                    encomendaMolde_id = 10
                });
        }

        if (request.Method == HttpMethod.Get && path == "/api/fichas-producao/by-molde?moldeId=1&page=1&pageSize=200")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<FichaProducaoResumoDto>
                {
                    Items = [],
                    Page = 1,
                    PageSize = 200,
                    TotalItems = 0
                });
        }

        if (request.Method == HttpMethod.Get && path == "/api/fichas-producao/42/export/frm")
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 })
            };
        }

        return path switch
        {
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
                            NumeroMoldeCliente = "CL-001",
                            Nome = "Molde com contexto",
                            ImagemCapaPath = "Storage/Uploads/molde-1.png",
                            TipoPedido = "Normal",
                            Numero_cavidades = 2
                        },
                        new MoldeDto
                        {
                            MoldeId = 2,
                            Numero = "M-002",
                            NumeroMoldeCliente = "CL-002",
                            Nome = "Molde sem contexto",
                            ImagemCapaPath = "Storage/Uploads/molde-2.png",
                            TipoPedido = "Normal",
                            Numero_cavidades = 4
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 2
                }),
            "/api/encomenda-moldes/por-molde/1?page=1&pageSize=1" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<EncomendaMoldeDto>
                {
                    Items =
                    [
                        new EncomendaMoldeDto
                        {
                            EncomendaMolde_id = 10,
                            Encomenda_id = 20,
                            Molde_id = 1,
                            Quantidade = 120,
                            Prioridade = 1,
                            DataEntregaPrevista = new DateTime(2026, 6, 18),
                            Estado = "ABERTO",
                            NumeroEncomendaCliente = "ENC-001",
                            NumeroMolde = "M-001"
                        }
                    ],
                    Page = 1,
                    PageSize = 1,
                    TotalItems = 1
                }),
            "/api/encomenda-moldes/por-molde/2?page=1&pageSize=1" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<EncomendaMoldeDto>
                {
                    Items = [],
                    Page = 1,
                    PageSize = 1,
                    TotalItems = 0
                }),
            "/api/encomenda-moldes/por-molde/1?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<EncomendaMoldeDto>
                {
                    Items =
                    [
                        new EncomendaMoldeDto
                        {
                            EncomendaMolde_id = 10,
                            Encomenda_id = 20,
                            Molde_id = 1,
                            Quantidade = 120,
                            Prioridade = 1,
                            DataEntregaPrevista = new DateTime(2026, 6, 18),
                            Estado = "ABERTO",
                            NumeroEncomendaCliente = "ENC-001",
                            NumeroMolde = "M-001"
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 1
                }),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        out List<HttpRequestMessage> requests)
    {
        var handler = new RecordingHttpMessageHandler(responder);
        requests = handler.Requests;

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };
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

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_responder(request));
        }
    }
}
