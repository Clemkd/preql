using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Preql;

/// <summary>
/// Extension methods for Preql that generate FormattableString at build time.
/// These methods are intercepted by the source generator to produce SQL without runtime reflection.
/// </summary>
public static class PreqlExtensions
{
    /// <summary>
    /// Converts a typed query expression to a FormattableString containing SQL.
    /// This method is intercepted at build time by the Preql source generator.
    /// The result is compatible with EF Core's FromInterpolatedSql.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="context">The Preql context.</param>
    /// <param name="queryExpression">A lambda expression containing an interpolated string with the query.</param>
    /// <returns>A FormattableString containing the SQL query with parameters.</returns>
    /// <example>
    /// <code>
    /// var sql = db.ToSql&lt;User&gt;((u) => $"SELECT {u.Id}, {u.Name} FROM {u} WHERE {u.Id} = {id}");
    /// // Can be used with EF Core:
    /// // context.Users.FromInterpolatedSql(sql);
    /// </code>
    /// </example>
    public static FormattableString ToSql<T>(this IPreqlContext context, Expression<Func<T, FormattableString>> queryExpression)
    {
        // This method should be intercepted by the source generator at build time.
        // If we reach here at runtime, it means the source generator didn't run.
        // Fall back to runtime analysis for development/debugging purposes.
        
        var result = ExpressionAnalyzer.Analyze<T>(queryExpression, context.Dialect);
        
        // Convert QueryResult to FormattableString
        return ToFormattableString(result);
    }
    
    private static FormattableString ToFormattableString(QueryResult result)
    {
        if (result.Parameters == null)
        {
            return FormattableStringFactory.Create(result.Sql);
        }
        
        // Extract parameter values in order
        var paramDict = result.Parameters as Dictionary<string, object?>;
        if (paramDict == null)
        {
            return FormattableStringFactory.Create(result.Sql);
        }
        
        // Build the format string and arguments array
        var format = result.Sql;
        var args = new List<object?>();
        
        // Replace @p0, @p1, etc. with {0}, {1}, etc.
        foreach (var kvp in paramDict.OrderBy(k => k.Key))
        {
            var paramName = $"@{kvp.Key}";
            var placeholder = $"{{{args.Count}}}";
            format = format.Replace(paramName, placeholder);
            args.Add(kvp.Value);
        }
        
        return FormattableStringFactory.Create(format, args.ToArray());
    }
}
