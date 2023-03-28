using System;
using System.Linq;
using System.Text;

using RoslynAnalysis.Convert.Rewriter;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertType
{
    /// <summary>
    /// 传递类型语法，分析出具体类型
    /// </summary>
    /// <param name="typeNode"></param>
    /// <returns></returns>
    public static string GenerateCode(TypeSyntax typeNode)
    {
        typeNode = new TypeRewriter().Visit(typeNode) as TypeSyntax;

        return typeNode.ToString();
    }
}