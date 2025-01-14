using System.Data;
using System.Data.Common;
using System.Text;
using JetBrains.Annotations;

namespace Lynx.ExecutionPlan.Sqlite;

[PublicAPI]
internal class SqliteExecutionPlanNode
{
    public int Id { get; }
    public int? ParentId { get; }
    public bool NotUsed { get; }
    public string Detail { get; }
    public SqliteExecutionPlanNode? Parent { get; set; }

    public IReadOnlyList<SqliteExecutionPlanNode> Children { get; set; } = null!;

    public SqliteExecutionPlanNode(DbDataReader reader)
    {
        Id = reader.GetInt32("id");
        ParentId = reader.GetInt32("parent");
        NotUsed = reader.GetBoolean("notUsed");
        Detail = reader.GetString("detail");
        if (ParentId == 0)
            ParentId = null;
    }

    public override string ToString() => new
        {
            Id,
            Detail,
            Children = Children.Count,
            NotUsed
        }
        .ToString()!;

    /// <summary>
    /// Writes the node to the string
    /// </summary>
    /// <param name="sb">String builder</param>
    /// <param name="indentLevel">Current indent level</param>
    /// <param name="maxDetailLength">Length of the longest detail string (for padding)</param>
    public void Write(StringBuilder sb, int indentLevel, int maxDetailLength)
    {
        var indent = new string(' ', indentLevel * IndentChars);

        var id = Id.ToString().PadLeft(6, ' ');
        var detail = Detail.PadRight(maxDetailLength - (indentLevel * IndentChars), ' ');

        const string notUsedMsg = " (Not Used)";
        var notUsed = NotUsed ? notUsedMsg : new string(' ', notUsedMsg.Length);
        sb.AppendLine($"{indent}{detail}{id}{notUsed}");
        foreach (var c in Children)
            c.Write(sb, indentLevel + 1, maxDetailLength);
    }

    /// <summary>
    /// Get the maximum detail length (including indent)
    /// </summary>
    public int GetMaxDetailLength(int indentLevel)
    {
        return Children
            .Select(c => c.GetMaxDetailLength(indentLevel + 1))
            .Append(Detail.Length + (IndentChars * indentLevel))
            .Max();
    }

    private const int IndentChars = 4;
}