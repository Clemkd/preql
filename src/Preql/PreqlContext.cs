namespace Preql;

/// <summary>
/// Default implementation of <see cref="IPreqlContext"/>.
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
}
