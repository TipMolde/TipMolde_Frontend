using System.ComponentModel;
using TipMolde.Diagnostics;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.View.Shared;

public partial class TopBarView : ContentView
{
    public static readonly BindableProperty TitleTextProperty =
        BindableProperty.Create(
            nameof(TitleText),
            typeof(string),
            typeof(TopBarView),
            string.Empty);

    public static readonly BindableProperty ViewModelProperty =
        BindableProperty.Create(
            nameof(ViewModel),
            typeof(TopBarViewModel),
            typeof(TopBarView),
            null);

    private bool _isInitialized;
    private Page? _parentPage;
    private ResponsiveLayoutService? _layoutService;
    private bool _showNavigationMenu;
    private Thickness _rootPadding = new(28, 22);
    private Thickness _actionsMargin = Thickness.Zero;
    private Thickness _userBadgePadding = new(16, 8);
    private Thickness _logoutPadding = new(18, 10);
    private double _titleFontSize = 28;
    private double _userNameFontSize = 14;
    private double _actionFontSize = 14;
    private double _actionsSpacing = 12;

    public string TitleText
    {
        get => (string)GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public TopBarViewModel? ViewModel
    {
        get => (TopBarViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public bool ShowNavigationMenu
    {
        get => _showNavigationMenu;
        set
        {
            _showNavigationMenu = value;
            OnPropertyChanged();
        }
    }

    public Thickness RootPadding
    {
        get => _rootPadding;
        set
        {
            _rootPadding = value;
            OnPropertyChanged();
        }
    }

    public Thickness ActionsMargin
    {
        get => _actionsMargin;
        set
        {
            _actionsMargin = value;
            OnPropertyChanged();
        }
    }

    public Thickness UserBadgePadding
    {
        get => _userBadgePadding;
        set
        {
            _userBadgePadding = value;
            OnPropertyChanged();
        }
    }

    public Thickness LogoutPadding
    {
        get => _logoutPadding;
        set
        {
            _logoutPadding = value;
            OnPropertyChanged();
        }
    }

    public double TitleFontSize
    {
        get => _titleFontSize;
        set
        {
            _titleFontSize = value;
            OnPropertyChanged();
        }
    }

    public double UserNameFontSize
    {
        get => _userNameFontSize;
        set
        {
            _userNameFontSize = value;
            OnPropertyChanged();
        }
    }

    public double ActionFontSize
    {
        get => _actionFontSize;
        set
        {
            _actionFontSize = value;
            OnPropertyChanged();
        }
    }

    public double ActionsSpacing
    {
        get => _actionsSpacing;
        set
        {
            _actionsSpacing = value;
            OnPropertyChanged();
        }
    }

    public TopBarView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        TaskMonitor.Observe("TipMolde.View.Shared.TopBarView.OnLoaded", OnLoadedAsync());
    }

    private async Task OnLoadedAsync()
    {
        EnsureLayoutService();
        ApplyResponsiveLayout();

        if (Handler?.MauiContext?.Services.GetService(typeof(TopBarViewModel)) is not TopBarViewModel vm)
            return;

        ViewModel = vm;

        if (!_isInitialized)
        {
            AttachToParentPage();
            _isInitialized = true;
        }

        await vm.EnsureLoadedAsync(forceRefresh: true);
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (_layoutService is not null)
            _layoutService.PropertyChanged -= OnLayoutServicePropertyChanged;

        if (_parentPage is not null)
            _parentPage.Appearing -= OnParentPageAppearing;

        _layoutService = null;
        _parentPage = null;
        _isInitialized = false;
    }

    private void AttachToParentPage()
    {
        _parentPage = FindParentPage();
        if (_parentPage is not null)
            _parentPage.Appearing += OnParentPageAppearing;
    }

    private void OnParentPageAppearing(object? sender, EventArgs e)
    {
        TaskMonitor.Observe("TipMolde.View.Shared.TopBarView.OnParentPageAppearing", OnParentPageAppearingAsync());
    }

    private async Task OnParentPageAppearingAsync()
    {
        if (ViewModel is null)
            return;

        await ViewModel.EnsureLoadedAsync(forceRefresh: true);
    }

    private Page? FindParentPage()
    {
        Element? current = Parent;
        while (current is not null)
        {
            if (current is Page page)
                return page;

            current = current.Parent;
        }

        return null;
    }

    private void EnsureLayoutService()
    {
        if (_layoutService is not null)
            return;

        _layoutService = Handler?.MauiContext?.Services.GetService(typeof(ResponsiveLayoutService)) as ResponsiveLayoutService;
        if (_layoutService is not null)
            _layoutService.PropertyChanged += OnLayoutServicePropertyChanged;
    }

    private void OnLayoutServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.PropertyName) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.LayoutMode), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.ShowNavigationMenu), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.TopBarPadding), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.TopBarActionsMargin), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.TopBarUserBadgePadding), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.TopBarLogoutPadding), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.TopBarTitleFontSize), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.TopBarUserNameFontSize), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.TopBarActionFontSize), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.TopBarActionsSpacing), StringComparison.Ordinal))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(ApplyResponsiveLayout);
    }

    private void ApplyResponsiveLayout()
    {
        if (_layoutService is null)
            return;

        ShowNavigationMenu = _layoutService.ShowNavigationMenu;
        RootPadding = _layoutService.TopBarPadding;
        ActionsMargin = _layoutService.TopBarActionsMargin;
        UserBadgePadding = _layoutService.TopBarUserBadgePadding;
        LogoutPadding = _layoutService.TopBarLogoutPadding;
        TitleFontSize = _layoutService.TopBarTitleFontSize;
        UserNameFontSize = _layoutService.TopBarUserNameFontSize;
        ActionFontSize = _layoutService.TopBarActionFontSize;
        ActionsSpacing = _layoutService.TopBarActionsSpacing;
    }
}
