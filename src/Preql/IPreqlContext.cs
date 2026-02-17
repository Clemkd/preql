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

    /// <summary>
    /// Generates a SQL query from a typed interpolated string expression with two table aliases.
    /// </summary>
    /// <typeparam name="T1">The first entity type.</typeparam>
    /// <typeparam name="T2">The second entity type.</typeparam>
    /// <param name="queryExpression">A lambda expression containing an interpolated string with the query.</param>
    /// <returns>A <see cref="QueryResult"/> containing the SQL and parameters.</returns>
    /// <example>
    /// <code>
    /// var query = context.Query&lt;User, Post&gt;((u, p) => $"SELECT {u.Id}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");
    /// </code>
    /// </example>
    QueryResult Query<T1, T2>(Expression<Func<T1, T2, FormattableString>> queryExpression) 
        where T1 : class where T2 : class;

    /// <summary>
    /// Generates a SQL query from a typed interpolated string expression with three table aliases.
    /// </summary>
    QueryResult Query<T1, T2, T3>(Expression<Func<T1, T2, T3, FormattableString>> queryExpression) 
        where T1 : class where T2 : class where T3 : class;

    /// <summary>
    /// Generates a SQL query from a typed interpolated string expression with four table aliases.
    /// </summary>
    QueryResult Query<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, FormattableString>> queryExpression) 
        where T1 : class where T2 : class where T3 : class where T4 : class;

    /// <summary>
    /// Generates a SQL query from a typed interpolated string expression with five table aliases.
    /// </summary>
    QueryResult Query<T1, T2, T3, T4, T5>(Expression<Func<T1, T2, T3, T4, T5, FormattableString>> queryExpression) 
        where T1 : class where T2 : class where T3 : class where T4 : class where T5 : class;
}
