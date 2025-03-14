using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace Lynx.ExecutionPlan.Npgsql;

/// <summary>
/// Execution plan for a Npgsql command
/// </summary>
[DebuggerDisplay($"{{{nameof(DebugView)}}}")]
internal class NpgsqlExecutionPlan : IExecutionPlan
{
    public List<string> Nodes { get; }
    public string CommandText { get; }

    private NpgsqlExecutionPlan(string commandText, List<string> nodes)
    {
        CommandText = commandText;
        Nodes = nodes;
    }

    public string DebugView => Format(Nodes);

    IReadOnlyList<object> IExecutionPlan.Nodes => Nodes.Cast<object>().ToList();

    public static NpgsqlExecutionPlan Create(DbCommand command)
    {
        var sql = command.CommandText;
        command.CommandText = $"EXPLAIN {command.CommandText}";
        var result = new List<string>();
        using (var reader = command.ExecuteReader())
            while (reader.Read())
                result.Add(reader.GetString(0));
        command.CommandText = sql; // Reset
        return new NpgsqlExecutionPlan(sql, result);
    }

    private static string Format(List<string> nodes)
    {
        var sb = new StringBuilder();
        foreach (var node in nodes)
            sb.AppendLine(node);
        return sb.ToString();
    }
}