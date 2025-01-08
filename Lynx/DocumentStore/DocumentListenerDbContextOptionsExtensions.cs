using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Lynx.DocumentStore;

public static class DocumentListenerDbContextOptionsExtensions
{
    /// <summary>
    /// Uses the specified document session listener with the database context.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="listener">The document listener to use</param>
    /// <typeparam name="TOptions"></typeparam>
    /// <returns></returns>
    public static TOptions UseDocumentListener<TOptions>(this TOptions options, IDocumentSessionListener listener)
        where TOptions : DbContextOptionsBuilder
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(listener);

        ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(new DocumentListenerDbContextOptionsExtension(listener));
        return options;
    }
    
    /// <summary>
    /// Configures the database context to use the document listener from the application services.
    /// </summary>
    /// <param name="options"></param>
    /// <typeparam name="TOptions"></typeparam>
    /// <returns></returns>
    public static TOptions UseDocumentListenerFromApplicationServices<TOptions>(this TOptions options)
        where TOptions : DbContextOptionsBuilder
    {
        ArgumentNullException.ThrowIfNull(options);

        return options;
    }
}