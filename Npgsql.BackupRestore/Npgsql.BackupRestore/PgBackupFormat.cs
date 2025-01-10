using JetBrains.Annotations;

namespace Npgsql.BackupRestore;

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