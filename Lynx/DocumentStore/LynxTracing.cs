using System.Diagnostics;

namespace Lynx.DocumentStore;

/// <summary>
/// Lynx tracing activity source
/// </summary>
public static class LynxTracing
{
    /// <summary>
    /// Lynx tracing activity source
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("Lynx.DocumentStore");
}