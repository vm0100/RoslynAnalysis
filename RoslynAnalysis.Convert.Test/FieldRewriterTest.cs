using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using RoslynAnalysis.Convert.Rewriter;

namespace RoslynAnalysis.Convert.Test
{
    public class FieldReWriterTest
    {

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
            InlineData("Dictionary<Guid, User> Dict;", "Map<UUID, User> Dict;"),
            InlineData("Dictionary<Guid, List<User>> Dict;", "Map<UUID, List<User>> Dict;"),
            InlineData("List<string> Lst;", "List<String> Lst;"),
            InlineData("User Usr;", "User Usr;"),
            InlineData("UserDTO UsrDto;", "UserDTO UsrDto;")]
        public void NormalRewriteTest(string csharpCode, string expectCode)
        {
            var fieldDeclareSyntax = (FieldDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(csharpCode);
            var javaCode = new CSharpToJavaRewriter().Visit(fieldDeclareSyntax).ToFullString();
            Assert.Equal(expectCode, javaCode);
        }

        [Theory(DisplayName = "依赖服务字段验证"),
            InlineData("LazyService<DomainService> ds;", "[@Resource]\r\nDomainService ds;"),
            InlineData("EntityService<User> es;", "[@Resource]\r\nIUserDao es;"),
            InlineData("LazyService<EntityService<User>> es;", "[@Resource]\r\nIUserDao es;"),
            InlineData("LazyService<DomainService> ds = new LazyService<DomainService>();", "[@Resource]\r\nDomainService ds;"),
            InlineData("EntityService<User> es = new EntityService<User>();", "[@Resource]\r\nIUserDao es;"),
            InlineData("LazyService<EntityService<User>> es = new LazyService<EntityService<User>>();", "[@Resource]\r\nIUserDao es;"),
            ]
        public void LazyServiceRewriteTest(string csharpCode, string expectCode)
        {
            var fieldDeclareSyntax = (FieldDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(csharpCode);
            var javaCode = new CSharpToJavaRewriter().Visit(fieldDeclareSyntax).ToFullString();

            Assert.Equal(expectCode, javaCode);
        }

        [Theory(DisplayName = "普通类型初始化字段验证"),
            InlineData("UserDTO UsrDto = new UserDTO();", "UserDTO UsrDto = new UserDTO();"),
            InlineData("int num = 1;", "Integer num = 1;"),
            InlineData("bool flag = false;", "Boolean flag = false;"),
            InlineData("string str = \"\";", "String str = \"\";"),
            InlineData("decimal money = 0;", "BigDecimal money = BigDecimal.valueOf(0);"),
            InlineData("decimal money = 1;", "BigDecimal money = BigDecimal.valueOf(1);")]
        public void NormalCreateRewriteTest(string csharpCode, string expectCode)
        {
            var fieldDeclareSyntax = (FieldDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(csharpCode);
            var javaCode = new CSharpToJavaRewriter().Visit(fieldDeclareSyntax).ToFullString();

            Assert.Equal(expectCode, javaCode);
        }

        [Theory(DisplayName = "列表类型初始化字段验证"),
            InlineData("List<int> numList = new List<int>();", "List<Integer> numList = Lists.newArrayList();"),
            InlineData("int[] intArr = new int[5];", "Integer[] intArr = new Integer[5];"),
            InlineData("Dictionary<Guid, string> dict = new Dictionary<Guid, string>();", "Map<UUID, String> dict = Maps.newHashMap();")]
        public void NormalEnumerableCreateRewriteTest(string csharpCode, string expectCode)
        {
            var fieldDeclareSyntax = (FieldDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(csharpCode);
            var javaCode = new CSharpToJavaRewriter().Visit(fieldDeclareSyntax).ToFullString();
            Assert.Equal(expectCode, javaCode);
        }

        [Theory(DisplayName = "列表类型初始化数据验证"),
            InlineData("List<int> numList = new List<int>() {1, 2, 3};", "List<Integer> numList = Lists.newArrayList(1, 2, 3);"),
            InlineData("int[] intArr = new int[3];", "Integer[] intArr = new Integer[3];"),
            InlineData("int[] intArr = new int[3]{1,2,3};", "Integer[] intArr = new Integer[]{1,2,3};"),
            InlineData("int[] intArr = new int[]{1,2,3};", "Integer[] intArr = new Integer[]{1,2,3};"),
            InlineData("Dictionary<Guid, string> dict = new Dictionary<Guid, string>() {{ Guid.NewGuid(), \"张三\"} };", 
                       "Map<UUID, String> dict = new HashMap<UUID, String>() {{put(GuidGenerator.generateRandomGuid(), \"张三\"); }};")]
        public void NormalEnumerableCreateInitializeRewriteTest(string csharpCode, string expectCode)
        {
            var fieldDeclareSyntax = (FieldDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(csharpCode);
            var javaCode = new CSharpToJavaRewriter().Visit(fieldDeclareSyntax).ToFullString();

            Assert.Equal(expectCode, javaCode);
        }

        [Theory(DisplayName = "普通类型属性验证"),
            InlineData("public string Name { get; set; }", "private String name;"),
            InlineData("public int Num { get; set; }", "private Integer num;"),
            InlineData("public bool Flag { get; set; }", "private Boolean flag;"),
            InlineData("public Guid Uid { get; set; }", "private UUID uid;"),
            InlineData("public Dictionary<string, string> Dict { get; set; }", "private Map<String, String> dict;"),
            InlineData("public Dictionary<Guid, User> Dict { get; set; }", "private Map<UUID, User> dict;"),
            InlineData("public Dictionary<Guid, List<User>> Dict { get; set; }", "private Map<UUID, List<User>> dict;"),
            InlineData("public List<string> Lst { get; set; }", "private List<String> lst;"),
            InlineData("public User Usr { get; set; }", "private User usr;"),
            InlineData("public UserDTO UsrDto { get; set; }", "private UserDTO usrDto;")]
        public void NormalPropertyRewriterTest(string csharpCode, string expectCode)
        {
            var propDeclareSyntax = (PropertyDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(csharpCode);
            var javaCode = new CSharpToJavaRewriter().Visit(propDeclareSyntax).ToFullString();
            Assert.Equal(expectCode, javaCode);
        }
    }
}