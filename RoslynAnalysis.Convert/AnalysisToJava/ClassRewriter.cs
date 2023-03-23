using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using RoslynAnalysis.Convert.ToJava;

namespace RoslynAnalysis.Convert.AnalysisToJava;

public class ClassRewriter : RewriterBase<TypeDeclarationSyntax>
{
    private SyntaxTriviaList _leadingTrivia;

    public ClassRewriter(TypeDeclarationSyntax typeDeclaration) : base(typeDeclaration)
    {
        _leadingTrivia = typeDeclaration.GetLeadingTrivia();

        _declaration = _declaration.WithLeadingTrivia(SyntaxFactory.Space);
    }

    public static ClassRewriter Build(TypeDeclarationSyntax typeDeclaration) => new ClassRewriter(typeDeclaration);

    public override TypeDeclarationSyntax Rewriter()
    {
        VisitComment().VisitAnnotation().VisitDefinedName().VisitExtendBase();

        _declaration = _declaration.WithLeadingTrivia(_leadingTrivia);
        return base.Rewriter();
    }

    public ClassRewriter VisitComment()
    {
        if (_declaration.HasLeadingTrivia == false)
        {
            return this;
        }

        _leadingTrivia = SyntaxFactory.TriviaList(
            SyntaxFactory.Trivia(
                SyntaxFactory.DocumentationCommentTrivia(
                    SyntaxKind.MultiLineDocumentationCommentTrivia,
                    SyntaxFactory.SingletonList<XmlNodeSyntax>(
                        SyntaxFactory.XmlText(ConvertComment.GenerateTypeDeclareComment(_declaration))))));
        return this;
    }

    public ClassRewriter VisitAnnotation()
    {
        var className = _declaration.Identifier.ValueText;
        var annotationList = _declaration.AttributeLists.ToManyList(a => a.Attributes) ?? new List<AttributeSyntax>();
        var baseTypeList = _declaration.BaseList?.Types.ToList(t => t.Type.ToString()) ?? new List<string>();
        var isEntity = IsEntity(className, annotationList, baseTypeList);
        var isDto = IsDto(className, annotationList, baseTypeList);
        var isService = IsService(className, annotationList, baseTypeList);
        annotationList.RemoveAll(attr => ((IdentifierNameSyntax)attr.Name).Identifier.ValueText == "Serializable");
        if (isEntity)
        {
            annotationList.Add(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Data")));
        }
        else if (isDto)
        {
            annotationList.RemoveAll(attr => ((IdentifierNameSyntax)attr.Name).Identifier.ValueText == "DtoDescription");
        }
        else if (isService)
        {
            annotationList.Add(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Service")));
            annotationList.Add(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Log4j")));
        }

        // 更名
        for (int i = 0; i < annotationList.Count; i++)
        {
            var attrSyntax = annotationList[i];
            var attrName = ((IdentifierNameSyntax)attrSyntax.Name).Identifier.ValueText;
            var newAttrName = attrName switch
            {
                "EntityName" => "@TableName",
                _ => "@" + attrName
            };

            if (attrName == newAttrName)
            {
                continue;
            }

            var newAttrSyntax = attrSyntax.WithName(SyntaxFactory.IdentifierName(newAttrName));
            annotationList[i] = newAttrSyntax;
        }

        // 转换成[attr1]\n[attr2]\n[attr3]
        var annotationListSyntax = SyntaxFactory.List(annotationList.Select(annotation =>
        {
            var attrList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(annotation));
            return attrList.WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n"));
        }));

        _declaration = _declaration.WithAttributeLists(annotationListSyntax);
        return this;
    }

    public ClassRewriter VisitDefinedName()
    {
        var className = _declaration.Identifier.ValueText;
        var attrList = _declaration.AttributeLists.ToManyList(attr => attr.Attributes);
        var baseTypeList = _declaration.BaseList.Types.ToList(t => t.Type.ToString());

        if (IsEntity(className, attrList, baseTypeList))
        {
            _declaration = _declaration.WithIdentifier(SyntaxFactory.Identifier(className.InEndsWithIgnoreCase("Entity") ? className : className + "Entity"));
            return this;
        }

        if (IsDto(className, attrList, baseTypeList))
        {
            _declaration = _declaration.WithIdentifier(SyntaxFactory.Identifier(className.InEndsWithIgnoreCase("Dto") ? className : className + "DTO"));

            return this;
        }

        return this;
    }

    public ClassRewriter VisitExtendBase()
    {
        var className = _declaration.Identifier.ValueText;
        var attrList = _declaration.AttributeLists.ToManyList(attr => attr.Attributes);

        var baseTypeList = _declaration.BaseList.Types.ToList(t => t.Type.ToString());

        if (IsEntity(className, attrList, baseTypeList))
        {
            if (baseTypeList.Contains("Entity") == false)
            {
                return this;
            }
            var baseTypeSyntaxList = SyntaxFactory.SeparatedList<BaseTypeSyntax>();
            foreach (var baseType in _declaration.BaseList.Types)
            {
                if (baseType.Type.ToString() == "Entity")
                {
                    baseTypeSyntaxList = baseTypeSyntaxList.Add(SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("BaseSlxtEntity")));
                    continue;
                }
                baseTypeSyntaxList = baseTypeSyntaxList.Add(baseType);
            }

            _declaration = _declaration.WithBaseList(_declaration.BaseList.WithTypes(baseTypeSyntaxList));
            return this;
        }

        if(IsDto(className, attrList, baseTypeList))
        {
            if (baseTypeList.Contains("DtoBase") == false && baseTypeList.Contains("BaseDto") == false)
            {
                return this;
            }
            var baseTypeSyntaxList = SyntaxFactory.SeparatedList<BaseTypeSyntax>();
            foreach (var baseType in _declaration.BaseList.Types)
            {
                if (baseType.Type.ToString().In("BaseDto", "DtoBase"))
                {
                    baseTypeSyntaxList = baseTypeSyntaxList.Add(SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("DTO")));
                    continue;
                }
                baseTypeSyntaxList = baseTypeSyntaxList.Add(baseType);
            }

            _declaration = _declaration.WithBaseList(_declaration.BaseList.WithTypes(baseTypeSyntaxList));
            return this;
        }

        return this;
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