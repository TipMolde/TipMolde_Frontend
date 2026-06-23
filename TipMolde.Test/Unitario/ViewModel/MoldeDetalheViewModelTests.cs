using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using TipMolde.Models;
using TipMolde.ViewModel;

namespace TipMolde.Test.Unitario.ViewModel;

[TestFixture]
[Category("Unit")]
public class MoldeDetalheViewModelTests
{
    [Test]
    public void CalcularTempoTotalPecas_Should_Somar_Todos_Os_Intervalos_Das_Pecas_Do_Molde()
    {
        // ARRANGE
        var pecaIds = new[] { 10, 11 };
        var registos = new List<RegistoProducaoDto>
        {
            new()
            {
                PecaId = 10,
                EstadoProducao = "PREPARACAO",
                DataHora = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                PecaId = 10,
                EstadoProducao = "PAUSADO",
                DataHora = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                PecaId = 10,
                EstadoProducao = "EM_CURSO",
                DataHora = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                PecaId = 10,
                EstadoProducao = "CONCLUIDO",
                DataHora = new DateTime(2026, 6, 1, 10, 45, 0, DateTimeKind.Utc)
            },
            new()
            {
                PecaId = 11,
                EstadoProducao = "PREPARACAO",
                DataHora = new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                PecaId = 11,
                EstadoProducao = "CONCLUIDO",
                DataHora = new DateTime(2026, 6, 1, 12, 30, 0, DateTimeKind.Utc)
            },
            new()
            {
                PecaId = 99,
                EstadoProducao = "PREPARACAO",
                DataHora = new DateTime(2026, 6, 1, 13, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                PecaId = 99,
                EstadoProducao = "CONCLUIDO",
                DataHora = new DateTime(2026, 6, 1, 15, 0, 0, DateTimeKind.Utc)
            }
        };

        // ACT
        var resultado = InvokeCalculoTempoTotal(pecaIds, registos);

        // ASSERT
        resultado.Should().Be(TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(15)));
    }

    private static TimeSpan InvokeCalculoTempoTotal(IEnumerable<int> pecaIds, IEnumerable<RegistoProducaoDto> registos)
    {
        var metodo = typeof(MoldeDetalheViewModel).GetMethod(
            "CalcularTempoTotalPecas",
            BindingFlags.Static | BindingFlags.NonPublic);

        metodo.Should().NotBeNull();

        return (TimeSpan)metodo!.Invoke(null, [pecaIds, registos])!;
    }
}
