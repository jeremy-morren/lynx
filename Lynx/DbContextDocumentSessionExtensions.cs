using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Lynx;

public static class DbContextDocumentSessionExtensions
{
    /// <summary>
    /// Creates a new document session for the database context.
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="isolationLevel">Optional: Transaction isolation level</param>
    /// <returns></returns>
    public static IDocumentSession CreateSession(this DbContext context, IsolationLevel? isolationLevel = null)
    {
        return new DocumentSession(context, isolationLevel, context.GetListener());
    }

    private static IDocumentSessionListener? GetListener(this DbContext context) =>
        ((IInfrastructure<IServiceProvider>)context).Instance.GetService<IDocumentSessionListener>();
}