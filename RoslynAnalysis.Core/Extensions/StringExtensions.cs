using System;
using System.Linq;
using System.Text;

namespace RoslynAnalysis.Core;

public static class StringExtensions
{
    public static string PadIndented(this string str, int indentCount)
    {
        return "".PadLeft(indentCount * 4, ' ') + str;
    }

    public static string ToUpperTitleCase(this string str)
        => str.ToTitleCase(char.ToUpper);

    public static string ToLowerTitleCase(this string str)
        => (str ?? string.Empty).ToTitleCase(char.ToLower);

    public static string ToTitleCase(this string str, Func<char, char> func)
    {
        if (str.IsNullOrWhiteSpace())
            return str;
        if (str.Length < 1)
            return str.ToLower();

        return func(str[0]) + str[1..];
    }

    public static bool IsNullOrWhiteSpace(this string str)
        => string.IsNullOrWhiteSpace(str);

    public static bool IsNotNullOrWhiteSpace(this string str)
        => str.IsNullOrWhiteSpace() == false;

    public static string NotNullWhiteSpaceOrElse(this string str, string elseStr)
        => str.IsNullOrWhiteSpace() ? string.Empty : elseStr;

    public static string NullWhiteSpaceOrElse(this string str, string elseStr)
        => str.IsNullOrWhiteSpace() ? elseStr : str;

    public static bool IsUpper(this string str) => str.Any(c => c >= 'A' && c <= 'Z');

    public static bool In(this string str, params string[] strArr) => strArr.Contains(str);

    public static bool NotIn(this string str, params string[] strArr) => str.In(strArr) == false;

    public static bool InContains(this string str, params string[] strArr) => strArr.Any(s => str.Contains(s));

    public static bool InStartsWith(this string str, params string[] strArr) => strArr.Any(s => str.StartsWith(s));

    public static bool InEndsWith(this string str, params string[] strArr) => strArr.Any(s => str.EndsWith(s));

    public static bool InEndsWithIgnoreCase(this string str, params string[] strArr) => strArr.Any(s => str.EndsWith(s, StringComparison.OrdinalIgnoreCase));

    public static string TrimEnds(this string str, params string[] strArr)
    {
        while (str.InEndsWithIgnoreCase(strArr))
        {
            str = strArr.Aggregate(str, (str, trimStr) => { return str.EndsWith(trimStr, StringComparison.OrdinalIgnoreCase) ? str.Substring(0, str.Length - trimStr.Length) : str; });
        }

        return str;
    }

    public static string GetFileNameWithoutExtension(this string str) => Path.GetFileNameWithoutExtension(str);

    public static bool IsEndsWithDate(this string str)
    {
        var isDate = str.InEndsWithIgnoreCase("Date", "Now");
        if (isDate)
        {
            return isDate;
        }

        if (str.EndsWith(")") == false)
        {
            return false;
        }

        var lastLeftBracketIdx = str.LastIndexOf('(');
        if (lastLeftBracketIdx < 4)
        {
            return false;
        }

        return str.Substring(lastLeftBracketIdx - 4, 4).InEndsWithIgnoreCase("Date", "Now");
    }

    public static bool IsInterfaceDeclare(this string typeName)
        => !typeName.IsNullOrWhiteSpace() && typeName.Length > 1 && typeName.StartsWith('I') && char.IsUpper(typeName[1]);

}