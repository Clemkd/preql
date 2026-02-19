using Preql;

namespace Preql.Tests;

public class CrossJoinQueryTests
{
    private class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private static object?[] GetArguments(FormattableString query) => query.GetArguments();

    [Fact]
    public void Query_CrossJoin_SameType_PostgreSql_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        var query = preql.Query<User, User>((u1, u2) =>
            $"select {u1.Name}, {u2.Name} from {u1} cross join {u2}");

        Assert.Equal(
            "select u1.\"Name\", u2.\"Name\" from \"User\" u1 cross join \"User\" u2",
            query.Format);
        Assert.Empty(GetArguments(query));
    }

    [Fact]
    public void Query_CrossJoin_SameType_SqlServer_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);

        var query = preql.Query<User, User>((u1, u2) =>
            $"select {u1.Name}, {u2.Name} from {u1} cross join {u2}");

        Assert.Equal(
            "select u1.[Name], u2.[Name] from [User] u1 cross join [User] u2",
            query.Format);
        Assert.Empty(GetArguments(query));
    }

    [Fact]
    public void Query_CrossJoin_SameType_MySql_GeneratesCorrectSql()
    {
        var preql = new PreqlContext(SqlDialect.MySql);

        var query = preql.Query<User, User>((u1, u2) =>
            $"select {u1.Name}, {u2.Name} from {u1} cross join {u2}");

        Assert.Equal(
            "select u1.`Name`, u2.`Name` from `User` u1 cross join `User` u2",
            query.Format);
        Assert.Empty(GetArguments(query));
    }

    [Fact]
    public void Query_CrossJoin_SameType_UsesLambdaParameterNamesAsAliases()
    {
        var preql = new PreqlContext(SqlDialect.SqlServer);

        var query = preql.Query<User, User>((u1, u2) =>
            $"select {u1.Name}, {u2.Name} from {u1} cross join {u2}");

        Assert.Contains("u1.[Name]", query.Format);
        Assert.Contains("u2.[Name]", query.Format);
        Assert.Contains("[User] u1", query.Format);
        Assert.Contains("[User] u2", query.Format);
    }
}
