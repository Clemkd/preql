# 🛡️ Preql

[![CI](https://github.com/Clemkd/preql/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Clemkd/preql/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Clemkd/preql/branch/main/graph/badge.svg)](https://codecov.io/gh/Clemkd/preql)

**Preql** (pronounced *Prequel*) is a C# library that transmutes typed interpolated strings into SQL.

By analyzing C# expression trees, Preql can intelligently distinguish between table references, column references, and parameter values, generating clean, parameterized SQL queries at compile time with automatic table aliases.

## 🤔 Why Preql?

Preql was born from real frustrations encountered during day-to-day development with EF Core:

- **Developers know SQL — but fight LINQ**: Many developers are comfortable writing SQL directly, yet spend more time wrestling with LINQ and EF Core to *generate* the SQL they already have in mind, rather than simply writing it.

- **LINQ reaches its limits on advanced queries**: LINQ with EF Core works well for simple queries, but as soon as you need more advanced SQL features — `WITH CTE`, `ROW_NUMBER`, window functions, etc. — you quickly hit walls or become tightly coupled to a specific database provider.

- **Database portability is overrated in practice**: While EF Core's abstraction layer theoretically makes it easier to switch database engines, this rarely (if ever) happens during the lifetime of a real project.

- **The goal: write SQL, keep C# safety**: Preql was created to stop wasting time writing LINQ extensions and expression trees to produce SQL that was already clear in the developer's head — and to avoid unpredictable behavior changes between EF Core versions. Preql is **not a replacement for LINQ in EF Core** — it is a complement, designed for complex or advanced SQL queries that are poorly suited to LINQ generation. At the same time, it retains the ability to track C# property renames through the Roslyn analyzer, catching breaking changes at compile time instead of at runtime after a refactoring.

## ✨ Key Features

* 🚀 **Clean SQL Generation**: Automatically converts typed queries into parameterized SQL
* 🛠️ **Strongly Typed**: Refactor-friendly. If you rename a property in your class, your query will be updated accordingly.
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

public class UserRepository(IPreqlContext preql, IDbConnection conn)
{
    public async Task<User> GetById(int id)
    {
        var query = preql.Query<User>((u) => 
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
    var query = preql.Query<User, Post>((u, p) => 
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
var query = preql.Query<User, Post>((u, p) =>
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

var query = preql.Query<User>((u) => $"SELECT {u.Id}, {u.Name} FROM {u}");
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
  5. Tries to resolve the dialect of the receiver (e.g. from new PreqlContext(SqlDialect.PostgreSql))
     • Dialect known   → embeds a SINGLE SQL string constant for that dialect only
     • Dialect unknown → embeds pre-computed strings for all dialects, selects at runtime
  6. Emits PreqlInterceptor_XXXX.g.cs with [InterceptsLocation], replacing the original call
         ↓
Generated Interceptor (Runtime):
  - No SQL building at all — the complete SQL is already a string literal
  - When dialect is known: return pre-built FormattableString directly (no-param queries)
    or param extraction only (param queries)
  - When dialect is unknown: single array-index lookup + optional param extraction
         ↓
Result:
  query.Format: "SELECT u.\"Id\" FROM \"User\" u WHERE u.\"Id\" = {0}"
  query.GetArguments(): [userId_value]
```

**When the dialect is known at compile time** (e.g. `var ctx = new PreqlContext(SqlDialect.PostgreSql); ctx.Query<T>(...)`), the generator emits a single SQL constant — no array, no runtime lookup:

```csharp
// PreqlInterceptor_XXXX.g.cs  (auto-generated — do not edit)
// SQL structure and dialect quoting fully determined at compile time.
// At runtime: only parameter value extraction + FormattableString creation.

file static class PreqlInterceptor_XXXX
{
    // Single SQL constant — only the dialect of this call site, known at compile time:
    private static readonly string __format =
        @"SELECT u.""Id"" FROM ""User"" u WHERE u.""Id"" = {0}";  // PostgreSql

    [InterceptsLocation(1, "base64encodedlocationdata==")]
    public static FormattableString QueryXXXX<T>(this PreqlContext context,
        Expression<Func<T, FormattableString>> queryExpression) where T : class
    {
        // Entire runtime body — only parameter value extraction:
        var __call = (MethodCallExpression)queryExpression.Body;
        var __p0 = SqlIdentifierHelper.EvalParamArg(__call, /* index */ 2);
        return FormattableStringFactory.Create(__format, __p0);
    }
}
```

For no-parameter queries with a known dialect, the method body is a single field return:

```csharp
    private static readonly global::System.FormattableString __result =
        FormattableStringFactory.Create(@"SELECT u.""Id"", u.""Name"" FROM ""User"" u");

    // Entire runtime body — zero allocations:
    return __result;
```

**When the dialect is unknown** (e.g. injected `IPreqlContext`), all 4 dialect variants are embedded and the correct one is selected via a single array-index lookup:

```csharp
    private static readonly string[] __formats =
    {
        @"SELECT u.""Id"" FROM ""User"" u WHERE u.""Id"" = {0}",  // PostgreSql
        @"SELECT u.[Id] FROM [User] u WHERE u.[Id] = {0}",          // SqlServer
        @"SELECT u.`Id` FROM `User` u WHERE u.`Id` = {0}",           // MySql
        @"SELECT u.""Id"" FROM ""User"" u WHERE u.""Id"" = {0}",  // Sqlite
    };

    var __di = (int)context.Dialect;
    var __format = (uint)__di < (uint)__formats.Length ? __formats[__di] : __formats[1];
    // ... param extraction ...
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

Benchmarks compare two paths:

- **WithoutInterceptor** — pure runtime expression-tree analysis on every call
- **WithInterceptor** — compile-time-generated interceptor (array-index lookup ± parameter extraction)

Results are produced by [BenchmarkDotNet](https://benchmarkdotnet.org/) (`[ShortRunJob]`, `[MemoryDiagnoser]`)
and saved as a Markdown report on every push to `main`.

👉 **[View the latest benchmark results](https://github.com/Clemkd/preql/actions/workflows/ci.yml)**
(open the most recent successful run on `main` and download the `benchmark-results` artifact — it contains a `.md` report you can read directly)

To run the benchmarks locally:

```bash
dotnet run -c Release --project benchmarks/Preql.Benchmarks
```

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
