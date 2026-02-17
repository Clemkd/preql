using System.Runtime.CompilerServices;
using System.Text;

namespace Preql;

/// <summary>
/// Interpolated string handler for building SQL queries without runtime reflection.
/// This handler is used automatically by the C# compiler when building interpolated strings.
/// </summary>
[InterpolatedStringHandler]
public ref struct PreqlSqlHandler
{
    private StringBuilder _builder;
    private List<object?> _parameters;
    private int _paramIndex;

    /// <summary>
    /// Initializes the handler with size hints from the compiler.
    /// </summary>
    public PreqlSqlHandler(int literalLength, int formattedCount)
    {
        _builder = new StringBuilder(literalLength);
        _parameters = new List<object?>(formattedCount);
        _paramIndex = 0;
    }

    /// <summary>
    /// Appends a literal string part.
    /// </summary>
    public void AppendLiteral(string s) => _builder.Append(s);

    /// <summary>
    /// Appends a SQL column reference.
    /// </summary>
    public void AppendFormatted(SqlColumn column) => _builder.Append(column.ToString());

    /// <summary>
    /// Appends a SQL table reference.
    /// </summary>
    public void AppendFormatted(SqlTable table) => _builder.Append(table.ToString());

    /// <summary>
    /// Appends a parameterized value.
    /// </summary>
    public void AppendFormatted(SqlValue value)
    {
        var paramName = $"@p{_paramIndex++}";
        _builder.Append(paramName);
        _parameters.Add(value.Value ?? DBNull.Value);
    }

    /// <summary>
    /// Appends a formatted value.
    /// Automatically determines if the value is a SQL element (column/table) or a parameter value.
    /// </summary>
    public void AppendFormatted<T>(T value)
    {
        if (value is SqlColumn column)
        {
            AppendFormatted(column);
        }
        else if (value is SqlTable table)
        {
            AppendFormatted(table);
        }
        else if (value is SqlValue sqlValue)
        {
            AppendFormatted(sqlValue);
        }
        else if (value is SqlTableAlias alias)
        {
            // Convert alias to table
            AppendFormatted(alias.AsTable());
        }
        else if (value is AliasProxy proxy)
        {
            // Convert generated proxy to table
            AppendFormatted(proxy.AsTable());
        }
        else
        {
            // Check if the value can be implicitly converted to SqlTable
            // This handles generated proxies like UserProxy that have implicit conversion operators
            var valueType = value?.GetType();
            if (valueType != null)
            {
                var toSqlTableMethod = valueType.GetMethod(
                    "op_Implicit",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null,
                    new[] { valueType },
                    null);
                
                if (toSqlTableMethod != null && toSqlTableMethod.ReturnType == typeof(SqlTable))
                {
                    var sqlTable = (SqlTable)toSqlTableMethod.Invoke(null, new object?[] { value })!;
                    AppendFormatted(sqlTable);
                    return;
                }
            }
            
            // Default: treat as a parameter for safety
            AppendFormatted(new SqlValue(value));
        }
    }

    /// <summary>
    /// Builds the final SQL string and parameter list.
    /// </summary>
    public (string Sql, IReadOnlyList<object?> Parameters) Build() => (_builder.ToString(), _parameters);

    /// <summary>
    /// Builds a FormattableString compatible with EF Core.
    /// </summary>
    public FormattableString BuildFormattable()
    {
        var sql = _builder.ToString();
        var args = _parameters.ToArray();
        
        // Replace @p0, @p1, etc. with {0}, {1}, etc.
        for (int i = 0; i < args.Length; i++)
        {
            sql = sql.Replace($"@p{i}", $"{{{i}}}");
        }
        
        return FormattableStringFactory.Create(sql, args);
    }
}
