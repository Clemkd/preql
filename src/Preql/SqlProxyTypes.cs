namespace Preql;

/// <summary>
/// Represents a SQL column in a typed query.
/// Used by the InterpolatedStringHandler to identify column references.
/// </summary>
public readonly struct SqlColumn
{
    private readonly string _name;
    private readonly SqlDialect _dialect;

    internal SqlColumn(string name, SqlDialect dialect)
    {
        _name = name;
        _dialect = dialect;
    }

    public override string ToString()
    {
        return _dialect switch
        {
            SqlDialect.PostgreSql => $"\"{_name}\"",
            SqlDialect.SqlServer => $"[{_name}]",
            SqlDialect.MySql => $"`{_name}`",
            SqlDialect.Sqlite => $"\"{_name}\"",
            _ => $"[{_name}]"
        };
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

    internal SqlTable(string name, SqlDialect dialect)
    {
        _name = name;
        _dialect = dialect;
    }

    public override string ToString()
    {
        return _dialect switch
        {
            SqlDialect.PostgreSql => $"\"{_name}\"",
            SqlDialect.SqlServer => $"[{_name}]",
            SqlDialect.MySql => $"`{_name}`",
            SqlDialect.Sqlite => $"\"{_name}\"",
            _ => $"[{_name}]"
        };
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
