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
        // This method should never be called at runtime.
        // It will be intercepted by the source generator at compile-time.
        throw new NotImplementedException(
            "This method should be intercepted by the Preql source generator. " +
            "Ensure the Preql.SourceGenerator is properly referenced in your project.");
    }
}
