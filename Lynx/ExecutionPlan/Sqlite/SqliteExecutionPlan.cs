using System.Text;
using Microsoft.Data.Sqlite;

namespace Lynx.ExecutionPlan.Sqlite;

internal class SqliteExecutionPlan : IExecutionPlan
{
    public string CommandText { get; }

    public List<SqliteExecutionPlanNode> Nodes { get; }

    private SqliteExecutionPlan(string commandText, IEnumerable<SqliteExecutionPlanNode> nodes)
    {
        CommandText = commandText;
        Nodes = nodes.ToList();
    }

    public string DebugView => CreateDebugView(Nodes);

    IReadOnlyList<object> IExecutionPlan.Nodes => Nodes.Cast<object>().ToList();

    public static SqliteExecutionPlan Create(SqliteCommand cmd)
    {
        var query = cmd.CommandText;
        cmd.CommandText = $"EXPLAIN QUERY PLAN {cmd.CommandText}";

        using var reader = cmd.ExecuteReader();

        var nodes = new List<SqliteExecutionPlanNode>();
        while (reader.Read())
            nodes.Add(new SqliteExecutionPlanNode(reader));

        foreach (var node in nodes)
        {
            if (node.ParentId != null)
                node.Parent = nodes.Single(x => x.Id == node.ParentId);

            node.Children = nodes.Where(x => x.ParentId == node.Id).ToList();
        }

        return new SqliteExecutionPlan(query, nodes.Where(p => p.Parent == null));
    }

    private static string CreateDebugView(List<SqliteExecutionPlanNode> nodes)
    {
        var maxDetailLength = nodes.Select(n => n.GetMaxDetailLength(0)).Max();
        var sb = new StringBuilder();
        foreach (var n in nodes)
            n.Write(sb, 0, maxDetailLength);
        return sb.ToString();
    }
}