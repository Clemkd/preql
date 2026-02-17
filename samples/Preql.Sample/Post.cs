namespace Preql.Sample;

/// <summary>
/// Example Post entity for testing multi-table queries
/// </summary>
public class Post
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
}
