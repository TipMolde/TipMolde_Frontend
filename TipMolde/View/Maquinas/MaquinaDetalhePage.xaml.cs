using TipMolde.Diagnostics;
using System.Threading;
using Microsoft.Maui.ApplicationModel;
using TipMolde.Helper;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class MaquinaDetalhePage : ContentPage, IQueryAttributable
{
    private static readonly TimeSpan AutoRefreshInterval = TimeSpan.FromSeconds(15);
    private readonly MaquinaDetalheViewModel _viewModel;
    private int? _lastMaquinaId;
    private CancellationTokenSource? _autoRefreshCts;

    public MaquinaDetalhePage(MaquinaDetalheViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartAutoRefresh();
    }

    protected override void OnDisappearing()
    {
        StopAutoRefresh();
        base.OnDisappearing();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("maquina_id", out var rawMaquinaId))
            return;

        var maquinaId = QueryAttributeHelper.ParseInt(rawMaquinaId) ?? 0;

        if (maquinaId <= 0 || _lastMaquinaId == maquinaId)
            return;

        _lastMaquinaId = maquinaId;
        TaskMonitor.Observe("TipMolde.View.MaquinaDetalhePage.ApplyQueryAttributes", _viewModel.LoadAsync(maquinaId));
        StartAutoRefresh();
    }

    private void StartAutoRefresh()
    {
        if (_lastMaquinaId is null || _autoRefreshCts is not null)
            return;

        _autoRefreshCts = new CancellationTokenSource();
        TaskMonitor.Observe("TipMolde.View.MaquinaDetalhePage.AutoRefresh", AutoRefreshLoopAsync(_autoRefreshCts.Token));
    }

    private void StopAutoRefresh()
    {
        if (_autoRefreshCts is null)
            return;

        _autoRefreshCts.Cancel();
        _autoRefreshCts.Dispose();
        _autoRefreshCts = null;
    }

    private async Task AutoRefreshLoopAsync(CancellationToken token)
    {
        try
        {
            using var timer = new PeriodicTimer(AutoRefreshInterval);

            while (await timer.WaitForNextTickAsync(token))
            {
                if (_viewModel.Maquina is null || _viewModel.IsLoading || _viewModel.IsSaving || _viewModel.IsConclusaoSelectionActive)
                    continue;

                await MainThread.InvokeOnMainThreadAsync(() => _viewModel.RefreshAsync());
            }
        }
        catch (OperationCanceledException)
        {
            // Esperado quando a pagina deixa de estar visivel.
        }
    }
}
