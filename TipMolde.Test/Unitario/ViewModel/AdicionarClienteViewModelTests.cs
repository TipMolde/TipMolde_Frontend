using FluentAssertions;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Text;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

/// <summary>
/// Testes unitarios do ViewModel de criacao de clientes.
/// </summary>
[TestFixture]
[Category("Unit")]
public class AdicionarClienteViewModelTests
{
    [Test(Description = "T1FRT - O formulario deve bloquear criacao quando os dados obrigatorios estao vazios.")]
    public async Task CreateCommand_Should_SetValidationError_When_RequiredFieldsAreMissing()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            out var requests);
        var dialogService = new Mock<IDialogService>();
        var sut = new AdicionarClienteViewModel(new ClientesService(httpClient), dialogService.Object);

        // ACT
        await sut.CreateCommand.ExecuteAsync(null);

        // ASSERT
        sut.ErrorMessage.Should().Be("O nome do cliente deve ter entre 3 e 100 caracteres.");
        sut.HasError.Should().BeTrue();
        requests.Should().BeEmpty();
        dialogService.Verify(
            service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test(Description = "T2FRT - O formulario deve devolver erro funcional quando a API falha na criacao.")]
    public async Task CreateCommand_Should_SurfaceApiError_When_ServiceThrows()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"detail":"Falha ao criar cliente."}""",
                    Encoding.UTF8,
                    "application/json")
            },
            out var requests);
        var dialogService = new Mock<IDialogService>();
        var sut = new AdicionarClienteViewModel(new ClientesService(httpClient), dialogService.Object)
        {
            Nome = "  Cliente Final  ",
            Nif = "123456789",
            Sigla = "CF",
            Pais = "Portugal",
            Email = "cliente@tipmolde.pt",
            Telefone = "+351912345678"
        };

        // ACT
        await sut.CreateCommand.ExecuteAsync(null);

        // ASSERT
        sut.ErrorMessage.Should().Be("Falha ao criar cliente.");
        sut.IsSaving.Should().BeFalse();
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Post);
        requests[0].Path.Should().Be("/api/clientes");
        requests[0].Body.Should().Contain("\"nome\":\"Cliente Final\"");
        dialogService.Verify(
            service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
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
