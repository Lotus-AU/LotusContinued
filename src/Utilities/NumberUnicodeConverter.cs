using System;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Lotus.Utilities;

public static class NumberUnicodeConverter
{
    private static readonly Regex TagOrTextRegex = new(@"<[^>]*>|[^<]+", RegexOptions.Compiled);


    public static string ConvertNumbersToUnicodes(string input)
    {
        return TagOrTextRegex.Replace(input, match =>match.Value[0] == '<' ? match.Value : ConvertDigitsToFullwidth(match.Value));
    }

    private static string ConvertDigitsToFullwidth(string text)
    {
        var sb = new StringBuilder(text.Length);

        foreach (char c in text)
            // ASCII '0' (0x30) -> Fullwidth '０' (0xFF10) (convert to unicode to bypass anticheat)
            if (c >= '0' && c <= '9') sb.Append((char)('０' + (c - '0')));
            else sb.Append(c);


        return sb.ToString();
    }
}