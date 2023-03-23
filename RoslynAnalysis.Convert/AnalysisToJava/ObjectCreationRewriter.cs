using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using RoslynAnalysis.Convert.ToJava;

namespace RoslynAnalysis.Convert.AnalysisToJava;

public class ObjectCreationRewriter : RewriterBase<ObjectCreationExpressionSyntax>
{
    public ObjectCreationRewriter(ObjectCreationExpressionSyntax typeDeclaration) : base(typeDeclaration)
    {
    }

    public static ObjectCreationRewriter Build(ObjectCreationExpressionSyntax typeDeclaration) => new ObjectCreationRewriter(typeDeclaration);

    public new ExpressionSyntax Rewriter()
    {
        var type = _declaration.Type;
        if (type.Iskind(SyntaxKind.GenericName))
        {
            var genericTypeName = (type as GenericNameSyntax).Identifier.ValueText;

            if (genericTypeName == "List")
            {
                return RewriterList();
            }

            if (genericTypeName == "Dictionary")
            {
                return RewriterEmptyDictionary();
            }
        }

        return _declaration;
    }

    public ExpressionSyntax RewriterList()
    {
        // new List<string>() { "", "", ""};转换为 Lists.newArrayList("", "", "");
        var listsNewArrayMember = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("Lists"),
            SyntaxFactory.IdentifierName("newArrayList"));

        return SyntaxFactory.InvocationExpression(listsNewArrayMember,
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    _declaration.Initializer?.Expressions.Select(SyntaxFactory.Argument))));
    }

    public ExpressionSyntax RewriterEmptyDictionary()
    {
        if (_declaration.Initializer != null || _declaration.Initializer.Expressions.Count > 0)
        {
            var genericType = _declaration.Type as GenericNameSyntax;

            _declaration = _declaration.WithType(genericType.WithIdentifier(SyntaxFactory.Identifier("HashMap")));

            return _declaration;
        }

        var dictNewArrayMember = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("Maps"),
            SyntaxFactory.IdentifierName("newHashMap"));

        return SyntaxFactory.InvocationExpression(dictNewArrayMember, SyntaxFactory.ArgumentList());
    }
}