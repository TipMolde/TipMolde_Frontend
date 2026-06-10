namespace TipMolde.Helper;

public static class MoldeImageSourceHelper
{
    public const string FallbackSource = "image.png";

    public static string Resolve(string? imagePath)
    {
        return string.IsNullOrWhiteSpace(imagePath)
            ? FallbackSource
            : imagePath.Trim();
    }
}
