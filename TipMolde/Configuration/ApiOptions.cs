namespace TipMolde.Configuration
{
    /// <summary>
    /// Representa a configuracao minima do endpoint base usado pelo frontend.
    /// </summary>
    public sealed class ApiOptions
    {
        /// <summary>
        /// URL base da API consumida pela aplicacao MAUI.
        /// </summary>
        public string BaseUrl { get; init; } = string.Empty;

        /// <summary>
        /// Ambiente configurado para a aplicacao.
        /// </summary>
        public string EnvironmentName { get; init; } = string.Empty;

        /// <summary>
        /// Origem usada para resolver o endpoint final.
        /// </summary>
        public string ConfigurationSource { get; init; } = string.Empty;
    }
}
