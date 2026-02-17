using Preql;
using Preql.Sample.Generated;

namespace Preql.Sample;

// Example entity class
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🛡️ Preql Sample Application");
        Console.WriteLine("=============================\n");

        // Run the new multi-table query examples
        Console.WriteLine("Running Multi-Table Query Examples with Aliases...\n");
        AliasExamples.Run();
    }

    static string FormatParamList(IReadOnlyList<object?> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return "none";

        return string.Join(", ", parameters.Select((p, i) => $"@p{i}={p}"));
    }
}
