# Preql - EF Core Integration Guide

## Using Preql with Entity Framework Core

Preql provides a `ToSql()` method that returns a `FormattableString`, making it compatible with EF Core's `FromInterpolatedSql()` method.

### Benefits

- ✅ **No Runtime Reflection**: SQL is generated at build time (when source generator is fully enabled)
- ✅ **Type-Safe**: Full IntelliSense support for tables and columns
- ✅ **EF Core Compatible**: Works seamlessly with `FromInterpolatedSql()`
- ✅ **Parameterized**: Automatic SQL parameter generation prevents injection

### Basic Usage

```csharp
using Microsoft.EntityFrameworkCore;
using Preql;

public class MyDbContext : DbContext
{
    private readonly IPreqlContext _preql;
    
    public MyDbContext(DbContextOptions options, IPreqlContext preql) 
        : base(options)
    {
        _preql = preql;
    }
    
    public DbSet<User> Users { get; set; }
    
    public IEnumerable<User> GetUserById(int id)
    {
        // Use ToSql() to generate a FormattableString
        var sql = _preql.ToSql<User>((u) => 
            $"SELECT {u.Id}, {u.Name}, {u.Email} FROM {u} WHERE {u.Id} = {id}");
        
        // Pass directly to EF Core's FromInterpolatedSql
        return Users.FromInterpolatedSql(sql).ToList();
    }
}
```

### How It Works

The `ToSql()` method:
1. Analyzes the interpolated string at compile time (when source generator is enabled)
2. Distinguishes between:
   - Table references: `{u}` → `"Users"`
   - Column references: `{u.Name}` → `"Name"`
   - Parameters: `{id}` → `{0}` (with value captured)
3. Generates a `FormattableString` compatible with EF Core

### Notes

- Returns a standard .NET `FormattableString`, not a custom type
- Parameters are automatically numbered `{0}`, `{1}`, etc.
- Compatible with any method accepting `FormattableString`
