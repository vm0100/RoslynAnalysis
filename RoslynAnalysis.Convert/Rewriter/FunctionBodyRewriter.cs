using System;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RoslynAnalysis.Convert.Rewriter;

public partial class CSharpToJavaRewriter : CSharpSyntaxRewriter
{
    /// <summary>
    /// 只处理 entityService.Instance 或者 repository.Value的节点
    /// 重写为 entityService 和 repository
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (node.Expression.IsKind(SyntaxKind.IdentifierName) == false)
        {
            return base.VisitMemberAccessExpression(node);
        }

        var callerName = (node.Expression as IdentifierNameSyntax).Identifier.ValueText;
        var memberName = node.Name.Identifier.ValueText;

        var isServiceOrRepository = callerName.InEndsWithIgnoreCase("EntityService", "Repository", "Service");
        if (isServiceOrRepository && memberName.In("Instance", "Value"))
        {
            // 移除Instance
            return base.Visit(node.Expression);
        }

        // 首字母大写视为静态类
        if (callerName[..1].IsUpper())
        {
            return VisitStatisMemberAccessExpression(callerName, memberName, node);
        }

        if (callerName[..1].IsLower())
        {
            return VisitInstantiationMemberAccessExpression(callerName, memberName, node);
        }

        return base.VisitMemberAccessExpression(node);
    }

    public SyntaxNode VisitStatisMemberAccessExpression(string callerName, string memberName, MemberAccessExpressionSyntax node)
    {
        var newNode = node;
        if (callerName == nameof(Guid))
        {
            switch (memberName)
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

        return base.VisitMemberAccessExpression(newNode.WithTrailingTrivia(node.GetTrailingTrivia()));
    }

    public SyntaxNode VisitInstantiationMemberAccessExpression(string callerName, string memberName, MemberAccessExpressionSyntax node) 
    {


        return node;
    }

    public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        return base.VisitAssignmentExpression(node);
    }

    public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        // 只处理 entityService.xx 或者 repository.xx 并且参数是单行lambda的语句
        var isRepository = node.Expression is MemberAccessExpressionSyntax memberAccess
                            && memberAccess.Expression is IdentifierNameSyntax identifier
                            && identifier.Identifier.ValueText.InEndsWithIgnoreCase("EntityService", "Repository");
        var isArgumentLambda = node.ArgumentList.Arguments.FirstOrDefault()?.Expression.IsKind(SyntaxKind.SimpleLambdaExpression);
        if (isRepository == false || isArgumentLambda == false)
        {
            return base.VisitInvocationExpression(node);
        }

        // 1. 待判定函数参数格式
        // 2. 待判定函数映射关系
        return base.VisitInvocationExpression(node);
    }
}