using System.Globalization;

namespace TipMolde.Helper;

public static class QueryAttributeHelper
{
    public static int? ParseInt(object? rawValue, bool allowUriDecoding = false)
    {
        if (rawValue is int intValue)
            return intValue;

        if (rawValue is not string text)
            return null;

        if (allowUriDecoding)
        {
            var decodedText = Uri.UnescapeDataString(text);
            if (int.TryParse(decodedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var decodedValue))
                return decodedValue;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
