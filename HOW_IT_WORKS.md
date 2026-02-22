# 🏗️ How Preql Works

This document explains the two execution paths Preql uses to turn a typed interpolated string into a parameterized SQL query.

## Runtime fallback (always available)

On every call, Preql walks the lambda expression tree at runtime to classify each interpolated hole:

- `{u}` → table reference (emits `"User" u`)
- `{u.Id}` → column reference (emits `u."Id"`)
- `{userId}` → parameter value (emits `{0}`, `{1}`, …)

This path requires no extra setup and works out of the box with any `IPreqlContext`.

## Compile-time generation (recommended — zero analysis overhead)

When `Preql.SourceGenerator` is referenced as an analyzer in your project, Preql intercepts every `context.Query<…>(lambda)` call **at compile time** using C# source generators and interceptors:

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

### Known dialect at compile time

When the dialect can be resolved at compile time (e.g. `var ctx = new PreqlContext(SqlDialect.PostgreSql); ctx.Query<T>(...)`), the generator emits a single SQL constant — no array, no runtime lookup:

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

### Unknown dialect (injected `IPreqlContext`)

When the dialect is unknown (e.g. injected via DI), all 4 dialect variants are embedded and the correct one is selected via a single array-index lookup:

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

## Table alias generation

When you write:

```csharp
db.Query<User, Post>((u, p) => $"SELECT {u.Name}, {p.Message} FROM {u} JOIN {p}...")
```

Preql automatically generates:

- `{u.Name}` → `u."Name"` (column with table alias)
- `{p.Message}` → `p."Message"` (column with table alias)
- `{u}` in FROM → `"User" u` (table with alias)
- `{p}` in JOIN → `"Post" p` (table with alias)
