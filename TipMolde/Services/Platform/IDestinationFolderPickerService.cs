namespace TipMolde.Services;

/// <summary>
/// Abstrai a selecao de pastas de destino no frontend.
/// </summary>
public interface IDestinationFolderPickerService
{
    /// <summary>
    /// Pede ao utilizador uma pasta de destino.
    /// </summary>
    /// <param name="title">Titulo funcional do seletor.</param>
    /// <returns>Caminho da pasta escolhida ou nulo quando a operacao e cancelada.</returns>
    Task<string?> PickFolderAsync(string title = "Seleciona a pasta de destino");
}
