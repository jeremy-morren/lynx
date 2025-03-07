using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;

namespace Lynx.Providers.Common.Models;

/// <summary>
/// A property chain (a list of property names).
/// </summary>
internal class PropertyChain : IReadOnlyList<string>, IEquatable<IReadOnlyList<string>>, IEquatable<PropertyChain>
{
    private readonly ImmutableArray<string> _properties;

    private PropertyChain(ImmutableArray<string> properties)
    {
        _properties = properties;
        SqlColumnName = string.Join("_", properties);
    }

    /// <summary>
    /// SQL column name (underscore separated)
    /// </summary>
    public string SqlColumnName { get; }

    /// <summary>
    /// SQL parameter name
    /// </summary>
    public string SqlParamName => $"@{SqlColumnName}";

    /// <summary>
    /// Creates a new root chain
    /// </summary>
    /// <param name="columnName"></param>
    /// <returns></returns>
    [Pure]
    public static PropertyChain NewRoot(string columnName) => new([columnName]);

    /// <summary>
    /// Adds a new property (owned type).
    /// </summary>
    [Pure]
    public PropertyChain Add(string columnName) => new(_properties.Add(columnName));

    /// <summary>
    /// Creates a new chain
    /// </summary>
    [Pure]
    public static PropertyChain Create(ImmutableArray<string> columnNames) => new(columnNames);

    public override string ToString() => $"[ {string.Join(", ", _properties)} ]";

    #region Implementation of IReadOnlyList<string>

    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)_properties).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_properties).GetEnumerator();

    public int Count => _properties.Length;

    public string this[int index] => _properties[index];

    #endregion

    #region Equality members

    public bool Equals(IReadOnlyList<string>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _properties.Length == other.Count
               && Enumerable.Range(0, _properties.Length).All(i => _properties[i] == other[i]);
    }

    public bool Equals(PropertyChain? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals((IReadOnlyList<string>)other);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is IReadOnlyList<string> other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(_properties.Length);
        foreach (var property in _properties)
            hashCode.Add(property);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(PropertyChain? left, IReadOnlyList<string>? right) => Equals(left, right);

    public static bool operator !=(PropertyChain? left, IReadOnlyList<string>? right) => !Equals(left, right);

    public static bool operator ==(PropertyChain? left, PropertyChain? right) => Equals(left, right);

    public static bool operator !=(PropertyChain? left, PropertyChain? right) => !Equals(left, right);

    #endregion
}