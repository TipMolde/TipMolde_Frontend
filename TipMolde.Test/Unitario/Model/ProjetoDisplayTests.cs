using FluentAssertions;
using NUnit.Framework;
using TipMolde.Models;

namespace TipMolde.Test.Unitario.Model;

[TestFixture]
[Category("Unit")]
public class ProjetoDisplayTests
{
    [Test(Description = "O display do molde de projeto deve preferir o numero funcional quando existe.")]
    public void ProjetoDto_MoldeDisplay_Should_UseNumeroMolde_WhenAvailable()
    {
        var dto = new ProjetoDto
        {
            Molde_id = 42,
            NumeroMolde = "M-042"
        };

        dto.MoldeDisplay.Should().Be("M-042");
    }

    [Test(Description = "O display do molde de projeto deve cair para o id quando o numero nao vem.")]
    public void ProjetoComRevisoesDto_MoldeDisplay_Should_FallBackToId_WhenNumeroMoldeIsMissing()
    {
        var dto = new ProjetoComRevisoesDto
        {
            Molde_id = 42
        };

        dto.MoldeDisplay.Should().Be("Molde #42");
    }
}
