using System.ComponentModel;
using TipMolde.Diagnostics;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.View.Shared;

public partial class SidebarView : ContentView
{
    public static readonly BindableProperty ViewModelProperty =
        BindableProperty.Create(
            nameof(ViewModel),
            typeof(SidebarViewModel),
            typeof(SidebarView),
            null);

    private double _sidebarWidth;
    private bool _showLabels;
    private bool _isInitialized;
    private Page? _parentPage;
    private ResponsiveLayoutService? _layoutService;

    public double SidebarWidth
    {
        get => _sidebarWidth;
        set
        {
            _sidebarWidth = value;
            OnPropertyChanged();
        }
    }

    public bool ShowLabels
    {
        get => _showLabels;
        set
        {
            _showLabels = value;
            OnPropertyChanged();
        }
    }

    public SidebarViewModel? ViewModel
    {
        get => (SidebarViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public SidebarView()
    {
        InitializeComponent();
        ApplyFallbackLayout();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void ApplyFallbackLayout()
    {
        IsVisible = false;
        ShowLabels = false;
        SidebarWidth = 0;
    }

    private void OnHeaderTapped(object sender, TappedEventArgs e)
    {
        if (_layoutService is null || !_layoutService.ShowSidebar)
            return;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        TaskMonitor.Observe("TipMolde.View.Shared.SidebarView.OnLoaded", OnLoadedAsync());
    }

    private async Task OnLoadedAsync()
    {
        EnsureLayoutService();
        ApplyResponsiveLayout();

        if (Handler?.MauiContext?.Services.GetService(typeof(SidebarViewModel)) is not SidebarViewModel vm)
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
        TaskMonitor.Observe("TipMolde.View.Shared.SidebarView.OnParentPageAppearing", OnParentPageAppearingAsync());
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
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.ShowSidebar), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.ShowSidebarLabels), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.SidebarWidth), StringComparison.Ordinal))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(ApplyResponsiveLayout);
    }

    private void ApplyResponsiveLayout()
    {
        if (_layoutService is null)
        {
            ApplyFallbackLayout();
            return;
        }

        IsVisible = _layoutService.ShowSidebar;
        ShowLabels = _layoutService.ShowSidebarLabels;
        SidebarWidth = _layoutService.SidebarWidth;
    }
}
