using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace RoslynAnalysis.Convert.AnalysisToJava;

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

        return base.VisitObjectCreationExpression(node);
    }


    public SyntaxNode RewriterList(ObjectCreationExpressionSyntax node)
    {
        // new List<string>() { "", "", ""};转换为 Lists.newArrayList("", "", "");
        var listsNewArrayMember = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("Lists"),
            SyntaxFactory.IdentifierName("newArrayList"));

        var invocationExp = SyntaxFactory.InvocationExpression(listsNewArrayMember,
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(
                                        node.Initializer?.Expressions.Select(SyntaxFactory.Argument))));

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
}