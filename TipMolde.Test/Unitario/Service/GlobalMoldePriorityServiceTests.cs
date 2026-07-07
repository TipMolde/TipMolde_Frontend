using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.Test.Unitario.Service;

/// <summary>
/// Testes unitarios do servico de prioridade global dos moldes.
/// </summary>
[TestFixture]
[Category("Unit")]
public class GlobalMoldePriorityServiceTests
{
    [Test(Description = "T1FRT - O calculo de prioridades deve integrar moldes em rascunho com encomendas abertas.")]
    public async Task CalculateDraftPrioritiesAsync_Should_MergeDraftsWithOpenOrders()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(HandlePriorityRequests, out _);
        var sut = new GlobalMoldePriorityService(new EncomendasService(httpClient));
        var drafts = new[]
        {
            new GlobalMoldeDraftPriorityItem("draft-1", 0, 501, new DateTime(2026, 7, 9), "M-501"),
            new GlobalMoldeDraftPriorityItem("draft-2", 0, 502, new DateTime(2026, 7, 13), "M-502")
        };

        // ACT
        var result = await sut.CalculateDraftPrioritiesAsync(drafts);

        // ASSERT
        result.Should().HaveCount(2);
        result["draft-1"].Should().Be(1);
        result["draft-2"].Should().Be(5);
    }

    [Test(Description = "T2FRT - O rebalanceamento deve atualizar apenas as prioridades que realmente mudaram.")]
    public async Task RebalanceAsync_Should_UpdateOnlyChangedPriorities()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(HandlePriorityRequests, out var requests);
        var sut = new GlobalMoldePriorityService(new EncomendasService(httpClient));

        // ACT
        await sut.RebalanceAsync();

        // ASSERT
        var updateRequests = requests
            .Where(request => request.Method == HttpMethod.Put && request.Path.StartsWith("/api/encomenda-moldes/", StringComparison.Ordinal))
            .ToList();

        updateRequests.Should().ContainSingle();
        updateRequests[0].Path.Should().Be("/api/encomenda-moldes/200");
        updateRequests[0].Body.Should().Contain("\"prioridade\":2");
    }

    [Test(Description = "T3FRT - A recolha de moldes abertos deve consolidar varias paginas de encomendas e moldes.")]
    public async Task GetOpenEncomendaMoldesAsync_Should_AggregateItemsAcrossPages()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(HandlePriorityRequests, out _);
        var sut = new GlobalMoldePriorityService(new EncomendasService(httpClient));

        // ACT
        var result = await sut.GetOpenEncomendaMoldesAsync();

        // ASSERT
        result.Should().HaveCount(3);
        result.Select(item => item.EncomendaMoldeId).Should().Contain(100).And.Contain(200).And.Contain(300);
        result.Select(item => item.NumeroEncomendaCliente).Should().Contain("ENC-100").And.Contain("ENC-200");
    }

    [Test(Description = "T4FRT - Rascunhos sem chave valida nao devem produzir prioridades calculadas.")]
    public async Task CalculateDraftPrioritiesAsync_Should_IgnoreDraftsWithoutValidKey()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(HandlePriorityRequests, out _);
        var sut = new GlobalMoldePriorityService(new EncomendasService(httpClient));

        // ACT
        var result = await sut.CalculateDraftPrioritiesAsync(
            [
                new GlobalMoldeDraftPriorityItem(string.Empty, 0, 1, new DateTime(2026, 7, 10), "M-1"),
                new GlobalMoldeDraftPriorityItem(" ", 0, 2, new DateTime(2026, 7, 11), "M-2")
            ]);

        // ASSERT
        result.Should().BeEmpty();
    }

    private static HttpResponseMessage HandlePriorityRequests(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery switch
        {
            "/api/encomendas/em-producao?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<EncomendaResumoDto>
                {
                    Items =
                    [
                        new EncomendaResumoDto { Encomenda_id = 10, NumeroEncomendaCliente = "ENC-100", Estado = "EM_PRODUCAO" }
                    ],
                    Page = 1,
                    PageSize = 1,
                    TotalItems = 2
                }),
            "/api/encomendas/em-producao?page=2&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<EncomendaResumoDto>
                {
                    Items =
                    [
                        new EncomendaResumoDto { Encomenda_id = 20, NumeroEncomendaCliente = "ENC-200", Estado = "EM_PRODUCAO" }
                    ],
                    Page = 2,
                    PageSize = 1,
                    TotalItems = 2
                }),
            "/api/encomenda-moldes/por-encomenda/10?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<EncomendaMoldeDto>
                {
                    Items =
                    [
                        new EncomendaMoldeDto
                        {
                            EncomendaMolde_id = 100,
                            Encomenda_id = 10,
                            Molde_id = 1000,
                            Quantidade = 1,
                            Prioridade = 1,
                            DataEntregaPrevista = new DateTime(2026, 7, 10),
                            NumeroMolde = "M-1000"
                        },
                        new EncomendaMoldeDto
                        {
                            EncomendaMolde_id = 200,
                            Encomenda_id = 10,
                            Molde_id = 2000,
                            Quantidade = 1,
                            Prioridade = 5,
                            DataEntregaPrevista = new DateTime(2026, 7, 11),
                            NumeroMolde = "M-2000"
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 2
                }),
            "/api/encomenda-moldes/por-encomenda/20?page=1&pageSize=100" => CreateJsonResponse(
                HttpStatusCode.OK,
                new PagedResult<EncomendaMoldeDto>
                {
                    Items =
                    [
                        new EncomendaMoldeDto
                        {
                            EncomendaMolde_id = 300,
                            Encomenda_id = 20,
                            Molde_id = 3000,
                            Quantidade = 1,
                            Prioridade = 3,
                            DataEntregaPrevista = new DateTime(2026, 7, 12),
                            NumeroMolde = "M-3000"
                        }
                    ],
                    Page = 1,
                    PageSize = 100,
                    TotalItems = 1
                }),
            "/api/encomenda-moldes/200" when request.Method == HttpMethod.Put => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            },
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
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
