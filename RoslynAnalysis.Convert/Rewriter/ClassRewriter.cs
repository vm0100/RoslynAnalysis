using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;

using RoslynAnalysis.Convert.ToJava;

namespace RoslynAnalysis.Convert.Rewriter;

public partial class CSharpToJavaRewriter : CSharpSyntaxRewriter
{
    private bool isEntity = false;
    private bool isDto = false;
    private bool isService = false;

    public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
    {
        if (trivia.IsDocumentationComment())
        {
            var commentText = ConvertComment.GenerateClassComment(trivia);
            // ConvertComment.GenerateDeclareCommennt(trivia)
            trivia = SyntaxFactory.Trivia(
                        SyntaxFactory.DocumentationCommentTrivia(
                            SyntaxKind.MultiLineDocumentationCommentTrivia,
                            SyntaxFactory.SingletonList<XmlNodeSyntax>(
                                SyntaxFactory.XmlText(commentText))));
        }

        return base.VisitTrivia(trivia);
    }


    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var className = node.Identifier.ValueText;
        var attrList = node.AttributeLists.ToManyList(attr => attr.Attributes);
        var baseTypeList = node.BaseList?.Types.ToList(t => t.Type.ToString()) ?? new List<string>();

        isEntity = IsEntity(className, attrList, baseTypeList);

        if (isEntity)
        {
            node = node.WithIdentifier(SyntaxFactory.Identifier(className.InEndsWithIgnoreCase("Entity") ? className : className + "Entity"));

            return base.VisitClassDeclaration(node);
        }
        isDto = IsDto(className, attrList, baseTypeList);

        if (isDto)
        {
            node = node.WithIdentifier(SyntaxFactory.Identifier(className.InEndsWithIgnoreCase("Dto") ? className : className + "DTO"));

            return base.VisitClassDeclaration(node);
        }
        isService = IsService(className, attrList, baseTypeList);
        
        return base.VisitClassDeclaration(VisitAttribute(node));
    }

    public override SyntaxNode VisitAttribute(AttributeSyntax node)
    {
        node = node.WithName(SyntaxFactory.IdentifierName("@" + (node.Name as IdentifierNameSyntax).Identifier.ValueText.TrimStart('@')));
        return base.VisitAttribute(node);
    }

    public ClassDeclarationSyntax VisitAttribute(ClassDeclarationSyntax node)
    {
        var annotationList = node.AttributeLists.ToManyList(a => a.Attributes) ?? new List<AttributeSyntax>();

        annotationList.RemoveAll(attr => ((IdentifierNameSyntax)attr.Name).Identifier.ValueText == "Serializable");
        if (isEntity)
        {
            annotationList.Add(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Data")));
        }
        else if (isDto)
        {
            annotationList.RemoveAll(attr => ((IdentifierNameSyntax)attr.Name).Identifier.ValueText == "DtoDescription");
            annotationList.Add(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Data")));
        }
        else if (isService)
        {
            annotationList.Add(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Service")));
            annotationList.Add(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Log4j")));
        }

        // 转换成[attr1]\n[attr2]\n[attr3]
        var annotationListSyntax = SyntaxFactory.List(annotationList.Select(annotation =>
        {
            var attrList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(annotation));
            return attrList.WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n"));
        }));

        node = node.WithAttributeLists(annotationListSyntax).WithLeadingTrivia(node.GetLeadingTrivia());
        return node;
    }

    public override SyntaxNode VisitBaseList(BaseListSyntax node)
    {
        var classDeclaration = node.Parent as ClassDeclarationSyntax;

        var baseTypeList = node?.Types.ToList(t => t.Type.ToString()) ?? new List<string>();

        if (isEntity)
        {
            if (baseTypeList.Contains("Entity") == false)
            {
                return base.VisitBaseList(node);
            }
            var baseTypeSyntaxList = SyntaxFactory.SeparatedList<BaseTypeSyntax>();
            foreach (var baseType in node.Types)
            {
                if (baseType.Type.ToString() == "Entity")
                {
                    baseTypeSyntaxList = baseTypeSyntaxList.Add(SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("BaseSlxtEntity")));
                    continue;
                }
                baseTypeSyntaxList = baseTypeSyntaxList.Add(baseType);
            }

            return base.VisitBaseList(node.WithTypes(baseTypeSyntaxList));
        }

        if (isDto)
        {
            if (baseTypeList.Contains("DtoBase") == false && baseTypeList.Contains("BaseDto") == false)
            {
                return base.VisitBaseList(node);
            }
            var baseTypeSyntaxList = SyntaxFactory.SeparatedList<BaseTypeSyntax>();
            foreach (var baseType in node.Types)
            {
                if (baseType.Type.ToString().In("BaseDto", "DtoBase"))
                {
                    baseTypeSyntaxList = baseTypeSyntaxList.Add(SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("DTO")));
                    continue;
                }
                baseTypeSyntaxList = baseTypeSyntaxList.Add(baseType);
            }

            return base.VisitBaseList(node.WithTypes(baseTypeSyntaxList));
        }

        return base.VisitBaseList(node);
    }

    private static bool IsEntity(string className, List<AttributeSyntax> attrList, List<string> baseTypes)
    {
        var isExtendEntity = baseTypes.Contains("Entity");
        var isExistsEntityNameAttr = attrList.Any(attr => attr.Name.ToString().InStartsWith("EntityName", "@EntityName", "@TableName"));
        return isExtendEntity || isExistsEntityNameAttr || className.InEndsWithIgnoreCase("Entity");
    }

    private static bool IsDto(string className, List<AttributeSyntax> attrList, List<string> baseTypes)
    {
        var isExtendDto = baseTypes.Any(t => t.In("DtoBase", "BaseDto"));
        var isExistsDtoAttr = attrList.Any(attr => attr.Name.ToString().InStartsWith("DtoDescription", "@DtoDescription"));
        return (isExtendDto || isExistsDtoAttr || className.InEndsWithIgnoreCase("Dto")) && className.InEndsWithIgnoreCase("Args") == false;
    }

    public static bool IsService(string className, List<AttributeSyntax> attrList, List<string> baseTypes)
    {
        var isExtendService = baseTypes.Any(t => t.In("DomainService", "AppService", "AggregateService"));
        var isExistsServiceScopeAttr = attrList.Any(t => t.Name.ToString().In("AppServiceScope"));
        return isExtendService || isExistsServiceScopeAttr || className.InEndsWithIgnoreCase("Service");
    }

}