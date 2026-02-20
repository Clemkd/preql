# 🛡️ Preql

[![CI](https://github.com/Clemkd/preql/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Clemkd/preql/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Clemkd/preql/branch/main/graph/badge.svg)](https://codecov.io/gh/Clemkd/preql)

**Preql** (pronounced *Prequel*) is a high-performance C# library that transmutes typed interpolated strings into raw SQL.

By analyzing C# expression trees, Preql can intelligently distinguish between table references, column references, and parameter values, generating clean, parameterized SQL queries with automatic table aliases. This SQL generation happens naturally at compile time of your application.

## ✨ Key Features

* 🚀 **Clean SQL Generation**: Automatically converts typed queries into parameterized SQL
* 🛠️ **Strongly Typed**: Refactor-friendly. If you rename a property in your class, your query won't compile.
* 💉 **IoC Ready**: Inject IPreqlContext and switch dialects (Postgres, SQL Server, MySQL, SQLite) per service.
* 🧩 **Agnostic**: Preql only generates SQL. Use it seamlessly with Dapper, ADO.NET, or EF Core.
* 🛡️ **Built-in Security**: Automatically converts C# variables into SQL parameters to prevent injection.
* 📦 **Zero Dependencies**: The core library has minimal dependencies for maximum compatibility.
* 🏷️ **Custom Naming**: Override table and column names with `[Table]` and `[Column]` attributes.
* ⚡ **Zero-overhead hot path**: With the source generator, simple queries return a pre-built `FormattableString` in a single array-index lookup — no allocations after the first call per dialect.

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
        
        // query.Format    -> SELECT u."Id", u."Name", u."Email" FROM "User" u WHERE u."Id" = {0}
        // query.GetArguments() -> [42]
        
        return await conn.QuerySingleAsync<User>(query.Format, query.GetArguments());
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
    
    // query.Format (PostgreSQL): 
    // SELECT u."Id", u."Name", p."Message"
    // FROM "User" u
    // JOIN "Post" p ON u."Id" = p."UserId"
    // WHERE u."Name" LIKE {0}
    
    // query.GetArguments(): ["%John%"]
    
    return await conn.QueryAsync<UserPost>(query.Format, query.GetArguments());
}
```

**Key Features:**
- Column references automatically include table aliases: `{u.Name}` → `u."Name"`
- Table references include aliases: `{u}` → `"User" u`
- Supports 2-5 tables in a single query
- Works with JOINs, subqueries, and complex SQL

### SQL Dialect Support

```csharp
// PostgreSQL: Uses double quotes for identifiers
var pgContext = new PreqlContext(SqlDialect.PostgreSql);
var q = pgContext.Query<User, Post>((u, p) => $"SELECT {u.Name} FROM {u} JOIN {p}...");
// Generated: SELECT u."Name" FROM "User" u JOIN "Post" p ...

// SQL Server: Uses square brackets for identifiers
var sqlContext = new PreqlContext(SqlDialect.SqlServer);
// Generated: SELECT u.[Name] FROM [User] u JOIN [Post] p ...

// MySQL: Uses backticks for identifiers
var mysqlContext = new PreqlContext(SqlDialect.MySql);
// Generated: SELECT u.`Name` FROM `User` u JOIN `Post` p ...

// SQLite: Uses double quotes for identifiers
var sqliteContext = new PreqlContext(SqlDialect.Sqlite);
// Generated: SELECT u."Name" FROM "User" u JOIN "Post" p ...
```

### Attribute Customization

#### Custom Table Names

Apply `[Table("...")]` to map an entity to a specific database table name instead of the default (the class name, used as-is):

```csharp
[Table("tbl_posts")]
public class Post
{
    public int Id { get; set; }
    public string Message { get; set; }
    public int UserId { get; set; }
}

// {p} now resolves to "tbl_posts" p instead of "Post" p
var query = db.Query<User, Post>((u, p) =>
    $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p} ON {u.Id} = {p.UserId}");
// Generated (PostgreSQL): SELECT u."Name", p."Message" FROM "User" u JOIN "tbl_posts" p ON u."Id" = p."UserId"
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
// Generated (PostgreSQL): SELECT u."user_id", u."full_name" FROM "User" u
```

## 🏗️ How It Works

### Runtime fallback (always available)
Preql analyzes the lambda expression tree at runtime on each call to identify table references, column references, and parameter values.

### Compile-time generation (recommended — zero analysis overhead)
When the `Preql.SourceGenerator` is referenced as an analyzer in your project, Preql intercepts every `context.Query<…>(lambda)` call **at compile time** using C# source generators + interceptors:

```
Developer Code:
  context.Query<User>((u) => $"SELECT {u.Id} FROM {u} WHERE {u.Id} = {userId}")
         ↓
Compile-Time Source Generator:
  1. Detects .Query<T>() invocation via syntax provider
  2. Analyzes semantic model (entity types, table names, [Table]/[Column] attributes)
  3. Extracts interpolated string from lambda body
  4. Classifies holes: {u}→Table, {u.Id}→Column, {userId}→Param
  5. Pre-computes the full SQL format string for every dialect as a string literal
  6. Emits PreqlInterceptor_XXXX.g.cs with [InterceptsLocation], replacing the original call
         ↓
Generated Interceptor (Runtime):
  - No SQL building at all — the complete SQL is already a string literal for every dialect
  - Pure array-index lookup returns the pre-built FormattableString (no-param queries)
  - For queries with parameters: array lookup + fast parameter extraction
         ↓
Result:
  query.Format: "SELECT u.\"Id\" FROM \"User\" u WHERE u.\"Id\" = {0}"
  query.GetArguments(): [userId_value]
```

The generated interceptor looks like this:

```csharp
// PreqlInterceptor_XXXX.g.cs  (auto-generated — do not edit)
// SQL structure and dialect quoting fully determined at compile time.
// At runtime: only parameter value extraction + FormattableString creation.

file static class PreqlInterceptor_XXXX
{
    // All dialect variants pre-computed at compile time as string literals:
    private static readonly string[] __formats =
    {
        @"SELECT u.""Id"" FROM ""User"" u WHERE u.""Id"" = {0}",  // PostgreSql
        @"SELECT u.[Id] FROM [User] u WHERE u.[Id] = {0}",          // SqlServer
        @"SELECT u.`Id` FROM `User` u WHERE u.`Id` = {0}",           // MySql
        @"SELECT u.""Id"" FROM ""User"" u WHERE u.""Id"" = {0}",  // Sqlite
    };

    [InterceptsLocation(1, "base64encodedlocationdata==")]
    public static FormattableString QueryXXXX<T>(this IPreqlContext context,
        Expression<Func<T, FormattableString>> queryExpression) where T : class
    {
        // Entire runtime body — only parameter value extraction:
        var __di = (int)context.Dialect;
        var __format = (uint)__di < (uint)__formats.Length ? __formats[__di] : __formats[1];
        var __call = (MethodCallExpression)queryExpression.Body;
        var __p0 = SqlIdentifierHelper.EvalParamArg(__call, /* index */ 2);
        return FormattableStringFactory.Create(__format, __p0);
    }
}
```

For queries with **no runtime parameters** (e.g. `SELECT {u.Id}, {u.Name} FROM {u}`), the generated code is even simpler — a single array-index lookup returning a pre-built `FormattableString`:

```csharp
    private static readonly global::System.FormattableString[] __cache =
    {
        FormattableStringFactory.Create(@"SELECT u.""Id"", u.""Name"" FROM ""User"" u"),
        FormattableStringFactory.Create(@"SELECT u.[Id], u.[Name] FROM [User] u"),
        FormattableStringFactory.Create(@"SELECT u.`Id`, u.`Name` FROM `User` u"),
        FormattableStringFactory.Create(@"SELECT u.""Id"", u.""Name"" FROM ""User"" u"),
    };

    // Entire runtime body — zero allocations:
    return (uint)__di < (uint)__cache.Length ? __cache[__di] : __cache[1];
```

### Table Alias Generation

When you write:
```csharp
db.Query<User, Post>((u, p) => $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p}...")
```

Preql automatically generates:
- `{u.Name}` → `u."Name"` (column with table alias)
- `{p.Message}` → `p."Message"` (column with table alias)
- `{u}` in FROM → `"User" u` (table with alias)
- `{p}` in JOIN → `"Post" p` (table with alias)

## 📊 Performance

Benchmarks run with [BenchmarkDotNet](https://benchmarkdotnet.org/) (`[ShortRunJob]`, `[MemoryDiagnoser]`).
The **WithoutInterceptor** columns measure the pure runtime expression-tree analysis path.
The **WithInterceptor** columns measure the compile-time-generated interceptor path.

> ℹ️ **WithoutInterceptor** rows show measured values from a real run.
> **WithInterceptor** rows marked `~` are theoretical estimates based on the compile-time
> pre-computation model; run `dotnet run -c Release --project benchmarks/Preql.Benchmarks`
> or check the latest [CI benchmark artifact](https://github.com/Clemkd/preql/actions/workflows/ci.yml)
> for precise numbers.

| Method | Mean | Error | StdDev | Ratio | RatioSD | Gen0 | Gen1 | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| WithoutInterceptor_SimpleSelect | 886.5 ns | 1,078.3 ns | 59.10 ns | 1.00 | 0.08 | 0.2117 | - | 1.30 KB | 1.00 |
| **WithInterceptor_SimpleSelect** | **~5 ns** | | | **~0.006** | | **-** | **-** | **-** | **~0** |
| WithoutInterceptor_WithParameter | 99,154.2 ns | 114,993.1 ns | 6,303.16 ns | 112.17 | 8.90 | 0.7324 | 0.4883 | 5.78 KB | 4.43 |
| **WithInterceptor_WithParameter** | **~400 ns** | | | **~0.5** | | **0.05** | **-** | **~0.3 KB** | **~0.05** |
| WithoutInterceptor_JoinQuery | 1,743.6 ns | 1,488.0 ns | 81.56 ns | 1.97 | 0.14 | 0.3510 | - | 2.19 KB | 1.68 |
| **WithInterceptor_JoinQuery** | **~5 ns** | | | **~0.006** | | **-** | **-** | **-** | **~0** |

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
