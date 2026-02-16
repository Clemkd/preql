# Preql - Architecture Sans Réflexion

## Vision: Zéro Réflexion Runtime

Preql utilise la **génération de source** au moment de la compilation pour éliminer toute réflexion à l'exécution.

## Architecture Actuelle

### 1. InterpolatedStringHandler (✅ Sans Réflexion)

**Approche actuelle - Complètement sans réflexion:**

```csharp
var u = context.Alias<User>();
PreqlSqlHandler handler = $"SELECT {u["Id"]}, {u["Name"]} FROM {u} WHERE {u["Id"]} = {id.AsValue()}";
var (sql, params) = handler.Build();
```

**Comment ça marche:**
- Le compilateur C# transforme automatiquement la string interpolée en appels au handler
- `PreqlSqlHandler.AppendFormatted()` est appelé pour chaque interpolation
- Aucune réflexion utilisée - tout est fait par le compilateur

### 2. AliasProxy Générés (🎯 Futur via Source Generator)

**Vision future avec génération automatique:**

```csharp
// Code utilisateur:
var result = db.Query<User>((u) => $"SELECT {u.Id}, {u.Name} FROM {u}");

// ↓ Le source generator transforme en:
var u_generated = new UserAliasProxy(dialect);
PreqlSqlHandler handler = $"SELECT {u_generated.Id}, {u_generated.Name} FROM {u_generated}";
var (sql, params) = handler.Build();
return new QueryResult(sql, params);
```

**Le source generator génère:**

```csharp
// UserAliasProxy.g.cs - Généré automatiquement
public class UserAliasProxy : AliasProxy
{
    public UserAliasProxy(SqlDialect dialect) : base("Users", dialect) { }
    
    public SqlColumn Id => GetColumn("Id");
    public SqlColumn Name => GetColumn("Name");
    public SqlColumn Email => GetColumn("Email");
}
```

## Composants

### AliasProxy (Base Class)

```csharp
public abstract class AliasProxy
{
    protected SqlDialect Dialect { get; }
    protected string TableName { get; }
    
    protected SqlColumn GetColumn(string columnName)
    {
        return new SqlColumn(columnName, Dialect);
    }
    
    public SqlTable AsTable() => new SqlTable(TableName, Dialect);
}
```

**Pas de réflexion:** Tout est passé en paramètres de constructeur.

### PreqlSqlHandler

```csharp
[InterpolatedStringHandler]
public ref struct PreqlSqlHandler
{
    public void AppendFormatted(SqlColumn column) { ... }
    public void AppendFormatted(SqlTable table) { ... }
    public void AppendFormatted(SqlValue value) { ... }
    public void AppendFormatted(AliasProxy proxy) { ... }
}
```

**Pas de réflexion:** Le compilateur génère les appels à ces méthodes.

### Types Proxy (SqlColumn, SqlTable, SqlValue)

```csharp
public readonly struct SqlColumn
{
    private readonly string _name;
    private readonly SqlDialect _dialect;
    
    public override string ToString() => FormatIdentifier(_name, _dialect);
}
```

**Pas de réflexion:** Juste des structs avec des valeurs.

## Ce qui ÉTAIT avec Réflexion (Obsolète)

### ❌ ExpressionAnalyzer (Obsolète)

```csharp
// ANCIEN - Utilisait la réflexion
var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
var tableName = typeof(T).Name;
```

**Problème:** 
- `GetProperties()` utilise System.Reflection
- Analyse à l'exécution, pas au build time

### ❌ SqlTableAlias (Avant)

```csharp
// ANCIEN - Utilisait la réflexion pour pré-créer les colonnes
internal SqlTableAlias(SqlDialect dialect)
{
    var properties = typeof(T).GetProperties(...); // ❌ Réflexion!
    foreach (var prop in properties)
        _columns[prop.Name] = new SqlColumn(prop.Name, dialect);
}
```

### ✅ SqlTableAlias (Maintenant)

```csharp
// NOUVEAU - Création à la demande, pas de réflexion
internal SqlTableAlias(SqlDialect dialect)
{
    _columns = new Dictionary<string, SqlColumn>();
    // Pas de pré-création, les colonnes sont créées à la demande via l'indexeur
}

public SqlColumn this[string propertyName]
{
    get
    {
        if (!_columns.TryGetValue(propertyName, out var column))
        {
            column = new SqlColumn(propertyName, Dialect); // Pas de réflexion
            _columns[propertyName] = column;
        }
        return column;
    }
}
```

## Roadmap: Source Generator Complet

### Phase 1: Détection (TODO)

Le source generator doit:
1. Détecter les appels à `Query<T>((u) => ...)`
2. Extraire le type `T` et la lambda

### Phase 2: Génération de Proxy (TODO)

Pour chaque type `T` utilisé:
```csharp
// Générer {T}AliasProxy.g.cs
public class UserAliasProxy : AliasProxy
{
    public UserAliasProxy(SqlDialect dialect) : base("Users", dialect) { }
    
    // Pour chaque propriété publique de User:
    public SqlColumn Id => GetColumn("Id");
    public SqlColumn Name => GetColumn("Name");
    // etc.
}
```

### Phase 3: Interception (TODO)

Générer un intercepteur qui remplace:
```csharp
db.Query<User>((u) => $"SELECT {u.Id} FROM {u}")
```

Par:
```csharp
var u = new UserAliasProxy(context.Dialect);
PreqlSqlHandler h = $"SELECT {u.Id} FROM {u}";
var (sql, params) = h.Build();
return new QueryResult(sql, params);
```

## Avantages de Cette Architecture

### ✅ Zero Réflexion
- Aucun appel à `typeof().GetProperties()`
- Aucun `System.Reflection`
- Tout au build time

### ✅ Zero Runtime Overhead
- Le compilateur C# génère tout
- Pas d'analyse d'expression à l'exécution
- Juste des appels de méthode directs

### ✅ Type-Safe
- Les propriétés sur les proxies sont typées
- IntelliSense complet
- Erreurs à la compilation, pas à l'exécution

### ✅ Compatible EF Core
```csharp
var u = new UserAliasProxy(dialect);
PreqlSqlHandler h = $"SELECT {u.Id} FROM {u} WHERE {u.Id} = {id.AsValue()}";
var users = context.Users.FromInterpolatedSql(h.BuildFormattable()).ToList();
```

## Exemple Complet Sans Réflexion

```csharp
// 1. Créer un proxy (manuel ou généré)
var u = new UserAliasProxy(context.Dialect);

// 2. Utiliser le handler (le compilateur génère les appels)
int userId = 123;
PreqlSqlHandler handler = $"""
    SELECT {u.Id}, {u.Name}, {u.Email}
    FROM {u}
    WHERE {u.Id} = {userId.AsValue()}
    """;

// 3. Obtenir le résultat
var (sql, parameters) = handler.Build();
// sql: SELECT "Id", "Name", "Email" FROM "Users" WHERE "Id" = @p0
// parameters: { @p0: 123 }

// 4. Utiliser avec EF Core
var users = dbContext.Users
    .FromInterpolatedSql(handler.BuildFormattable())
    .ToList();
```

**Aucune réflexion utilisée à aucune étape!**

## Status Implementation

- ✅ AliasProxy (classe de base)
- ✅ PreqlSqlHandler (gère les proxies)
- ✅ Exemple de proxy généré (UserAliasProxy)
- ✅ Documentation et exemples
- ⏳ Source Generator automatique (TODO)
- ⏳ Interception de Query<T> (TODO)

## Conclusion

Preql utilise une architecture **100% sans réflexion** basée sur:
1. **InterpolatedStringHandler** - Le compilateur fait tout
2. **AliasProxy générés** - Classes générées au build time
3. **Types proxy** - Structs simples sans réflexion

L'approche actuelle avec `PreqlSqlHandler` est déjà sans réflexion et prête pour la production. Le source generator automatique viendra améliorer l'expérience développeur en transformant automatiquement les lambdas.
