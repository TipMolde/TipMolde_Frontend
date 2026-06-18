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
public class PedidosMaterialViewModelTests
{
    private Mock<IDialogService> _dialogService = null!;
    private PedidosMaterialViewModel _sut = null!;
    private List<RecordedRequest> _requests = null!;

    [SetUp]
    public void SetUp()
    {
        _dialogService = new Mock<IDialogService>();

        var httpClient = CreateHttpClient(HandleRequest, out _requests);
        _sut = new PedidosMaterialViewModel(
            new FornecedoresService(httpClient),
            new MoldesService(httpClient),
            new PecasService(httpClient),
            new PedidosMaterialService(httpClient),
            _dialogService.Object);

        _dialogService
            .Setup(service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    [Test(Description = "T1FRT - A lista de moldes deve mostrar apenas moldes com pecas disponiveis para pedido.")]
    public async Task LoadAsync_Should_ShowOnlyMoldesWithPieces_When_LoadingCatalog()
    {
        // ACT
        await _sut.LoadAsync();

        // ASSERT
        _sut.MoldesDisponiveis.Should().HaveCount(3);
        _sut.MoldesDisponiveis.Select(item => item.MoldeId).Should().ContainInOrder(1, 3, 4);
        _sut.MoldesDisponiveis.All(item => item.TotalPecasDisponiveis > 0).Should().BeTrue();
    }

    [Test(Description = "T1.0FRT - A lista de moldes nao deve repetir o mesmo molde quando o backend devolve duplicados.")]
    public async Task LoadAsync_Should_DeduplicateMoldesById_When_BackendReturnsRepeatedEntries()
    {
        // ARRANGE
        await _sut.LoadAsync();

        // ASSERT
        _sut.MoldesDisponiveis.Count(item => item.MoldeId == 1).Should().Be(1);
    }

    [Test(Description = "T1.1FRT - A pesquisa de pecas deve filtrar a lista visivel sem perder o contexto do molde.")]
    public async Task PecaSearchTerm_Should_FilterVisiblePieces_When_UserSearches()
    {
        // ARRANGE
        await _sut.LoadAsync();

        _sut.MoldesDisponiveis.First(item => item.MoldeId == 1).IsSelected = true;
        _sut.MoldesDisponiveis.First(item => item.MoldeId == 3).IsSelected = true;

        await WaitUntilAsync(() => !_sut.IsLoadingPecas && _sut.PecasDisponiveis.Count == 3);

        // ACT
        _sut.PecaSearchTerm = "Base";

        // ASSERT
        _sut.PecasDisponiveis.Should().HaveCount(1);
        _sut.PecasDisponiveis.Single().NumeroPeca.Should().Be("P-011");
        _sut.PecaInfoMessage.Should().Contain("correspondem");
    }

    [Test(Description = "T1.2FRT - A lista de pecas deve carregar paginas adicionais ao chegar ao fim da lista visivel.")]
    public async Task CarregarMaisPecasAsync_Should_AppendNextPage_When_ReachingThreshold()
    {
        // ARRANGE
        await _sut.LoadAsync();

        _sut.MoldesDisponiveis.First(item => item.MoldeId == 4).IsSelected = true;

        await WaitUntilAsync(() => !_sut.IsLoadingPecas && _sut.PecasDisponiveis.Count == 20);

        // ACT
        await _sut.CarregarMaisPecasCommand.ExecuteAsync(null);

        // ASSERT
        await WaitUntilAsync(() => !_sut.IsLoadingPecas && _sut.PecasDisponiveis.Count == 25);

        _sut.PecasDisponiveis.Should().HaveCount(25);
        _sut.PecasDisponiveis.Last().PecaId.Should().Be(65);
    }

    [Test(Description = "T2FRT - O pedido deve aceitar pecas de varios moldes e mostrar resumo antes de confirmar.")]
    public async Task CriarPedidoAsync_Should_ShowResumoAndCreatePedidoWithPiecesFromMultipleMoldes_When_UserConfirms()
    {
        // ARRANGE
        await _sut.LoadAsync();

        _sut.MoldesDisponiveis.First(item => item.MoldeId == 1).IsSelected = true;
        _sut.MoldesDisponiveis.First(item => item.MoldeId == 3).IsSelected = true;

        await WaitUntilAsync(() => !_sut.IsLoadingPecas && _sut.PecasDisponiveis.Count == 3);

        _sut.PecasDisponiveis.First(item => item.PecaId == 11).IsSelected = true;
        _sut.PecasDisponiveis.First(item => item.PecaId == 31).IsSelected = true;

        // ACT
        await _sut.CriarPedidoCommand.ExecuteAsync(null);

        // ASSERT
        _sut.IsPedidoResumoVisible.Should().BeTrue();
        _sut.PedidoResumoTitulo.Should().Be("Resumo do pedido de material");
        _sut.PedidoResumoTexto.Should().Contain("Fornecedor: Fornecedor Teste");
        _sut.PedidoResumoTexto.Should().Contain("M-001");
        _sut.PedidoResumoTexto.Should().Contain("M-003");
        _sut.PedidoResumoTexto.Should().Contain("Total de linhas: 2");

        _requests.Should().NotContain(request => request.Method == HttpMethod.Post && request.Path == "/api/pedidos-material");

        await _sut.ConfirmarResumoPedidoCommand.ExecuteAsync(null);

        _sut.IsPedidoResumoVisible.Should().BeFalse();

        _requests.Should().ContainSingle(request => request.Method == HttpMethod.Post && request.Path == "/api/pedidos-material");
        var post = _requests.Single(request => request.Method == HttpMethod.Post && request.Path == "/api/pedidos-material");
        post.Body.Should().Contain("\"fornecedor_id\":1");
        post.Body.Should().Contain("\"peca_id\":11");
        post.Body.Should().Contain("\"peca_id\":31");
    }

    private HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/fornecedores?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<FornecedorDto>
                {
                    Items =
                    [
                        new FornecedorDto
                        {
                            FornecedorId = 1,
                            Nome = "Fornecedor Teste"
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 1
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
                            Nome = "Molde com pecas 1",
                            TipoPedido = "Normal",
                            Numero_cavidades = 2,
                            ImagemCapaPath = "Storage/Uploads/molde-1.png"
                        },
                        new MoldeDto
                        {
                            MoldeId = 2,
                            Numero = "M-002",
                            Nome = "Molde sem pecas",
                            TipoPedido = "Normal",
                            Numero_cavidades = 2,
                            ImagemCapaPath = "Storage/Uploads/molde-2.png"
                        },
                        new MoldeDto
                        {
                            MoldeId = 3,
                            Numero = "M-003",
                            Nome = "Molde com pecas 2",
                            TipoPedido = "Normal",
                            Numero_cavidades = 2,
                            ImagemCapaPath = "Storage/Uploads/molde-3.png"
                        },
                        new MoldeDto
                        {
                            MoldeId = 4,
                            Numero = "M-004",
                            Nome = "Molde paginado",
                            TipoPedido = "Normal",
                            Numero_cavidades = 2,
                            ImagemCapaPath = "Storage/Uploads/molde-4.png"
                        },
                        new MoldeDto
                        {
                            MoldeId = 1,
                            Numero = "M-001",
                            Nome = "Molde com pecas 1",
                            TipoPedido = "Normal",
                            Numero_cavidades = 2,
                            ImagemCapaPath = "Storage/Uploads/molde-1.png"
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 5
                }),
            "/api/pecas/por-molde/1/sem-pedido-material?page=1&pageSize=1" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items =
                    [
                        new PecaDto { PecaId = 11, NumeroPeca = "P-011", Designacao = "Base", Quantidade = 2, Molde_id = 1 }
                    ],
                    Page = 1,
                    PageSize = 1,
                    TotalItems = 2
                }),
            "/api/pecas/por-molde/2/sem-pedido-material?page=1&pageSize=1" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items = [],
                    Page = 1,
                    PageSize = 1,
                    TotalItems = 0
                }),
            "/api/pecas/por-molde/3/sem-pedido-material?page=1&pageSize=1" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items =
                    [
                        new PecaDto { PecaId = 31, NumeroPeca = "P-031", Designacao = "Tampa", Quantidade = 4, Molde_id = 3 }
                    ],
                    Page = 1,
                    PageSize = 1,
                    TotalItems = 1
                }),
            "/api/pecas/por-molde/4/sem-pedido-material?page=1&pageSize=1" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items =
                    [
                        new PecaDto { PecaId = 41, NumeroPeca = "P-041", Designacao = "Base paginada", Quantidade = 1, Molde_id = 4 }
                    ],
                    Page = 1,
                    PageSize = 1,
                    TotalItems = 25
                }),
            "/api/pecas/por-molde/1/sem-pedido-material?page=1&pageSize=20" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items =
                    [
                        new PecaDto { PecaId = 11, NumeroPeca = "P-011", Designacao = "Base", Quantidade = 2, Molde_id = 1 },
                        new PecaDto { PecaId = 12, NumeroPeca = "P-012", Designacao = "Lateral", Quantidade = 3, Molde_id = 1 }
                    ],
                    Page = 1,
                    PageSize = 20,
                    TotalItems = 2
                }),
            "/api/pecas/por-molde/2/sem-pedido-material?page=1&pageSize=20" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items = [],
                    Page = 1,
                    PageSize = 20,
                    TotalItems = 0
                }),
            "/api/pecas/por-molde/3/sem-pedido-material?page=1&pageSize=20" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items =
                    [
                        new PecaDto { PecaId = 31, NumeroPeca = "P-031", Designacao = "Tampa", Quantidade = 4, Molde_id = 3 }
                    ],
                    Page = 1,
                    PageSize = 20,
                    TotalItems = 1
                }),
            "/api/pecas/por-molde/4/sem-pedido-material?page=1&pageSize=20" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items =
                    [
                        new PecaDto { PecaId = 41, NumeroPeca = "P-041", Designacao = "Base paginada 1", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 42, NumeroPeca = "P-042", Designacao = "Base paginada 2", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 43, NumeroPeca = "P-043", Designacao = "Base paginada 3", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 44, NumeroPeca = "P-044", Designacao = "Base paginada 4", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 45, NumeroPeca = "P-045", Designacao = "Base paginada 5", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 46, NumeroPeca = "P-046", Designacao = "Base paginada 6", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 47, NumeroPeca = "P-047", Designacao = "Base paginada 7", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 48, NumeroPeca = "P-048", Designacao = "Base paginada 8", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 49, NumeroPeca = "P-049", Designacao = "Base paginada 9", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 50, NumeroPeca = "P-050", Designacao = "Base paginada 10", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 51, NumeroPeca = "P-051", Designacao = "Base paginada 11", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 52, NumeroPeca = "P-052", Designacao = "Base paginada 12", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 53, NumeroPeca = "P-053", Designacao = "Base paginada 13", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 54, NumeroPeca = "P-054", Designacao = "Base paginada 14", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 55, NumeroPeca = "P-055", Designacao = "Base paginada 15", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 56, NumeroPeca = "P-056", Designacao = "Base paginada 16", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 57, NumeroPeca = "P-057", Designacao = "Base paginada 17", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 58, NumeroPeca = "P-058", Designacao = "Base paginada 18", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 59, NumeroPeca = "P-059", Designacao = "Base paginada 19", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 60, NumeroPeca = "P-060", Designacao = "Base paginada 20", Quantidade = 1, Molde_id = 4 }
                    ],
                    Page = 1,
                    PageSize = 20,
                    TotalItems = 25
                }),
            "/api/pecas/por-molde/4/sem-pedido-material?page=2&pageSize=20" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items =
                    [
                        new PecaDto { PecaId = 61, NumeroPeca = "P-061", Designacao = "Base paginada 21", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 62, NumeroPeca = "P-062", Designacao = "Base paginada 22", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 63, NumeroPeca = "P-063", Designacao = "Base paginada 23", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 64, NumeroPeca = "P-064", Designacao = "Base paginada 24", Quantidade = 1, Molde_id = 4 },
                        new PecaDto { PecaId = 65, NumeroPeca = "P-065", Designacao = "Base paginada 25", Quantidade = 1, Molde_id = 4 }
                    ],
                    Page = 2,
                    PageSize = 20,
                    TotalItems = 25
                }),
            "/api/pedidos-material" when request.Method == HttpMethod.Post => CreateJsonResponse(
                HttpStatusCode.OK,
                new PedidoMaterialDto
                {
                    PedidoMaterialId = 99,
                    DataPedido = new DateTime(2026, 6, 18),
                    Estado = "ABERTO",
                    FornecedorId = 1,
                    Itens =
                    [
                        new PedidoMaterialItemDto { ItemId = 1, PecaId = 11, Quantidade = 2 },
                        new PedidoMaterialItemDto { ItemId = 2, PecaId = 31, Quantidade = 4 }
                    ]
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

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(50);
        }

        Assert.Fail("Condition was not met before the timeout expired.");
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
}
