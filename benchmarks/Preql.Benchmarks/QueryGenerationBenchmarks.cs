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
    private Expression<Func<User, Post, Order, Product, FormattableString>> _complexExpr = null!;

    private int _userId;

    // ── 100 runtime parameters for the complex benchmark ─────────────────────
    private int _p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8, _p9, _p10;
    private int _p11, _p12, _p13, _p14, _p15, _p16, _p17, _p18, _p19, _p20;
    private int _p21, _p22, _p23, _p24, _p25, _p26, _p27, _p28, _p29, _p30;
    private int _p31, _p32, _p33, _p34, _p35, _p36, _p37, _p38, _p39, _p40;
    private int _p41, _p42, _p43, _p44, _p45, _p46, _p47, _p48, _p49, _p50;
    private int _p51, _p52, _p53, _p54, _p55, _p56, _p57, _p58, _p59, _p60;
    private int _p61, _p62, _p63, _p64, _p65, _p66, _p67, _p68, _p69, _p70;
    private int _p71, _p72, _p73, _p74, _p75, _p76, _p77, _p78, _p79, _p80;
    private int _p81, _p82, _p83, _p84, _p85, _p86, _p87, _p88, _p89, _p90;
    private int _p91, _p92, _p93, _p94, _p95, _p96, _p97, _p98, _p99, _p100;

    [GlobalSetup]
    public void Setup()
    {
        _context = new PreqlContext(SqlDialect.PostgreSql);
        _userId = 42;

        // Initialise all 100 runtime parameters.
        _p1=1;_p2=2;_p3=3;_p4=4;_p5=5;_p6=6;_p7=7;_p8=8;_p9=9;_p10=10;
        _p11=11;_p12=12;_p13=13;_p14=14;_p15=15;_p16=16;_p17=17;_p18=18;_p19=19;_p20=20;
        _p21=21;_p22=22;_p23=23;_p24=24;_p25=25;_p26=26;_p27=27;_p28=28;_p29=29;_p30=30;
        _p31=31;_p32=32;_p33=33;_p34=34;_p35=35;_p36=36;_p37=37;_p38=38;_p39=39;_p40=40;
        _p41=41;_p42=42;_p43=43;_p44=44;_p45=45;_p46=46;_p47=47;_p48=48;_p49=49;_p50=50;
        _p51=51;_p52=52;_p53=53;_p54=54;_p55=55;_p56=56;_p57=57;_p58=58;_p59=59;_p60=60;
        _p61=61;_p62=62;_p63=63;_p64=64;_p65=65;_p66=66;_p67=67;_p68=68;_p69=69;_p70=70;
        _p71=71;_p72=72;_p73=73;_p74=74;_p75=75;_p76=76;_p77=77;_p78=78;_p79=79;_p80=80;
        _p81=81;_p82=82;_p83=83;_p84=84;_p85=85;_p86=86;_p87=87;_p88=88;_p89=89;_p90=90;
        _p91=91;_p92=92;_p93=93;_p94=94;_p95=95;_p96=96;_p97=97;_p98=98;_p99=99;_p100=100;

        // Capture the expression trees once so that WithoutInterceptor only measures Analyze().
        _simpleExpr = (u) => $"SELECT {u.Id}, {u.Name} FROM {u}";
        _paramExpr  = (u) => $"SELECT {u.Id}, {u.Name} FROM {u} WHERE {u.Id} = {_userId}";
        _joinExpr   = (u, p) => $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}";

        // Complex query: 4 JOINs, window functions (ROW_NUMBER/DENSE_RANK/NTILE),
        // CTEs, CASE WHEN, COALESCE, NOT EXISTS, BETWEEN, LIKE, NOT IN, 100 parameters.
        _complexExpr = (u, p, o, pr) => $"""
            WITH ranked_orders AS (
                SELECT "Id", "UserId", "ProductId", "Amount",
                       ROW_NUMBER() OVER (PARTITION BY "UserId" ORDER BY "Amount" DESC) AS rn,
                       SUM("Amount") OVER (PARTITION BY "UserId") AS user_total,
                       AVG("Amount") OVER (PARTITION BY "UserId") AS user_avg
                FROM "Order"
                WHERE "Amount" > {_p1}
            ),
            product_stats AS (
                SELECT "ProductId", COUNT(*) AS order_count,
                       SUM("Amount") AS total_amount, AVG("Amount") AS avg_amount
                FROM "Order"
                GROUP BY "ProductId"
                HAVING COUNT(*) > {_p2} AND SUM("Amount") > {_p3}
            )
            SELECT
                {u.Id}, {u.Name},
                {p.Id}, {p.Message},
                {o.Id}, {o.Amount},
                {pr.Id}, {pr.Name}, {pr.Price},
                CASE
                    WHEN {o.Amount} >= {_p4} THEN 'Premium'
                    WHEN {o.Amount} >= {_p5} THEN 'Standard'
                    ELSE 'Basic'
                END AS tier,
                ROW_NUMBER() OVER (PARTITION BY {u.Id} ORDER BY {o.Amount} DESC) AS order_rank,
                DENSE_RANK() OVER (PARTITION BY {p.UserId} ORDER BY {o.Amount} DESC) AS dense_rk,
                NTILE(4) OVER (ORDER BY {o.Amount}) AS quartile,
                COALESCE(ps.total_amount, 0) AS product_total,
                COALESCE(ps.avg_amount, 0) AS product_avg,
                ro.user_total AS user_order_total,
                LEAD({o.Amount}, 1) OVER (PARTITION BY {u.Id} ORDER BY {o.Id}) AS next_amount,
                LAG({o.Amount}, 1) OVER (PARTITION BY {u.Id} ORDER BY {o.Id}) AS prev_amount
            FROM {u}
            JOIN {p} ON {u.Id} = {p.UserId}
            JOIN {o} ON {u.Id} = {o.UserId}
            JOIN {pr} ON {o.ProductId} = {pr.Id}
            JOIN "AuditLog" al ON {u.Id} = al.UserId AND al.Action IN ({_p6},{_p7})
            LEFT JOIN ranked_orders ro ON {u.Id} = ro.UserId AND ro.rn = 1
            LEFT JOIN product_stats ps ON {pr.Id} = ps.ProductId
            WHERE {u.Id} IN ({_p8},{_p9},{_p10},{_p11},{_p12},{_p13},{_p14},{_p15},{_p16},{_p17},{_p18},{_p19},{_p20},{_p21},{_p22},{_p23},{_p24},{_p25},{_p26},{_p27})
              AND {p.UserId} IN ({_p28},{_p29},{_p30},{_p31},{_p32},{_p33},{_p34},{_p35},{_p36},{_p37},{_p38},{_p39},{_p40},{_p41},{_p42},{_p43},{_p44},{_p45},{_p46},{_p47})
              AND {o.Id} IN ({_p48},{_p49},{_p50},{_p51},{_p52},{_p53},{_p54},{_p55},{_p56},{_p57},{_p58},{_p59},{_p60},{_p61},{_p62},{_p63},{_p64},{_p65},{_p66},{_p67})
              AND {pr.Id} IN ({_p68},{_p69},{_p70},{_p71},{_p72},{_p73},{_p74},{_p75},{_p76},{_p77},{_p78},{_p79},{_p80},{_p81},{_p82},{_p83},{_p84},{_p85},{_p86},{_p87})
              AND {pr.Price} BETWEEN {_p88} AND {_p89}
              AND {u.Name} LIKE {_p90}
              AND NOT EXISTS (
                  SELECT 1 FROM "BlockedUser" bu
                  WHERE bu.UserId = {u.Id} AND bu.Reason IN ({_p91},{_p92},{_p93})
              )
              AND {o.Amount} NOT IN ({_p94},{_p95},{_p96},{_p97},{_p98},{_p99},{_p100})
            ORDER BY order_rank, {u.Name}
            """;
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

    // ── Complex query: 4 JOINs + window functions + CTE + 100 parameters ──────

    /// <summary>
    /// Runtime expression-tree analysis for a complex 4-table query with CTEs,
    /// window functions (ROW_NUMBER, DENSE_RANK, NTILE, LEAD, LAG), CASE WHEN,
    /// NOT EXISTS, BETWEEN, LIKE, NOT IN, and 100 parameters.
    /// </summary>
    [Benchmark(Description = "WithoutInterceptor_ComplexQuery")]
    public FormattableString WithoutInterceptor_ComplexQuery()
        => QueryExpressionAnalyzer.Analyze(_complexExpr, SqlDialect.PostgreSql);

    /// <summary>
    /// Compile-time interceptor path for a complex 4-table query with CTEs,
    /// window functions (ROW_NUMBER, DENSE_RANK, NTILE, LEAD, LAG), CASE WHEN,
    /// NOT EXISTS, BETWEEN, LIKE, NOT IN, and 100 runtime parameters.
    /// 4 JOINs (3 typed + 1 raw AuditLog), 100 parameter values extracted at call time.
    /// </summary>
    [Benchmark(Description = "WithInterceptor_ComplexQuery")]
    public FormattableString WithInterceptor_ComplexQuery()
        => _context.Query<User, Post, Order, Product>((u, p, o, pr) => $"""
            WITH ranked_orders AS (
                SELECT "Id", "UserId", "ProductId", "Amount",
                       ROW_NUMBER() OVER (PARTITION BY "UserId" ORDER BY "Amount" DESC) AS rn,
                       SUM("Amount") OVER (PARTITION BY "UserId") AS user_total,
                       AVG("Amount") OVER (PARTITION BY "UserId") AS user_avg
                FROM "Order"
                WHERE "Amount" > {_p1}
            ),
            product_stats AS (
                SELECT "ProductId", COUNT(*) AS order_count,
                       SUM("Amount") AS total_amount, AVG("Amount") AS avg_amount
                FROM "Order"
                GROUP BY "ProductId"
                HAVING COUNT(*) > {_p2} AND SUM("Amount") > {_p3}
            )
            SELECT
                {u.Id}, {u.Name},
                {p.Id}, {p.Message},
                {o.Id}, {o.Amount},
                {pr.Id}, {pr.Name}, {pr.Price},
                CASE
                    WHEN {o.Amount} >= {_p4} THEN 'Premium'
                    WHEN {o.Amount} >= {_p5} THEN 'Standard'
                    ELSE 'Basic'
                END AS tier,
                ROW_NUMBER() OVER (PARTITION BY {u.Id} ORDER BY {o.Amount} DESC) AS order_rank,
                DENSE_RANK() OVER (PARTITION BY {p.UserId} ORDER BY {o.Amount} DESC) AS dense_rk,
                NTILE(4) OVER (ORDER BY {o.Amount}) AS quartile,
                COALESCE(ps.total_amount, 0) AS product_total,
                COALESCE(ps.avg_amount, 0) AS product_avg,
                ro.user_total AS user_order_total,
                LEAD({o.Amount}, 1) OVER (PARTITION BY {u.Id} ORDER BY {o.Id}) AS next_amount,
                LAG({o.Amount}, 1) OVER (PARTITION BY {u.Id} ORDER BY {o.Id}) AS prev_amount
            FROM {u}
            JOIN {p} ON {u.Id} = {p.UserId}
            JOIN {o} ON {u.Id} = {o.UserId}
            JOIN {pr} ON {o.ProductId} = {pr.Id}
            JOIN "AuditLog" al ON {u.Id} = al.UserId AND al.Action IN ({_p6},{_p7})
            LEFT JOIN ranked_orders ro ON {u.Id} = ro.UserId AND ro.rn = 1
            LEFT JOIN product_stats ps ON {pr.Id} = ps.ProductId
            WHERE {u.Id} IN ({_p8},{_p9},{_p10},{_p11},{_p12},{_p13},{_p14},{_p15},{_p16},{_p17},{_p18},{_p19},{_p20},{_p21},{_p22},{_p23},{_p24},{_p25},{_p26},{_p27})
              AND {p.UserId} IN ({_p28},{_p29},{_p30},{_p31},{_p32},{_p33},{_p34},{_p35},{_p36},{_p37},{_p38},{_p39},{_p40},{_p41},{_p42},{_p43},{_p44},{_p45},{_p46},{_p47})
              AND {o.Id} IN ({_p48},{_p49},{_p50},{_p51},{_p52},{_p53},{_p54},{_p55},{_p56},{_p57},{_p58},{_p59},{_p60},{_p61},{_p62},{_p63},{_p64},{_p65},{_p66},{_p67})
              AND {pr.Id} IN ({_p68},{_p69},{_p70},{_p71},{_p72},{_p73},{_p74},{_p75},{_p76},{_p77},{_p78},{_p79},{_p80},{_p81},{_p82},{_p83},{_p84},{_p85},{_p86},{_p87})
              AND {pr.Price} BETWEEN {_p88} AND {_p89}
              AND {u.Name} LIKE {_p90}
              AND NOT EXISTS (
                  SELECT 1 FROM "BlockedUser" bu
                  WHERE bu.UserId = {u.Id} AND bu.Reason IN ({_p91},{_p92},{_p93})
              )
              AND {o.Amount} NOT IN ({_p94},{_p95},{_p96},{_p97},{_p98},{_p99},{_p100})
            ORDER BY order_rank, {u.Name}
            """);
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

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public decimal Amount { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
