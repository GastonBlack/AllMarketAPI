using System.Globalization;

namespace AllMarket.Helpers.Formatting;

public static class NameFormatting
{
    public static string NormalizeString(string rawString)
    {
        if (string.IsNullOrWhiteSpace(rawString))
            throw new ArgumentException("Value cannot be empty.");

        string cleanString = string.Join(" ", rawString.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        var lowerValue = cleanString.ToLowerInvariant();

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(lowerValue);
    }
}

public static class NumberFormatting
{
    public static string RemoveNumberSpaces(string rawNumber)
    {
        return rawNumber.Replace(" ", "");
    }
}
