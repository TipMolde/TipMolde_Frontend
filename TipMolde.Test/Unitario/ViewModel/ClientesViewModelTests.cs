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
public class ClientesViewModelTests
{
    private Mock<IDialogService> _dialogService = null!;
    private ClientesViewModel _sut = null!;
    private List<RecordedRequest> _requests = null!;

    [SetUp]
    public void SetUp()
    {
        _dialogService = new Mock<IDialogService>();
        _dialogService.Setup(service => service.ConfirmDeleteAsync(It.IsAny<string>())).ReturnsAsync(true);
        _dialogService.Setup(service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _dialogService.Setup(service => service.ShowErrorAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var httpClient = CreateHttpClient(HandleRequest, out _requests);
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1));

        _sut = new ClientesViewModel(
            new ClientesService(httpClient),
            _dialogService.Object,
            new AuthorizationService(new SessaoPersistidaService(httpClient), new UtilizadoresService(httpClient)));
        _sut.PageSize = 2;
    }

    [Test(Description = "T1CLI - A listagem de clientes deve carregar a pagina atual com permissao de remocao para ADMIN.")]
    [Category("Smoke")]
    public async Task LoadClientesAsync_Should_LoadCurrentPageAndPermissions()
    {
        _sut.Page = 1;

        await _sut.LoadClientesAsync();

        _sut.CanDeleteClients.Should().BeTrue();
        _sut.Clientes.Should().HaveCount(2);
        _sut.TotalItems.Should().Be(3);
        _sut.TotalPages.Should().Be(2);
        _sut.Clientes.Select(item => item.Cliente_id).Should().ContainInOrder(1, 2);
    }

    [Test(Description = "T2CLI - A pesquisa por sigla deve usar o endpoint correto e devolver apenas clientes correspondentes.")]
    public async Task PesquisarCommand_Should_SearchBySigla_When_SearchModeIsSigla()
    {
        _sut.EnsureDefaultSearchMode();
        _sut.SearchTerm = "TM";

        await _sut.PesquisarCommand.ExecuteAsync(null);

        _sut.Clientes.Should().HaveCount(1);
        _sut.Clientes.Single().Sigla.Should().Be("TM");
        _requests.Should().Contain(request =>
            request.Path == "/api/clientes/search/by-sigla?searchTerm=TM&page=1&pageSize=2");
    }

    [Test(Description = "T3CLI - A pesquisa por nome deve usar o endpoint correto quando o modo selecionado e Nome.")]
    public async Task PesquisarCommand_Should_SearchByName_When_SearchModeIsNome()
    {
        _sut.SelectedSearchModeIndex = 0;
        _sut.SearchTerm = "Molde";

        await _sut.PesquisarCommand.ExecuteAsync(null);

        _sut.Clientes.Should().HaveCount(2);
        _sut.Clientes.Select(item => item.Cliente_id).Should().ContainInOrder(1, 3);
        _requests.Should().Contain(request =>
            request.Path == "/api/clientes/search/by-name?searchTerm=Molde&page=1&pageSize=2");
    }

    [Test(Description = "T3BCLI - A pesquisa deve regressar a primeira pagina mesmo quando parte de uma pagina posterior.")]
    public async Task PesquisarCommand_Should_ResetPageToFirst_When_SearchStartsFromSecondPage()
    {
        _sut.Page = 2;
        _sut.EnsureDefaultSearchMode();
        _sut.SearchTerm = "TM";

        await _sut.PesquisarCommand.ExecuteAsync(null);

        _sut.Page.Should().Be(1);
        _requests.Should().Contain(request =>
            request.Path == "/api/clientes/search/by-sigla?searchTerm=TM&page=1&pageSize=2");
    }

    [Test(Description = "T4CLI - Ao eliminar o ultimo item da pagina, o frontend deve recuar uma pagina e recarregar a lista.")]
    public async Task DeleteCommand_Should_MoveBackOnePage_When_LastItemOfCurrentPageIsRemoved()
    {
        _sut.Page = 2;
        await _sut.LoadClientesAsync();

        await _sut.DeleteCommand.ExecuteAsync(_sut.Clientes.Single());

        _sut.Page.Should().Be(1);
        _sut.Clientes.Should().HaveCount(2);
        _sut.Clientes.Select(item => item.Cliente_id).Should().ContainInOrder(1, 2);
        _requests.Should().Contain(request => request.Method == HttpMethod.Delete && request.Path == "/api/clientes/3");
        _dialogService.Verify(service => service.ShowSuccessAsync("Sucesso", "O cliente foi eliminado com sucesso."), Times.Once);
    }

    [Test(Description = "T5CLI - Um erro da API ao carregar clientes deve limpar a lista e expor a mensagem funcional.")]
    public async Task LoadClientesAsync_Should_SetErrorAndClearItems_When_ServiceReturnsNull()
    {
        var httpClient = CreateHttpClient(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/users/me")
            {
                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    new UtilizadorDto { User_id = 1, Nome = "Admin", Email = "admin@tipmolde.pt", Role = "ADMIN" });
            }

            if (request.RequestUri?.PathAndQuery == "/api/clientes?page=1&pageSize=2")
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }, out _);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(1));

        var sut = new ClientesViewModel(
            new ClientesService(httpClient),
            _dialogService.Object,
            new AuthorizationService(new SessaoPersistidaService(httpClient), new UtilizadoresService(httpClient)))
        {
            PageSize = 2
        };

        await sut.LoadClientesAsync();

        sut.ErrorMessage.Should().Be("Nao foi possivel carregar os clientes.");
        sut.Clientes.Should().BeEmpty();
        sut.TotalItems.Should().Be(0);
        sut.TotalPages.Should().Be(1);
    }

    [Test(Description = "T6CLI - Um gestor comercial deve conseguir ver a lista, mas sem permissao para eliminar clientes.")]
    public async Task LoadClientesAsync_Should_DisableDeletion_When_RoleIsCommercialManager()
    {
        var httpClient = CreateHttpClient(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/users/me")
            {
                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    new UtilizadorDto
                    {
                        User_id = 8,
                        Nome = "Gestor Comercial",
                        Email = "comercial@tipmolde.pt",
                        Role = "GESTOR_COMERCIAL"
                    });
            }

            if (request.RequestUri?.PathAndQuery == "/api/clientes?page=1&pageSize=2")
            {
                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    CreateClientPage(1, 2, 2, [CreateClient(1, "Tip Molde", "TM"), CreateClient(2, "Cliente XPTO", "CX")]));
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }, out _);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwtToken(8));

        var sut = new ClientesViewModel(
            new ClientesService(httpClient),
            _dialogService.Object,
            new AuthorizationService(new SessaoPersistidaService(httpClient), new UtilizadoresService(httpClient)))
        {
            PageSize = 2
        };

        await sut.LoadClientesAsync();

        sut.CanDeleteClients.Should().BeFalse();
        sut.Clientes.Should().HaveCount(2);
        sut.ErrorMessage.Should().BeEmpty();
    }

    private static HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/users/me" => CreateJsonResponse(
                HttpStatusCode.OK,
                new UtilizadorDto { User_id = 1, Nome = "Admin", Email = "admin@tipmolde.pt", Role = "ADMIN" }),
            "/api/clientes?page=1&pageSize=2" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreateClientPage(1, 2, 3, [CreateClient(1, "Tip Molde", "TM"), CreateClient(2, "Cliente XPTO", "CX")])),
            "/api/clientes?page=2&pageSize=2" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreateClientPage(2, 2, 3, [CreateClient(3, "Molde Norte", "MN")])),
            "/api/clientes/search/by-sigla?searchTerm=TM&page=1&pageSize=2" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreateClientPage(1, 2, 1, [CreateClient(1, "Tip Molde", "TM")])),
            "/api/clientes/search/by-name?searchTerm=Molde&page=1&pageSize=2" => CreateJsonResponse(
                HttpStatusCode.OK,
                CreateClientPage(1, 2, 2, [CreateClient(1, "Tip Molde", "TM"), CreateClient(3, "Molde Norte", "MN")])),
            "/api/clientes/3" when request.Method == HttpMethod.Delete => new HttpResponseMessage(HttpStatusCode.NoContent),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private static ClienteDto CreateClient(int id, string name, string sigla)
    {
        return new ClienteDto
        {
            Cliente_id = id,
            Nome = name,
            Sigla = sigla,
            NIF = $"500{id}",
            Pais = "Portugal",
            Email = $"cliente{id}@tipmolde.pt",
            Telefone = $"9100000{id}"
        };
    }

    private static PagedResult<ClienteDto> CreateClientPage(int page, int pageSize, int totalItems, List<ClienteDto> items)
    {
        return new PagedResult<ClienteDto>
        {
            Items = items,
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
        return new HttpClient(handler) { BaseAddress = new Uri("https://tipmolde.test/") };
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
