using FluentAssertions;
using NUnit.Framework;
using TipMolde.Models;

namespace TipMolde.Test.Unitario.Model;

[TestFixture]
[Category("Unit")]
[Category("CrossPlatform")]
public class MaquinaItemTests
{
    [Test(Description = "T1MOD - A maquina deve indicar que tem conexao quando existe IP configurado.")]
    public void HasIpAddress_Should_BeTrue_When_IpIsConfigured()
    {
        var machine = new MaquinaItem { IpAddress = "192.168.1.50" };

        machine.HasIpAddress.Should().BeTrue();
        machine.IpAddressDisplay.Should().Be("192.168.1.50");
        machine.LigacaoDisplay.Should().Be("Conexao configurada");
    }

    [Test(Description = "T2MOD - A maquina deve mostrar fallback de conexao quando o IP esta vazio.")]
    public void IpAddressDisplay_Should_ShowFallback_When_IpIsMissing()
    {
        var machine = new MaquinaItem { IpAddress = "   " };

        machine.HasIpAddress.Should().BeFalse();
        machine.IpAddressDisplay.Should().Be("Sem conexao configurada");
        machine.LigacaoDisplay.Should().Be("Sem conexao");
    }

    [Test(Description = "T3MOD - O estado e a ligacao devem refletir os badges visuais esperados para a UI.")]
    public void BadgeProperties_Should_ReflectStateAndConnection()
    {
        var machine = new MaquinaItem
        {
            Estado = "EM_USO",
            IpAddress = "10.10.10.10"
        };

        machine.EstadoBadgeBackground.Should().Be("#DBEAFE");
        machine.EstadoBadgeForeground.Should().Be("#1D4ED8");
        machine.LigacaoBadgeBackground.Should().Be("#EFF6FF");
        machine.LigacaoBadgeForeground.Should().Be("#1D4ED8");
    }
}
