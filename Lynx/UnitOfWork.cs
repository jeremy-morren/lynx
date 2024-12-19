using System.Collections;

namespace Lynx;

/// <summary>
/// Represents a unit of work (a collection of operations to be applied to the database).
/// </summary>
internal class UnitOfWork : IReadOnlyList<IDocumentSessionOperations>
{
    private readonly List<IDocumentSessionOperations> _operations = [];

    public void Add(IDocumentSessionOperations operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _operations.Add(operation);
    }

    public void Reset() => _operations.Clear();

    #region IReadOnlyList<ILynxOperation>
    
    public IEnumerator<IDocumentSessionOperations> GetEnumerator() => _operations.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _operations.GetEnumerator();

    public int Count => _operations.Count;

    public IDocumentSessionOperations this[int index] => _operations[index];
    
    #endregion
}