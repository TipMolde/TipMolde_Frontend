using FluentAssertions;
using NUnit.Framework;
using System.Reflection;
using TipMolde.Services;

namespace TipMolde.Test.Unitario.Service;

/// <summary>
/// Testes unitarios do servico de autorizacao do frontend.
/// </summary>
/// <remarks>
/// Valida as regras de permissao das funcionalidades expostas na interface.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class AuthorizationServiceTests
{
    private AuthorizationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var httpClient = new HttpClient(new HttpClientHandler())
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var sessaoPersistidaService = new SessaoPersistidaService(httpClient);
        var utilizadoresService = new UtilizadoresService(httpClient);

        _sut = new AuthorizationService(sessaoPersistidaService, utilizadoresService);
    }

    /// <summary>
    /// Define a role em cache para simular a sessao ativa do utilizador.
    /// </summary>
    /// <param name="role">Role a injetar no cache interno do servico.</param>
    private void SetCachedRole(string? role)
    {
        typeof(AuthorizationService)
            .GetField("_cachedRole", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_sut, role);
    }

    /// <summary>
    /// Define o identificador em cache para simular a associacao ao utilizador atual.
    /// </summary>
    /// <param name="userId">Identificador a injetar no cache interno do servico.</param>
    private void SetCachedUserId(int? userId)
    {
        typeof(AuthorizationService)
            .GetField("_cachedUserId", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_sut, userId);
    }

    [Test(Description = "T1FRT - O administrador deve poder criar maquinas.")]
    public void CanCreateMachines_Should_ReturnTrue_When_RoleIsAdmin()
    {
        // ARRANGE
        SetCachedRole("ADMIN");
        SetCachedUserId(1);

        // ACT
        var result = _sut.CanCreateMachines();

        // ASSERT
        result.Should().BeTrue();
    }

    [Test(Description = "T2FRT - O gestor de comercial nao deve poder criar maquinas.")]
    public void CanCreateMachines_Should_ReturnFalse_When_RoleIsCommercialManager()
    {
        // ARRANGE
        SetCachedRole("GESTOR_COMERCIAL");
        SetCachedUserId(1);

        // ACT
        var result = _sut.CanCreateMachines();

        // ASSERT
        result.Should().BeFalse();
    }

    [Test(Description = "T3FRT - O gestor de producao deve poder alterar o estado das maquinas.")]
    public void CanEditMachineState_Should_ReturnTrue_When_RoleIsProductionManager()
    {
        // ARRANGE
        SetCachedRole("GESTOR_PRODUCAO");
        SetCachedUserId(1);

        // ACT
        var result = _sut.CanEditMachineState();

        // ASSERT
        result.Should().BeTrue();
    }

    [Test(Description = "T4FRT - O gestor de desenho deve poder gerir pecas.")]
    public void CanManagePieces_Should_ReturnTrue_When_RoleIsDesignManager()
    {
        // ARRANGE
        SetCachedRole("GESTOR_DESENHO");
        SetCachedUserId(1);

        // ACT
        var result = _sut.CanManagePieces();

        // ASSERT
        result.Should().BeTrue();
    }

    [Test(Description = "T5FRT - O servico deve limpar o cache interno quando pedido.")]
    public void Clear_Should_ResetCachedState()
    {
        // ARRANGE
        SetCachedRole("ADMIN");
        SetCachedUserId(1);

        // ACT
        _sut.Clear();

        // ASSERT
        typeof(AuthorizationService)
            .GetField("_cachedRole", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_sut)
            .Should()
            .BeNull();

        typeof(AuthorizationService)
            .GetField("_cachedUserId", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_sut)
            .Should()
            .BeNull();
    }
}
