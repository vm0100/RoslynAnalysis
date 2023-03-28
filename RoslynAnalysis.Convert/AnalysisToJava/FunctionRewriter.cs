using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalysis.Convert.AnalysisToJava;

public class FunctionRewriter : RewriterBase<MethodDeclarationSyntax>
{
    private BlockSyntax _blockSyntax;

    public FunctionRewriter(MethodDeclarationSyntax methodNode) : base(methodNode)
    {
        _blockSyntax = methodNode.Body;
    }

    public static FunctionRewriter Build(MethodDeclarationSyntax methodNode) => new FunctionRewriter(methodNode);

    public override MethodDeclarationSyntax Rewriter()
    {
        if(_blockSyntax != null)
        {
            _declaration = _declaration.WithBody(FunctionBodyRewriter.Build(_blockSyntax).Rewriter());
        }

        // 表达式主体函数
        // _declaration.WithExpressionBody

        return base.Rewriter();
    }



}
