namespace Npgsql.BackupRestore.DataDump;

/// <summary>
/// A stream that reads table data from a dump created by <see cref="DataDumpWriter"/>
/// </summary>
internal class DataDumpStream : Stream
{
    /// <summary>
    /// Buffer for reading segment length header
    /// </summary>
    private readonly byte[] _lengthBuffer = new byte[sizeof(short)];

    /// <summary>
    /// Underlying stream
    /// </summary>
    private readonly Stream _underlying;

    public DataDumpStream(Stream underlying, int bufferSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

        _underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
        _buffer = new byte[bufferSize];
    }

    /// <summary>
    /// Bytes read by consumers so far
    /// </summary>
    private long _position;

    /// <summary>
    /// Total length of the current segment. When 0, stream is at EOF.
    /// </summary>
    private int _segmentLength = -1;

    /// <summary>
    /// Bytes read from the current segment so far.
    /// When less than <see cref="_segmentLength"/>, more data is available in the current segment.
    /// </summary>
    private int _readFromSegment = -1;

    /// <summary>
    /// Buffer for reading data
    /// </summary>
    private readonly byte[] _buffer;

    /// <summary>
    /// Total data in <see cref="_buffer"/>
    /// </summary>
    private int _bufferLength = -1;

    /// <summary>
    /// Position read within <see cref="_buffer"/>
    /// </summary>
    private int _bufferPos = -1;

    public override int Read(Span<byte> buffer)
    {
        if (_segmentLength == 0)
            return 0; // End of stream, nothing to read

        if (buffer.Length == 0)
            return 0; // Empty buffer, nothing to read

        var read = 0;
        while (read < buffer.Length)
        {
            // How much data can we read from the current segment?
            var canRead = Math.Min(buffer.Length - read, _bufferLength - _bufferPos);
            if (canRead > 0)
            {
                // There's data available, copy it to the user buffer
                _buffer.AsSpan(_bufferPos, canRead).CopyTo(buffer[read..]);
                read += canRead;
                _bufferPos += canRead;
                _position += canRead;
            }
            else
            {
                // We have read all data in _buffer
                _bufferPos = 0;

                if (_readFromSegment == _segmentLength)
                {
                    // We have read all data in the segment, read the next segment

                    _underlying.ReadExactly(_lengthBuffer);
                    _segmentLength = BitConverter.ToInt16(_lengthBuffer);
                    if (_segmentLength == 0)
                    {
                        // Zero-length segment signals end of stream
                        break;
                    }
                    _bufferLength = Math.Min(_segmentLength, _buffer.Length);
                    _underlying.ReadExactly(_buffer.AsSpan(0, _bufferLength));
                    _readFromSegment = _bufferLength;
                }
                else
                {
                    // More data is available in current segment (_buffer is smaller than segment)
                    _bufferLength = Math.Min(_segmentLength - _readFromSegment, _buffer.Length);
                    _underlying.ReadExactly(_buffer.AsSpan(0, _bufferLength));
                    _readFromSegment += _bufferLength;
                }
            }
        }
        _position += read;
        return read;
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_segmentLength == 0)
            return 0; // End of stream, nothing to read

        if (buffer.Length == 0)
            return 0; // Empty buffer, nothing to read

        var read = 0;
        while (read < buffer.Length)
        {
            // How much data can we read from the current segment?
            var canRead = Math.Min(buffer.Length - read, _bufferLength - _bufferPos);
            if (canRead > 0)
            {
                // There's data available, copy it to the user buffer
                _buffer.AsMemory(_bufferPos, canRead).CopyTo(buffer[read..]);
                read += canRead;
                _bufferPos += canRead;
                _position += canRead;
            }
            else
            {
                // We have read all data in _buffer
                _bufferPos = 0;

                if (_readFromSegment == _segmentLength)
                {
                    // We have read all data in the segment, read the next segment

                    await _underlying.ReadExactlyAsync(_lengthBuffer, cancellationToken);
                    _segmentLength = BitConverter.ToInt16(_lengthBuffer);
                    if (_segmentLength == 0)
                    {
                        // Zero-length segment signals end of stream
                        break;
                    }
                    _bufferLength = Math.Min(_segmentLength, _buffer.Length);
                    await _underlying.ReadExactlyAsync(_buffer.AsMemory(0, _bufferLength), cancellationToken);
                    _readFromSegment = _bufferLength;
                }
                else
                {
                    // More data is available in current segment (_buffer is smaller than segment)
                    _bufferLength = Math.Min(_segmentLength - _readFromSegment, _buffer.Length);
                    await _underlying.ReadExactlyAsync(_buffer.AsMemory(0, _bufferLength), cancellationToken);
                    _readFromSegment += _bufferLength;
                }
            }
        }
        _position += read;
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    #region Stream


    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;

    public override long Length => throw new InvalidOperationException();

    public override long Position
    {
        get => _position;
        set => throw new InvalidOperationException();
    }

    #endregion

    #region Not implemented

    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    public override void Write(ReadOnlySpan<byte> buffer) => throw new NotImplementedException();

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new InvalidOperationException();

    public override void SetLength(long value) => throw new InvalidOperationException();

    public override void Flush() => throw new InvalidOperationException();

    #endregion
}