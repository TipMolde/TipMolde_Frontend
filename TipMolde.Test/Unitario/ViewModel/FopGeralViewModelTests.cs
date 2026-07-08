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
public class FopGeralViewModelTests
{
    [Test(Description = "TFOPVM1 - O carregamento deve rejeitar intervalos invalidos antes de chamar a API.")]
    public async Task LoadAsync_Should_SetErrorAndClearRows_When_DateRangeIsInvalid()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound), out var requests);
        var sut = new FopGeralViewModel(
            new RelatoriosService(httpClient),
            Mock.Of<IDestinationFolderPickerService>(),
            Mock.Of<IDialogService>())
        {
            DataInicio = new DateTime(2026, 7, 10),
            DataFim = new DateTime(2026, 7, 1)
        };

        // ACT
        await sut.LoadAsync();

        // ASSERT
        sut.ErrorMessage.Should().Be("A data final tem de ser igual ou posterior a data inicial.");
        sut.Linhas.Should().BeEmpty();
        sut.TotalPages.Should().Be(1);
        requests.Should().BeEmpty();
    }

    [Test(Description = "TFOPVM2 - O carregamento deve preencher a primeira pagina da FOP geral quando a API responde com sucesso.")]
    public async Task LoadAsync_Should_LoadPagedRows_When_ServiceReturnsData()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/fichas-producao/fop-geral?dataInicio=2026-07-01&dataFim=2026-07-31&page=1&pageSize=20")
            {
                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    new PagedResult<FopGeralLinhaDto>
                    {
                        Items = new List<FopGeralLinhaDto>
                        {
                            new()
                            {
                                FichaFopLinha_id = 1,
                                Ocorrencia = "Ocorrencia A",
                                ResponsavelNome = "Ana",
                                Data = new DateTime(2026, 7, 3, 10, 0, 0)
                            }
                        },
                        Page = 1,
                        PageSize = 20,
                        TotalItems = 1
                    });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }, out var requests);

        var sut = new FopGeralViewModel(
            new RelatoriosService(httpClient),
            Mock.Of<IDestinationFolderPickerService>(),
            Mock.Of<IDialogService>())
        {
            DataInicio = new DateTime(2026, 7, 1),
            DataFim = new DateTime(2026, 7, 31)
        };

        // ACT
        await sut.LoadAsync();

        // ASSERT
        sut.Linhas.Should().ContainSingle();
        sut.Linhas[0].Ocorrencia.Should().Be("Ocorrencia A");
        sut.HasLinhas.Should().BeTrue();
        requests.Should().ContainSingle(request => request.Path == "/api/fichas-producao/fop-geral?dataInicio=2026-07-01&dataFim=2026-07-31&page=1&pageSize=20");
    }

    [Test(Description = "TFOPVM3 - A exportacao deve mostrar erro funcional e repor o estado quando o backend falha.")]
    public async Task ExportarExcelCommand_Should_ShowErrorAndResetFlag_When_ServiceFails()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(request =>
        {
            if (request.RequestUri?.PathAndQuery == "/api/fichas-producao/fop-geral/export?dataInicio=2026-07-01&dataFim=2026-07-31")
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(
                        """{"detail":"Falha ao gerar FOP."}""",
                        Encoding.UTF8,
                        "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }, out _);

        var folderPicker = new Mock<IDestinationFolderPickerService>();
        folderPicker
            .Setup(service => service.PickFolderAsync(It.IsAny<string>()))
            .ReturnsAsync("C:\\Temp\\Relatorios");

        var dialogService = new Mock<IDialogService>();
        dialogService.Setup(service => service.ShowErrorAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = new FopGeralViewModel(
            new RelatoriosService(httpClient),
            folderPicker.Object,
            dialogService.Object)
        {
            DataInicio = new DateTime(2026, 7, 1),
            DataFim = new DateTime(2026, 7, 31)
        };

        // ACT
        await sut.ExportarExcelCommand.ExecuteAsync(null);

        // ASSERT
        sut.ErrorMessage.Should().Be("Falha ao gerar FOP.");
        sut.IsExportingExcel.Should().BeFalse();
        dialogService.Verify(service => service.ShowErrorAsync("Erro", "Falha ao gerar FOP."), Times.Once);
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
