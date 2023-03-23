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
using System.Xml.Linq;

namespace RoslynAnalysis.Convert.AnalysisToJava
{
    public class FieldRewriter : RewriterBase<FieldDeclarationSyntax>
    {
        private SyntaxTriviaList _leadingTrivia;
        public FieldRewriter(FieldDeclarationSyntax declaration) : base(declaration)
        {
            _leadingTrivia = declaration.GetLeadingTrivia();

            // 先清空注释，避免注释混乱
            _declaration = _declaration.WithLeadingTrivia(SyntaxFactory.Space);
        }

        public static FieldRewriter Build(FieldDeclarationSyntax declaration) => new FieldRewriter(declaration);

        public override FieldDeclarationSyntax Rewriter()
        {
            VisitLazyService().VisitRepository().VisitVarDefine().VisitType();

            // 还原注释
            _declaration = _declaration.WithLeadingTrivia(_leadingTrivia);
            return base.Rewriter();
        }

        /// <summary>
        /// 替换LazyService和Lazy定义
        /// </summary>
        /// <returns></returns>
        public FieldRewriter VisitLazyService()
        {
            var fieldDeclaration = _declaration.Declaration;
            var typeSyntax = fieldDeclaration.Type;
            if (typeSyntax.IsKind(SyntaxKind.GenericName) == false)
            {
                return this;
            }

            var genericTypeSyntax = (GenericNameSyntax)typeSyntax;
            var isLazy = genericTypeSyntax.Identifier.ValueText.In("LazyService", "Lazy");

            if (isLazy == false)
            {
                return this;
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

            _declaration = _declaration.ReplaceNode(_declaration.Declaration, fieldDeclaration);

            _declaration = _declaration.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Resource")))));

            return VisitLazyServiceModifiers();
        }

        public FieldRewriter VisitRepository()
        {
            var fieldDeclaration = _declaration.Declaration;
            var typeSyntax = fieldDeclaration.Type;

            if (typeSyntax.IsKind(SyntaxKind.GenericName) == false)
            {
                return this;
            }

            // 替换定义
            var genericTypeSyntax = (GenericNameSyntax)typeSyntax;
            if (genericTypeSyntax.Identifier.ValueText.NotIn("EntityService", "IRepository"))
            {
                return this;
            }

            typeSyntax = genericTypeSyntax.TypeArgumentList.Arguments[0] as TypeSyntax;
            fieldDeclaration = fieldDeclaration.WithType(SyntaxFactory.IdentifierName("I" + typeSyntax.ToString() + "Dao"));

            //// 更名
            //var variables = fieldDeclaration.Variables;
            //var newVariables = variables;
            //foreach (var variable in variables)
            //{
            //    var variableRewriter = variable;
            //    variableRewriter = variable.WithIdentifier(SyntaxFactory.Identifier(typeSyntax.ToString().ToLowerTitleCase() + "Dao"));

            //    newVariables = fieldDeclaration.Variables.Replace(variable, variableRewriter);
            //}

            //fieldDeclaration = fieldDeclaration.WithVariables(newVariables);

            _declaration = _declaration.WithDeclaration(fieldDeclaration);

            return this;
        }

        public FieldRewriter VisitLazyServiceModifiers()
        {
            var modifiers = SyntaxFactory.TokenList(_declaration.Modifiers.Where(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)));
            _declaration = _declaration.WithModifiers(modifiers);

            return this;
        }

        public FieldRewriter VisitType()
        {
            var type = _declaration.Declaration.Type;
            var newType = TypeRewriter.Build(type).Rewriter();

            _declaration = _declaration.WithDeclaration(_declaration.Declaration.WithType(newType).WithTrailingTrivia(type.GetTrailingTrivia()));

            return this;
        }

        public FieldRewriter VisitVarDefine()
        {
            var fieldDeclaration = _declaration.Declaration;
            var typeSyntax = fieldDeclaration.Type;
            if (typeSyntax.IsVar == false)
            {
                return this;
            }
            var firstVariableValue = fieldDeclaration.Variables[0].Initializer.Value;

            // 数字全是 NumericLiteralToken 没法儿判断类型
            fieldDeclaration = fieldDeclaration.WithType(((SyntaxKind)firstVariableValue.RawKind switch
            {
                SyntaxKind.StringLiteralExpression => SyntaxFactory.IdentifierName(nameof(String)),
                SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression => SyntaxFactory.IdentifierName(nameof(Boolean)),
                SyntaxKind.CharacterLiteralExpression => SyntaxFactory.IdentifierName(nameof(Char)),
                SyntaxKind.ObjectCreationExpression => (firstVariableValue as ObjectCreationExpressionSyntax).Type,
                _ => typeSyntax
            }).WithTrailingTrivia(typeSyntax.GetTrailingTrivia()));

            _declaration = _declaration.WithDeclaration(fieldDeclaration);

            return this;
        }
    }
}
