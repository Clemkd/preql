using System.Text.RegularExpressions;
using Dapper;

namespace Preql.IntegrationTests;

/// <summary>
/// Helpers to convert a Preql <see cref="FormattableString"/> (with <c>{0}</c>-style
/// positional placeholders) into a format that Dapper can consume.
/// </summary>
public static class DapperHelper
{
    /// <summary>
    /// Converts a Preql <see cref="FormattableString"/> to a SQL string and
    /// <see cref="DynamicParameters"/> suitable for Dapper.
    /// Replaces <c>{0}</c>, <c>{1}</c>, … with <c>@p0</c>, <c>@p1</c>, … and
    /// builds a <see cref="DynamicParameters"/> containing the values.
    /// </summary>
    public static (string Sql, DynamicParameters Parameters) ToDapper(FormattableString query)
    {
        var args = query.GetArguments();
        var dp = new DynamicParameters();

        var sql = Regex.Replace(query.Format, @"\{(\d+)\}", m =>
        {
            int idx = int.Parse(m.Groups[1].Value);
            string paramName = $"p{idx}";
            dp.Add(paramName, args[idx]);
            return $"@{paramName}";
        });

        return (sql, dp);
    }
}
