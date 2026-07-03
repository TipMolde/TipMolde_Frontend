using CommunityToolkit.Mvvm.ComponentModel;
using TipMolde.Configuration;

namespace TipMolde.Services;

public partial class ResponsiveLayoutService : ObservableObject
{
    [ObservableProperty]
    private double windowWidth;

    [ObservableProperty]
    private ResponsiveLayoutMode layoutMode = ResponsiveLayoutMode.Expanded;

    public bool IsCompact => LayoutMode == ResponsiveLayoutMode.Compact;
    public bool IsMedium => LayoutMode == ResponsiveLayoutMode.Medium;
    public bool IsExpanded => LayoutMode == ResponsiveLayoutMode.Expanded;

    public bool ShowSidebar => ResponsiveLayoutDefaults.GetShowSidebar(LayoutMode);
    public bool ShowSidebarLabels => ResponsiveLayoutDefaults.GetShowSidebarLabels(LayoutMode);
    public double SidebarWidth => ResponsiveLayoutDefaults.GetSidebarWidth(LayoutMode);
    public bool ShowNavigationMenu => ResponsiveLayoutDefaults.GetShowNavigationMenu(LayoutMode);
    public Thickness PageContentPadding => ResponsiveLayoutDefaults.GetPageContentPadding(LayoutMode);
    public Thickness TopBarPadding => ResponsiveLayoutDefaults.GetTopBarPadding(LayoutMode);
    public Thickness TopBarActionsMargin => ResponsiveLayoutDefaults.GetTopBarActionsMargin(LayoutMode);
    public Thickness TopBarUserBadgePadding => ResponsiveLayoutDefaults.GetTopBarUserBadgePadding(LayoutMode);
    public Thickness TopBarLogoutPadding => ResponsiveLayoutDefaults.GetTopBarLogoutPadding(LayoutMode);
    public double TopBarTitleFontSize => ResponsiveLayoutDefaults.GetTopBarTitleFontSize(LayoutMode);
    public double TopBarUserNameFontSize => ResponsiveLayoutDefaults.GetTopBarUserNameFontSize(LayoutMode);
    public double TopBarActionFontSize => ResponsiveLayoutDefaults.GetTopBarActionFontSize(LayoutMode);
    public double TopBarActionsSpacing => ResponsiveLayoutDefaults.GetTopBarActionsSpacing(LayoutMode);
    public double DashboardPlanificacaoCellSize => LayoutMode switch
    {
        ResponsiveLayoutMode.Compact => 96,
        ResponsiveLayoutMode.Medium => 112,
        _ => 132
    };

    public void UpdateWidth(double width)
    {
        if (width <= 0)
            return;

        WindowWidth = width;

        var nextMode = ResponsiveLayoutDefaults.ResolveMode(width);
        if (LayoutMode != nextMode)
            LayoutMode = nextMode;
    }

    partial void OnLayoutModeChanged(ResponsiveLayoutMode value)
    {
        OnPropertyChanged(nameof(IsCompact));
        OnPropertyChanged(nameof(IsMedium));
        OnPropertyChanged(nameof(IsExpanded));
        OnPropertyChanged(nameof(ShowSidebar));
        OnPropertyChanged(nameof(ShowSidebarLabels));
        OnPropertyChanged(nameof(SidebarWidth));
        OnPropertyChanged(nameof(ShowNavigationMenu));
        OnPropertyChanged(nameof(PageContentPadding));
        OnPropertyChanged(nameof(TopBarPadding));
        OnPropertyChanged(nameof(TopBarActionsMargin));
        OnPropertyChanged(nameof(TopBarUserBadgePadding));
        OnPropertyChanged(nameof(TopBarLogoutPadding));
        OnPropertyChanged(nameof(TopBarTitleFontSize));
        OnPropertyChanged(nameof(TopBarUserNameFontSize));
        OnPropertyChanged(nameof(TopBarActionFontSize));
        OnPropertyChanged(nameof(TopBarActionsSpacing));
        OnPropertyChanged(nameof(DashboardPlanificacaoCellSize));
    }
}
