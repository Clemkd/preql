using Preql;

namespace Preql.Tests;

public class AttributeTests
{
    [Table("tbl_users")]
    private class UserWithTableAttribute
    {
        public int Id { get; set; }

        [Column("user_name")]
        public string Name { get; set; } = string.Empty;

        [Column("user_email")]
        public string Email { get; set; } = string.Empty;
    }

    private class UserWithColumnAttribute
    {
        public int Id { get; set; }

        [Column("first_name")]
        public string FirstName { get; set; } = string.Empty;
    }

    private static IReadOnlyList<object?> GetParameters(QueryResult query) =>
        query.Parameters as IReadOnlyList<object?> ?? [];

    // --- ColumnAttribute ---

    [Fact]
    public void Query_ColumnAttribute_PostgreSql_UsesAttributeName()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<UserWithColumnAttribute>((u) =>
            $"SELECT {u.FirstName} FROM {u}");

        Assert.Equal("SELECT u.\"first_name\" FROM \"UserWithColumnAttributes\" u", query.Sql);
    }

    [Fact]
    public void Query_ColumnAttribute_SqlServer_UsesAttributeName()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);

        var query = preql.Query<UserWithTableAttribute>((u) =>
            $"SELECT {u.Name}, {u.Email} FROM {u}");

        // Column names come from [Column] attributes
        Assert.Contains("u.[user_name]", query.Sql);
        Assert.Contains("u.[user_email]", query.Sql);
    }

    [Fact]
    public void Query_ColumnAttribute_MySql_UsesAttributeName()
    {
        var preql = new PreqlContext(SqlDialect.MySql);

        var query = preql.Query<UserWithColumnAttribute>((u) =>
            $"SELECT {u.FirstName} FROM {u}");

        Assert.Equal("SELECT u.`first_name` FROM `UserWithColumnAttributes` u", query.Sql);
    }

    [Fact]
    public void Query_ColumnAttribute_Sqlite_UsesAttributeName()
    {
        var preql = new PreqlContext(SqlDialect.Sqlite);

        var query = preql.Query<UserWithColumnAttribute>((u) =>
            $"SELECT {u.FirstName} FROM {u}");

        Assert.Equal("SELECT u.\"first_name\" FROM \"UserWithColumnAttributes\" u", query.Sql);
    }

    // --- ColumnAttribute constructor ---

    [Fact]
    public void ColumnAttribute_Constructor_SetsName()
    {
        var attr = new ColumnAttribute("my_column");
        Assert.Equal("my_column", attr.Name);
    }

    [Fact]
    public void ColumnAttribute_Constructor_ThrowsOnNullName()
    {
        Assert.Throws<ArgumentException>(() => new ColumnAttribute(null!));
    }

    [Fact]
    public void ColumnAttribute_Constructor_ThrowsOnEmptyName()
    {
        Assert.Throws<ArgumentException>(() => new ColumnAttribute(""));
    }

    [Fact]
    public void ColumnAttribute_Constructor_ThrowsOnWhitespaceName()
    {
        Assert.Throws<ArgumentException>(() => new ColumnAttribute("   "));
    }

    // --- TableAttribute constructor ---

    [Fact]
    public void TableAttribute_Constructor_SetsName()
    {
        var attr = new TableAttribute("my_table");
        Assert.Equal("my_table", attr.Name);
    }

    // --- TableAttribute applied to class (runtime fallback uses type name, not attribute) ---

    [Fact]
    public void Query_TableAttribute_RuntimeFallback_UsesTypeName()
    {
        // The runtime analyzer does simple pluralization of the C# type name.
        // The [Table] attribute is honored by the source generator (compile-time),
        // not by the runtime fallback. This test documents that behavior.
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<UserWithTableAttribute>((u) =>
            $"SELECT {u.Id} FROM {u}");

        // Runtime uses type name "UserWithTableAttribute" + pluralization
        Assert.Contains("\"UserWithTableAttributes\" u", query.Sql);
    }
}
