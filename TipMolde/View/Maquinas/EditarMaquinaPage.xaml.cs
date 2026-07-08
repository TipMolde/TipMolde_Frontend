using TipMolde.Diagnostics;
using TipMolde.Helper;
using TipMolde.ViewModel;

namespace TipMolde.View;

public partial class EditarMaquinaPage : ContentPage, IQueryAttributable
{
    private readonly EditarMaquinaViewModel _viewModel;
    private int? _lastMaquinaId;

    public EditarMaquinaPage(EditarMaquinaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("maquina_id", out var rawMaquinaId))
            return;

        query.TryGetValue("numero", out var rawNumero);
        query.TryGetValue("nome_modelo", out var rawNomeModelo);
        query.TryGetValue("fase_dedicada", out var rawFaseDedicada);
        query.TryGetValue("estado_atual", out var rawEstadoAtual);
        query.TryGetValue("ip_address", out var rawIpAddress);
        query.Clear();
        TaskMonitor.Observe(
            "TipMolde.View.EditarMaquinaPage.ApplyQueryAttributes",
            CarregarMaquinaAsync(rawMaquinaId, rawNumero, rawNomeModelo, rawFaseDedicada, rawEstadoAtual, rawIpAddress));
    }

    private async Task CarregarMaquinaAsync(
        object rawMaquinaId,
        object? rawNumero,
        object? rawNomeModelo,
        object? rawFaseDedicada,
        object? rawEstadoAtual,
        object? rawIpAddress)
    {
        var maquinaId = QueryAttributeHelper.ParseInt(rawMaquinaId);
        var numero = QueryAttributeHelper.ParseInt(rawNumero);

        if (!maquinaId.HasValue || !numero.HasValue || _lastMaquinaId == maquinaId.Value)
            return;

        _lastMaquinaId = maquinaId.Value;
        await _viewModel.LoadAsync(
            maquinaId.Value,
            numero.Value,
            rawNomeModelo?.ToString(),
            rawFaseDedicada?.ToString(),
            rawEstadoAtual?.ToString(),
            rawIpAddress?.ToString());
    }
}
