using System.Diagnostics;
using Lynx.Providers.Common.Models;
using Npgsql;
using NpgsqlTypes;

namespace Lynx.Provider.Npgsql;

/// <summary>
/// A column and its value.
/// </summary>
/// <remarks>
/// This class is used to write a column value to a <see cref="NpgsqlBinaryImporter"/>.
/// </remarks>
[DebuggerDisplay($"{{{nameof(Property)}}}, {nameof(DBType)}={{{nameof(DBType)}}}")]
internal class NpgsqlEntityColumn<TEntity, TWriter>
{
    private readonly Action<TEntity, TWriter> _writeAction;
    private readonly Func<TEntity, TWriter, CancellationToken, Task> _writeAsyncAction;

    public NpgsqlEntityColumn(
        IColumnPropertyInfo property,
        NpgsqlDbType? dbType,
        Action<TEntity, TWriter> writeAction,
        Func<TEntity, TWriter, CancellationToken, Task> writeAsyncAction)
    {
        _writeAction = writeAction;
        _writeAsyncAction = writeAsyncAction;

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
    /// Writes the value of the column for the entity to the writer.
    /// </summary>
    public void Write(TEntity entity, TWriter writer) => _writeAction(entity, writer);

    /// <summary>
    /// Writes the value of the column for the entity to the writer.
    /// </summary>
    public Task WriteAsync(TEntity entity, TWriter writer, CancellationToken ct) => _writeAsyncAction(entity, writer, ct);
}