using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Preql.IntegrationTests.EFCore;

/// <summary>
/// Integration tests for Preql with EF Core and a PostgreSQL database.
/// Tests are automatically skipped when the <c>POSTGRES_CONNECTION_STRING</c>
/// environment variable is not set (see <see cref="PostgresFactAttribute"/>).
/// </summary>
public sealed class EFCorePostgreSqlTests : IAsyncLifetime
{
    private readonly string? _connectionString = Environment.GetEnvironmentVariable(PostgresFactAttribute.EnvVar);
    private DbContextOptions? _options;
    private readonly PreqlContext _preql = new(SqlDialect.PostgreSql);

    public async Task InitializeAsync()
    {
        if (_connectionString is null) return;

        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        await using var db = new TestDbContext(_options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await SeedDataAsync(db);
    }

    public async Task DisposeAsync()
    {
        if (_options is null) return;
        await using var db = new TestDbContext(_options);
        await db.Database.EnsureDeletedAsync();
    }

    private static async Task SeedDataAsync(TestDbContext db)
    {
        db.Categories.AddRange(
            new Category { Id = 1, Name = "Electronics" },
            new Category { Id = 2, Name = "Books" }
        );
        db.Products.AddRange(
            new Product { Id = 1, Name = "Laptop",      Price = 999.99, Stock = 10, CategoryId = 1 },
            new Product { Id = 2, Name = "Phone",       Price = 499.99, Stock = 25, CategoryId = 1 },
            new Product { Id = 3, Name = "Tablet",      Price = 299.99, Stock = 15, CategoryId = 1 },
            new Product { Id = 4, Name = "C# in Depth", Price = 39.99,  Stock = 50, CategoryId = 2 },
            new Product { Id = 5, Name = "Clean Code",  Price = 34.99,  Stock = 60, CategoryId = 2 }
        );
        await db.SaveChangesAsync();
    }

    // ── SELECT – single table ─────────────────────────────────────────────────

    [PostgresFact]
    public async Task Select_AllRows_ReturnsAllProducts()
    {
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p}");

        await using var db = new TestDbContext(_options!);
        var results = await db.Database
            .SqlQueryRaw<Product>(query.Format, query.GetArguments()!)
            .ToListAsync();

        Assert.Equal(5, results.Count);
    }

    [PostgresFact]
    public async Task Select_WithWhere_ReturnsFilteredProducts()
    {
        double minPrice = 100.0;
        var query = _preql.Query<Product>((p) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} WHERE {p.Price} >= {minPrice}");

        await using var db = new TestDbContext(_options!);
        var results = await db.Database
            .SqlQueryRaw<Product>(query.Format, query.GetArguments()!)
            .ToListAsync();

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.Price >= minPrice));
    }

    [PostgresFact]
    public async Task Select_JoinWithWhereParameter_ReturnsFilteredJoinedResults()
    {
        string categoryName = "Electronics";
        var query = _preql.Query<Product, Category>((p, c) =>
            $"SELECT {p.Id}, {p.Name}, {p.Price}, {p.Stock}, {p.CategoryId} FROM {p} INNER JOIN {c} ON {p.CategoryId} = {c.Id} WHERE {c.Name} = {categoryName}");

        await using var db = new TestDbContext(_options!);
        var results = await db.Database
            .SqlQueryRaw<Product>(query.Format, query.GetArguments()!)
            .ToListAsync();

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal(1, r.CategoryId));
    }

    // ── INSERT ────────────────────────────────────────────────────────────────

    [PostgresFact]
    public async Task Insert_SingleRow_ProductIsStoredInDatabase()
    {
        string name = "Headphones";
        double price = 149.99;
        int stock = 30;
        int categoryId = 1;

        // For INSERT, use Preql for parameter binding; column names are literal SQL text.
        var query = _preql.Query<Product>((p) =>
            $"INSERT INTO \"Product\" (\"Name\", \"Price\", \"Stock\", \"CategoryId\") VALUES ({name}, {price}, {stock}, {categoryId})");

        await using var db = new TestDbContext(_options!);
        var affected = await db.Database.ExecuteSqlRawAsync(query.Format, query.GetArguments()!);
        Assert.Equal(1, affected);
    }

    // ── UPDATE (PostgreSQL supports aliases in UPDATE) ────────────────────────

    [PostgresFact]
    public async Task Update_SetPrice_UpdatesExistingRow()
    {
        double newPrice = 799.99;
        int productId = 1;

        // PostgreSQL supports table aliases in UPDATE: UPDATE "Product" p SET p."Price" = $1 WHERE p."Id" = $2
        var query = _preql.Query<Product>((p) =>
            $"UPDATE {p} SET {p.Price} = {newPrice} WHERE {p.Id} = {productId}");

        await using var db = new TestDbContext(_options!);
        var affected = await db.Database.ExecuteSqlRawAsync(query.Format, query.GetArguments()!);
        Assert.Equal(1, affected);

        var updated = await db.Database
            .SqlQueryRaw<Product>(
                "SELECT \"Id\", \"Name\", \"Price\", \"Stock\", \"CategoryId\" FROM \"Product\" WHERE \"Id\" = 1")
            .FirstAsync();
        Assert.Equal(799.99, updated.Price);
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    [PostgresFact]
    public async Task Delete_ByPrimaryKey_RemovesRow()
    {
        int productId = 5;

        var query = _preql.Query<Product>((p) =>
            $"DELETE FROM {p} WHERE {p.Id} = {productId}");

        await using var db = new TestDbContext(_options!);
        var affected = await db.Database.ExecuteSqlRawAsync(query.Format, query.GetArguments()!);
        Assert.Equal(1, affected);
    }
}
