using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        var comment = ConvertComment.GenerateDeclareCommennt(propertyNode, indent);
        var type = ConvertType.GenerateCode(propertyNode.Type);

        var propName = propertyNode.Identifier.ValueText.ToLowerTitleCase();
        return comment + $"private {type} {propName};".PadIndented(indent);
    }
}