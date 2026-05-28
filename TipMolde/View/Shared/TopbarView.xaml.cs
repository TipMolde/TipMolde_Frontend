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
        if (_isInitialized)
            return;

        if (Handler?.MauiContext?.Services.GetService(typeof(TopBarViewModel)) is not TopBarViewModel vm)
            return;

        ViewModel = vm;
        _isInitialized = true;

        await vm.EnsureLoadedAsync();
    }
}