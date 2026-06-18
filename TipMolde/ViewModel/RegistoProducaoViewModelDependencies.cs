using TipMolde.Services;

namespace TipMolde.ViewModel;

public sealed record RegistoProducaoViewModelDependencies(
    RegistosProducaoService RegistosProducaoService,
    FasesProducaoService FasesProducaoService,
    MaquinasService MaquinasService,
    PecasService PecasService,
    MoldesService MoldesService);
