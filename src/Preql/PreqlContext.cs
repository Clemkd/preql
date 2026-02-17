using System.Linq.Expressions;
using System.Reflection;

namespace Preql;

/// <summary>
/// Default implementation of <see cref="IPreqlContext"/>.
/// Provides type-safe SQL generation via lambda expressions.
/// </summary>
/// <example>
/// <code>
/// var context = new PreqlContext(SqlDialect.PostgreSql);
/// int userId = 123;
/// var query = context.Query&lt;User&gt;((u) => $"SELECT {u.Id} FROM {u} WHERE {u.Id} = {userId}");
/// </code>
/// </example>
public class PreqlContext : IPreqlContext
{
    /// <inheritdoc />
    public SqlDialect Dialect { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PreqlContext"/> class.
    /// </summary>
    /// <param name="dialect">The SQL dialect to use.</param>
    public PreqlContext(SqlDialect dialect)
    {
        Dialect = dialect;
    }

    /// <inheritdoc />
    public QueryResult Query<T>(Expression<Func<T, FormattableString>> queryExpression) where T : class
    {
        // NOTE: This method is designed to be intercepted by a source generator.
        // For runtime, we'll use a simpler approach with dynamic proxies.
        
        // Get the parameter name from the lambda (e.g., "u" in (u) => ...)
        var paramName = GetParameterName(queryExpression, 0);
        var proxy = EntityProxyFactory.CreateProxy<T>(Dialect, paramName);
        
        try
        {
            var formattableString = queryExpression.Compile()(proxy);
            // Process the FormattableString using PreqlSqlHandler
            return ProcessFormattableString(formattableString);
        }
        catch (Exception ex)
        {
            // If runtime proxy fails, provide helpful error message
            throw new InvalidOperationException(
                $"Runtime proxy execution failed. Consider using generated proxy types for {typeof(T).Name}. " +
                $"The Query method works best with source-generated proxy types. " +
                $"Error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public QueryResult Query<T1, T2>(Expression<Func<T1, T2, FormattableString>> queryExpression) 
        where T1 : class where T2 : class
    {
        var param1Name = GetParameterName(queryExpression, 0);
        var param2Name = GetParameterName(queryExpression, 1);
        
        var proxy1 = EntityProxyFactory.CreateProxy<T1>(Dialect, param1Name);
        var proxy2 = EntityProxyFactory.CreateProxy<T2>(Dialect, param2Name);
        
        try
        {
            var formattableString = queryExpression.Compile()(proxy1, proxy2);
            return ProcessFormattableString(formattableString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Runtime proxy execution failed for types {typeof(T1).Name} and {typeof(T2).Name}. " +
                $"Consider using generated proxy types. Error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public QueryResult Query<T1, T2, T3>(Expression<Func<T1, T2, T3, FormattableString>> queryExpression) 
        where T1 : class where T2 : class where T3 : class
    {
        var param1Name = GetParameterName(queryExpression, 0);
        var param2Name = GetParameterName(queryExpression, 1);
        var param3Name = GetParameterName(queryExpression, 2);
        
        var proxy1 = EntityProxyFactory.CreateProxy<T1>(Dialect, param1Name);
        var proxy2 = EntityProxyFactory.CreateProxy<T2>(Dialect, param2Name);
        var proxy3 = EntityProxyFactory.CreateProxy<T3>(Dialect, param3Name);
        
        try
        {
            var formattableString = queryExpression.Compile()(proxy1, proxy2, proxy3);
            return ProcessFormattableString(formattableString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Runtime proxy execution failed. Consider using generated proxy types. Error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public QueryResult Query<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, FormattableString>> queryExpression) 
        where T1 : class where T2 : class where T3 : class where T4 : class
    {
        var param1Name = GetParameterName(queryExpression, 0);
        var param2Name = GetParameterName(queryExpression, 1);
        var param3Name = GetParameterName(queryExpression, 2);
        var param4Name = GetParameterName(queryExpression, 3);
        
        var proxy1 = EntityProxyFactory.CreateProxy<T1>(Dialect, param1Name);
        var proxy2 = EntityProxyFactory.CreateProxy<T2>(Dialect, param2Name);
        var proxy3 = EntityProxyFactory.CreateProxy<T3>(Dialect, param3Name);
        var proxy4 = EntityProxyFactory.CreateProxy<T4>(Dialect, param4Name);
        
        try
        {
            var formattableString = queryExpression.Compile()(proxy1, proxy2, proxy3, proxy4);
            return ProcessFormattableString(formattableString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Runtime proxy execution failed. Consider using generated proxy types. Error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public QueryResult Query<T1, T2, T3, T4, T5>(Expression<Func<T1, T2, T3, T4, T5, FormattableString>> queryExpression) 
        where T1 : class where T2 : class where T3 : class where T4 : class where T5 : class
    {
        var param1Name = GetParameterName(queryExpression, 0);
        var param2Name = GetParameterName(queryExpression, 1);
        var param3Name = GetParameterName(queryExpression, 2);
        var param4Name = GetParameterName(queryExpression, 3);
        var param5Name = GetParameterName(queryExpression, 4);
        
        var proxy1 = EntityProxyFactory.CreateProxy<T1>(Dialect, param1Name);
        var proxy2 = EntityProxyFactory.CreateProxy<T2>(Dialect, param2Name);
        var proxy3 = EntityProxyFactory.CreateProxy<T3>(Dialect, param3Name);
        var proxy4 = EntityProxyFactory.CreateProxy<T4>(Dialect, param4Name);
        var proxy5 = EntityProxyFactory.CreateProxy<T5>(Dialect, param5Name);
        
        try
        {
            var formattableString = queryExpression.Compile()(proxy1, proxy2, proxy3, proxy4, proxy5);
            return ProcessFormattableString(formattableString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Runtime proxy execution failed. Consider using generated proxy types. Error: {ex.Message}", ex);
        }
    }

    private string GetParameterName(LambdaExpression expression, int index)
    {
        if (index < expression.Parameters.Count)
        {
            return expression.Parameters[index].Name ?? $"p{index}";
        }
        return $"p{index}";
    }

    private QueryResult ProcessFormattableString(FormattableString formattableString)
    {
        var handler = new PreqlSqlHandler(formattableString.Format.Length, formattableString.ArgumentCount);
        var format = formattableString.Format;
        var args = formattableString.GetArguments();
        
        int argIndex = 0;
        int lastPos = 0;
        
        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] == '{' && i + 1 < format.Length)
            {
                if (format[i + 1] == '{')
                {
                    // Escaped brace
                    handler.AppendLiteral(format.Substring(lastPos, i - lastPos + 1));
                    i++;
                    lastPos = i + 1;
                    continue;
                }
                
                // Find the end of the placeholder
                int endBrace = format.IndexOf('}', i);
                if (endBrace > i)
                {
                    // Append literal part before the placeholder
                    if (i > lastPos)
                    {
                        handler.AppendLiteral(format.Substring(lastPos, i - lastPos));
                    }
                    
                    // Append the formatted argument
                    if (argIndex < args.Length)
                    {
                        handler.AppendFormatted(args[argIndex]);
                        argIndex++;
                    }
                    
                    i = endBrace;
                    lastPos = i + 1;
                }
            }
        }
        
        // Append any remaining literal
        if (lastPos < format.Length)
        {
            handler.AppendLiteral(format.Substring(lastPos));
        }
        
        var (sql, parameters) = handler.Build();
        return new QueryResult(sql, parameters);
    }
}

/// <summary>
/// Factory for creating entity proxies at runtime.
/// In production, a source generator would create strongly-typed proxy classes.
/// For runtime, we dynamically generate proxy types that return SqlColumn for properties.
/// </summary>
internal static class EntityProxyFactory
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type, string?), Type> _proxyTypeCache = new();

    public static T CreateProxy<T>(SqlDialect dialect, string? tableAlias = null) where T : class
    {
        var entityType = typeof(T);
        var tableName = GetTableName<T>();
        
        // Get or create a proxy type for this entity type and alias combination
        var proxyType = _proxyTypeCache.GetOrAdd((entityType, tableAlias), key =>
        {
            return CreateProxyType(key.Item1, dialect, tableName, key.Item2);
        });
        
        // Create an instance of the proxy type
        var instance = Activator.CreateInstance(proxyType, dialect, tableName, tableAlias);
        return (T)instance!;
    }

    private static Type CreateProxyType(Type entityType, SqlDialect dialect, string tableName, string? tableAlias)
    {
        // Use Reflection.Emit to dynamically create a proxy type that implements the entity interface
        var assemblyName = new System.Reflection.AssemblyName($"Preql.DynamicProxies_{Guid.NewGuid():N}");
        var assemblyBuilder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
            assemblyName, System.Reflection.Emit.AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        
        var typeBuilder = moduleBuilder.DefineType(
            $"{entityType.Name}Proxy_{Guid.NewGuid():N}",
            System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class,
            typeof(object));
        
        // Add the entity type as an implemented interface if it's an interface
        if (entityType.IsInterface)
        {
            typeBuilder.AddInterfaceImplementation(entityType);
        }
        
        // Define fields to store dialect, table name, and alias
        var dialectField = typeBuilder.DefineField("_dialect", typeof(SqlDialect), System.Reflection.FieldAttributes.Private);
        var tableNameField = typeBuilder.DefineField("_tableName", typeof(string), System.Reflection.FieldAttributes.Private);
        var aliasField = typeBuilder.DefineField("_alias", typeof(string), System.Reflection.FieldAttributes.Private);
        
        // Define constructor
        var constructor = typeBuilder.DefineConstructor(
            System.Reflection.MethodAttributes.Public,
            System.Reflection.CallingConventions.Standard,
            new[] { typeof(SqlDialect), typeof(string), typeof(string) });
        
        var ilGen = constructor.GetILGenerator();
        ilGen.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
        ilGen.Emit(System.Reflection.Emit.OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        ilGen.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
        ilGen.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
        ilGen.Emit(System.Reflection.Emit.OpCodes.Stfld, dialectField);
        ilGen.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
        ilGen.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);
        ilGen.Emit(System.Reflection.Emit.OpCodes.Stfld, tableNameField);
        ilGen.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
        ilGen.Emit(System.Reflection.Emit.OpCodes.Ldarg_3);
        ilGen.Emit(System.Reflection.Emit.OpCodes.Stfld, aliasField);
        ilGen.Emit(System.Reflection.Emit.OpCodes.Ret);
        
        // Implement properties that return SqlColumn
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var propertyBuilder = typeBuilder.DefineProperty(
                prop.Name,
                System.Reflection.PropertyAttributes.None,
                typeof(SqlColumn),
                Type.EmptyTypes);
            
            var getMethodBuilder = typeBuilder.DefineMethod(
                $"get_{prop.Name}",
                System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Virtual | System.Reflection.MethodAttributes.SpecialName | System.Reflection.MethodAttributes.HideBySig,
                typeof(SqlColumn),
                Type.EmptyTypes);
            
            var getIL = getMethodBuilder.GetILGenerator();
            
            // Create and return a SqlColumn: new SqlColumn(propertyName, this._dialect, this._alias)
            getIL.Emit(System.Reflection.Emit.OpCodes.Ldstr, prop.Name);
            getIL.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            getIL.Emit(System.Reflection.Emit.OpCodes.Ldfld, dialectField);
            getIL.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            getIL.Emit(System.Reflection.Emit.OpCodes.Ldfld, aliasField);
            
            var sqlColumnCtor = typeof(SqlColumn).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(SqlDialect), typeof(string) },
                null);
            
            getIL.Emit(System.Reflection.Emit.OpCodes.Newobj, sqlColumnCtor!);
            getIL.Emit(System.Reflection.Emit.OpCodes.Ret);
            
            propertyBuilder.SetGetMethod(getMethodBuilder);
        }
        
        // Override ToString to return SqlTable
        var toStringMethod = typeBuilder.DefineMethod(
            "ToString",
            System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Virtual | System.Reflection.MethodAttributes.HideBySig,
            typeof(string),
            Type.EmptyTypes);
        
        var toStringIL = toStringMethod.GetILGenerator();
        var sqlTableLocal = toStringIL.DeclareLocal(typeof(SqlTable));
        
        // Create SqlTable: new SqlTable(this._tableName, this._dialect, this._alias)
        toStringIL.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
        toStringIL.Emit(System.Reflection.Emit.OpCodes.Ldfld, tableNameField);
        toStringIL.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
        toStringIL.Emit(System.Reflection.Emit.OpCodes.Ldfld, dialectField);
        toStringIL.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
        toStringIL.Emit(System.Reflection.Emit.OpCodes.Ldfld, aliasField);
        
        var sqlTableCtor = typeof(SqlTable).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(SqlDialect), typeof(string) },
            null);
        
        toStringIL.Emit(System.Reflection.Emit.OpCodes.Newobj, sqlTableCtor!);
        toStringIL.Emit(System.Reflection.Emit.OpCodes.Stloc, sqlTableLocal);
        toStringIL.Emit(System.Reflection.Emit.OpCodes.Ldloca_S, sqlTableLocal);
        toStringIL.Emit(System.Reflection.Emit.OpCodes.Call, typeof(SqlTable).GetMethod("ToString")!);
        toStringIL.Emit(System.Reflection.Emit.OpCodes.Ret);
        
        return typeBuilder.CreateType()!;
    }

    private static string GetTableName<T>()
    {
        var typeName = typeof(T).Name;
        return typeName.EndsWith("s", StringComparison.OrdinalIgnoreCase) 
            ? typeName 
            : typeName + "s";
    }
}
