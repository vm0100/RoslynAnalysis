using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using RoslynAnalysis.Convert.Rewriter;

namespace RoslynAnalysis.Convert.Test
{
    public class ClassRewriterTest
    {
        [Fact(DisplayName = "类注释转换测试")]
        public void VisitCommentTest()
        {
            var code = @"
/// <summary>
/// 张三李四王五
/// </summary>
public class ConvertArgument{}";

            var expected = @$"
/**
 * @author 
 * @Description:  张三李四王五
 * @date: {DateTime.Now:yyyy-MM-dd}
 */
public class ConvertArgument{{}}";

            var memberSyntax = SyntaxFactory.ParseMemberDeclaration(code);
            memberSyntax = new CSharpToJavaRewriter().Visit(memberSyntax) as MemberDeclarationSyntax;
            var actual = memberSyntax.ToFullString();
            Assert.Equal(expected, actual);
        }

        [Fact(DisplayName = "类属性转换测试")]
        public void VisitAnnotationTest()
        {
            var code = @"
/// <summary>
/// 张三李四王五
/// </summary>
[Attr(name=""表1"")]
[Attr1(""表2""), Attr2]
public class ConvertArgument{}";

            var expected = @$"
/**
 * @author 
 * @Description:  张三李四王五
 * @date: {DateTime.Now:yyyy-MM-dd}
 */
[@Attr(name=""表1"")]
[@Attr1(""表2"")]
[@Attr2]
public class ConvertArgument{{}}";

            var memberSyntax = SyntaxFactory.ParseMemberDeclaration(code);
            memberSyntax = new CSharpToJavaRewriter().Visit(memberSyntax) as MemberDeclarationSyntax;
            var actual = memberSyntax.ToFullString();
            Assert.Equal(expected, actual);
        }

        [Theory(DisplayName = "类名称转换测试"),
            InlineData("public class ConvertArgument:Entity{}", "public class ConvertArgumentEntity:BaseSlxtEntity{}"),
            InlineData("public class ConvertArgument:BaseDto{}", "public class ConvertArgumentDTO:DTO{}"),
            InlineData("public class ConvertArgument:DtoBase{}", "public class ConvertArgumentDTO:DTO{}"),
            InlineData("public class ConvertArgument{}", "public class ConvertArgument{}")]
        public void VisitDefinedNameTest(string source, string expect)
        {
            var memberSyntax = SyntaxFactory.ParseMemberDeclaration(source);
            memberSyntax = new CSharpToJavaRewriter().Visit(memberSyntax) as MemberDeclarationSyntax;
            var actual = memberSyntax.ToFullString();
            Assert.Equal(expect, actual);
        }
    }
}