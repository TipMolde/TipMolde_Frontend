using CommunityToolkit.Mvvm.Input;
using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Concentra o carregamento principal do detalhe de projeto.
/// </summary>
public partial class ProjetoDetalheViewModel
{
    /// <summary>
    /// Carrega o contexto completo do projeto, incluindo revisoes e tempo.
    /// </summary>
    /// <param name="projetoId">Identificador do projeto a carregar.</param>
    /// <returns>Tarefa assincrona do carregamento inicial.</returns>
    public async Task LoadAsync(int projetoId)
    {
        ProjetoId = projetoId;
        ErrorMessage = string.Empty;

        try
        {
            await ExecuteLoadAsync(async () =>
            {
                await EnsureCurrentUserAsync();

                var projetoCarregado = await _projetosService.GetWithRevisoesAsync(projetoId);
                if (projetoCarregado is null)
                {
                    ErrorMessage = "Nao foi possivel carregar o projeto.";
                    ClearContext();
                    return;
                }

                Projeto = projetoCarregado;

                Revisoes.Clear();
                foreach (var revisao in projetoCarregado.Revisoes.OrderByDescending(item => item.NumRevisao))
                    Revisoes.Add(revisao);

                RegistosTempo.Clear();
                if (CanAccessTempo && _currentUserId.HasValue)
                {
                    var historico = await GetAllRegistosTempoAsync(projetoId, _currentUserId.Value);
                    foreach (var registo in historico.OrderByDescending(item => item.Data_hora))
                        RegistosTempo.Add(registo);
                }

                AtualizarResumoTempo();
                OnStateChanged();
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ClearContext();
        }
    }

    /// <summary>
    /// Recarrega o projeto atualmente em contexto.
    /// </summary>
    /// <returns>Tarefa assincrona do recarregamento.</returns>
    [RelayCommand]
    private async Task RecarregarAsync()
    {
        if (ProjetoId <= 0)
            return;

        await LoadAsync(ProjetoId);
    }

    /// <summary>
    /// Regressa a pagina anterior.
    /// </summary>
    /// <returns>Tarefa assincrona da navegacao de retorno.</returns>
    [RelayCommand]
    private static async Task VoltarAsync()
    {
        await ShellNavigationService.GoBackAsync();
    }

    private async Task ExecuteLoadAsync(Func<Task> loadAction)
    {
        if (IsLoading)
            return;

        IsLoading = true;

        try
        {
            await loadAction();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task EnsureCurrentUserAsync()
    {
        if (_currentUserId.HasValue)
            return;

        _currentUserId = _sessaoPersistidaService.TryGetCurrentUserId();
        await EnsureUserRoleAsync();
    }

    private async Task EnsureUserRoleAsync()
    {
        if (_roleLoaded)
            return;

        _roleLoaded = true;

        try
        {
            await _authorizationService.GetCurrentRoleAsync();
            IsAdmin = _authorizationService.CanCreateMachines();
            CanManageTempo = _authorizationService.CanManagePieces();
        }
        catch
        {
            IsAdmin = false;
            CanManageTempo = false;
        }
    }

    private void ClearContext()
    {
        Projeto = null;
        Revisoes.Clear();
        RegistosTempo.Clear();
        TempoRegistadoTotal = TimeSpan.Zero;
        TempoSessaoAtiva = string.Empty;
        OnStateChanged();
    }

    private void OnStateChanged()
    {
        OnPropertyChanged(nameof(HasProjeto));
        OnPropertyChanged(nameof(HasRevisoes));
        OnPropertyChanged(nameof(HasRegistosTempo));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(CanAccessTempo));
        OnPropertyChanged(nameof(CanCreateRevisao));
        OnPropertyChanged(nameof(ProjetoTituloDisplay));
        OnPropertyChanged(nameof(ProjetoSubtituloDisplay));
        OnPropertyChanged(nameof(ProjetoCaminhoDisplay));
        OnPropertyChanged(nameof(RevisoesResumoDisplay));
        OnPropertyChanged(nameof(EmptyRevisoesMessage));
        OnPropertyChanged(nameof(EmptyTempoMessage));
        OnPropertyChanged(nameof(TempoTotalDisplay));
        OnPropertyChanged(nameof(TempoSessaoAtivaDisplay));
    }
}
