using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Agrupa as dependencias principais usadas pelo view model de registo de producao.
/// </summary>
public sealed record RegistoProducaoViewModelDependencies(
    RegistosProducaoService RegistosProducaoService,
    FasesProducaoService FasesProducaoService,
    MaquinasService MaquinasService,
    PecasService PecasService,
    MoldesService MoldesService);
