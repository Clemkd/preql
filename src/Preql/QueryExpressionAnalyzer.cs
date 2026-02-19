using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace Preql;

/// <summary>
/// Analyzes lambda expression trees to generate SQL without executing or proxying the lambda.
/// The interpolated string <c>$"SELECT {u.Name} FROM {u}"</c> is parsed from the expression tree
/// to identify table references, column references, and parameter values.
/// </summary>
internal static class QueryExpressionAnalyzer
{
    private record ParameterTableInfo(ParameterExpression Parameter, string TableName, string Alias);

    /// <summary>Analyzes a single-type query expression.</summary>
    public static QueryResult Analyze<T>(
        Expression<Func<T, FormattableString>> lambda,
        SqlDialect dialect) where T : class
    {
        var paramInfos = new[]
        {
            new ParameterTableInfo(lambda.Parameters[0], GetTableName(typeof(T)), lambda.Parameters[0].Name ?? "t0")
        };
        return AnalyzeCore(lambda.Body, paramInfos, dialect);
    }

    /// <summary>Analyzes a two-type query expression.</summary>
    public static QueryResult Analyze<T1, T2>(
        Expression<Func<T1, T2, FormattableString>> lambda,
        SqlDialect dialect) where T1 : class where T2 : class
    {
        var paramInfos = new[]
        {
            new ParameterTableInfo(lambda.Parameters[0], GetTableName(typeof(T1)), lambda.Parameters[0].Name ?? "t0"),
            new ParameterTableInfo(lambda.Parameters[1], GetTableName(typeof(T2)), lambda.Parameters[1].Name ?? "t1")
        };
        return AnalyzeCore(lambda.Body, paramInfos, dialect);
    }

    /// <summary>Analyzes a three-type query expression.</summary>
    public static QueryResult Analyze<T1, T2, T3>(
        Expression<Func<T1, T2, T3, FormattableString>> lambda,
        SqlDialect dialect) where T1 : class where T2 : class where T3 : class
    {
        var paramInfos = new[]
        {
            new ParameterTableInfo(lambda.Parameters[0], GetTableName(typeof(T1)), lambda.Parameters[0].Name ?? "t0"),
            new ParameterTableInfo(lambda.Parameters[1], GetTableName(typeof(T2)), lambda.Parameters[1].Name ?? "t1"),
            new ParameterTableInfo(lambda.Parameters[2], GetTableName(typeof(T3)), lambda.Parameters[2].Name ?? "t2")
        };
        return AnalyzeCore(lambda.Body, paramInfos, dialect);
    }

    /// <summary>Analyzes a four-type query expression.</summary>
    public static QueryResult Analyze<T1, T2, T3, T4>(
        Expression<Func<T1, T2, T3, T4, FormattableString>> lambda,
        SqlDialect dialect) where T1 : class where T2 : class where T3 : class where T4 : class
    {
        var paramInfos = new[]
        {
            new ParameterTableInfo(lambda.Parameters[0], GetTableName(typeof(T1)), lambda.Parameters[0].Name ?? "t0"),
            new ParameterTableInfo(lambda.Parameters[1], GetTableName(typeof(T2)), lambda.Parameters[1].Name ?? "t1"),
            new ParameterTableInfo(lambda.Parameters[2], GetTableName(typeof(T3)), lambda.Parameters[2].Name ?? "t2"),
            new ParameterTableInfo(lambda.Parameters[3], GetTableName(typeof(T4)), lambda.Parameters[3].Name ?? "t3")
        };
        return AnalyzeCore(lambda.Body, paramInfos, dialect);
    }

    /// <summary>Analyzes a five-type query expression.</summary>
    public static QueryResult Analyze<T1, T2, T3, T4, T5>(
        Expression<Func<T1, T2, T3, T4, T5, FormattableString>> lambda,
        SqlDialect dialect) where T1 : class where T2 : class where T3 : class where T4 : class where T5 : class
    {
        var paramInfos = new[]
        {
            new ParameterTableInfo(lambda.Parameters[0], GetTableName(typeof(T1)), lambda.Parameters[0].Name ?? "t0"),
            new ParameterTableInfo(lambda.Parameters[1], GetTableName(typeof(T2)), lambda.Parameters[1].Name ?? "t1"),
            new ParameterTableInfo(lambda.Parameters[2], GetTableName(typeof(T3)), lambda.Parameters[2].Name ?? "t2"),
            new ParameterTableInfo(lambda.Parameters[3], GetTableName(typeof(T4)), lambda.Parameters[3].Name ?? "t3"),
            new ParameterTableInfo(lambda.Parameters[4], GetTableName(typeof(T5)), lambda.Parameters[4].Name ?? "t4")
        };
        return AnalyzeCore(lambda.Body, paramInfos, dialect);
    }

    private static QueryResult AnalyzeCore(
        Expression body,
        IReadOnlyList<ParameterTableInfo> paramInfos,
        SqlDialect dialect)
    {
        // Build fast lookup: ParameterExpression -> table info
        var paramMap = paramInfos.ToDictionary(p => p.Parameter);

        if (body is MethodCallExpression methodCall && IsFormattableStringCreate(methodCall))
        {
            return ProcessFormattableStringCreate(methodCall, paramMap, dialect);
        }

        throw new InvalidOperationException(
            "The lambda body must be an interpolated string that returns FormattableString. " +
            "Example: (u) => $\"SELECT {u.Name} FROM {u}\"");
    }

    private static QueryResult ProcessFormattableStringCreate(
        MethodCallExpression call,
        Dictionary<ParameterExpression, ParameterTableInfo> paramMap,
        SqlDialect dialect)
    {
        // Arguments[0] is the format string constant
        if (call.Arguments[0] is not ConstantExpression formatConst || formatConst.Value is not string format)
            throw new InvalidOperationException("Could not extract the format string from the interpolated string.");

        // Arguments[1] is either a NewArrayExpression (params array) or arguments are listed individually
        IReadOnlyList<Expression> argExprs = call.Arguments.Count == 2 && call.Arguments[1] is NewArrayExpression arr
            ? (IReadOnlyList<Expression>)arr.Expressions
            : call.Arguments.Skip(1).ToList();

        var sqlBuilder = new StringBuilder();
        var parameters = new List<object?>();
        int sqlParamIndex = 0;

        // Walk through the format string, replacing {n} placeholders with SQL fragments
        int lastPos = 0;
        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] != '{')
                continue;

            if (i + 1 < format.Length && format[i + 1] == '{')
            {
                // Escaped {{ → emit single {
                sqlBuilder.Append(format, lastPos, i - lastPos + 1);
                i++;
                lastPos = i + 1;
                continue;
            }

            int endBrace = format.IndexOf('}', i + 1);
            if (endBrace < 0)
                continue;

            // Emit literal text before this placeholder
            sqlBuilder.Append(format, lastPos, i - lastPos);

            // Parse the placeholder index (may have format specifiers like {0:d} or alignment {0,-5})
            var placeholder = format.AsSpan(i + 1, endBrace - i - 1);
            var separatorIdx = placeholder.IndexOfAny([':', ',']);
            var indexSpan = separatorIdx >= 0 ? placeholder[..separatorIdx] : placeholder;

            if (int.TryParse(indexSpan, out int argIdx) && argIdx < argExprs.Count)
            {
                var argExpr = UnwrapConvert(argExprs[argIdx]);
                ProcessArgument(argExpr, paramMap, dialect, sqlBuilder, parameters, ref sqlParamIndex);
            }

            i = endBrace;
            lastPos = i + 1;
        }

        // Append any trailing literal text
        sqlBuilder.Append(format, lastPos, format.Length - lastPos);

        return new QueryResult(sqlBuilder.ToString(), parameters);
    }

    private static void ProcessArgument(
        Expression argExpr,
        Dictionary<ParameterExpression, ParameterTableInfo> paramMap,
        SqlDialect dialect,
        StringBuilder sqlBuilder,
        List<object?> parameters,
        ref int sqlParamIndex)
    {
        // Case 1: Direct lambda parameter → table reference  e.g. {u} → "Users" u
        if (argExpr is ParameterExpression paramExpr && paramMap.TryGetValue(paramExpr, out var tableInfo))
        {
            sqlBuilder.Append(FormatTableRef(tableInfo.TableName, tableInfo.Alias, dialect));
            return;
        }

        // Case 2: Member access on a lambda parameter → column reference  e.g. {u.Name} → u."Name"
        if (argExpr is MemberExpression memberExpr &&
            memberExpr.Expression is ParameterExpression memberParam &&
            paramMap.TryGetValue(memberParam, out var colTableInfo))
        {
            sqlBuilder.Append(FormatColumnRef(GetColumnName(memberExpr.Member), colTableInfo.Alias, dialect));
            return;
        }

        // Case 3: Anything else → evaluate and pass as a SQL parameter  e.g. {userId} → @p0
        var value = EvaluateExpression(argExpr);
        sqlBuilder.Append($"@p{sqlParamIndex++}");
        parameters.Add(value);
    }

    /// <summary>
    /// Returns the SQL column name for a member, using <see cref="ColumnAttribute"/> if present,
    /// otherwise falling back to the member name (case-sensitive).
    /// </summary>
    private static string GetColumnName(System.Reflection.MemberInfo member)
    {
        var attr = member.GetCustomAttributes(typeof(ColumnAttribute), inherit: true)
                         .OfType<ColumnAttribute>()
                         .FirstOrDefault();
        return attr?.Name ?? member.Name;
    }

    /// <summary>
    /// Evaluates an expression (e.g. a captured local variable) to obtain its runtime value
    /// for use as a SQL parameter.
    /// </summary>
    private static object? EvaluateExpression(Expression expr)
    {
        try
        {
            var lambda = Expression.Lambda<Func<object?>>(Expression.Convert(expr, typeof(object)));
            return lambda.Compile()();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to evaluate expression '{expr}' as a SQL parameter value. " +
                "Ensure all non-table, non-column interpolation arguments are accessible variables or constants.",
                ex);
        }
    }

    private static bool IsFormattableStringCreate(MethodCallExpression call)
    {
        return call.Method.DeclaringType == typeof(FormattableStringFactory)
               && call.Method.Name == "Create";
    }

    /// <summary>Strips <see cref="ExpressionType.Convert"/> wrappers used to box value types.</summary>
    private static Expression UnwrapConvert(Expression expr)
    {
        while (expr is UnaryExpression { NodeType: ExpressionType.Convert } unary)
            expr = unary.Operand;
        return expr;
    }

    private static string GetTableName(Type type)
    {
        var attr = type.GetCustomAttributes(typeof(TableAttribute), inherit: false)
                       .OfType<TableAttribute>()
                       .FirstOrDefault();
        if (attr != null)
            return attr.Name;

        var name = type.Name;
        // Simple pluralization: append "s" unless the name already ends with "s".
        // Note: irregular plurals (e.g. Person→People, Child→Children) are not handled here.
        // Use [Table("...")] to specify an exact table name for these cases.
        return name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? name : name + "s";
    }

    private static string FormatTableRef(string tableName, string alias, SqlDialect dialect)
    {
        var quoted = FormatIdentifier(tableName, dialect);
        return string.IsNullOrEmpty(alias) ? quoted : $"{quoted} {alias}";
    }

    private static string FormatColumnRef(string columnName, string alias, SqlDialect dialect)
    {
        var quoted = FormatIdentifier(columnName, dialect);
        return string.IsNullOrEmpty(alias) ? quoted : $"{alias}.{quoted}";
    }

    private static string FormatIdentifier(string identifier, SqlDialect dialect) =>
        dialect switch
        {
            SqlDialect.PostgreSql => $"\"{identifier}\"",
            SqlDialect.SqlServer  => $"[{identifier}]",
            SqlDialect.MySql      => $"`{identifier}`",
            SqlDialect.Sqlite     => $"\"{identifier}\"",
            _                     => $"[{identifier}]"
        };
}
