﻿using System;
using System.Linq;
using System.Text;

using RoslynAnalysis.Convert.AnalysisToJava;

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
        typeNode = TypeRewriter.Build(typeNode).Rewriter();

        return typeNode.ToString();
    }
}