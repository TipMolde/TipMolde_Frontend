namespace TipMolde.Services;

public interface INavigationService
{
    Task GoToAsync(string route);
    Task GoToAsync(string route, IDictionary<string, object> parameters);
    Task GoBackAsync();
    Task NavigateToDashboardAfterLoginAsync();
    Task NavigateToAuthenticationAsync();
    void OpenFlyout();
}
