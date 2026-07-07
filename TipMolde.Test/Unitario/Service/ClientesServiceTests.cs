using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.Test.Unitario.Service;

/// <summary>
/// Testes unitarios do servico de clientes do frontend.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ClientesServiceTests
{
    [Test(Description = "T1FRT - O servico deve devolver clientes paginados quando a API responde com sucesso.")]
    public async Task GetClientesAsync_Should_ReturnPagedClientes_When_RequestIsSuccessful()
    {
        // ARRANGE
        var expected = new PagedResult<ClienteDto>
        {
            Items =
            [
                new ClienteDto
                {
                    Cliente_id = 4,
                    Nome = "TipMolde",
                    NIF = "123456789",
                    Sigla = "TM"
                }
            ],
            Page = 1,
            PageSize = 10,
            TotalItems = 1
        };

        var httpClient = CreateHttpClient(
            _ => CreateJsonResponse(HttpStatusCode.OK, expected),
            out var requests);
        var sut = new ClientesService(httpClient);

        // ACT
        var result = await sut.GetClientesAsync(1, 10);

        // ASSERT
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        result.Items[0].Nome.Should().Be("TipMolde");
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Get);
        requests[0].Path.Should().Be("/api/clientes?page=1&pageSize=10");
    }

    [Test(Description = "T2FRT - A pesquisa por nome deve escapar o termo e chamar o endpoint correto.")]
    public async Task SearchByNameAsync_Should_UseEscapedQueryString_When_SearchTermContainsSpaces()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => CreateJsonResponse(HttpStatusCode.OK, new PagedResult<ClienteDto>()),
            out var requests);
        var sut = new ClientesService(httpClient);

        // ACT
        await sut.SearchByNameAsync("Molde Base", 2, 5);

        // ASSERT
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Get);
        requests[0].Path.Should().Be("/api/clientes/search/by-name?searchTerm=Molde%20Base&page=2&pageSize=5");
    }

    [Test(Description = "T3FRT - A criacao de cliente deve enviar o payload esperado para a API.")]
    public async Task CreateAsync_Should_PostExpectedPayload_When_DataIsValid()
    {
        // ARRANGE
        var expected = new ClienteDto
        {
            Cliente_id = 9,
            Nome = "Cliente Novo",
            NIF = "123456789",
            Sigla = "CN"
        };

        var httpClient = CreateHttpClient(
            _ => CreateJsonResponse(HttpStatusCode.Created, expected),
            out var requests);
        var sut = new ClientesService(httpClient);

        // ACT
        var result = await sut.CreateAsync(
            "Cliente Novo",
            "123456789",
            "CN",
            "Portugal",
            "cliente@tipmolde.pt",
            "+351912345678");

        // ASSERT
        result.Should().NotBeNull();
        result!.Cliente_id.Should().Be(9);
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Post);
        requests[0].Path.Should().Be("/api/clientes");

        using var payload = JsonDocument.Parse(requests[0].Body);
        payload.RootElement.GetProperty("nome").GetString().Should().Be("Cliente Novo");
        payload.RootElement.GetProperty("nif").GetString().Should().Be("123456789");
        payload.RootElement.GetProperty("sigla").GetString().Should().Be("CN");
        payload.RootElement.GetProperty("pais").GetString().Should().Be("Portugal");
    }

    [Test(Description = "T4FRT - A eliminacao de cliente deve usar o endpoint DELETE esperado.")]
    public async Task DeleteAsync_Should_SendDeleteRequest_When_RequestIsSuccessful()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.NoContent),
            out var requests);
        var sut = new ClientesService(httpClient);

        // ACT
        await sut.DeleteAsync(15);

        // ASSERT
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Delete);
        requests[0].Path.Should().Be("/api/clientes/15");
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
