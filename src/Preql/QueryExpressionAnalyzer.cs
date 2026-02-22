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
    public static FormattableString Analyze<T>(
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
    public static FormattableString Analyze<T1, T2>(
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
    public static FormattableString Analyze<T1, T2, T3>(
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
    public static FormattableString Analyze<T1, T2, T3, T4>(
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
    public static FormattableString Analyze<T1, T2, T3, T4, T5>(
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

    private static FormattableString AnalyzeCore(
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

    private static FormattableString ProcessFormattableStringCreate(
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

        // SQLite does not support table aliases in UPDATE/DELETE statements.
        // Detect such statements and suppress aliases for SQLite.
        bool suppressAlias = dialect == SqlDialect.Sqlite && IsUpdateOrDelete(format);

        var formatBuilder = new StringBuilder();
        var parameters = new List<object?>();
        int paramIndex = 0;

        // Walk through the format string, replacing {n} placeholders with SQL fragments
        int lastPos = 0;
        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] != '{')
                continue;

            if (i + 1 < format.Length && format[i + 1] == '{')
            {
                // Escaped {{ → keep {{ escaped in format
                AppendEscapedForInterpolation(formatBuilder, format, lastPos, i - lastPos);
                formatBuilder.Append("{{");
                i++;
                lastPos = i + 1;
                continue;
            }

            int endBrace = format.IndexOf('}', i + 1);
            if (endBrace < 0)
                continue;

            // Emit literal text before this placeholder
            AppendEscapedForInterpolation(formatBuilder, format, lastPos, i - lastPos);

            // Parse the placeholder index (may have format specifiers like {0:d} or alignment {0,-5})
            var placeholder = format.AsSpan(i + 1, endBrace - i - 1);
            var separatorIdx = placeholder.IndexOfAny([':', ',']);
            var indexSpan = separatorIdx >= 0 ? placeholder[..separatorIdx] : placeholder;

            if (int.TryParse(indexSpan, out int argIdx) && argIdx < argExprs.Count)
            {
                var argExpr = UnwrapConvert(argExprs[argIdx]);
                ProcessArgument(argExpr, paramMap, dialect, formatBuilder, parameters, ref paramIndex, suppressAlias);
            }

            i = endBrace;
            lastPos = i + 1;
        }

        // Append any trailing literal text
        AppendEscapedForInterpolation(formatBuilder, format, lastPos, format.Length - lastPos);

        return FormattableStringFactory.Create(formatBuilder.ToString(), parameters.ToArray());
    }

    private static void ProcessArgument(
        Expression argExpr,
        Dictionary<ParameterExpression, ParameterTableInfo> paramMap,
        SqlDialect dialect,
        StringBuilder formatBuilder,
        List<object?> parameters,
        ref int paramIndex,
        bool suppressAlias = false)
    {
        // Case 1: Direct lambda parameter → table reference  e.g. {u} → "Users" u
        if (argExpr is ParameterExpression paramExpr && paramMap.TryGetValue(paramExpr, out var tableInfo))
        {
            var tableRef = FormatTableRef(tableInfo.TableName, tableInfo.Alias, dialect, suppressAlias);
            AppendEscapedForInterpolation(formatBuilder, tableRef);
            return;
        }

        // Case 2: Member access on a lambda parameter → column reference  e.g. {u.Name} → u."Name"
        if (argExpr is MemberExpression memberExpr &&
            memberExpr.Expression is ParameterExpression memberParam &&
            paramMap.TryGetValue(memberParam, out var colTableInfo))
        {
            var colRef = FormatColumnRef(GetColumnName(memberExpr.Member), colTableInfo.Alias, dialect, suppressAlias);
            AppendEscapedForInterpolation(formatBuilder, colRef);
            return;
        }

        // Case 3: Anything else → evaluate and pass as a positional parameter  e.g. {userId} → {0}
        var value = EvaluateExpression(argExpr);
        formatBuilder.Append($"{{{paramIndex}}}");
        paramIndex++;
        parameters.Add(value);
    }

    /// <summary>
    /// Appends a substring to <paramref name="sb"/>, escaping <c>{</c> as <c>{{</c>
    /// and <c>}</c> as <c>}}</c> so the result is safe to use inside a
    /// <see cref="FormattableString"/> format string.
    /// </summary>
    private static void AppendEscapedForInterpolation(StringBuilder sb, string text, int start, int length)
    {
        for (int i = start; i < start + length; i++)
        {
            char c = text[i];
            if (c == '{') sb.Append("{{");
            else if (c == '}') sb.Append("}}");
            else sb.Append(c);
        }
    }

    private static void AppendEscapedForInterpolation(StringBuilder sb, string text)
        => AppendEscapedForInterpolation(sb, text, 0, text.Length);

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
        // Fast path: compile-time constant
        if (expr is ConstantExpression ce)
            return ce.Value;

        // Fast path: captured variable or field on a closure constant (avoids Lambda.Compile()).
        if (expr is MemberExpression me && me.Expression is ConstantExpression holder)
        {
            if (me.Member is System.Reflection.FieldInfo fi)
                return fi.GetValue(holder.Value);
            if (me.Member is System.Reflection.PropertyInfo pi)
                return pi.GetValue(holder.Value);
        }

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

    /// <summary>Returns true if the format string represents an UPDATE or DELETE statement.</summary>
    private static bool IsUpdateOrDelete(string format)
    {
        var trimmed = format.AsSpan().TrimStart();

        const string update = "UPDATE";
        const string delete = "DELETE";

        if (trimmed.StartsWith(update, StringComparison.OrdinalIgnoreCase))
        {
            var len = update.Length;
            return trimmed.Length == len || char.IsWhiteSpace(trimmed[len]);
        }

        if (trimmed.StartsWith(delete, StringComparison.OrdinalIgnoreCase))
        {
            var len = delete.Length;
            return trimmed.Length == len || char.IsWhiteSpace(trimmed[len]);
        }

        return false;
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
        return attr?.Name ?? type.Name;
    }

    private static string FormatTableRef(string tableName, string alias, SqlDialect dialect, bool suppressAlias = false)
    {
        var quoted = FormatIdentifier(tableName, dialect);
        return (string.IsNullOrEmpty(alias) || suppressAlias) ? quoted : $"{quoted} {alias}";
    }

    private static string FormatColumnRef(string columnName, string alias, SqlDialect dialect, bool suppressAlias = false)
    {
        var quoted = FormatIdentifier(columnName, dialect);
        return (string.IsNullOrEmpty(alias) || suppressAlias) ? quoted : $"{alias}.{quoted}";
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
