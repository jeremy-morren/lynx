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
    /// <remarks>
    /// Restore only the data, not the schema (data definitions).
    /// Table data, large objects, and sequence values are restored, if present in the archive.
    /// </remarks>
    public bool DataOnly { get; set; }
    
    /// <summary>
    /// Clean (drop) database objects before recreating them (<c>--clean</c> switch)
    /// </summary>
    /// <remarks>
    /// Before restoring database objects, issue commands to <c>DROP</c> all the objects that will be restored. This option is useful for overwriting an existing database.
    /// If any of the objects do not exist in the destination database, ignorable error messages will be reported, unless <see cref="IfExists"/> is also specified.
    /// </remarks>
    public bool Clean { get; set; }
    
    /// <summary>
    /// use <c>IF EXISTS</c> when dropping objects (<c>--if-exists</c> switch)
    /// </summary>
    /// <remarks>
    /// Use <c>DROP ... IF EXISTS</c> commands to drop objects if <see cref="Clean"/> is specified.
    /// This suppresses “does not exist” errors that might otherwise be reported.
    /// This option is not valid unless <see cref="Clean"/> is specified.
    /// </remarks>
    public bool IfExists { get; set; }
    
    /// <summary>
    /// Create the target database (<c>--create</c> switch)
    /// </summary>
    /// <remarks>
    /// <para>
    /// Create the database before restoring into it.
    /// If <see cref="Clean"/> is also specified, drop and recreate the target database before connecting to it.
    /// </para>
    /// <para>
    /// With <see cref="Create"/>, pg_restore also restores the database's comment if any, and any configuration variable settings that are specific to this database, that is,
    /// any ALTER DATABASE ... SET ... and ALTER ROLE ... IN DATABASE ... SET ... commands that mention this database.
    /// Access privileges for the database itself are also restored, unless <see cref="NoPrivileges"/> is specified.
    /// </para>
    /// <para>
    /// When this option is used, the database in <see cref="Database"/> is used only to issue the initial
    /// DROP DATABASE and CREATE DATABASE commands. All data is restored into the database name that appears in the archive.
    /// </para>
    /// </remarks>
    public bool Create { get; set; }
    
    /// <summary>
    /// Exit on error, default is to continue (<c>--exit-on-error</c> switch)
    /// </summary>
    /// <remarks>
    /// Exit if an error is encountered while sending SQL commands to the database.
    /// The default is to continue and to display a count of errors at the end of the restoration.
    /// </remarks>
    public bool ExitOnError { get; set; }
    
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
    public bool NoOwner { get; set; }
    
    /// <summary>
    /// Skip restoration of access privileges (grant/revoke) (<c>--no-privileges</c> switch)
    /// </summary>
    public bool NoPrivileges { get; set; }
    
    /// <summary>
    /// Restore as a single transaction (<c>--single-transaction</c> switch)
    /// </summary>
    public bool SingleTransaction { get; set; }
    
    /// <summary>
    /// Use this many parallel jobs to restore (<c>--jobs</c> option)
    /// </summary>
    public int? Jobs { get; set; }
    
    /// <summary>
    /// Verbose mode (<c>--verbose</c> switch)
    /// </summary>
    public bool Verbose { get; set; }
}