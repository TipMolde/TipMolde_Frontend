using TipMolde.Services;

namespace TipMolde.ViewModel;

/// <summary>
/// Agrupa as dependencias principais usadas pelo view model da pagina de producao.
/// </summary>
public sealed record ProducaoViewModelDependencies(
    EncomendasService EncomendasService,
    PecasService PecasService,
    FasesProducaoService FasesProducaoService,
    RegistosProducaoService RegistosProducaoService);
