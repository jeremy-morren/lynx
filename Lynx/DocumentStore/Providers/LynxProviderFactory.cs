using System.Collections.Concurrent;
using Lynx.Providers.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.DocumentStore.Providers;

internal static class LynxProviderFactory
{
    /// <summary>
    /// Gets a <see cref="CachingLynxProvider"/> for the given <see cref="DbContext"/>.
    /// </summary>
    public static CachingLynxProvider GetProvider(DbContext context)
    {
        var efCoreProvider = context.Database.ProviderName!;
        var model = context.Model;
        return ProvidersCache.GetOrAdd((model, efCoreProvider),
            _ =>
            {
                var provider = CreateProvider(efCoreProvider);
                return new CachingLynxProvider(model, provider);
            });
    }
    
    private static readonly ConcurrentDictionary<(IModel, string), CachingLynxProvider> ProvidersCache = new();
    
    /// <summary>
    /// Creates a new <see cref="ILynxProvider"/> based on the EF Core provider.
    /// </summary>
    private static ILynxProvider CreateProvider(string efCoreProvider)
    {
        ArgumentNullException.ThrowIfNull(efCoreProvider);

        (string Assembly, string Type) typeName = efCoreProvider switch
        {
            "Microsoft.EntityFrameworkCore.Sqlite" =>
                ("Lynx.Provider.Sqlite", "Lynx.Provider.Sqlite.SqliteLynxProvider"),
            "Npgsql.EntityFrameworkCore.PostgreSQL" => 
                ("Lynx.Provider.Npgsql", "Lynx.Provider.Npgsql.NpgsqlLynxProvider"),
            _ => throw new NotSupportedException($"EF Core provider {efCoreProvider} is not supported.")
        };
        var type = Type.GetType($"{typeName.Type}, {typeName.Assembly}")
                   ?? throw new InvalidOperationException(
                       $"Could not find provider type. Add a reference to the {typeName.Assembly} package.");

        return (ILynxProvider)Activator.CreateInstance(type)!;
    }
}