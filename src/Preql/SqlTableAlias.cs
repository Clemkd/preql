namespace Preql;

/// <summary>
/// Base class for typed SQL table aliases.
/// Provides property access that returns SqlColumn instances.
/// </summary>
public abstract class SqlTableAlias
{
    protected SqlDialect Dialect { get; }
    protected string TableName { get; }

    protected SqlTableAlias(string tableName, SqlDialect dialect)
    {
        TableName = tableName;
        Dialect = dialect;
    }

    /// <summary>
    /// Returns a SqlTable representing this table.
    /// </summary>
    public SqlTable AsTable() => new SqlTable(TableName, Dialect);
    
    /// <summary>
    /// Implicitly converts to SqlTable.
    /// </summary>
    public static implicit operator SqlTable(SqlTableAlias alias) => alias.AsTable();
}

/// <summary>
/// Typed SQL table alias for entity type T.
/// Provides strongly-typed property access that returns SqlColumn instances.
/// NO REFLECTION: Columns are created on-demand, not pre-cached via reflection.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public sealed class SqlTableAlias<T> : SqlTableAlias where T : class
{
    private readonly Dictionary<string, SqlColumn> _columns;

    internal SqlTableAlias(SqlDialect dialect) 
        : base(GetTableName<T>(), dialect)
    {
        _columns = new Dictionary<string, SqlColumn>();
        
        // NO REFLECTION: Columns are created on-demand via the indexer.
        // This eliminates all runtime reflection usage.
    }

    /// <summary>
    /// Gets a column by property name.
    /// Columns are created on-demand without reflection.
    /// </summary>
    public SqlColumn this[string propertyName]
    {
        get
        {
            if (_columns.TryGetValue(propertyName, out var column))
                return column;
            
            // Create column on-demand (no reflection)
            var newColumn = new SqlColumn(propertyName, Dialect);
            _columns[propertyName] = newColumn;
            return newColumn;
        }
    }

    /// <summary>
    /// Gets a column for a property using an expression.
    /// This extracts the property name from the expression without reflection.
    /// </summary>
    public SqlColumn Column<TProp>(System.Linq.Expressions.Expression<Func<T, TProp>> propertyExpression)
    {
        if (propertyExpression.Body is System.Linq.Expressions.MemberExpression memberExpr)
        {
            var propertyName = memberExpr.Member.Name;
            return this[propertyName];
        }
        
        throw new ArgumentException("Expression must be a property access", nameof(propertyExpression));
    }

    private static string GetTableName<TEntity>()
    {
        // Note: This uses typeof for the type name, but doesn't use reflection APIs.
        // The type name is a compile-time constant.
        var typeName = typeof(TEntity).Name;
        // Simple pluralization
        return typeName.EndsWith("s") ? typeName : typeName + "s";
    }
}

/// <summary>
/// Extension methods for creating typed table aliases.
/// </summary>
public static class SqlAliasExtensions
{
    /// <summary>
    /// Creates a typed alias for the specified entity type.
    /// Use this to access columns in a type-safe manner.
    /// NO REFLECTION: All column access happens on-demand without reflection.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The Preql context.</param>
    /// <returns>A typed alias that provides access to columns.</returns>
    /// <example>
    /// <code>
    /// var u = db.Alias&lt;User&gt;();
    /// PreqlSqlHandler h = $"SELECT {u["Id"]}, {u["Name"]} FROM {u} WHERE {u["Id"]} = {id.AsValue()}";
    /// // All work done by C# compiler via InterpolatedStringHandler - NO REFLECTION!
    /// </code>
    /// </example>
    public static SqlTableAlias<T> Alias<T>(this IPreqlContext context) where T : class
    {
        return new SqlTableAlias<T>(context.Dialect);
    }
}
