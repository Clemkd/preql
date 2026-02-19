using Microsoft.Extensions.DependencyInjection;
using Preql;

namespace Preql.Tests;

public class ServiceCollectionExtensionsTests
{
    // --- AddPreql ---

    [Fact]
    public void AddPreql_RegistersIPreqlContext_WithCorrectDialect()
    {
        var services = new ServiceCollection();
        services.AddPreql(SqlDialect.PostgreSql);

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<IPreqlContext>();

        Assert.NotNull(context);
        Assert.Equal(SqlDialect.PostgreSql, context.Dialect);
    }

    [Fact]
    public void AddPreql_SqlServer_RegistersIPreqlContextWithSqlServerDialect()
    {
        var services = new ServiceCollection();
        services.AddPreql(SqlDialect.SqlServer);

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<IPreqlContext>();

        Assert.Equal(SqlDialect.SqlServer, context.Dialect);
    }

    [Fact]
    public void AddPreql_MySql_RegistersIPreqlContextWithMySqlDialect()
    {
        var services = new ServiceCollection();
        services.AddPreql(SqlDialect.MySql);

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<IPreqlContext>();

        Assert.Equal(SqlDialect.MySql, context.Dialect);
    }

    [Fact]
    public void AddPreql_Sqlite_RegistersIPreqlContextWithSqliteDialect()
    {
        var services = new ServiceCollection();
        services.AddPreql(SqlDialect.Sqlite);

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<IPreqlContext>();

        Assert.Equal(SqlDialect.Sqlite, context.Dialect);
    }

    [Fact]
    public void AddPreql_ReturnsSameServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddPreql(SqlDialect.PostgreSql);

        Assert.Same(services, result);
    }

    [Fact]
    public void AddPreql_IsSingleton_ReturnsSameInstance()
    {
        var services = new ServiceCollection();
        services.AddPreql(SqlDialect.PostgreSql);

        var provider = services.BuildServiceProvider();
        var context1 = provider.GetRequiredService<IPreqlContext>();
        var context2 = provider.GetRequiredService<IPreqlContext>();

        Assert.Same(context1, context2);
    }

    // --- AddPreql<TContext> ---

    [Fact]
    public void AddPreql_Generic_RegistersTContext_WithCorrectDialect()
    {
        var services = new ServiceCollection();
        services.AddPreql<PreqlContext>(SqlDialect.PostgreSql);

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<PreqlContext>();

        Assert.NotNull(context);
        Assert.Equal(SqlDialect.PostgreSql, context.Dialect);
    }

    [Fact]
    public void AddPreql_Generic_ReturnsSameServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddPreql<PreqlContext>(SqlDialect.SqlServer);

        Assert.Same(services, result);
    }

    [Fact]
    public void AddPreql_Generic_ThrowsWhenTypeHasNoMatchingConstructor()
    {
        var services = new ServiceCollection();
        services.AddPreql<InvalidContext>(SqlDialect.PostgreSql);
        var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<InvalidContext>());
    }

    // A context type that does NOT have a constructor accepting SqlDialect
    private class InvalidContext : IPreqlContext
    {
        public SqlDialect Dialect => SqlDialect.PostgreSql;

        public QueryResult Query<T>(
            System.Linq.Expressions.Expression<Func<T, FormattableString>> queryExpression) where T : class
            => new QueryResult();

        public QueryResult Query<T1, T2>(
            System.Linq.Expressions.Expression<Func<T1, T2, FormattableString>> queryExpression)
            where T1 : class where T2 : class
            => new QueryResult();

        public QueryResult Query<T1, T2, T3>(
            System.Linq.Expressions.Expression<Func<T1, T2, T3, FormattableString>> queryExpression)
            where T1 : class where T2 : class where T3 : class
            => new QueryResult();

        public QueryResult Query<T1, T2, T3, T4>(
            System.Linq.Expressions.Expression<Func<T1, T2, T3, T4, FormattableString>> queryExpression)
            where T1 : class where T2 : class where T3 : class where T4 : class
            => new QueryResult();

        public QueryResult Query<T1, T2, T3, T4, T5>(
            System.Linq.Expressions.Expression<Func<T1, T2, T3, T4, T5, FormattableString>> queryExpression)
            where T1 : class where T2 : class where T3 : class where T4 : class where T5 : class
            => new QueryResult();
    }
}
