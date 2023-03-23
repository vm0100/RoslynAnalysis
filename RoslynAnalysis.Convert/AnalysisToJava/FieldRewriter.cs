using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynAnalysis.Core;

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

        public override RewriterBase<FieldDeclarationSyntax> Visit()
        {
            return VisitDefined();
        }

        public override FieldDeclarationSyntax Rewriter()
        {
            // 还原注释
            _declaration = _declaration.WithLeadingTrivia(_leadingTrivia);
            return base.Rewriter();
        }

        /// <summary>
        /// 语法转换
        /// </summary>
        public FieldRewriter VisitDefined()
        {
            VisitLazyService().VisitRepository();

            return this;
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

            typeSyntax = genericTypeSyntax.TypeArgumentList.Arguments[0] as TypeSyntax;
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

            // 更名
            var variables = fieldDeclaration.Variables;
            var newVariables = variables;
            foreach (var variable in variables)
            {
                var variableRewriter = variable;
                variableRewriter = variable.WithIdentifier(SyntaxFactory.Identifier(typeSyntax.ToString().ToLowerTitleCase() + "Dao"));

                newVariables = fieldDeclaration.Variables.Replace(variable, variableRewriter.WithInitializer(null));
            }

            fieldDeclaration = fieldDeclaration.WithVariables(newVariables);

            _declaration = _declaration.ReplaceNode(_declaration.Declaration, fieldDeclaration);


            return this;
        }

        public FieldRewriter VisitLazyServiceModifiers()
        {
            var modifiers = SyntaxFactory.TokenList();
            foreach (var modifierSyntax in _declaration.Modifiers)
            {
                if (modifierSyntax.IsKind(SyntaxKind.ReadOnlyKeyword))
                {
                    continue;
                }

                modifiers = modifiers.Add(modifierSyntax);
            }

            _declaration = _declaration.WithModifiers(modifiers);

            return this;
        }
    }
}
