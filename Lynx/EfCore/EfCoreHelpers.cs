using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Lynx.EfCore;

internal static class EfCoreHelpers
{
    public static DbContext GetDbContext(this IQueryable query)
    {
        if (query is not IInfrastructure<IServiceProvider> src)
            throw new InvalidOperationException("Query is not an EF Core query");
        return src.Instance.GetRequiredService<ICurrentDbContext>().Context;
    }
    
    /// <summary>
    /// Gets the entity type from the model or throws.
    /// </summary>
    public static IEntityType GetEntityType(this IModel model, Type entityType) => 
        model.FindEntityType(entityType) ?? throw new InvalidOperationException($"Type {entityType} not registered in model");
}