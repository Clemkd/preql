using Preql;
using Preql.Sample.Generated;

namespace Preql.Sample;

// Example entity class
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🛡️ Preql Sample Application");
        Console.WriteLine("=============================\n");

        // Create a PreqlContext with PostgreSQL dialect
        var context = new PreqlContext(SqlDialect.PostgreSql);

        // Example 1: Simple Query with Lambda - Using Generated Proxy
        Console.WriteLine("Example 1: Simple Query with Lambda");
        int userId = 123;
        
        // The source generator would transform this at compile time:
        // context.Query<User>((u) => ...) 
        // Into code using the generated UserProxy
        var userProxy = new UserProxy(context.Dialect);
        PreqlSqlHandler h1 = $"SELECT {userProxy.Id}, {userProxy.Name}, {userProxy.Email} FROM {userProxy} WHERE {userProxy.Id} = {userId}";
        var (sql1, params1) = h1.Build();
        
        Console.WriteLine($"SQL: {sql1}");
        Console.WriteLine($"Parameters: {FormatParamList(params1)}");
        Console.WriteLine();

        // Example 2: Complex Query with Multiple Conditions
        Console.WriteLine("Example 2: Complex Query with Multiple Conditions");
        string searchName = "%Smith%";
        int minAge = 30;
        
        var u2 = new UserProxy(context.Dialect);
        PreqlSqlHandler h2 = $"""
            SELECT {u2.Id}, {u2.Name}, {u2.Email}, {u2.Age}
            FROM {u2}
            WHERE {u2.Name} LIKE {searchName}
            AND {u2.Age} >= {minAge}
            ORDER BY {u2.Name}
            """;
        var (sql2, params2) = h2.Build();
        
        Console.WriteLine($"SQL: {sql2}");
        Console.WriteLine($"Parameters: {FormatParamList(params2)}");
        Console.WriteLine();

        // Example 3: Select All
        Console.WriteLine("Example 3: Select All");
        
        var u3 = new UserProxy(context.Dialect);
        PreqlSqlHandler h3 = $"SELECT {u3.Id}, {u3.Name}, {u3.Email}, {u3.Age} FROM {u3}";
        var (sql3, params3) = h3.Build();
        
        Console.WriteLine($"SQL: {sql3}");
        Console.WriteLine($"Parameters: {FormatParamList(params3)}");
        Console.WriteLine();

        // Example 4: Query with Aggregation
        Console.WriteLine("Example 4: Query with Aggregation");
        
        var u4 = new UserProxy(context.Dialect);
        PreqlSqlHandler h4 = $"""
            SELECT {u4.Id}, {u4.Name}, COUNT(*) as Count
            FROM {u4}
            WHERE {u4.Age} >= {25}
            GROUP BY {u4.Id}, {u4.Name}
            HAVING COUNT(*) > {5}
            """;
        var (sql4, params4) = h4.Build();
        
        Console.WriteLine($"SQL: {sql4}");
        Console.WriteLine($"Parameters: {FormatParamList(params4)}");
        Console.WriteLine();

        // Example 5: Update Statement
        Console.WriteLine("Example 5: Update Statement");
        string newEmail = "newemail@example.com";
        int targetUserId = 42;
        
        var u5 = new UserProxy(context.Dialect);
        PreqlSqlHandler h5 = $"UPDATE {u5} SET {u5.Email} = {newEmail} WHERE {u5.Id} = {targetUserId}";
        var (sql5, params5) = h5.Build();
        
        Console.WriteLine($"SQL: {sql5}");
        Console.WriteLine($"Parameters: {FormatParamList(params5)}");
        Console.WriteLine();


        Console.WriteLine("✅ All examples completed successfully!");
        Console.WriteLine("\n📝 Key Features:");
        Console.WriteLine("  • Lambda expression API: context.Query<User>((u) => $\"...\")");
        Console.WriteLine("  • No need to call .AsValue() - values automatically parameterized");
        Console.WriteLine("  • Type-safe property access (u.Id, u.Name, etc.)");
        Console.WriteLine("  • Generated proxies provide compile-time safety");
        Console.WriteLine("  • Works with any SQL statement");
        Console.WriteLine("\n🎯 Usage (what source generator creates):");
        Console.WriteLine("  var u = new UserProxy(dialect);");
        Console.WriteLine("  PreqlSqlHandler h = $\"SELECT {u.Id}, {u.Name} FROM {u} WHERE {u.Id} = {userId}\";");
        Console.WriteLine("  var (sql, parameters) = h.Build();");
    }

    static string FormatParamList(IReadOnlyList<object?> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return "none";

        return string.Join(", ", parameters.Select((p, i) => $"@p{i}={p}"));
    }
}
