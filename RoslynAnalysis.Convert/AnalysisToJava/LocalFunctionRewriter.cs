using System;
using System.Linq;
using System.Text;

namespace RoslynAnalysis.Convert.AnalysisToJava;

public class LocalFunctionRewriter : RewriterBase<LocalFunctionStatementSyntax>
{
    private BlockSyntax _blockSyntax;

    public LocalFunctionRewriter(LocalFunctionStatementSyntax methodNode) : base(methodNode)
    {
        _blockSyntax = methodNode.Body;
    }

    public static LocalFunctionRewriter Build(LocalFunctionStatementSyntax methodNode) => new LocalFunctionRewriter(methodNode);

    public override LocalFunctionStatementSyntax Rewriter()
    {
        if (_blockSyntax != null)
        {
            _declaration = _declaration.WithBody(FunctionBodyRewriter.Build(_blockSyntax).Rewriter());
        }

        // 表达式主体函数
        // _declaration.WithExpressionBody

        return base.Rewriter();
    }
}