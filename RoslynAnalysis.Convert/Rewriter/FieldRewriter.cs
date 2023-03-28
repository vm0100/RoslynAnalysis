using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace RoslynAnalysis.Convert.Rewriter
{
    public class FieldRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            node = VisitVarDefine(node);
            node = VisitLazyService(node);
            node = VisitRepository(node);
            node = VisitType(node);

            return base.VisitFieldDeclaration(node);
        }

        public override SyntaxNode VisitAttributeList(AttributeListSyntax node)
        {
            //_declaration = _declaration.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Resource")))));

            return base.VisitAttributeList(node);
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
            var isLazy = genericTypeSyntax.Identifier.ValueText.In("LazyService", "Lazy");

            if (isLazy == false)
            {
                return node;
            }

            typeSyntax = (genericTypeSyntax.TypeArgumentList.Arguments[0] as TypeSyntax).WithTrailingTrivia(typeSyntax.GetTrailingTrivia());
            fieldDeclaration = fieldDeclaration.WithType(typeSyntax);

            // 将服务初始化语句移除
            var variables = fieldDeclaration.Variables;
            var newVariables = variables;
            foreach (var variable in variables)
            {
                var variableRewriter = variable;
                if (variableRewriter.Initializer == null)
                {
                    continue;
                }

                // 只处理new的数据
                if (!variableRewriter.Initializer.Value.IsKind(SyntaxKind.ObjectCreationExpression))
                {
                    continue;
                }

                variableRewriter = variable.WithIdentifier(SyntaxFactory.Identifier(variable.Identifier.ValueText.TrimStart('_')));
                newVariables = fieldDeclaration.Variables.Replace(variable, variableRewriter.WithInitializer(null));
            }

            fieldDeclaration = fieldDeclaration.WithVariables(newVariables);

            node = node.ReplaceNode(node.Declaration, fieldDeclaration);

            node = VisitLazyServiceModifiers(node);

            return node;
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
            if (genericTypeSyntax.Identifier.ValueText.NotIn("EntityService", "IRepository"))
            {
                return node;
            }

            typeSyntax = genericTypeSyntax.TypeArgumentList.Arguments[0] as TypeSyntax;
            fieldDeclaration = fieldDeclaration.WithType(SyntaxFactory.IdentifierName("I" + typeSyntax.ToString() + "Dao"));

            // 更名
            var variables = fieldDeclaration.Variables;
            var newVariables = variables;
            foreach (var variable in variables)
            {
                var variableRewriter = variable;
                //variableRewriter = variable.WithIdentifier(SyntaxFactory.Identifier(typeSyntax.ToString().ToLowerTitleCase() + "Dao"));
                variableRewriter = variable.WithIdentifier(SyntaxFactory.Identifier(variable.Identifier.ValueText.TrimStart('_')));

                newVariables = fieldDeclaration.Variables.Replace(variable, variableRewriter);
            }

            fieldDeclaration = fieldDeclaration.WithVariables(newVariables);

            node = node.WithDeclaration(fieldDeclaration);

            return node;
        }

        public FieldDeclarationSyntax VisitLazyServiceModifiers(FieldDeclarationSyntax node)
        {
            var modifiers = SyntaxFactory.TokenList(node.Modifiers.Where(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)));
            node = node.WithModifiers(modifiers);

            return node;
        }

        public FieldDeclarationSyntax VisitType(FieldDeclarationSyntax node)
        {
            var type = node.Declaration.Type;

            if (type.IsKind(SyntaxKind.GenericName) == false)
            {
                var newType = new TypeRewriter().Visit(type) as TypeSyntax;
                node = node.WithDeclaration(node.Declaration.WithType(newType).WithTrailingTrivia(type.GetTrailingTrivia()));
                return node;
            }

            var genericNameText = (type as GenericNameSyntax).Identifier.ValueText;
            if (genericNameText == "Dictionary" || genericNameText == "IDictionary")
            {
                node = node.WithDeclaration(node.Declaration.WithType(SyntaxFactory.IdentifierName("Map")).WithTrailingTrivia(type.GetTrailingTrivia()));
                return node;
            }

            return node;
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
    }
}