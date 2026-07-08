using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.Test.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class FornecedoresServiceTests
{
    [Test(Description = "T1FOR - O servico deve devolver fornecedores paginados quando a API responde com sucesso.")]
    public async Task GetAllAsync_Should_ReturnPagedFornecedores_When_RequestIsSuccessful()
    {
        // ARRANGE
        var expected = new PagedResult<FornecedorDto>
        {
            Items =
            [
                new FornecedorDto
                {
                    FornecedorId = 3,
                    Nome = "Fornecedor A",
                    NIF = "123456789"
                }
            ],
            Page = 1,
            PageSize = 10,
            TotalItems = 1
        };

        var httpClient = CreateHttpClient(
            _ => CreateJsonResponse(HttpStatusCode.OK, expected),
            out var requests);
        var sut = new FornecedoresService(httpClient);

        // ACT
        var result = await sut.GetAllAsync(1, 10);

        // ASSERT
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Get);
        requests[0].Path.Should().Be("/api/fornecedores?page=1&pageSize=10");
    }

    [Test(Description = "T2FOR - A criacao de fornecedor deve enviar o payload esperado para a API.")]
    public async Task CreateAsync_Should_PostExpectedPayload_When_RequestIsSuccessful()
    {
        // ARRANGE
        var expected = new FornecedorDto
        {
            FornecedorId = 9,
            Nome = "Fornecedor Novo",
            NIF = "123456789",
            Morada = "Rua X",
            Email = "fornecedor@tipmolde.pt",
            Telefone = "+351912345678"
        };

        var httpClient = CreateHttpClient(
            _ => CreateJsonResponse(HttpStatusCode.Created, expected),
            out var requests);
        var sut = new FornecedoresService(httpClient);

        // ACT
        var result = await sut.CreateAsync("Fornecedor Novo", "123456789", "Rua X", "fornecedor@tipmolde.pt", "+351912345678");

        // ASSERT
        result.Should().NotBeNull();
        result!.FornecedorId.Should().Be(9);
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Post);
        requests[0].Path.Should().Be("/api/fornecedores");
        requests[0].Body.Should().Contain("\"nome\":\"Fornecedor Novo\"");
        requests[0].Body.Should().Contain("\"nif\":\"123456789\"");
    }

    [Test(Description = "T3FOR - A eliminacao de fornecedor deve usar o endpoint DELETE esperado.")]
    public async Task DeleteAsync_Should_SendDeleteRequest_When_RequestIsSuccessful()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.NoContent),
            out var requests);
        var sut = new FornecedoresService(httpClient);

        // ACT
        await sut.DeleteAsync(21);

        // ASSERT
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Delete);
        requests[0].Path.Should().Be("/api/fornecedores/21");
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
