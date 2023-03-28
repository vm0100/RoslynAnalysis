using System;
using System.Linq;
using System.Text;

namespace RoslynAnalysis.Convert.AnalysisToJava
{
    public class PropertyRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            node = node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.ValueText.TrimStart('_').ToLowerTitleCase()));

            node = node.WithType(new TypeRewriter().Visit(node.Type) as TypeSyntax);

            return base.VisitPropertyDeclaration(node);
        }

    }
}