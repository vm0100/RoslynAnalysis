using System;
using System.Linq;
using System.Text;

namespace RoslynAnalysis.Convert.Rewriter;

public partial class CSharpToJavaRewriter
{
    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var newNode = node;
        newNode = newNode.WithModifiers(SyntaxFactory.TokenList(newNode.Modifiers.Where(m => m.IsKind(SyntaxKind.VirtualKeyword))));

        return base.VisitMethodDeclaration(newNode);
    }

    public override SyntaxNode VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        var newNode = node;
        newNode = newNode.WithModifiers(SyntaxFactory.TokenList(newNode.Modifiers.Where(m => m.IsKind(SyntaxKind.VirtualKeyword))));
        return base.VisitLocalFunctionStatement(newNode);
    }
}