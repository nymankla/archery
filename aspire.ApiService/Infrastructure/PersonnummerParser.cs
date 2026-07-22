using System.Globalization;
using System.Text.RegularExpressions;

namespace aspire.ApiService.Infrastructure;

public static partial class PersonnummerParser
{
    public static Result<string?> Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<string?>.Success(null);

        var trimmed = input.Trim();
        if (!TryExtractParts(trimmed, out var birthDate, out var serial))
            return Result<string?>.Failure("Personnummer is invalid.");

        var digits = birthDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + serial;
        if (!HasValidChecksum(digits))
            return Result<string?>.Failure("Personnummer is invalid.");

        return Result<string?>.Success(digits);
    }

    static bool TryExtractParts(string input, out DateOnly birthDate, out string serial)
    {
        birthDate = default;
        serial = string.Empty;

        var match = PersonnummerRegex.Match(input);
        if (!match.Success)
            return false;

        var yearPart = match.Groups[1].Value;
        var month = match.Groups[2].Value;
        var day = match.Groups[3].Value;
        var separator = match.Groups[4].Value;
        serial = match.Groups[5].Value;

        int year;
        if (yearPart.Length == 4)
        {
            year = int.Parse(yearPart, CultureInfo.InvariantCulture);
        }
        else
        {
            var twoDigitYear = int.Parse(yearPart, CultureInfo.InvariantCulture);
            var currentYear = DateTime.Today.Year;
            var currentCentury = currentYear / 100;
            year = (currentCentury * 100) + twoDigitYear;

            if (separator == "+" || year > currentYear)
                year -= 100;
        }

        return DateOnly.TryParseExact(
            $"{year:D4}{month}{day}",
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out birthDate);
    }

    static bool HasValidChecksum(string digits)
    {
        if (digits.Length != 12 || !digits.All(char.IsDigit))
            return false;

        var luhnDigits = digits[2..];
        var sum = 0;

        for (var i = 0; i < luhnDigits.Length; i++)
        {
            var digit = luhnDigits[i] - '0';
            if (i % 2 == 0)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
        }

        return sum % 10 == 0;
    }

    private static readonly Regex PersonnummerRegex = MyRegex();

    [GeneratedRegex("^(\\d{2}|\\d{4})(\\d{2})(\\d{2})([-+]?)?(\\d{4})$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();


}
