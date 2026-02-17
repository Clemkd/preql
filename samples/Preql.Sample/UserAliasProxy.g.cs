// This is an example of what the source generator would create.
// In production, this would be auto-generated for each entity type used with Query<T>.

namespace Preql.Sample.Generated;

using Preql;

/// <summary>
/// Generated alias proxy for User entity.
/// Provides typed property access for SQL generation without reflection.
/// </summary>
public class UserAliasProxy : AliasProxy
{
    public UserAliasProxy(SqlDialect dialect) 
        : base("Users", dialect)
    {
    }

    /// <summary>
    /// Gets the Id column.
    /// </summary>
    public SqlColumn Id => GetColumn("Id");

    /// <summary>
    /// Gets the Name column.
    /// </summary>
    public SqlColumn Name => GetColumn("Name");

    /// <summary>
    /// Gets the Email column.
    /// </summary>
    public SqlColumn Email => GetColumn("Email");

    /// <summary>
    /// Gets the Age column.
    /// </summary>
    public SqlColumn Age => GetColumn("Age");
}
