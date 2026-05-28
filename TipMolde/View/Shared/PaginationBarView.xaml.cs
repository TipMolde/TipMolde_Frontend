using System.Windows.Input;

namespace TipMolde.View.Shared;

public partial class PaginationBarView : ContentView
{
    public static readonly BindableProperty PageProperty =
        BindableProperty.Create(
            nameof(Page),
            typeof(int),
            typeof(PaginationBarView),
            1);

    public static readonly BindableProperty TotalPagesProperty =
        BindableProperty.Create(
            nameof(TotalPages),
            typeof(int),
            typeof(PaginationBarView),
            1);

    public static readonly BindableProperty PageInputProperty =
        BindableProperty.Create(
            nameof(PageInput),
            typeof(string),
            typeof(PaginationBarView),
            "1",
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty CanGoFirstProperty =
        BindableProperty.Create(
            nameof(CanGoFirst),
            typeof(bool),
            typeof(PaginationBarView),
            false);

    public static readonly BindableProperty CanGoPreviousProperty =
        BindableProperty.Create(
            nameof(CanGoPrevious),
            typeof(bool),
            typeof(PaginationBarView),
            false);

    public static readonly BindableProperty CanGoNextProperty =
        BindableProperty.Create(
            nameof(CanGoNext),
            typeof(bool),
            typeof(PaginationBarView),
            false);

    public static readonly BindableProperty CanGoLastProperty =
        BindableProperty.Create(
            nameof(CanGoLast),
            typeof(bool),
            typeof(PaginationBarView),
            false);

    public static readonly BindableProperty FirstPageCommandProperty =
        BindableProperty.Create(
            nameof(FirstPageCommand),
            typeof(ICommand),
            typeof(PaginationBarView),
            null);

    public static readonly BindableProperty PreviousPageCommandProperty =
        BindableProperty.Create(
            nameof(PreviousPageCommand),
            typeof(ICommand),
            typeof(PaginationBarView),
            null);

    public static readonly BindableProperty GoToPageCommandProperty =
        BindableProperty.Create(
            nameof(GoToPageCommand),
            typeof(ICommand),
            typeof(PaginationBarView),
            null);

    public static readonly BindableProperty NextPageCommandProperty =
        BindableProperty.Create(
            nameof(NextPageCommand),
            typeof(ICommand),
            typeof(PaginationBarView),
            null);

    public static readonly BindableProperty LastPageCommandProperty =
        BindableProperty.Create(
            nameof(LastPageCommand),
            typeof(ICommand),
            typeof(PaginationBarView),
            null);

    public int Page
    {
        get => (int)GetValue(PageProperty);
        set => SetValue(PageProperty, value);
    }

    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    public string PageInput
    {
        get => (string)GetValue(PageInputProperty);
        set => SetValue(PageInputProperty, value);
    }

    public bool CanGoFirst
    {
        get => (bool)GetValue(CanGoFirstProperty);
        set => SetValue(CanGoFirstProperty, value);
    }

    public bool CanGoPrevious
    {
        get => (bool)GetValue(CanGoPreviousProperty);
        set => SetValue(CanGoPreviousProperty, value);
    }

    public bool CanGoNext
    {
        get => (bool)GetValue(CanGoNextProperty);
        set => SetValue(CanGoNextProperty, value);
    }

    public bool CanGoLast
    {
        get => (bool)GetValue(CanGoLastProperty);
        set => SetValue(CanGoLastProperty, value);
    }

    public ICommand? FirstPageCommand
    {
        get => (ICommand?)GetValue(FirstPageCommandProperty);
        set => SetValue(FirstPageCommandProperty, value);
    }

    public ICommand? PreviousPageCommand
    {
        get => (ICommand?)GetValue(PreviousPageCommandProperty);
        set => SetValue(PreviousPageCommandProperty, value);
    }

    public ICommand? GoToPageCommand
    {
        get => (ICommand?)GetValue(GoToPageCommandProperty);
        set => SetValue(GoToPageCommandProperty, value);
    }

    public ICommand? NextPageCommand
    {
        get => (ICommand?)GetValue(NextPageCommandProperty);
        set => SetValue(NextPageCommandProperty, value);
    }

    public ICommand? LastPageCommand
    {
        get => (ICommand?)GetValue(LastPageCommandProperty);
        set => SetValue(LastPageCommandProperty, value);
    }

    public PaginationBarView()
    {
        InitializeComponent();
    }
}