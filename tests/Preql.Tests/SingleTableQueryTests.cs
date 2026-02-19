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

    private static IReadOnlyList<object?> GetParameters(QueryResult query) =>
        query.Parameters as IReadOnlyList<object?> ?? [];

    // --- PostgreSQL ---

    [Fact]
    public void Query_SingleType_PostgreSql_SelectColumns()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u}");

        Assert.Equal("SELECT u.\"Id\", u.\"Name\" FROM \"User\" u", query.Sql);
        Assert.Empty(GetParameters(query));
    }

    [Fact]
    public void Query_SingleType_PostgreSql_WithParameter()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);
        int userId = 42;

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u} WHERE {u.Id} = {userId}");

        Assert.Equal("SELECT u.\"Id\", u.\"Name\" FROM \"User\" u WHERE u.\"Id\" = @p0", query.Sql);
        var parameters = GetParameters(query);
        Assert.Single(parameters);
        Assert.Equal(42, parameters[0]);
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
            "SELECT u.\"Id\" FROM \"User\" u WHERE u.\"Name\" = @p0 AND u.\"Email\" = @p1",
            query.Sql);
        var parameters = GetParameters(query);
        Assert.Equal(2, parameters.Count);
        Assert.Equal("Alice", parameters[0]);
        Assert.Equal("alice@example.com", parameters[1]);
    }

    // --- SQL Server ---

    [Fact]
    public void Query_SingleType_SqlServer_SelectColumns()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u}");

        Assert.Equal("SELECT u.[Id], u.[Name] FROM [User] u", query.Sql);
        Assert.Empty(GetParameters(query));
    }

    [Fact]
    public void Query_SingleType_SqlServer_WithParameter()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);
        int userId = 7;

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Name} FROM {u} WHERE {u.Id} = {userId}");

        Assert.Equal("SELECT u.[Name] FROM [User] u WHERE u.[Id] = @p0", query.Sql);
        var parameters = GetParameters(query);
        Assert.Single(parameters);
        Assert.Equal(7, parameters[0]);
    }

    // --- MySQL ---

    [Fact]
    public void Query_SingleType_MySql_SelectColumns()
    {
        var preql = new PreqlContext(SqlDialect.MySql);

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u}");

        Assert.Equal("SELECT u.`Id`, u.`Name` FROM `User` u", query.Sql);
        Assert.Empty(GetParameters(query));
    }

    [Fact]
    public void Query_SingleType_MySql_WithParameter()
    {
        var preql = new PreqlContext(SqlDialect.MySql);
        string name = "Bob";

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id} FROM {u} WHERE {u.Name} = {name}");

        Assert.Equal("SELECT u.`Id` FROM `User` u WHERE u.`Name` = @p0", query.Sql);
        var parameters = GetParameters(query);
        Assert.Single(parameters);
        Assert.Equal("Bob", parameters[0]);
    }

    // --- SQLite ---

    [Fact]
    public void Query_SingleType_Sqlite_SelectColumns()
    {
        var preql = new PreqlContext(SqlDialect.Sqlite);

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u}");

        Assert.Equal("SELECT u.\"Id\", u.\"Name\" FROM \"User\" u", query.Sql);
        Assert.Empty(GetParameters(query));
    }

    [Fact]
    public void Query_SingleType_Sqlite_WithParameter()
    {
        var preql = new PreqlContext(SqlDialect.Sqlite);
        int userId = 99;

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Name} FROM {u} WHERE {u.Id} = {userId}");

        Assert.Equal("SELECT u.\"Name\" FROM \"User\" u WHERE u.\"Id\" = @p0", query.Sql);
        var parameters = GetParameters(query);
        Assert.Single(parameters);
        Assert.Equal(99, parameters[0]);
    }

    // --- Table name: entity name used as-is ---

    [Fact]
    public void Query_SingleType_TableNameUsedAsIs()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<Status>((s) =>
            $"SELECT {s.Id} FROM {s}");

        // "Status" is used as-is, without any pluralization
        Assert.Equal("SELECT s.\"Id\" FROM \"Status\" s", query.Sql);
    }

    // --- Dialect property ---

    [Fact]
    public void PreqlContext_Dialect_ReturnsConfiguredDialect()
    {
        var preql = new PreqlContext(SqlDialect.MySql);
        Assert.Equal(SqlDialect.MySql, preql.Dialect);
    }

    // --- Interpolated property ---

    [Fact]
    public void Query_SingleType_PostgreSql_Interpolated_NoParameters()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u}");

        Assert.NotNull(query.Interpolated);
        Assert.Equal("SELECT u.\"Id\", u.\"Name\" FROM \"User\" u", query.Interpolated!.Format);
        Assert.Empty(query.Interpolated.GetArguments());
    }

    [Fact]
    public void Query_SingleType_PostgreSql_Interpolated_WithParameter()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);
        int userId = 42;

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name} FROM {u} WHERE {u.Id} = {userId}");

        Assert.NotNull(query.Interpolated);
        Assert.Equal("SELECT u.\"Id\", u.\"Name\" FROM \"User\" u WHERE u.\"Id\" = {0}", query.Interpolated!.Format);
        var args = query.Interpolated.GetArguments();
        Assert.Single(args);
        Assert.Equal(42, args[0]);
    }

    [Fact]
    public void Query_SingleType_PostgreSql_Interpolated_WithMultipleParameters()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);
        string name = "Alice";
        string email = "alice@example.com";

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Id} FROM {u} WHERE {u.Name} = {name} AND {u.Email} = {email}");

        Assert.NotNull(query.Interpolated);
        Assert.Equal("SELECT u.\"Id\" FROM \"User\" u WHERE u.\"Name\" = {0} AND u.\"Email\" = {1}", query.Interpolated!.Format);
        var args = query.Interpolated.GetArguments();
        Assert.Equal(2, args.Length);
        Assert.Equal("Alice", args[0]);
        Assert.Equal("alice@example.com", args[1]);
    }

    [Fact]
    public void Query_SingleType_SqlServer_Interpolated_WithParameter()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);
        int userId = 7;

        var query = preql.Query<User>((u) =>
            $"SELECT {u.Name} FROM {u} WHERE {u.Id} = {userId}");

        Assert.NotNull(query.Interpolated);
        Assert.Equal("SELECT u.[Name] FROM [User] u WHERE u.[Id] = {0}", query.Interpolated!.Format);
        var args = query.Interpolated.GetArguments();
        Assert.Single(args);
        Assert.Equal(7, args[0]);
    }

    // --- QueryResult struct ---

    [Fact]
    public void QueryResult_DefaultConstructor_SqlIsNull()
    {
        var result = new QueryResult();
        Assert.Null(result.Sql);
        Assert.Null(result.Parameters);
        Assert.Null(result.Interpolated);
    }

    [Fact]
    public void QueryResult_Constructor_SetsProperties()
    {
        var parameters = new List<object?> { 1, "test" };
        var result = new QueryResult("SELECT 1", parameters);
        Assert.Equal("SELECT 1", result.Sql);
        Assert.Same(parameters, result.Parameters);
    }

    [Fact]
    public void QueryResult_Constructor_ParametersDefaultsToNull()
    {
        var result = new QueryResult("SELECT 1");
        Assert.Equal("SELECT 1", result.Sql);
        Assert.Null(result.Parameters);
    }
}
