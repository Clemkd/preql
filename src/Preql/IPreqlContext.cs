using System.Linq.Expressions;

namespace Preql;

/// <summary>
/// Represents a context for Preql SQL generation.
/// Use Query method with lambda expressions for type-safe SQL generation.
/// </summary>
/// <example>
/// <code>
/// var context = new PreqlContext(SqlDialect.PostgreSql);
/// int userId = 123;
/// var query = context.Query&lt;User&gt;((u) => $"SELECT {u.Id}, {u.Name} FROM {u} WHERE {u.Id} = {userId}");
/// // query.Sql: SELECT "Id", "Name" FROM "Users" WHERE "Id" = @p0
/// // query.Parameters: [@p0=123]
/// </code>
/// </example>
public interface IPreqlContext
{
    /// <summary>
    /// Gets the SQL dialect used by this context.
    /// </summary>
    SqlDialect Dialect { get; }

    /// <summary>
    /// Generates a SQL query from a typed interpolated string expression.
    /// The lambda parameter represents the table alias with typed properties.
    /// Values are automatically parameterized for safety.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="queryExpression">A lambda expression containing an interpolated string with the query.</param>
    /// <returns>A <see cref="QueryResult"/> containing the SQL and parameters.</returns>
    /// <example>
    /// <code>
    /// var query = context.Query&lt;User&gt;((u) => $"SELECT {u.Id} FROM {u} WHERE {u.Id} = {userId}");
    /// </code>
    /// </example>
    QueryResult Query<T>(Expression<Func<T, FormattableString>> queryExpression) where T : class;
}
