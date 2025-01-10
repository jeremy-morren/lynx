using JetBrains.Annotations;

namespace Npgsql.BackupRestore;

/// <summary>
/// Options for <c>pg_dump</c> tool
/// </summary>
/// <remarks>
/// See https://www.postgresql.org/docs/current/app-pgdump.html
/// </remarks>
[PublicAPI]
public class PgBackupOptions
{
    /// <summary>
    /// Output file or directory name (<c>--file</c> option)
    /// </summary>
    public string? FileName { get; set; }
    
    /// <summary>
    /// Output file format (<c>--format</c> option)
    /// </summary>
    public PgBackupFormat? Format { get; set; }
    
    /// <summary>
    /// Compression level for compressed formats (<c>--compress</c> option)
    /// </summary>
    public int? Compression { get; set; }
    
    /// <summary>
    /// Dump only the data, not the schema (<c>--data-only</c> switch)
    /// </summary>
    public bool? DataOnly { get; set; }
    
    /// <summary>
    /// Include large objects in dump (<c>--blobs</c> switch)
    /// </summary>
    public bool? IncludeBlobs { get; set; }
    
    /// <summary>
    /// Exclude large objects in dump (<c>--no-blobs</c> switch)
    /// </summary>
    public bool? ExcludeBlobs { get; set; }
    
    /// <summary>
    /// Clean (drop) database objects before recreating them (<c>--clean</c> switch)
    /// </summary>
    public bool? Clean { get; set; }
    
    /// <summary>
    /// Include commands to create database in dump (<c>--create</c> switch)
    /// </summary>
    public bool? Create { get; set; }
    
    /// <summary>
    /// Dump the data in this encoding (<c>--encoding</c> option)
    /// </summary>
    public string? Encoding { get; set; }
    
    /// <summary>
    /// Dump the specified schema(s) only (<c>--schema=PATTERN</c> option)
    /// </summary>
    public string? Schema { get; set; }
    
    /// <summary>
    /// Do NOT dump the specified schema(s) (<c>--exclude-schema=PATTERN</c> option)
    /// </summary>
    public string? ExcludeSchema { get; set; }
    
    /// <summary>
    /// Skip restoration of object ownership in plain-text format (<c>--no-owner</c> switch)
    /// </summary>
    public bool? NoOwner { get; set; }
    
    /// <summary>
    /// Dump only the schema, no data (<c>--schema-only</c> switch)
    /// </summary>
    public bool? SchemaOnly { get; set; }
    
    /// <summary>
    /// Dump the specified table(s) only (<c>--table=PATTERN</c> option)
    /// </summary>
    public string? Table { get; set; }
    
    /// <summary>
    /// Do NOT dump the specified table(s) (<c>--exclude-table=PATTERN</c> option)
    /// </summary>
    public string? ExcludeTable { get; set; }
    
    /// <summary>
    /// Do not dump privileges (grant/revoke) (<c>--no-privileges</c> switch)
    /// </summary>
    public bool? NoPrivileges { get; set; }
    
    /// <summary>
    /// Use this many parallel jobs to dump (<c>--jobs</c> option)
    /// </summary>
    public int? Jobs { get; set; }
    
    /// <summary>
    /// Dump data as INSERT commands, rather than COPY (<c>--inserts</c> switch)
    /// </summary>
    public bool? Inserts { get; set; }
    
    /// <summary>
    /// Dump data as INSERT commands with column names, rather than COPY (<c>--column-inserts</c> switch)
    /// </summary>
    public bool? ColumnInserts { get; set; }
    
    /// <summary>
    /// Verbose mode (<c>--verbose</c> switch)
    /// </summary>
    public bool? Verbose { get; set; }
}