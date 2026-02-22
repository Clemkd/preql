using Dapper;
using Microsoft.Data.Sqlite;

namespace Preql.IntegrationTests.Dapper;

/// <summary>
/// Integration tests for Preql with Dapper and a SQLite in-memory database.
/// These tests verify that Preql-generated SQL, converted to Dapper's named-parameter
/// format via <see cref="DapperHelper.ToDapper"/>, executes correctly.
/// </summary>
public sealed class DapperSqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PreqlContext _preql = new(SqlDialect.Sqlite);

    public DapperSqliteTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        CreateSchema();
        SeedData();
    }

    public void Dispose() => _connection.Dispose();

    private void CreateSchema()
    {
        _connection.Execute(@"
            CREATE TABLE product_category (
                Id   INTEGER PRIMARY KEY,
                Name TEXT    NOT NULL
            );
            CREATE TABLE Product (
                Id         INTEGER PRIMARY KEY,
                Name       TEXT    NOT NULL,
                Price      REAL    NOT NULL,
                Stock      INTEGER NOT NULL,
                CategoryId INTEGER NOT NULL REFERENCES product_category(Id)
            );");
    }

    private void SeedData()
    {
        _connection.Execute(@"
            INSERT INTO product_category (Id, Name) VALUES
                (1, 'Electronics'), (2, 'Books');
            INSERT INTO Product (Id, Name, Price, Stock, CategoryId) VALUES
                (1, 'Laptop',       999.99, 10, 1),
                (2, 'Phone',        499.99, 25, 1),
                (3, 'Tablet',       299.99, 15, 1),
                (4, 'C# in Depth',   39.99, 50, 2),
                (5, 'Clean Code',    34.99, 60, 2);");
    }

    // ── SELECT – single table ─────────────────────────────────────────────────

    [Fact]
    public async Task Select_AllRows_ReturnsAllProducts()
    {
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var results = (await _connection.QueryAsync<Product>(sql, parameters)).ToList();

        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task Select_WithWhere_ReturnsFilteredProducts()
    {
        double minPrice = 100.0;
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} WHERE {p.Price} >= {minPrice}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var results = (await _connection.QueryAsync<Product>(sql, parameters)).ToList();

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.Price >= minPrice));
    }

    [Fact]
    public async Task Select_WithMultipleWhereConditions_ReturnsCorrectResults()
    {
        double minPrice = 30.0;
        double maxPrice = 100.0;
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} WHERE {p.Price} >= {minPrice} AND {p.Price} <= {maxPrice}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var results = (await _connection.QueryAsync<Product>(sql, parameters)).ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.InRange(r.Price, minPrice, maxPrice));
    }

    [Fact]
    public async Task Select_WithOrderBy_ReturnsSortedResults()
    {
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} ORDER BY {p.Price} ASC");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var results = (await _connection.QueryAsync<Product>(sql, parameters)).ToList();

        Assert.Equal(5, results.Count);
        Assert.Equal("Clean Code", results[0].Name);
        Assert.Equal("Laptop", results[4].Name);
    }

    [Fact]
    public async Task Select_WithLike_ReturnsMatchingProducts()
    {
        string pattern = "%Code%";
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} WHERE {p.Name} LIKE {pattern}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var results = (await _connection.QueryAsync<Product>(sql, parameters)).ToList();

        Assert.Single(results);
        Assert.Equal("Clean Code", results[0].Name);
    }

    // ── SELECT – join ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Select_InnerJoin_ReturnsJoinedResults()
    {
        var query = _preql.Query<Product, Category>((p, c) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} INNER JOIN {c} ON {p.CategoryId} = {c.Id}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var results = (await _connection.QueryAsync<Product>(sql, parameters)).ToList();

        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task Select_JoinWithWhereParameter_ReturnsFilteredJoinedResults()
    {
        string categoryName = "Electronics";
        var query = _preql.Query<Product, Category>((p, c) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} INNER JOIN {c} ON {p.CategoryId} = {c.Id} WHERE {c.Name} = {categoryName}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var results = (await _connection.QueryAsync<Product>(sql, parameters)).ToList();

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal(1, r.CategoryId));
    }

    // ── SELECT – aggregates ───────────────────────────────────────────────────

    [Fact]
    public async Task Select_CountAggregate_ReturnsCorrectCount()
    {
        int categoryId = 1;
        var query = _preql.Query<Product>((p) =>
            $"SELECT COUNT(*) FROM {p} WHERE {p.CategoryId} = {categoryId}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var count = await _connection.ExecuteScalarAsync<long>(sql, parameters);

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Select_GroupBy_ReturnsAggregatedResults()
    {
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.CategoryId}, COUNT(*) AS Count FROM {p} GROUP BY {p.CategoryId} ORDER BY {p.CategoryId}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var results = (await _connection.QueryAsync<CategoryCount>(sql, parameters)).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].CategoryId);
        Assert.Equal(3, results[0].Count);
        Assert.Equal(2, results[1].CategoryId);
        Assert.Equal(2, results[1].Count);
    }

    // ── INSERT ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Insert_SingleRow_ProductIsStoredInDatabase()
    {
        string name = "Headphones";
        double price = 149.99;
        int stock = 30;
        int categoryId = 1;

        // For INSERT, we use Preql for type-safe parameter binding.
        // Column names are provided as literal SQL text in the template.
        var query = _preql.Query<Product>((p) =>
            $"INSERT INTO \"Product\" (\"Name\", \"Price\", \"Stock\", \"CategoryId\") VALUES ({name}, {price}, {stock}, {categoryId})");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var affected = await _connection.ExecuteAsync(sql, parameters);
        Assert.Equal(1, affected);

        var count = await _connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM \"Product\" WHERE \"Name\" = 'Headphones'");
        Assert.Equal(1, count);
    }

    // ── UPDATE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_SetPrice_UpdatesExistingRow()
    {
        double newPrice = 799.99;
        int productId = 1;

        // SQLite does not support table aliases in UPDATE statements.
        // We write the table and column names as literal SQL text.
        // Preql still provides safe, type-correct parameter binding.
        var query = _preql.Query<Product>((p) =>
            $"UPDATE \"Product\" SET \"Price\" = {newPrice} WHERE \"Id\" = {productId}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var affected = await _connection.ExecuteAsync(sql, parameters);
        Assert.Equal(1, affected);

        var updated = await _connection.QuerySingleAsync<Product>(
            "SELECT Id, Name, Price, Stock, CategoryId FROM \"Product\" WHERE \"Id\" = 1");
        Assert.Equal(799.99, updated.Price);
    }

    [Fact]
    public async Task Update_MultipleColumns_UpdatesCorrectly()
    {
        double newPrice = 249.99;
        int newStock = 5;
        int productId = 3;

        var query = _preql.Query<Product>((p) =>
            $"UPDATE \"Product\" SET \"Price\" = {newPrice}, \"Stock\" = {newStock} WHERE \"Id\" = {productId}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        await _connection.ExecuteAsync(sql, parameters);

        var updated = await _connection.QuerySingleAsync<Product>(
            "SELECT Id, Name, Price, Stock, CategoryId FROM \"Product\" WHERE \"Id\" = 3");
        Assert.Equal(249.99, updated.Price);
        Assert.Equal(5, updated.Stock);
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ByPrimaryKey_RemovesRow()
    {
        int productId = 5;

        // SQLite does not support table aliases in DELETE statements.
        // We write the table and column names as literal SQL text.
        var query = _preql.Query<Product>((p) =>
            $"DELETE FROM \"Product\" WHERE \"Id\" = {productId}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var affected = await _connection.ExecuteAsync(sql, parameters);
        Assert.Equal(1, affected);

        var count = await _connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM \"Product\"");
        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Delete_WithMultipleConditions_RemovesMatchingRows()
    {
        int categoryId = 2;
        double maxPrice = 40.0;

        var query = _preql.Query<Product>((p) =>
            $"DELETE FROM \"Product\" WHERE \"CategoryId\" = {categoryId} AND \"Price\" <= {maxPrice}");

        var (sql, parameters) = DapperHelper.ToDapper(query);
        var affected = await _connection.ExecuteAsync(sql, parameters);
        Assert.Equal(2, affected);

        var count = await _connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM \"Product\" WHERE \"CategoryId\" = 2");
        Assert.Equal(0, count);
    }

    // ── Projection types ──────────────────────────────────────────────────────

    private class CategoryCount
    {
        public int CategoryId { get; set; }
        public long Count { get; set; }
    }
}
