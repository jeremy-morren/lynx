using System.Collections;

namespace Lynx.DocumentStore;

/// <summary>
/// Represents a unit of work (a collection of operations to be applied to the database).
/// </summary>
internal class UnitOfWork : IReadOnlyList<IDocumentSessionOperation>
{
    private readonly List<IDocumentSessionOperation> _operations = [];

    public void Add(IDocumentSessionOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _operations.Add(operation);
    }

    public void Reset() => _operations.Clear();

    #region IReadOnlyList<ILynxOperation>
    
    public IEnumerator<IDocumentSessionOperation> GetEnumerator() => _operations.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _operations.GetEnumerator();

    public int Count => _operations.Count;

    public IDocumentSessionOperation this[int index] => _operations[index];
    
    #endregion
}