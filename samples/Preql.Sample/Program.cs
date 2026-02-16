using Preql;

namespace Preql.Sample;

// Example entity class
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

// Example repository using Preql
public class UserRepository
{
    private readonly IPreqlContext _db;

    public UserRepository(IPreqlContext db)
    {
        _db = db;
    }

    public QueryResult GetById(int id)
    {
        // Preql automatically distinguishes between Tables {u}, Columns {u.Name} and Variables {id}
        var query = _db.Query<User>((u) => 
            $"SELECT {u.Id}, {u.Name}, {u.Email} FROM {u} WHERE {u.Id} = {id}");

        return query;
    }

    // New method using ToSql that returns FormattableString (compatible with EF Core)
    public FormattableString GetByIdSql(int id)
    {
        // This returns a FormattableString that can be used with EF Core's FromInterpolatedSql
        var sql = _db.ToSql<User>((u) => 
            $"SELECT {u.Id}, {u.Name}, {u.Email} FROM {u} WHERE {u.Id} = {id}");

        return sql;
    }

    public QueryResult SearchUsers(string searchTerm, int minAge)
    {
        var query = _db.Query<User>((u) => 
            $"SELECT {u.Id}, {u.Name}, {u.Email} FROM {u} WHERE {u.Name} LIKE {searchTerm} AND {u.Age} >= {minAge} ORDER BY {u.Name}");

        return query;
    }

    // New method using ToSql
    public FormattableString SearchUsersSql(string searchTerm, int minAge)
    {
        var sql = _db.ToSql<User>((u) => 
            $"SELECT {u.Id}, {u.Name}, {u.Email} FROM {u} WHERE {u.Name} LIKE {searchTerm} AND {u.Age} >= {minAge} ORDER BY {u.Name}");

        return sql;
    }

    public QueryResult GetAllUsers()
    {
        var query = _db.Query<User>((u) =>
            $"SELECT {u.Id}, {u.Name}, {u.Email}, {u.Age} FROM {u}");

        return query;
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🛡️ Preql Sample Application");
        Console.WriteLine("=============================\n");

        // Create a PreqlContext with PostgreSQL dialect
        var context = new PreqlContext(SqlDialect.PostgreSql);
        var repository = new UserRepository(context);

        // Example 1: Get user by ID
        Console.WriteLine("Example 1: Get User By ID");
        var query1 = repository.GetById(42);
        Console.WriteLine($"SQL: {query1.Sql}");
        Console.WriteLine($"Parameters: {FormatParameters(query1.Parameters)}");
        Console.WriteLine();

        // Example 2: Search users
        Console.WriteLine("Example 2: Search Users");
        var query2 = repository.SearchUsers("%John%", 18);
        Console.WriteLine($"SQL: {query2.Sql}");
        Console.WriteLine($"Parameters: {FormatParameters(query2.Parameters)}");
        Console.WriteLine();

        // Example 3: Get all users
        Console.WriteLine("Example 3: Get All Users");
        var query3 = repository.GetAllUsers();
        Console.WriteLine($"SQL: {query3.Sql}");
        Console.WriteLine($"Parameters: {FormatParameters(query3.Parameters)}");
        Console.WriteLine();

        // NEW: Examples using ToSql (returns FormattableString compatible with EF Core)
        Console.WriteLine("=============================");
        Console.WriteLine("NEW API: ToSql() - Returns FormattableString");
        Console.WriteLine("(Compatible with EF Core's FromInterpolatedSql)");
        Console.WriteLine("=============================\n");

        Console.WriteLine("Example 4: Get User By ID with ToSql");
        var sql1 = repository.GetByIdSql(42);
        Console.WriteLine($"FormattableString Format: {sql1.Format}");
        Console.WriteLine($"Arguments: {FormatFormattableStringArgs(sql1)}");
        Console.WriteLine($"Rendered SQL: {string.Format(sql1.Format, sql1.GetArguments())}");
        Console.WriteLine();

        Console.WriteLine("Example 5: Search Users with ToSql");
        var sql2 = repository.SearchUsersSql("%Jane%", 25);
        Console.WriteLine($"FormattableString Format: {sql2.Format}");
        Console.WriteLine($"Arguments: {FormatFormattableStringArgs(sql2)}");
        Console.WriteLine($"Rendered SQL: {string.Format(sql2.Format, sql2.GetArguments())}");
        Console.WriteLine();

        // NEW: Examples using InterpolatedStringHandler with Proxy Types (ZERO REFLECTION!)
        Console.WriteLine("=============================");
        Console.WriteLine("BEST API: InterpolatedStringHandler + Proxy Types");
        Console.WriteLine("(ZERO RUNTIME REFLECTION - Pure Build-Time!)");
        Console.WriteLine("=============================\n");

        Console.WriteLine("Example 6: Using SqlTableAlias with Handler");
        var u = context.Alias<User>();
        int userId = 123;
        
        // This uses the PreqlSqlHandler automatically!
        PreqlSqlHandler handler = $"SELECT {u["Id"]}, {u["Name"]}, {u["Email"]} FROM {u} WHERE {u["Id"]} = {userId.AsValue()}";
        var (handlerSql, handlerParams) = handler.Build();
        
        Console.WriteLine($"SQL: {handlerSql}");
        Console.WriteLine($"Parameters: {FormatParamList(handlerParams)}");
        Console.WriteLine();

        Console.WriteLine("Example 7: Complex Query with Handler");
        string searchName = "%Smith%";
        int minAge = 30;
        
        PreqlSqlHandler handler2 = $"""
            SELECT {u["Id"]}, {u["Name"]}, {u["Email"]}, {u["Age"]}
            FROM {u}
            WHERE {u["Name"]} LIKE {searchName.AsValue()}
            AND {u["Age"]} >= {minAge.AsValue()}
            ORDER BY {u["Name"]}
            """;
        var (complexSql, complexParams) = handler2.Build();
        
        Console.WriteLine($"SQL: {complexSql}");
        Console.WriteLine($"Parameters: {FormatParamList(complexParams)}");
        Console.WriteLine();

        Console.WriteLine("Example 8: Building FormattableString for EF Core");
        PreqlSqlHandler handler3 = $"SELECT {u["Id"]}, {u["Name"]} FROM {u} WHERE {u["Id"]} = {userId.AsValue()}";
        var efCoreCompatible = handler3.BuildFormattable();
        
        Console.WriteLine($"FormattableString Format: {efCoreCompatible.Format}");
        Console.WriteLine($"Arguments: {FormatFormattableStringArgs(efCoreCompatible)}");
        Console.WriteLine($"Can be used with: context.Users.FromInterpolatedSql(...)");
        Console.WriteLine();

        // NEW: Example using GENERATED AliasProxy (what source generator would create)
        Console.WriteLine("=============================");
        Console.WriteLine("ULTIMATE: Generated AliasProxy (Source Generator)");
        Console.WriteLine("(NO REFLECTION - Typed Properties Generated at Build Time!)");
        Console.WriteLine("=============================\n");

        Console.WriteLine("Example 9: Using Generated UserAliasProxy");
        var userProxy = new Preql.Sample.Generated.UserAliasProxy(context.Dialect);
        int searchId = 999;
        
        // Note: With real source generator, Query<User>((u) => ...) would be automatically
        // transformed to use this generated proxy instead of runtime expression analysis!
        PreqlSqlHandler handler4 = $"SELECT {userProxy.Id}, {userProxy.Name}, {userProxy.Email} FROM {userProxy} WHERE {userProxy.Id} = {searchId.AsValue()}";
        var (proxySql, proxyParams) = handler4.Build();
        
        Console.WriteLine($"SQL: {proxySql}");
        Console.WriteLine($"Parameters: {FormatParamList(proxyParams)}");
        Console.WriteLine();
        Console.WriteLine("✨ Notice: Properties like 'userProxy.Id' instead of u[\"Id\"]!");
        Console.WriteLine("✨ This provides full IntelliSense and compile-time safety!");
        Console.WriteLine("✨ Generated by source generator - NO runtime reflection!");
        Console.WriteLine();

        Console.WriteLine("✅ All examples completed successfully!");
        Console.WriteLine("\n📝 Note: The InterpolatedStringHandler approach (Examples 6-9) is the BEST option:");
        Console.WriteLine("  • ZERO runtime reflection");
        Console.WriteLine("  • ZERO runtime overhead");  
        Console.WriteLine("  • All work done by the C# compiler at build time");
        Console.WriteLine("  • Type-safe column and table references");
        Console.WriteLine("  • Compatible with EF Core via BuildFormattable()");
        Console.WriteLine("\n🎯 FUTURE: With full source generator implementation:");
        Console.WriteLine("  db.Query<User>((u) => $\"SELECT {u.Id} FROM {u}\")");
        Console.WriteLine("  ↓ transformed at build time to ↓");
        Console.WriteLine("  var u = new UserAliasProxy(dialect);");
        Console.WriteLine("  PreqlSqlHandler h = $\"SELECT {u.Id} FROM {u}\";");
        Console.WriteLine("\nUsage with EF Core:");
        Console.WriteLine("  var u = new UserAliasProxy(dialect);");
        Console.WriteLine("  PreqlSqlHandler h = $\"SELECT {u.Id} FROM {u} WHERE {u.Id} = {id.AsValue()}\";");
        Console.WriteLine("  var users = context.Users.FromInterpolatedSql(h.BuildFormattable());");
    }

    static string FormatParameters(object? parameters)
    {
        if (parameters == null)
            return "none";

        if (parameters is Dictionary<string, object?> dict)
        {
            if (dict.Count == 0)
                return "none";

            var items = dict.Select(kvp => $"{kvp.Key}={kvp.Value}");
            return string.Join(", ", items);
        }

        return parameters.ToString() ?? "unknown";
    }

    static string FormatFormattableStringArgs(FormattableString fs)
    {
        var args = fs.GetArguments();
        if (args.Length == 0)
            return "none";

        return string.Join(", ", args.Select((arg, i) => $"[{i}]={arg}"));
    }

    static string FormatParamList(IReadOnlyList<object?> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return "none";

        return string.Join(", ", parameters.Select((p, i) => $"@p{i}={p}"));
    }
}
