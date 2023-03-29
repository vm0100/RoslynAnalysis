using System;

using RoslynAnalysis.Convert.Rewriter;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertField
{
    /// <summary>
    /// 类的字段
    /// </summary>
    /// <param name="fieldNode"></param>
    /// <param name="indent"></param>
    /// <returns></returns>
    public static string GenerateCode(FieldDeclarationSyntax fieldNode, int indent = 0)
    {
        var comment = ConvertComment.GenerateDeclareCommennt(fieldNode.GetTrailingTrivia(), indent);
        var attribute = string.Join("\n", fieldNode.AttributeLists.SelectMany(attrList => attrList.Attributes.Select(attr => "@" + attr.Name.ToString() + (attr.ArgumentList == null || attr.ArgumentList.Arguments.Count < 1 ? "" : "(" + string.Join(", ", attr.ArgumentList.Arguments.Select(arg => ConvertInvoke.GenerateCode(arg.Expression))) + ")"))));
        if (attribute.IsNotNullOrWhiteSpace())
        {
            attribute = attribute.PadIndented(indent) + "\n";
        }

        var modifier = $"{string.Join(" ", fieldNode.Modifiers.Select(ConvertCommon.KeywordToJava).Where(StringExtensions.IsNotNullOrWhiteSpace).ToList())}{(fieldNode.Modifiers.Any() ? " " : "")}";
        var type = ConvertType.GenerateCode(fieldNode.Declaration.Type);

        if (modifier.IsNotNullOrWhiteSpace())
        {
            modifier = modifier.PadIndented(indent);
        }
        else
        {
            type = type.PadIndented(indent);
        }

        return string.Join("\n", fieldNode.Declaration.Variables.Select(variableSyntax =>
        {
            var fieldName = variableSyntax.Identifier.ValueText.TrimStart('_');
            var initializer = variableSyntax.Initializer == null ? ";" : $" = {ConvertInvoke.GenerateCode(variableSyntax.Initializer.Value, indent)};";
            return $"{comment}{attribute}{modifier}{type} {fieldName}{initializer}";
        }));
    }

    /// <summary>
    /// 直接翻译字段
    /// </summary>
    /// <param name="fieldNode"></param>
    /// <param name="indent"></param>
    /// <returns></returns>
    public static string GenerateCode(LocalDeclarationStatementSyntax fieldNode, int indent = 0)
    {
        return GenerateCode(SyntaxFactory.FieldDeclaration(fieldNode.AttributeLists, fieldNode.Modifiers, fieldNode.Declaration, fieldNode.SemicolonToken), indent);
    }
}