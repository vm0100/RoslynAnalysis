using System;
using System.Text;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SyntaxExtensions = RoslynAnalysis.Core.SyntaxExtensions;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertComment
{
    public static string GenerateTypeDeclareComment(TypeDeclarationSyntax classNode, int indent = 0)
    {
        if (!classNode.HasLeadingTrivia)
        {
            return string.Empty;
        }

        var sbdr = new StringBuilder(1000);

        sbdr.AppendLine("/**".PadIndented(indent));
        sbdr.AppendLine(" * @author ".PadIndented(indent));
        var documentComment = classNode.GetLeadingTrivia().FirstOrDefault(SyntaxExtensions.IsSingleLineDocumentationComment).ToString();
        sbdr.AppendLine($" * @Description: {AnalysisToComment(documentComment).Replace("* ", "").Trim()}".PadIndented(indent));
        sbdr.AppendLine($" * @date: {DateTime.Now:yyyy-MM-dd}".PadIndented(indent));
        sbdr.AppendLine(" */".PadIndented(indent));
        return sbdr.ToString();
    }

    public static string GenerateDeclareCommennt(SyntaxNode syntaxNode, int indent = 0)
    {
        if (syntaxNode == null || !syntaxNode.HasLeadingTrivia)
        {
            return string.Empty;
        }
        var syntaxTriviaList = syntaxNode.GetLeadingTrivia();
        if (!syntaxTriviaList.Any(SyntaxExtensions.IsSingleLineDocumentationComment))
        {
            return string.Empty;
        }

        var sbdr = new StringBuilder(1000);
        sbdr.AppendLine("/**".PadIndented(indent));
        sbdr.AppendLine(AnalysisToComment(syntaxTriviaList.FirstOrDefault(SyntaxExtensions.IsSingleLineDocumentationComment).ToString(), indent));
        sbdr.AppendLine(" */".PadIndented(indent));
        return sbdr.ToString();
    }

    public static string AnalysisToComment(string comment, int indent = 0)
    {
        if (comment.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        try
        {
            var xElm = XElement.Parse($"<root>{comment.Replace("///", "")}</root>");

            return string.Join("\n", xElm.Elements().Select(node => (" * " + (node.Name.ToString() switch
            {
                "summary" => node.Value.Trim(),
                "param" => $"@param {node.Attribute("name").Value?.ToLowerTitleCase()} {node.Value?.Trim()}",
                "returns" => $"@return {node.Value?.Trim()}",
                _ => node.Value.Trim()
            })).PadIndented(indent)));
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public static string GenerateBeforeComment(CSharpSyntaxNode syntaxNode, int indent = 0)
    {
        return string.Join("\n", syntaxNode.GetLeadingTrivia().Where(SyntaxExtensions.IsCommentTrivia).Select(item => item.ToString().PadIndented(indent)));
    }

    public static string GenerateAfterComment(CSharpSyntaxNode syntaxNode, int indent = 0)
    {
        return string.Join("\n", syntaxNode.GetTrailingTrivia().Where(SyntaxExtensions.IsCommentTrivia).Select(item => item.ToString().PadIndented(indent)));
    }
}