using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lynx.DocumentStore;

[PublicAPI]
public static class DocumentStoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds an <see cref="IDocumentStore"/> for the specified <typeparamref name="TContext"/> to the service collection
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TContext">The underlying <see cref="DbContext"/> type</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddDocumentStore<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddTransient<IDocumentStore, DocumentStore<TContext>>();
        return services;
    }

    /// <summary>
    /// Adds an <see cref="IDocumentStoreFactory"/> for the specified <typeparamref name="TContext"/> to the service collection
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TContext">The underlying <see cref="DbContext"/> type</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddDocumentStoreFactory<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddTransient<IDocumentStoreFactory, DocumentStoreFactory<TContext>>();
        return services;
    }
}