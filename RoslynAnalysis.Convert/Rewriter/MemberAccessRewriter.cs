using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynAnalysis.Convert.Rewriter
{
    public class MemberAccessRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            var newNode = node;
            // 首字母大写的方法引用
            if (newNode.Expression is IdentifierNameSyntax identifierName && identifierName.Identifier.ValueText[..1].IsUpper())
            {
                if (identifierName.Identifier.ValueText == nameof(Guid))
                {
                    var funcName = newNode.Name.Identifier.ValueText;
                    switch (funcName)
                    {
                        case nameof(Guid.Empty):
                            newNode = newNode.WithExpression(SyntaxFactory.IdentifierName("UUIDUtil"))
                                             .WithName(SyntaxFactory.IdentifierName("emptyUUID"));
                            break;
                        case nameof(Guid.Parse):
                            newNode = newNode.WithExpression(SyntaxFactory.IdentifierName("UUIDUtil"))
                                             .WithName(SyntaxFactory.IdentifierName("parse"));
                            break;
                        case nameof(Guid.NewGuid):
                            newNode = newNode.WithExpression(SyntaxFactory.IdentifierName("GuidGenerator"))
                                             .WithName(SyntaxFactory.IdentifierName("generateRandomGuid"));
                            break;
                        default:
                            break;
                    }
                }
            }

            return base.VisitMemberAccessExpression(newNode.WithTrailingTrivia(node.GetTrailingTrivia()));
        }
    }
}
