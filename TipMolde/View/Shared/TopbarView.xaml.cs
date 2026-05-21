using TipMolde.Services;

namespace TipMolde.View.Shared;

public partial class TopBarView : ContentView
{
    public static readonly BindableProperty TitleTextProperty =
        BindableProperty.Create(
            nameof(TitleText),
            typeof(string),
            typeof(TopBarView),
            string.Empty);

    public static readonly BindableProperty UserNameTextProperty =
        BindableProperty.Create(
            nameof(UserNameText),
            typeof(string),
            typeof(TopBarView),
            "Utilizador",
            propertyChanged: OnUserNameTextChanged);

    public string TitleText
    {
        get => (string)GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public string UserNameText
    {
        get => (string)GetValue(UserNameTextProperty);
        set => SetValue(UserNameTextProperty, value);
    }

    public string DisplayUserName =>
    DeviceInfo.Current.Idiom == DeviceIdiom.Phone
        ? GetFirstName(UserNameText)
        : UserNameText;

    public TopBarView()
    {
        InitializeComponent();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        if (Handler?.MauiContext?.Services.GetService(typeof(SessaoPersistidaService))
            is SessaoPersistidaService sessaoPersistidaService)
        {
            await sessaoPersistidaService.ClearSessionAsync();
        }

        await Shell.Current.GoToAsync("//AutenticacaoPage");
    }

    private static void OnUserNameTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TopBarView topBarView)
            topBarView.OnPropertyChanged(nameof(DisplayUserName));
    }

    private static string GetFirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "Utilizador";

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : fullName;
    }
}