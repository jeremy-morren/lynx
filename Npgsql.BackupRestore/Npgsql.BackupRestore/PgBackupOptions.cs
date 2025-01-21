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
    /// Compression method and/or compression level for compressed formats (<c>--compress</c> option). Format: <c>level</c> or <c>method:[detail]</c>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Specify the compression method and/or the compression level to use. The compression method can be set to <c>gzip</c>, <c>lz4</c>, <c>zstd</c>, or none for no compression.
    /// A compression detail string can optionally be specified. If the detail string is an integer, it specifies the compression level.
    /// Otherwise, it should be a comma-separated list of items, each of the form <c>keyword</c> or <c>keyword=value</c>.
    /// Currently, the supported keywords are <c>level</c> and <c>long</c>.
    /// </para>
    /// <para>
    /// If no compression level is specified (<see cref="string.Empty"/>), the default compression level will be used.
    /// If only a level is specified without mentioning an algorithm,
    ///<c>gzip</c> compression will be used if the level is greater than <c>0</c>, and no compression will be used if the level is <c>0</c>.
    /// </para>
    /// <para>
    /// For the custom and directory archive formats, this specifies compression of individual table-data segments,
    /// and the default is to compress using gzip at a moderate level.
    /// For <see cref="PgBackupFormat.Plain"/> output, setting a nonzero compression level causes the entire output file to be compressed,
    /// as though it had been fed through <c>gzip</c>, <c>lz4</c>, <c>zstd</c>; but the default is not to compress.
    /// With <c>zstd</c> compression, long mode may improve the compression ratio, at the cost of increased memory use.
    /// </para>
    /// <para>
    /// The <see cref="PgBackupFormat.Tar"/> archive format currently does not support compression at all.
    /// </para>
    /// </remarks>
    public string? Compression { get; set; }
    
    /// <summary>
    /// Dump only the data, not the schema (<c>--data-only</c> switch)
    /// </summary>
    /// <remarks>
    /// Dump only the data, not the schema (data definitions). Table data, large objects, and sequence values are dumped.
    /// </remarks>
    public bool DataOnly { get; set; }
    
    /// <summary>
    /// Include large objects in dump (<c>--blobs</c> switch)
    /// </summary>
    public bool IncludeBlobs { get; set; }
    
    /// <summary>
    /// Exclude large objects in dump (<c>--no-blobs</c> switch)
    /// </summary>
    public bool ExcludeBlobs { get; set; }
    
    /// <summary>
    /// Clean (drop) database objects before recreating them (<c>--clean</c> switch)
    /// </summary>
    /// <remarks>
    /// <para>
    /// Output commands to DROP all the dumped database objects prior to outputting the commands for creating them.
    /// This option is useful when the restore is to overwrite an existing database.
    /// If any of the objects do not exist in the destination database, ignorable error messages will be reported during restore,
    /// unless <see cref="UseIfExists"/> is <c>true</c>.
    /// </para>
    /// <para>
    /// This option is ignored when <see cref="Format"/> is not <see cref="PgBackupFormat.Plain"/>.
    /// For the archive formats, specify the option in <see cref="PgRestoreOptions.Clean"/>
    /// </para>
    /// </remarks>
    public bool Clean { get; set; }
    
    /// <summary>
    /// Include commands to create database in dump (<c>--create</c> switch)
    /// </summary>
    /// <remarks>
    /// <para>
    /// Begin the output with a command to create the database itself and reconnect to the created database.
    /// (With a script of this form, it doesn't matter which database in the destination installation you connect to before running the script.)
    /// If <see cref="Clean"/> is also specified, the script drops and recreates the target database before reconnecting to it.
    /// </para>
    /// <para>
    /// With <see cref="Create"/>, the output also includes the database's comment if any,
    /// and any configuration variable settings that are specific to this database, that is, any
    /// ALTER DATABASE ... SET ... and ALTER ROLE ... IN DATABASE ... SET ... commands that mention this database.
    /// Access privileges for the database itself are also dumped, unless <see cref="NoPrivileges"/> is specified.
    /// </para>
    /// <para>
    /// This option is ignored when <see cref="Format"/> is not <see cref="PgBackupFormat.Plain"/>.
    /// For the archive formats, specify the option in <see cref="PgRestoreOptions.Create"/>
    /// </para>
    /// </remarks>
    public bool Create { get; set; }
    
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
    public bool NoOwner { get; set; }
    
    /// <summary>
    /// Dump only the schema, no data (<c>--schema-only</c> switch)
    /// </summary>
    public bool SchemaOnly { get; set; }
    
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
    public bool NoPrivileges { get; set; }
    
    /// <summary>
    /// Use this many parallel jobs to dump (<c>--jobs</c> option)
    /// </summary>
    public int? Jobs { get; set; }
    
    /// <summary>
    /// Dump data as <c>INSERT</c> commands, rather than <c>COPY</c> (<c>--inserts</c> switch)
    /// </summary>
    /// <remarks>
    /// Dump data as <c>INSERT</c> commands (rather than <c>COPY</c>). This will make restoration very slow;
    /// it is mainly useful for making dumps that can be loaded into non-PostgreSQL databases.
    /// Any error during restoring will cause only rows that are part of the problematic <c>INSERT</c> to be lost,
    /// rather than the entire table contents. Note that the restore might fail altogether if you have rearranged column order.
    /// The <see cref="ColumnInserts"/> option is safe against column order changes, though even slower.
    /// </remarks>
    public bool Inserts { get; set; }
    
    /// <summary>
    /// Dump data as <c>INSERT</c> commands with column names, rather than <c>COPY</c> (<c>--column-inserts</c> switch)
    /// </summary>
    /// <remarks>
    /// Dump data as <c>INSERT</c> commands with explicit column names (<c>INSERT INTO table (column, ...) VALUES ...</c>).
    /// This will make restoration very slow; it is mainly useful for making dumps that can be loaded into non-PostgreSQL databases.
    /// Any error during restoring will cause only rows that are part of the problematic <c>INSERT</c> to be lost, rather than the entire table contents.
    /// </remarks>
    public bool ColumnInserts { get; set; }
    
    /// <summary>
    /// use <c>IF EXISTS</c> when dropping objects (<c>--if-exists</c> switch)
    /// </summary>
    public bool UseIfExists { get; set; }
    
    /// <summary>
    /// Verbose mode (<c>--verbose</c> switch)
    /// </summary>
    public bool Verbose { get; set; }
}