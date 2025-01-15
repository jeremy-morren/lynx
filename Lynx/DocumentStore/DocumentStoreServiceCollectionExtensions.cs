using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lynx.DocumentStore;

public static class DocumentStoreServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentStore<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddTransient<IDocumentStore, DocumentStore<TContext>>();
        return services;
    }
}