using TipMolde.Services;

namespace TipMolde.Helper;

public static class MoldeImageSourceHelper
{
    public const string FallbackSource = "tipmolde_default.jpg";
    private const string BackendDefaultTemplateSource = "Templates/tipmolde_default.jpg";

    public static string Resolve(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return FallbackSource;

        var normalizedPath = imagePath.Trim().Replace('\\', '/');
        if (string.Equals(normalizedPath, BackendDefaultTemplateSource, StringComparison.OrdinalIgnoreCase))
            return FallbackSource;

        if (Uri.TryCreate(normalizedPath, UriKind.Absolute, out _) || Path.IsPathRooted(normalizedPath))
            return normalizedPath;

        return BuildBackendImageUrl(normalizedPath);
    }

    private static string BuildBackendImageUrl(string relativePath)
    {
        var baseUrl = ApiEndpointResolver.Resolve().BaseUrl.TrimEnd('/');
        return $"{baseUrl}/uploads/{relativePath}";
    }
}
