namespace Npgsql.BackupRestore.DataDump;

internal interface IPgTable
{
    /// <summary>Table schema</summary>
    string Schema { get; }

    /// <summary>Table name</summary>
    string Table { get; }

    /// <summary>Table columns</summary>
    IReadOnlyList<string> Columns { get; }
}