using System.Buffers;
using System.Text;

namespace Npgsql.BackupRestore.DataDump;

/// <summary>
/// Writes a data dump to a stream
/// </summary>
/// <remarks>
/// Writes segments of data to the stream, each preceded by a 4-byte length header.
/// </remarks>
internal class DataDumpWriter
{
    private readonly Stream _stream;

    public DataDumpWriter(Stream stream)
    {
        _stream = stream;
    }

    #region Async

    /// <summary>
    /// Writes a segment containing a string to the stream.
    /// </summary>
    public async Task WriteStringAsync(string str, CancellationToken cancellationToken = default)
    {
        var bytes = GetBytes(str);
        await _stream.WriteAsync(BitConverterHelpers.GetBytes((short)bytes.Length), cancellationToken);
        await _stream.WriteAsync(bytes, cancellationToken);
    }

    /// <summary>
    /// Copies data from a stream to the dump stream.
    /// </summary>
    public async Task CopyFromStreamAsync(Stream stream, short segmentSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segmentSize);
        var rented = ArrayPool<byte>.Shared.Rent(segmentSize);
        var buffer = rented.AsMemory(0, segmentSize);

        try
        {
            while (true)
            {
                //Read as much as we can, up to segmentSize
                var read = await stream.ReadAtLeastAsync(buffer, segmentSize, false, cancellationToken);
                // Write segment length
                await _stream.WriteAsync(BitConverterHelpers.GetBytes((short)read), cancellationToken);
                if (read == 0)
                    break; // Zero-length segment signals end of stream
                // Write segment data
                await _stream.WriteAsync(buffer[..read], cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    #endregion

    #region Sync

    /// <summary>
    /// Writes a segment containing a string to the stream.
    /// </summary>
    public void WriteString(string str)
    {
        var bytes = GetBytes(str);
        _stream.Write(BitConverterHelpers.GetBytes((short)bytes.Length));
        _stream.Write(bytes);
    }

    /// <summary>
    /// Copies data from a stream to the dump stream.
    /// </summary>
    public void CopyFromStream(Stream stream, short bufferSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);
        var rented = ArrayPool<byte>.Shared.Rent(bufferSize);
        var buffer = rented.AsSpan(0, bufferSize);

        try
        {
            while (true)
            {
                var read = stream.ReadAtLeast(buffer, bufferSize, false);
                // Write segment length
                _stream.Write(BitConverterHelpers.GetBytes((short)read));
                if (read == 0)
                    break; // Zero-length segment signals end of stream
                // Write segment data
                _stream.Write(buffer[..read]);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    #endregion

    private static byte[] GetBytes(string str) => Encoding.UTF8.GetBytes(str);

}