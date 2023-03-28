using System;
using System.Linq;
using System.Text;

using RoslynAnalysis.Convert.ToJava;

namespace RoslynAnalysis.Convert.Rewriter;

public class TypeRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {
        return base.VisitIdentifierName(SyntaxFactory.IdentifierName(ConvertCommon.TypeToJava(node)));
    }
}