﻿using System;
using System.Linq;
using System.Text;

namespace RoslynAnalysis.Convert.AnalysisToJava;

public class FunctionRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitBlock(BlockSyntax node)
    {
        return base.Visit(new FunctionBodyRewriter().Visit(node));
    }
}