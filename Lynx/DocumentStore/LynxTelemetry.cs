using System.Diagnostics;

namespace Lynx.DocumentStore;

public class LynxTelemetry
{
    public static readonly ActivitySource ActivitySource = new("Lynx.DocumentStore");
}