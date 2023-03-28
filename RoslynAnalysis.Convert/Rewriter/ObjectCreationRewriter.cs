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

        var invocationExp = SyntaxFactory.InvocationExpression(listsNewArrayMember,
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(
                                        node.Initializer?.Expressions.Select(SyntaxFactory.Argument), 
                                        Enumerable.Range(0, node.Initializer?.Expressions.Count - 1 ?? 0).Select(i => separator))));

        return base.VisitInvocationExpression(invocationExp);
    }

    public SyntaxNode RewriterEmptyDictionary(ObjectCreationExpressionSyntax node)
    {
        if (node.Initializer != null || node.Initializer.Expressions.Count > 0)
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
        // 如果类型是HashMap，重写为put(xxx)
        return base.VisitInitializerExpression(node);
    }
}