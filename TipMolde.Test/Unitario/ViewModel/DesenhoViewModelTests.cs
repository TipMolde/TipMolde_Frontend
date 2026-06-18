using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using TipMolde.Models;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

/// <summary>
/// Testes unitarios do ViewModel de desenho do frontend.
/// </summary>
/// <remarks>
/// Valida a filtragem de moldes confirmados sem projetos associados e com pecas existentes.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class DesenhoViewModelTests
{
    private Mock<IDialogService> _dialogService = null!;
    private DesenhoViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dialogService = new Mock<IDialogService>();

        var httpClient = CreateHttpClient(HandleRequest, out _);

        _sut = new DesenhoViewModel(
            new EncomendasService(httpClient),
            new PecasService(httpClient),
            _dialogService.Object);
    }

    [Test(Description = "T1FRT - O ViewModel deve mostrar apenas moldes aptos para desenho e nao deve bloquear moldes sem pecas.")]
    public async Task LoadAsync_Should_ShowOnlyMoldesEligibleForDrawing()
    {
        // ACT
        await _sut.LoadAsync();

        // ASSERT
        _sut.Moldes.Should().HaveCount(2);

        var moldeComPecas = _sut.Moldes.First(item => item.MoldeId == 101);
        moldeComPecas.TotalPecas.Should().Be(2);
        moldeComPecas.PecasResumoDisplayText.Should().Contain("Base");
        moldeComPecas.PecasResumoDisplayText.Should().Contain("Tampa");

        var moldeSemPecas = _sut.Moldes.First(item => item.MoldeId == 103);
        moldeSemPecas.TotalPecas.Should().Be(0);
        moldeSemPecas.PecasResumoDisplayText.Should().Contain("Sem detalhes");

        _sut.TotalMoldes.Should().Be(2);
        _sut.TotalEncomendasConfirmadas.Should().Be(2);
    }

    private static HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/encomenda-moldes/encomendas-confirmadas-para-desenho?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<EncomendaMoldeDto>
                {
                    Items =
                    [
                        new EncomendaMoldeDto
                        {
                            EncomendaMolde_id = 11,
                            Encomenda_id = 21,
                            Molde_id = 101,
                            Quantidade = 120,
                            Prioridade = 1,
                            DataEntregaPrevista = new DateTime(2026, 6, 18),
                            Estado = "CONFIRMADA",
                            NumeroEncomendaCliente = "ENC-101",
                            NumeroMolde = "M-101"
                        },
                        new EncomendaMoldeDto
                        {
                            EncomendaMolde_id = 13,
                            Encomenda_id = 23,
                            Molde_id = 103,
                            Quantidade = 60,
                            Prioridade = 3,
                            DataEntregaPrevista = new DateTime(2026, 6, 20),
                            Estado = "CONFIRMADA",
                            NumeroEncomendaCliente = "ENC-103",
                            NumeroMolde = "M-103"
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 2
                }),
            "/api/pecas/por-molde/101?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items =
                    [
                        new PecaDto
                        {
                            PecaId = 1,
                            NumeroPeca = "P-001",
                            Designacao = "Base",
                            Quantidade = 20,
                            Molde_id = 101
                        },
                        new PecaDto
                        {
                            PecaId = 2,
                            NumeroPeca = "P-002",
                            Designacao = "Tampa",
                            Quantidade = 30,
                            Molde_id = 101
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 2
                }),
            "/api/pecas/por-molde/103?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<PecaDto>
                {
                    Items = [],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 0
                }),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        out List<HttpRequestMessage> requests)
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

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_responder(request));
        }
    }
}
