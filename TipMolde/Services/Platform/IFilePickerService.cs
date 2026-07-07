namespace TipMolde.Services;

/// <summary>
/// Abstrai a selecao de ficheiros usada pelos view models do frontend.
/// </summary>
public interface IFilePickerService
{
    /// <summary>
    /// Pede ao utilizador um ficheiro compatível com as opcoes fornecidas.
    /// </summary>
    /// <param name="options">Configuracao do seletor de ficheiros.</param>
    /// <returns>Ficheiro selecionado ou nulo quando a operacao e cancelada.</returns>
    Task<FileResult?> PickAsync(PickOptions options);
}
