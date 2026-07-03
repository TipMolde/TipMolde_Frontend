using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TipMolde.Services
{
    /// <summary>
    /// Encapsula um teste simples de conectividade com a API.
    /// </summary>
    public sealed class ApiConnectivityService
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Inicializa o servico com o cliente HTTP configurado para a API.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP com a BaseAddress da API resolvida por plataforma.</param>
        public ApiConnectivityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// URL base atualmente usada para contactar a API.
        /// </summary>
        public string BaseUrl => _httpClient.BaseAddress?.ToString() ?? string.Empty;

        /// <summary>
        /// Chama o endpoint de health da API para validar ligacao e disponibilidade.
        /// </summary>
        /// <param name="cancellationToken">Token opcional de cancelamento.</param>
        /// <returns>Resultado enriquecido com estado de sucesso e detalhe operacional.</returns>
        public async Task<ApiConnectionResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.GetAsync("api/health", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiConnectionResult.Failure(
                        BaseUrl,
                        $"A API respondeu com codigo {(int)response.StatusCode}.");
                }

                var payload = await response.Content.ReadFromJsonAsync<HealthResponse>(
                    cancellationToken: cancellationToken);

                var status = payload?.Status ?? "desconhecido";

                return ApiConnectionResult.Success(
                    BaseUrl,
                    $"API respondeu com estado '{status}'.",
                    payload?.TimestampUtc);
            }
            catch (Exception ex)
            {
                return ApiConnectionResult.Failure(
                    BaseUrl,
                    $"Erro ao contactar a API: {ex.Message}");
            }
        }

        private sealed record HealthResponse(
            [property: JsonPropertyName("status")] string Status,
            [property: JsonPropertyName("timestampUtc")] DateTimeOffset TimestampUtc);
    }

    /// <summary>
    /// Resultado da tentativa de ligacao ao backend.
    /// </summary>
    /// <param name="IsSuccess">Indica se a API respondeu com sucesso.</param>
    /// <param name="BaseUrl">URL base usada na tentativa.</param>
    /// <param name="Message">Descricao resumida do resultado.</param>
    /// <param name="TimestampUtc">Instante UTC devolvido pela API quando disponivel.</param>
    public sealed record ApiConnectionResult(
        bool IsSuccess,
        string BaseUrl,
        string Message,
        DateTimeOffset? TimestampUtc)
    {
        /// <summary>
        /// Cria um resultado de sucesso.
        /// </summary>
        public static ApiConnectionResult Success(
            string baseUrl,
            string message,
            DateTimeOffset? timestampUtc) =>
            new(true, baseUrl, message, timestampUtc);

        /// <summary>
        /// Cria um resultado de falha.
        /// </summary>
        public static ApiConnectionResult Failure(
            string baseUrl,
            string message) =>
            new(false, baseUrl, message, null);
    }
}
