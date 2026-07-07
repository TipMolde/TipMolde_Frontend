using FluentAssertions;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

/// <summary>
/// Testes unitarios e smoke leves do ViewModel de criacao de encomendas.
/// </summary>
[TestFixture]
[Category("Unit")]
public class AdicionarEncomendaViewModelTests
{
    [Test(Description = "T1FRT - O carregamento inicial deve preencher clientes, servicos e a primeira pagina de moldes.")]
    [Category("Smoke")]
    public async Task LoadAsync_Should_PopulateFirstPageAndDefaultSelections_When_DataIsAvailable()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(HandleLoadRequest, out var requests);
        var dialogService = new Mock<IDialogService>();
        var sut = CreateSut(httpClient, dialogService.Object);

        // ACT
        await sut.LoadAsync();

        // ASSERT
        sut.ClientesDisponiveis.Should().HaveCount(2);
        sut.NomeServicoOptions.Should().HaveCount(3);
        sut.MoldesVisiveis.Should().HaveCount(6);
        sut.MoldeTotalPages.Should().Be(2);
        sut.CanGoPreviousMoldes.Should().BeFalse();
        sut.CanGoNextMoldes.Should().BeTrue();
        sut.SelectedClienteOption.Should().NotBeNull();
        sut.SelectedClienteOption!.Cliente.Cliente_id.Should().Be(1);
        requests.Should().Contain(request => request.Path == "/api/clientes?page=1&pageSize=100");
        requests.Should().Contain(request => request.Path == "/api/moldes?page=1&pageSize=100");
    }

    [Test(Description = "T2FRT - A pesquisa de moldes deve reiniciar a pagina e filtrar a lista visivel.")]
    [Category("Smoke")]
    public async Task MoldeSearchTerm_Should_ResetPageAndFilterVisibleMoldes_When_UserSearches()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(HandleLoadRequest, out _);
        var sut = CreateSut(httpClient, new Mock<IDialogService>().Object);
        await sut.LoadAsync();

        // ACT
        sut.NextMoldesPageCommand.Execute(null);
        sut.MoldePage.Should().Be(2);

        sut.MoldeSearchTerm = "M-007";

        // ASSERT
        sut.MoldePage.Should().Be(1);
        sut.MoldesVisiveis.Should().ContainSingle();
        sut.MoldesVisiveis[0].Molde.MoldeId.Should().Be(7);
        sut.CanGoNextMoldes.Should().BeFalse();
    }

    [Test(Description = "T3FRT - A criacao deve falhar cedo quando o numero da encomenda nao e indicado.")]
    public async Task CreateCommand_Should_SetValidationError_When_NumeroEncomendaIsMissing()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(HandleLoadRequest, out var requests);
        var dialogService = new Mock<IDialogService>();
        var sut = CreateSut(httpClient, dialogService.Object);

        // ACT
        await sut.CreateCommand.ExecuteAsync(null);

        // ASSERT
        sut.ErrorMessage.Should().Be("Indique o numero da encomenda.");
        sut.HasError.Should().BeTrue();
        requests.Should().BeEmpty();
        dialogService.Verify(
            service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    private static AdicionarEncomendaViewModel CreateSut(HttpClient httpClient, IDialogService dialogService)
    {
        var encomendasService = new EncomendasService(httpClient);

        return new AdicionarEncomendaViewModel(
            new ClientesService(httpClient),
            new MoldesService(httpClient),
            encomendasService,
            new GlobalMoldePriorityService(encomendasService),
            dialogService);
    }

    private static HttpResponseMessage HandleLoadRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/clientes?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<ClienteDto>
                {
                    Items =
                    [
                        new ClienteDto
                        {
                            Cliente_id = 2,
                            Nome = "Beta",
                            NIF = "222222222",
                            Sigla = "BE"
                        },
                        new ClienteDto
                        {
                            Cliente_id = 1,
                            Nome = "Alfa",
                            NIF = "111111111",
                            Sigla = "AL"
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 2
                }),
            "/api/moldes?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<MoldeDto>
                {
                    Items =
                    [
                        BuildMolde(1, "M-001", "Molde 1"),
                        BuildMolde(2, "M-002", "Molde 2"),
                        BuildMolde(3, "M-003", "Molde 3"),
                        BuildMolde(4, "M-004", "Molde 4"),
                        BuildMolde(5, "M-005", "Molde 5"),
                        BuildMolde(6, "M-006", "Molde 6"),
                        BuildMolde(7, "M-007", "Molde 7")
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 7
                }),
            "/api/encomendas/em-producao?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<EncomendaResumoDto>
                {
                    Items = [],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 0
                }),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { detail = $"Endpoint de teste nao previsto: {request.RequestUri?.PathAndQuery}" }),
                    Encoding.UTF8,
                    "application/json")
            }
        };
    }

    private static MoldeDto BuildMolde(int moldeId, string numero, string nome)
    {
        return new MoldeDto
        {
            MoldeId = moldeId,
            Numero = numero,
            Nome = nome,
            Descricao = $"Descricao {moldeId}"
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
