using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using Preql;

namespace Preql.Benchmarks;

/// <summary>
/// Benchmarks comparing SQL query generation:
/// <list type="bullet">
///   <item><description>
///     <b>WithoutInterceptor</b>: the runtime path — walks the expression tree to
///     produce the SQL at every call (no compile-time analysis).
///   </description></item>
///   <item><description>
///     <b>WithInterceptor</b>: the compile-time path — the Preql source generator
///     intercepts the <c>Query&lt;T&gt;</c> call and replaces it with a pre-analysed
///     stub that only applies dialect quoting at runtime (no expression-tree walking).
///   </description></item>
/// </list>
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class QueryGenerationBenchmarks
{
    private PreqlContext _context = null!;

    // Pre-built expressions for the WithoutInterceptor benchmark so that
    // expression-tree construction is not measured.
    private Expression<Func<User, FormattableString>> _simpleExpr = null!;
    private Expression<Func<User, FormattableString>> _paramExpr = null!;
    private Expression<Func<User, Post, FormattableString>> _joinExpr = null!;

    private int _userId;

    [GlobalSetup]
    public void Setup()
    {
        _context = new PreqlContext(SqlDialect.PostgreSql);
        _userId = 42;

        // Capture the expression trees once so that WithoutInterceptor only measures Analyze().
        _simpleExpr = (u) => $"SELECT {u.Id}, {u.Name} FROM {u}";
        _paramExpr  = (u) => $"SELECT {u.Id}, {u.Name} FROM {u} WHERE {u.Id} = {_userId}";
        _joinExpr   = (u, p) => $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}";
    }

    // ── Simple SELECT (no parameters) ────────────────────────────────────────

    /// <summary>Runtime expression-tree analysis for a simple SELECT.</summary>
    [Benchmark(Baseline = true, Description = "WithoutInterceptor_SimpleSelect")]
    public FormattableString WithoutInterceptor_SimpleSelect()
        => QueryExpressionAnalyzer.Analyze(_simpleExpr, SqlDialect.PostgreSql);

    /// <summary>Compile-time interceptor path for a simple SELECT.</summary>
    [Benchmark(Description = "WithInterceptor_SimpleSelect")]
    public FormattableString WithInterceptor_SimpleSelect()
        => _context.Query<User>((u) => $"SELECT {u.Id}, {u.Name} FROM {u}");

    // ── SELECT with one parameter ─────────────────────────────────────────────

    /// <summary>Runtime expression-tree analysis for a SELECT with a parameter.</summary>
    [Benchmark(Description = "WithoutInterceptor_WithParameter")]
    public FormattableString WithoutInterceptor_WithParameter()
        => QueryExpressionAnalyzer.Analyze(_paramExpr, SqlDialect.PostgreSql);

    /// <summary>Compile-time interceptor path for a SELECT with a parameter.</summary>
    [Benchmark(Description = "WithInterceptor_WithParameter")]
    public FormattableString WithInterceptor_WithParameter()
        => _context.Query<User>((u) => $"SELECT {u.Id}, {u.Name} FROM {u} WHERE {u.Id} = {_userId}");

    // ── JOIN query (two entity types) ─────────────────────────────────────────

    /// <summary>Runtime expression-tree analysis for a two-table JOIN.</summary>
    [Benchmark(Description = "WithoutInterceptor_JoinQuery")]
    public FormattableString WithoutInterceptor_JoinQuery()
        => QueryExpressionAnalyzer.Analyze(_joinExpr, SqlDialect.PostgreSql);

    /// <summary>Compile-time interceptor path for a two-table JOIN.</summary>
    [Benchmark(Description = "WithInterceptor_JoinQuery")]
    public FormattableString WithInterceptor_JoinQuery()
        => _context.Query<User, Post>((u, p) =>
            $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Post
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}
