namespace Preql;

/// <summary>
/// Specifies the database table name for an entity type.
/// When applied, the source generator will use this name instead of falling back to the type name.
/// </summary>
/// <example>
/// <code>
/// [Table("tbl_users")]
/// public class User { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class TableAttribute : Attribute
{
    /// <summary>
    /// The name of the database table.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="TableAttribute"/> with the specified table name.
    /// </summary>
    /// <param name="name">The database table name.</param>
    public TableAttribute(string name)
    {
        Name = name;
    }
}
