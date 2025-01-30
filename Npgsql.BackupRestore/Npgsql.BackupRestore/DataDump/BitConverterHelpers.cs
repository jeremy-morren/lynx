namespace Npgsql.BackupRestore.DataDump;

/// <summary>
/// Bit converter helpers that handle endianness
/// </summary>
internal static class BitConverterHelpers
{
    public static byte[] GetBytes(short value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    public static short ToInt16(ReadOnlySpan<byte> bytes)
    {
        if (BitConverter.IsLittleEndian)
            return BitConverter.ToInt16(bytes);

        var array = bytes.ToArray();
        Array.Reverse(array);
        return BitConverter.ToInt16(array);
    }
}