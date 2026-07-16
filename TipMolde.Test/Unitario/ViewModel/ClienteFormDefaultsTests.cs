using FluentAssertions;
using NUnit.Framework;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.Test.Unitario.ViewModel;

/// <summary>
/// Testes unitarios das regras de validacao base do formulario de clientes.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ClienteFormDefaultsTests
{
    [Test(Description = "T1FRT - Os campos opcionais devem ser normalizados com trim e nulos quando vazios.")]
    public void NormalizeOptional_Should_TrimValueOrReturnNull_When_InputIsOptional()
    {
        ClienteFormDefaults.NormalizeOptional("  Portugal  ").Should().Be("Portugal");
        ClienteFormDefaults.NormalizeOptional("   ").Should().BeNull();
    }

    [Test(Description = "T2FRT - O pais deve respeitar a capitalizacao funcional esperada.")]
    public void ValidatePais_Should_ReturnMessage_When_CapitalizationIsInvalid()
    {
        var result = ClienteFormDefaults.ValidatePais("portugal");

        result.Should().Be("O pais deve comecar por maiuscula e manter capitalizacao normal, por exemplo 'Portugal' ou 'Estados Unidos'.");
    }

    [Test(Description = "T3FRT - O email deve ser rejeitado quando nao cumpre o formato esperado.")]
    public void ValidateEmail_Should_ReturnMessage_When_EmailIsInvalid()
    {
        var result = ClienteFormDefaults.ValidateEmail("cliente-inválido");

        result.Should().Be("O email introduzido não é válido.");
    }

    [Test(Description = "T4FRT - O telefone deve aceitar apenas digitos e um mais opcional no inicio.")]
    public void ValidateTelefone_Should_AcceptInternationalFormat_When_ValueIsValid()
    {
        var result = ClienteFormDefaults.ValidateTelefone("+351912345678");

        result.Should().BeNull();
    }
}
