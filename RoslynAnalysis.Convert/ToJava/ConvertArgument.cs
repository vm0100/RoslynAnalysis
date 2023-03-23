using System.Text;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertArgument
{
    public static string GenerateCode(ArgumentListSyntax argumentListSyntax, int indent = 0)
    {
        if (argumentListSyntax == null || argumentListSyntax.Arguments.Count < 1)
        {
            return string.Empty;
        }

        return string.Join(", ", argumentListSyntax.Arguments.Select(argSyntax =>
        {
            return GenerateCode(argSyntax, indent);
        }));
    }

    public static string GenerateCode(ArgumentSyntax argumentSyntax, int indent = 0)
    {
        return ConvertComment.GenerateBeforeComment(argumentSyntax) + ConvertInvoke.GenerateCode(argumentSyntax.Expression, indent) + ConvertComment.GenerateAfterComment(argumentSyntax);
    }
}