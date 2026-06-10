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

    private bool IsPhone => DeviceInfo.Current.Idiom == DeviceIdiom.Phone;

    public SidebarView()
    {
        InitializeComponent();
        IsVisible = !IsPhone;
        ConfigureSidebar();
        Loaded += OnLoaded;
    }

    private void ConfigureSidebar()
    {
        if (IsPhone)
            SetExpanded(false);
        else
            SetExpanded(true);
    }

    private void SetExpanded(bool expanded)
    {
        ShowLabels = expanded;

        if (expanded)
            SidebarWidth = IsPhone ? 230 : 280;
        else
            SidebarWidth = IsPhone ? 88 : 96;
    }

    private void OnHeaderTapped(object sender, TappedEventArgs e)
    {
        if (!IsPhone)
            return;

        SetExpanded(!ShowLabels);
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (!IsVisible)
            return;

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

    private void AttachToParentPage()
    {
        _parentPage = FindParentPage();
        if (_parentPage is not null)
            _parentPage.Appearing += OnParentPageAppearing;
    }

    private async void OnParentPageAppearing(object? sender, EventArgs e)
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
}
