using System;
using System.Linq;
using System.Text;

using RoslynAnalysis.Convert.AnalysisToJava;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertProperty
{
    /// <summary>
    /// Java属性均使用@Data注解，所以定义的字段都是private的
    /// </summary>
    /// <param name="propertyNode"></param>
    /// <param name="indent"></param>
    /// <returns></returns>
    public static string GenerateCode(PropertyDeclarationSyntax propertyNode, int indent = 0)
    {
        propertyNode = new PropertyRewriter().Visit(propertyNode) as PropertyDeclarationSyntax;

        var sbdr = new StringBuilder(propertyNode.Span.End);
        sbdr.Append(ConvertComment.GenerateDeclareCommennt(propertyNode, indent));
        if (propertyNode.AttributeLists.Count > 0)
        {
            sbdr.AppendLine(propertyNode.AttributeLists.ExpandAndToString(attr => attr.ToString().TrimStart('[').TrimEnd(']'), "\n" + "".PadIndented(indent)).PadIndented(indent));
        }
        sbdr.Append("private ".PadIndented(indent));
        sbdr.Append(propertyNode.Type);
        sbdr.Append(' ');
        sbdr.Append(propertyNode.Identifier.ValueText);
        sbdr.Append(';');

        return sbdr.ToString();
    }
}