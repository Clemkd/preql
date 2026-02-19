using System.Linq.Expressions;

namespace Preql;

/// <summary>
/// Lightweight helpers used by source-generated interceptors.
/// <list type="bullet">
///   <item><see cref="Col"/> / <see cref="Table"/> apply dialect-specific quoting to
///   pre-known identifiers (SQL structure is determined at compile-time; only quoting
///   is applied at runtime).</item>
///   <item><see cref="EvalParamArg"/> extracts a single runtime parameter value from
///   the interpolated-string expression tree without full analysis.</item>
/// </list>
/// </summary>
public static class SqlIdentifierHelper
{
    /// <summary>
    /// Returns a dialect-quoted, alias-prefixed column reference, e.g. <c>u."Name"</c>.
    /// </summary>
    public static string Col(SqlDialect dialect, string tableAlias, string columnName)
    {
        var quoted = Quote(dialect, columnName);
        return string.IsNullOrEmpty(tableAlias) ? quoted : $"{tableAlias}.{quoted}";
    }

    /// <summary>
    /// Returns a dialect-quoted table reference with an alias, e.g. <c>"Users" u</c>.
    /// </summary>
    public static string Table(SqlDialect dialect, string tableName, string alias)
    {
        var quoted = Quote(dialect, tableName);
        return string.IsNullOrEmpty(alias) ? quoted : $"{quoted} {alias}";
    }

    /// <summary>
    /// Compiles and evaluates the argument at <paramref name="index"/> inside the
    /// <c>FormattableStringFactory.Create</c> call captured in <paramref name="callExpr"/>.
    /// This handles both the <c>new object[] { … }</c> and the individual-argument forms
    /// that the C# compiler may emit for interpolated strings.
    /// </summary>
    /// <remarks>
    /// Each call compiles a small expression-tree lambda to extract the value from the
    /// current closure. For high-frequency query call sites, consider caching the compiled
    /// delegate (e.g. via a static field in the generated interceptor class).  The source
    /// generator will address this in a future enhancement.
    /// </remarks>
    public static object? EvalParamArg(MethodCallExpression callExpr, int index)
    {
        Expression argExpr = callExpr.Arguments.Count == 2 && callExpr.Arguments[1] is NewArrayExpression arr
            ? arr.Expressions[index]
            : callExpr.Arguments[1 + index];

        // Strip any boxing Convert nodes
        while (argExpr is UnaryExpression { NodeType: ExpressionType.Convert } unary)
            argExpr = unary.Operand;

        return Expression.Lambda<Func<object?>>(
            Expression.Convert(argExpr, typeof(object))).Compile()();
    }

    private static string Quote(SqlDialect dialect, string identifier) =>
        dialect switch
        {
            SqlDialect.PostgreSql => $"\"{identifier}\"",
            SqlDialect.SqlServer  => $"[{identifier}]",
            SqlDialect.MySql      => $"`{identifier}`",
            SqlDialect.Sqlite     => $"\"{identifier}\"",
            _                     => $"[{identifier}]"
        };
}
