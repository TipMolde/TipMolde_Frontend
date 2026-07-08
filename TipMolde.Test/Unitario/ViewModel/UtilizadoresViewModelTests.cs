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

[TestFixture]
[Category("Unit")]
public class UtilizadoresViewModelTests
{
    private UtilizadoresViewModel _sut = null!;
    private Mock<IDialogService> _dialogService = null!;
    private ScenarioState _state = null!;
    private List<RecordedRequest> _requests = null!;

    [SetUp]
    public void SetUp()
    {
        _state = new ScenarioState();
        _dialogService = new Mock<IDialogService>();
        _dialogService.Setup(service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _dialogService.Setup(service => service.ShowErrorAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var httpClient = CreateHttpClient(request => HandleRequest(request, _state), out _requests);
        _sut = new UtilizadoresViewModel(new UtilizadoresService(httpClient), _dialogService.Object)
        {
            PageSize = 2
        };
    }

    [Test(Description = "T1USRLIST - A listagem deve carregar a primeira pagina de utilizadores com paginacao coerente.")]
    public async Task LoadUtilizadoresAsync_Should_LoadFirstPage_When_SearchIsEmpty()
    {
        // ACT
        await _sut.LoadUtilizadoresAsync();

        // ASSERT
        _sut.Utilizadores.Should().HaveCount(2);
        _sut.TotalPages.Should().Be(2);
        _sut.Utilizadores.Select(item => item.User_id).Should().ContainInOrder(1, 2);
        _requests.Should().Contain(request => request.Path == "/api/users?page=1&pageSize=2");
    }

    [Test(Description = "T2USRLIST - A pesquisa deve usar o endpoint de search e devolver apenas os resultados filtrados.")]
    public async Task PesquisarCommand_Should_UseSearchEndpoint_When_SearchTermIsProvided()
    {
        // ARRANGE
        await _sut.LoadUtilizadoresAsync();
        _sut.SearchTerm = "Ana";

        // ACT
        await _sut.PesquisarCommand.ExecuteAsync(null);

        // ASSERT
        _sut.Utilizadores.Should().ContainSingle();
        _sut.Utilizadores[0].Nome.Should().Be("Ana Silva");
        _requests.Should().Contain(request => request.Path == "/api/users/search?searchTerm=Ana&page=1&pageSize=2");
    }

    [Test(Description = "T3USRLIST - Alterar o cargo deve atualizar o backend, recarregar a lista e mostrar sucesso.")]
    public async Task EditarCargoCommand_Should_UpdateRoleReloadAndShowSuccess_When_NewRoleIsSelected()
    {
        // ARRANGE
        await _sut.LoadUtilizadoresAsync();
        var utilizador = _sut.Utilizadores.First(item => item.User_id == 2);

        _dialogService
            .Setup(service => service.ShowSelectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync("GESTOR_PRODUCAO");

        // ACT
        await _sut.EditarCargoCommand.ExecuteAsync(utilizador);

        // ASSERT
        _state.User2Role.Should().Be("GESTOR_PRODUCAO");
        _sut.Utilizadores.First(item => item.User_id == 2).Role.Should().Be("GESTOR_PRODUCAO");
        _dialogService.Verify(service => service.ShowSuccessAsync(
            "Sucesso",
            "O cargo do utilizador Bruno Costa foi atualizado para GESTOR_PRODUCAO com sucesso."),
            Times.Once);
    }

    [Test(Description = "T4USRLIST - Repor password deve falhar localmente quando a nova password nao cumpre as regras.")]
    public async Task ReporPasswordCommand_Should_ShowValidationError_When_NewPasswordIsInvalid()
    {
        // ARRANGE
        await _sut.LoadUtilizadoresAsync();
        var utilizador = _sut.Utilizadores.First();

        _dialogService
            .Setup(service => service.PromptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PromptDialogOptions?>()))
            .ReturnsAsync("fraca");

        // ACT
        await _sut.ReporPasswordCommand.ExecuteAsync(utilizador);

        // ASSERT
        _dialogService.Verify(
            service => service.ShowErrorAsync("Erro", "A password tem de ter pelo menos 8 caracteres."),
            Times.Once);
        _requests.Should().NotContain(request => request.Path.Contains("/password/reset", StringComparison.Ordinal));
    }

    [Test(Description = "T5USRLIST - Eliminar o unico utilizador da segunda pagina deve recuar para a pagina anterior e recarregar a lista.")]
    public async Task AbrirOpcoesCommand_Should_DeleteAndReturnToPreviousPage_When_LastItemIsRemovedFromSecondPage()
    {
        // ARRANGE
        await _sut.LoadUtilizadoresAsync();
        await _sut.NextPageCommand.ExecuteAsync(null);
        _sut.Page.Should().Be(2);
        _sut.Utilizadores.Should().ContainSingle();

        var utilizador = _sut.Utilizadores.Single();

        _dialogService
            .Setup(service => service.ShowOptionsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("Eliminar utilizador");
        _dialogService
            .Setup(service => service.ConfirmDeleteAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // ACT
        await _sut.AbrirOpcoesCommand.ExecuteAsync(utilizador);

        // ASSERT
        _state.DeletedUserId.Should().Be(3);
        _sut.Page.Should().Be(1);
        _sut.Utilizadores.Should().HaveCount(2);
        _sut.Utilizadores.Select(item => item.User_id).Should().ContainInOrder(1, 2);
        _dialogService.Verify(service => service.ShowSuccessAsync("Sucesso", "O utilizador foi eliminado com sucesso."), Times.Once);
    }

    private static HttpResponseMessage HandleRequest(HttpRequestMessage request, ScenarioState state)
    {
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;

        if (path == "/api/users?page=1&pageSize=2")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<UtilizadorDto>
                {
                    Items =
                    [
                        new UtilizadorDto { User_id = 1, Nome = "Ana Silva", Email = "ana@tipmolde.pt", Role = "ADMIN" },
                        new UtilizadorDto { User_id = 2, Nome = "Bruno Costa", Email = "bruno@tipmolde.pt", Role = state.User2Role }
                    ],
                    Page = 1,
                    PageSize = 2,
                    TotalItems = state.DeletedUserId == 3 ? 2 : 3
                });
        }

        if (path == "/api/users?page=2&pageSize=2")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<UtilizadorDto>
                {
                    Items = state.DeletedUserId == 3
                        ? []
                        : [new UtilizadorDto { User_id = 3, Nome = "Carla Matos", Email = "carla@tipmolde.pt", Role = "GESTOR_DESENHO" }],
                    Page = 2,
                    PageSize = 2,
                    TotalItems = state.DeletedUserId == 3 ? 2 : 3
                });
        }

        if (path == "/api/users/search?searchTerm=Ana&page=1&pageSize=2")
        {
            return CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<UtilizadorDto>
                {
                    Items = [new UtilizadorDto { User_id = 1, Nome = "Ana Silva", Email = "ana@tipmolde.pt", Role = "ADMIN" }],
                    Page = 1,
                    PageSize = 2,
                    TotalItems = 1
                });
        }

        if (path == "/api/users/2/role" && request.Method == HttpMethod.Put)
        {
            state.User2Role = "GESTOR_PRODUCAO";
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        if (path == "/api/users/3" && request.Method == HttpMethod.Delete)
        {
            state.DeletedUserId = 3;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
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
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
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

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new RecordedRequest(request.Method, request.RequestUri?.PathAndQuery ?? string.Empty, body));
            return _responder(request);
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Path, string Body);

    private sealed class ScenarioState
    {
        public string User2Role { get; set; } = "GESTOR_COMERCIAL";
        public int? DeletedUserId { get; set; }
    }
}
