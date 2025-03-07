using Npgsql;
using NpgsqlTypes;

namespace Lynx.Provider.Npgsql;

internal class NpgsqlBulkCopy
{
    public void Write()
    {
        NpgsqlConnection conn = null!;

        using var writer = conn.BeginBinaryImport("COPY table_name (column1, column2) FROM STDIN (FORMAT BINARY)");
        writer.StartRow();
        writer.Write("HEllo", NpgsqlDbType.Bigint);
        writer.WriteNull();

    }
}