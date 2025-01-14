using System.Text;
using Npgsql;

namespace Lynx.ExecutionPlan.Npgsql;

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

    public static NpgsqlExecutionPlan Create(NpgsqlCommand command)
    {
        var query = command.CommandText;
        command.CommandText = $"EXPLAIN {command.CommandText}";
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
            result.Add(reader.GetString(0));
        return new NpgsqlExecutionPlan(query, result);
    }

    private static string Format(List<string> nodes)
    {
        var sb = new StringBuilder();
        foreach (var node in nodes)
            sb.AppendLine(node);
        return sb.ToString();
    }
}