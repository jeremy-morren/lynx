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
    /// Use this many parallel jobs to dump (<c>--jobs</c> option)
    /// </summary>
    public int? Jobs { get; set; }
    
    /// <summary>
    /// Verbose mode (<c>--verbose</c> switch)
    /// </summary>
    public bool? Verbose { get; set; }
    
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
    /// Include commands to create database objects (<c>--create</c> switch)
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
}

/// <summary>
/// Postgres backup format
/// </summary>
[PublicAPI]
public enum PgBackupFormat
{
    /// <summary>
    /// Output a plain-text SQL script file (the default).
    /// </summary>
    Plain,
    
    /// <summary>
    /// Output a custom-format archive suitable for input into pg_restore.
    /// </summary>
    /// <remarks>
    /// Together with the directory output format, this is the most flexible output format in that it allows
    /// manual selection and reordering of archived items during restore.
    /// This format is also compressed by default.
    /// </remarks>
    Custom,
    
    /// <summary>
    /// Output a directory-format archive suitable for input into pg_restore.
    /// </summary>
    /// <remarks>
    /// This will create a directory with one file for each table and large object being dumped,
    /// plus a so-called Table of Contents file describing the dumped objects in a machine-readable format that pg_restore can read.
    /// A directory format archive can be manipulated with standard Unix tools;
    /// for example, files in an uncompressed archive can be compressed with the gzip, lz4, or zstd tools.
    /// This format is compressed by default using gzip and also supports parallel dumps.
    /// </remarks>
    Directory,
    
    /// <summary>
    /// Output a tar-format archive suitable for input into pg_restore.
    /// </summary>
    /// <remarks>
    /// The tar format is compatible with the directory format: extracting a tar-format archive produces a valid directory-format archive.
    /// However, the tar format does not support compression.
    /// Also, when using tar format the relative order of table data items cannot be changed during restore.
    /// </remarks>
    Tar
}