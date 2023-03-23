using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertType
{
    /// <summary>
    /// 传递类型语法，分析出具体类型
    /// </summary>
    /// <param name="typeNode"></param>
    /// <returns></returns>
    public static string GenerateCode(TypeSyntax typeNode) => (SyntaxKind)typeNode?.RawKind switch
    {
        // 无返回值
        SyntaxKind.VoidKeyword => "void",

        // 泛型
        SyntaxKind.GenericName => GenerateCode((GenericNameSyntax)typeNode),

        // 数组
        SyntaxKind.ArrayType => GenerateCode((ArrayTypeSyntax)typeNode),

        // 自定义返回值
        _ => GenerateType(typeNode)
    };

    public static string GenerateCode(GenericNameSyntax genericNameSyntax)
    {
        // 泛型标识类型
        var typeName = genericNameSyntax.Identifier.ValueText;
        // 泛型T的类型
        var genericType = string.Join(", ", genericNameSyntax.TypeArgumentList.Arguments.Select(GenerateCode));

        return $"{ConvertCommon.TypeToJava(genericNameSyntax)}<{genericType}>";
    }

    public static string GenerateCode(ArrayTypeSyntax arrayTypeSyntax)
    {
        return GenerateCode(arrayTypeSyntax.ElementType) + "[]";
    }

    public static string GenerateType(TypeSyntax typeSyntax)
    {
        return ConvertCommon.TypeToJava(typeSyntax);
    }
}