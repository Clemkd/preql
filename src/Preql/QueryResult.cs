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
    /// The query as a <see cref="FormattableString"/> for use with APIs such as
    /// EF Core's <c>FromSqlInterpolated</c>. SQL identifiers are embedded as
    /// literal text while parameter values are represented as format arguments
    /// (<c>{0}</c>, <c>{1}</c>, …) so the ORM can sanitize them naturally.
    /// </summary>
    public FormattableString? Interpolated { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryResult"/> struct.
    /// </summary>
    /// <param name="sql">The SQL query string.</param>
    /// <param name="parameters">The query parameters.</param>
    /// <param name="interpolated">The query as a <see cref="FormattableString"/>.</param>
    public QueryResult(string sql, object? parameters = null, FormattableString? interpolated = null)
    {
        Sql = sql;
        Parameters = parameters;
        Interpolated = interpolated;
    }
}
