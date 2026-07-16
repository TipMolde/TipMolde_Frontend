using System.Reflection;
using System.Text.Json;
using TipMolde.Configuration;

namespace TipMolde.Services
{
    /// <summary>
    /// Resolve o endpoint da API a partir de configuracao por ambiente,
    /// com fallback seguro por plataforma.
    /// </summary>
    public static class ApiEndpointResolver
    {
        private const string EnvironmentVariableName = "TIPMOLDE_ENVIRONMENT";
        private const string BaseUrlVariableName = "TIPMOLDE_API_BASE_URL";
        private const string DefaultEnvironmentName = "Production";

        /// <summary>
        /// Resolve a configuracao final do endpoint da API para a plataforma atual.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Resolve o ambiente corrente.
        /// 2. Carrega configuracao embebida base e por ambiente.
        /// 3. Da prioridade a variaveis de ambiente explicitas.
        /// 4. So permite fallback local controlado em desenvolvimento.
        /// </remarks>
        /// <returns>Opcoes finais da API prontas para injeccao no frontend.</returns>
        public static ApiOptions Resolve()
        {
            var environmentName = ResolveEnvironmentName();
            var platformKey = GetPlatformKey();
            var configuredSettings = LoadMergedSettings(environmentName);
            var environmentBaseUrl = NormalizeUrl(Environment.GetEnvironmentVariable(BaseUrlVariableName));

            if (!string.IsNullOrWhiteSpace(environmentBaseUrl))
            {
                return new ApiOptions
                {
                    BaseUrl = environmentBaseUrl,
                    EnvironmentName = environmentName,
                    ConfigurationSource = $"variavel de ambiente {BaseUrlVariableName}"
                };
            }

            var configuredBaseUrl = GetConfiguredBaseUrl(configuredSettings, platformKey);
            if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                EnsureConfiguredUrlIsAllowed(configuredBaseUrl, environmentName);

                return new ApiOptions
                {
                    BaseUrl = configuredBaseUrl,
                    EnvironmentName = environmentName,
                    ConfigurationSource = $"configuracao embebida ({environmentName})"
                };
            }

            if (!IsLocalDevelopmentAllowed(environmentName))
            {
                throw new InvalidOperationException(
                    $"Nao foi possivel resolver o endpoint da API para o ambiente '{environmentName}'. Defina a configuracao embebida ou a variavel {BaseUrlVariableName}.");
            }

            return new ApiOptions
            {
                BaseUrl = GetPlatformFallbackBaseUrl(),
                EnvironmentName = environmentName,
                ConfigurationSource = "fallback por plataforma"
            };
        }

        private static string ResolveEnvironmentName()
        {
            var configuredEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (!string.IsNullOrWhiteSpace(configuredEnvironment))
                return configuredEnvironment.Trim();

#if DEBUG
            return "Development";
#else
            return DefaultEnvironmentName;
#endif
        }

        private static ApiSettings LoadMergedSettings(string environmentName)
        {
            var baseSettings = LoadSettingsResource("appsettings.json");
            var environmentSettings = string.Equals(environmentName, DefaultEnvironmentName, StringComparison.OrdinalIgnoreCase)
                ? null
                : LoadSettingsResource($"appsettings.{environmentName}.json");

            return Merge(baseSettings, environmentSettings);
        }

        private static ApiSettings LoadSettingsResource(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{assembly.GetName().Name}.Configuration.{fileName}";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                return new ApiSettings();

            using var document = JsonDocument.Parse(stream);

            if (!document.RootElement.TryGetProperty("Api", out var apiElement))
                return new ApiSettings();

            var settings = new ApiSettings
            {
                BaseUrl = apiElement.TryGetProperty("BaseUrl", out var baseUrlElement)
                    ? NormalizeUrl(baseUrlElement.GetString())
                    : null
            };

            if (apiElement.TryGetProperty("Platforms", out var platformsElement) &&
                platformsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in platformsElement.EnumerateObject())
                {
                    var value = NormalizeUrl(property.Value.GetString());
                    if (!string.IsNullOrWhiteSpace(value))
                        settings.Platforms[property.Name] = value;
                }
            }

            return settings;
        }

        private static ApiSettings Merge(ApiSettings baseSettings, ApiSettings? overrideSettings)
        {
            var merged = new ApiSettings
            {
                BaseUrl = overrideSettings?.BaseUrl ?? baseSettings.BaseUrl
            };

            foreach (var pair in baseSettings.Platforms)
                merged.Platforms[pair.Key] = pair.Value;

            if (overrideSettings is not null)
            {
                foreach (var pair in overrideSettings.Platforms)
                    merged.Platforms[pair.Key] = pair.Value;
            }

            return merged;
        }

        private static string? GetConfiguredBaseUrl(ApiSettings settings, string platformKey)
        {
            if (settings.Platforms.TryGetValue(platformKey, out var platformUrl) &&
                !string.IsNullOrWhiteSpace(platformUrl))
            {
                return platformUrl;
            }

            return settings.BaseUrl;
        }

        private static string GetPlatformKey()
        {
            var platform = DeviceInfo.Current.Platform;

            if (platform == DevicePlatform.Android)
                return "android";

            if (platform == DevicePlatform.iOS)
                return "ios";

            if (platform == DevicePlatform.MacCatalyst)
                return "maccatalyst";

            if (platform == DevicePlatform.WinUI)
                return "windows";

            return "default";
        }

        private static string GetPlatformFallbackBaseUrl()
        {
            return DeviceInfo.Current.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:8080/"
                : "http://localhost:8080/";
        }

        private static void EnsureConfiguredUrlIsAllowed(string configuredBaseUrl, string environmentName)
        {
            if (IsLocalDevelopmentAllowed(environmentName))
                return;

            if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var uri) &&
                IsLocalDevelopmentHost(uri.Host))
            {
                throw new InvalidOperationException(
                    $"O endpoint configurado '{configuredBaseUrl}' so pode ser usado em DEBUG e desenvolvimento local controlado.");
            }
        }

        private static bool IsLocalDevelopmentAllowed(string environmentName)
        {
#if DEBUG
            return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
#else
            _ = environmentName;
            return false;
#endif
        }

        private static bool IsLocalDevelopmentHost(string host)
        {
            return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(host, "10.0.2.2", StringComparison.OrdinalIgnoreCase);
        }

        private static string? NormalizeUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var normalized = value.Trim();
            return normalized.EndsWith('/')
                ? normalized
                : $"{normalized}/";
        }

        private sealed class ApiSettings
        {
            public string? BaseUrl { get; init; }

            public Dictionary<string, string> Platforms { get; } = new(StringComparer.OrdinalIgnoreCase);
        }
    }
}
