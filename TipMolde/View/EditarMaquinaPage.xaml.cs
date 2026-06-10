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
        _ = CarregarMaquinaAsync(rawMaquinaId, rawNumero, rawNomeModelo, rawFaseDedicada, rawEstadoAtual, rawIpAddress);
    }

    private async Task CarregarMaquinaAsync(
        object rawMaquinaId,
        object? rawNumero,
        object? rawNomeModelo,
        object? rawFaseDedicada,
        object? rawEstadoAtual,
        object? rawIpAddress)
    {
        int? maquinaId = rawMaquinaId switch
        {
            int id => id,
            string text when int.TryParse(text, out var parsedId) => parsedId,
            _ => null
        };

        int? numero = rawNumero switch
        {
            int value => value,
            string text when int.TryParse(text, out var parsedValue) => parsedValue,
            _ => null
        };

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
