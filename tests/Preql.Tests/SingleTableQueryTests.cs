using Preql;

namespace Preql.Tests;

public class SingleTableQueryTests
{
    private class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private class Status
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    // --- PostgreSQL ---

    [Fact]
    public void Query_SingleType_PostgreSql_SelectColumns()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u}");

        Assert.Equal("SELECT u.\"Id\", u.\"Name\" FROM \"User\" u", query.Format);
        Assert.Empty(query.GetArguments());
    }

    [Fact]
    public void Query_SingleType_PostgreSql_WithParameter()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);
        int userId = 42;

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u} WHERE {u.Id} = {userId}");

        Assert.Equal("SELECT u.\"Id\", u.\"Name\" FROM \"User\" u WHERE u.\"Id\" = {0}", query.Format);
        var args = query.GetArguments();
        Assert.Single(args);
        Assert.Equal(42, args[0]);
    }

    [Fact]
    public void Query_SingleType_PostgreSql_WithMultipleParameters()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);
        string name = "Alice";
        string email = "alice@example.com";

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id} FROM {u} WHERE {u.Name} = {name} AND {u.Email} = {email}");

        Assert.Equal(
            "SELECT u.\"Id\" FROM \"User\" u WHERE u.\"Name\" = {0} AND u.\"Email\" = {1}",
            query.Format);
        var args = query.GetArguments();
        Assert.Equal(2, args.Length);
        Assert.Equal("Alice", args[0]);
        Assert.Equal("alice@example.com", args[1]);
    }

    // --- SQL Server ---

    [Fact]
    public void Query_SingleType_SqlServer_SelectColumns()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u}");

        Assert.Equal("SELECT u.[Id], u.[Name] FROM [User] u", query.Format);
        Assert.Empty(query.GetArguments());
    }

    [Fact]
    public void Query_SingleType_SqlServer_WithParameter()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);
        int userId = 7;

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Name} FROM {u} WHERE {u.Id} = {userId}");

        Assert.Equal("SELECT u.[Name] FROM [User] u WHERE u.[Id] = {0}", query.Format);
        var args = query.GetArguments();
        Assert.Single(args);
        Assert.Equal(7, args[0]);
    }

    // --- MySQL ---

    [Fact]
    public void Query_SingleType_MySql_SelectColumns()
    {
        var preql = new PreqlContext(SqlDialect.MySql);

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u}");

        Assert.Equal("SELECT u.`Id`, u.`Name` FROM `User` u", query.Format);
        Assert.Empty(query.GetArguments());
    }

    [Fact]
    public void Query_SingleType_MySql_WithParameter()
    {
        var preql = new PreqlContext(SqlDialect.MySql);
        string name = "Bob";

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id} FROM {u} WHERE {u.Name} = {name}");

        Assert.Equal("SELECT u.`Id` FROM `User` u WHERE u.`Name` = {0}", query.Format);
        var args = query.GetArguments();
        Assert.Single(args);
        Assert.Equal("Bob", args[0]);
    }

    // --- SQLite ---

    [Fact]
    public void Query_SingleType_Sqlite_SelectColumns()
    {
        var preql = new PreqlContext(SqlDialect.Sqlite);

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u}");

        Assert.Equal("SELECT u.\"Id\", u.\"Name\" FROM \"User\" u", query.Format);
        Assert.Empty(query.GetArguments());
    }

    [Fact]
    public void Query_SingleType_Sqlite_WithParameter()
    {
        var preql = new PreqlContext(SqlDialect.Sqlite);
        int userId = 99;

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Name} FROM {u} WHERE {u.Id} = {userId}");

        Assert.Equal("SELECT u.\"Name\" FROM \"User\" u WHERE u.\"Id\" = {0}", query.Format);
        var args = query.GetArguments();
        Assert.Single(args);
        Assert.Equal(99, args[0]);
    }

    [Fact]
    public void Query_SingleType_Sqlite_Update_NoAlias()
    {
        var preql = new PreqlContext(SqlDialect.Sqlite);
        string newName = "Alice";
        int userId = 42;

        var query = preql.Query<User>((u) =>
            $"UPDATE {u} SET {u.Name} = {newName} WHERE {u.Id} = {userId}");

        // SQLite does not support table aliases in UPDATE; alias must be suppressed
        Assert.Equal("UPDATE \"User\" SET \"Name\" = {0} WHERE \"Id\" = {1}", query.Format);
        var args = query.GetArguments();
        Assert.Equal(2, args.Length);
        Assert.Equal("Alice", args[0]);
        Assert.Equal(42, args[1]);
    }

    [Fact]
    public void Query_SingleType_Sqlite_Delete_NoAlias()
    {
        var preql = new PreqlContext(SqlDialect.Sqlite);
        int userId = 7;

        var query = preql.Query<User>((u) =>
            $"DELETE FROM {u} WHERE {u.Id} = {userId}");

        // SQLite does not support table aliases in DELETE; alias must be suppressed
        Assert.Equal("DELETE FROM \"User\" WHERE \"Id\" = {0}", query.Format);
        var args = query.GetArguments();
        Assert.Single(args);
        Assert.Equal(7, args[0]);
    }

    // --- Table name: entity name used as-is ---

    [Fact]
    public void Query_SingleType_TableNameUsedAsIs()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<Status>((s) =>
            $"SELECT {s.Id} FROM {s}");

        // "Status" is used as-is, without any pluralization
        Assert.Equal("SELECT s.\"Id\" FROM \"Status\" s", query.Format);
    }

    // --- Dialect property ---

    [Fact]
    public void PreqlContext_Dialect_ReturnsConfiguredDialect()
    {
        var preql = new PreqlContext(SqlDialect.MySql);
        Assert.Equal(SqlDialect.MySql, preql.Dialect);
    }

    // --- FormattableString format and arguments ---

    [Fact]
    public void Query_SingleType_PostgreSql_NoParameters_FormatAndArguments()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u}");

        Assert.Equal("SELECT u.\"Id\", u.\"Name\" FROM \"User\" u", query.Format);
        Assert.Empty(query.GetArguments());
    }

    [Fact]
    public void Query_SingleType_PostgreSql_WithParameter_FormatAndArguments()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);
        int userId = 42;

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u} WHERE {u.Id} = {userId}");

        Assert.Equal("SELECT u.\"Id\", u.\"Name\" FROM \"User\" u WHERE u.\"Id\" = {0}", query.Format);
        var args = query.GetArguments();
        Assert.Single(args);
        Assert.Equal(42, args[0]);
    }

    [Fact]
    public void Query_SingleType_PostgreSql_WithMultipleParameters_FormatAndArguments()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);
        string name = "Alice";
        string email = "alice@example.com";

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id} FROM {u} WHERE {u.Name} = {name} AND {u.Email} = {email}");

        Assert.Equal("SELECT u.\"Id\" FROM \"User\" u WHERE u.\"Name\" = {0} AND u.\"Email\" = {1}", query.Format);
        var args = query.GetArguments();
        Assert.Equal(2, args.Length);
        Assert.Equal("Alice", args[0]);
        Assert.Equal("alice@example.com", args[1]);
    }

    [Fact]
    public void Query_SingleType_SqlServer_WithParameter_FormatAndArguments()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);
        int userId = 7;

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Name} FROM {u} WHERE {u.Id} = {userId}");

        Assert.Equal("SELECT u.[Name] FROM [User] u WHERE u.[Id] = {0}", query.Format);
        var args = query.GetArguments();
        Assert.Single(args);
        Assert.Equal(7, args[0]);
    }
}
