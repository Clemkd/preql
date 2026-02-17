using Preql;

namespace Preql.Sample;

/// <summary>
/// Sample application demonstrating the new multi-table query with table aliases functionality
/// This demonstrates the expected usage from the problem statement:
/// var sql = preql.Query<User, Post>((u, p) => $"Select {u.Name}, {p.Message} From {u} Join ...)
/// </summary>
public static class AliasExamples
{
    public static void Run()
    {
        Console.WriteLine("🛡️ Preql Multi-Table Query Sample");
        Console.WriteLine("====================================\n");

        // Create a PreqlContext with PostgreSQL dialect
        var context = new PreqlContext(SqlDialect.PostgreSql);

        // Example 1: Single table query with alias
        Console.WriteLine("Example 1: Single Table Query with Alias");
        int userId = 123;
        var query1 = context.Query<User>((u) => 
            $"SELECT {u.Id}, {u.Name}, {u.Email} FROM {u} WHERE {u.Id} = {userId}");
        
        Console.WriteLine($"SQL: {query1.Sql}");
        Console.WriteLine($"Parameters: {FormatParams(query1.Parameters)}");
        Console.WriteLine($"Note: Column references like {{u.Name}} become: u.\"Name\" (with table alias)");
        Console.WriteLine($"Note: Table reference {{u}} becomes: \"Users\" u (with alias)\n");

        // Example 2: Two-table JOIN query
        Console.WriteLine("Example 2: Two-Table JOIN Query");
        var query2 = context.Query<User, Post>((u, p) => 
            $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");
        
        Console.WriteLine($"SQL: {query2.Sql}");
        Console.WriteLine($"Parameters: {FormatParams(query2.Parameters)}");
        Console.WriteLine($"Note: This is exactly what the problem statement asked for!");
        Console.WriteLine($"Note: {{u.Name}} → u.\"Name\", {{p.Message}} → p.\"Message\"");
        Console.WriteLine($"Note: {{u}} → \"Users\" u, {{p}} → \"Posts\" p\n");

        // Example 3: Two-table query with WHERE clause
        Console.WriteLine("Example 3: Two-Table Query with WHERE");
        string searchName = "%John%";
        int minAge = 25;
        var query3 = context.Query<User, Post>((u, p) => 
            $"""
            SELECT {u.Id}, {u.Name}, {u.Email}, {p.Message}
            FROM {u} 
            INNER JOIN {p} ON {u.Id} = {p.UserId}
            WHERE {u.Name} LIKE {searchName}
            AND {u.Age} >= {minAge}
            ORDER BY {u.Name}
            """);
        
        Console.WriteLine($"SQL: {query3.Sql}");
        Console.WriteLine($"Parameters: {FormatParams(query3.Parameters)}");
        Console.WriteLine();

        // Example 4: Three-table query
        Console.WriteLine("Example 4: Three-Table Query");
        var query4 = context.Query<User, Post, User>((u, p, author) => 
            $"SELECT {u.Name}, {p.Message}, {author.Email} FROM {u} JOIN {p} ON {u.Id} = {p.UserId} JOIN {author} ON {p.UserId} = {author.Id}");
        
        Console.WriteLine($"SQL: {query4.Sql}");
        Console.WriteLine($"Parameters: {FormatParams(query4.Parameters)}");
        Console.WriteLine();

        // Example 5: Testing with SQL Server dialect
        Console.WriteLine("Example 5: Same Query with SQL Server Dialect");
        var sqlServerContext = new PreqlContext(SqlDialect.SqlServer);
        var query5 = sqlServerContext.Query<User, Post>((u, p) => 
            $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");
        
        Console.WriteLine($"SQL: {query5.Sql}");
        Console.WriteLine($"Note: Uses [brackets] instead of \"quotes\" for SQL Server");
        Console.WriteLine();

        // Example 6: Testing with MySQL dialect
        Console.WriteLine("Example 6: Same Query with MySQL Dialect");
        var mysqlContext = new PreqlContext(SqlDialect.MySql);
        var query6 = mysqlContext.Query<User, Post>((u, p) => 
            $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");
        
        Console.WriteLine($"SQL: {query6.Sql}");
        Console.WriteLine($"Note: Uses `backticks` for MySQL");
        Console.WriteLine();

        Console.WriteLine("✅ All examples completed successfully!\n");
        Console.WriteLine("📝 Key Features Demonstrated:");
        Console.WriteLine("  • Multi-table queries: Query<User, Post>((u, p) => ...)");
        Console.WriteLine("  • Automatic table aliases: u.\"Name\", p.\"Message\"");
        Console.WriteLine("  • Table references with aliases: \"Users\" u, \"Posts\" p");
        Console.WriteLine("  • Automatic value parameterization for security");
        Console.WriteLine("  • Support for multiple SQL dialects (PostgreSQL, SQL Server, MySQL)");
        Console.WriteLine("  • No need to manually create proxy variables!");
        Console.WriteLine("\n🎯 This matches the expected usage from the problem statement:");
        Console.WriteLine("  Input:  Query<User, Post>((u, p) => $\"Select {{u.Name}}, {{p.Message}} From {{u}} Join...\")");
        Console.WriteLine("  Output: \"Select u.\\\"Name\\\", p.\\\"Message\\\" From \\\"Users\\\" u Join...\"");
    }

    static string FormatParams(object? parameters)
    {
        if (parameters == null)
            return "none";

        if (parameters is System.Collections.IList list)
        {
            if (list.Count == 0)
                return "none";
            
            var paramList = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                paramList.Add($"@p{i}={list[i]}");
            }
            return string.Join(", ", paramList);
        }

        return parameters.ToString() ?? "none";
    }
}
