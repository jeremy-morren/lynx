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
    /// <param name="configureOptions">Configure document store</param>
    /// <typeparam name="TContext">The underlying <see cref="DbContext"/> type</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddDocumentStore<TContext>(this IServiceCollection services,
        Action<DocumentStoreOptions>? configureOptions = null)
        where TContext : DbContext
    {
        services.AddTransient<IDocumentStore, DocumentStore<TContext>>();

        services.AddOptions<DocumentStoreOptions>()
            .Validate(o => o.Validate());
        if (configureOptions != null)
            services.Configure(configureOptions);

        return services;
    }

    /// <summary>
    /// Adds an <see cref="IDocumentStoreFactory"/> for the specified <typeparamref name="TContext"/> to the service collection
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureOptions">Configure document store</param>
    /// <typeparam name="TContext">The underlying <see cref="DbContext"/> type</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddDocumentStoreFactory<TContext>(this IServiceCollection services,
        Action<DocumentStoreOptions>? configureOptions = null)
        where TContext : DbContext
    {
        services.AddTransient<IDocumentStoreFactory, DocumentStoreFactory<TContext>>();

        services.AddOptions<DocumentStoreOptions>()
            .Validate(o => o.Validate());
        if (configureOptions != null)
            services.Configure(configureOptions);

        return services;
    }
}