using System.Linq.Expressions;

namespace Preql;

/// <summary>
/// Default implementation of <see cref="IPreqlContext"/>.
/// Provides type-safe SQL generation by analyzing lambda expression trees.
/// No proxy types are required — simply pass a typed lambda and Preql parses it
/// to distinguish table references, column references and parameter values.
/// </summary>
/// <example>
/// <code>
/// var context = new PreqlContext(SqlDialect.PostgreSql);
/// int userId = 123;
/// var query = context.Query&lt;User&gt;((u) => $"SELECT {u.Id} FROM {u} WHERE {u.Id} = {userId}");
/// // query.Sql  → SELECT u."Id" FROM "Users" u WHERE u."Id" = @p0
/// // query.Parameters → [@p0=123]
/// </code>
/// </example>
public class PreqlContext : IPreqlContext
{
    /// <inheritdoc />
    public SqlDialect Dialect { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PreqlContext"/> class.
    /// </summary>
    /// <param name="dialect">The SQL dialect to use for identifier quoting.</param>
    public PreqlContext(SqlDialect dialect)
    {
        Dialect = dialect;
    }

    /// <inheritdoc />
    public QueryResult Query<T>(Expression<Func<T, FormattableString>> queryExpression) where T : class
        => QueryExpressionAnalyzer.Analyze(queryExpression, Dialect);

    /// <inheritdoc />
    public QueryResult Query<T1, T2>(Expression<Func<T1, T2, FormattableString>> queryExpression)
        where T1 : class where T2 : class
        => QueryExpressionAnalyzer.Analyze(queryExpression, Dialect);

    /// <inheritdoc />
    public QueryResult Query<T1, T2, T3>(Expression<Func<T1, T2, T3, FormattableString>> queryExpression)
        where T1 : class where T2 : class where T3 : class
        => QueryExpressionAnalyzer.Analyze(queryExpression, Dialect);

    /// <inheritdoc />
    public QueryResult Query<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, FormattableString>> queryExpression)
        where T1 : class where T2 : class where T3 : class where T4 : class
        => QueryExpressionAnalyzer.Analyze(queryExpression, Dialect);

    /// <inheritdoc />
    public QueryResult Query<T1, T2, T3, T4, T5>(Expression<Func<T1, T2, T3, T4, T5, FormattableString>> queryExpression)
        where T1 : class where T2 : class where T3 : class where T4 : class where T5 : class
        => QueryExpressionAnalyzer.Analyze(queryExpression, Dialect);
}
