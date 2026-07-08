using FluentAssertions;
using NUnit.Framework;
using TipMolde.ViewModel.Defaults;

namespace TipMolde.Test.Unitario.ViewModel;

[TestFixture]
[Category("Unit")]
public class FornecedorFormDefaultsTests
{
    [Test(Description = "T1FORDFLT - Os campos opcionais do fornecedor devem ser normalizados com trim ou nulo quando vazios.")]
    public void NormalizeOptional_Should_TrimValueOrReturnNull_When_FieldIsOptional()
    {
        FornecedorFormDefaults.NormalizeOptional("  Rua Central  ").Should().Be("Rua Central");
        FornecedorFormDefaults.NormalizeOptional("   ").Should().BeNull();
    }

    [Test(Description = "T2FORDFLT - O NIF do fornecedor deve ser rejeitado quando nao tem 9 digitos.")]
    public void ValidateNif_Should_ReturnMessage_When_NifIsInvalid()
    {
        var result = FornecedorFormDefaults.ValidateNif("123");

        result.Should().Be("O NIF do fornecedor deve ter exatamente 9 digitos.");
    }

    [Test(Description = "T3FORDFLT - O telefone deve aceitar formato internacional com mais opcional no inicio.")]
    public void ValidateTelefone_Should_ReturnNull_When_InternationalNumberIsValid()
    {
        var result = FornecedorFormDefaults.ValidateTelefone("+351912345678");

        result.Should().BeNull();
    }

    [Test(Description = "T4FORDFLT - A morada deve falhar quando e demasiado curta para ser util.")]
    public void ValidateMorada_Should_ReturnMessage_When_AddressIsTooShort()
    {
        var result = FornecedorFormDefaults.ValidateMorada("Rua");

        result.Should().Be("A morada do fornecedor deve ter entre 5 e 255 caracteres.");
    }
}
