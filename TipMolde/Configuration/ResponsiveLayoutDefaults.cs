namespace TipMolde.Configuration;

public enum ResponsiveLayoutMode
{
    Compact,
    Medium,
    Expanded
}

public static class ResponsiveLayoutDefaults
{
    public const double CompactMaxWidth = 900;
    public const double MediumMaxWidth = 1280;

    public const double SidebarCollapsedWidth = 96;
    public const double SidebarExpandedWidth = 280;

    public static ResponsiveLayoutMode ResolveMode(double width)
    {
        if (width <= 0)
            return ResponsiveLayoutMode.Expanded;

        if (width < CompactMaxWidth)
            return ResponsiveLayoutMode.Compact;

        if (width < MediumMaxWidth)
            return ResponsiveLayoutMode.Medium;

        return ResponsiveLayoutMode.Expanded;
    }

    public static Thickness GetPageContentPadding(ResponsiveLayoutMode mode) => mode switch
    {
        ResponsiveLayoutMode.Compact => new Thickness(16, 16, 16, 16),
        ResponsiveLayoutMode.Medium => new Thickness(20, 18, 20, 18),
        _ => new Thickness(28, 22, 28, 20)
    };

    public static Thickness GetTopBarPadding(ResponsiveLayoutMode mode) => mode switch
    {
        ResponsiveLayoutMode.Compact => new Thickness(16, 14),
        ResponsiveLayoutMode.Medium => new Thickness(20, 18),
        _ => new Thickness(28, 22)
    };

    public static Thickness GetTopBarActionsMargin(ResponsiveLayoutMode mode) => mode switch
    {
        ResponsiveLayoutMode.Compact => new Thickness(0, 12, 0, 0),
        _ => Thickness.Zero
    };

    public static Thickness GetTopBarUserBadgePadding(ResponsiveLayoutMode mode) => mode switch
    {
        ResponsiveLayoutMode.Compact => new Thickness(12, 8),
        _ => new Thickness(16, 8)
    };

    public static Thickness GetTopBarLogoutPadding(ResponsiveLayoutMode mode) => mode switch
    {
        ResponsiveLayoutMode.Compact => new Thickness(12, 8),
        _ => new Thickness(18, 10)
    };

    public static double GetTopBarTitleFontSize(ResponsiveLayoutMode mode) => mode switch
    {
        ResponsiveLayoutMode.Compact => 22,
        ResponsiveLayoutMode.Medium => 24,
        _ => 28
    };

    public static double GetTopBarUserNameFontSize(ResponsiveLayoutMode mode) => mode switch
    {
        ResponsiveLayoutMode.Compact => 13,
        _ => 14
    };

    public static double GetTopBarActionFontSize(ResponsiveLayoutMode mode) => mode switch
    {
        ResponsiveLayoutMode.Compact => 12,
        _ => 14
    };

    public static double GetTopBarActionsSpacing(ResponsiveLayoutMode mode) => mode switch
    {
        ResponsiveLayoutMode.Compact => 8,
        _ => 12
    };

    public static double GetSidebarWidth(ResponsiveLayoutMode mode) => mode switch
    {
        ResponsiveLayoutMode.Compact => 0,
        ResponsiveLayoutMode.Medium => SidebarCollapsedWidth,
        _ => SidebarExpandedWidth
    };

    public static bool GetShowSidebar(ResponsiveLayoutMode mode) => mode != ResponsiveLayoutMode.Compact;

    public static bool GetShowSidebarLabels(ResponsiveLayoutMode mode) => mode == ResponsiveLayoutMode.Expanded;

    public static bool GetShowNavigationMenu(ResponsiveLayoutMode mode) => mode == ResponsiveLayoutMode.Compact;
}
