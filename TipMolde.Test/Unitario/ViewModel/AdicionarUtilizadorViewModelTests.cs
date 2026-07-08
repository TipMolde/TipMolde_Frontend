using FluentAssertions;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Text;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

[TestFixture]
[Category("Unit")]
public class AdicionarUtilizadorViewModelTests
{
    [Test(Description = "T1USRADD - O formulario deve bloquear a criacao quando nao existe cargo selecionado.")]
    public async Task CreateCommand_Should_SetValidationError_When_RoleIsMissing()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound), out var requests);
        var dialogService = new Mock<IDialogService>();
        var sut = new AdicionarUtilizadorViewModel(new UtilizadoresService(httpClient), dialogService.Object)
        {
            Nome = "Goncalo Barroso",
            Email = "goncalo@tipmolde.pt",
            Password = "Password123!",
            SelectedRole = string.Empty
        };

        // ACT
        await sut.CreateCommand.ExecuteAsync(null);

        // ASSERT
        sut.ErrorMessage.Should().Be("Tens de selecionar um cargo.");
        requests.Should().BeEmpty();
        dialogService.Verify(service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test(Description = "T2USRADD - O formulario deve validar a password antes de chamar a API.")]
    public async Task CreateCommand_Should_SetValidationError_When_PasswordIsWeak()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound), out var requests);
        var dialogService = new Mock<IDialogService>();
        var sut = new AdicionarUtilizadorViewModel(new UtilizadoresService(httpClient), dialogService.Object)
        {
            Nome = "Goncalo Barroso",
            Email = "goncalo@tipmolde.pt",
            Password = "fraca",
            SelectedRole = "GESTOR_DESENHO"
        };

        // ACT
        await sut.CreateCommand.ExecuteAsync(null);

        // ASSERT
        sut.ErrorMessage.Should().Be("A password tem de ter pelo menos 8 caracteres.");
        requests.Should().BeEmpty();
    }

    [Test(Description = "T3USRADD - O formulario deve expor o erro funcional devolvido pela API na criacao.")]
    public async Task CreateCommand_Should_SurfaceApiError_When_ServiceThrows()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"detail":"Ja existe um utilizador com este email."}""",
                    Encoding.UTF8,
                    "application/json")
            },
            out var requests);
        var dialogService = new Mock<IDialogService>();
        var sut = new AdicionarUtilizadorViewModel(new UtilizadoresService(httpClient), dialogService.Object)
        {
            Nome = "  Goncalo Barroso  ",
            Email = "goncalo@tipmolde.pt",
            Password = "Password123!",
            SelectedRole = "GESTOR_DESENHO"
        };

        // ACT
        await sut.CreateCommand.ExecuteAsync(null);

        // ASSERT
        sut.ErrorMessage.Should().Be("Ja existe um utilizador com este email.");
        sut.IsSaving.Should().BeFalse();
        requests.Should().ContainSingle();
        requests[0].Path.Should().Be("/api/users");
        requests[0].Body.Should().Contain("\"nome\":\"Goncalo Barroso\"");
        requests[0].Body.Should().Contain("\"role\":\"GESTOR_DESENHO\"");
        dialogService.Verify(service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
}
