namespace TipMolde.Services
{
    /// <summary>
    /// Resolve o endpoint de desenvolvimento correto para cada plataforma MAUI.
    /// </summary>
    public static class ApiEndpointResolver
    {
        /// <summary>
        /// Devolve a URL base padrao da API tendo em conta a plataforma atual.
        /// </summary>
        /// <returns>Endereco base esperado pelo HttpClient da aplicacao.</returns>
        public static string GetDefaultBaseUrl()
        {
#if ANDROID
            return "http://10.0.2.2:57664/";
#else
            return "http://localhost:57664/";
#endif
        }
    }
}
