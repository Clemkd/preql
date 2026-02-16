using System.Linq.Expressions;

namespace Preql;

/// <summary>
/// Default implementation of <see cref="IPreqlContext"/>.
/// </summary>
public class PreqlContext : IPreqlContext
{
    /// <inheritdoc />
    public SqlDialect Dialect { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PreqlContext"/> class.
    /// </summary>
    /// <param name="dialect">The SQL dialect to use.</param>
    public PreqlContext(SqlDialect dialect)
    {
        Dialect = dialect;
    }

    /// <inheritdoc />
    [Obsolete("This method uses runtime expression analysis. Use PreqlSqlHandler with InterpolatedStringHandler instead for zero-reflection SQL generation. Example: PreqlSqlHandler h = $\"SELECT {u[\"Id\"]} FROM {u}\"; See docs/InterpolatedStringHandler.md")]
    public QueryResult Query<T>(Expression<Func<T, FormattableString>> queryExpression)
    {
        // OBSOLETE: This method uses runtime expression tree analysis.
        // For zero-reflection SQL generation, use PreqlSqlHandler instead:
        //
        //   var u = context.Alias<User>();
        //   PreqlSqlHandler h = $"SELECT {u["Id"]} FROM {u} WHERE {u["Id"]} = {id.AsValue()}";
        //   var (sql, params) = h.Build();
        //
        // The InterpolatedStringHandler approach eliminates all runtime overhead.
        
        return ExpressionAnalyzer.Analyze<T>(queryExpression, Dialect);
    }
}
