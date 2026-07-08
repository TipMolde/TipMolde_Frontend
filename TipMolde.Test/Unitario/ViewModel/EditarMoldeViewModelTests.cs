using FluentAssertions;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TipMolde.Domain.Enums;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

[TestFixture]
[Category("Unit")]
public class EditarMoldeViewModelTests
{
    [Test(Description = "TEDMOL1 - Um utilizador sem permissao de administrador nao deve conseguir carregar o formulario de edicao.")]
    public async Task LoadAsync_Should_SetError_When_UserIsNotAdmin()
    {
        var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound), out var requests);
        var authorizationService = CreateAuthorizationService(httpClient, "GESTOR_COMERCIAL", 7);
        var sut = new EditarMoldeViewModel(
            new MoldesService(httpClient),
            Mock.Of<IDialogService>(),
            authorizationService,
            Mock.Of<IFilePickerService>());

        await sut.LoadAsync(55);

        sut.ErrorMessage.Should().Be("Apenas o administrador pode editar moldes.");
        sut.IsLoadingData.Should().BeFalse();
        requests.Should().BeEmpty();
    }

    [Test(Description = "TEDMOL2 - O carregamento deve preencher o formulario e selecionar os valores correspondentes do molde.")]
    public async Task LoadAsync_Should_PopulateFields_When_UserIsAdminAndMoldeExists()
    {
        var httpClient = CreateHttpClient(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/moldes/55")
            {
                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    new MoldeDto
                    {
                        MoldeId = 55,
                        Numero = "M-055",
                        NumeroMoldeCliente = "CL-055",
                        Nome = "Molde Critico",
                        ImagemCapaPath = "C:\\imagens\\molde.png",
                        Descricao = "Descricao",
                        Numero_cavidades = 4,
                        TipoPedido = "ALTERACAO",
                        Largura = 10.5m,
                        Comprimento = 20m,
                        Altura = 30m,
                        PesoEstimado = 40.5m,
                        TipoInjecao = "Canal quente",
                        SistemaInjecao = "Sequencial",
                        Contracao = 1.2m,
                        AcabamentoPeca = "Polido",
                        Cor = CorMolde.BICOLOR,
                        MaterialMacho = "Aco 1",
                        MaterialCavidade = "Aco 2",
                        MaterialMovimentos = "Aco 3",
                        MaterialInjecao = "ABS"
                    });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }, out _);

        var sut = new EditarMoldeViewModel(
            new MoldesService(httpClient),
            Mock.Of<IDialogService>(),
            CreateAuthorizationService(httpClient, "ADMIN", 1),
            Mock.Of<IFilePickerService>());

        await sut.LoadAsync(55);

        sut.MoldeId.Should().Be(55);
        sut.Numero.Should().Be("M-055");
        sut.Nome.Should().Be("Molde Critico");
        sut.NumeroCavidades.Should().Be(4);
        sut.SelectedTipoPedidoOption.Should().NotBeNull();
        sut.SelectedTipoPedidoOption!.Value.Should().Be("ALTERACAO");
        sut.SelectedCorOption.Should().NotBeNull();
        sut.SelectedCorOption!.Value.Should().Be(CorMolde.BICOLOR);
        sut.HasImagemCapaSelecionada.Should().BeTrue();
        sut.CanSave.Should().BeTrue();
        sut.ErrorMessage.Should().BeEmpty();
    }

    [Test(Description = "TEDMOL3 - O formulario deve bloquear a gravacao quando uma medida decimal nao e valida.")]
    public async Task GuardarCommand_Should_SetValidationError_When_DecimalIsInvalid()
    {
        var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound), out var requests);
        var sut = new EditarMoldeViewModel(
            new MoldesService(httpClient),
            Mock.Of<IDialogService>(),
            CreateAuthorizationService(httpClient, "ADMIN", 1),
            Mock.Of<IFilePickerService>())
        {
            MoldeId = 55,
            Numero = "M-055",
            Nome = "Molde Critico",
            SelectedTipoPedidoOption = new TipoPedidoOption("NOVO_MOLDE", "Novo Molde"),
            Largura = "abc"
        };

        await sut.GuardarCommand.ExecuteAsync(null);

        sut.ErrorMessage.Should().Be("A largura nao e valida.");
        sut.IsSaving.Should().BeFalse();
        requests.Should().BeEmpty();
    }

    [Test(Description = "TEDMOL4 - Se a API falhar na atualizacao, o frontend deve expor o erro e enviar os dados normalizados.")]
    public async Task GuardarCommand_Should_SurfaceApiErrorAndTrimPayload_When_ServiceFails()
    {
        var httpClient = CreateHttpClient(
            request =>
            {
                if (request.RequestUri?.PathAndQuery == "/api/moldes/55" && request.Method == HttpMethod.Put)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(
                            """{"detail":"Falha ao atualizar molde."}""",
                            Encoding.UTF8,
                            "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            },
            out var requests);

        var dialogService = new Mock<IDialogService>();
        var sut = new EditarMoldeViewModel(
            new MoldesService(httpClient),
            dialogService.Object,
            CreateAuthorizationService(httpClient, "ADMIN", 1),
            Mock.Of<IFilePickerService>())
        {
            MoldeId = 55,
            Numero = "  M-055  ",
            NumeroMoldeCliente = "  CL-055  ",
            Nome = "  Molde Critico  ",
            Descricao = "  Descricao  ",
            NumeroCavidades = 4,
            SelectedTipoPedidoOption = new TipoPedidoOption("REPARACAO", "Reparacao"),
            SelectedCorOption = new CorMoldeOption(CorMolde.OUTRO, "Outro")
        };

        await sut.GuardarCommand.ExecuteAsync(null);

        sut.ErrorMessage.Should().Be("Falha ao atualizar molde.");
        sut.IsSaving.Should().BeFalse();
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Put);
        requests[0].Path.Should().Be("/api/moldes/55");
        requests[0].Body.Should().Contain("\"numero\":\"M-055\"");
        requests[0].Body.Should().Contain("\"numeroMoldeCliente\":\"CL-055\"");
        requests[0].Body.Should().Contain("\"nome\":\"Molde Critico\"");
        requests[0].Body.Should().Contain("\"tipoPedido\":\"REPARACAO\"");
        dialogService.Verify(service => service.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static AuthorizationService CreateAuthorizationService(HttpClient httpClient, string role, int userId)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwtToken(userId));

        var authorizationService = new AuthorizationService(
            new SessaoPersistidaService(httpClient),
            new UtilizadoresService(httpClient));

        typeof(AuthorizationService)
            .GetField("_cachedRole", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(authorizationService, role);

        typeof(AuthorizationService)
            .GetField("_cachedUserId", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(authorizationService, userId);

        return authorizationService;
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
