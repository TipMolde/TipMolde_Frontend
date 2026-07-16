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
public class EditarClienteViewModelTests
{
    [Test(Description = "TEDCLI1 - O carregamento deve preencher o formulario com os dados atuais do cliente.")]
    public async Task LoadAsync_Should_PopulateFields_When_ClientExists()
    {
        var sut = new EditarClienteViewModel(
            new ClientesService(CreateHttpClient(request =>
            {
                if (request.RequestUri?.PathAndQuery == "/api/clientes/9")
                {
                    return CreateJsonResponse(
                        HttpStatusCode.OK,
                        new ClienteDto
                        {
                            Cliente_id = 9,
                            Nome = "Cliente Norte",
                            NIF = "509999999",
                            Sigla = "CN",
                            Pais = "Portugal",
                            Email = "norte@tipmolde.pt",
                            Telefone = "910100100"
                        });
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }, out _)),
            Mock.Of<IDialogService>());

        await sut.LoadAsync(9);

        sut.Cliente_id.Should().Be(9);
        sut.Nome.Should().Be("Cliente Norte");
        sut.Nif.Should().Be("509999999");
        sut.Sigla.Should().Be("CN");
        sut.Pais.Should().Be("Portugal");
        sut.Email.Should().Be("norte@tipmolde.pt");
        sut.Telefone.Should().Be("910100100");
        sut.ErrorMessage.Should().BeEmpty();
    }

    [Test(Description = "TEDCLI2 - Quando o cliente nao existe, o frontend deve mostrar erro e manter o formulario vazio.")]
    public async Task LoadAsync_Should_SetError_When_ClientDoesNotExist()
    {
        var sut = new EditarClienteViewModel(
            new ClientesService(CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound), out _)),
            Mock.Of<IDialogService>());

        await sut.LoadAsync(404);

        sut.ErrorMessage.Should().Be("Nao foi possivel carregar o cliente para edicao.");
        sut.HasError.Should().BeTrue();
        sut.Nome.Should().BeEmpty();
    }

    [Test(Description = "TEDCLI3 - O formulario deve bloquear a gravacao quando o email nao e valido.")]
    public async Task SaveCommand_Should_SetValidationError_When_EmailIsInvalid()
    {
        var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound), out var requests);
        var sut = new EditarClienteViewModel(new ClientesService(httpClient), Mock.Of<IDialogService>())
        {
            Cliente_id = 9,
            Nome = "Cliente Norte",
            Nif = "509999999",
            Sigla = "CN",
            Email = "email-invalido"
        };

        await sut.SaveCommand.ExecuteAsync(null);

        sut.ErrorMessage.Should().Be("O email introduzido não é válido.");
        sut.IsSaving.Should().BeFalse();
        requests.Should().BeEmpty();
    }

    [Test(Description = "TEDCLI4 - Se a API falhar na atualizacao, o frontend deve expor o erro e enviar os dados normalizados.")]
    public async Task SaveCommand_Should_SurfaceApiErrorAndTrimPayload_When_ServiceFails()
    {
        var httpClient = CreateHttpClient(
            request =>
            {
                if (request.RequestUri?.PathAndQuery == "/api/clientes/9" && request.Method == HttpMethod.Put)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(
                            """{"detail":"Falha ao atualizar cliente."}""",
                            Encoding.UTF8,
                            "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            },
            out var requests);

        var dialogService = new Mock<IDialogService>();
        var sut = new EditarClienteViewModel(new ClientesService(httpClient), dialogService.Object)
        {
            Cliente_id = 9,
            Nome = "  Cliente Norte  ",
            Nif = "509999999",
            Sigla = "  CN  ",
            Pais = "  Portugal  ",
            Email = "  norte@tipmolde.pt  ",
            Telefone = "  910100100  "
        };

        await sut.SaveCommand.ExecuteAsync(null);

        sut.ErrorMessage.Should().Be("Falha ao atualizar cliente.");
        sut.IsSaving.Should().BeFalse();
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Put);
        requests[0].Path.Should().Be("/api/clientes/9");
        requests[0].Body.Should().Contain("\"nome\":\"Cliente Norte\"");
        requests[0].Body.Should().Contain("\"sigla\":\"CN\"");
        requests[0].Body.Should().Contain("\"pais\":\"Portugal\"");
        requests[0].Body.Should().Contain("\"email\":\"norte@tipmolde.pt\"");
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

    private static HttpResponseMessage CreateJsonResponse<T>(HttpStatusCode statusCode, T payload)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
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
