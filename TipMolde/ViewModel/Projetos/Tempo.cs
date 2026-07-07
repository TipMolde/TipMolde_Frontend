using CommunityToolkit.Mvvm.Input;
using TipMolde.Models;
using TipMolde.ViewModel.Helpers;

namespace TipMolde.ViewModel;

/// <summary>
/// Concentra o fluxo de registo temporal do detalhe de projeto.
/// </summary>
public partial class ProjetoDetalheViewModel
{
    /// <summary>
    /// Regista um novo estado temporal para o projeto atual.
    /// </summary>
    /// <returns>Tarefa assincrona da operacao de registo temporal.</returns>
    [RelayCommand]
    private async Task RegistarTempoAsync()
    {
        if (!CanAccessTempo || Projeto is null || Projeto.Projeto_id <= 0 || isRegisteringTempo)
            return;

        if (!_currentUserId.HasValue)
        {
            await _dialogService.ShowErrorAsync(
                "Tempo de desenho",
                "Nao foi possivel identificar o utilizador autenticado.");
            return;
        }

        var estadosDisponiveis = TempoProjetoStateOptions.GetAllowedStates(RegistosTempo);
        if (estadosDisponiveis.Count == 0)
        {
            await _dialogService.ShowInfoAsync(
                "Tempo de desenho",
                "Nao foi possivel determinar estados disponiveis para este projeto.");
            return;
        }

        var estado = await _dialogService.ShowSelectionAsync(
            $"Registar tempo para {Projeto.NomeProjetoDisplay}",
            DialogCancel,
            estadosDisponiveis.ToArray());

        if (string.IsNullOrWhiteSpace(estado))
            return;

        try
        {
            IsRegisteringTempo = true;
            await _registosTempoProjetoService.CreateAsync(Projeto.Projeto_id, _currentUserId.Value, estado);

            await _dialogService.ShowSuccessAsync(
                "Tempo registado",
                $"O estado {estado} foi registado no projeto {Projeto.NomeProjetoDisplay}.");

            await LoadAsync(Projeto.Projeto_id);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Tempo de desenho", ex.Message);
        }
        finally
        {
            IsRegisteringTempo = false;
        }
    }

    private async Task<List<RegistoTempoProjetoDto>> GetAllRegistosTempoAsync(int projetoId, int autorId)
    {
        var primeiraPagina = await _registosTempoProjetoService.GetHistoricoAsync(projetoId, autorId, 1, 100);
        if (primeiraPagina is null)
            return [];

        var registos = primeiraPagina.Items.ToList();

        for (var page = 2; page <= primeiraPagina.TotalPages; page++)
        {
            var pagina = await _registosTempoProjetoService.GetHistoricoAsync(projetoId, autorId, page, 100);
            if (pagina?.Items is null)
                continue;

            registos.AddRange(pagina.Items);
        }

        return registos;
    }

    private void AtualizarResumoTempo()
    {
        if (RegistosTempo.Count == 0)
        {
            TempoRegistadoTotal = TimeSpan.Zero;
            TempoSessaoAtiva = string.Empty;
            return;
        }

        var registosOrdenados = RegistosTempo.OrderBy(item => item.Data_hora).ToList();
        var total = TimeSpan.Zero;
        DateTime? inicioSessao = null;

        foreach (var registo in registosOrdenados)
        {
            var estado = registo.Estado_tempo.Trim().ToUpperInvariant();

            if (estado is "INICIADO" or "RETOMADO")
            {
                inicioSessao ??= registo.Data_hora;
                continue;
            }

            if (estado is EstadoPausado or EstadoConcluido)
            {
                if (inicioSessao.HasValue && registo.Data_hora > inicioSessao.Value)
                    total += registo.Data_hora - inicioSessao.Value;

                inicioSessao = null;
            }
        }

        if (inicioSessao.HasValue)
            total += DateTime.UtcNow - inicioSessao.Value;

        TempoRegistadoTotal = total;
        TempoSessaoAtiva = inicioSessao.HasValue
            ? $"Sessao ativa desde {inicioSessao.Value.ToLocalTime():dd/MM/yyyy HH:mm}"
            : "Sem sessao ativa";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return "0m";

        var totalHours = (int)duration.TotalHours;
        var minutes = duration.Minutes;

        if (totalHours <= 0)
            return $"{minutes}m";

        return minutes <= 0 ? $"{totalHours}h" : $"{totalHours}h {minutes:00}m";
    }
}
