using System;
using System.Text;
using System.Xml.Linq;

using SyntaxExtensions = RoslynAnalysis.Core.SyntaxExtensions;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertComment
{
    public static string GenerateClassComment(SyntaxTrivia commentNode, int indent = 0)
    {
        var sbdr = new StringBuilder(commentNode.Span.End);

        sbdr.AppendLine("/**".PadIndented(indent));
        sbdr.AppendLine(" * @author ".PadIndented(indent));
        sbdr.AppendLine($" * @Description: {AnalysisToComment(commentNode.ToFullString(), indent).Trim().TrimStart('*')}".PadIndented(indent));
        sbdr.AppendLine($" * @date: {DateTime.Now:yyyy-MM-dd}".PadIndented(indent));
        sbdr.AppendLine(" */".PadIndented(indent));
        return sbdr.ToString();
    }

    public static string GenerateDeclareCommennt(SyntaxTrivia commentNode, int indent = 0)
    {
        var sbdr = new StringBuilder(1000);
        sbdr.AppendLine("/**".PadIndented(indent));
        sbdr.AppendLine(AnalysisToComment(commentNode.ToFullString(), indent).PadIndented(indent));
        sbdr.AppendLine(" */".PadIndented(indent));
        return sbdr.ToString();
    }


    public static string GenerateDeclareCommennt(SyntaxTriviaList commentNode, int indent = 0)
    {
        var sbdr = new StringBuilder(1000);
        sbdr.AppendLine("/**".PadIndented(indent));
        sbdr.AppendLine(AnalysisToComment(commentNode.ToFullString(), indent).PadIndented(indent));
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
        return string.Join("\n", syntaxNode.GetLeadingTrivia().Where(SyntaxExtensions.IsComment).Select(item => item.ToString().PadIndented(indent)));
    }

    public static string GenerateAfterComment(CSharpSyntaxNode syntaxNode, int indent = 0)
    {
        return string.Join("\n", syntaxNode.GetTrailingTrivia().Where(SyntaxExtensions.IsComment).Select(item => item.ToString().PadIndented(indent)));
    }
}