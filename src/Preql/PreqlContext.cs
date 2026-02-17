using System.Linq.Expressions;
using System.Reflection;

namespace Preql;

/// <summary>
/// Default implementation of <see cref="IPreqlContext"/>.
/// Provides type-safe SQL generation via lambda expressions.
/// </summary>
/// <example>
/// <code>
/// var context = new PreqlContext(SqlDialect.PostgreSql);
/// int userId = 123;
/// var query = context.Query&lt;User&gt;((u) => $"SELECT {u.Id} FROM {u} WHERE {u.Id} = {userId}");
/// </code>
/// </example>
public class PreqlContext : IPreqlContext
{
    /// <inheritdoc />
    public SqlDialect Dialect { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PreqlContext"/> class.
    /// </summary>
    /// <param name="dialect">The SQL dialect to use.</param>
    public PreqlContext(SqlDialect dialect)
    {
        Dialect = dialect;
    }

    /// <inheritdoc />
    public QueryResult Query<T>(Expression<Func<T, FormattableString>> queryExpression) where T : class
    {
        // NOTE: This method is designed to be intercepted by a source generator.
        // The source generator would:
        // 1. Generate a UserProxy class with SqlColumn properties
        // 2. Transform: context.Query<User>((u) => $"SELECT {u.Id}...")
        // 3. Into: new UserProxy(dialect); PreqlSqlHandler h = $"SELECT {proxy.Id}..."
        //
        // For now, we create a runtime proxy that provides similar functionality.
        
        var proxy = EntityProxyFactory.CreateProxy<T>(Dialect);
        var formattableString = queryExpression.Compile()(proxy);
        
        // Process the FormattableString using PreqlSqlHandler
        return ProcessFormattableString(formattableString);
    }

    private QueryResult ProcessFormattableString(FormattableString formattableString)
    {
        var handler = new PreqlSqlHandler(formattableString.Format.Length, formattableString.ArgumentCount);
        var format = formattableString.Format;
        var args = formattableString.GetArguments();
        
        int argIndex = 0;
        int lastPos = 0;
        
        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] == '{' && i + 1 < format.Length)
            {
                if (format[i + 1] == '{')
                {
                    // Escaped brace
                    handler.AppendLiteral(format.Substring(lastPos, i - lastPos + 1));
                    i++;
                    lastPos = i + 1;
                    continue;
                }
                
                // Find the end of the placeholder
                int endBrace = format.IndexOf('}', i);
                if (endBrace > i)
                {
                    // Append literal part before the placeholder
                    if (i > lastPos)
                    {
                        handler.AppendLiteral(format.Substring(lastPos, i - lastPos));
                    }
                    
                    // Append the formatted argument
                    if (argIndex < args.Length)
                    {
                        handler.AppendFormatted(args[argIndex]);
                        argIndex++;
                    }
                    
                    i = endBrace;
                    lastPos = i + 1;
                }
            }
        }
        
        // Append any remaining literal
        if (lastPos < format.Length)
        {
            handler.AppendLiteral(format.Substring(lastPos));
        }
        
        var (sql, parameters) = handler.Build();
        return new QueryResult(sql, parameters);
    }
}

/// <summary>
/// Factory for creating entity proxies at runtime.
/// In production, a source generator would eliminate this entirely.
/// </summary>
internal static class EntityProxyFactory
{
    public static T CreateProxy<T>(SqlDialect dialect) where T : class
    {
        // Create a proxy using DispatchProxy that intercepts property getters
        var tableName = GetTableName<T>();
        return EntityProxyWrapper<T>.Create(dialect, tableName);
    }

    private static string GetTableName<T>()
    {
        var typeName = typeof(T).Name;
        return typeName.EndsWith("s", StringComparison.OrdinalIgnoreCase) 
            ? typeName 
            : typeName + "s";
    }
}

/// <summary>
/// Simple wrapper that intercepts property access and returns SqlColumn instances.
/// In production, a source generator would create concrete types with real properties.
/// </summary>
internal class EntityProxyWrapper<T> : DispatchProxy where T : class
{
    private SqlDialect _dialect;
    private string _tableName = string.Empty;
    private Dictionary<string, SqlColumn> _columnCache = new();

    public static T Create(SqlDialect dialect, string tableName)
    {
        var proxy = Create<T, EntityProxyWrapper<T>>();
        var wrapper = (proxy as EntityProxyWrapper<T>)!;
        wrapper._dialect = dialect;
        wrapper._tableName = tableName;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null)
            return null;

        // Intercept property getters
        if (targetMethod.Name.StartsWith("get_"))
        {
            var propertyName = targetMethod.Name.Substring(4);
            
            if (!_columnCache.TryGetValue(propertyName, out var column))
            {
                column = new SqlColumn(propertyName, _dialect);
                _columnCache[propertyName] = column;
            }
            
            return column;
        }

        // Intercept ToString for table name
        if (targetMethod.Name == "ToString")
        {
            return FormatIdentifier(_tableName, _dialect);
        }

        return null;
    }

    private static string FormatIdentifier(string identifier, SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.PostgreSql => $"\"{identifier}\"",
            SqlDialect.SqlServer => $"[{identifier}]",
            SqlDialect.MySql => $"`{identifier}`",
            SqlDialect.Sqlite => $"\"{identifier}\"",
            _ => identifier
        };
    }
}
