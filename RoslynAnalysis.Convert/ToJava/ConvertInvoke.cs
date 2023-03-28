using System.Text;

using RoslynAnalysis.Convert.AnalysisToJava;

namespace RoslynAnalysis.Convert.ToJava;

public class ConvertInvoke
{
    public static string GenerateCode(ExpressionSyntax exp, int indent = 0)
    {
        if (exp is BinaryExpressionSyntax syntax)
        {
            return GenerateBinary(syntax);
        }

        return (SyntaxKind)exp.RawKind switch
        {
            // 对象创建
            SyntaxKind.ObjectCreationExpression => GenerateObjectCreation((ObjectCreationExpressionSyntax)exp),
            // 数组创建
            SyntaxKind.ArrayCreationExpression => GenerateArrayCreation((ArrayCreationExpressionSyntax)exp),
            // 内置类型
            SyntaxKind.StringLiteralExpression
                or SyntaxKind.NumericLiteralExpression
                or SyntaxKind.CharacterLiteralExpression
                or SyntaxKind.TrueLiteralExpression
                or SyntaxKind.FalseLiteralExpression
                or SyntaxKind.NullLiteralExpression => GenerateLiteral((LiteralExpressionSyntax)exp),
            // 属性 字段 调用
            SyntaxKind.SimpleMemberAccessExpression => GenerateSimpleMemberAccess((MemberAccessExpressionSyntax)exp),
            // 属性设置
            SyntaxKind.SimpleAssignmentExpression => GenerateSetProperty((AssignmentExpressionSyntax)exp),
            // 方法调用
            SyntaxKind.InvocationExpression => GenerateInvocation((InvocationExpressionSyntax)exp, indent),
            // 数组初始化
            SyntaxKind.ArrayInitializerExpression => GenerateArrayInitializer((InitializerExpressionSyntax)exp),
            // 复杂类型初始化 暂时只有字典场景
            SyntaxKind.ComplexElementInitializerExpression => GenerateDictioniaryInitializer((InitializerExpressionSyntax)exp),
            // 取定义类型名称
            SyntaxKind.IdentifierName => GenerateIdentifierName((IdentifierNameSyntax)exp),
            // 取类型定义名称
            SyntaxKind.PredefinedType => ConvertCommon.TypeToJava((PredefinedTypeSyntax)exp),
            // 后置一元运算符 比如i++;i--
            SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression or SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression => GeneratePostfixUnary((PostfixUnaryExpressionSyntax)exp),
            // 逻辑否
            SyntaxKind.LogicalNotExpression => GenerateLogicalNot((PrefixUnaryExpressionSyntax)exp),
            // 括号
            SyntaxKind.ParenthesizedExpression => GenerateParenthesized((ParenthesizedExpressionSyntax)exp),
            // 单参数lambda
            SyntaxKind.SimpleLambdaExpression => GenerateSimpleLambda((SimpleLambdaExpressionSyntax)exp, indent),
            // 多参数lambda
            SyntaxKind.ParenthesizedLambdaExpression => GenerateParenthesizedLambda((ParenthesizedLambdaExpressionSyntax)exp, indent),
            // 条件运算
            SyntaxKind.ConditionalExpression => GenerateConnditional((ConditionalExpressionSyntax)exp),
            // 条件访问（可空运算？）
            SyntaxKind.ConditionalAccessExpression => GenerateConditionalAccess((ConditionalAccessExpressionSyntax)exp, indent),
            SyntaxKind.MemberBindingExpression => GenerateMemberBinding((MemberBindingExpressionSyntax)exp),
            // 泛型类型
            SyntaxKind.GenericName => ConvertType.GenerateCode((GenericNameSyntax)exp),
            // 匿名方法
            SyntaxKind.AnonymousMethodExpression => GenerateAnonymousMethod((AnonymousMethodExpressionSyntax)exp, indent),
            // 插值字符串
            SyntaxKind.InterpolatedStringExpression => GenerateInterpolatedString((InterpolatedStringExpressionSyntax)exp),
            _ => exp.ToString(),
        };
    }

    /// <summary>
    /// 对象创建
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    private static string GenerateObjectCreation(ObjectCreationExpressionSyntax exp)
    {
        var rewriterExp = new ObjectCreationRewriter().Visit(exp) as ExpressionSyntax;
        if (rewriterExp.IsKind(SyntaxKind.ObjectCreationExpression) == false)
        {
            return GenerateCode(rewriterExp);
        }

        var initializerSyntax = exp.Initializer;

        var className = exp.Type.ToString();
        var args = ConvertArgument.GenerateCode(exp.ArgumentList);

        var sbdr = new StringBuilder($"new {className}({args})", exp.Span.Length);

        if (initializerSyntax != null && initializerSyntax.Expressions.Count > 0)
        {
            var initializerExp = initializerSyntax.Expressions;

            sbdr.Append(" {{ ");
            sbdr.Append(initializerSyntax.Expressions.Select(iexp => GenerateCode(iexp) + ";").ExpandAndToString(" "));
            sbdr.Append("}}");
        }

        return sbdr.ToString();
    }

    public static string GenerateSetProperty(AssignmentExpressionSyntax exp)
    {
        var sbdr = new StringBuilder(exp.Span.Length);

        if (exp.Left.IsKind(SyntaxKind.IdentifierName))
        {
            // 如果设置的值是一个声明
            if (exp.Parent.IsKind(SyntaxKind.ExpressionStatement))
            {
                sbdr.Append(GenerateCode(exp.Left) + " = " + GenerateCode(exp.Right));
                return sbdr.ToString();
            }

            sbdr.Append($"set{GenerateCode(exp.Left)}");
        }
        else if (exp.Left.IsKind(SyntaxKind.ElementAccessExpression))
        {
            sbdr.Append(GenerateCode(exp.Left));
        }
        else
        {
            // 字段属性获取
            var propGetCode = GenerateCode(exp.Left);

            var lastPropIdx = propGetCode.LastIndexOf(".") + 1;
            var lastPropBrackets = propGetCode.LastIndexOf("(");
            var propSetCode = string.Concat(propGetCode.AsSpan(0, lastPropIdx), "s", propGetCode.AsSpan(lastPropIdx + 1, lastPropBrackets - lastPropIdx - 1));
            sbdr.Append(propSetCode);
        }
        sbdr.Append("(" + GenerateCode(exp.Right) + ")");

        return sbdr.ToString();
    }

    /// <summary>
    /// 数组对象创建
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    private static string GenerateArrayCreation(ArrayCreationExpressionSyntax exp)
    {
        var initType = ConvertType.GenerateCode(exp.Type.ElementType);
        var initializerSyntax = exp.Initializer;
        var isNoInitializer = initializerSyntax == null;

        var sbdr = new StringBuilder(200);
        sbdr.Append("new " + initType);

        foreach (ArrayRankSpecifierSyntax rank in exp.Type.RankSpecifiers)
        {
            foreach (ExpressionSyntax size in rank.Sizes)
            {
                sbdr.Append("[" + (isNoInitializer ? size : string.Empty) + "]");
            }
        }

        if (isNoInitializer)
        {
            return sbdr.ToString();
        }

        sbdr.Append(" { ");
        sbdr.Append(string.Join(", ", initializerSyntax.Expressions.Select(GenerateCode)));
        sbdr.Append(" }");

        return sbdr.ToString();
    }

    /// <summary>
    /// 数组初始化
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    private static string GenerateArrayInitializer(InitializerExpressionSyntax exp)
    {
        return $"{{{string.Join(", ", exp.Expressions.Select(GenerateCode))}}}";
    }

    /// <summary>
    /// 字典初始化
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    private static string GenerateDictioniaryInitializer(InitializerExpressionSyntax exp)
    {
        return $"put({string.Join(", ", exp.Expressions.Select(GenerateCode))})";
    }

    /// <summary>
    /// 简单类型赋值
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    public static string GenerateLiteral(LiteralExpressionSyntax exp)
    {
        // 赋值语句
        if (exp.Parent.IsKind(SyntaxKind.EqualsValueClause))
        {
            var javaType = ConvertType.GenerateCode(((VariableDeclarationSyntax)exp.Parent.Parent.Parent).Type);
            if (javaType == "BigDecimal")
            {
                if (exp.Token.ValueText == "0")
                {
                    return "BigDecimal.ZERO";
                }

                return $"BigDecimal.valueOf({exp})";
            }
        }

        return exp.ToString();
    }

    /// <summary>
    /// 属性/字段调用
    /// </summary>
    /// <param name="expressionSyntax"></param>
    /// <param name="argsSyntax"></param>
    /// <returns></returns>
    public static string GenerateSimpleMemberAccess(MemberAccessExpressionSyntax exp)
    {
        var className = exp.Expression.IsKind(SyntaxKind.IdentifierName) ? ConvertCommon.TypeToJava((IdentifierNameSyntax)exp.Expression) : GenerateCode(exp.Expression);
        string invokeMethodName = exp.Name.ToString();
        return ConvertCommon.ConstantToJava(className, invokeMethodName).NullWhiteSpaceOrElse($"{className}.get{invokeMethodName}()");
    }

    /// <summary>
    /// 静态函数调用
    /// </summary>
    /// <returns></returns>
    public static string GenerateStaticMethodInvoke(MemberAccessExpressionSyntax exp, ArgumentListSyntax argumentSyntax, int indent = 0)
    {
        // TODO: 未经过exp.expression = IdentifierName时未经过类型转换
        var className = GenerateCode(exp.Expression, indent);
        string invokeName = GenerateCode(exp.Name);
        string args = ConvertArgument.GenerateCode(argumentSyntax, indent);

        if (className.In("BaseSlxtEntity", "EntityFactory") && exp.Name.IsKind(SyntaxKind.GenericName))
        {
            var genericArg = ((GenericNameSyntax)exp.Name).TypeArgumentList.Arguments.First();
            if (genericArg is IdentifierNameSyntax identifier)
            {
                return $"BaseSlxtEntity.create({ConvertCommon.TypeToJava(identifier)}.class)";
            }

            return $"BaseSlxtEntity.create({ConvertCommon.TypeToJava(genericArg)}.class)";
        }

        var javaCode = ConvertCommon.ConstantToJava(className, invokeName, args);

        if (javaCode.IsNotNullOrWhiteSpace())
        {
            return javaCode;
        }

        if (invokeName == "Equals" && argumentSyntax.Arguments.Count > 1 && argumentSyntax.Arguments[1].ToString() == "StringComparison.OrdinalIgnoreCase")
        {
            return className + "." + "equalsIgnoreCase(" + ConvertArgument.GenerateCode(argumentSyntax.Arguments[0]) + ")";
        }

        string javaMethodName = ConvertCommon.MethodToJava(className, invokeName, args);
        if (className.EndsWith("Enum"))
        {
            return className + "." + javaMethodName + ".getValue()";
        }

        if (className.InEndsWith("Const"))
        {
            return className + "." + javaMethodName;
        }

        if (className.EndsWith("Res"))
        {
            return className + "." + javaMethodName + "()";
        }

        return $"{className}.{javaMethodName}";
    }

    /// <summary>
    /// 本地函数调用
    /// </summary>
    /// <returns></returns>
    public static string GenerateInvocation(InvocationExpressionSyntax exp, int indent = 0)
    {
        if (exp.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
        {
            return GenerateStaticMethodInvoke((MemberAccessExpressionSyntax)exp.Expression, exp.ArgumentList, indent);
        }

        if (exp.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
        {
            var memberAccessExp = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(""), SyntaxFactory.IdentifierName(GenerateCode(exp.Expression)));

            return GenerateStaticMethodInvoke(memberAccessExp, exp.ArgumentList, indent).TrimStart('.');
        }

        string invokeMethodName = GenerateCode(exp.Expression, indent)?.ToLowerTitleCase();
        var args = ConvertArgument.GenerateCode(exp.ArgumentList, indent);

        if (invokeMethodName == "nameof")
        {
            var argVal = (args.Contains('.') ? args[args.LastIndexOf('.')..] : args).TrimStart('.');
            if (argVal.StartsWith("get"))
            {
                argVal = argVal[3..];
            }
            if (argVal.EndsWith("()"))
            {
                argVal = argVal[..(argVal.Length - 2)];
            }
            return "\"" + argVal + "\"";
        }

        return $"{invokeMethodName}({args})";
    }

    /// <summary>
    /// 合并（二元）运算符
    /// </summary>
    /// <returns></returns>
    public static string GenerateCoalesce(BinaryExpressionSyntax exp)
    {
        var left = GenerateCode(exp.Left);
        var right = GenerateCode(exp.Right);
        return $"{left} == null ? {right} : {left}";
    }

    /// <summary>
    /// 后置一元 暂时只知道++ --
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    public static string GeneratePostfixUnary(PostfixUnaryExpressionSyntax exp)
    {
        var identifierName = GenerateCode(exp.Operand);

        if (exp.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
        {
            if (exp.IsKind(SyntaxKind.PostIncrementExpression))
            {
                return $"{identifierName.Replace(".get", ".set").TrimEnd(')')}{identifierName} + 1)";
            }

            if (exp.IsKind(SyntaxKind.PostDecrementExpression))
            {
                return $"{identifierName.Replace(".get", ".set").TrimEnd(')')}{identifierName} - 1)";
            }
        }

        return identifierName + exp.OperatorToken.Text;
    }

    /// <summary>
    /// 括号
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    public static string GenerateParenthesized(ParenthesizedExpressionSyntax exp)
    {
        return $"({GenerateCode(exp.Expression)})";
    }

    public static string GenerateLogicalNot(PrefixUnaryExpressionSyntax exp)
    {
        return "!" + GenerateCode(exp.Operand);
    }

    public static string GenerateBinary(BinaryExpressionSyntax exp)
    {
        var sbdr = new StringBuilder();

        var left = GenerateCode(exp.Left);
        var right = GenerateCode(exp.Right);

        var isDateCondition = left.IsEndsWithDate() || right.IsEndsWithDate();

        if (isDateCondition)
        {
            return (SyntaxKind)exp.RawKind switch
            {
                // equals
                SyntaxKind.EqualsExpression => $"DateUtil.equalDate({left}, {right})",
                SyntaxKind.NotEqualsExpression => $"DateUtil.equalDate({left}, {right}) == false",
                SyntaxKind.LessThanExpression => $"DateUtil.lessThenDate({left}, {right})",
                SyntaxKind.LessThanOrEqualExpression => $"DateUtil.lessThenEqualDate({left}, {right})",
                SyntaxKind.GreaterThanExpression => $"DateUtil.greatThenDate({left}, {right})",
                SyntaxKind.GreaterThanOrEqualExpression => $"DateUtil.greatThenEqualDate({left}, {right})",
                // 合并运算符（二元） 暂只支持 ?? 运算符
                SyntaxKind.CoalesceExpression => GenerateCoalesce(exp),
                _ => left + " " + exp.OperatorToken.ValueText + " " + right
            };
        }

        // 替换any语句产生的 size() > 0
        if (left.EndsWith("> 0") && right == "false")
        {
            return left[..(left.LastIndexOf(">"))] + "< 1";
        }

        var rightIsZero = right.Equals("0");
        var rightIsNullOrZero = exp.Right.IsKind(SyntaxKind.NullLiteralExpression) || exp.Right.IsBoolExpressioon();

        return (SyntaxKind)exp.RawKind switch
        {
            // equals
            SyntaxKind.EqualsExpression => rightIsZero ? $"NumberUtil.equalsZero({left})" : rightIsNullOrZero ? $"{left} == {right}" : $"{left}.equals({right})",
            SyntaxKind.NotEqualsExpression => rightIsZero ? $"NumberUtil.equalsZero({left}) == false" : rightIsNullOrZero ? $"{left} != {right}" : $"{left}.equals({right}) == false",
            SyntaxKind.LessThanExpression => rightIsZero ? $"NumberUtil.lessThenZero({left})" : $"NumberUtil.lessThenNumber({left}, {right})",
            SyntaxKind.LessThanOrEqualExpression => rightIsZero ? $"NumberUtil.lessThenEqualZero({left})" : $"NumberUtil.lessThenEqualNumber({left}, {right})",
            SyntaxKind.GreaterThanExpression => rightIsZero ? $"NumberUtil.greatThenZero({left})" : $"NumberUtil.greatThenNumber({left}, {right})",
            SyntaxKind.GreaterThanOrEqualExpression => rightIsZero ? $"NumberUtil.greatThenEqualZero({left})" : $"NumberUtil.greatThenEqualNumber({left}, {right})",
            // 合并运算符（二元） 暂只支持 ?? 运算符
            SyntaxKind.CoalesceExpression => GenerateCoalesce(exp),
            _ => left + " " + exp.OperatorToken.ValueText + " " + right
        };
    }

    /// <summary>
    /// 简单lambda
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    public static string GenerateSimpleLambda(SimpleLambdaExpressionSyntax exp, int indent = 0)
    {
        var left = ConvertParameter.GenerateCode(exp.Parameter) + " -> ";
        if (exp.Block == null)
        {
            return left + GenerateCode(exp.ExpressionBody, indent);
        }

        return left + "{\n " + ConvertMethod.GenerateBody(exp.Block, indent) + "\n" + " }".PadIndented(indent);
    }

    /// <summary>
    /// 带括号lambda
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    public static string GenerateParenthesizedLambda(ParenthesizedLambdaExpressionSyntax exp, int indent = 0)
    {
        var param = ConvertParameter.GenerateCode(exp.ParameterList);
        if (exp.ParameterList.Parameters.Count > 1)
        {
            param = $"({param})";
        }

        var left = param + " -> ";
        if (exp.Block == null)
        {
            return left + GenerateCode(exp.ExpressionBody, indent);
        }

        return left + "{\n " + ConvertMethod.GenerateBody(exp.Block, indent) + "\n" + " }".PadIndented(indent);
    }

    /// <summary>
    /// 条件表达式
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    public static string GenerateConnditional(ConditionalExpressionSyntax exp)
    {
        return GenerateCode(exp.Condition) + " ? " + GenerateCode(exp.WhenTrue) + " : " + GenerateCode(exp.WhenFalse);
    }

    /// <summary>
    /// 条件表达式访问
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    public static string GenerateConditionalAccess(ConditionalAccessExpressionSyntax exp, int indent = 0)
    {
        //// 调用者
        //if (exp.Parent.IsKind(SyntaxKind.Block) || exp.Parent.IsKind(SyntaxKind.ExpressionStatement))
        //{
        //    var ifCondition = SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, exp.Expression, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

        //    var blockExp = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, exp.Expression, SyntaxFactory.IdentifierName(GenerateCode(exp.WhenNotNull)));

        //    var ifBlock = SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ExpressionStatement(blockExp)));
        //    var ifStatement = SyntaxFactory.IfStatement(ifCondition, ifBlock);
        //    return ConvertMethod.GenerateIfStatement(ifStatement, indent - 1).TrimStart(' ');
        //}

        var prop = GenerateCode(exp.Expression);
        //// 如果是连续计算
        //if (exp.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
        //{
        //    prop = GenerateCode(((ConditionalAccessExpressionSyntax)exp.Parent).Expression) + "." + prop;
        //}

        var whenNotNull = GenerateCode(exp.WhenNotNull);
        return $"{prop}?.{whenNotNull}";
    }

    public static string GenerateMemberBinding(MemberBindingExpressionSyntax exp)
    {
        /**
         * 1. 当前是属性
         * 2. 当前是方法 parent = invoke
         */

        if (exp.Parent.IsKind(SyntaxKind.InvocationExpression))
        {
            var invokeMethod = ConvertCommon.MethodToJava(string.Empty, exp.Name.Identifier.ValueText, string.Empty);
            if (invokeMethod.EndsWith("()"))
            {
                return invokeMethod[..(invokeMethod.Length - 2)];
            }
            return invokeMethod;
        }

        return "get" + exp.Name.Identifier.ValueText.ToUpperTitleCase() + "()";
    }

    public static string GenerateAnonymousMethod(AnonymousMethodExpressionSyntax exp, int indent = 0)
    {
        var param = ConvertParameter.GenerateCode(exp.ParameterList);
        if (exp.ParameterList.Parameters.Count > 1)
        {
            param = $"({param})";
        }

        if (exp.Block == null)
        {
            return param + " -> {\n" + GenerateCode(exp.ExpressionBody, indent + 1) + "}".PadIndented(indent);
        }

        return param + " -> {\n" + ConvertMethod.GenerateBody(exp.Block, indent + 1) + "}".PadIndented(indent);
    }

    public static string GenerateIdentifierName(IdentifierNameSyntax exp)
    {
        var identifierName = exp.Identifier.ValueText.TrimStart('_');
        var lastDotIdx = identifierName.LastIndexOf('.');
        if (lastDotIdx < 0)
        {
            return identifierName;
        }

        return identifierName[(lastDotIdx + 1)..];
    }

    public static string GenerateInterpolatedString(InterpolatedStringExpressionSyntax exp)
    {
        var sbdr = new StringBuilder("StringUtil.formatMessage(\"", exp.Span.Length);
        var i = 0;
        var args = new List<string>();
        foreach (var syntax in exp.Contents)
        {
            if (syntax.IsKinds(SyntaxKind.Interpolation, SyntaxKind.InterpolationFormatClause, SyntaxKind.InterpolationAlignmentClause))
            {
                sbdr.Append("{" + i++ + "}");

                InterpolationSyntax interpolationSyntax = (InterpolationSyntax)syntax;
                var arg = GenerateCode(interpolationSyntax.Expression);
                if (interpolationSyntax.FormatClause != null)
                {
                    arg += ".toString(\"" + interpolationSyntax.FormatClause.FormatStringToken.Text + "\")";
                }
                if (interpolationSyntax.AlignmentClause != null)
                {
                    if (interpolationSyntax.AlignmentClause.Value.IsKind(SyntaxKind.UnaryMinusExpression))
                    {
                        arg = "StringUtil.padLeft(" + arg + ", " + ((PrefixUnaryExpressionSyntax)interpolationSyntax.AlignmentClause.Value).Operand + ")";
                    }
                    else
                    {
                        arg = "StringUtil.padRight(" + arg + ", " + interpolationSyntax.AlignmentClause.Value + ")";
                    }
                }
                args.Add(arg);
                continue;
            }

            sbdr.Append(syntax.ToString());
        }
        sbdr.Append('"');
        if (args.Count > 0)
        {
            sbdr.Append(", ");
            sbdr.Append(string.Join(", ", args));
        }
        sbdr.Append(')');
        return sbdr.ToString();
    }
}