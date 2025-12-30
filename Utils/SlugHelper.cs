using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MissaoBackend.Utils;

public static class SlugHelper
{
    public static string Slugify(string phrase)
    {
        string str = phrase.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in str)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var cleaned = sb.ToString().Normalize(NormalizationForm.FormC);
        cleaned = Regex.Replace(cleaned, @"[^\w\s-]", "");
        cleaned = Regex.Replace(cleaned, @"\s+", "-").Trim('-');

        return cleaned;
    }
}
