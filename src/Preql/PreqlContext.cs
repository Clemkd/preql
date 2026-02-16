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
    public QueryResult Query<T>(Expression<Func<T, FormattableString>> queryExpression)
    {
        // Note: In a production implementation with C# 12 Interceptors enabled,
        // this method would be intercepted at compile-time by the source generator.
        // This runtime implementation analyzes the expression tree to demonstrate the concept.
        
        return ExpressionAnalyzer.Analyze<T>(queryExpression, Dialect);
    }
}
