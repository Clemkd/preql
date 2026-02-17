// NOTE: This is a manually-created demonstration file.
// In production, the source generator would automatically create this file at build time.

using Preql;

namespace Preql.Sample.Generated;

/// <summary>
/// Generated proxy for User that supports table aliases.
/// Each property returns a SqlColumn with the appropriate table alias.
/// This enables: context.Query&lt;User&gt;((u) => $"SELECT {u.Id}...") with aliases
/// </summary>
public class UserAliasProxy
{
    private readonly SqlDialect _dialect;
    private readonly string _alias;

    public UserAliasProxy(SqlDialect dialect, string alias)
    {
        _dialect = dialect;
        _alias = alias;
    }

    // Properties return SqlColumn with table alias for use in SQL generation
    public SqlColumn Id => new SqlColumn("Id", _dialect, _alias);
    public SqlColumn Name => new SqlColumn("Name", _dialect, _alias);
    public SqlColumn Email => new SqlColumn("Email", _dialect, _alias);
    public SqlColumn Age => new SqlColumn("Age", _dialect, _alias);

    // Allow using the proxy as a table in FROM clauses with alias
    public static implicit operator SqlTable(UserAliasProxy proxy)
    {
        return new SqlTable("Users", proxy._dialect, proxy._alias);
    }

    public SqlTable AsTable()
    {
        return new SqlTable("Users", _dialect, _alias);
    }

    public override string ToString()
    {
        return AsTable().ToString();
    }
}

/// <summary>
/// Generated proxy for Post that supports table aliases.
/// </summary>
public class PostAliasProxy
{
    private readonly SqlDialect _dialect;
    private readonly string _alias;

    public PostAliasProxy(SqlDialect dialect, string alias)
    {
        _dialect = dialect;
        _alias = alias;
    }

    public SqlColumn Id => new SqlColumn("Id", _dialect, _alias);
    public SqlColumn Message => new SqlColumn("Message", _dialect, _alias);
    public SqlColumn UserId => new SqlColumn("UserId", _dialect, _alias);

    public static implicit operator SqlTable(PostAliasProxy proxy)
    {
        return new SqlTable("Posts", proxy._dialect, proxy._alias);
    }

    public SqlTable AsTable()
    {
        return new SqlTable("Posts", _dialect, _alias);
    }

    public override string ToString()
    {
        return AsTable().ToString();
    }
}
