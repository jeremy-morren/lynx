using System.Diagnostics;
using Lynx.Provider.Common.Models;
using Npgsql;
using NpgsqlTypes;

namespace Lynx.Provider.Npgsql;

/// <summary>
/// A column and its value.
/// </summary>
/// <remarks>
/// This class is used to write a column value to a <see cref="NpgsqlBinaryImporter"/>.
/// </remarks>
[DebuggerDisplay($"{{{nameof(Property)}, {nameof(DBType)}={{{nameof(DBType)}}}")]
internal class NpgsqlEntityColumn<T>
{
    private readonly Func<T, object?> _getValue;

    public NpgsqlEntityColumn(
        IColumnPropertyInfo property,
        NpgsqlDbType? dbType,
        Func<T, object?> getValue)
    {
        _getValue = getValue;
        Property = property;
        DBType = dbType;
    }

    /// <summary>
    /// Entity column.
    /// </summary>
    public IColumnPropertyInfo Property { get; }

    /// <summary>
    /// Npgsql database type, if known.
    /// </summary>
    public NpgsqlDbType? DBType { get; }

    /// <summary>
    /// Gets the value of the column for the entity.
    /// </summary>
    public object? GetValue(T entity) => _getValue(entity);

    public void Write(T entity, NpgsqlBinaryImporter writer)
    {
        var value = _getValue(entity);
        if (value == null)
            writer.WriteNull();
        else if (DBType.HasValue)
            writer.Write(value, DBType.Value);
        else
            writer.Write(value);
    }

    public async Task WriteAsync(T entity, NpgsqlBinaryImporter writer, CancellationToken ct)
    {
        var value = _getValue(entity);
        if (value == null)
            await writer.WriteNullAsync(ct);
        else if (DBType.HasValue)
            await writer.WriteAsync(value, DBType.Value, ct);
        else
            await writer.WriteAsync(value, ct);
    }
}