using Preql;

namespace Preql.Tests;

public class MultiTableQueryTests
{
    private class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class Post
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private class Comment
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    private class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class Category
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    private static object?[] GetArguments(FormattableString query) => query.GetArguments();

    // --- Two tables ---

    [Fact]
    public void Query_TwoTypes_PostgreSql_Join_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<User, Post>((u, p) =>
            $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");

        Assert.Equal(
            "SELECT u.\"Name\", p.\"Message\" FROM \"User\" u JOIN \"Post\" p ON u.\"Id\" = p.\"UserId\"",
            query.Format);
        Assert.Empty(GetArguments(query));
    }

    [Fact]
    public void Query_TwoTypes_SqlServer_Join_WithParameter()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);
        string searchTerm = "%Alice%";

        var query = preql.Query<User, Post>((u, p) =>
            $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId} WHERE {u.Name} LIKE {searchTerm}");

        Assert.Equal(
            "SELECT u.[Name], p.[Message] FROM [User] u JOIN [Post] p ON u.[Id] = p.[UserId] WHERE u.[Name] LIKE {0}",
            query.Format);
        var args = GetArguments(query);
        Assert.Single(args);
        Assert.Equal("%Alice%", args[0]);
    }

    [Fact]
    public void Query_TwoTypes_MySql_Join_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.MySql);

        var query = preql.Query<User, Post>((u, p) =>
            $"SELECT {u.Id}, {p.Id} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");

        Assert.Equal(
            "SELECT u.`Id`, p.`Id` FROM `User` u JOIN `Post` p ON u.`Id` = p.`UserId`",
            query.Format);
    }

    [Fact]
    public void Query_TwoTypes_Sqlite_Join_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.Sqlite);

        var query = preql.Query<User, Post>((u, p) =>
            $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");

        Assert.Equal(
            "SELECT u.\"Name\", p.\"Message\" FROM \"User\" u JOIN \"Post\" p ON u.\"Id\" = p.\"UserId\"",
            query.Format);
    }

    // --- Three tables ---

    [Fact]
    public void Query_ThreeTypes_PostgreSql_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<User, Post, Comment>((u, p, c) =>
            $"SELECT {u.Name}, {p.Message}, {c.Content} FROM {u} JOIN {p} ON {u.Id} = {p.UserId} JOIN {c} ON {p.Id} = {c.PostId}");

        Assert.Equal(
            "SELECT u.\"Name\", p.\"Message\", c.\"Content\" FROM \"User\" u JOIN \"Post\" p ON u.\"Id\" = p.\"UserId\" JOIN \"Comment\" c ON p.\"Id\" = c.\"PostId\"",
            query.Format);
        Assert.Empty(GetArguments(query));
    }

    [Fact]
    public void Query_ThreeTypes_SqlServer_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);

        var query = preql.Query<User, Post, Comment>((u, p, c) =>
            $"SELECT {u.Id}, {p.Id}, {c.Id} FROM {u} JOIN {p} ON {u.Id} = {p.UserId} JOIN {c} ON {p.Id} = {c.PostId}");

        Assert.Equal(
            "SELECT u.[Id], p.[Id], c.[Id] FROM [User] u JOIN [Post] p ON u.[Id] = p.[UserId] JOIN [Comment] c ON p.[Id] = c.[PostId]",
            query.Format);
    }

    // --- Four tables ---

    [Fact]
    public void Query_FourTypes_PostgreSql_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<User, Post, Comment, Tag>((u, p, c, t) =>
            $"SELECT {u.Name}, {p.Message}, {c.Content}, {t.Name} FROM {u} JOIN {p} ON {u.Id} = {p.UserId} JOIN {c} ON {p.Id} = {c.PostId} JOIN {t} ON {t.Id} = {p.Id}");

        Assert.Contains("u.\"Name\"", query.Format);
        Assert.Contains("p.\"Message\"", query.Format);
        Assert.Contains("c.\"Content\"", query.Format);
        Assert.Contains("t.\"Name\"", query.Format);
        Assert.Contains("\"User\" u", query.Format);
        Assert.Contains("\"Post\" p", query.Format);
        Assert.Contains("\"Comment\" c", query.Format);
        Assert.Contains("\"Tag\" t", query.Format);
    }

    [Fact]
    public void Query_FourTypes_SqlServer_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);

        var query = preql.Query<User, Post, Comment, Tag>((u, p, c, t) =>
            $"SELECT {u.Id}, {p.Id}, {c.Id}, {t.Id} FROM {u} JOIN {p} ON {u.Id} = {p.UserId} JOIN {c} ON {p.Id} = {c.PostId} JOIN {t} ON {t.Id} = {p.Id}");

        Assert.Contains("[User] u", query.Format);
        Assert.Contains("[Post] p", query.Format);
        Assert.Contains("[Comment] c", query.Format);
        Assert.Contains("[Tag] t", query.Format);
    }

    // --- Five tables ---

    [Fact]
    public void Query_FiveTypes_PostgreSql_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<User, Post, Comment, Tag, Category>((u, p, c, t, cat) =>
            $"SELECT {u.Name}, {p.Message}, {c.Content}, {t.Name}, {cat.Title} FROM {u} JOIN {p} ON {u.Id} = {p.UserId} JOIN {c} ON {p.Id} = {c.PostId} JOIN {t} ON {t.Id} = {p.Id} JOIN {cat} ON {cat.Id} = {p.Id}");

        Assert.Contains("u.\"Name\"", query.Format);
        Assert.Contains("p.\"Message\"", query.Format);
        Assert.Contains("c.\"Content\"", query.Format);
        Assert.Contains("t.\"Name\"", query.Format);
        Assert.Contains("cat.\"Title\"", query.Format);
        Assert.Contains("\"User\" u", query.Format);
        Assert.Contains("\"Post\" p", query.Format);
        Assert.Contains("\"Comment\" c", query.Format);
        Assert.Contains("\"Tag\" t", query.Format);
        Assert.Contains("\"Category\" cat", query.Format);
    }

    [Fact]
    public void Query_FiveTypes_SqlServer_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);

        var query = preql.Query<User, Post, Comment, Tag, Category>((u, p, c, t, cat) =>
            $"SELECT {u.Id}, {p.Id}, {c.Id}, {t.Id}, {cat.Id} FROM {u} JOIN {p} ON {u.Id} = {p.UserId} JOIN {c} ON {p.Id} = {c.PostId} JOIN {t} ON {t.Id} = {p.Id} JOIN {cat} ON {cat.Id} = {p.Id}");

        Assert.Contains("[User] u", query.Format);
        Assert.Contains("[Post] p", query.Format);
        Assert.Contains("[Comment] c", query.Format);
        Assert.Contains("[Tag] t", query.Format);
        Assert.Contains("[Category] cat", query.Format);
    }

    [Fact]
    public void Query_FiveTypes_WithParameter_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);
        int minId = 10;

        var query = preql.Query<User, Post, Comment, Tag, Category>((u, p, c, t, cat) =>
            $"SELECT {u.Name} FROM {u} JOIN {p} ON {u.Id} = {p.UserId} JOIN {c} ON {p.Id} = {c.PostId} JOIN {t} ON {t.Id} = {p.Id} JOIN {cat} ON {cat.Id} = {p.Id} WHERE {u.Id} > {minId}");

        Assert.Contains("{0}", query.Format);
        var args = GetArguments(query);
        Assert.Single(args);
        Assert.Equal(10, args[0]);
    }
}
