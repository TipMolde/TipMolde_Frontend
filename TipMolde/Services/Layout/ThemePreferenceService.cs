namespace TipMolde.Services;

/// <summary>
/// Gere a preferencia de tema visual escolhida pelo utilizador.
/// </summary>
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

    /// <summary>
    /// Obtem o tema persistido localmente, normalizado para uma opcao suportada.
    /// </summary>
    /// <returns>Opcao de tema valida para a aplicacao.</returns>
    public static string GetStoredTheme()
    {
        var storedTheme = Preferences.Default.Get(ThemePreferenceKey, SystemThemeOption);
        return NormalizeThemeOption(storedTheme);
    }

    /// <summary>
    /// Aplica o tema persistido sem exigir interacao adicional da UI.
    /// </summary>
    public static void ApplyStoredTheme()
    {
        ApplyTheme(GetStoredTheme());
    }

    /// <summary>
    /// Persiste e aplica o tema pedido ao estado visual atual da aplicacao.
    /// </summary>
    /// <param name="themeOption">Opcao funcional de tema escolhida pelo utilizador.</param>
    public static void ApplyTheme(string themeOption)
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
