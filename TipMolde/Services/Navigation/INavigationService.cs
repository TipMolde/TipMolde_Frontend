namespace TipMolde.Services;

/// <summary>
/// Abstrai a navegacao principal do frontend para evitar dependencia direta de <see cref="Shell"/>.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navega para uma rota sem parametros.
    /// </summary>
    /// <param name="route">Rota Shell de destino.</param>
    /// <returns>Tarefa assincrona da navegacao.</returns>
    Task GoToAsync(string route);

    /// <summary>
    /// Navega para uma rota transportando parametros para a pagina de destino.
    /// </summary>
    /// <param name="route">Rota Shell de destino.</param>
    /// <param name="parameters">Colecao de parametros a injetar na navegacao.</param>
    /// <returns>Tarefa assincrona da navegacao.</returns>
    Task GoToAsync(string route, IDictionary<string, object> parameters);

    /// <summary>
    /// Regressa a pagina anterior respeitando stacks modal e normal.
    /// </summary>
    /// <returns>Tarefa assincrona da navegacao de retorno.</returns>
    Task GoBackAsync();

    /// <summary>
    /// Encaminha o utilizador autenticado para o dashboard apos login bem-sucedido.
    /// </summary>
    /// <returns>Tarefa assincrona da navegacao pos-login.</returns>
    Task NavigateToDashboardAfterLoginAsync();

    /// <summary>
    /// Encaminha o utilizador para o ecran de autenticacao.
    /// </summary>
    /// <returns>Tarefa assincrona da navegacao para autenticacao.</returns>
    Task NavigateToAuthenticationAsync();

    /// <summary>
    /// Abre o menu de navegacao quando o layout atual o suporta.
    /// </summary>
    void OpenFlyout();
}
