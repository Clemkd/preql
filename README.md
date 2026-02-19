# 🛡️ Preql

[![CI](https://github.com/Clemkd/preql/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Clemkd/preql/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Clemkd/preql/branch/main/graph/badge.svg)](https://codecov.io/gh/Clemkd/preql)

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

        // 2. Preql generates (at compile time via source generator, or at runtime as fallback):
        // query.Sql -> "SELECT u.\"Id\", u.\"Name\", u.\"Email\" FROM \"Users\" u WHERE u.\"Id\" = @p0"
        // query.Parameters -> [@p0=42]
        
        return await conn.QuerySingleAsync<User>(query.Sql, query.Parameters);
    }
}
```

By analyzing C# expression trees, Preql can intelligently distinguish between table references, column references, and parameter values, generating clean, parameterized SQL queries with automatic table aliases.

## ✨ Key Features

* 🚀 **Clean SQL Generation**: Automatically converts typed queries into parameterized SQL
* 🛠️ **Strongly Typed**: Refactor-friendly. If you rename a property in your class, your query won't compile.
* 💉 **IoC Ready**: Inject IPreqlContext and switch dialects (Postgres, SQL Server, MySQL, SQLite) per service.
* 🧩 **Agnostic**: Preql only generates SQL. Use it seamlessly with Dapper, ADO.NET, or EF Core.
* 🛡️ **Built-in Security**: Automatically converts C# variables into SQL parameters to prevent injection.
* 📦 **Zero Dependencies**: The core library has minimal dependencies for maximum compatibility.
* 🏷️ **Custom Naming**: Override table and column names with `[Table]` and `[Column]` attributes.

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
        
        // Generated SQL: SELECT u."Id", u."Name", u."Email" FROM "Users" u WHERE u."Id" = @p0
        // Parameters: [@p0=42]
        
        return await conn.QuerySingleAsync<User>(query.Sql, query.Parameters);
    }
}
```

### Multi-Table Queries with Aliases

Preql supports multi-table queries with automatic table aliases:

```csharp
public async Task<IEnumerable<UserPost>> GetUserPosts(string searchTerm)
{
    // Multi-table query with automatic aliases
    var query = db.Query<User, Post>((u, p) => 
        $"""
        SELECT {u.Id}, {u.Name}, {p.Message}
        FROM {u}
        JOIN {p} ON {u.Id} = {p.UserId}
        WHERE {u.Name} LIKE {searchTerm}
        """);
    
    // Generated SQL (PostgreSQL): 
    // SELECT u."Id", u."Name", p."Message"
    // FROM "Users" u
    // JOIN "Posts" p ON u."Id" = p."UserId"
    // WHERE u."Name" LIKE @p0
    
    // Parameters: { p0: "%John%" }
    
    return await conn.QueryAsync<UserPost>(query.Sql, query.Parameters);
}
```

**Key Features:**
- Column references automatically include table aliases: `{u.Name}` → `u."Name"`
- Table references include aliases: `{u}` → `"Users" u`
- Supports 2-5 tables in a single query
- Works with JOINs, subqueries, and complex SQL

### SQL Dialect Support

```csharp
// PostgreSQL: Uses double quotes for identifiers
var pgContext = new PreqlContext(SqlDialect.PostgreSql);
var q = pgContext.Query<User, Post>((u, p) => $"SELECT {u.Name} FROM {u} JOIN {p}...");
// Generated: SELECT u."Name" FROM "Users" u JOIN "Posts" p ...

// SQL Server: Uses square brackets for identifiers
var sqlContext = new PreqlContext(SqlDialect.SqlServer);
// Generated: SELECT u.[Name] FROM [Users] u JOIN [Posts] p ...

// MySQL: Uses backticks for identifiers
var mysqlContext = new PreqlContext(SqlDialect.MySql);
// Generated: SELECT u.`Name` FROM `Users` u JOIN `Posts` p ...

// SQLite: Uses double quotes for identifiers
var sqliteContext = new PreqlContext(SqlDialect.Sqlite);
// Generated: SELECT u."Name" FROM "Users" u JOIN "Posts" p ...
```

### Attribute Customization

#### Custom Table Names

Apply `[Table("...")]` to map an entity to a specific database table name instead of relying on automatic pluralization:

```csharp
[Table("tbl_posts")]
public class Post
{
    public int Id { get; set; }
    public string Message { get; set; }
    public int UserId { get; set; }
}

// {p} now resolves to "tbl_posts" p instead of "Posts" p
var query = db.Query<User, Post>((u, p) =>
    $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");
// Generated (PostgreSQL): SELECT u."Name", p."Message" FROM "Users" u JOIN "tbl_posts" p ON u."Id" = p."UserId"
```

#### Custom Column Names

Apply `[Column("...")]` to a property to use a specific SQL column name instead of the property name:

```csharp
public class User
{
    [Column("user_id")]
    public int Id { get; set; }

    [Column("full_name")]
    public string Name { get; set; }
}

var query = db.Query<User>((u) => $"SELECT {u.Id}, {u.Name} FROM {u}");
// Generated (PostgreSQL): SELECT u."user_id", u."full_name" FROM "Users" u
```

## 🏗️ How It Works

### Runtime fallback (always available)
Preql analyzes the lambda expression tree at runtime on each call to identify table references, column references, and parameter values.

### Compile-time generation (recommended — zero analysis overhead)
When the `Preql.SourceGenerator` is referenced as an analyzer in your project, Preql intercepts every `context.Query<…>(lambda)` call **at compile time** using C# source generators + interceptors:

1. **At compile time** — the source generator parses the interpolated-string lambda from the syntax tree, classifies each `{…}` hole as a table reference, column reference, or runtime parameter, and emits an interceptor method in `Preql.Generated` containing the SQL structure as pre-built string-concat operations.

2. **At runtime** — only two cheap things happen:
   - Dialect-specific quoting is applied to pre-known identifiers (simple `string.Concat`).
   - Only the parameter-value expressions (e.g. `{userId}`) are compiled/evaluated — no expression-tree walking of the SQL structure.

```
Developer writes:
  context.Query<User, Post>((u, p) =>
      $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}")

Source generator emits (PreqlInterceptor_XXXX.g.cs):
  [InterceptsLocation("ProgramWithAliases.cs", 33, 27)]
  public static QueryResult QueryXXXX<T1, T2>(this IPreqlContext context, ...)
  {
      var __d = context.Dialect;
      var __sql = string.Concat(
          "SELECT ",
          SqlIdentifierHelper.Col(__d, "u", "Name"),   // ← compile-time knowledge
          ", ",
          SqlIdentifierHelper.Col(__d, "p", "Message"),
          " FROM ",
          SqlIdentifierHelper.Table(__d, "Users", "u"),
          " JOIN ",
          SqlIdentifierHelper.Table(__d, "Posts", "p"),
          ...
      );
      return new QueryResult(__sql, Array.Empty<object?>());
  }
```

### Table Alias Generation

When you write:
```csharp
db.Query<User, Post>((u, p) => $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p}...")
```

Preql automatically generates:
- `{u.Name}` → `u."Name"` (column with table alias)
- `{p.Message}` → `p."Message"` (column with table alias)
- `{u}` in FROM → `"Users" u` (table with alias)
- `{p}` in JOIN → `"Posts" p` (table with alias)

## 🔮 Future Enhancements

- **Stable interceptor form**: Migrate from the file-path `[InterceptsLocation(string, int, int)]` form (experimental) to the stable `InterceptableLocation`-based form once it is broadly available in NuGet releases of the Roslyn SDK.
- **Caching for parameter extractors**: Cache compiled parameter-extraction delegates per call-site to eliminate repeated `Expression.Compile()` overhead.

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
