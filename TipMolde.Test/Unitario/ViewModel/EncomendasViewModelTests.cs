using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.View;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

[TestFixture]
[Category("Unit")]
public class EncomendasViewModelTests
{
    private Mock<INavigationService> _navigationService = null!;
    private EncomendasViewModel _sut = null!;
    private List<string> _paths = null!;

    [SetUp]
    public void SetUp()
    {
        _navigationService = new Mock<INavigationService>();
        var httpClient = CreateHttpClient(HandleRequest, out _paths);
        _sut = new EncomendasViewModel(new EncomendasService(httpClient), _navigationService.Object);
        _sut.PageSize = 2;
    }

    [Test(Description = "T1ENC - A listagem principal de encomendas deve carregar apenas encomendas ativas e respeitar pagina atual.")]
    [Category("Smoke")]
    public async Task LoadEncomendasAsync_Should_LoadCurrentActiveOrdersPage()
    {
        _sut.Page = 1;

        await _sut.LoadEncomendasAsync();

        _sut.Encomendas.Should().HaveCount(2);
        _sut.TotalItems.Should().Be(3);
        _sut.TotalPages.Should().Be(2);
        _sut.HasEncomendas.Should().BeTrue();
    }

    [Test(Description = "T2ENC - A pesquisa deve usar o endpoint de encomendas nao concluidas filtradas.")]
    public async Task PesquisarCommand_Should_UseSearchEndpointForActiveOrders()
    {
        _sut.SearchTerm = "Aero";

        await _sut.PesquisarCommand.ExecuteAsync(null);

        _sut.Encomendas.Should().HaveCount(1);
        _sut.Encomendas.Single().Encomenda_id.Should().Be(30);
        _paths.Should().Contain("/api/encomendas/em-producao/search?searchTerm=Aero&page=1&pageSize=2");
    }

    [Test(Description = "T2BENC - A pesquisa deve voltar a primeira pagina quando parte de uma pagina posterior.")]
    public async Task PesquisarCommand_Should_ResetPageToFirst_When_SearchStartsFromSecondPage()
    {
        _sut.Page = 2;
        _sut.SearchTerm = "Aero";

        await _sut.PesquisarCommand.ExecuteAsync(null);

        _sut.Page.Should().Be(1);
        _paths.Should().Contain("/api/encomendas/em-producao/search?searchTerm=Aero&page=1&pageSize=2");
    }

    [Test(Description = "T2CENC - Quando a API falha, a listagem deve expor erro funcional e manter o estado vazio.")]
    public async Task LoadEncomendasAsync_Should_SetError_When_ServiceReturnsNull()
    {
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            out _);
        var sut = new EncomendasViewModel(new EncomendasService(httpClient), _navigationService.Object)
        {
            PageSize = 2
        };

        await sut.LoadEncomendasAsync();

        sut.ErrorMessage.Should().Be("Nao foi possivel carregar as encomendas ativas.");
        sut.Encomendas.Should().BeEmpty();
        sut.HasEncomendas.Should().BeFalse();
    }

    [Test(Description = "T2DENC - A segunda pagina deve carregar a encomenda remanescente quando o utilizador avanca na paginacao.")]
    public async Task NextPageCommand_Should_LoadRemainingOrder()
    {
        await _sut.LoadEncomendasAsync();

        await _sut.NextPageCommand.ExecuteAsync(null);

        _sut.Page.Should().Be(2);
        _sut.Encomendas.Should().ContainSingle();
        _sut.Encomendas.Single().Encomenda_id.Should().Be(32);
        _paths.Should().Contain("/api/encomendas/em-producao?page=2&pageSize=2");
    }

    [Test(Description = "T3ENC - O frontend deve navegar para criar encomenda quando o utilizador usa o atalho respetivo.")]
    public async Task AbrirAdicionarEncomendaCommand_Should_NavigateToCreatePage()
    {
        await _sut.AbrirAdicionarEncomendaCommand.ExecuteAsync(null);

        _navigationService.Verify(service => service.GoToAsync(nameof(AdicionarEncomendaPage)), Times.Once);
    }

    [Test(Description = "T4ENC - O frontend deve navegar para o detalhe da encomenda selecionada.")]
    public async Task AbrirEncomendaCommand_Should_NavigateToDetailPage()
    {
        await _sut.AbrirEncomendaCommand.ExecuteAsync(new EncomendaResumoDto { Encomenda_id = 31 });

        _navigationService.Verify(service => service.GoToAsync($"{nameof(EncomendaDetalhePage)}?encomenda_id=31"), Times.Once);
    }

    private static HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/encomendas/em-producao?page=1&pageSize=2" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreatePage(1, 2, 3,
                [
                    CreateOrder(30, "Aero 1"),
                    CreateOrder(31, "Aero 2")
                ])),
            "/api/encomendas/em-producao?page=2&pageSize=2" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreatePage(2, 2, 3,
                [
                    CreateOrder(32, "Aero 3")
                ])),
            "/api/encomendas/em-producao/search?searchTerm=Aero&page=1&pageSize=2" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreatePage(1, 2, 1,
                [
                    CreateOrder(30, "Aero 1")
                ])),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private static EncomendaResumoDto CreateOrder(int id, string service)
    {
        return new EncomendaResumoDto
        {
            Encomenda_id = id,
            NumeroEncomendaCliente = $"ENC-{id}",
            NomeServicoCliente = service,
            Estado = "EM_PRODUCAO",
            NomeCliente = "Cliente Teste",
            DataRegisto = new DateTime(2026, 7, 7)
        };
    }

    private static PagedResult<EncomendaResumoDto> CreatePage(int page, int pageSize, int totalItems, List<EncomendaResumoDto> items)
    {
        return new PagedResult<EncomendaResumoDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responder, out List<string> paths)
    {
        var handler = new StubHttpMessageHandler(responder);
        paths = handler.Paths;
        return new HttpClient(handler) { BaseAddress = new Uri("https://tipmolde.test/") };
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
        public List<string> Paths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri?.PathAndQuery ?? string.Empty);
            return Task.FromResult(responder(request));
        }
    }
}
