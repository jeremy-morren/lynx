using System.Diagnostics;

namespace Lynx.DocumentStore;

public class LynxTracing
{
    public static readonly ActivitySource ActivitySource = new("Lynx.DocumentStore");
}