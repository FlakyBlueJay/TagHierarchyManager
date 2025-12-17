using System.Globalization;
using System.Text;

namespace TagHierarchyManager.Utilities;

/// <summary>
///     A utility class for normalising strings for the search handler.
/// </summary>
public static class StringNormaliser
{
    /// <summary>
    ///     Strips out any diacritics from the input so it can be searched with regular Latin alphabet keys.
    /// </summary>
    /// <param name="input">The string to be normalised.</param>
    /// <returns><paramref name="input" /> with the diacritics stripped.</returns>
    public static string FormatStringForSearch(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string decomposedString = input.Normalize(NormalizationForm.FormD);

        StringBuilder sb = new();
        foreach (char c in decomposedString.Where(c =>
                     CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)) sb.Append(c);

        return sb.ToString().ToLowerInvariant();
    }
}