namespace TipMolde.Services;

/// <summary>
/// Encapsula a selecao de ficheiros exposta pela plataforma MAUI.
/// </summary>
public sealed class FilePickerService : IFilePickerService
{
    /// <summary>
    /// Abre o seletor de ficheiros da plataforma atual.
    /// </summary>
    /// <param name="options">Configuracao do tipo e contexto de ficheiros permitidos.</param>
    /// <returns>Ficheiro selecionado ou nulo quando o utilizador cancela.</returns>
    public Task<FileResult?> PickAsync(PickOptions options)
    {
        return FilePicker.Default.PickAsync(options);
    }
}
