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
    }
}
