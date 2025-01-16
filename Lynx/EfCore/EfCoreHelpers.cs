using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Lynx.EfCore;

internal static class EfCoreHelpers
{
    public static DbContext GetDbContext<T>(this IQueryable<T> query, string methodName) where T : class
    {
        return query switch
        {
            IInfrastructure<IServiceProvider> src => src.Instance.GetRequiredService<ICurrentDbContext>().Context,

            // If method is called after other methods e.g. AsNoTracking(),
            // then it isn't trivially possible to get the DbContext.
            // Throw a helpful exception in that case.
            EntityQueryable<T> => throw new InvalidOperationException(
                $"Cannot get DbContext. Ensure is called {methodName}() before any other extension methods."),

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