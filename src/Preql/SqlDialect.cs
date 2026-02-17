namespace Preql;

/// <summary>
/// Represents the SQL dialect to use for query generation.
/// </summary>
public enum SqlDialect
{
    /// <summary>
    /// PostgreSQL database dialect.
    /// </summary>
    PostgreSql,

    /// <summary>
    /// Microsoft SQL Server database dialect.
    /// </summary>
    SqlServer,

    /// <summary>
    /// MySQL database dialect.
    /// </summary>
    MySql,

    /// <summary>
    /// SQLite database dialect.
    /// </summary>
    Sqlite
}
