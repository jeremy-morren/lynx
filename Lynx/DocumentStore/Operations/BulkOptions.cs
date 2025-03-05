using EFCore.BulkExtensions;

namespace Lynx.DocumentStore.Operations;

internal static class BulkOptions
{
    public static BulkConfig Config => new()
    {
        PreserveInsertOrder = true,
        SqlBulkCopyOptions = SqlBulkCopyOptions.KeepIdentity,
        UseTempDB = true,
    };
}