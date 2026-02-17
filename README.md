# 🛡️ Preql

**Preql** (pronounced *Prequel*) is a high-performance C# library that transmutes typed interpolated strings into raw SQL.

```csharp
public class UserRepository(IPreqlContext db, IDbConnection conn) 
{
    public async Task<User> GetById(int id) 
    {
        // 1. Write this (Fully typed, IntelliSense supported)
        // Preql automatically distinguishes between Tables {u}, Columns {u.Name} and Variables {id}
        var query = db.Query<User>((u) => 
            $"SELECT {u.Id}, {u.Name}, {u.Email} FROM {u} WHERE {u.Id} = {id}");

        // 2. At runtime, Preql analyzes the expression tree to generate:
        // query.Sql -> "SELECT \"Id\", \"Name\", \"Email\" FROM \"Users\" WHERE \"Id\" = @p0"
        // query.Parameters -> { p0: 42 }
        
        return await conn.QuerySingleAsync<User>(query.Sql, query.Parameters);
    }
}
```

By analyzing C# expression trees, Preql can intelligently distinguish between table references, column references, and parameter values, generating clean, parameterized SQL queries.

## ✨ Key Features

* 🚀 **Clean SQL Generation**: Automatically converts typed queries into parameterized SQL
* 🛠️ **Strongly Typed**: Refactor-friendly. If you rename a property in your class, your query won't compile.
* 💉 **IoC Ready**: Inject IPreqlContext and switch dialects (Postgres, SQL Server, MySQL, SQLite) per service.
* 🧩 **Agnostic**: Preql only generates SQL. Use it seamlessly with Dapper, ADO.NET, or EF Core.
* 🛡️ **Built-in Security**: Automatically converts C# variables into SQL parameters to prevent injection.
* 📦 **Zero Dependencies**: The core library has minimal dependencies for maximum compatibility.

## 🚀 Setup

### 1. Register in Program.cs

Define your database dialect globally or specifically for different contexts.

```csharp
// Standard setup
builder.Services.AddPreql(SqlDialect.PostgreSql);

// Multi-database setup
builder.Services.AddPreql<PostgresContext>(SqlDialect.PostgreSql);
builder.Services.AddPreql<SqlServerContext>(SqlDialect.SqlServer);
```

## 📚 Usage Examples

### Basic Query

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class UserRepository(IPreqlContext db, IDbConnection conn)
{
    public async Task<User> GetById(int id)
    {
        var query = db.Query<User>((u) => 
            $"SELECT {u.Id}, {u.Name}, {u.Email} FROM {u} WHERE {u.Id} = {id}");
        
        // Generated SQL: SELECT "Id", "Name", "Email" FROM "Users" WHERE "Id" = @p0
        // Parameters: { p0: 42 }
        
        return await conn.QuerySingleAsync<User>(query.Sql, query.Parameters);
    }
}
```

### Complex Queries

```csharp
public async Task<IEnumerable<User>> SearchUsers(string searchTerm, int minAge)
{
    var query = db.Query<User>((u) => 
        $"""
        SELECT {u.Id}, {u.Name}, {u.Email} 
        FROM {u} 
        WHERE {u.Name} LIKE {searchTerm} 
        AND {u.Age} >= {minAge}
        ORDER BY {u.Name}
        """);
    
    // Generated SQL: SELECT "Id", "Name", "Email" FROM "Users" 
    //                WHERE "Name" LIKE @p0 AND "Age" >= @p1 ORDER BY "Name"
    // Parameters: { p0: "%John%", p1: 18 }
    
    return await conn.QueryAsync<User>(query.Sql, query.Parameters);
}
```

### SQL Dialect Support

```csharp
// PostgreSQL: Uses double quotes for identifiers
var pgContext = new PreqlContext(SqlDialect.PostgreSql);
// Generated: SELECT "Name" FROM "Users"

// SQL Server: Uses square brackets for identifiers  
var sqlContext = new PreqlContext(SqlDialect.SqlServer);
// Generated: SELECT [Name] FROM [Users]

// MySQL: Uses backticks for identifiers
var mysqlContext = new PreqlContext(SqlDialect.MySql);
// Generated: SELECT `Name` FROM `Users`

// SQLite: Uses double quotes for identifiers
var sqliteContext = new PreqlContext(SqlDialect.Sqlite);
// Generated: SELECT "Name" FROM "Users"
```

## 🏗️ How It Works

1. **Write your query** using a lambda expression with interpolated strings
2. **Preql analyzes** the expression tree to identify:
   - Table references (parameter itself: `{u}`)
   - Column references (member access: `{u.Name}`)
   - Parameter values (variables: `{id}`)
3. **SQL is generated** with proper identifier quoting and parameter placeholders
4. **Parameters are extracted** into a dictionary for safe execution

## 🔮 Future Enhancements

The current implementation uses runtime expression tree analysis. A future version could leverage C# 12 Source Generators and Interceptors to perform this analysis at compile-time, eliminating all runtime overhead and providing:
- Zero runtime cost - SQL generation happens at build time
- Static SQL strings directly in your binary
- Compile-time validation of queries

## 📦 Installation

```bash
dotnet add package Preql
```

## 🎯 Running the Sample

```bash
cd samples/Preql.Sample
dotnet run
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
