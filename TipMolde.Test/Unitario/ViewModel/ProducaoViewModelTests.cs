using FluentAssertions;
using Moq;
using NUnit.Framework;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

/// <summary>
/// Testes unitarios do ViewModel da pagina de producao do frontend.
/// </summary>
/// <remarks>
/// Garante que a pesquisa da pagina nao expõe a opcao de fase removida.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class ProducaoViewModelTests
{
    private ProducaoViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var httpClient = new HttpClient(new HttpClientHandler())
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var dependencies = new ProducaoViewModelDependencies(
            new EncomendasService(httpClient),
            new PecasService(httpClient),
            new FasesProducaoService(httpClient),
            new MaquinasService(httpClient),
            new RegistosProducaoService(httpClient));

        _sut = new ProducaoViewModel(
            dependencies,
            new SessaoPersistidaService(httpClient),
            new UtilizadoresService(httpClient),
            new Mock<IDialogService>().Object);
    }

    [Test(Description = "T1FRT - A pagina de producao deve permitir pesquisa apenas por molde e peca.")]
    public void SearchModes_Should_ExcludePhaseMode_When_PageIsConfigured()
    {
        // ASSERT
        _sut.SearchModes.Should().Equal("Molde", "Peca");
    }
}
