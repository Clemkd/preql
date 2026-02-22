using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Preql.IntegrationTests.EFCore;

/// <summary>
/// Integration tests for Preql with EF Core and a SQLite in-memory database.
/// These tests verify that Preql-generated SQL executes correctly via
/// <see cref="Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.SqlQueryRaw{T}"/>
/// and <see cref="Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw"/>.
/// </summary>
public sealed class EFCoreSqliteTests : IDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly DbContextOptions _options;
    private readonly PreqlContext _preql = new(SqlDialect.Sqlite);

    public EFCoreSqliteTests()
    {
        _keepAlive = new SqliteConnection("Data Source=:memory:");
        _keepAlive.Open();

        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_keepAlive)
            .Options;

        using var db = new TestDbContext(_options);
        db.Database.EnsureCreated();
        SeedData(db);
    }

    public void Dispose() => _keepAlive.Dispose();

    private static void SeedData(TestDbContext db)
    {
        db.Categories.AddRange(
            new Category { Id = 1, Name = "Electronics" },
            new Category { Id = 2, Name = "Books" }
        );
        db.Products.AddRange(
            new Product { Id = 1, Name = "Laptop",    Price = 999.99,  Stock = 10, CategoryId = 1 },
            new Product { Id = 2, Name = "Phone",     Price = 499.99,  Stock = 25, CategoryId = 1 },
            new Product { Id = 3, Name = "Tablet",    Price = 299.99,  Stock = 15, CategoryId = 1 },
            new Product { Id = 4, Name = "C# in Depth", Price = 39.99, Stock = 50, CategoryId = 2 },
            new Product { Id = 5, Name = "Clean Code",  Price = 34.99, Stock = 60, CategoryId = 2 }
        );
        db.SaveChanges();
    }

    // ── SELECT – single table ─────────────────────────────────────────────────

    [Fact]
    public async Task Select_AllRows_ReturnsAllProducts()
    {
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p}");

        await using var db = new TestDbContext(_options);
        var results = await db.Database
            .SqlQueryRaw<Product>(query.Format, query.GetArguments()!)
            .ToListAsync();

        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task Select_WithWhere_ReturnsFilteredProducts()
    {
        double minPrice = 100.0;
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} WHERE {p.Price} >= {minPrice}");

        await using var db = new TestDbContext(_options);
        var results = await db.Database
            .SqlQueryRaw<Product>(query.Format, query.GetArguments()!)
            .ToListAsync();

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

        await using var db = new TestDbContext(_options);
        var results = await db.Database
            .SqlQueryRaw<Product>(query.Format, query.GetArguments()!)
            .ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.InRange(r.Price, minPrice, maxPrice));
    }

    [Fact]
    public async Task Select_WithOrderBy_ReturnsSortedResults()
    {
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} ORDER BY {p.Price} ASC");

        await using var db = new TestDbContext(_options);
        var results = await db.Database
            .SqlQueryRaw<Product>(query.Format, query.GetArguments()!)
            .ToListAsync();

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

        await using var db = new TestDbContext(_options);
        var results = await db.Database
            .SqlQueryRaw<Product>(query.Format, query.GetArguments()!)
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal("Clean Code", results[0].Name);
    }

    // ── SELECT – join ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Select_InnerJoin_ReturnsJoinedResults()
    {
        var query = _preql.Query<Product, Category>((p, c) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} INNER JOIN {c} ON {p.CategoryId} = {c.Id}");

        await using var db = new TestDbContext(_options);
        var results = await db.Database
            .SqlQueryRaw<Product>(query.Format, query.GetArguments()!)
            .ToListAsync();

        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task Select_JoinWithWhereParameter_ReturnsFilteredJoinedResults()
    {
        string categoryName = "Electronics";
        var query = _preql.Query<Product, Category>((p, c) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} INNER JOIN {c} ON {p.CategoryId} = {c.Id} WHERE {c.Name} = {categoryName}");

        await using var db = new TestDbContext(_options);
        var results = await db.Database
            .SqlQueryRaw<Product>(query.Format, query.GetArguments()!)
            .ToListAsync();

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal(1, r.CategoryId));
    }

    // ── SELECT – aggregates ───────────────────────────────────────────────────

    [Fact]
    public async Task Select_CountAggregate_ReturnsCorrectCount()
    {
        int categoryId = 1;
        var query = _preql.Query<Product>((p) =>
            $"SELECT COUNT(*) AS \"Value\" FROM {p} WHERE {p.CategoryId} = {categoryId}");

        await using var db = new TestDbContext(_options);
        var count = await db.Database
            .SqlQueryRaw<ScalarResult>(query.Format, query.GetArguments()!)
            .FirstAsync();

        Assert.Equal(3, count.Value);
    }

    [Fact]
    public async Task Select_GroupBy_ReturnsAggregatedResults()
    {
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.CategoryId} AS \"Id\", COUNT(*) AS \"Count\" FROM {p} GROUP BY {p.CategoryId} ORDER BY {p.CategoryId}");

        await using var db = new TestDbContext(_options);
        var results = await db.Database
            .SqlQueryRaw<CategoryCount>(query.Format, query.GetArguments()!)
            .ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].Id);
        Assert.Equal(3, results[0].Count);
        Assert.Equal(2, results[1].Id);
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

        Assert.Equal(
            "INSERT INTO \"Product\" (\"Name\", \"Price\", \"Stock\", \"CategoryId\") VALUES ({0}, {1}, {2}, {3})",
            query.Format);

        await using var db = new TestDbContext(_options);
        var affected = await db.Database.ExecuteSqlRawAsync(query.Format, query.GetArguments()!);
        Assert.Equal(1, affected);

        var count = await db.Database
            .SqlQueryRaw<ScalarResult>("SELECT COUNT(*) AS \"Value\" FROM \"Product\" WHERE \"Name\" = 'Headphones'")
            .FirstAsync();
        Assert.Equal(1, count.Value);
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

        await using var db = new TestDbContext(_options);
        var affected = await db.Database.ExecuteSqlRawAsync(query.Format, query.GetArguments()!);
        Assert.Equal(1, affected);

        var updated = await db.Database
            .SqlQueryRaw<Product>(
                "SELECT \"Id\", \"Name\", \"Price\", \"Stock\", \"CategoryId\" FROM \"Product\" WHERE \"Id\" = 1")
            .FirstAsync();
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

        await using var db = new TestDbContext(_options);
        await db.Database.ExecuteSqlRawAsync(query.Format, query.GetArguments()!);

        var updated = await db.Database
            .SqlQueryRaw<Product>(
                "SELECT \"Id\", \"Name\", \"Price\", \"Stock\", \"CategoryId\" FROM \"Product\" WHERE \"Id\" = 3")
            .FirstAsync();
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

        await using var db = new TestDbContext(_options);
        var affected = await db.Database.ExecuteSqlRawAsync(query.Format, query.GetArguments()!);
        Assert.Equal(1, affected);

        var count = await db.Database
            .SqlQueryRaw<ScalarResult>("SELECT COUNT(*) AS \"Value\" FROM \"Product\"")
            .FirstAsync();
        Assert.Equal(4, count.Value);
    }

    [Fact]
    public async Task Delete_WithMultipleConditions_RemovesMatchingRows()
    {
        int categoryId = 2;
        double maxPrice = 40.0;

        var query = _preql.Query<Product>((p) =>
            $"DELETE FROM \"Product\" WHERE \"CategoryId\" = {categoryId} AND \"Price\" <= {maxPrice}");

        await using var db = new TestDbContext(_options);
        var affected = await db.Database.ExecuteSqlRawAsync(query.Format, query.GetArguments()!);
        Assert.Equal(2, affected);

        var count = await db.Database
            .SqlQueryRaw<ScalarResult>("SELECT COUNT(*) AS \"Value\" FROM \"Product\" WHERE \"CategoryId\" = 2")
            .FirstAsync();
        Assert.Equal(0, count.Value);
    }

    // ── Projection types ──────────────────────────────────────────────────────

    private class ScalarResult
    {
        public long Value { get; set; }
    }

    private class CategoryCount
    {
        public int Id { get; set; }
        public long Count { get; set; }
    }
}
