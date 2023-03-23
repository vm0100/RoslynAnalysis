using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalysis.Convert.Test
{
    public class PropertyTest
    {
        /// <summary>
        /// 执行生成测试
        /// </summary>
        /// <param name="csharpCode"></param>
        /// <param name="expectCode"></param>
        [Theory(DisplayName = "普通类型属性验证"),
            InlineData("public string Name { get; set; }", "private String name;"),
            InlineData("public int Num { get; set; }", "private Integer num;"),
            InlineData("public bool Flag { get; set; }", "private Boolean flag;"),
            InlineData("public Guid Uid { get; set; }", "private UUID uid;"),
            InlineData("public Dictionary<string, string> Dict { get; set; }", "private Map<String, String> dict;"),
            InlineData("public Dictionary<Guid, User> Dict { get; set; }", "private Map<UUID, UserDTO> dict;"),
            InlineData("public Dictionary<Guid, List<User>> Dict { get; set; }", "private Map<UUID, List<UserDTO>> dict;"),
            InlineData("public List<string> Lst { get; set; }", "private List<String> lst;"),
            InlineData("public User Usr { get; set; }", "private UserDTO usr;"),
            InlineData("public UserDTO UsrDto { get; set; }", "private UserDTO usrDto;")]
        public void NormalTest(string csharpCode, string expectCode)
        {
            var propDeclareSyntax = (PropertyDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(csharpCode);
            var javaCode = ConvertProperty.GenerateCode(propDeclareSyntax);
            Assert.Equal(expectCode, javaCode);
        }
    }
}