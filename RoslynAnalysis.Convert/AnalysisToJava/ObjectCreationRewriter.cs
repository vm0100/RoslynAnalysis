using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalysis.Convert.AnalysisToJava;

public class ObjectCreationRewriter : RewriterBase<ObjectCreationExpressionSyntax>
{
    public ObjectCreationRewriter(ObjectCreationExpressionSyntax typeDeclaration) : base(typeDeclaration)
    {
    }

    public static ObjectCreationRewriter Build(ObjectCreationExpressionSyntax typeDeclaration) => new ObjectCreationRewriter(typeDeclaration);

    public override ObjectCreationRewriter Visit()
    {
        var type = _declaration.Type;
        if (type.Iskind(SyntaxKind.GenericName))
        {

        }

        return this;
    }

    public void VisitList()
    {

    }
}