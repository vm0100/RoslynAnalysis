using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalysis.Core;

public static class SyntaxExtensions
{
    public static bool IsLiteralExpression(this SyntaxNode node)
    {
        return node != null && (
            node.IsKind(SyntaxKind.StringLiteralExpression) ||
            node.IsKind(SyntaxKind.NumericLiteralExpression) ||
            node.IsKind(SyntaxKind.CharacterLiteralExpression) ||
            node.IsKind(SyntaxKind.TrueLiteralExpression) ||
            node.IsKind(SyntaxKind.FalseLiteralExpression));
    }

    public static bool IsSingleLineDocumentationComment(this SyntaxTrivia node) => node.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia);

    public static bool IsLambdaExpressioon(this ExpressionSyntax exp)
        => exp.IsKind(SyntaxKind.SimpleLambdaExpression) || exp.IsKind(SyntaxKind.ParenthesizedLambdaExpression);

    public static bool IsBoolExpressioon(this ExpressionSyntax exp) => exp.IsKind(SyntaxKind.TrueLiteralExpression) || exp.IsKind(SyntaxKind.FalseLiteralExpression);

    public static bool IsMemberBinding(this ExpressionSyntax exp) => exp.IsKind(SyntaxKind.MemberBindingExpression);

    public static bool IsKinds(this SyntaxNode exp, params SyntaxKind[] kinds) => kinds.Any(k => exp.IsKind(k));

    public static bool IsCommentTrivia(this SyntaxTrivia syntax) => syntax.IsKind(SyntaxKind.SingleLineCommentTrivia) || syntax.IsKind(SyntaxKind.MultiLineCommentTrivia);
}