using FluentAssertions;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;
using TipMolde.Models;
using TipMolde.Services;

namespace TipMolde.Test.Unitario.Service;

/// <summary>
/// Testes unitarios do servico de utilizadores do frontend.
/// </summary>
/// <remarks>
/// Valida os pedidos HTTP usados para listar, atualizar e remover utilizadores.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class UtilizadoresServiceTests
{
    [Test(Description = "T1FRT - O servico deve devolver um utilizador quando o endpoint responde com sucesso.")]
    public async Task GetUtilizadorByIdAsync_Should_ReturnUtilizador_When_RequestIsSuccessful()
    {
        // ARRANGE
        var expectedUser = BuildUtilizadorDto();
        var httpClient = CreateHttpClient(
            _ => CreateJsonResponse(HttpStatusCode.OK, expectedUser),
            out var requests);
        var sut = new UtilizadoresService(httpClient);

        // ACT
        var result = await sut.GetUtilizadorByIdAsync(expectedUser.User_id);

        // ASSERT
        result.Should().NotBeNull();
        result.User_id.Should().Be(expectedUser.User_id);
        result.Email.Should().Be(expectedUser.Email);
        requests.Should().HaveCount(1);
        requests[0].Method.Should().Be(HttpMethod.Get);
        requests[0].RequestUri!.PathAndQuery.Should().Be($"/api/users/{expectedUser.User_id}");
    }

    [Test(Description = "T2FRT - O servico deve atualizar o cargo do utilizador no endpoint esperado.")]
    public async Task UpdateUtilizadorRoleAsync_Should_PutExpectedPayload_When_RequestIsSuccessful()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.NoContent),
            out var requests);
        var sut = new UtilizadoresService(httpClient);

        // ACT
        await sut.UpdateUtilizadorRoleAsync(22, "GESTOR_DESENHO");

        // ASSERT
        requests.Should().HaveCount(1);
        requests[0].Method.Should().Be(HttpMethod.Put);
        requests[0].RequestUri!.PathAndQuery.Should().Be("/api/users/22/role");

        var requestBody = await requests[0].Content!.ReadAsStringAsync();
        requestBody.Should().Contain("\"role\":\"GESTOR_DESENHO\"");
    }

    [Test(Description = "T3FRT - O servico deve eliminar o utilizador no endpoint esperado.")]
    public async Task DeleteUtilizadorAsync_Should_SendDeleteRequest_When_RequestIsSuccessful()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.NoContent),
            out var requests);
        var sut = new UtilizadoresService(httpClient);

        // ACT
        await sut.DeleteUtilizadorAsync(33);

        // ASSERT
        requests.Should().HaveCount(1);
        requests[0].Method.Should().Be(HttpMethod.Delete);
        requests[0].RequestUri!.PathAndQuery.Should().Be("/api/users/33");
    }

    [Test(Description = "T4FRT - O servico deve devolver erro quando a listagem de utilizadores falha.")]
    public async Task GetUtilizadoresAsync_Should_ThrowInvalidOperationException_When_RequestFails()
    {
        // ARRANGE
        var httpClient = CreateHttpClient(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(
                    """{"detail":"Falha na listagem de utilizadores."}""",
                    Encoding.UTF8,
                    "application/json")
            },
            out _);
        var sut = new UtilizadoresService(httpClient);

        // ACT
        Func<Task> act = () => sut.GetUtilizadoresAsync(1, 10);

        // ASSERT
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Be("Falha na listagem de utilizadores.");
    }

    /// <summary>
    /// Cria um utilizador de teste para validar a desserializacao.
    /// </summary>
    /// <returns>DTO de utilizador pronto para os testes.</returns>
    private static UtilizadorDto BuildUtilizadorDto() => new()
    {
        User_id = 7,
        Nome = "Ana Silva",
        Email = "ana.silva@tipmolde.pt",
        Role = "GESTOR_DESENHO"
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

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_responder(request));
        }
    }
}
