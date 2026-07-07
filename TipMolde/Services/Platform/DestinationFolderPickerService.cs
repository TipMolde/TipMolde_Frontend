namespace TipMolde.Services;

/// <summary>
/// Implementa a selecao de pastas de destino para exportacoes locais.
/// </summary>
public sealed class DestinationFolderPickerService : IDestinationFolderPickerService
{
    private readonly IDialogService _dialogService;

    /// <summary>
    /// Construtor do seletor de pasta de destino.
    /// </summary>
    /// <param name="dialogService">Servico usado para informar o utilizador quando a plataforma nao suporta a operacao.</param>
    public DestinationFolderPickerService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    /// <summary>
    /// Abre o seletor de pasta da plataforma atual.
    /// </summary>
    /// <param name="title">Titulo funcional apresentado ao utilizador quando aplicavel.</param>
    /// <returns>Caminho da pasta escolhida ou nulo quando a operacao nao esta disponivel ou e cancelada.</returns>
    public async Task<string?> PickFolderAsync(string title = "Seleciona a pasta de destino")
    {
#if WINDOWS
        var picker = new Windows.Storage.Pickers.FolderPicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads
        };

        picker.FileTypeFilter.Add("*");

        var window = Application.Current?.Windows.FirstOrDefault();
        if (window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
            throw new InvalidOperationException("Nao foi possivel abrir o seletor de pasta.");

        WinRT.Interop.InitializeWithWindow.Initialize(
            picker,
            WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow));

        var pasta = await picker.PickSingleFolderAsync();
        return pasta?.Path;
#else
        await _dialogService.ShowInfoAsync(
            title,
            "A selecao de pasta de destino esta disponivel no Windows.");
        return null;
#endif
    }
}
