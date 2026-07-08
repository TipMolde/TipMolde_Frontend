using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.Test.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class AutenticacaoServiceTests
{
    [Test(Description = "T1AUTH - O login deve devolver o token e configurar o cabecalho Authorization quando a API responde com sucesso.")]
    public async Task LoginAsync_Should_ReturnTokenAndSetAuthorizationHeader_When_RequestIsSuccessful()
    {
        // ARRANGE
        var expected = new ResponseLoginDto
        {
            Token = "jwt-token-valido",
            ExpiresAt = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero)
        };

        var httpClient = CreateHttpClient(
            _ => CreateJsonResponse(HttpStatusCode.OK, expected),
            out var requests);
        var sut = new AutenticacaoService(httpClient);

        // ACT
        var result = await sut.LoginAsync("admin@tipmolde.pt", "Password123!");

        // ASSERT
        result.Token.Should().Be("jwt-token-valido");
        requests.Should().ContainSingle();
        requests[0].Method.Should().Be(HttpMethod.Post);
        requests[0].Path.Should().Be("/api/auth/login");
        requests[0].Body.Should().Contain("\"email\":\"admin@tipmolde.pt\"");
        requests[0].Body.Should().Contain("\"password\":\"Password123!\"");
        httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        httpClient.DefaultRequestHeaders.Authorization!.Parameter.Should().Be("jwt-token-valido");
    }

    [Test(Description = "T2AUTH - O login deve falhar com erro funcional quando a API devolve unauthorized.")]
    public async Task LoginAsync_Should_ThrowInvalidOperationException_When_ResponseIsUnauthorized()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.Unauthorized),
            out _);
        var sut = new AutenticacaoService(httpClient);

        // ACT
        Func<Task> act = () => sut.LoginAsync("admin@tipmolde.pt", "errada");

        // ASSERT
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("Acesso");
    }

    [Test(Description = "T3AUTH - O login deve falhar quando a API responde sem token valido.")]
    public async Task LoginAsync_Should_ThrowInvalidOperationException_When_TokenIsMissing()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => CreateJsonResponse(
                HttpStatusCode.OK,
                new ResponseLoginDto
                {
                    Token = string.Empty,
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
                }),
            out _);
        var sut = new AutenticacaoService(httpClient);

        // ACT
        Func<Task> act = () => sut.LoginAsync("admin@tipmolde.pt", "Password123!");

        // ASSERT
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Be("A API nao devolveu um token valido.");
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
