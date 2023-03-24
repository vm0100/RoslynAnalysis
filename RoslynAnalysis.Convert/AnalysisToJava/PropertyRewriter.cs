using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynAnalysis.Core;
using RoslynAnalysis.Convert.ToJava;

namespace RoslynAnalysis.Convert.AnalysisToJava
{
    public class PropertyRewriter : RewriterBase<PropertyDeclarationSyntax>
    {
        private SyntaxTriviaList _leadingTrivia;
        public PropertyRewriter(PropertyDeclarationSyntax declaration) : base(declaration)
        {
            _leadingTrivia = declaration.GetLeadingTrivia();

            // 先清空注释，避免注释混乱
            _declaration = _declaration.WithLeadingTrivia(SyntaxFactory.Space);
        }

        public static PropertyRewriter Build(PropertyDeclarationSyntax declaration) => new PropertyRewriter(declaration);

        public override PropertyDeclarationSyntax Rewriter()
        {
            VisitDefined().VisitAnnotation().VisitType();
            // 还原注释
            _declaration = _declaration.WithLeadingTrivia(_leadingTrivia);
            return base.Rewriter();
        }

        public PropertyRewriter VisitDefined()
        {
            _declaration = _declaration.WithIdentifier(SyntaxFactory.Identifier(_declaration.Identifier.ValueText.TrimStart('_').ToLowerTitleCase()));

            return this;
        }

        public PropertyRewriter VisitAnnotation()
        {
            var annotationList = _declaration.AttributeLists.ToManyList(a => a.Attributes) ?? new List<AttributeSyntax>();

            // 更名
            for (int i = 0; i < annotationList.Count; i++)
            {
                var attrSyntax = annotationList[i];
                var attrName = ((IdentifierNameSyntax)attrSyntax.Name).Identifier.ValueText;
                var newAttrSyntax = attrSyntax.WithName(SyntaxFactory.IdentifierName("@" + attrName));
                annotationList[i] = newAttrSyntax;
            }

            // 转换成[attr1]\n[attr2]\n[attr3]
            var annotationListSyntax = SyntaxFactory.List(annotationList.Select(annotation =>
            {
                var attrList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(annotation));
                return attrList.WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n"));
            }));

            _declaration = _declaration.WithAttributeLists(annotationListSyntax);
            return this;
        }

        public PropertyRewriter VisitType()
        {
            _declaration = _declaration.WithType(TypeRewriter.Build(_declaration.Type).Rewriter());

            return this;
        }

    }
}
