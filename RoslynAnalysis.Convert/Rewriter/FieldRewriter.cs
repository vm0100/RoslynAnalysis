using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;
using System.Text;

using RoslynAnalysis.Convert.ToJava;

namespace RoslynAnalysis.Convert.Rewriter
{
    public class FieldRewriter : CSharpSyntaxRewriter
    {
        private bool isRepository = false;
        private bool isLazyService = false;

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.IsDocumentationComment())
            {
                trivia = SyntaxFactory.Trivia(
                            SyntaxFactory.DocumentationCommentTrivia(
                                SyntaxKind.MultiLineDocumentationCommentTrivia,
                                SyntaxFactory.SingletonList<XmlNodeSyntax>(
                                    SyntaxFactory.XmlText(ConvertComment.GenerateDeclareCommennt(trivia)))));
            }

            return base.VisitTrivia(trivia);
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var newNode = VisitVarDefine(node);
            newNode = VisitLazyService(newNode);
            newNode = VisitRepository(newNode);
            newNode = VisitType(newNode);

            if (isLazyService || isRepository)
            {
                newNode = VisitLazyServiceModifiers(newNode);

                newNode = newNode.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Resource")))));
            }

            newNode = VisitAttribute(newNode);

            return base.VisitFieldDeclaration(newNode);
        }

        /// <summary>
        /// 替换LazyService和Lazy定义
        /// </summary>
        /// <returns></returns>
        public FieldDeclarationSyntax VisitLazyService(FieldDeclarationSyntax node)
        {
            var fieldDeclaration = node.Declaration;
            var typeSyntax = fieldDeclaration.Type;
            if (typeSyntax.IsKind(SyntaxKind.GenericName) == false)
            {
                return node;
            }

            var genericTypeSyntax = (GenericNameSyntax)typeSyntax;
            isLazyService = genericTypeSyntax.Identifier.ValueText.In("LazyService", "Lazy");

            if (isLazyService == false)
            {
                return node;
            }

            typeSyntax = (genericTypeSyntax.TypeArgumentList.Arguments[0] as TypeSyntax).WithTrailingTrivia(typeSyntax.GetTrailingTrivia());
            fieldDeclaration = fieldDeclaration.WithType(typeSyntax);

            return node.WithDeclaration(fieldDeclaration);
        }

        public FieldDeclarationSyntax VisitRepository(FieldDeclarationSyntax node)
        {
            var fieldDeclaration = node.Declaration;
            var typeSyntax = fieldDeclaration.Type;

            if (typeSyntax.IsKind(SyntaxKind.GenericName) == false)
            {
                return node;
            }

            // 替换定义
            var genericTypeSyntax = (GenericNameSyntax)typeSyntax;
            isRepository = genericTypeSyntax.Identifier.ValueText.In("EntityService", "IRepository");
            if (isRepository == false)
            {
                return node;
            }

            var newTypeSyntax = genericTypeSyntax.TypeArgumentList.Arguments[0] as TypeSyntax;
            fieldDeclaration = fieldDeclaration.WithType(SyntaxFactory.IdentifierName("I" + newTypeSyntax.ToString() + "Dao").WithTrailingTrivia(typeSyntax.GetTrailingTrivia()));
            return node.WithDeclaration(fieldDeclaration);
        }

        public FieldDeclarationSyntax VisitLazyServiceModifiers(FieldDeclarationSyntax node)
        {
            var modifiers = SyntaxFactory.TokenList(node.Modifiers.Where(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)));
            return node.WithModifiers(modifiers).WithLeadingTrivia(node.GetLeadingTrivia());
        }

        public FieldDeclarationSyntax VisitType(FieldDeclarationSyntax node)
        {
            var declaration = node.Declaration;

            var newType = new TypeRewriter().Visit(declaration.Type) as TypeSyntax;
            return node.WithDeclaration(declaration.WithType(newType)).WithLeadingTrivia(node.GetLeadingTrivia());
        }

        public FieldDeclarationSyntax VisitVarDefine(FieldDeclarationSyntax node)
        {
            var fieldDeclaration = node.Declaration;
            var typeSyntax = fieldDeclaration.Type;
            if (typeSyntax.IsVar == false)
            {
                return node;
            }
            var firstVariableValue = fieldDeclaration.Variables[0].Initializer.Value;

            // 数字全是 NumericLiteralToken 没法儿判断类型
            fieldDeclaration = fieldDeclaration.WithType(((SyntaxKind)firstVariableValue.RawKind switch
            {
                SyntaxKind.StringLiteralExpression or SyntaxKind.InterpolatedStringExpression => SyntaxFactory.IdentifierName(nameof(String)),
                SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression => SyntaxFactory.IdentifierName(nameof(Boolean)),
                SyntaxKind.CharacterLiteralExpression => SyntaxFactory.IdentifierName(nameof(Char)),
                SyntaxKind.ObjectCreationExpression => (firstVariableValue as ObjectCreationExpressionSyntax).Type,
                _ => typeSyntax
            }).WithTrailingTrivia(typeSyntax.GetTrailingTrivia()));

            node = node.WithDeclaration(fieldDeclaration);

            return node;
        }


        public FieldDeclarationSyntax VisitAttribute(FieldDeclarationSyntax node)
        {
            var annotationList = node.AttributeLists.ToManyList(a => a.Attributes) ?? new List<AttributeSyntax>();

            var attributeList = SyntaxFactory.List(annotationList.Select(annotation => SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(annotation))
                                                                                                    .WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n"))));

            node = node.WithAttributeLists(attributeList).WithLeadingTrivia(node.GetLeadingTrivia());
            return node;
        }

        public override SyntaxNode VisitAttribute(AttributeSyntax node)
        {
            node = node.WithName(SyntaxFactory.IdentifierName("@" + (node.Name as IdentifierNameSyntax).Identifier.ValueText.TrimStart('@')));
            return base.VisitAttribute(node);
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            if (isLazyService || isRepository)
            {
                node = node.WithInitializer(null).WithTrailingTrivia();
            }

            return base.VisitVariableDeclarator(node);
        }

        public override SyntaxNode VisitArrayType(ArrayTypeSyntax node)
        {
            return new TypeRewriter().Visit(node);
        }

        public override SyntaxNode VisitEqualsValueClause(EqualsValueClauseSyntax node)
        {
            if (node.Parent != null
                    && node.Parent.Parent != null
                    && node.Parent.Parent is VariableDeclarationSyntax variable
                    && variable.Type is IdentifierNameSyntax identifierName
                    && identifierName.Identifier.ValueText == "BigDecimal")
            {
                var invocationExp = SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName(identifierName.Identifier.ValueText),
                                            SyntaxFactory.IdentifierName("valueOf")));
                invocationExp = invocationExp.WithArgumentList(
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(node.Value))));

                node = node.WithValue(invocationExp);
            }

            if (node.Value.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                node = node.WithValue(new ObjectCreationRewriter().Visit(node.Value) as ExpressionSyntax);
            }

            if (node.Value.IsKind(SyntaxKind.ArrayCreationExpression))
            {
                node = node.WithValue(new ObjectCreationRewriter().Visit(node.Value) as ExpressionSyntax);
            }

            return base.VisitEqualsValueClause(node);
        }

    }
}