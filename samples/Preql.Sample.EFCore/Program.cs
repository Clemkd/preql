using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Preql;
using Preql.Sample.EFCore;

Console.WriteLine("🛡️ Preql + EF Core SQLite Sample");
Console.WriteLine("====================================\n");

// ── Keep one open SQLite in-memory connection for the lifetime of the app ────
// SQLite ":memory:" databases are destroyed when the last connection closes.
// Holding this connection open ensures the schema and data survive across
// the multiple DbContext instances created below.
await using var keepAliveConnection = new SqliteConnection("Data Source=:memory:");
await keepAliveConnection.OpenAsync();

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(keepAliveConnection)
    .Options;

// ── Set up schema and seed data ───────────────────────────────────────────────
await using (var db = new AppDbContext(options))
{
    await db.Database.EnsureCreatedAsync();

    db.Users.AddRange(
        new User { Id = 1, Name = "Alice",   Email = "alice@example.com",   Age = 30 },
        new User { Id = 2, Name = "Bob",     Email = "bob@example.com",     Age = 25 },
        new User { Id = 3, Name = "Charlie", Email = "charlie@example.com", Age = 35 }
    );
    db.Posts.AddRange(
        new Post { Id = 1, Message = "Hello from Alice!",   UserId = 1 },
        new Post { Id = 2, Message = "Bob's first post",    UserId = 2 },
        new Post { Id = 3, Message = "Alice strikes again", UserId = 1 }
    );
    await db.SaveChangesAsync();
}

var preql = new PreqlContext(SqlDialect.Sqlite);

// ── Example 1: Single-table query with parameter ─────────────────────────────
Console.WriteLine("Example 1: Single-Table Query (SQLite dialect)");
int minAge = 28;

var q1 = preql.Query<User>((u) =>
    $"SELECT {u.Id}, {u.Name}, {u.Email} FROM {u} WHERE {u.Age} >= {minAge}");

Console.WriteLine($"  SQL Format : {q1.Format}");
Console.WriteLine($"  Parameters : {string.Join(", ", q1.GetArguments().Select((v, i) => $"@p{i}={v}"))}");

await using (var db = new AppDbContext(options))
{
    var users = await db.Database
        .SqlQueryRaw<UserResult>(q1.Format, q1.GetArguments()!)
        .ToListAsync();

    Console.WriteLine($"  Results ({users.Count}):");
    foreach (var u in users)
        Console.WriteLine($"    [{u.Id}] {u.Name} <{u.Email}>");
}
Console.WriteLine();

// ── Example 2: Two-table JOIN query ──────────────────────────────────────────
Console.WriteLine("Example 2: Two-Table JOIN Query");

var q2 = preql.Query<User, Post>((u, p) =>
    $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");

Console.WriteLine($"  SQL Format : {q2.Format}");

await using (var db = new AppDbContext(options))
{
    var joined = await db.Database
        .SqlQueryRaw<UserPostResult>(q2.Format, q2.GetArguments()!)
        .ToListAsync();

    Console.WriteLine($"  Results ({joined.Count}):");
    foreach (var r in joined)
        Console.WriteLine($"    {r.Name}: \"{r.Message}\"");
}
Console.WriteLine();

// ── Example 3: JOIN with WHERE parameter ─────────────────────────────────────
Console.WriteLine("Example 3: JOIN with WHERE parameter");
string search = "Alice";

var q3 = preql.Query<User, Post>((u, p) =>
    $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId} WHERE {u.Name} = {search}");

Console.WriteLine($"  SQL Format : {q3.Format}");
Console.WriteLine($"  Parameters : {string.Join(", ", q3.GetArguments().Select((v, i) => $"@p{i}={v}"))}");

await using (var db = new AppDbContext(options))
{
    var filtered = await db.Database
        .SqlQueryRaw<UserPostResult>(q3.Format, q3.GetArguments()!)
        .ToListAsync();

    Console.WriteLine($"  Results ({filtered.Count}):");
    foreach (var r in filtered)
        Console.WriteLine($"    {r.Name}: \"{r.Message}\"");
}
Console.WriteLine();

Console.WriteLine("✅ All examples completed successfully!");

// ── Result projection types ───────────────────────────────────────────────────

public class UserResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserPostResult
{
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
