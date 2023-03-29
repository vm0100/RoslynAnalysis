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
            InlineData("Guid.Parse()", "UUIDUtil.emptyUUID")]
        public void PropertyInvokeRewriterTest(string csharpCode, string expectCode)
        {
            var statement = SyntaxFactory.ParseStatement(csharpCode);

            var javaCode = new CSharpToJavaRewriter().Visit(statement).ToFullString();

            Console.WriteLine(statement);
        }
    }
}
