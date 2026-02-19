using Preql;

namespace Preql.Tests;

public class SqlIdentifierHelperTests
{
    // --- Col ---

    [Fact]
    public void Col_PostgreSql_WithAlias_ReturnsQuotedColumnWithAlias()
    {
        var result = SqlIdentifierHelper.Col(SqlDialect.PostgreSql, "u", "Name");
        Assert.Equal("u.\"Name\"", result);
    }

    [Fact]
    public void Col_SqlServer_WithAlias_ReturnsQuotedColumnWithAlias()
    {
        var result = SqlIdentifierHelper.Col(SqlDialect.SqlServer, "u", "Name");
        Assert.Equal("u.[Name]", result);
    }

    [Fact]
    public void Col_MySql_WithAlias_ReturnsQuotedColumnWithAlias()
    {
        var result = SqlIdentifierHelper.Col(SqlDialect.MySql, "u", "Name");
        Assert.Equal("u.`Name`", result);
    }

    [Fact]
    public void Col_Sqlite_WithAlias_ReturnsQuotedColumnWithAlias()
    {
        var result = SqlIdentifierHelper.Col(SqlDialect.Sqlite, "u", "Name");
        Assert.Equal("u.\"Name\"", result);
    }

    [Fact]
    public void Col_EmptyAlias_ReturnsOnlyQuotedColumn()
    {
        var result = SqlIdentifierHelper.Col(SqlDialect.PostgreSql, "", "Name");
        Assert.Equal("\"Name\"", result);
    }

    [Fact]
    public void Col_UnknownDialect_UsesBracketQuoting()
    {
        var result = SqlIdentifierHelper.Col((SqlDialect)999, "u", "Name");
        Assert.Equal("u.[Name]", result);
    }

    // --- Table ---

    [Fact]
    public void Table_PostgreSql_WithAlias_ReturnsQuotedTableWithAlias()
    {
        var result = SqlIdentifierHelper.Table(SqlDialect.PostgreSql, "Users", "u");
        Assert.Equal("\"Users\" u", result);
    }

    [Fact]
    public void Table_SqlServer_WithAlias_ReturnsQuotedTableWithAlias()
    {
        var result = SqlIdentifierHelper.Table(SqlDialect.SqlServer, "Users", "u");
        Assert.Equal("[Users] u", result);
    }

    [Fact]
    public void Table_MySql_WithAlias_ReturnsQuotedTableWithAlias()
    {
        var result = SqlIdentifierHelper.Table(SqlDialect.MySql, "Users", "u");
        Assert.Equal("`Users` u", result);
    }

    [Fact]
    public void Table_Sqlite_WithAlias_ReturnsQuotedTableWithAlias()
    {
        var result = SqlIdentifierHelper.Table(SqlDialect.Sqlite, "Users", "u");
        Assert.Equal("\"Users\" u", result);
    }

    [Fact]
    public void Table_EmptyAlias_ReturnsOnlyQuotedTable()
    {
        var result = SqlIdentifierHelper.Table(SqlDialect.PostgreSql, "Users", "");
        Assert.Equal("\"Users\"", result);
    }

    [Fact]
    public void Table_UnknownDialect_UsesBracketQuoting()
    {
        var result = SqlIdentifierHelper.Table((SqlDialect)999, "Users", "u");
        Assert.Equal("[Users] u", result);
    }
}
