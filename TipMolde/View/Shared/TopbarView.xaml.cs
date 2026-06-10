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

    public TopBarView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
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
