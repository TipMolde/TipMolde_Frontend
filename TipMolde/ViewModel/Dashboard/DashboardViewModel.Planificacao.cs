using CommunityToolkit.Mvvm.Input;
using TipMolde.Models;
using TipMolde.View;

namespace TipMolde.ViewModel;

public partial class DashboardViewModel
{
    partial void OnSelectedPlanificacaoDiaChanged(DashboardPlanificacaoDiaItem? value)
    {
        MoldesDiaSelecionado.Clear();

        if (value is not null)
        {
            foreach (var molde in value.Moldes.OrderBy(item => item.Prioridade).ThenBy(item => item.NumeroMoldeDisplay))
                MoldesDiaSelecionado.Add(molde);
        }

        OnPropertyChanged(nameof(HasSelectedPlanificacaoDia));
        OnPropertyChanged(nameof(HasNoMoldesDiaSelecionado));
        OnPropertyChanged(nameof(SelectedPlanificacaoDiaDisplay));
        OnPropertyChanged(nameof(SelectedPlanificacaoDiaResumoDisplay));
    }

    partial void OnSemanaInicialPlanificacaoChanged(DateTime value)
    {
        var inicioSemana = StartOfWeek(value.Date, DayOfWeek.Sunday);
        if (value.Date != inicioSemana)
        {
            SemanaInicialPlanificacao = inicioSemana;
            return;
        }

        OnPropertyChanged(nameof(IntervaloPlanificacaoDisplay));
        AtualizarPlanificacao();
    }

    partial void OnNumeroSemanasPlanificacaoChanged(int value)
    {
        var semanasNormalizadas = Math.Clamp(value, 1, 8);
        if (value != semanasNormalizadas)
        {
            NumeroSemanasPlanificacao = semanasNormalizadas;
            return;
        }

        OnPropertyChanged(nameof(IntervaloPlanificacaoDisplay));
        OnPropertyChanged(nameof(NumeroSemanasPlanificacaoDisplay));
        AtualizarPlanificacao();
    }

    [RelayCommand]
    private void SelecionarPlanificacaoDia(DashboardPlanificacaoDiaItem? dia)
    {
        if (dia is null)
            return;

        SelectedPlanificacaoDia = dia;
    }

    [RelayCommand]
    private void FecharPlanificacaoDia()
    {
        SelectedPlanificacaoDia = null;
    }

    [RelayCommand]
    private void SemanaAnteriorPlanificacao()
    {
        SemanaInicialPlanificacao = SemanaInicialPlanificacao.AddDays(-7);
    }

    [RelayCommand]
    private void SemanaSeguintePlanificacao()
    {
        SemanaInicialPlanificacao = SemanaInicialPlanificacao.AddDays(7);
    }

    [RelayCommand]
    private void SemanaAtualPlanificacao()
    {
        SemanaInicialPlanificacao = StartOfWeek(DateTime.Today, DayOfWeek.Sunday);
    }

    [RelayCommand]
    private void ReduzirSemanasPlanificacao()
    {
        if (NumeroSemanasPlanificacao > 1)
            NumeroSemanasPlanificacao--;
    }

    [RelayCommand]
    private void AumentarSemanasPlanificacao()
    {
        if (NumeroSemanasPlanificacao < 8)
            NumeroSemanasPlanificacao++;
    }

    [RelayCommand]
    private async Task AbrirMoldePlanificacaoAsync(FilaGlobalMoldeItemDto? item)
    {
        if (item is null || item.MoldeId <= 0)
            return;

        await _navigationService.GoToAsync($"{nameof(MoldeDetalhePage)}?molde_id={item.MoldeId}");
    }

    private void AtualizarPlanificacao()
    {
        MoldesPlanificacao.Clear();
        PlanificacaoDias.Clear();
        PlanificacaoSemanas.Clear();

        var inicio = StartOfWeek(SemanaInicialPlanificacao.Date, DayOfWeek.Sunday);
        var fim = inicio.AddDays((Math.Clamp(NumeroSemanasPlanificacao, 1, 8) * 7) - 1);

        var filtrados = _todosMoldesPlanificacao.Count == 0
            ? []
            : _todosMoldesPlanificacao
                .Where(item => item.DataEntregaPrevista.Date >= inicio && item.DataEntregaPrevista.Date <= fim)
                .GroupBy(item => item.MoldeId)
                .Select(group => group
                    .OrderBy(item => item.DataEntregaPrevista <= DateTime.MinValue ? DateTime.MaxValue : item.DataEntregaPrevista)
                    .ThenBy(item => item.Prioridade)
                    .First())
                .OrderBy(item => item.DataEntregaPrevista <= DateTime.MinValue ? DateTime.MaxValue : item.DataEntregaPrevista)
                .ThenBy(item => item.Prioridade)
                .ThenBy(item => item.NumeroMoldeDisplay)
                .ToList();

        foreach (var item in filtrados)
            MoldesPlanificacao.Add(item);

        var moldesPorDia = filtrados
            .GroupBy(item => item.DataEntregaPrevista.Date)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Prioridade).ThenBy(item => item.NumeroMoldeDisplay).ToList());

        var calendarioInicio = StartOfWeek(inicio, DayOfWeek.Sunday);
        var calendarioFim = EndOfWeek(fim, DayOfWeek.Saturday);

        var diasCalendario = new List<DashboardPlanificacaoDiaItem>();
        for (var data = calendarioInicio; data <= calendarioFim; data = data.AddDays(1))
        {
            var dia = BuildDiaPlanificacao(data, inicio, fim, moldesPorDia, calendarioInicio);
            PlanificacaoDias.Add(dia);
            diasCalendario.Add(dia);
        }

        for (var index = 0; index + 6 < diasCalendario.Count; index += 7)
        {
            PlanificacaoSemanas.Add(new DashboardPlanificacaoSemanaItem(
                diasCalendario[index],
                diasCalendario[index + 1],
                diasCalendario[index + 2],
                diasCalendario[index + 3],
                diasCalendario[index + 4],
                diasCalendario[index + 5],
                diasCalendario[index + 6]));
        }

        var selectedDate = SelectedPlanificacaoDia?.Data.Date;
        SelectedPlanificacaoDia = selectedDate.HasValue
            ? PlanificacaoDias.FirstOrDefault(item => item.Data.Date == selectedDate.Value)
            : null;

        OnPropertyChanged(nameof(HasPlanificacao));
        OnPropertyChanged(nameof(HasNoPlanificacao));
        OnPropertyChanged(nameof(PlanificacaoResumoDisplay));
    }

    private static DashboardPlanificacaoDiaItem BuildDiaPlanificacao(
        DateTime data,
        DateTime inicio,
        DateTime fim,
        IReadOnlyDictionary<DateTime, List<FilaGlobalMoldeItemDto>> moldesPorDia,
        DateTime calendarioInicio)
    {
        var dia = data.Date;
        var dentroDoIntervalo = dia >= inicio && dia <= fim;
        var mostrarDataLonga = dia == calendarioInicio || dia.Day == 1;

        moldesPorDia.TryGetValue(dia, out var moldesDoDia);

        return new DashboardPlanificacaoDiaItem(
            dia,
            dentroDoIntervalo,
            mostrarDataLonga,
            dentroDoIntervalo && moldesDoDia is not null ? moldesDoDia : []);
    }

    private static DateTime StartOfWeek(DateTime date, DayOfWeek firstDayOfWeek)
    {
        var diff = (7 + (date.DayOfWeek - firstDayOfWeek)) % 7;
        return date.Date.AddDays(-diff);
    }

    private static DateTime EndOfWeek(DateTime date, DayOfWeek lastDayOfWeek)
    {
        var diff = (7 + (lastDayOfWeek - date.DayOfWeek)) % 7;
        return date.Date.AddDays(diff);
    }

    private void LimparPlanificacao()
    {
        _todosMoldesPlanificacao = [];
        MoldesPlanificacao.Clear();
        PlanificacaoDias.Clear();
        PlanificacaoSemanas.Clear();
        MoldesDiaSelecionado.Clear();
        SelectedPlanificacaoDia = null;
        OnPropertyChanged(nameof(HasPlanificacao));
        OnPropertyChanged(nameof(HasNoPlanificacao));
        OnPropertyChanged(nameof(HasSelectedPlanificacaoDia));
        OnPropertyChanged(nameof(PlanificacaoResumoDisplay));
    }
}
