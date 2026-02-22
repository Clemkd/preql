namespace Preql.IntegrationTests;

/// <summary>
/// A <see cref="FactAttribute"/> that automatically skips the test when the
/// <c>POSTGRES_CONNECTION_STRING</c> environment variable is not set.
/// Set the variable to a valid PostgreSQL connection string to enable these tests.
/// Example: <c>Host=localhost;Database=preql_test;Username=postgres;Password=secret</c>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgresFactAttribute : FactAttribute
{
    internal const string EnvVar = "POSTGRES_CONNECTION_STRING";

    public PostgresFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(EnvVar) is null)
            Skip = $"Set the '{EnvVar}' environment variable to run PostgreSQL integration tests.";
    }
}
