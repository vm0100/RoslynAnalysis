using System.Text;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertCommon
{
    public static bool IsInterfaceDeclare(string typeName)
        => !typeName.IsNullOrWhiteSpace() && typeName.Length > 1 && typeName.StartsWith('I') && char.IsUpper(typeName[1]);

    public static string BaeTypeToJava(string propertyType) => propertyType switch
    {
        "Object" or "object" => "Object",
        "long" or "long?" => "Long",
        "byte" or "Byte" or "byte?" => "Byte",
        "bool" or "Boolean" or "bool?" => "Boolean",
        "DateTime" or "DateTime?" => "Date",
        "decimal" or "Decimal" or "decimal?" => "BigDecimal",
        "float" or "float?" => "Float",
        "double" or "Double" or "double?" => "Double",
        "int" or "Int32" or "int?" => "Integer",
        "char" or "Char" or "char?" => "Character",
        "short" or "short?" => "Short",
        "Guid" or "Guid?" => "UUID",
        "string" or "String" => "String",
        "Dictionary" or "IDictionary" => "Map",
        "List" or "Array" => "List",
        "ArgumentNullException" => "IllegalArgumentException",
        "BusinessUnitDomainService" => "DomainService",
        _ => string.Empty
    };

    public static string TypeToJava(TypeSyntax typeSyntax)
    {
        var typeName = typeSyntax.IsKind(SyntaxKind.GenericName) ? ((GenericNameSyntax)typeSyntax).Identifier.ValueText : typeSyntax.ToString();
        if (typeName.StartsWith("_"))
        {
            return typeName.TrimStart('_');
        }

        if (typeName.Contains('.'))
        {
            typeName = typeName[(typeName.LastIndexOf('.')..)];
        }

        string propertyType = BaeTypeToJava(typeName);

        // 如果首字母是小写，并且可以转换为基础类型
        if (typeName[..1].IsUpper() == false && propertyType.IsNotNullOrWhiteSpace())
        {
            return propertyType;
        }

        if (propertyType.IsNotNullOrWhiteSpace())
        {
            if (typeSyntax.Parent.IsKinds(SyntaxKind.Parameter, SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.PropertyDeclaration, SyntaxKind.VariableDeclaration))
            {
                return propertyType switch
                {
                    "HashMap" => "Map",
                    _ => propertyType
                };
            }

            return propertyType;
        }
        else
        {
            propertyType = typeName;
        }

        // 如果父级是属性调用
        if (typeSyntax.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
        {
            switch (propertyType)
            {
                case "Lists":
                    return "Lists";

                case "Maps":
                    return "Maps";
                // 实体工厂
                case "EntityFactory":
                    return "BaseSlxtEntity";
                // Guid生成器
                case "GuidHelper":
                    return "GuidGenerator";
                // DTO基类
                case "BaseDto":
                case "DtoBase":
                    return "DTO";
                // 实体状态
                case "EntityState":
                    return "EntityState";

                default:
                    if (propertyType.EndsWith("Helper"))
                    {
                        return propertyType[..(propertyType.Length - 6)] + "Util";
                    }
                    return propertyType;
            }
        }

        // 如果父级是属性定义
        //if (typeSyntax.Parent.IsKind(SyntaxKind.VariableDeclaration))

        if (propertyType.Length <= 3 || !propertyType[..1].IsUpper() || propertyType.IsInterfaceDeclare() || propertyType.InEndsWith("Enum", "Exception", "Res", "Const", "Arg", "Args", "Service", "Dao"))
        {
            return propertyType;
        }

        if (propertyType.EndsWith("DTO", StringComparison.OrdinalIgnoreCase))
        {
            return propertyType[..(propertyType.Length - 3)] + "DTO";
        }
        if (propertyType.EndsWith("Entity", StringComparison.OrdinalIgnoreCase))
        {
            return propertyType[..(propertyType.Length - 6)] + "Entity";
        }
        return propertyType;
    }

    public static string GenerateImportPackage()
    {
        var sbdr = new StringBuilder(1000);
        sbdr.AppendLine("import java.math.BigDecimal;");
        sbdr.AppendLine("import java.util.Date;");
        sbdr.AppendLine("import java.util.List;");
        sbdr.AppendLine("import java.util.UUID;");
        sbdr.AppendLine("import java.util.Map;");
        sbdr.AppendLine("import java.util.ArrayList;");
        sbdr.AppendLine("import org.apache.commons.lang3.NotImplementedException;");
        return sbdr.ToString();
    }

    public static string KeywordToJava(SyntaxNode node) => KeywordToJava((SyntaxKind)node.RawKind);

    public static string KeywordToJava(SyntaxToken node) => KeywordToJava((SyntaxKind)node.RawKind);

    public static string KeywordToJava(SyntaxKind syntaxKind) => syntaxKind switch
    {
        SyntaxKind.PublicKeyword => "public",
        SyntaxKind.PrivateKeyword => "private",
        SyntaxKind.PropertyKeyword => "protected",
        SyntaxKind.VoidKeyword => "void",
        SyntaxKind.StaticKeyword => "static",
        SyntaxKind.ReadOnlyKeyword or SyntaxKind.ConstKeyword => "final",
        _ => ""
    };

    public static string ConstantToJava(string typeName, string constantName, string args = "")
    {
        switch (typeName)
        {
            case nameof(Guid):
            case "GuidHelper":
            case "GuidGenerator":
            case "UUID":
                return constantName switch
                {
                    nameof(Guid.Empty) => "UUIDUtil.emptyUUID()",
                    nameof(Guid.Parse) => $"UUIDUtil.parse({args})",
                    nameof(Guid.NewGuid) or "NewSeqGuid" => "GuidGenerator.generateRandomGuid()",
                    _ => string.Empty
                };

            case "string":
            case "String":
            case "StringUtil":
                return constantName switch
                {
                    nameof(string.Empty) => $"StringUtil.empty",
                    nameof(string.Format) => $"StringUtil.formatMessage({args})",
                    _ => string.Empty
                };

            case "int":
            case "Int32":
            case "Integer":
                return constantName switch
                {
                    nameof(int.MaxValue) => "Integer.MAX_VALUE",
                    nameof(int.MinValue) => "Integer.MIN_VALUE",
                    _ => string.Empty
                };

            case "decimal":
            case "BigDecimal":
                return constantName switch
                {
                    nameof(decimal.Zero) => "BigDecimal.ZERO",
                    _ => string.Empty
                };

            case "Date":
                return constantName switch
                {
                    nameof(DateTime.Now) => "Date.from(clock.now().toInstant())",
                    nameof(DateTime.Today) => "clock.now().toLocalDate().convertToUserTimezone()",
                    _ => string.Empty
                };

            case "Date.from(clock.now().toInstant())":
            case "clock.now().toLocalDate().convertToUserTimezone()":
                return constantName switch
                {
                    nameof(DateTime.AddDays) => $"DateUtil.addDay({typeName}, {args})",
                    nameof(DateTime.AddMinutes) => $"DateUtil.addMinute({typeName}, {args})",
                    nameof(DateTime.AddMonths) => $"DateUtil.addMonth({typeName}, {args})",
                    nameof(DateTime.ToString) => $"DateTimeUtil.dateToString({typeName}, {args})",
                    _ => string.Empty
                };

            case "EntityState":
                return typeName + "." + constantName;

            default:
                if (typeName == "BaseSlxtEntity" && constantName == "Value")
                {
                    return typeName;
                }
                if (typeName.EndsWith("Enum"))
                {
                    return typeName + "." + constantName + ".getValue()";
                }
                if (typeName.EndsWith("Repository") && constantName == "Value")
                {
                    return typeName;
                }
                return args.IsNull() ? typeName + "." + constantName : typeName + "." + constantName + "(" + args + ")";
        }
    }

    public static string MethodToJava(string className, string methodName, string args)
    {
        switch (methodName)
        {
            case "Select":
                return $"map({args})";

            case "Where":
                return $"filter({args})";

            case "ToList":
                return "collect(Collectors.toList())";

            case "Take":
                return $"limit({args})";

            case "OrderBy":
                return $"sorted(Comparator.comparing({args}))";

            case "NewSeqGuid":
                return "generateRandomGuid()";

            case "AddRange":
                return $"addAll({args})";

            default:
                return $"{methodName.ToLowerTitleCase()}({args})";
        }
    }
}