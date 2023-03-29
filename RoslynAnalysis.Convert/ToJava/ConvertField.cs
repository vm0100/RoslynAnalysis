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
        var comment = ConvertComment.GenerateDeclareCommennt(fieldNode.GetTrailingTrivia()).PadIndented(indent);
        var attribute = string.Join("\n", fieldNode.AttributeLists.ExpandAndToString(attr => attr.ToString().TrimStart('[').TrimEnd(']'), "\n" + "".PadIndented(indent)));
        if (attribute.IsNotNullOrWhiteSpace())
        {
            attribute = attribute.PadIndented(indent) + "\n";
        }

        var modifier = $"{string.Join(" ", fieldNode.Modifiers.Select(ConvertCommon.KeywordToJava).Where(StringExtensions.IsNotNullOrWhiteSpace).ToList())}{(fieldNode.Modifiers.Any() ? " " : "")}";
        var type = fieldNode.Declaration.Type.ToString();

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
            return $"{comment}{attribute}{modifier}{type} {fieldName}{variableSyntax.Initializer?.ToString()}";
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
        return GenerateCode(SyntaxFactory.FieldDeclaration(fieldNode.AttributeLists, fieldNode.Modifiers, fieldNode.Declaration, fieldNode.SemicolonToken)
                                         .WithLeadingTrivia(fieldNode.GetLeadingTrivia()), indent);
    }
}