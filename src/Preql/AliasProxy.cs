namespace Preql;

/// <summary>
/// Base class for generated alias proxy types.
/// Source generator creates derived classes with typed properties for each entity.
/// This enables compile-time SQL generation without reflection.
/// </summary>
/// <example>
/// Generated code:
/// <code>
/// public class UserAliasProxy : AliasProxy
/// {
///     public UserAliasProxy(SqlDialect dialect) : base("Users", dialect) { }
///     public SqlColumn Id => GetColumn("Id");
///     public SqlColumn Name => GetColumn("Name");
///     public SqlColumn Email => GetColumn("Email");
/// }
/// </code>
/// </example>
public abstract class AliasProxy
{
    protected SqlDialect Dialect { get; }
    protected string TableName { get; }

    protected AliasProxy(string tableName, SqlDialect dialect)
    {
        TableName = tableName;
        Dialect = dialect;
    }

    /// <summary>
    /// Gets a SqlColumn for the specified property name.
    /// Used by generated proxy properties.
    /// </summary>
    protected SqlColumn GetColumn(string columnName)
    {
        return new SqlColumn(columnName, Dialect);
    }

    /// <summary>
    /// Returns a SqlTable representing this table.
    /// Used when the proxy is used as a table reference in queries.
    /// </summary>
    public SqlTable AsTable() => new SqlTable(TableName, Dialect);

    /// <summary>
    /// Implicitly converts to SqlTable for use in queries.
    /// </summary>
    public static implicit operator SqlTable(AliasProxy proxy) => proxy.AsTable();
}
