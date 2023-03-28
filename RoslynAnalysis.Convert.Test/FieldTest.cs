using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using RoslynAnalysis.Convert.AnalysisToJava;

namespace RoslynAnalysis.Convert.Test
{
    public class FieldTest
    {
        [Fact(DisplayName = "实体服务转换测试")]
        private void FieldRewriteTest()
        {
            var code = @"
var i = new List<string>;";

            var expected = @$"
Integer i = 0";

            var memberSyntax = SyntaxFactory.ParseMemberDeclaration(code);
            memberSyntax = FieldRewriter.Build(memberSyntax as FieldDeclarationSyntax).VisitVarDefine().Rewriter();
            var actual = memberSyntax.ToFullString();
            Assert.Equal(expected.Trim(), actual);
        }

        /// <summary>
        /// 执行生成测试
        /// </summary>
        /// <param name="csharpCode"></param>
        /// <param name="expectCode"></param>
        [Theory(DisplayName = "普通类型字段验证"),
            InlineData("string Name;", "String Name;"),
            InlineData("int Num;", "Integer Num;"),
            InlineData("bool Flag;", "Boolean Flag;"),
            InlineData("Guid Uid;", "UUID Uid;"),
            InlineData("Dictionary<string, string> Dict;", "Map<String, String> Dict;"),
            InlineData("Dictionary<Guid, User> Dict;", "Map<UUID, UserDTO> Dict;"),
            InlineData("Dictionary<Guid, List<User>> Dict;", "Map<UUID, List<UserDTO>> Dict;"),
            InlineData("List<string> Lst;", "List<String> Lst;"),
            InlineData("User Usr;", "UserDTO Usr;"),
            InlineData("UserDTO UsrDto;", "UserDTO UsrDto;")]
        public void NormalTest(string csharpCode, string expectCode)
        {
            var fieldDeclareSyntax = (FieldDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(csharpCode);
            var javaCode = ConvertField.GenerateCode(fieldDeclareSyntax);
            Assert.Equal(expectCode, javaCode);
        }

        [Theory(DisplayName = "依赖服务字段验证"),
            InlineData("LazyService<DomainService> ds;", "@Resource\nDomainService ds;"),
            InlineData("EntityService<User> es;", "@Resource\nUserDTO es;"),
            InlineData("LazyService<EntityService<User>> es;", "@Resource\nUserDTO es;"),
            InlineData("LazyService<DomainService> ds = new LazyService<DomainService>();", "@Resource\nDomainService ds;"),
            InlineData("EntityService<User> es = new EntityService<User>();", "@Resource\nUserDTO es;"),
            InlineData("LazyService<EntityService<User>> es = new LazyService<EntityService<User>>();", "@Resource\nUserDTO es;"),
            ]
        public void LazyServiceTest(string csharpCode, string expectCode)
        {
            var fieldDeclareSyntax = (FieldDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(csharpCode);
            var javaCode = ConvertField.GenerateCode(fieldDeclareSyntax);
            Assert.Equal(expectCode, javaCode);
        }

        [Theory(DisplayName = "普通类型初始化字段验证"),
            InlineData("UserDTO UsrDto = new UserDTO();", "UserDTO UsrDto = new UserDTO();"),
            InlineData("int num = 1;", "Integer num = 1;"),
            InlineData("bool flag = false;", "Boolean flag = false;"),
            InlineData("string str = \"\";", "String str = \"\";"),
            InlineData("decimal money = 0;", "BigDecimal money = BigDecimal.ZERO;"),
            InlineData("decimal money = 1;", "BigDecimal money = BigDecimal.valueOf(1);")]
        public void NormalCreateTest(string csharpCode, string expectCode)
        {
            var fieldDeclareSyntax = (FieldDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(csharpCode);
            var javaCode = ConvertField.GenerateCode(fieldDeclareSyntax);
            Assert.Equal(expectCode, javaCode);
        }

        [Theory(DisplayName = "列表类型初始化字段验证"),
            InlineData("List<int> numList = new List<int>();", "List<Integer> numList = Lists.newArrayList();"),
            InlineData("int[] intArr = new int[5];", "Integer[] intArr = new Integer[5];"),
            InlineData("Dictionary<Guid, string> dict = new Dictionary<Guid, string>();", "Map<UUID, String> dict = Maps.newHashMap();")]
        public void NormalEnumerableCreateTest(string csharpCode, string expectCode)
        {
            var fieldDeclareSyntax = (FieldDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(csharpCode);
            var javaCode = ConvertField.GenerateCode(fieldDeclareSyntax);
            Assert.Equal(expectCode, javaCode);
        }

        [Theory(DisplayName = "列表类型初始化数据验证"),
            InlineData("List<int> numList = new List<int>(){1,2,3};", "List<Integer> numList = Lists.newArrayList(1, 2, 3);"),
            InlineData("int[] intArr = new int[5]{1,2,3,4,5};", "Integer[] intArr = new Integer[] { 1, 2, 3, 4, 5 };"),
            InlineData("Dictionary<Guid, string> dict = new Dictionary<Guid, string>(){{Guid.NewGuid(), \"张三\"}};", "Map<UUID, String> dict = new HashMap<UUID, String>() {{ put(GuidGenerator.generateRandomGuid(), \"张三\"); }};")]
        public void NormalEnumerableCreateInitializeTest(string csharpCode, string expectCode)
        {
            var fieldDeclareSyntax = (FieldDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(csharpCode);
            var javaCode = ConvertField.GenerateCode(fieldDeclareSyntax);
            Assert.Equal(expectCode, javaCode);
        }
    }
}