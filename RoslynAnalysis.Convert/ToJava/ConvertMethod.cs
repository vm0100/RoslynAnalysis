using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using RoslynAnalysis.Convert.AnalysisToJava;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertMethod
{
    public static string GenerateCode(MethodDeclarationSyntax methodNode, int indent = 0)
    {
        methodNode = FunctionRewriter.Build(methodNode).Rewriter();

        var sbdr = new StringBuilder(2000);
        sbdr.Append(ConvertComment.GenerateDeclareCommennt(methodNode, indent));
        var modifier = string.Join(" ", methodNode.Modifiers.Select(ConvertCommon.KeywordToJava).Where(StringExtensions.IsNotNullOrWhiteSpace).ToList());

        sbdr.Append($"{modifier} {ConvertType.GenerateCode(methodNode.ReturnType)} {methodNode.Identifier.ValueText.ToLowerTitleCase()}({ConvertParameter.GenerateCode(methodNode.ParameterList)})".PadIndented(indent));
        if (methodNode.Body == null)
        {
            sbdr.AppendLine(";");
            return sbdr.ToString();
        }
        sbdr.AppendLine("{");
        sbdr.AppendLine(GenerateBody(methodNode.Body, indent + 1));
        sbdr.AppendLine("}".PadIndented(indent));

        return sbdr.ToString();
    }

    public static string GenerateCode(LocalFunctionStatementSyntax localFunction, int indent = 0)
    {
        localFunction = LocalFunctionRewriter.Build(localFunction).Rewriter();

        var sbdr = new StringBuilder(2000);
        sbdr.Append(ConvertComment.GenerateDeclareCommennt(localFunction, indent));
        var modifier = string.Join(" ", localFunction.Modifiers.Select(ConvertCommon.KeywordToJava).Where(StringExtensions.IsNotNullOrWhiteSpace).ToList());

        sbdr.Append($"{modifier} {ConvertType.GenerateCode(localFunction.ReturnType)} {localFunction.Identifier.ValueText.ToLowerTitleCase()}({ConvertParameter.GenerateCode(localFunction.ParameterList)})".PadIndented(indent));
        if (localFunction.Body == null)
        {
            sbdr.AppendLine(";");
            return sbdr.ToString();
        }
        sbdr.AppendLine("{");
        sbdr.AppendLine(GenerateBody(localFunction.Body, indent + 1));
        sbdr.AppendLine("}".PadIndented(indent));
        return sbdr.ToString();
    }

    public static string GenerateBody(BlockSyntax blockSyntax, int indent = 0)
    {
        if (blockSyntax == null)
        {
            return string.Empty;
        }

        return string.Join("", blockSyntax.Statements.Select(statement => GenerateStatement(statement, indent)));
    }

    public static string GenerateStatement(StatementSyntax statement, int indent = 0)
    {
        if (statement == null)
        {
            return string.Empty;
        }

        var sbdr = new StringBuilder(statement.Span.Length);
        var beforeComment = ConvertComment.GenerateBeforeComment(statement, indent);
        sbdr.Append(beforeComment + (beforeComment.IsNotNullOrWhiteSpace() ? "\n" : ""));
        sbdr.Append((SyntaxKind)statement.RawKind switch
        {
            SyntaxKind.LocalFunctionStatement => GenerateCode((LocalFunctionStatementSyntax)statement, indent),
            SyntaxKind.Block => GenerateBody((BlockSyntax)statement, indent),
            SyntaxKind.IfStatement => GenerateIfStatement((IfStatementSyntax)statement, indent),
            SyntaxKind.ForEachStatement => GenerateForEachStatement((ForEachStatementSyntax)statement, indent),
            SyntaxKind.ForStatement => GenerateForStatement((ForStatementSyntax)statement, indent),
            SyntaxKind.WhileStatement => GenerateWhileStatement((WhileStatementSyntax)statement, indent),
            SyntaxKind.SwitchStatement => GenerateSwitchStatement(statement as SwitchStatementSyntax, indent),
            SyntaxKind.ReturnStatement => GenerateReturnStatement((ReturnStatementSyntax)statement, indent),
            SyntaxKind.UsingStatement => GenerateUsingStatement((UsingStatementSyntax)statement, indent),
            SyntaxKind.ExpressionStatement => ConvertInvoke.GenerateCode(((ExpressionStatementSyntax)statement).Expression, indent + 1).PadIndented(indent) + ";\n",
            SyntaxKind.LocalDeclarationStatement => ConvertField.GenerateCode((LocalDeclarationStatementSyntax)statement, indent) + "\n",
            SyntaxKind.TryStatement => GenerateTryStatement((TryStatementSyntax)statement, indent),
            SyntaxKind.ThrowStatement => GenerateThrowException((ThrowStatementSyntax)statement, indent),
            _ => statement.ToString().PadIndented(indent) + "\n"
        });

        var afterComment = ConvertComment.GenerateAfterComment(statement, indent);
        sbdr.Append(afterComment + (afterComment.IsNotNullOrWhiteSpace() ? "\n" : ""));
        return sbdr.ToString();
    }

    public static string GenerateIfStatement(IfStatementSyntax ifStatement, int indent = 0)
    {
        var sbdr = new StringBuilder(ifStatement.Span.Length);
        sbdr.AppendLine($"if({ConvertInvoke.GenerateCode(ifStatement.Condition)}) {{".PadIndented(indent));
        sbdr.AppendLine(GenerateStatement(ifStatement.Statement, indent + 1).TrimEnd('\n'));
        sbdr.Append("}".PadIndented(indent));

        if (ifStatement.Else != null)
        {
            sbdr.Append(" else ");
            if (ifStatement.Else.Statement.IsKind(SyntaxKind.IfStatement))
            {
                sbdr.Append(GenerateIfStatement((IfStatementSyntax)ifStatement.Else.Statement, indent + 1).Trim());
            }
            else
            {
                sbdr.AppendLine("{");
                sbdr.AppendLine(GenerateStatement(ifStatement.Else.Statement, indent + 1).TrimEnd('\n'));
                sbdr.Append("}".PadIndented(indent));
            }
        }
        sbdr.AppendLine();
        return sbdr.ToString();
    }

    public static string GenerateForEachStatement(ForEachStatementSyntax forEachStatement, int indent = 0)
    {
        var sbdr = new StringBuilder(forEachStatement.Span.Length);
        sbdr.AppendLine($"for ({ConvertType.GenerateCode(forEachStatement.Type)} {forEachStatement.Identifier.ValueText}:{ConvertInvoke.GenerateCode(forEachStatement.Expression)}) {{".PadIndented(indent));
        sbdr.Append(GenerateStatement(forEachStatement.Statement, indent + 1));
        sbdr.AppendLine("}".PadIndented(indent));
        return sbdr.ToString();
    }

    public static string GenerateForStatement(ForStatementSyntax forStatement, int indent = 0)
    {
        var declaration = GenerateVariableDeclaration(forStatement.Declaration);
        var condition = ConvertInvoke.GenerateCode(forStatement.Condition);
        var incrementor = string.Join(",", forStatement.Incrementors.Select(ConvertInvoke.GenerateCode));
        var body = GenerateStatement(forStatement.Statement, indent + 1);

        var sbdr = new StringBuilder(forStatement.Span.Length);
        sbdr.Append("for(".PadIndented(indent));
        sbdr.Append(declaration);
        sbdr.Append("; ");
        sbdr.Append(condition);
        sbdr.Append("; ");
        sbdr.Append(incrementor);
        sbdr.AppendLine(") {");
        sbdr.Append(body);
        sbdr.AppendLine("}".PadIndented(indent));

        return sbdr.ToString();
    }

    public static string GenerateWhileStatement(WhileStatementSyntax whileStatement, int indent = 0)
    {
        return $"while({ConvertInvoke.GenerateCode(whileStatement.Condition)}){GenerateStatement(whileStatement.Statement, indent + 1)}".PadIndented(indent);
    }

    public static string GenerateSwitchStatement(SwitchStatementSyntax switchStatement, int indent = 0)
    {
        var sbdr = new StringBuilder(switchStatement.Span.End);
        sbdr.Append("switch (".PadIndented(indent));
        sbdr.Append(ConvertInvoke.GenerateCode(switchStatement.Expression));
        sbdr.AppendLine(") {");

        foreach (var section in switchStatement.Sections)
        {
            if (section.HasLeadingTrivia)
            {
                var triviaList = section.GetLeadingTrivia().Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)).ToList();
                if (triviaList.Any())
                {
                    sbdr.AppendLine(section.GetLeadingTrivia().Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)).ExpandAndToString("\n"));
                }
            }
            sbdr.AppendLine(section.Labels.ToString().PadIndented(indent + 1));
            foreach (var statementCode in section.Statements.Select(staement => GenerateStatement(staement, indent + 2)))
            {
                sbdr.AppendLine(statementCode);
            }
        }

        sbdr.AppendLine("}".PadIndented(indent));

        return sbdr.ToString();
    }

    public static string GenerateTryStatement(TryStatementSyntax tryStatement, int indent = 0)
    {
        var sbdr = new StringBuilder(tryStatement.Span.Length);
        sbdr.AppendLine("try {".PadIndented(indent));
        sbdr.AppendLine(GenerateStatement(tryStatement.Block, indent));
        sbdr.Append("}".PadIndented(indent));

        foreach (var catchSyntax in tryStatement.Catches)
        {
            if (catchSyntax.Declaration == null)
            {
                sbdr.AppendLine(" catch {");
            }
            else
            {
                var expType = ConvertType.GenerateCode(catchSyntax.Declaration.Type);
                var variable = catchSyntax.Declaration.Identifier.ValueText;
                sbdr.AppendLine(" catch (" + expType + (variable.IsNullOrWhiteSpace() ? "" : (" " + variable)) + ") {");
            }

            sbdr.AppendLine(GenerateStatement(catchSyntax.Block, indent));
            sbdr.Append("}".PadIndented(indent));
        }

        if (tryStatement.Finally != null)
        {
            sbdr.AppendLine(" finally {");
            sbdr.AppendLine(GenerateStatement(tryStatement.Finally.Block, indent));
            sbdr.AppendLine("}".PadIndented(indent));
        }
        return sbdr.ToString();
    }

    public static string GenerateUsingStatement(UsingStatementSyntax statementSyntax, int indent = 0)
    {
        var sbdr = new StringBuilder(statementSyntax.Span.Length);

        sbdr.AppendLine($"try ({GenerateVariableDeclaration(statementSyntax.Declaration)}) {{".PadIndented(indent));
        sbdr.AppendLine(GenerateStatement(statementSyntax.Statement, indent + 1));
        sbdr.AppendLine("} catch (Exception e) {".PadIndented(indent));
        sbdr.AppendLine("throw e;".PadIndented(indent + 1));
        sbdr.AppendLine("}".PadIndented(indent));
        return sbdr.ToString();
    }

    public static string GenerateReturnStatement(ReturnStatementSyntax returnStatement, int indent = 0)
    {
        if (returnStatement.Expression == null)
        {
            return "return;".PadIndented(indent);
        }
        return $"return {ConvertInvoke.GenerateCode(returnStatement.Expression)};".PadIndented(indent);
    }

    /// <summary>
    /// 变量定义
    /// </summary>
    /// <returns></returns>
    public static string GenerateVariableDeclaration(VariableDeclarationSyntax declarationSyntax)
    {
        if (declarationSyntax == null)
        {
            return string.Empty;
        }

        return string.Join(", ", declarationSyntax.Variables.Select(variableSyntax => $"{ConvertType.GenerateCode(declarationSyntax.Type)} {variableSyntax.Identifier.ValueText} {(variableSyntax.Initializer == null ? ";" : $"= {ConvertInvoke.GenerateCode(variableSyntax.Initializer.Value)}")}"));
    }

    public static string GenerateThrowException(ThrowStatementSyntax statement, int indent = 0)
    {
        if (statement.Expression == null)
        {
            return "throw;".PadIndented(indent);
        }
        return "throw ".PadIndented(indent) + ConvertInvoke.GenerateCode(statement.Expression, indent) + ";";
    }

    public static string GetExpAssignmentDeclareName(ExpressionSyntax expSyntax)
    {
        if (expSyntax.IsKind(SyntaxKind.SimpleAssignmentExpression))
        {
            return ConvertInvoke.GenerateCode(((AssignmentExpressionSyntax)expSyntax).Left);
        }

        if (expSyntax.IsKind(SyntaxKind.ParenthesizedExpression))
        {
            return GetExpAssignmentDeclareName(((ParenthesizedExpressionSyntax)expSyntax).Expression);
        }

        return null;
    }
}