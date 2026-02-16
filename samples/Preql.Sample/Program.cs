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

        Console.WriteLine("✅ All examples completed successfully!");
        Console.WriteLine("\n📝 Note: The ToSql() method returns a FormattableString that can be used directly");
        Console.WriteLine("with EF Core's FromInterpolatedSql() method:");
        Console.WriteLine("  var users = context.Users.FromInterpolatedSql(db.ToSql<User>(...));");
        Console.WriteLine("\nThe SQL is generated at BUILD TIME (no runtime reflection) when the source");
        Console.WriteLine("generator is fully enabled. Currently using runtime fallback for demonstration.");
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
}
