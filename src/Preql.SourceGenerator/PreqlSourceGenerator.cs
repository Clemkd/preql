using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Preql.SourceGenerator
{
    /// <summary>
    /// Incremental source generator that intercepts Query method calls and generates SQL at compile-time.
    /// </summary>
    [Generator]
    public class PreqlSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find all invocation expressions that could be Query calls
            var queryInvocations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsQueryInvocation(node),
                    transform: static (ctx, _) => GetQueryInvocationInfo(ctx))
                .Where(static m => m.HasValue);

            // Generate interceptors for each invocation
            context.RegisterSourceOutput(queryInvocations, static (spc, invocationInfo) =>
            {
                if (!invocationInfo.HasValue) return;

                var source = GenerateInterceptor(invocationInfo.Value);
                spc.AddSource($"PreqlInterceptor_{invocationInfo.Value.UniqueId}.g.cs", source);
            });
        }

        private static bool IsQueryInvocation(SyntaxNode node)
        {
            if (!(node is InvocationExpressionSyntax invocation))
                return false;

            // Check if it's a member access like db.Query<T>(...)
            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
                return false;

            return memberAccess.Name.Identifier.Text == "Query";
        }

        private static QueryInvocationInfo? GetQueryInvocationInfo(GeneratorSyntaxContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            // Get the symbol information
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            if (!(symbolInfo.Symbol is IMethodSymbol methodSymbol))
                return null;

            // Check if it's IPreqlContext.Query
            if (methodSymbol.Name != "Query" || !methodSymbol.ContainingType.Name.Contains("PreqlContext"))
                return null;

            // Get the generic type argument (the entity type)
            var entityType = methodSymbol.TypeArguments.FirstOrDefault();
            if (entityType == null)
                return null;

            // Get the lambda expression argument
            if (invocation.ArgumentList.Arguments.Count == 0)
                return null;

            var argument = invocation.ArgumentList.Arguments[0].Expression;
            if (!(argument is LambdaExpressionSyntax lambda))
                return null;

            // Extract the interpolated string from the lambda
            var interpolatedString = ExtractInterpolatedString(lambda);
            if (interpolatedString == null)
                return null;

            // Get the file path and position for the interceptor attribute
            var location = invocation.GetLocation();
            var lineSpan = location.GetLineSpan();
            var filePath = lineSpan.Path;
            var line = lineSpan.StartLinePosition.Line + 1;
            var character = lineSpan.StartLinePosition.Character + 1;

            // Generate a unique ID for this invocation
            var uniqueId = $"{filePath}_{line}_{character}".GetHashCode().ToString("X8");

            return new QueryInvocationInfo(
                EntityTypeName: entityType.ToDisplayString(),
                InterpolatedString: interpolatedString,
                FilePath: filePath,
                Line: line,
                Character: character,
                UniqueId: uniqueId,
                Lambda: lambda,
                SemanticModel: semanticModel
            );
        }

        private static InterpolatedStringExpressionSyntax ExtractInterpolatedString(LambdaExpressionSyntax lambda)
        {
            // The lambda body should contain an interpolated string
            var body = lambda.Body;

            if (body is InterpolatedStringExpressionSyntax interpolated)
                return interpolated;

            // Handle case where body is a block with return statement
            if (body is BlockSyntax block)
            {
                var returnStatement = block.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();
                if (returnStatement?.Expression is InterpolatedStringExpressionSyntax blockInterpolated)
                    return blockInterpolated;
            }

            return null;
        }

        private static string GenerateInterceptor(QueryInvocationInfo info)
        {
            // Parse the interpolated string and generate SQL
            var result = GenerateSqlAndParameters(info);
            var sql = result.Item1;
            var parameters = result.Item2;

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using System.Runtime.CompilerServices;");
            sb.AppendLine("using Preql;");
            sb.AppendLine();
            sb.AppendLine("namespace Preql.Generated;");
            sb.AppendLine();
            sb.AppendLine($"file static class PreqlInterceptor_{info.UniqueId}");
            sb.AppendLine("{");
            sb.AppendLine($"    [InterceptsLocation(@\"{info.FilePath}\", {info.Line}, {info.Character})]");
            sb.AppendLine($"    public static QueryResult Query{info.UniqueId}<T>(this IPreqlContext context, System.Linq.Expressions.Expression<System.Func<T, FormattableString>> queryExpression)");
            sb.AppendLine("    {");
            sb.AppendLine($"        return new QueryResult(");
            sb.AppendLine($"            @\"{sql.Replace("\"", "\"\"")}\",");
            sb.AppendLine($"            {parameters}");
            sb.AppendLine("        );");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static (string, string) GenerateSqlAndParameters(QueryInvocationInfo info)
        {
            var sqlBuilder = new StringBuilder();
            var parameterList = new List<string>();
            var parameterIndex = 0;

            var lambda = info.Lambda;

            // Get parameter name from the lambda
            string parameterName = "p";
            if (lambda is SimpleLambdaExpressionSyntax simpleLambda)
            {
                parameterName = simpleLambda.Parameter.Identifier.Text;
            }
            else if (lambda is ParenthesizedLambdaExpressionSyntax parenthesizedLambda &&
                     parenthesizedLambda.ParameterList.Parameters.Count > 0)
            {
                parameterName = parenthesizedLambda.ParameterList.Parameters[0].Identifier.Text;
            }

            foreach (var content in info.InterpolatedString.Contents)
            {
                if (content is InterpolatedStringTextSyntax text)
                {
                    // Regular text - add as-is
                    sqlBuilder.Append(text.TextToken.Text);
                }
                else if (content is InterpolationSyntax interpolation)
                {
                    // Analyze the interpolation to determine if it's a table, column, or parameter
                    var expression = interpolation.Expression;

                    if (IsTableReference(expression, parameterName))
                    {
                        // Table reference: {u} -> [Users] or "Users" depending on dialect
                        // TODO: Implement more sophisticated pluralization (e.g., Person -> People)
                        // TODO: Support custom table name attributes
                        // NOTE: Currently uses SQL Server bracket syntax; in production this would
                        // be determined from compile-time analysis of the dialect
                        var tableName = info.EntityTypeName.Split('.').Last();
                        if (tableName.EndsWith(">"))
                        {
                            tableName = tableName.Substring(0, tableName.IndexOf('<'));
                        }
                        sqlBuilder.Append($"[{tableName}s]"); // Simple pluralization
                    }
                    else if (IsColumnReference(expression, parameterName))
                    {
                        // Column reference: {u.Name} -> [Name]
                        var memberAccess = (MemberAccessExpressionSyntax)expression;
                        var columnName = memberAccess.Name.Identifier.Text;
                        sqlBuilder.Append($"[{columnName}]");
                    }
                    else
                    {
                        // Variable reference: {id} -> @p0
                        var paramName = $"@p{parameterIndex}";
                        sqlBuilder.Append(paramName);

                        // Extract the variable name
                        var variableName = expression.ToString();
                        parameterList.Add($"{{ \"{paramName.Substring(1)}\", {variableName} }}");
                        parameterIndex++;
                    }
                }
            }

            var parametersCode = parameterList.Count > 0
                ? $"new {{ {string.Join(", ", parameterList)} }}"
                : "null";

            return (sqlBuilder.ToString(), parametersCode);
        }

        private static bool IsTableReference(ExpressionSyntax expression, string parameterName)
        {
            // Check if the expression is just the parameter itself
            return expression is IdentifierNameSyntax identifier &&
                   identifier.Identifier.Text == parameterName;
        }

        private static bool IsColumnReference(ExpressionSyntax expression, string parameterName)
        {
            // Check if the expression is a member access on the parameter
            if (!(expression is MemberAccessExpressionSyntax memberAccess))
                return false;

            if (!(memberAccess.Expression is IdentifierNameSyntax identifier))
                return false;

            return identifier.Identifier.Text == parameterName;
        }
    }

    internal struct QueryInvocationInfo
    {
        public string EntityTypeName { get; }
        public InterpolatedStringExpressionSyntax InterpolatedString { get; }
        public string FilePath { get; }
        public int Line { get; }
        public int Character { get; }
        public string UniqueId { get; }
        public LambdaExpressionSyntax Lambda { get; }
        public SemanticModel SemanticModel { get; }

        public QueryInvocationInfo(
            string EntityTypeName,
            InterpolatedStringExpressionSyntax InterpolatedString,
            string FilePath,
            int Line,
            int Character,
            string UniqueId,
            LambdaExpressionSyntax Lambda,
            SemanticModel SemanticModel)
        {
            this.EntityTypeName = EntityTypeName;
            this.InterpolatedString = InterpolatedString;
            this.FilePath = FilePath;
            this.Line = Line;
            this.Character = Character;
            this.UniqueId = UniqueId;
            this.Lambda = Lambda;
            this.SemanticModel = SemanticModel;
        }
    }
}
