# 🛡️ Preql

**Preql** (pronounced *Prequel*) is an ultra-high-performance C# library that transmutes typed interpolated strings into raw SQL **at compile-time**.

```csharp
public class UserRepository(IPreqlContext db, IDbConnection conn) 
{
    public async Task<User> GetById(int id) 
    {
        // 1. Write this (Fully typed, IntelliSense supported)
        // Preql automatically distinguishes between Tables {u}, Columns {u.Name} and Variables {id}
        var query = db.Query<User>((u) => 
            $"""SELECT {u.Name} FROM {u} WHERE {u.Id} = {id}""");

        // 2. The Interceptor replaces it at Build-time with a static result:
        // query.Sql -> "SELECT [Name] FROM [Users] WHERE [Id] = @p0"
        
        return await conn.QuerySingleAsync<User>(query.Sql, query.Parameters);
    }
}
```

By leveraging C# Source Generators and Interceptors, Preql eliminates all runtime reflection and expression tree parsing. Your C# code is "baked" into a raw SQL string constant directly in your binary.

## ✨ Key Features

* 🚀 **Zero Runtime Overhead**: No lambda execution at runtime. Zero allocations for SQL generation.
* 🛠️ **Strongly Typed**: Refactor-friendly. If you rename a property in your class, your SQL won't compile.
* 💉 **IoC Ready**: Inject IPreqlContext and switch dialects (Postgres, SQL Server, etc.) per service.
* 🧩 **Agnostic**: Preql only generates SQL. Use it seamlessly with Dapper, ADO.NET, or EF Core.
* 🛡️ **Built-in Security**: Automatically converts C# variables into SQL parameters to prevent injection.

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
            $"""SELECT {u.Id}, {u.Name}, {u.Email} FROM {u} WHERE {u.Id} = {id}""");
        
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
    
    return await conn.QueryAsync<User>(query.Sql, query.Parameters);
}
```

## 🏗️ How It Works

1. **Write your query** using a lambda expression with interpolated strings
2. **Source Generator analyzes** your code at compile-time
3. **Interceptor replaces** the method call with a static QueryResult
4. **Zero runtime cost** - just direct SQL execution

## 📦 Installation

```bash
dotnet add package Preql
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
