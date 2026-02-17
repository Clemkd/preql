namespace Preql;

/// <summary>
/// Represents a SQL column in a typed query.
/// Used by the InterpolatedStringHandler to identify column references.
/// </summary>
public readonly struct SqlColumn
{
    private readonly string _name;
    private readonly SqlDialect _dialect;
    private readonly string? _tableAlias;

    internal SqlColumn(string name, SqlDialect dialect, string? tableAlias = null)
    {
        _name = name;
        _dialect = dialect;
        _tableAlias = tableAlias;
    }

    public override string ToString()
    {
        var quotedName = _dialect switch
        {
            SqlDialect.PostgreSql => $"\"{_name}\"",
            SqlDialect.SqlServer => $"[{_name}]",
            SqlDialect.MySql => $"`{_name}`",
            SqlDialect.Sqlite => $"\"{_name}\"",
            _ => $"[{_name}]"
        };

        // If table alias is provided, prefix the column with it
        return string.IsNullOrEmpty(_tableAlias) 
            ? quotedName 
            : $"{_tableAlias}.{quotedName}";
    }
}

/// <summary>
/// Represents a SQL table/alias in a typed query.
/// Used by the InterpolatedStringHandler to identify table references.
/// </summary>
public readonly struct SqlTable
{
    private readonly string _name;
    private readonly SqlDialect _dialect;
    private readonly string? _alias;

    internal SqlTable(string name, SqlDialect dialect, string? alias = null)
    {
        _name = name;
        _dialect = dialect;
        _alias = alias;
    }

    public override string ToString()
    {
        var quotedName = _dialect switch
        {
            SqlDialect.PostgreSql => $"\"{_name}\"",
            SqlDialect.SqlServer => $"[{_name}]",
            SqlDialect.MySql => $"`{_name}`",
            SqlDialect.Sqlite => $"\"{_name}\"",
            _ => $"[{_name}]"
        };

        // If alias is provided, append it after the table name
        return string.IsNullOrEmpty(_alias) 
            ? quotedName 
            : $"{quotedName} {_alias}";
    }
}

/// <summary>
/// Wraps a value to be used as a SQL parameter.
/// Used by the InterpolatedStringHandler to identify parameter values.
/// </summary>
public readonly struct SqlValue
{
    internal readonly object? Value;

    public SqlValue(object? value)
    {
        Value = value;
    }
}

/// <summary>
/// Extension methods for converting values to SqlValue.
/// </summary>
public static class SqlValueExtensions
{
    /// <summary>
    /// Wraps a value as a SQL parameter.
    /// </summary>
    public static SqlValue AsValue<T>(this T value) => new SqlValue(value);
}
