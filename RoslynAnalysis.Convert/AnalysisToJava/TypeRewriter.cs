using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using RoslynAnalysis.Convert.ToJava;

namespace RoslynAnalysis.Convert.AnalysisToJava;

public class TypeRewriter : RewriterBase<TypeSyntax>
{
    public TypeRewriter(TypeSyntax typeDeclaration) : base(typeDeclaration)
    {
    }

    public static TypeRewriter Build(TypeSyntax typeDeclaration) => new TypeRewriter(typeDeclaration);

    public override TypeRewriter Visit()
    {
        _declaration = VisitType(_declaration);
        return this;
    }

    public override TypeSyntax Rewriter()
    {
        return base.Rewriter();
    }

    public TypeSyntax VisitType(TypeSyntax syntax)
    {
        if (syntax.IsKind(SyntaxKind.ArrayType))
        {
            return (syntax as ArrayTypeSyntax).WithElementType(VisitType((_declaration as ArrayTypeSyntax).ElementType));
        }

        if (syntax.IsKind(SyntaxKind.GenericName))
        {
            var genericType = syntax as GenericNameSyntax;

            genericType = genericType.WithIdentifier(SyntaxFactory.Identifier(ConvertCommon.TypeToJava(syntax)));

            genericType = genericType.WithTypeArgumentList(genericType.TypeArgumentList.WithArguments(SyntaxFactory.SeparatedList(genericType.TypeArgumentList.Arguments.Select(VisitType))));

            return genericType;
        }

        return SyntaxFactory.IdentifierName(ConvertCommon.TypeToJava(syntax));
    }

}