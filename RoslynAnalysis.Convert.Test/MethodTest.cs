using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalysis.Convert.Test
{
    public class MethodTest
    {
        [Fact(DisplayName = "try{}catch(Exception e){}测试")]
        public void TryCatchTest()
        {
            var tryStatement = (TryStatementSyntax)SyntaxFactory.ParseStatement("try{}catch(Exception e){}");

            var javaCode = ConvertMethod.GenerateTryStatement(tryStatement);
            Assert.Equal("try {} catch (Exception e) {}", javaCode.Replace("\r\n", ""));
        }

        [Fact(DisplayName = "泛型实例方法调用测试")]
        public void InvokeGenericMethodTest()
        {
            var invokeExpression = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression("paramSettingPublicService.GetConfigParam<T>()");
            var javaCode = ConvertInvoke.GenerateInvocation(invokeExpression);

            Assert.Equal("paramSettingPublicService.getConfigParam<T>()", javaCode);
        }

        [Fact(DisplayName = "常量调用测试")]
        public void InvokeConstantTest()
        {
            var invokeExpression = (MemberAccessExpressionSyntax)SyntaxFactory.ParseExpression("string.Empty");
            var javaCode = ConvertInvoke.GenerateSimpleMemberAccess(invokeExpression);

            Assert.Equal("StringUtil.empty", javaCode);
        }

        [Fact(DisplayName = "属性调用测试")]
        public void InvokePropertyTest()
        {
            var invokeExpression = (MemberAccessExpressionSyntax)SyntaxFactory.ParseExpression("entity.Prop1");
            var javaCode = ConvertInvoke.GenerateSimpleMemberAccess(invokeExpression);

            Assert.Equal("entity.getProp1()", javaCode);
        }

        [Theory(DisplayName = "方法调用测试"),
            InlineData("_service.Instance.Test()", "service.test()"),
            InlineData("result.AddRange(errors)", "result.addAll(errors)"),
            InlineData("result.ErrorList.AddRange(errors)", "result.getErrorList().addAll(errors)"),
            InlineData("result.ErrorList.Any()", "result.getErrorList().size() > 0"),]
        public void InvokeMethodTest(string csharpCode, string expectCode)
        {
            var invokeExpression = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression(csharpCode);
            var javaCode = ConvertInvoke.GenerateInvocation(invokeExpression);

            Assert.Equal(expectCode, javaCode);
        }

        [Theory(DisplayName = "Number if语句测试"),
            InlineData("if(i > 0){}", "if(NumberUtil.greatThenZero(i)) {}"),
            InlineData("if(i >= 0){}", "if(NumberUtil.greatThenEqualZero(i)) {}"),
            InlineData("if(i < 0){}", "if(NumberUtil.lessThenZero(i)) {}"),
            InlineData("if(i <= 0){}", "if(NumberUtil.lessThenEqualZero(i)) {}"),
            InlineData("if(i == 0){}", "if(NumberUtil.equalsZero(i)) {}"),
            InlineData("if(i != 0){}", "if(NumberUtil.equalsZero(i) == false) {}")]
        public void IfNumberStatementTest(string csharpCode, string expectCode)
        {
            var ifStaement = (IfStatementSyntax)SyntaxFactory.ParseStatement(csharpCode);
            var javaCode = ConvertMethod.GenerateIfStatement(ifStaement);

            Assert.Equal(expectCode, javaCode.Replace("\r\n", ""));
        }

        [Theory(DisplayName = "bool if语句测试"),
            InlineData("if(flag){}", "if(flag) {}"),
            InlineData("if(flag == true){}", "if(flag == true) {}"),
            InlineData("if(!flag){}", "if(!flag) {}"),
            InlineData("if(flag == false){}", "if(flag == false) {}")]
        public void IfBoolStatementTest(string csharpCode, string expectCode)
        {
            var ifStaement = (IfStatementSyntax)SyntaxFactory.ParseStatement(csharpCode);
            var javaCode = ConvertMethod.GenerateIfStatement(ifStaement);

            Assert.Equal(expectCode, javaCode.Replace("\r\n", ""));
        }

        [Theory(DisplayName = "String if语句测试"),
            InlineData("if(str == \"\"){}", "if(str.equals(\"\")) {}"),
            InlineData("if(str.StartsWith(\"1\")){}", "if(str.startsWith(\"1\")) {}"),
            InlineData("if(str.EtartsWith(\"1\")){}", "if(str.etartsWith(\"1\")) {}"),
            InlineData("if(str.Equals(\"1\")){}", "if(str.equals(\"1\")) {}"),
            InlineData("if(str.Equals(\"1\", StringComparison.OrdinalIgnoreCase)){}", "if(str.equalsIgnoreCase(\"1\")) {}")]
        public void IfStringStatementTest(string csharpCode, string expectCode)
        {
            var ifStaement = (IfStatementSyntax)SyntaxFactory.ParseStatement(csharpCode);
            var javaCode = ConvertMethod.GenerateIfStatement(ifStaement);

            Assert.Equal(expectCode, javaCode.Replace("\r\n", ""));
        }

        [Theory(DisplayName = "Date if语句测试"),
            InlineData("if(xDate == DateTime.Today){}", "if(DateUtil.equalDate(xDate, clock.now().toLocalDate().convertToUserTimezone())) {}"),
            InlineData("if(xDate == DateTime.Now){}", "if(DateUtil.equalDate(xDate, Date.from(clock.now().toInstant()))) {}"),
            InlineData("if(xDate > DateTime.Now){}", "if(DateUtil.greatThenDate(xDate, Date.from(clock.now().toInstant()))) {}"),
            InlineData("if(xDate >= DateTime.Now){}", "if(DateUtil.greatThenEqualDate(xDate, Date.from(clock.now().toInstant()))) {}"),
            InlineData("if(xDate < DateTime.Now){}", "if(DateUtil.lessThenDate(xDate, Date.from(clock.now().toInstant()))) {}"),
            InlineData("if(xDate <= DateTime.Now){}", "if(DateUtil.lessThenEqualDate(xDate, Date.from(clock.now().toInstant()))) {}"),
            InlineData("if(xDate != DateTime.Now){}", "if(DateUtil.equalDate(xDate, Date.from(clock.now().toInstant())) == false) {}")]
        public void IfDateStatementTest(string csharpCode, string expectCode)
        {
            var ifStaement = (IfStatementSyntax)SyntaxFactory.ParseStatement(csharpCode);
            var javaCode = ConvertMethod.GenerateIfStatement(ifStaement);

            Assert.Equal(expectCode, javaCode.Replace("\r\n", ""));
        }

        [Theory(DisplayName = "Date 方法 if语句测试"),
            InlineData("if(xDate == DateTime.Now.AddDays(1)){}", "if(DateUtil.equalDate(xDate, DateUtil.addDay(Date.from(clock.now().toInstant()), 1))) {}"),
            InlineData("if(DateTime.Today.AddDays(-isAllowChangeDays).Date <= subscribeSaveDTO.Order.QSDate){}",
                       "if(DateUtil.lessThenEqualDate(DateUtil.addDay(clock.now().toLocalDate().convertToUserTimezone(), -isAllowChangeDays).getDate(), subscribeSaveDTO.getOrder().getQSDate())) {}"),
            InlineData("if(DateTime.Now.AddDays(-isAllowChangeDays).Date <= subscribeSaveDTO.Order.QSDate){}",
                       "if(DateUtil.lessThenEqualDate(DateUtil.addDay(Date.from(clock.now().toInstant()), -isAllowChangeDays).getDate(), subscribeSaveDTO.getOrder().getQSDate())) {}")]
        public void IfDateMethodStatementTest(string csharpCode, string expectCode)
        {
            var ifStaement = (IfStatementSyntax)SyntaxFactory.ParseStatement(csharpCode);
            var javaCode = ConvertMethod.GenerateIfStatement(ifStaement);

            Assert.Equal(expectCode, javaCode.Replace("\r\n", ""));
        }
    }
}