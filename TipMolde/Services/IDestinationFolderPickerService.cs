namespace TipMolde.Services;

public interface IDestinationFolderPickerService
{
    Task<string?> PickFolderAsync(string title = "Seleciona a pasta de destino");
}
