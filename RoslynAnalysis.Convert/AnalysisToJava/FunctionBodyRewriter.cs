using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalysis.Convert.AnalysisToJava;

public class FunctionBodyRewriter : RewriterBase<BlockSyntax>
{
    public FunctionBodyRewriter(BlockSyntax declaration) : base(declaration)
    {
    }

    public static FunctionBodyRewriter Build(BlockSyntax blockSyntax) => new FunctionBodyRewriter(blockSyntax);

    public override BlockSyntax Rewriter()
    {
        /**
         * 1. 解析属性调用 因为实体服务肯定会访问Instance或者Value
         * 2. 解析属性调用名称是Repository结尾，EntityService结尾的并且有调用Instance和Value的调用
         * 3. API映射，待定。。
         * 4. Lambda表达式转Wrapper
         * 5. 解析后续lambda调用
         * 
         * 实体服务调用可能存在位置：
         * 1. 方法调用参数
         * 2. if判断语句
         * 3. block行
         * 4. 简单lambda表达式
         * 5. 对象初始化
         * 6. 字段赋值
         */

        var nodes = _declaration.DescendantNodes();

        nodes.Where(a=>a.IsKind(SyntaxKind.ExpressionStatement));


        return base.Rewriter();
    }
}
