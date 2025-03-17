using JetBrains.Annotations;

namespace Lynx.DocumentStore;

/// <summary>
/// Options for <see cref="IDocumentStore"/>
/// </summary>
[PublicAPI]
public class DocumentStoreOptions
{
    /// <summary>
    /// Whether bulk operations should be used for upsert if the provider supports them. Default is <c>false</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For insert operations, bulk operations are always used if the provider supports them.
    /// Bulk operations for upserts require additional permissions hence this option.
    /// </para>
    /// <para>
    /// Currently bulk operations are only supported by PostgreSQL.
    /// </para>
    /// <para>
    /// PostgreSQL bulk operations requires <c>CREATE TEMP TABLE</c> permission.
    /// </para>
    /// </remarks>
    public bool UseBulkOperationsForUpsert { get; set; }

    /// <summary>
    /// Minimum number of entities required to use bulk operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the number of entities is less than this value,
    /// bulk operations will not be used even if <see cref="UseBulkOperationsForUpsert"/> is <c>true</c>.
    /// </para>
    /// <para>
    /// Default is <c>100</c>.
    /// </para>
    /// </remarks>
    public int BulkOperationThreshold { get; set; } = 100;

    internal bool Validate()
    {
        if (BulkOperationThreshold < 1)
            throw new InvalidOperationException($"{nameof(BulkOperationThreshold)} must be greater than 0.");
        return true;
    }
}