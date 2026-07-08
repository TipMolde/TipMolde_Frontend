using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;
using TipMolde.Services;

namespace TipMolde.Test.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class RevisoesServiceTests
{
    private static readonly HttpRequestOptionsKey<string> BodyOptionKey = new("RecordedBody");

    [Test(Description = "T1FRT - O servico deve enviar feedback textual e imagem no payload da resposta do cliente.")]
    public async Task UpdateRespostaClienteAsync_Should_PostFeedbackImagemPath_When_Provided()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.NoContent),
            out var requests);
        var sut = new RevisoesService(httpClient);

        // ACT
        await sut.UpdateRespostaClienteAsync(
            42,
            false,
            "Desenho precisa de ajuste",
            @"Storage\Uploads\feedback\rev-42.png");

        // ASSERT
        requests.Should().HaveCount(1);
        requests[0].Method.Should().Be(HttpMethod.Put);
        requests[0].RequestUri!.PathAndQuery.Should().Be("/api/revisoes/42/resposta-cliente");

        var requestBody = GetRecordedBody(requests[0]);
        requestBody.Should().Contain("\"aprovado\":false");
        requestBody.Should().Contain("\"feedbackTexto\":\"Desenho precisa de ajuste\"");
        requestBody.Should().Contain("\"feedbackImagemPath\":\"Storage\\\\Uploads\\\\feedback\\\\rev-42.png\"");
    }

    [Test(Description = "T2FRT - O servico deve enviar multipart quando existe anexo numa revisao reprovada.")]
    public async Task UpdateRespostaClienteComAnexoAsync_Should_PostMultipart_When_AttachmentIsProvided()
    {
        // ARRANGE
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3, 4 });

        try
        {
            var httpClient = CreateHttpClient(
                _ => new HttpResponseMessage(HttpStatusCode.NoContent),
                out var requests);
            var sut = new RevisoesService(httpClient);

            // ACT
            await sut.UpdateRespostaClienteComAnexoAsync(
                42,
                false,
                "Desenho precisa de ajuste",
                tempFile);

            // ASSERT
            requests.Should().HaveCount(1);
            requests[0].Method.Should().Be(HttpMethod.Put);
            requests[0].RequestUri!.PathAndQuery.Should().Be("/api/revisoes/42/resposta-cliente");
            requests[0].Content!.Headers.ContentType!.MediaType.Should().Be("multipart/form-data");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
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

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            request.Options.Set(
                BodyOptionKey,
                request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken));

            return _responder(request);
        }
    }

    private static string GetRecordedBody(HttpRequestMessage request)
    {
        return request.Options.TryGetValue(BodyOptionKey, out string? body)
            ? body ?? string.Empty
            : string.Empty;
    }
}
