using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TipMolde.ViewModel.Defaults;

/// <summary>
/// Centraliza normalizacao e validacao dos formularios de fornecedor.
/// </summary>
public static class FornecedorFormDefaults
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);
    private static readonly EmailAddressAttribute EmailValidator = new();
    private static readonly Regex NifRegex = new(@"^\d{9}$", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex TelefoneRegex = new(@"^\+?\d+$", RegexOptions.Compiled, RegexTimeout);

    /// <summary>
    /// Normaliza um campo opcional removendo espacos redundantes.
    /// </summary>
    /// <param name="value">Valor recebido do formulario.</param>
    /// <returns>Valor limpo ou nulo quando o input vem vazio.</returns>
    public static string? NormalizeOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Valida o nome introduzido para o fornecedor.
    /// </summary>
    /// <param name="nome">Nome a validar.</param>
    /// <returns>Mensagem de erro quando invalido; nulo quando o valor e aceite.</returns>
    public static string? ValidateNome(string nome)
    {
        return nome.Length < 3 || nome.Length > 100
            ? "O nome do fornecedor deve ter entre 3 e 100 caracteres."
            : null;
    }

    /// <summary>
    /// Valida o NIF introduzido para o fornecedor.
    /// </summary>
    /// <param name="nif">NIF a validar.</param>
    /// <returns>Mensagem de erro quando invalido; nulo quando o valor e aceite.</returns>
    public static string? ValidateNif(string nif)
    {
        return !NifRegex.IsMatch(nif)
            ? "O NIF do fornecedor deve ter exatamente 9 digitos."
            : null;
    }

    /// <summary>
    /// Valida a morada introduzida para o fornecedor.
    /// </summary>
    /// <param name="morada">Morada a validar.</param>
    /// <returns>Mensagem de erro quando invalida; nulo quando o valor e aceite.</returns>
    public static string? ValidateMorada(string? morada)
    {
        if (morada is null)
            return null;

        return morada.Length < 5 || morada.Length > 255
            ? "A morada do fornecedor deve ter entre 5 e 255 caracteres."
            : null;
    }

    /// <summary>
    /// Valida o email do fornecedor.
    /// </summary>
    /// <param name="email">Email a validar.</param>
    /// <returns>Mensagem de erro quando invalido; nulo quando o valor e aceite.</returns>
    public static string? ValidateEmail(string? email)
    {
        if (email is null)
            return null;

        return email.Length > 100 || !EmailValidator.IsValid(email)
            ? "O email introduzido para o fornecedor nao e valido."
            : null;
    }

    /// <summary>
    /// Valida o telefone do fornecedor.
    /// </summary>
    /// <param name="telefone">Telefone a validar.</param>
    /// <returns>Mensagem de erro quando invalido; nulo quando o valor e aceite.</returns>
    public static string? ValidateTelefone(string? telefone)
    {
        if (telefone is null)
            return null;

        return telefone.Length < 7 || telefone.Length > 20 || !TelefoneRegex.IsMatch(telefone)
            ? "O telefone do fornecedor deve conter apenas digitos e um '+' opcional no inicio."
            : null;
    }
}
