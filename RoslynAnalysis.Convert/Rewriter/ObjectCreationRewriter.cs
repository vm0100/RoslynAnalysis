using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace RoslynAnalysis.Convert.Rewriter;

public class ObjectCreationRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        if (node.Type.Iskind(SyntaxKind.GenericName))
        {
            var genericTypeName = (node.Type as GenericNameSyntax).Identifier.ValueText;

            if (genericTypeName == "List")
            {
                return RewriterList(node);
            }

            if (genericTypeName == "Dictionary")
            {
                return RewriterEmptyDictionary(node);
            }
        }

        node = node.WithType(new TypeRewriter().Visit(node.Type) as TypeSyntax);

        return base.VisitObjectCreationExpression(node);
    }

    public override SyntaxNode VisitGenericName(GenericNameSyntax node)
    {
        return new TypeRewriter().Visit(node);
    }


    public SyntaxNode RewriterList(ObjectCreationExpressionSyntax node)
    {
        // new List<string>() { "", "", ""};转换为 Lists.newArrayList("", "", "");
        var listsNewArrayMember = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("Lists"),
            SyntaxFactory.IdentifierName("newArrayList"));

        SyntaxToken separator = SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space);
        var argumentList = node.Initializer?.Expressions.Select(SyntaxFactory.Argument) ?? new List<ArgumentSyntax>();
        var separatorList = SyntaxFactory.TokenList(Enumerable.Range(0, (node.Initializer?.Expressions.Count ?? 1) - 1).Select(i => separator));

        var invocationExp = SyntaxFactory.InvocationExpression(listsNewArrayMember,
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(argumentList, separatorList)));

        return base.VisitInvocationExpression(invocationExp);
    }

    public SyntaxNode RewriterEmptyDictionary(ObjectCreationExpressionSyntax node)
    {
        if (node.Initializer != null && node.Initializer.Expressions.Count > 0)
        {
            var genericType = node.Type as GenericNameSyntax;
            node = node.WithType(genericType.WithIdentifier(SyntaxFactory.Identifier("HashMap")));
            return base.VisitObjectCreationExpression(node);
        }

        var dictNewArrayMember = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("Maps"),
            SyntaxFactory.IdentifierName("newHashMap"));

        var invocationExp = SyntaxFactory.InvocationExpression(dictNewArrayMember, SyntaxFactory.ArgumentList());

        return base.VisitInvocationExpression(invocationExp);
    }

    public override SyntaxNode VisitInitializerExpression(InitializerExpressionSyntax node)
    {
        if (node.Expressions.Count < 1 || node.Parent.IsKind(SyntaxKind.CollectionInitializerExpression))
        {
            return base.VisitInitializerExpression(node);
        }

        var closeToken = SyntaxFactory.Token(SyntaxFactory.TriviaList(),
                                             SyntaxKind.CloseParenToken,
                                             SyntaxFactory.TriviaList(
                                                 SyntaxFactory.Trivia(
                                                     SyntaxFactory.SkippedTokensTrivia().WithTokens(
                                                         SyntaxFactory.TokenList(
                                                             SyntaxFactory.Token(SyntaxKind.SemicolonToken))))));

        var elmInitExpList = node.Expressions.OfType<InitializerExpressionSyntax>().Select(initExp =>
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.IdentifierName("put"))
                                                 .WithArgumentList(
                                                        SyntaxFactory.ArgumentList(
                                                            SyntaxFactory.SeparatedList(
                                                                initExp.Expressions.Select(SyntaxFactory.Argument)))
                                                 ).WithLeadingTrivia(SyntaxFactory.Space)).ToList();

        var elmInitCloseTokenList = Enumerable.Range(0, elmInitExpList.Count).Select(i => SyntaxFactory.Token(SyntaxKind.SemicolonToken)).ToList();
        if (elmInitCloseTokenList.Count > 0)
        {
            elmInitCloseTokenList[elmInitCloseTokenList.Count - 1] = elmInitCloseTokenList.Last().WithTrailingTrivia(SyntaxFactory.Space);
        }

        var newNode = node.WithExpressions(
            SyntaxFactory.SeparatedList<ExpressionSyntax>(
                SyntaxFactory.SingletonList(
                    SyntaxFactory.InitializerExpression(
                    SyntaxKind.ComplexElementInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(elmInitExpList, elmInitCloseTokenList)))));

        return base.VisitInitializerExpression(newNode);
    }
}