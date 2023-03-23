using Microsoft.CodeAnalysis.CSharp;

namespace RoslynAnalysis.Convert.Test
{
    public class ParameterTest
    {
        /// <summary>
        /// ִ�е����������ɲ���
        /// </summary>
        /// <param name="csharpType"></param>
        /// <param name="expectType"></param>
        [Theory(DisplayName = "��������֤"),
            InlineData("", ""),
            InlineData("string", "String"),
            InlineData("int", "Integer"),
            InlineData("bool", "Boolean"),
            InlineData("Guid", "UUID"),
            InlineData("DateTime", "Date"),
            InlineData("object", "Object"),
            InlineData("User", "UserEntity"),
            InlineData("UserDTO", "UserDTO"),
            InlineData("List<int>", "List<Integer>"),
            InlineData("List<User>", "List<UserEntity>"),
            InlineData("List<UserDTO>", "List<UserDTO>"),
            InlineData("string[]", "String[]"),
            InlineData("User[]", "UserEntity[]"),
            InlineData("UserDTO[]", "UserDTO[]"),
            InlineData("Dictionary<string, int>", "Map<String, Integer>"),
            InlineData("Dictionary<Guid, User>", "Map<UUID, UserEntity>"),
            InlineData("Dictionary<Guid, UserDTO>", "Map<UUID, UserDTO>"),
            InlineData("Dictionary<Guid, List<User>>", "Map<UUID, List<UserEntity>>"),
            InlineData("Dictionary<Guid, List<UserDTO>>", "Map<UUID, List<UserDTO>>")]
        public void SingleParamTest(string csharpType, string expectType)
        {
            var args = csharpType.IsNullOrWhiteSpace() ? SyntaxFactory.ParameterList() : SyntaxFactory.ParseParameterList(csharpType + " arg");
            var javaCode = ConvertParameter.GenerateCode(args);
            Assert.Equal(expectType.IsNullOrWhiteSpace() ? "" : (expectType + " arg"), javaCode);
        }

        /// <summary>
        /// ִ�ж���������ɲ���
        /// </summary>
        [Theory(DisplayName = "���������"),
            InlineData("string|Guid", "String|UUID"),
            InlineData("Dictionary<string, Guid>|List<User>", "Map<String, UUID>|List<UserEntity>")]
        public static void MultipleParamTest(string csharpType, string expectType)
        {
            var args = SyntaxFactory.ParseParameterList(string.Join(", ", csharpType.Split('|').Select((type, i) => type + " arg" + i)));
            var javaCode = ConvertParameter.GenerateCode(args);
            Assert.Equal(string.Join(", ", expectType.Split('|').Select((type, i) => type + " arg" + i)), javaCode);
        }
    }
}