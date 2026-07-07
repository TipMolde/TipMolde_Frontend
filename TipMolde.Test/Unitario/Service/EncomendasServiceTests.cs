using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.Test.Unitario.Service;

/// <summary>
/// Testes unitarios do servico de encomendas do frontend.
/// </summary>
[TestFixture]
[Category("Unit")]
public class EncomendasServiceTests
{
    [Test(Description = "T1FRT - O servico deve devolver encomendas ativas paginadas quando a API responde com sucesso.")]
    public async Task GetEncomendasNaoConcluidasAsync_Should_ReturnPagedItems_When_RequestIsSuccessful()
    {
        // ARRANGE
        var expected = new PagedResult<EncomendaResumoDto>
        {
            Items =
            [
                new EncomendaResumoDto
                {
                    Encomenda_id = 3,
                    NumeroEncomendaCliente = "ENC-003",
                    NomeCliente = "Cliente A"
                }
            ],
            Page = 1,
            PageSize = 10,
            TotalItems = 1
        };

        var httpClient = CreateHttpClient(
            _ => CreateJsonResponse(HttpStatusCode.OK, expected),
            out var requests);
        var sut = new EncomendasService(httpClient);

        // ACT
        var result = await sut.GetEncomendasNaoConcluidasAsync(1, 10);

        // ASSERT
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        result.Items[0].NumeroEncomendaCliente.Should().Be("ENC-003");
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Get);
        requests[0].Path.Should().Be("/api/encomendas/em-producao?page=1&pageSize=10");
    }

    [Test(Description = "T2FRT - A pesquisa de encomendas em producao deve limpar e escapar o termo.")]
    public async Task SearchEncomendasNaoConcluidasAsync_Should_TrimAndEscapeSearchTerm_When_Searching()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => CreateJsonResponse(HttpStatusCode.OK, new PagedResult<EncomendaResumoDto>()),
            out var requests);
        var sut = new EncomendasService(httpClient);

        // ACT
        await sut.SearchEncomendasNaoConcluidasAsync("  Aero Molde  ", 2, 8);

        // ASSERT
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Get);
        requests[0].Path.Should().Be("/api/encomendas/em-producao/search?searchTerm=Aero%20Molde&page=2&pageSize=8");
    }

    [Test(Description = "T3FRT - A atualizacao do estado da encomenda deve usar PATCH com o payload esperado.")]
    public async Task UpdateEstadoAsync_Should_SendPatchRequest_When_UpdatingState()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.NoContent),
            out var requests);
        var sut = new EncomendasService(httpClient);

        // ACT
        await sut.UpdateEstadoAsync(27, "EM_PRODUCAO");

        // ASSERT
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Patch);
        requests[0].Path.Should().Be("/api/encomendas/27/estado");

        using var payload = JsonDocument.Parse(requests[0].Body);
        payload.RootElement.GetProperty("estado").GetString().Should().Be("EM_PRODUCAO");
    }

    [Test(Description = "T4FRT - A associacao encomenda-molde deve enviar quantidade, prioridade e data prevista.")]
    public async Task CreateEncomendaMoldeAsync_Should_PostExpectedPayload_When_RequestIsSuccessful()
    {
        // ARRANGE
        var entrega = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Local);
        var expected = new EncomendaMoldeDto
        {
            EncomendaMolde_id = 12,
            Encomenda_id = 7,
            Molde_id = 19,
            Quantidade = 2,
            Prioridade = 4,
            DataEntregaPrevista = entrega
        };

        var httpClient = CreateHttpClient(
            _ => CreateJsonResponse(HttpStatusCode.Created, expected),
            out var requests);
        var sut = new EncomendasService(httpClient);

        // ACT
        var result = await sut.CreateEncomendaMoldeAsync(7, 19, 2, 4, entrega);

        // ASSERT
        result.Should().NotBeNull();
        result!.EncomendaMolde_id.Should().Be(12);
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Post);
        requests[0].Path.Should().Be("/api/encomenda-moldes");

        using var payload = JsonDocument.Parse(requests[0].Body);
        payload.RootElement.GetProperty("encomenda_id").GetInt32().Should().Be(7);
        payload.RootElement.GetProperty("molde_id").GetInt32().Should().Be(19);
        payload.RootElement.GetProperty("quantidade").GetInt32().Should().Be(2);
        payload.RootElement.GetProperty("prioridade").GetInt32().Should().Be(4);
        payload.RootElement.GetProperty("dataEntregaPrevista").GetDateTime().Date.Should().Be(entrega.Date);
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
