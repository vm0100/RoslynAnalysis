using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using RoslynAnalysis.Convert.ToJava;

namespace RoslynAnalysis.Convert.Rewriter;

public class TypeRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {
        return base.VisitIdentifierName(SyntaxFactory.IdentifierName(ConvertCommon.TypeToJava(node))).WithTrailingTrivia(node.GetTrailingTrivia());
    }

    public override SyntaxNode VisitPredefinedType(PredefinedTypeSyntax node)
    {
        return SyntaxFactory.IdentifierName(ConvertCommon.TypeToJava(node)).WithTrailingTrivia(node.GetTrailingTrivia());
    }

    public override SyntaxNode VisitGenericName(GenericNameSyntax node)
    {
        return base.VisitGenericName(node.WithIdentifier(SyntaxFactory.Identifier(ConvertCommon.TypeToJava(node))));
    }

    public override SyntaxNode VisitArrayType(ArrayTypeSyntax node)
    {
        node = node.WithElementType(SyntaxFactory.IdentifierName(ConvertCommon.TypeToJava(node.ElementType)));


        // TODO: 清理new int[6];中的6
        //if (node.RankSpecifiers.Count > 0)
        //{

        //    node = node.WithRankSpecifiers(SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier()));
        //}

        return base.VisitArrayType(node);
    }
}