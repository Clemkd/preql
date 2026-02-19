using System.Linq.Expressions;
using Preql;

namespace Preql.Tests;

public class ErrorHandlingTests
{
    private class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void Query_NonInterpolatedStringLambda_ThrowsInvalidOperationException()
    {
        var preql = new PreqlContext(SqlDialect.PostgreSql);

        // Build a lambda whose body is a ConstantExpression (null FormattableString),
        // which is NOT the FormattableStringFactory.Create call emitted by the compiler
        // for interpolated strings. The analyzer should reject it.
        var param = Expression.Parameter(typeof(User), "u");
        var body = Expression.Constant(null, typeof(FormattableString));
        var lambda = Expression.Lambda<Func<User, FormattableString>>(body, param);

        Assert.Throws<InvalidOperationException>(() => preql.Query(lambda));
    }
}
