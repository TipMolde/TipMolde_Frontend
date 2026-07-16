using System.Net;
using FluentAssertions;
using NUnit.Framework;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

[TestFixture]
[Category("Unit")]
public class ClienteDetalheViewModelTests
{
    private ClienteDetalheViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var httpClient = CreateHttpClient(HandleRequest);
        _sut = new ClienteDetalheViewModel(new ClientesService(httpClient));
    }

    [Test(Description = "T1CLIDET - O detalhe do cliente deve carregar cinco encomendas por pagina, priorizando as em producao.")]
    [Category("Smoke")]
    public async Task LoadAsync_Should_LoadFirstFiveOrdersSortedByStateAndDate()
    {
        await _sut.LoadAsync(9);

        _sut.Nome.Should().Be("Cliente Norte");
        _sut.SiglaDisplay.Should().Be("CN");
        _sut.PageSize.Should().Be(5);
        _sut.Encomendas.Should().HaveCount(5);
        _sut.TotalPages.Should().Be(2);
        _sut.Encomendas.Select(item => item.Encomenda_id).Should().ContainInOrder(14, 12, 10, 13, 15);
    }

    [Test(Description = "T2CLIDET - O filtro de concluidas deve recarregar localmente apenas as encomendas concluidas.")]
    public async Task SelectedEstadoFilterIndex_Should_FilterConcludedOrders()
    {
        await _sut.LoadAsync(9);

        _sut.SelectedEstadoFilterIndex = 2;
        await WaitUntilAsync(() => _sut.Encomendas.Count == 2);

        _sut.Encomendas.Should().HaveCount(2);
        _sut.Encomendas.All(item => string.Equals(item.Estado, "CONCLUIDA", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
        _sut.TotalPages.Should().Be(1);
    }

    [Test(Description = "T3CLIDET - A pesquisa local deve filtrar por numero, servico, responsavel ou projeto do cliente.")]
    public async Task PesquisarCommand_Should_FilterOrdersBySearchTerm()
    {
        await _sut.LoadAsync(9);
        _sut.SearchTerm = "Projeto-Gamma";

        await _sut.PesquisarCommand.ExecuteAsync(null);

        _sut.Encomendas.Should().HaveCount(1);
        _sut.Encomendas.Single().Encomenda_id.Should().Be(12);
        _sut.EmptyEncomendasMessage.Should().Be("Nenhuma encomenda corresponde aos filtros atuais.");
    }

    [Test(Description = "T4CLIDET - Ao limpar a pesquisa, a pagina deve voltar ao conjunto completo da primeira pagina.")]
    public async Task LimparPesquisaCommand_Should_ResetSearchAndReloadFirstPage()
    {
        await _sut.LoadAsync(9);
        _sut.SearchTerm = "Alpha";
        await _sut.PesquisarCommand.ExecuteAsync(null);

        await _sut.LimparPesquisaCommand.ExecuteAsync(null);

        _sut.SearchTerm.Should().BeEmpty();
        _sut.Page.Should().Be(1);
        _sut.Encomendas.Should().HaveCount(5);
    }

    [Test(Description = "T5CLIDET - A segunda pagina deve mostrar a encomenda restante quando o utilizador avanca na paginacao.")]
    public async Task NextPageCommand_Should_LoadRemainingOrders()
    {
        await _sut.LoadAsync(9);

        await _sut.NextPageCommand.ExecuteAsync(null);

        _sut.Page.Should().Be(2);
        _sut.Encomendas.Should().HaveCount(1);
        _sut.Encomendas.Single().Encomenda_id.Should().Be(11);
        _sut.CanGoNext.Should().BeFalse();
        _sut.CanGoPrevious.Should().BeTrue();
    }

    [Test(Description = "T6CLIDET - Quando o cliente nao tem encomendas, o detalhe deve apresentar a mensagem vazia correta.")]
    public async Task LoadAsync_Should_ShowEmptyMessage_When_ClientHasNoOrders()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/clientes/99/encomendas")
            {
                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    new ClienteComEncomendasDto
                    {
                        Cliente_id = 99,
                        Nome = "Cliente Sem Encomendas",
                        Sigla = "CSE",
                        Encomendas = []
                    });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var sut = new ClienteDetalheViewModel(new ClientesService(httpClient));

        // ACT
        await sut.LoadAsync(99);

        // ASSERT
        sut.Encomendas.Should().BeEmpty();
        sut.HasNoEncomendas.Should().BeTrue();
        sut.EmptyEncomendasMessage.Should().Be("Este cliente ainda não tem encomendas associadas.");
        sut.TotalPages.Should().Be(1);
    }

    [Test(Description = "T7CLIDET - Um detalhe inexistente deve expor erro e manter a lista vazia.")]
    public async Task LoadAsync_Should_SetErrorMessage_When_ClientDetailCannotBeLoaded()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new ClienteDetalheViewModel(new ClientesService(httpClient));

        // ACT
        await sut.LoadAsync(404);

        // ASSERT
        sut.ErrorMessage.Should().Be("Nao foi possivel carregar o detalhe do cliente.");
        sut.HasError.Should().BeTrue();
        sut.Encomendas.Should().BeEmpty();
    }

    [Test(Description = "T8CLIDET - O detalhe deve apresentar valores por omissao quando os campos opcionais do cliente nao existem.")]
    public async Task LoadAsync_Should_ExposeFallbackDisplays_When_OptionalFieldsAreMissing()
    {
        var httpClient = CreateHttpClient(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/clientes/10/encomendas")
            {
                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    new ClienteComEncomendasDto
                    {
                        Cliente_id = 10,
                        Nome = "Cliente Sem Dados",
                        Sigla = string.Empty,
                        Pais = string.Empty,
                        Email = " ",
                        Telefone = string.Empty,
                        NIF = string.Empty,
                        Encomendas = []
                    });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var sut = new ClienteDetalheViewModel(new ClientesService(httpClient));

        await sut.LoadAsync(10);

        sut.SiglaDisplay.Should().Be("Nao definido");
        sut.PaisDisplay.Should().Be("Nao definido");
        sut.EmailDisplay.Should().Be("Nao definido");
        sut.TelefoneDisplay.Should().Be("Nao definido");
        sut.NifDisplay.Should().Be("Nao definido");
    }

    [Test(Description = "T9CLIDET - Um salto para pagina invalida deve devolver erro sem alterar a pagina atual.")]
    public async Task GoToPageCommand_Should_SetValidationError_When_PageExceedsTotalPages()
    {
        await _sut.LoadAsync(9);
        _sut.PageInput = "99";

        await _sut.GoToPageCommand.ExecuteAsync(null);

        _sut.Page.Should().Be(1);
        _sut.ErrorMessage.Should().Be("A página não pode ser maior que 2.");
        _sut.Encomendas.Should().HaveCount(5);
    }

    private static HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/clientes/9/encomendas" => CreateJsonResponse(
                HttpStatusCode.OK,
                new ClienteComEncomendasDto
                {
                    Cliente_id = 9,
                    Nome = "Cliente Norte",
                    Sigla = "CN",
                    Pais = "Portugal",
                    Email = "cliente.norte@tipmolde.pt",
                    Telefone = "910100100",
                    NIF = "509999999",
                    Encomendas =
                    [
                        CreateOrder(10, "ENC-010", "Projeto-Alpha", "Servico Alpha", "Ana", "EM_PRODUCAO", new DateTime(2026, 7, 2)),
                        CreateOrder(11, "ENC-011", "Projeto-Beta", "Servico Beta", "Bruno", "CONCLUIDA", new DateTime(2026, 7, 3)),
                        CreateOrder(12, "ENC-012", "Projeto-Gamma", "Servico Gamma", "Carla", "EM_PRODUCAO", new DateTime(2026, 7, 4)),
                        CreateOrder(13, "ENC-013", "Projeto-Delta", "Servico Delta", "Diogo", "EM_PRODUCAO", new DateTime(2026, 7, 1)),
                        CreateOrder(14, "ENC-014", "Projeto-Epsilon", "Servico Epsilon", "Eva", "EM_PRODUCAO", new DateTime(2026, 7, 5)),
                        CreateOrder(15, "ENC-015", "Projeto-Zeta", "Servico Zeta", "Filipe", "CONCLUIDA", new DateTime(2026, 7, 6))
                    ]
                }),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private static EncomendaResumoDto CreateOrder(
        int id,
        string number,
        string project,
        string service,
        string responsible,
        string state,
        DateTime createdAt)
    {
        return new EncomendaResumoDto
        {
            Encomenda_id = id,
            NumeroEncomendaCliente = number,
            NumeroProjetoCliente = project,
            NomeServicoCliente = service,
            NomeResponsavelCliente = responsible,
            Estado = state,
            DataRegisto = createdAt,
            Cliente_id = 9,
            NomeCliente = "Cliente Norte"
        };
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
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var start = Environment.TickCount64;
        while (!condition())
        {
            if (Environment.TickCount64 - start > timeoutMs)
                Assert.Fail("Timeout waiting for the view model to update.");

            await Task.Delay(25);
        }
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
