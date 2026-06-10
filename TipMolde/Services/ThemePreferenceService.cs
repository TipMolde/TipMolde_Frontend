using Microsoft.Maui.Storage;

namespace TipMolde.Services;

public sealed class ThemePreferenceService
{
    private const string ThemePreferenceKey = "preferred_app_theme";

    public const string SystemThemeOption = "Seguir sistema";
    public const string LightThemeOption = "Claro";
    public const string DarkThemeOption = "Escuro";

    public IReadOnlyList<string> AvailableThemes { get; } = new[]
    {
        SystemThemeOption,
        LightThemeOption,
        DarkThemeOption
    };

    public string GetStoredTheme()
    {
        var storedTheme = Preferences.Default.Get(ThemePreferenceKey, SystemThemeOption);
        return NormalizeThemeOption(storedTheme);
    }

    public void ApplyStoredTheme()
    {
        ApplyTheme(GetStoredTheme());
    }

    public void ApplyTheme(string themeOption)
    {
        var normalizedTheme = NormalizeThemeOption(themeOption);

        Preferences.Default.Set(ThemePreferenceKey, normalizedTheme);

        if (Application.Current is null)
            return;

        Application.Current.UserAppTheme = normalizedTheme switch
        {
            LightThemeOption => AppTheme.Light,
            DarkThemeOption => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    private static string NormalizeThemeOption(string? themeOption)
    {
        return themeOption switch
        {
            LightThemeOption => LightThemeOption,
            DarkThemeOption => DarkThemeOption,
            _ => SystemThemeOption
        };
    }
}
