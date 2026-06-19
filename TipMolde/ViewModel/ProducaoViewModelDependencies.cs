using TipMolde.Services;

namespace TipMolde.ViewModel;

public sealed record ProducaoViewModelDependencies(
    EncomendasService EncomendasService,
    PecasService PecasService,
    FasesProducaoService FasesProducaoService,
    RegistosProducaoService RegistosProducaoService);
