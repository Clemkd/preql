using Preql;

namespace Preql.Sample;

/// <summary>
/// Example Post entity for testing multi-table queries.
/// Uses the [Table] attribute to specify a custom table name.
/// </summary>
[Table("tbl_posts")]
public class Post
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
}
