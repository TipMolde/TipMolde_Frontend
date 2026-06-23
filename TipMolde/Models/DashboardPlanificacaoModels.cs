using System.Collections.ObjectModel;
using System.Globalization;

namespace TipMolde.Models;

public sealed class DashboardPlanificacaoDiaItem
{
    private static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo("pt-PT");

    public DashboardPlanificacaoDiaItem(
        DateTime data,
        bool dentroDoIntervalo,
        bool mostrarDataLonga,
        IEnumerable<FilaGlobalMoldeItemDto> moldes)
    {
        Data = data.Date;
        DentroDoIntervalo = dentroDoIntervalo;
        MostrarDataLonga = mostrarDataLonga;
        Moldes = new ObservableCollection<FilaGlobalMoldeItemDto>(moldes);
    }

    public DateTime Data { get; }
    public bool DentroDoIntervalo { get; }
    public bool MostrarDataLonga { get; }
    public ObservableCollection<FilaGlobalMoldeItemDto> Moldes { get; }

    public string DiaSemanaDisplay => Capitalize(Data.ToString("dddd", PtCulture));
    public string DataDisplay => MostrarDataLonga
        ? Data.ToString("d 'de' MMMM", PtCulture)
        : Data.ToString("dd", PtCulture);
    public string MoldesResumoDisplay => Moldes.Count == 1
        ? "1 molde"
        : $"{Moldes.Count} moldes";
    public bool TemMoldes => Moldes.Count > 0;
    public int MoldesExtraCount => Math.Max(0, Moldes.Count - 2);
    public bool TemMoldesExtra => MoldesExtraCount > 0;
    public bool SemMoldes => !TemMoldes;
    public IEnumerable<FilaGlobalMoldeItemDto> MoldesVisiveis => Moldes.Take(2);

    private static string Capitalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}

public sealed class DashboardPlanificacaoSemanaItem
{
    public DashboardPlanificacaoSemanaItem(
        DashboardPlanificacaoDiaItem domingo,
        DashboardPlanificacaoDiaItem segunda,
        DashboardPlanificacaoDiaItem terca,
        DashboardPlanificacaoDiaItem quarta,
        DashboardPlanificacaoDiaItem quinta,
        DashboardPlanificacaoDiaItem sexta,
        DashboardPlanificacaoDiaItem sabado)
    {
        DiaDomingo = domingo;
        DiaSegunda = segunda;
        DiaTerca = terca;
        DiaQuarta = quarta;
        DiaQuinta = quinta;
        DiaSexta = sexta;
        DiaSabado = sabado;
    }

    public DashboardPlanificacaoDiaItem DiaDomingo { get; }
    public DashboardPlanificacaoDiaItem DiaSegunda { get; }
    public DashboardPlanificacaoDiaItem DiaTerca { get; }
    public DashboardPlanificacaoDiaItem DiaQuarta { get; }
    public DashboardPlanificacaoDiaItem DiaQuinta { get; }
    public DashboardPlanificacaoDiaItem DiaSexta { get; }
    public DashboardPlanificacaoDiaItem DiaSabado { get; }
}
