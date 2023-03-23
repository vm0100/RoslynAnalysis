using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertParameter
{
    public static string GenerateCode(ParameterListSyntax parameterList)
    {
        return string.Join(", ", parameterList.Parameters.Select(argSyntax => GenerateCode(argSyntax)));
    }

    public static string GenerateCode(ParameterSyntax parameter)
    {
        if (parameter.Type == null)
        {
            return parameter.Identifier.ValueText;
        }
        return ConvertType.GenerateCode(parameter.Type) + " " + parameter.Identifier.ValueText;
    }
}