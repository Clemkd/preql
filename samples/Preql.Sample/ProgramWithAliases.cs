using Preql;

namespace Preql.Sample;

/// <summary>
/// Demonstrates the clean lambda API: context.Query&lt;T1, T2&gt;((u, p) =&gt; $"...")
/// No proxy variables are created by the developer — Preql analyzes the expression tree
/// to identify table references, column references and parameter values automatically.
/// </summary>
public static class AliasExamples
{
    public static void Run()
    {
        Console.WriteLine("🛡️ Preql Multi-Table Query Sample");
        Console.WriteLine("====================================\n");

        var postgres = new PreqlContext(SqlDialect.PostgreSql);
        var mssql = new PreqlContext(SqlDialect.SqlServer);
        var mysql = new PreqlContext(SqlDialect.MySql);

        // ── Example 1: single table with alias ──────────────────────────────────
        Console.WriteLine("Example 1: Single-Table Query");
        int userId = 123;
        var q1 = postgres.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name}, {u.Email} FROM {u} WHERE {u.Id} = {userId}");

        Console.WriteLine($"SQL:    {q1.Sql}");
        Console.WriteLine($"Params: {FormatParams(q1)}");
        Console.WriteLine();

        // ── Example 2: two-table JOIN (the problem-statement scenario) ───────────
        Console.WriteLine("Example 2: Two-Table JOIN Query  ← problem statement scenario");
        var q2 = postgres.Query<User, Post>((u, p) =>
            $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");

        Console.WriteLine($"SQL:    {q2.Sql}");
        Console.WriteLine($"Params: {FormatParams(q2)}");
        Console.WriteLine();

        // ── Example 3: two-table with WHERE parameters ───────────────────────────
        Console.WriteLine("Example 3: JOIN with WHERE parameters");
        string searchName = "%John%";
        int minAge = 25;
        var q3 = postgres.Query<User, Post>((u, p) =>
            $"""
            SELECT {u.Id}, {u.Name}, {u.Email}, {p.Message}
            FROM {u}
            INNER JOIN {p} ON {u.Id} = {p.UserId}
            WHERE {u.Name} LIKE {searchName}
              AND {u.Age} >= {minAge}
            ORDER BY {u.Name}
            """);

        Console.WriteLine($"SQL:    {q3.Sql}");
        Console.WriteLine($"Params: {FormatParams(q3)}");
        Console.WriteLine();

        // ── Example 4: SQL Server dialect ───────────────────────────────────────
        Console.WriteLine("Example 4: Same JOIN with SQL Server dialect");
        var q4 = mssql.Query<User, Post>((u, p) =>
            $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");

        Console.WriteLine($"SQL:    {q4.Sql}");
        Console.WriteLine();

        // ── Example 5: MySQL dialect ─────────────────────────────────────────────
        Console.WriteLine("Example 5: Same JOIN with MySQL dialect");
        var q5 = mysql.Query<User, Post>((u, p) =>
            $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");

        Console.WriteLine($"SQL:    {q5.Sql}");
        Console.WriteLine();

        // ── Example 6: three-table query ────────────────────────────────────────
        Console.WriteLine("Example 6: Three-Table Query");
        var q6 = postgres.Query<User, Post, User>((u, p, author) =>
            $"SELECT {u.Name}, {p.Message}, {author.Email} FROM {u} JOIN {p} ON {u.Id} = {p.UserId} JOIN {author} ON {p.UserId} = {author.Id}");

        Console.WriteLine($"SQL:    {q6.Sql}");
        Console.WriteLine();

        Console.WriteLine("✅ All examples completed successfully!");
        Console.WriteLine("\n🎯 Problem Statement Result:");
        Console.WriteLine("  Input:  context.Query<User, Post>((u, p) => $\"Select {u.Name}, {p.Message} From {u} Join...\")");
        Console.WriteLine($"  Output: \"{q2.Sql}\"");
    }

    private static string FormatParams(QueryResult q)
    {
        if (q.Parameters is not IReadOnlyList<object?> list || list.Count == 0)
            return "none";
        return string.Join(", ", list.Select((v, i) => $"@p{i}={v}"));
    }
}
