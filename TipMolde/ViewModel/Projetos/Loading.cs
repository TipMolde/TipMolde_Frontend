using CommunityToolkit.Mvvm.Input;
using TipMolde.Services;

namespace TipMolde.ViewModel;

public partial class ProjetoDetalheViewModel
{
    public async Task LoadAsync(int projetoId)
    {
        ProjetoId = projetoId;
        ErrorMessage = string.Empty;

        try
        {
            await ExecuteLoadAsync(async () =>
            {
                await EnsureCurrentUserAsync();

                var projeto = await _projetosService.GetWithRevisoesAsync(projetoId);
                if (projeto is null)
                {
                    ErrorMessage = "Nao foi possivel carregar o projeto.";
                    ClearContext();
                    return;
                }

                Projeto = projeto;

                Revisoes.Clear();
                foreach (var revisao in projeto.Revisoes.OrderByDescending(item => item.NumRevisao))
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

    [RelayCommand]
    private async Task RecarregarAsync()
    {
        if (ProjetoId <= 0)
            return;

        await LoadAsync(ProjetoId);
    }

    [RelayCommand]
    private async Task VoltarAsync()
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
