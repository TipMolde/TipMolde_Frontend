namespace TipMolde.Services;

public sealed class DestinationFolderPickerService : IDestinationFolderPickerService
{
    private readonly IDialogService _dialogService;

    public DestinationFolderPickerService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

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
