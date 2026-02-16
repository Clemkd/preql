using System.Linq.Expressions;

namespace Preql;

/// <summary>
/// Analyzes query expression trees to generate SQL.
/// </summary>
internal static class ExpressionAnalyzer
{
    public static QueryResult Analyze<T>(Expression<Func<T, FormattableString>> queryExpression, SqlDialect dialect)
    {
        // Visit the lambda body to analyze the interpolated string
        if (queryExpression.Body is not MethodCallExpression methodCall)
        {
            throw new InvalidOperationException("Expected interpolated string expression");
        }
        
        // The method call should be FormattableStringFactory.Create
        if (methodCall.Method.Name != "Create" || 
            methodCall.Method.DeclaringType?.Name != "FormattableStringFactory")
        {
            throw new InvalidOperationException("Expected FormattableStringFactory.Create call");
        }
        
        // First argument is the format string
        var formatExpr = methodCall.Arguments[0];
        var format = GetConstantValue<string>(formatExpr);
        
        // Second argument is the array of interpolated values
        var argsArrayExpr = methodCall.Arguments[1];
        
        if (argsArrayExpr is not NewArrayExpression newArrayExpr)
        {
            throw new InvalidOperationException("Expected array of interpolated values");
        }
        
        var parameter = queryExpression.Parameters[0];
        var sql = format!;
        var parameters = new Dictionary<string, object?>();
        var paramIndex = 0;
        
        // Analyze each interpolated value
        for (int i = 0; i < newArrayExpr.Expressions.Count; i++)
        {
            var expr = newArrayExpr.Expressions[i];
            var placeholder = $"{{{i}}}";
            
            if (IsParameterReference(expr, parameter))
            {
                // Table reference: {u} -> [Users]
                var tableName = typeof(T).Name + "s"; // Simple pluralization
                sql = sql.Replace(placeholder, FormatIdentifier(tableName, dialect));
            }
            else if (IsMemberAccess(expr, parameter, out var memberName))
            {
                // Column reference: {u.Name} -> [Name]
                sql = sql.Replace(placeholder, FormatIdentifier(memberName!, dialect));
            }
            else
            {
                // Variable/parameter reference: {id} -> @p0
                var paramName = $"p{paramIndex}";
                sql = sql.Replace(placeholder, $"@{paramName}");
                
                // Evaluate the expression to get its runtime value
                var value = EvaluateExpression(expr);
                parameters[paramName] = value;
                paramIndex++;
            }
        }
        
        return new QueryResult(sql, parameters);
    }
    
    private static bool IsParameterReference(Expression expr, ParameterExpression parameter)
    {
        // Check if this is a direct reference to the parameter
        return expr is ParameterExpression paramExpr && paramExpr == parameter;
    }
    
    private static bool IsMemberAccess(Expression expr, ParameterExpression parameter, out string? memberName)
    {
        memberName = null;
        
        // Remove any Convert/ConvertChecked nodes
        while (expr is UnaryExpression unary && 
               (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
        {
            expr = unary.Operand;
        }
        
        // Check if this is a member access on the parameter (e.g., u.Name)
        if (expr is MemberExpression memberExpr)
        {
            if (memberExpr.Expression is ParameterExpression paramExpr && paramExpr == parameter)
            {
                memberName = memberExpr.Member.Name;
                return true;
            }
        }
        
        return false;
    }
    
    private static object? EvaluateExpression(Expression expr)
    {
        // Only evaluate expressions that don't reference the lambda parameter
        // For member accesses on the parameter, we shouldn't evaluate them
        try
        {
            // Check if the expression contains any parameter references
            if (ContainsParameterReference(expr))
            {
                // This should not be evaluated - it's a column reference
                return null;
            }
            
            // Compile and execute the expression to get its value
            var lambda = Expression.Lambda(expr);
            var compiled = lambda.Compile();
            return compiled.DynamicInvoke();
        }
        catch
        {
            return null;
        }
    }
    
    private static bool ContainsParameterReference(Expression expr)
    {
        // Check if the expression tree contains any ParameterExpression nodes
        var visitor = new ParameterReferenceVisitor();
        visitor.Visit(expr);
        return visitor.HasParameterReference;
    }
    
    private class ParameterReferenceVisitor : ExpressionVisitor
    {
        public bool HasParameterReference { get; private set; }
        
        protected override Expression VisitParameter(ParameterExpression node)
        {
            HasParameterReference = true;
            return base.VisitParameter(node);
        }
    }
    
    private static T? GetConstantValue<T>(Expression expr)
    {
        if (expr is ConstantExpression constantExpr)
        {
            return (T?)constantExpr.Value;
        }
        return default;
    }
    
    private static string FormatIdentifier(string identifier, SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.PostgreSql => $"\"{identifier}\"",
            SqlDialect.SqlServer => $"[{identifier}]",
            SqlDialect.MySql => $"`{identifier}`",
            SqlDialect.Sqlite => $"\"{identifier}\"",
            _ => $"[{identifier}]"
        };
    }
}
