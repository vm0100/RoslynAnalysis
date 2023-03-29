using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RoslynAnalysis.Convert.Rewriter;

public class FunctionBodyRewriter : CSharpSyntaxRewriter
{
    /// <summary>
    /// 只处理 entityService.Instance 或者 repository.Value的节点
    /// 重写为 entityService 和 repository
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var isRepository = node.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText.InEndsWithIgnoreCase("EntityService", "Repository");
        if (isRepository == false || node.Name.Identifier.ValueText.In("Instance", "Value") == false)
        {
            return base.VisitMemberAccessExpression(node);
        }
        
        var newNode = RewriterRepositoryMemberAccess(node);
        if (newNode.IsKinds(SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.PointerMemberAccessExpression))
        {
            return newNode;
        }

        return base.VisitMemberAccessExpression(newNode as MemberAccessExpressionSyntax);
    }

    public SyntaxNode RewriterRepositoryMemberAccess(MemberAccessExpressionSyntax node)
    {
        var newNode = node;

        // 只处理 entityService.Instance 或者 repository.Value的节点
        // 重写为 entityService 和 repository
        var isRepository = newNode.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText.InEndsWithIgnoreCase("EntityService", "Repository");
        if (isRepository == false || newNode.Name.Identifier.ValueText.In("Instance", "Value") == false)
        {
            return newNode;
        }
        
        return base.Visit(newNode.Expression);
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