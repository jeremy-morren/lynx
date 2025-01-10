using JetBrains.Annotations;

namespace Npgsql.BackupRestore;

/// <summary>
/// Options for <c>pg_restore</c> tool
/// </summary>
/// <remarks>
/// See https://www.postgresql.org/docs/current/app-pgrestore.html
/// </remarks>
[PublicAPI]
public class PgRestoreOptions
{
    /// <summary>
    /// Connect to this database (<c>--dbname</c> option)
    /// </summary>
    public string? Database { get; set; }
    
    /// <summary>
    /// backup file format (should be determined automatically) (<c>--format</c> option)
    /// </summary>
    public PgBackupFormat? Format { get; set; }
    
    /// <summary>
    /// Restore only the data, not the schema (<c>--data-only</c> switch)
    /// </summary>
    public bool? DataOnly { get; set; }
    
    /// <summary>
    /// Clean (drop) database objects before recreating them (<c>--clean</c> switch)
    /// </summary>
    public bool? Clean { get; set; }
    
    /// <summary>
    /// Create the target database (<c>--create</c> switch)
    /// </summary>
    public bool? Create { get; set; }
    
    /// <summary>
    /// Exit on error, default is to continue (<c>--exit-on-error</c> switch)
    /// </summary>
    public bool? ExitOnError { get; set; }
    
    /// <summary>
    /// Index to restore (<c>--index=NAME</c> option)
    /// </summary>
    public string? Index { get; set; }
    
    /// <summary>
    /// Restore only objects in the specified schema (<c>--schema=NAME</c> option)
    /// </summary>
    public string? Schema { get; set; }
    
    /// <summary>
    /// Do not restore objects in the specified schema (<c>--exclude-schema=NAME</c> option)
    /// </summary>
    public string? ExcludeSchema { get; set; }
    
    /// <summary>
    /// Restore named relation (table, view, etc.) only (<c>--table=NAME</c> option)
    /// </summary>
    public string? Table { get; set; }
    
    /// <summary>
    /// Restore named trigger (<c>--trigger=NAME</c> option)
    /// </summary>
    public string? Trigger { get; set; }
    
    /// <summary>
    /// Restore named function (<c>--function=NAME</c> option)
    /// </summary>
    public string? Function { get; set; }
    
    /// <summary>
    /// Skip restoration of object ownership (<c>--no-owner</c> switch)
    /// </summary>
    public bool? NoOwner { get; set; }
    
    /// <summary>
    /// Skip restoration of access privileges (grant/revoke) (<c>--no-privileges</c> switch)
    /// </summary>
    public bool? NoPrivileges { get; set; }
    
    /// <summary>
    /// Restore as a single transaction (<c>--single-transaction</c> switch)
    /// </summary>
    public bool? SingleTransaction { get; set; }
    
    /// <summary>
    /// Use this many parallel jobs to restore (<c>--jobs</c> option)
    /// </summary>
    public int? Jobs { get; set; }
    
    /// <summary>
    /// Verbose mode (<c>--verbose</c> switch)
    /// </summary>
    public bool? Verbose { get; set; }
}