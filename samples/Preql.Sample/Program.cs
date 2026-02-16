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

    public QueryResult SearchUsers(string searchTerm, int minAge)
    {
        var query = _db.Query<User>((u) => 
            $"SELECT {u.Id}, {u.Name}, {u.Email} FROM {u} WHERE {u.Name} LIKE {searchTerm} AND {u.Age} >= {minAge} ORDER BY {u.Name}");

        return query;
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
        Console.WriteLine($"Parameters: {query1.Parameters}");
        Console.WriteLine();

        // Example 2: Search users
        Console.WriteLine("Example 2: Search Users");
        var query2 = repository.SearchUsers("%John%", 18);
        Console.WriteLine($"SQL: {query2.Sql}");
        Console.WriteLine($"Parameters: {query2.Parameters}");
        Console.WriteLine();

        // Example 3: Get all users
        Console.WriteLine("Example 3: Get All Users");
        var query3 = repository.GetAllUsers();
        Console.WriteLine($"SQL: {query3.Sql}");
        Console.WriteLine($"Parameters: {query3.Parameters}");
        Console.WriteLine();

        Console.WriteLine("✅ All examples completed successfully!");
        Console.WriteLine("\nNote: The SQL queries were generated at compile-time by the Preql source generator.");
        Console.WriteLine("In a real application, you would pass these queries to Dapper, ADO.NET, or EF Core.");
    }
}
