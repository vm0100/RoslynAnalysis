using System.Text;

using RoslynAnalysis.Convert.Rewriter;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertTypeDeclare
{
    public static string GenerateCode(TypeDeclarationSyntax classNode, int indent = 0)
    {
        var sbdr = new StringBuilder(10000);

        // 公共包部分
        sbdr.AppendLine(ConvertCommon.GenerateImportPackage());

        // 文档注释
        sbdr.Append(ConvertComment.GenerateClassComment(classNode.GetLeadingTrivia()));

        // 注解部分
        sbdr.AppendLine(GenerateAttribute(classNode));

        // 修饰符
        sbdr.Append(classNode.Modifiers.ToString() + " ");

        // 类型定义
        sbdr.Append(classNode.Keyword.Value + " ");

        // 命名定义
        sbdr.Append(classNode.Identifier.Value + " ");

        // 继承
        sbdr.AppendLine(GenerateExtend(classNode));

        sbdr.AppendLine("{ ");

        foreach (MemberDeclarationSyntax memberDeclaration in classNode.Members)
        {
            var generateCode = (SyntaxKind)memberDeclaration.RawKind switch
            {
                SyntaxKind.FieldDeclaration => ConvertField.GenerateCode(memberDeclaration as FieldDeclarationSyntax, indent + 1),
                SyntaxKind.PropertyDeclaration => ConvertProperty.GenerateCode(memberDeclaration as PropertyDeclarationSyntax, indent + 1),
                SyntaxKind.MethodDeclaration => ConvertMethod.GenerateCode(memberDeclaration as MethodDeclarationSyntax, indent + 1),
                SyntaxKind.ConstructorDeclaration => GenerateConstructor(memberDeclaration as ConstructorDeclarationSyntax, indent + 1),
                _ => ""
            };

            if (!generateCode.IsNullOrWhiteSpace())
            {
                sbdr.AppendLine(generateCode + "\n");
            }
        }

        sbdr.Append('}');
        return sbdr.ToString();
    }

    private static string GenerateAttribute(TypeDeclarationSyntax classNode)
    {
        return classNode.AttributeLists.SelectMany(attr => attr.Attributes).ExpandAndToString("\n");
    }

    private static string GenerateExtend(TypeDeclarationSyntax classNode)
    {
        if (classNode.BaseList.IsNull())
        {
            return string.Empty;
        }

        // 继承和实现
        var baseTypeNameList = classNode.BaseList.Types.Select(t => t.Type.ToString()).ToList();
        var sbdr = new StringBuilder(classNode.BaseList.Span.End);

        var implTypeNameList = baseTypeNameList.Where(ConvertCommon.IsInterfaceDeclare).ToList();

        if (baseTypeNameList.Count != implTypeNameList.Count)
        {
            // 继承(一般也不会存在多继承，但是还是选择拼接吧）
            sbdr.Append($"extends {string.Join(", ", baseTypeNameList.Except(implTypeNameList))} ");
        }

        if (implTypeNameList.Any())
        {
            // 实现
            sbdr.Append($"implements {string.Join(", ", implTypeNameList)} ");
        }

        return sbdr.ToString();
    }

    private static string GenerateConstructor(ConstructorDeclarationSyntax constructorDeclaration, int indent = 0)
    {
        var sbdr = new StringBuilder(constructorDeclaration.Span.End);

        sbdr.Append(ConvertComment.GenerateDeclareCommennt(constructorDeclaration.GetTrailingTrivia(), indent));
        sbdr.Append(constructorDeclaration.Modifiers.ToString().Trim().PadIndented(indent) + " ");
        sbdr.Append(constructorDeclaration.Identifier.ValueText);
        sbdr.Append('(');
        sbdr.Append(constructorDeclaration.ParameterList.ToString());
        sbdr.AppendLine(") {");
        sbdr.Append(ConvertMethod.GenerateStatement(constructorDeclaration.Body, indent + 1));
        sbdr.AppendLine("}".PadIndented(indent));
        return sbdr.ToString();
    }
}