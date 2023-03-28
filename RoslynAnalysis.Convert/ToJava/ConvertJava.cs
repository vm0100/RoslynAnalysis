using System.Linq;
using System.Text;

using RoslynAnalysis.Convert.Rewriter;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertJava : IAnalysisConvert
{
    public static string GenerateNamespace(BaseNamespaceDeclarationSyntax namespaceNode)
    {
        var namespaceStr = namespaceNode.Name.ToString();

        var sbdr = new StringBuilder();
        sbdr.AppendLine("package com." + namespaceStr.ToLower());
        sbdr.AppendLine();
        sbdr.AppendLine(string.Join("\n", namespaceNode.Members.Where(n => n.IsKinds(SyntaxKind.ClassDeclaration, SyntaxKind.InterfaceDeclaration)).Select(syntax => ConvertTypeDeclare.GenerateCode((ClassDeclarationSyntax)syntax))));

        return sbdr.ToString();
    }

    public static NamespaceDeclarationSyntax VisitRewriterNamespace(NamespaceDeclarationSyntax namespaceDeclaration)
    {
        //return string.Join("\n", namespaceNode.Members.Where(n => n.IsKinds(SyntaxKind.ClassDeclaration, SyntaxKind.InterfaceDeclaration)).Select(syntax => ConvertTypeDeclare.GenerateCode((ClassDeclarationSyntax)syntax)));

        foreach (var member in namespaceDeclaration.Members)
        {
            namespaceDeclaration.ReplaceNode(member, new ClassRewriter().Visit(member));
        }

        return namespaceDeclaration;
    }

    public string GenerateCode(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot() as CompilationUnitSyntax;

        var sbdr = new StringBuilder(root.Span.Length);

        foreach (var member in root.Members)
        {
            var visitMember = member;
            if (visitMember.IsKind(SyntaxKind.NamespaceDeclaration))
            {
                visitMember = VisitRewriterNamespace(visitMember as NamespaceDeclarationSyntax);
            }
            sbdr.AppendLine((SyntaxKind)visitMember.RawKind switch
            {
                SyntaxKind.FileScopedNamespaceDeclaration or SyntaxKind.NamespaceDeclaration => GenerateNamespace(visitMember as BaseNamespaceDeclarationSyntax),
                SyntaxKind.InterfaceDeclaration => ConvertTypeDeclare.GenerateCode(visitMember as InterfaceDeclarationSyntax),
                SyntaxKind.ClassDeclaration => ConvertTypeDeclare.GenerateCode(visitMember as ClassDeclarationSyntax),
                //SyntaxKind.EnumDeclaration => ConvertTypeDeclare.GenerateCode(visitMember as EnumDeclarationSyntax),
                SyntaxKind.MethodDeclaration => ConvertMethod.GenerateCode(visitMember as MethodDeclarationSyntax),
                SyntaxKind.GlobalStatement => ConvertMethod.GenerateStatement((visitMember as GlobalStatementSyntax).Statement),
                SyntaxKind.PropertyDeclaration => ConvertProperty.GenerateCode(visitMember as PropertyDeclarationSyntax),
                SyntaxKind.FieldDeclaration => ConvertField.GenerateCode(visitMember as FieldDeclarationSyntax),
                _ => visitMember.ToString()
            });
        }

        return sbdr.ToString();
    }
}