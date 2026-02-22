using Dapper;
using Npgsql;

namespace Preql.IntegrationTests.Dapper;

/// <summary>
/// Integration tests for Preql with Dapper and a PostgreSQL database.
/// Tests are automatically skipped when the <c>POSTGRES_CONNECTION_STRING</c>
/// environment variable is not set (see <see cref="PostgresFactAttribute"/>).
/// </summary>
public sealed class DapperPostgreSqlTests : IAsyncLifetime
{
    private readonly string? _connectionString = Environment.GetEnvironmentVariable(PostgresFactAttribute.EnvVar);
    private NpgsqlConnection? _connection;
    private readonly PreqlContext _preql = new(SqlDialect.PostgreSql);

    public async Task InitializeAsync()
    {
        if (_connectionString is null) return;

        _connection = new NpgsqlConnection(_connectionString);
        await _connection.OpenAsync();
        await CreateSchemaAsync();
        await SeedDataAsync();
    }

    public async Task DisposeAsync()
    {
        if (_connection is null) return;
        await _connection.ExecuteAsync("DROP TABLE IF EXISTS \"Product\"; DROP TABLE IF EXISTS product_category;");
        await _connection.DisposeAsync();
    }

    private async Task CreateSchemaAsync()
    {
        await _connection!.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS product_category (
                ""Id""   SERIAL PRIMARY KEY,
                ""Name"" TEXT   NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ""Product"" (
                ""Id""         SERIAL  PRIMARY KEY,
                ""Name""       TEXT    NOT NULL,
                ""Price""      NUMERIC NOT NULL,
                ""Stock""      INTEGER NOT NULL,
                ""CategoryId"" INTEGER NOT NULL REFERENCES product_category(""Id"")
            );");
    }

    private async Task SeedDataAsync()
    {
        await _connection!.ExecuteAsync(@"
            TRUNCATE product_category, ""Product"" RESTART IDENTITY CASCADE;
            INSERT INTO product_category (""Id"", ""Name"") OVERRIDING SYSTEM VALUE VALUES
                (1, 'Electronics'), (2, 'Books');
            INSERT INTO ""Product"" (""Id"", ""Name"", ""Price"", ""Stock"", ""CategoryId"") OVERRIDING SYSTEM VALUE VALUES
                (1, 'Laptop',       999.99, 10, 1),
                (2, 'Phone',        499.99, 25, 1),
                (3, 'Tablet',       299.99, 15, 1),
                (4, 'C# in Depth',   39.99, 50, 2),
                (5, 'Clean Code',    34.99, 60, 2);");
    }

    // ── SELECT – single table ─────────────────────────────────────────────────

    [PostgresFact]
    public async Task Select_AllRows_ReturnsAllProducts()
    {
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var results = (await _connection!.QueryAsync<Product>(sql, parameters)).ToList();

        Assert.Equal(5, results.Count);
    }

    [PostgresFact]
    public async Task Select_WithWhere_ReturnsFilteredProducts()
    {
        double minPrice = 100.0;
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} WHERE {p.Price} >= {minPrice}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var results = (await _connection!.QueryAsync<Product>(sql, parameters)).ToList();

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.Price >= minPrice));
    }

    [PostgresFact]
    public async Task Select_JoinWithWhereParameter_ReturnsFilteredJoinedResults()
    {
        string categoryName = "Electronics";
        var query = _preql.Query<Product, Category>((p, c) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} INNER JOIN {c} ON {p.CategoryId} = {c.Id} WHERE {c.Name} = {categoryName}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var results = (await _connection!.QueryAsync<Product>(sql, parameters)).ToList();

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal(1, r.CategoryId));
    }

    // ── INSERT ────────────────────────────────────────────────────────────────

    [PostgresFact]
    public async Task Insert_SingleRow_ProductIsStoredInDatabase()
    {
        await SeedDataAsync(); // ensure a clean, known state before mutating

        string name = "Headphones";
        double price = 149.99;
        int stock = 30;
        int categoryId = 1;

        // For INSERT, use Preql for parameter binding; column names are literal SQL text.
        var query = _preql.Query<Product>((p) =>
            $"INSERT INTO \"Product\" (\"Name\", \"Price\", \"Stock\", \"CategoryId\") VALUES ({name}, {price}, {stock}, {categoryId})");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var affected = await _connection!.ExecuteAsync(sql, parameters);
        Assert.Equal(1, affected);

        var count = await _connection!.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM \"Product\" WHERE \"Name\" = 'Headphones'");
        Assert.Equal(1, count);
    }

    // ── UPDATE (PostgreSQL supports aliases in UPDATE) ────────────────────────

    [PostgresFact]
    public async Task Update_SetPrice_UpdatesExistingRow()
    {
        await SeedDataAsync(); // ensure a clean, known state before mutating

        double newPrice = 799.99;
        int productId = 1;

        // PostgreSQL supports table aliases: UPDATE "Product" p SET p."Price" = @p0 WHERE p."Id" = @p1
        var query = _preql.Query<Product>((p) =>
            $"UPDATE {p} SET {p.Price} = {newPrice} WHERE {p.Id} = {productId}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var affected = await _connection!.ExecuteAsync(sql, parameters);
        Assert.Equal(1, affected);

        var updated = await _connection!.QuerySingleAsync<Product>(
            "SELECT \"Id\", \"Name\", \"Price\", \"Stock\", \"CategoryId\" FROM \"Product\" WHERE \"Id\" = 1");
        Assert.Equal(799.99, updated.Price);
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    [PostgresFact]
    public async Task Delete_ByPrimaryKey_RemovesRow()
    {
        await SeedDataAsync(); // ensure a clean, known state before mutating

        int productId = 5;

        var query = _preql.Query<Product>((p) =>
            $"DELETE FROM {p} WHERE {p.Id} = {productId}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var affected = await _connection!.ExecuteAsync(sql, parameters);
        Assert.Equal(1, affected);

        var count = await _connection!.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM \"Product\"");
        Assert.Equal(4, count);
    }
}
