using Lynx.DocumentStore;
using Lynx.DocumentStore.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Lynx.EfCore;

internal static class EfCoreHelpers
{
    public static DbContext GetDbContext<T>(this IQueryable<T> query) where T : class
    {
        return query switch
        {
            LynxQueryable<T> q => q.Context,
            IInfrastructure<IServiceProvider> i => i.Instance.GetRequiredService<ICurrentDbContext>().Context,
            EntityQueryable<T> => throw new InvalidOperationException("Cannot get DbContext from EntityQueryable. Use query method from document store"),

            _ => throw new InvalidOperationException("Query is not an EF Core query")
        };
    }
    
    /// <summary>
    /// Gets the entity type from the model or throws.
    /// </summary>
    public static IEntityType GetEntityType(this IModel model, Type entityType) => 
        model.FindEntityType(entityType) ?? throw new InvalidOperationException($"Type {entityType} not registered in model");

    /// <summary>
    /// Gets the primary key of an entity type or throws.
    /// </summary>
    public static IKey GetPrimaryKey(this IEntityType entityType) =>
        entityType.FindPrimaryKey() ?? throw new InvalidOperationException($"Entity {entityType} does not have a primary key.");
}