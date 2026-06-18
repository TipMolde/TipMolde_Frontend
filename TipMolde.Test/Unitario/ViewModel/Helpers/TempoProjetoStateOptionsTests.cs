using FluentAssertions;
using NUnit.Framework;
using TipMolde.Models;
using TipMolde.ViewModel.Helpers;

namespace TipMolde.Test.Unitario.ViewModel.Helpers;

[TestFixture]
[Category("Unit")]
public class TempoProjetoStateOptionsTests
{
    [Test]
    public void GetAllowedStates_Should_ReturnOnlyInicio_When_HistoricoIsEmpty()
    {
        // ACT
        var result = TempoProjetoStateOptions.GetAllowedStates([]);

        // ASSERT
        result.Should().Equal("INICIADO");
    }

    [Test]
    public void GetAllowedStates_Should_ReturnPausadoAndConcluido_AfterInicio()
    {
        // ARRANGE
        var historico = new[]
        {
            new RegistoTempoProjetoDto { Estado_tempo = "INICIADO", Data_hora = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc) }
        };

        // ACT
        var result = TempoProjetoStateOptions.GetAllowedStates(historico);

        // ASSERT
        result.Should().Equal("PAUSADO", "CONCLUIDO");
    }

    [Test]
    public void GetAllowedStates_Should_ReturnRetomado_AfterPausado()
    {
        // ARRANGE
        var historico = new[]
        {
            new RegistoTempoProjetoDto { Estado_tempo = "INICIADO", Data_hora = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc) },
            new RegistoTempoProjetoDto { Estado_tempo = "PAUSADO", Data_hora = new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc) }
        };

        // ACT
        var result = TempoProjetoStateOptions.GetAllowedStates(historico);

        // ASSERT
        result.Should().Equal("RETOMADO");
    }

    [Test]
    public void GetAllowedStates_Should_ReturnIniciado_AfterConcluido()
    {
        // ARRANGE
        var historico = new[]
        {
            new RegistoTempoProjetoDto { Estado_tempo = "INICIADO", Data_hora = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc) },
            new RegistoTempoProjetoDto { Estado_tempo = "CONCLUIDO", Data_hora = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc) }
        };

        // ACT
        var result = TempoProjetoStateOptions.GetAllowedStates(historico);

        // ASSERT
        result.Should().Equal("INICIADO");
    }
}
