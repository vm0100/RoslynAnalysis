using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using RoslynAnalysis.Convert.AnalysisToJava;

namespace RoslynAnalysis.Convert.Test
{
    public class ObjectCreationRewriteTest
    {
        [Theory(DisplayName = "重写List创建测试"),
            InlineData("new List<string>()", "Lists.newArrayList()"),
            InlineData("new List<string>(){}", "Lists.newArrayList()"),
            InlineData("new List<string>(){ \"张三\", \"李四\"}", "Lists.newArrayList(\"张三\",\"李四\")"),]
        public void RewriterListTest(string code, string expected)
        {
            var memberSyntax = SyntaxFactory.ParseStatement(code) as ExpressionStatementSyntax;
            var newMemberSyntax = new ObjectCreationRewriter().Visit(memberSyntax.Expression);
            var actual = newMemberSyntax.ToFullString();
            Assert.Equal(expected, actual);
        }

        [Fact(DisplayName = "重写Dictionary创建测试")]
        public void RewriterDictionaryTest()
        {
            var code = "new Dictionary<string, string>()";

            var expected = "new HashMap()";

            var memberSyntax = SyntaxFactory.ParseStatement(code) as ExpressionStatementSyntax;
            var newMemberSyntax = new ObjectCreationRewriter().Visit(memberSyntax.Expression as ObjectCreationExpressionSyntax);
            var actual = newMemberSyntax.ToFullString();
            Assert.Equal(expected, actual);
        }
    }
}