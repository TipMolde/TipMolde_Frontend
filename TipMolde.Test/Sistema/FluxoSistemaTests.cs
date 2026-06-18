using NUnit.Framework;

namespace TipMolde.Test.Sistema;

/// <summary>
/// Base para testes de sistema do frontend.
/// </summary>
/// <remarks>
/// Estes testes devem cobrir fluxos completos do utilizador e podem ser adicionados por partes.
/// </remarks>
[TestFixture]
[Category("System")]
public class FluxoSistemaTests
{
    [Test(Description = "TS1FRT - Fluxo completo de autenticação e navegacao inicial.")]
    [Explicit("Teste de sistema base para ser completado com o fluxo real da aplicacao.")]
    public void FluxoAutenticacao_E_Navegacao_Inicial_Deve_Ser_Validado()
    {
        // ARRANGE
        // ACT
        // ASSERT
        Assert.Pass("Estrutura base para teste de sistema criada.");
    }
}
