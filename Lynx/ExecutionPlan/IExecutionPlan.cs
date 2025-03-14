namespace Lynx.ExecutionPlan;

/// <summary>
/// A database execution plan
/// </summary>
public interface IExecutionPlan
{
    /// <summary>
    /// Display view of the execution plan
    /// </summary>
    string DebugView { get; }

    /// <summary>
    /// Nodes in the execution plan
    /// </summary>
    IReadOnlyList<object> Nodes { get; }

    /// <summary>
    /// SQL command text used to generate the execution plan
    /// </summary>
    string CommandText { get; }
}