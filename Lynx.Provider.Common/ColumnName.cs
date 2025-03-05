using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;

namespace Lynx.Provider.Common;

/// <summary>
/// A column name (with nested owned types) for a given entity.
/// </summary>
internal class ColumnName : IReadOnlyList<string>, IEquatable<ColumnName>
{
    private readonly ImmutableArray<string> _columnNames;

    private ColumnName(ImmutableArray<string> columnNames)
    {
        _columnNames = columnNames;
        SqlColumnName = string.Join("_", columnNames);
    }

    public string SqlColumnName { get; }

    /// <summary>
    /// Creates a new root column name.
    /// </summary>
    /// <param name="columnName"></param>
    /// <returns></returns>
    [Pure]
    public static ColumnName NewRoot(string columnName) => new([columnName]);

    /// <summary>
    /// Adds a new column name (owned type).
    /// </summary>
    [Pure]
    public ColumnName Add(string columnName) => new(_columnNames.Add(columnName));

    #region Implementation of IReadOnlyList<string>

    public IEnumerator<string> GetEnumerator()
    {
        return ((IEnumerable<string>)_columnNames).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_columnNames).GetEnumerator();
    }

    public int Count => _columnNames.Length;

    public string this[int index] => _columnNames[index];

    #endregion

    #region Equality members

    public bool Equals(ColumnName? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _columnNames.Length == other._columnNames.Length
               && Enumerable.Range(0, _columnNames.Length).All(i => _columnNames[i] == other._columnNames[i]);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is ColumnName columnName && Equals(columnName);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_columnNames, SqlColumnName);
    }

    public static bool operator ==(ColumnName? left, ColumnName? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ColumnName? left, ColumnName? right)
    {
        return !Equals(left, right);
    }

    #endregion
}