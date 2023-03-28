using System;
using System.Linq;
using System.Text;

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

        return base.Visit(node.Expression);
    }

    //public FunctionBodyWalker(BlockSyntax declaration) : base(declaration)
    //{
    //}

    //public static FunctionBodyWalker Build(BlockSyntax blockSyntax) => new FunctionBodyWalker(blockSyntax);

    //public override BlockSyntax Rewriter()
    //{

    //    /**
    //     * 1. 解析属性调用 因为实体服务肯定会访问Instance或者Value
    //     * 2. 解析属性调用名称是Repository结尾，EntityService结尾的并且有调用Instance和Value的调用
    //     * 3. API映射，待定。。
    //     * 4. Lambda表达式转Wrapper
    //     * 5. 解析后续lambda调用
    //     *
    //     * 实体服务调用可能存在位置：
    //     * 1. 方法调用参数
    //     * 2. if判断语句
    //     * 3. block行
    //     * 4. 简单lambda表达式
    //     * 5. 对象初始化
    //     * 6. 字段赋值
    //     */

    //    RewriterRepositoryMemberAccess()
    //        .RewriterRepositoryLambdaQuery();

    //    return base.Rewriter();
    //}

    //public FunctionBodyWalker RewriterRepositoryMemberAccess()
    //{
    //    foreach (var memberAccess in _declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
    //    {
    //        // 只处理 entityService.Instance 或者 repository.Value的节点
    //        // 重写为 entityService 和 repository
    //        var isRepository = memberAccess.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText.InEndsWithIgnoreCase("EntityService", "Repository");
    //        if (isRepository == false || memberAccess.Name.Identifier.ValueText.In("Instance", "Value") == false)
    //        {
    //            continue;
    //        }

    //        _declaration = _declaration.ReplaceNode(memberAccess, memberAccess.Expression);
    //    }

    //    return this;
    //}

    //public FunctionBodyWalker RewriterSimpleAssignment()
    //{
    //    var nodes = _declaration.DescendantNodes().OfType<AssignmentExpressionSyntax>();



    //    return this;
    //}

    //public FunctionBodyWalker RewriterRepositoryLambdaQuery()
    //{
    //    var nodes = _declaration.DescendantNodes().OfType<InvocationExpressionSyntax>();
    //    var invocationExpList = nodes.Where(a => a.IsKind(SyntaxKind.InvocationExpression)).Select(exp => exp as InvocationExpressionSyntax).ToList();

    //    foreach (var invocationExp in _declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
    //    {// 只处理 entityService.xx 或者 repository.xx 并且参数是单行lambda的语句
    //        var isRepository = invocationExp.Expression is MemberAccessExpressionSyntax memberAccess 
    //                            && memberAccess.Expression is IdentifierNameSyntax identifier 
    //                            && identifier.Identifier.ValueText.InEndsWithIgnoreCase("EntityService", "Repository");
    //        var isArgumentLambda = invocationExp.ArgumentList.Arguments.FirstOrDefault()?.Expression.IsKind(SyntaxKind.SimpleLambdaExpression);
    //        if (isRepository == false || isArgumentLambda == false)
    //        {
    //            continue;
    //        }

    //        // 1. 待判定函数参数格式
    //        // 2. 待判定函数映射关系
    //    }

    //    return this;
    //}
}