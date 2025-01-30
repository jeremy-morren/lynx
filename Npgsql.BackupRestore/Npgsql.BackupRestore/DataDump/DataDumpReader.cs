using System.Buffers;
using System.Text;

namespace Npgsql.BackupRestore.DataDump;

/// <summary>
/// Reads a data dump from a stream created by <see cref="DataDumpWriter"/>.
/// </summary>
internal class DataDumpReader
{
    /// <summary>
    /// Underlying stream to read from.
    /// </summary>
    private readonly Stream _stream;

    /// <summary>
    /// Buffer used to read the length of the next segment.
    /// </summary>
    private readonly byte[] _lengthBuffer = new byte[sizeof(short)];

    public DataDumpReader(Stream stream)
    {
        _stream = stream;
    }

    public DataDumpStream CreateStream(int bufferSize) => new (_stream, bufferSize);

    /// <summary>
    /// Set to true when the end of the stream is reached.
    /// </summary>
    private bool _eof;

    /// <summary>
    /// Gets segment length from <see cref="_lengthBuffer"/>
    /// </summary>
    private int? GetSegmentLength(int read)
    {
        switch (read)
        {
            case 0:
                _eof = true;
                return null; // End of underlying stream, no more data
        }
        var length = BitConverterHelpers.ToInt16(_lengthBuffer);
        switch (length)
        {
            case > 0:
                return length;
            case 0:
                _eof = true;
                return null;
            default:
                throw new InvalidDataException("Invalid segment length");
        }
    }

    /// <summary>
    /// Reads a segment length from the stream
    /// </summary>
    /// <returns>
    /// The length of the next segment, or null if the end of the stream is reached.
    /// </returns>
    private async Task<int?> ReadSegmentLengthAsync(CancellationToken cancellationToken)
    {
        if (_eof)
            throw new EndOfStreamException();

        return GetSegmentLength(await _stream.ReadAsync(_lengthBuffer, cancellationToken));
    }

    /// <summary>
    /// Reads a segment length from the stream
    /// </summary>
    /// <returns>
    /// The length of the next segment, or null if the end of the stream is reached.
    /// </returns>
    private int? ReadSegmentLength()
    {
        if (_eof)
            throw new EndOfStreamException();

        return GetSegmentLength(_stream.Read(_lengthBuffer));
    }

    /// <summary>
    /// Reads a string from the stream, or null if the end of the stream is reached.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string?> ReadStringAsync(CancellationToken cancellationToken = default)
    {
        var length = await ReadSegmentLengthAsync(cancellationToken);
        if (length == null)
            return null; // EOF

        var array = ArrayPool<byte>.Shared.Rent(length.Value);
        var buffer = array.AsMemory(0, length.Value);
        try
        {
            await _stream.ReadExactlyAsync(buffer, cancellationToken);
            return GetString(buffer.Span);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    /// <summary>
    /// Reads a string from the stream, or null if the end of the stream is reached.
    /// </summary>
    public string? ReadString()
    {
        var length = ReadSegmentLength();
        if (length == null)
            return null; // EOF

        var array = ArrayPool<byte>.Shared.Rent(length.Value);
        var buffer = array.AsMemory(0, length.Value);
        try
        {
            _stream.ReadExactly(buffer.Span);
            return GetString(buffer.Span);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    private static string GetString(ReadOnlySpan<byte> bytes) => Encoding.UTF8.GetString(bytes);
}