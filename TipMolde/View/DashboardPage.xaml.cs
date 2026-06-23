using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class MainPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public MainPage(DashboardViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = _viewModel;
        PlanificacaoCalendarRoot.SizeChanged += OnPlanificacaoCalendarRootSizeChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
        AtualizarPlanificacaoCellSize();
    }

    private void OnPlanificacaoCalendarRootSizeChanged(object? sender, EventArgs e)
    {
        AtualizarPlanificacaoCellSize();
    }

    private void AtualizarPlanificacaoCellSize()
    {
        if (PlanificacaoCalendarRoot.Width > 0)
            _viewModel.AtualizarPlanificacaoCellSize(PlanificacaoCalendarRoot.Width);
    }
}
