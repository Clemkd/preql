namespace Preql;

/// <summary>
/// Specifies the SQL column name that maps to this property.
/// If not set, the property name is used as-is (case-sensitive).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ColumnAttribute : Attribute
{
    /// <summary>The SQL column name for this property.</summary>
    public string Name { get; }

    public ColumnAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Column name must not be null or whitespace.", nameof(name));
        Name = name;
    }
}
