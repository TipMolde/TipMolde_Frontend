using System.ComponentModel;
using TipMolde.Diagnostics;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;
    private readonly ResponsiveLayoutService _responsiveLayoutService;

    public DashboardPage(
        DashboardViewModel vm,
        ResponsiveLayoutService responsiveLayoutService)
    {
        _viewModel = vm;
        _responsiveLayoutService = responsiveLayoutService;

        InitializeComponent();
        BindingContext = _viewModel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TaskMonitor.Observe("TipMolde.View.DashboardPage.OnAppearing", OnAppearingAsync());
    }

    private async Task OnAppearingAsync()
    {
        await _viewModel.LoadAsync();
    }

    public Thickness ResponsiveContentPadding => _responsiveLayoutService.PageContentPadding;
    public bool IsCompactLayout => _responsiveLayoutService.IsCompact;
    public bool IsWideLayout => !_responsiveLayoutService.IsCompact;
    public int AdaptiveSecondaryColumn => IsCompactLayout ? 0 : 1;
    public int AdaptiveSecondaryRow => IsCompactLayout ? 1 : 0;
    public int SummaryCardColumnSpan => IsCompactLayout ? 2 : 1;
    public int SummarySecondColumn => IsCompactLayout ? 0 : 1;
    public int SummarySecondRow => 0;
    public int SummaryThirdColumn => 0;
    public int SummaryThirdRow => IsCompactLayout ? 2 : 1;
    public int SummaryFourthColumn => IsCompactLayout ? 0 : 1;
    public int SummaryFourthRow => IsCompactLayout ? 3 : 1;

    private void OnLoaded(object? sender, EventArgs e)
    {
        _responsiveLayoutService.PropertyChanged += OnResponsiveLayoutChanged;
        ApplyResponsiveLayout();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _responsiveLayoutService.PropertyChanged -= OnResponsiveLayoutChanged;
    }

    private void OnResponsiveLayoutChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.PropertyName) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.LayoutMode), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(ResponsiveLayoutService.PageContentPadding), StringComparison.Ordinal))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(ApplyResponsiveLayout);
    }

    private void ApplyResponsiveLayout()
    {
        OnPropertyChanged(nameof(ResponsiveContentPadding));
        OnPropertyChanged(nameof(IsCompactLayout));
        OnPropertyChanged(nameof(IsWideLayout));
        OnPropertyChanged(nameof(AdaptiveSecondaryColumn));
        OnPropertyChanged(nameof(AdaptiveSecondaryRow));
        OnPropertyChanged(nameof(SummaryCardColumnSpan));
        OnPropertyChanged(nameof(SummarySecondColumn));
        OnPropertyChanged(nameof(SummarySecondRow));
        OnPropertyChanged(nameof(SummaryThirdColumn));
        OnPropertyChanged(nameof(SummaryThirdRow));
        OnPropertyChanged(nameof(SummaryFourthColumn));
        OnPropertyChanged(nameof(SummaryFourthRow));
        _viewModel.PlanificacaoCellSize = _responsiveLayoutService.DashboardPlanificacaoCellSize;
    }
}
