// This file demonstrates what a source generator would automatically create.
// In production, the source generator would create this file at build time.

using Preql;
using System.Reflection;

namespace Preql.Sample.Generated;

/// <summary>
/// Generated proxy for User that can be used in Query lambda expressions.
/// Each property returns a SqlColumn instead of the actual property value.
/// This is what enables: context.Query&lt;User&gt;((u) => $"SELECT {u.Id}...")
/// </summary>
public class UserProxy
{
    private readonly SqlDialect _dialect;

    public UserProxy(SqlDialect dialect)
    {
        _dialect = dialect;
    }

    // Properties return SqlColumn for use in SQL generation
    public SqlColumn Id => CreateColumn("Id");
    public SqlColumn Name => CreateColumn("Name");
    public SqlColumn Email => CreateColumn("Email");
    public SqlColumn Age => CreateColumn("Age");

    // Allow using the proxy as a table in FROM clauses
    public static implicit operator SqlTable(UserProxy proxy)
    {
        return CreateTable("Users", proxy._dialect);
    }

    private SqlColumn CreateColumn(string name)
    {
        // Use reflection to call internal constructor
        var ctor = typeof(SqlColumn).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(string), typeof(SqlDialect) },
            null);
        return (SqlColumn)ctor!.Invoke(new object[] { name, _dialect });
    }

    private static SqlTable CreateTable(string name, SqlDialect dialect)
    {
        var ctor = typeof(SqlTable).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(string), typeof(SqlDialect) },
            null);
        return (SqlTable)ctor!.Invoke(new object[] { name, dialect });
    }
}
