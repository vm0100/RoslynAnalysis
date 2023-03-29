using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CSharp;

using RoslynAnalysis.Convert.Rewriter;

namespace RoslynAnalysis.Convert.Test
{
    public class MemberAccessRewriterTest
    {
        [Theory,
            InlineData("Guid.NewGuid()", "GuidGenerator.generateRandomGuid()"),
            InlineData("Guid.Empty", "UUIDUtil.emptyUUID"),
            InlineData("Guid.Parse(\"00000000-0000-0000-0000-000000000000\")", "UUIDUtil.parse(\"00000000-0000-0000-0000-000000000000\")")]
        public void StatisMemberAccessRewriterTest(string csharpCode, string expectCode)
        {
            var statement = SyntaxFactory.ParseStatement(csharpCode);
            var javaCode = new CSharpToJavaRewriter().Visit(statement).ToFullString();

            Assert.Equal(expectCode, javaCode);
        }

        [Theory,
            InlineData("_entityService.Instance.Find()", "_entityService.Find()"),
            InlineData("_repository.Value.Find()", "_repository.Find()"),
            InlineData("_userDomainService.Instance.GetUser()", "_userDomainService.GetUser()")]
        public void LazyServiceAccessRewriterTest(string csharpCode, string expectCode)
        {
            var statement = SyntaxFactory.ParseStatement(csharpCode);
            var javaCode = new CSharpToJavaRewriter().Visit(statement).ToFullString();

            Assert.Equal(expectCode, javaCode);
        }

        [Theory,
            InlineData("user.UserName", "user.getUserName()")]
        public void InstantiationMemberAccessRewriterTest(string csharpCode, string expectCode)
        {
            var statement = SyntaxFactory.ParseStatement(csharpCode);
            var javaCode = new CSharpToJavaRewriter().Visit(statement).ToFullString();

            Assert.Equal(expectCode, javaCode);
        }
    }
}
