namespace Preql;

/// <summary>
/// Represents the result of a compiled query containing the SQL string and parameters.
/// </summary>
public readonly struct QueryResult
{
    /// <summary>
    /// The generated SQL query string with parameter placeholders.
    /// </summary>
    public string Sql { get; init; }

    /// <summary>
    /// The parameters extracted from the query, mapped by name to value.
    /// </summary>
    public object? Parameters { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryResult"/> struct.
    /// </summary>
    /// <param name="sql">The SQL query string.</param>
    /// <param name="parameters">The query parameters.</param>
    public QueryResult(string sql, object? parameters = null)
    {
        Sql = sql;
        Parameters = parameters;
    }
}
