using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace TipMolde.ViewModel.Defaults;

internal static class UtilizadorDefaults
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);
    private const string UppercasePasswordChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowercasePasswordChars = "abcdefghijkmnopqrstuvwxyz";
    private const string DigitPasswordChars = "23456789";
    private const string SymbolPasswordChars = "!@$?_-";
    private static readonly string SuggestedPasswordChars =
        string.Concat(UppercasePasswordChars, LowercasePasswordChars, DigitPasswordChars, SymbolPasswordChars);

    private static readonly Regex NomeRegex = new(
    @"^[A-ZÁÀÂÃÉÈÊÍÌÎÓÒÔÕÚÙÛÇ][a-záàâãéèêíìîóòôõúùûç]+(?: [A-ZÁÀÂÃÉÈÊÍÌÎÓÒÔÕÚÙÛÇ][a-záàâãéèêíìîóòôõúùûç]+)*$",
    RegexOptions.Compiled,
    RegexTimeout);

    private static readonly Regex EmailRegex = new(
        @"^[A-Za-z0-9]+@[A-Za-z0-9]+\.[A-Za-z]{2,}$",
        RegexOptions.Compiled,
        RegexTimeout);

    public static IReadOnlyList<string> AvailableRoles { get; } = new[]
    {
        "ADMIN",
        "GESTOR_COMERCIAL",
        "GESTOR_DESENHO",
        "GESTOR_PRODUCAO"
    };

    public static string CreateSuggestedPassword(int length = 16)
    {
        const int minimumLength = 8;

        if (length < minimumLength)
            throw new ArgumentOutOfRangeException(nameof(length), "A password sugerida deve ter pelo menos 8 caracteres.");

        var password = new char[length];
        password[0] = GetRandomChar(UppercasePasswordChars);
        password[1] = GetRandomChar(LowercasePasswordChars);
        password[2] = GetRandomChar(DigitPasswordChars);
        password[3] = GetRandomChar(SymbolPasswordChars);

        for (var i = 4; i < password.Length; i++)
            password[i] = GetRandomChar(SuggestedPasswordChars);

        Shuffle(password);
        return new string(password);
    }

    public static string? ValidatePassword(string password)
    {
        if (password.Length < 8)
            return "A password tem de ter pelo menos 8 caracteres.";

        if (!password.Any(char.IsUpper))
            return "A password tem de incluir pelo menos uma letra maiuscula.";

        if (!password.Any(char.IsLower))
            return "A password tem de incluir pelo menos uma letra minuscula.";

        if (!password.Any(char.IsDigit))
            return "A password tem de incluir pelo menos um numero.";

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            return "A password tem de incluir pelo menos um simbolo.";

        return null;
    }

    public static string? ValidateNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return "O nome e obrigatorio.";

        var totalLetras = nome.Count(char.IsLetter);

        if (totalLetras < 5)
            return "O nome tem de ter pelo menos 5 letras.";

        if (!NomeRegex.IsMatch(nome))
            return "O nome tem de comecar por maiuscula e cada palavra seguinte tambem. Ex.: Goncalo Barroso.";

        return null;
    }

    public static string? ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "O email e obrigatorio.";

        if (!EmailRegex.IsMatch(email))
            return "O email tem de ter o formato nome@dominio.xx.";

        return null;
    }

    private static char GetRandomChar(string source)
    {
        return source[RandomNumberGenerator.GetInt32(source.Length)];
    }

    private static void Shuffle(Span<char> value)
    {
        for (var i = value.Length - 1; i > 0; i--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(i + 1);
            (value[i], value[swapIndex]) = (value[swapIndex], value[i]);
        }
    }
}

