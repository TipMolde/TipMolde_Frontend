using FluentAssertions;
using Moq;
using NUnit.Framework;
using TipMolde.Services;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

/// <summary>
/// Testes unitarios do ViewModel da barra superior do frontend.
/// </summary>
/// <remarks>
/// Valida o comportamento basico de apresentacao e reinicio do estado.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class TopBarViewModelTests
{
    private TopBarViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var httpClient = new HttpClient(new HttpClientHandler())
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var sessaoPersistidaService = new SessaoPersistidaService(httpClient);
        var utilizadoresService = new UtilizadoresService(httpClient);
        var authorizationService = new AuthorizationService(sessaoPersistidaService, utilizadoresService);

        _sut = new TopBarViewModel(
            authorizationService,
            sessaoPersistidaService,
            utilizadoresService,
            Mock.Of<INavigationService>(),
            new ResponsiveLayoutService());
    }

    [Test(Description = "T1FRT - O ViewModel deve restaurar o nome padrao quando o estado e reiniciado.")]
    public void Reset_Should_RestoreDefaultUserName_When_Called()
    {
        // ARRANGE
        _sut.CurrentUserName = "Maria Oliveira";

        // ACT
        _sut.Reset();

        // ASSERT
        _sut.CurrentUserName.Should().Be("Utilizador");
    }

    [Test(Description = "T2FRT - O ViewModel deve devolver o nome completo quando nao estiver num telefone.")]
    public void DisplayUserName_Should_ReturnCurrentUserName_When_DeviceIsNotPhone()
    {
        // ARRANGE
        _sut.CurrentUserName = "Maria Oliveira";

        // ACT
        var result = _sut.DisplayUserName;

        // ASSERT
        result.Should().Be("Maria Oliveira");
    }
}
