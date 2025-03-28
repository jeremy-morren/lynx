using Microsoft.EntityFrameworkCore;

namespace Lynx.Bulk;

public static class BulkServiceDbContextExtensions
{
    /// <summary>
    /// Create a bulk service for the given EF core context.
    /// </summary>
    /// <param name="context">The DB context</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static IBulkService CreateBulkService(this DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new BulkService(context);
    }
}