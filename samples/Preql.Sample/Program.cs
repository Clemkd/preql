using Preql;

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

        AliasExamples.Run();
    }
}
