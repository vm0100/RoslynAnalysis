using System.Text;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertArgument
{
    public static string GenerateCode(ArgumentListSyntax argumentListSyntax, int indent = 0)
    {
        return string.Join(", ", argumentListSyntax?.Arguments.Select(argSyntax =>
        {
            return GenerateCode(argSyntax, indent);
        }));
    }

    public static string GenerateCode(ArgumentSyntax argumentSyntax, int indent = 0)
    {
        return ConvertComment.GenerateBeforeComment(argumentSyntax) + ConvertInvoke.GenerateCode(argumentSyntax.Expression, indent) + ConvertComment.GenerateAfterComment(argumentSyntax);
    }
}