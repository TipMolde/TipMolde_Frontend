using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TipMolde.ViewModel.Defaults;

/// <summary>
/// Centraliza normalizacao e validacao dos formularios de cliente.
/// </summary>
public static class ClienteFormDefaults
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);
    private static readonly CultureInfo PtPtCulture = CultureInfo.GetCultureInfo("pt-PT");
    private static readonly EmailAddressAttribute EmailValidator = new();
    private static readonly Regex TelefoneRegex = new(@"^\+?\d+$", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex PaisCaracteresRegex = new(@"^[\p{L}]+(?:[ '\-][\p{L}]+)*$", RegexOptions.Compiled, RegexTimeout);

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
    /// Valida o nome introduzido para o cliente.
    /// </summary>
    /// <param name="nome">Nome a validar.</param>
    /// <returns>Mensagem de erro quando invalido; nulo quando o valor e aceite.</returns>
    public static string? ValidateNome(string nome)
    {
        return nome.Length < 3 || nome.Length > 100
            ? "O nome do cliente deve ter entre 3 e 100 caracteres."
            : null;
    }

    /// <summary>
    /// Valida o NIF introduzido para o cliente.
    /// </summary>
    /// <param name="nif">NIF a validar.</param>
    /// <returns>Mensagem de erro quando invalido; nulo quando o valor e aceite.</returns>
    public static string? ValidateNif(string nif)
    {
        if (nif.Length != 9 || !nif.All(char.IsDigit))
            return "O NIF deve ter exatamente 9 digitos.";

        return null;
    }

    /// <summary>
    /// Valida a sigla funcional do cliente.
    /// </summary>
    /// <param name="sigla">Sigla a validar.</param>
    /// <returns>Mensagem de erro quando invalida; nulo quando o valor e aceite.</returns>
    public static string? ValidateSigla(string sigla)
    {
        return sigla.Length < 2 || sigla.Length > 10
            ? "A sigla deve ter entre 2 e 10 caracteres."
            : null;
    }

    /// <summary>
    /// Valida o pais introduzido para o cliente.
    /// </summary>
    /// <param name="pais">Pais a validar.</param>
    /// <returns>Mensagem de erro quando invalido; nulo quando o valor e aceite.</returns>
    public static string? ValidatePais(string? pais)
    {
        if (pais is null)
            return null;

        if (pais.Length > 50)
            return "O pais nao pode ter mais de 50 caracteres.";

        if (!PaisCaracteresRegex.IsMatch(pais))
            return "O pais so pode conter letras, espacos, apostrofos e hifens.";

        var normalized = NormalizePais(pais);
        if (!string.Equals(pais, normalized, StringComparison.Ordinal))
            return "O pais deve comecar por maiuscula e manter capitalizacao normal, por exemplo 'Portugal' ou 'Estados Unidos'.";

        return null;
    }

    /// <summary>
    /// Valida o email do cliente.
    /// </summary>
    /// <param name="email">Email a validar.</param>
    /// <returns>Mensagem de erro quando invalido; nulo quando o valor e aceite.</returns>
    public static string? ValidateEmail(string? email)
    {
        if (email is null)
            return null;

        if (email.Length > 100)
            return "O email nao pode ter mais de 100 caracteres.";

        if (!EmailValidator.IsValid(email))
            return "O email introduzido nao e valido.";

        return null;
    }

    /// <summary>
    /// Valida o telefone do cliente.
    /// </summary>
    /// <param name="telefone">Telefone a validar.</param>
    /// <returns>Mensagem de erro quando invalido; nulo quando o valor e aceite.</returns>
    public static string? ValidateTelefone(string? telefone)
    {
        if (telefone is null)
            return null;

        if (telefone.Length < 7 || telefone.Length > 15)
            return "O telefone deve ter entre 7 e 15 caracteres, contando ja com o '+'.";

        if (!TelefoneRegex.IsMatch(telefone))
            return "O telefone so pode conter digitos e um '+' opcional apenas no inicio.";

        return null;
    }

    private static string NormalizePais(string pais)
    {
        return PtPtCulture.TextInfo.ToTitleCase(pais.ToLower(PtPtCulture));
    }
}

