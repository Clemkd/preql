# Preql - InterpolatedStringHandler Guide

## The Best Approach: Zero Reflection with InterpolatedStringHandler

Preql provides an `InterpolatedStringHandler` approach that generates SQL **entirely at build time** without any runtime reflection or expression tree analysis.

## Quick Start

```csharp
using Preql;

// 1. Get a typed alias for your entity
var u = context.Alias<User>();

// 2. Write your query using the handler (automatic!)
int userId = 123;
PreqlSqlHandler handler = $"SELECT {u["Id"]}, {u["Name"]} FROM {u} WHERE {u["Id"]} = {userId.AsValue()}";

// 3. Get the SQL and parameters
var (sql, parameters) = handler.Build();
// SQL: SELECT "Id", "Name" FROM "Users" WHERE "Id" = @p0
// Parameters: [@p0=123]

// 4. Or get a FormattableString for EF Core
var formattable = handler.BuildFormattable();
var users = dbContext.Users.FromInterpolatedSql(formattable);
```

## How It Works

### 1. Proxy Types

Preql provides three proxy types that the compiler recognizes:

- **`SqlColumn`** - Represents a column (e.g., `u["Name"]`)
- **`SqlTable`** - Represents a table (e.g., `u` when used as table)
- **`SqlValue`** - Wraps a value as a parameter (e.g., `id.AsValue()`)

### 2. InterpolatedStringHandler

The `PreqlSqlHandler` is an interpolated string handler that the C# compiler uses automatically:

```csharp
PreqlSqlHandler handler = $"...";  // Compiler calls handler methods
```

The compiler transforms your interpolated string into method calls:
- `AppendLiteral("SELECT ")` for text
- `AppendFormatted(column)` for columns
- `AppendFormatted(table)` for tables
- `AppendFormatted(value)` for parameters

### 3. Zero Runtime Cost

Everything happens at **compile time**:
1. Compiler sees the interpolated string
2. Compiler calls `PreqlSqlHandler` methods
3. SQL is built during string construction
4. No reflection, no expression trees, no overhead

## Complete Example

```csharp
public class UserRepository
{
    private readonly IPreqlContext _preql;
    private readonly DbContext _dbContext;

    public UserRepository(IPreqlContext preql, DbContext dbContext)
    {
        _preql = preql;
        _dbContext = dbContext;
    }

    public IEnumerable<User> SearchUsers(string nameLike, int minAge)
    {
        // Create typed alias
        var u = _preql.Alias<User>();

        // Build query with handler
        PreqlSqlHandler handler = $"""
            SELECT {u["Id"]}, {u["Name"]}, {u["Email"]}, {u["Age"]}
            FROM {u}
            WHERE {u["Name"]} LIKE {nameLike.AsValue()}
            AND {u["Age"]} >= {minAge.AsValue()}
            ORDER BY {u["Name"]}
            """;

        // Use with EF Core
        return _dbContext.Users
            .FromInterpolatedSql(handler.BuildFormattable())
            .ToList();
    }

    public User? GetById(int id)
    {
        var u = _preql.Alias<User>();

        PreqlSqlHandler handler = $"""
            SELECT {u["Id"]}, {u["Name"]}, {u["Email"]}
            FROM {u}
            WHERE {u["Id"]} = {id.AsValue()}
            """;

        return _dbContext.Users
            .FromInterpolatedSql(handler.BuildFormattable())
            .FirstOrDefault();
    }
}
```

## API Reference

### SqlTableAlias<T>

```csharp
var u = context.Alias<User>();

// Access columns by name
u["Id"]       // Returns SqlColumn for "Id"
u["Name"]     // Returns SqlColumn for "Name"

// Use as table reference
$"FROM {u}"   // Becomes: FROM "Users"
```

### SqlValue Extension

```csharp
int id = 123;
string name = "John";

id.AsValue()     // Wraps as SqlValue
name.AsValue()   // Wraps as SqlValue

// In query:
$"WHERE {u["Id"]} = {id.AsValue()}"
// Becomes: WHERE "Id" = @p0
```

### PreqlSqlHandler

```csharp
PreqlSqlHandler handler = $"...your query...";

// Get SQL and parameters
var (sql, params) = handler.Build();

// Get FormattableString for EF Core
var formattable = handler.BuildFormattable();
```

## SQL Dialect Support

The handler respects your configured dialect:

```csharp
// PostgreSQL
builder.Services.AddPreql(SqlDialect.PostgreSql);
// Output: "TableName", "ColumnName"

// SQL Server
builder.Services.AddPreql(SqlDialect.SqlServer);
// Output: [TableName], [ColumnName]

// MySQL
builder.Services.AddPreql(SqlDialect.MySql);
// Output: `TableName`, `ColumnName`
```

## Comparison with Other Approaches

### vs. Runtime Expression Analysis
```csharp
// OLD: Runtime analysis (has overhead)
var query = db.Query<User>((u) => $"SELECT {u.Id} FROM {u}");
// Analyzes expression tree at runtime

// NEW: Build-time handler (zero overhead)
var u = db.Alias<User>();
PreqlSqlHandler h = $"SELECT {u["Id"]} FROM {u}";
// Compiled by C# compiler, no runtime work
```

### vs. Raw EF Core
```csharp
// EF Core: No type safety
var sql = $"SELECT Id, Name FROM Users WHERE Id = {id}";
// Column names are strings, prone to typos

// Preql: Type-safe columns
var u = db.Alias<User>();
PreqlSqlHandler h = $"SELECT {u["Id"]}, {u["Name"]} FROM {u} WHERE {u["Id"]} = {id.AsValue()}";
// Columns validated at build time
```

## Advanced: Multiple Tables

```csharp
var u = db.Alias<User>();
var o = db.Alias<Order>();

PreqlSqlHandler handler = $"""
    SELECT 
        {u["Id"]},
        {u["Name"]},
        COUNT(*) as OrderCount
    FROM {u}
    INNER JOIN {o} ON {u["Id"]} = {o["UserId"]}
    WHERE {o["CreatedAt"]} >= {since.AsValue()}
    GROUP BY {u["Id"]}, {u["Name"]}
    """;
```

## Benefits

✅ **Zero Runtime Reflection** - All work at compile time  
✅ **Zero Runtime Overhead** - Direct SQL string building  
✅ **Type-Safe** - Columns validated at build time  
✅ **EF Core Compatible** - Via `BuildFormattable()`  
✅ **IntelliSense Support** - Full editor support  
✅ **Refactor-Safe** - Change entity, update queries  
✅ **SQL Injection Safe** - Automatic parameterization  

## Limitations

- Column access via indexer `u["Name"]` (not `u.Name`)
- Requires .NET 6+ for InterpolatedStringHandler
- Simple table name pluralization (can be customized)

## Future Enhancements

A source generator could create typed proxies with properties:

```csharp
// Instead of: u["Name"]
// Could be: u.Name (with source generator)
```

This would provide full IntelliSense on properties while maintaining zero runtime cost.
