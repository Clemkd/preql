using System.Linq.Expressions;

namespace Preql;

/// <summary>
/// Represents a context for generating SQL queries from typed interpolated strings.
/// </summary>
public interface IPreqlContext
{
    /// <summary>
    /// Gets the SQL dialect used by this context.
    /// </summary>
    SqlDialect Dialect { get; }

    /// <summary>
    /// Generates a SQL query from a typed interpolated string expression.
    /// This method is intercepted at compile-time by the source generator.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="queryExpression">A lambda expression containing an interpolated string with the query.</param>
    /// <returns>A <see cref="QueryResult"/> containing the SQL and parameters.</returns>
    QueryResult Query<T>(Expression<Func<T, FormattableString>> queryExpression);
}
