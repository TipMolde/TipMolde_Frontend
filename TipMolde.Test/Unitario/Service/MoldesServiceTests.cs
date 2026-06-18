using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.Test.Unitario.Service;

/// <summary>
/// Testes unitarios do servico de moldes do frontend.
/// </summary>
/// <remarks>
/// Valida chamadas HTTP, serializacao e tratamento de erros do servico.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class MoldesServiceTests
{
    [Test(Description = "T1FRT - O servico deve devolver um molde quando o endpoint responde com sucesso.")]
    public async Task GetByIdAsync_Should_ReturnMolde_When_RequestIsSuccessful()
    {
        // ARRANGE
        var expectedMolde = BuildMoldeDto();
        var httpClient = CreateHttpClient(
            _ => CreateJsonResponse(HttpStatusCode.OK, expectedMolde),
            out var requests);
        var sut = new MoldesService(httpClient);

        // ACT
        var result = await sut.GetByIdAsync(expectedMolde.MoldeId);

        // ASSERT
        result.Should().NotBeNull();
        result!.MoldeId.Should().Be(expectedMolde.MoldeId);
        result.Numero.Should().Be(expectedMolde.Numero);
        requests.Should().HaveCount(1);
        requests[0].Method.Should().Be(HttpMethod.Get);
        requests[0].RequestUri!.PathAndQuery.Should().Be($"/api/moldes/{expectedMolde.MoldeId}");
    }

    [Test(Description = "T2FRT - O servico deve devolver null quando o endpoint de molde falha.")]
    public async Task GetByIdAsync_Should_ReturnNull_When_RequestFails()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            out _);
        var sut = new MoldesService(httpClient);

        // ACT
        var result = await sut.GetByIdAsync(999);

        // ASSERT
        result.Should().BeNull();
    }

    [Test(Description = "T3FRT - O servico deve criar um molde com o payload esperado.")]
    public async Task CreateAsync_Should_PostExpectedPayload_When_RequestIsSuccessful()
    {
        // ARRANGE
        var expectedMolde = BuildMoldeDto();
        var httpClient = CreateHttpClient(
            request => CreateJsonResponse(HttpStatusCode.OK, expectedMolde),
            out var requests);
        var sut = new MoldesService(httpClient);

        // ACT
        var result = await sut.CreateAsync(
            expectedMolde.Numero,
            expectedMolde.NumeroMoldeCliente,
            expectedMolde.Nome,
            expectedMolde.Descricao,
            expectedMolde.Numero_cavidades,
            expectedMolde.TipoPedido,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        // ASSERT
        result.Should().NotBeNull();
        requests.Should().HaveCount(1);
        requests[0].Method.Should().Be(HttpMethod.Post);
        requests[0].RequestUri!.PathAndQuery.Should().Be("/api/moldes");
        requests[0].Content!.Headers.ContentType!.MediaType.Should().Be("multipart/form-data");

        var requestBody = requests[0].Properties.TryGetValue("Body", out var bodyValue)
            ? bodyValue as string ?? string.Empty
            : string.Empty;
        requestBody.Should().Contain("Content-Disposition: form-data; name=Numero");
        requestBody.Should().Contain(expectedMolde.Numero);
        requestBody.Should().Contain("Content-Disposition: form-data; name=NumeroMoldeCliente");
        requestBody.Should().Contain(expectedMolde.NumeroMoldeCliente);
        requestBody.Should().Contain("Content-Disposition: form-data; name=Nome");
        requestBody.Should().Contain(expectedMolde.Nome);
        requestBody.Should().Contain("Content-Disposition: form-data; name=Descricao");
        requestBody.Should().Contain(expectedMolde.Descricao);
        requestBody.Should().Contain("Content-Disposition: form-data; name=Numero_cavidades");
        requestBody.Should().Contain(expectedMolde.Numero_cavidades.ToString());
        requestBody.Should().Contain("Content-Disposition: form-data; name=TipoPedido");
        requestBody.Should().Contain(expectedMolde.TipoPedido);
    }

    [Test(Description = "T3BFR - O servico deve enviar a imagem de capa ao criar um molde.")]
    public async Task CreateAsync_Should_PostImage_When_ImageFileExists()
    {
        // ARRANGE
        var expectedMolde = BuildMoldeDto();
        var imagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        await File.WriteAllTextAsync(imagePath, "fake-image");

        try
        {
            var httpClient = CreateHttpClient(
                _ => CreateJsonResponse(HttpStatusCode.OK, expectedMolde),
                out var requests);
            var sut = new MoldesService(httpClient);

            // ACT
            var result = await sut.CreateAsync(
                expectedMolde.Numero,
                expectedMolde.NumeroMoldeCliente,
                expectedMolde.Nome,
                expectedMolde.Descricao,
                expectedMolde.Numero_cavidades,
                expectedMolde.TipoPedido,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                imagePath);

            // ASSERT
            result.Should().NotBeNull();
            requests.Should().HaveCount(1);
            requests[0].Method.Should().Be(HttpMethod.Post);
            requests[0].RequestUri!.PathAndQuery.Should().Be("/api/moldes");
            requests[0].Content!.Headers.ContentType!.MediaType.Should().Be("multipart/form-data");

            var requestBody = requests[0].Properties.TryGetValue("Body", out var bodyValue)
                ? bodyValue as string ?? string.Empty
                : string.Empty;
            requestBody.Should().Contain($"filename={Path.GetFileName(imagePath)}");
            requestBody.Should().Contain("Content-Type: image/png");
        }
        finally
        {
            File.Delete(imagePath);
        }
    }

    [Test(Description = "T4FRT - O servico deve devolver o erro do backend quando a criacao do molde falha.")]
    public async Task CreateAsync_Should_ThrowInvalidOperationException_When_RequestFails()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"detail":"Nao foi possivel criar o molde."}""",
                    Encoding.UTF8,
                    "application/json")
            },
            out _);
        var sut = new MoldesService(httpClient);

        // ACT
        Func<Task> act = () => sut.CreateAsync(
            "M-001",
            "MC-001",
            "Molde de teste",
            "Descricao de teste",
            2,
            "Normal",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        // ASSERT
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Be("Nao foi possivel criar o molde.");
    }

    [Test(Description = "T5FRT - O servico deve enviar a imagem de capa ao atualizar um molde.")]
    public async Task UpdateImagemCapaAsync_Should_PostExpectedMultipart_When_FileExists()
    {
        // ARRANGE
        var expectedMolde = BuildMoldeDto();
        var imagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        await File.WriteAllTextAsync(imagePath, "fake-image");

        try
        {
            var httpClient = CreateHttpClient(
                _ => CreateJsonResponse(HttpStatusCode.OK, expectedMolde),
                out var requests);
            var sut = new MoldesService(httpClient);

            // ACT
            var result = await sut.UpdateImagemCapaAsync(31, imagePath);

            // ASSERT
            result.Should().NotBeNull();
            requests.Should().HaveCount(1);
            requests[0].Method.Should().Be(HttpMethod.Post);
            requests[0].RequestUri!.PathAndQuery.Should().Be("/api/moldes/31/imagem-capa");
            requests[0].Content!.Headers.ContentType!.MediaType.Should().Be("multipart/form-data");

            var requestBody = requests[0].Properties.TryGetValue("Body", out var bodyValue)
                ? bodyValue as string ?? string.Empty
                : string.Empty;
            requestBody.Should().Contain($"filename={Path.GetFileName(imagePath)}");
            requestBody.Should().Contain("Content-Type: image/png");
        }
        finally
        {
            File.Delete(imagePath);
        }
    }

    /// <summary>
    /// Cria um molde de teste para validar serializacao e resposta do servico.
    /// </summary>
    /// <returns>DTO de molde pronto para uso nos testes.</returns>
    private static MoldeDto BuildMoldeDto() => new()
    {
        MoldeId = 10,
        Numero = "M-010",
        NumeroMoldeCliente = "CL-010",
        Nome = "Molde de teste",
        ImagemCapaPath = "Storage/Uploads/capa.png",
        Descricao = "Descricao de teste",
        Numero_cavidades = 2,
        TipoPedido = "Normal"
    };

    /// <summary>
    /// Cria um cliente HTTP controlado para validar as chamadas do servico.
    /// </summary>
    /// <param name="responder">Funcao responsavel por devolver a resposta simulada.</param>
    /// <param name="requests">Lista de pedidos capturados durante o teste.</param>
    /// <returns>Cliente HTTP configurado com o handler de teste.</returns>
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

    /// <summary>
    /// Cria uma resposta JSON para simular o backend.
    /// </summary>
    /// <param name="statusCode">Codigo HTTP a devolver.</param>
    /// <param name="value">Objeto a serializar no corpo da resposta.</param>
    /// <returns>Resposta HTTP simulada.</returns>
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

    /// <summary>
    /// Handler HTTP controlado para registar pedidos e devolver respostas predefinidas.
    /// </summary>
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
            request.Properties["Body"] = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _responder(request);
        }
    }
}
