using FluentAssertions;
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

        _sut = new ProducaoViewModel(
            new FasesProducaoService(httpClient),
            new PecasService(httpClient),
            new SessaoPersistidaService(httpClient),
            new UtilizadoresService(httpClient),
            new RegistosProducaoService(httpClient),
            new DialogServiceStub());
    }

    [Test(Description = "T1FRT - A pagina de producao deve expor os modos de pesquisa por molde, peca e fase.")]
    public void SearchModes_Should_IncludePhaseMode_When_PageIsConfigured()
    {
        // ASSERT
        _sut.SearchModes.Should().HaveCount(3);
        _sut.SearchModes.Should().Contain("Molde");
        _sut.SearchModes.Should().Contain("Peca");
        _sut.SearchModes.Should().ContainSingle(mode => mode.Contains("fase", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class DialogServiceStub : IDialogService
    {
        public Page GetCurrentPage() => new ContentPage();
        public Task<string> ShowOptionsAsync(string message, string action) => Task.FromResult(string.Empty);
        public Task<string?> ShowSelectionAsync(string title, string cancel, params string[] options) => Task.FromResult<string?>(null);
        public Task<string?> PromptAsync(string title, string message, PromptDialogOptions? options = null) => Task.FromResult<string?>(null);
        public Task<bool> ConfirmDeleteAsync(string message) => Task.FromResult(false);
        public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;
        public Task ShowSuccessAsync(string title, string message) => Task.CompletedTask;
        public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    }
}
