using System.Text;

namespace Content.Arcane.Common.Localization;

public static class RomanNumerals
{
    private static readonly (int Value, string Symbol)[] Symbols =
    [
        (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
        (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
        (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
    ];

    /// <summary>
    /// Roman numeral for a positive number, empty for zero or less.
    /// </summary>
    public static string ToRoman(int number)
    {
        var result = new StringBuilder();
        foreach (var (value, symbol) in Symbols)
        {
            while (number >= value)
            {
                result.Append(symbol);
                number -= value;
            }
        }

        return result.ToString();
    }
}
