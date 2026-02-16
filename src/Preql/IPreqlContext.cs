namespace Preql;

/// <summary>
/// Represents a context for Preql SQL generation.
/// Use with PreqlSqlHandler for zero-reflection SQL generation.
/// </summary>
/// <example>
/// <code>
/// var context = new PreqlContext(SqlDialect.PostgreSql);
/// var u = context.Alias&lt;User&gt;();
/// PreqlSqlHandler h = $"SELECT {u["Id"]}, {u["Name"]} FROM {u} WHERE {u["Id"]} = {id.AsValue()}";
/// var (sql, parameters) = h.Build();
/// </code>
/// </example>
public interface IPreqlContext
{
    /// <summary>
    /// Gets the SQL dialect used by this context.
    /// </summary>
    SqlDialect Dialect { get; }
}
