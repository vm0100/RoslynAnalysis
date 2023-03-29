using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using RoslynAnalysis.Convert.ToJava;

namespace RoslynAnalysis.Convert.Rewriter;

public partial class CSharpToJavaRewriter
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
        var newNode = node.WithElementType(SyntaxFactory.IdentifierName(ConvertCommon.TypeToJava(node.ElementType)));

        //int[] iArr = new int[0]; // 无初始化语句必须带size
        //int[] iArr1 = { 1, 3 };
        //int[] iArr2 = new int[] { 1, 3 }; // 有初始化语句带size会报错，必须移除
        if (node.RankSpecifiers.Any(r => r.Sizes.Any(s => s.IsKind(SyntaxKind.OmittedArraySizeExpression) == false)))
        {
            var newRankSpecifiers = SyntaxFactory.List<ArrayRankSpecifierSyntax>();
            foreach (var arrayRank in node.RankSpecifiers)
            {
                newRankSpecifiers = newRankSpecifiers.Add(
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList<ExpressionSyntax>(
                        arrayRank.Sizes.Select(s =>
                            SyntaxFactory.OmittedArraySizeExpression()))));
            }
            newNode = node.WithRankSpecifiers(newRankSpecifiers).WithTrailingTrivia(node.GetTrailingTrivia());
        }

        return base.VisitArrayType(newNode);
    }
}