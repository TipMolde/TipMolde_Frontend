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
public class AdicionarMoldeViewModelTests
{
    [Test(Description = "TADDMOL1 - O carregamento inicial deve preencher as opcoes e selecionar valores por omissao.")]
    public async Task LoadAsync_Should_LoadDefaultOptions()
    {
        var sut = new AdicionarMoldeViewModel(
            new MoldesService(CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound), out _)),
            Mock.Of<IDialogService>(),
            Mock.Of<IFilePickerService>());

        await sut.LoadAsync();

        sut.TipoPedidoOptions.Should().HaveCount(3);
        sut.CorOptions.Should().HaveCount(3);
        sut.SelectedTipoPedidoOption.Should().NotBeNull();
        sut.SelectedTipoPedidoOption!.Value.Should().Be("NOVO_MOLDE");
        sut.SelectedCorOption.Should().NotBeNull();
        sut.SelectedCorOption!.DisplayName.Should().Be("Monocolor");
        sut.ErrorMessage.Should().BeEmpty();
    }

    [Test(Description = "TADDMOL2 - O formulario deve bloquear a criacao quando uma medida decimal nao e valida.")]
    public async Task CreateCommand_Should_SetValidationError_When_DecimalIsInvalid()
    {
        var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound), out var requests);
        var sut = new AdicionarMoldeViewModel(
            new MoldesService(httpClient),
            Mock.Of<IDialogService>(),
            Mock.Of<IFilePickerService>());

        await sut.LoadAsync();
        sut.Numero = "M-100";
        sut.Nome = "Molde Teste";
        sut.Largura = "abc";

        await sut.CreateCommand.ExecuteAsync(null);

        sut.ErrorMessage.Should().Be("A largura nao e valida.");
        sut.IsSaving.Should().BeFalse();
        requests.Should().BeEmpty();
    }

    [Test(Description = "TADDMOL3 - Se o seletor de ficheiros falhar, o frontend deve mostrar erro funcional e manter a imagem vazia.")]
    public async Task EscolherImagemCapaCommand_Should_SetError_When_FilePickerThrows()
    {
        var filePicker = new Mock<IFilePickerService>();
        filePicker
            .Setup(service => service.PickAsync(It.IsAny<PickOptions>()))
            .ThrowsAsync(new InvalidOperationException("Picker indisponivel."));

        var sut = new AdicionarMoldeViewModel(
            new MoldesService(CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound), out _)),
            Mock.Of<IDialogService>(),
            filePicker.Object);

        await sut.EscolherImagemCapaCommand.ExecuteAsync(null);

        sut.ErrorMessage.Should().Be("Nao foi possivel selecionar a imagem de capa. Picker indisponivel.");
        sut.ImagemCapaPath.Should().BeEmpty();
        sut.HasImagemCapaSelecionada.Should().BeFalse();
    }

    [Test(Description = "TADDMOL4 - Quando o backend falha na criacao, o frontend deve expor o erro e enviar os dados normalizados.")]
    public async Task CreateCommand_Should_SurfaceApiErrorAndTrimPayload_When_ServiceFails()
    {
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"detail":"Falha ao criar molde."}""",
                    Encoding.UTF8,
                    "application/json")
            },
            out var requests);

        var dialogService = new Mock<IDialogService>();
        var sut = new AdicionarMoldeViewModel(
            new MoldesService(httpClient),
            dialogService.Object,
            Mock.Of<IFilePickerService>());

        await sut.LoadAsync();
        sut.Numero = "  M-200  ";
        sut.NumeroMoldeCliente = "  C-200  ";
        sut.Nome = "  Molde Comercial  ";
        sut.Descricao = "  Descricao  ";

        await sut.CreateCommand.ExecuteAsync(null);

        sut.ErrorMessage.Should().Be("Falha ao criar molde.");
        sut.IsSaving.Should().BeFalse();
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Post);
        requests[0].Path.Should().Be("/api/moldes");
        requests[0].Body.Should().Contain("name=Numero");
        requests[0].Body.Should().Contain("M-200");
        requests[0].Body.Should().Contain("name=NumeroMoldeCliente");
        requests[0].Body.Should().Contain("C-200");
        requests[0].Body.Should().Contain("name=Nome");
        requests[0].Body.Should().Contain("Molde Comercial");
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
