using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Lynx.EfCore.Helpers;

internal static class EfCoreDbContextHelpers
{
    /// <summary>
    /// Gets the DbContext from the queryable
    /// </summary>
    public static DbContext GetDbContext<T>(this IQueryable<T> query)
    {
        if (query is IInfrastructure<IServiceProvider> infrastructure)
            return infrastructure.Instance.GetRequiredService<ICurrentDbContext>().Context;

        if (query.Provider is EntityQueryProvider provider)
            return GetContext(provider);

        throw new InvalidOperationException("Could not get DbContext from query");
    }

    private static DbContext GetContext(EntityQueryProvider provider)
    {
        var getContextFactory = _getQueryContextFactory ??= BuildGetQueryContextFactory(provider);
        var queryContext = getContextFactory(provider).Create();
        return queryContext.Context;
    }

    /// <summary>
    /// Gets query context factory from the query provider
    /// </summary>
    private static Func<EntityQueryProvider, IQueryContextFactory>? _getQueryContextFactory;

    private static Func<EntityQueryProvider, IQueryContextFactory> BuildGetQueryContextFactory(EntityQueryProvider queryProvider)
    {
        var queryCompilerField = queryProvider.GetType().GetInstanceField("_queryCompiler");
        var queryCompiler = queryCompilerField.GetValue(queryProvider)!;

        var queryContextFactoryField = queryCompiler.GetType().GetInstanceField("_queryContextFactory");

        var parameter = Expression.Parameter(typeof(EntityQueryProvider), "provider");
        Expression body = parameter;
        body = Expression.Convert(Expression.Field(body, queryCompilerField), queryCompiler.GetType());
        body = Expression.Convert(Expression.Field(body, queryContextFactoryField), typeof(IQueryContextFactory));
        return Expression.Lambda<Func<EntityQueryProvider, IQueryContextFactory>>(body, parameter).Compile();
    }

    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static FieldInfo GetInstanceField(this Type type, string name) =>
        type.GetField(name, InstanceFlags) ?? throw new InvalidOperationException($"Field {name} not found on {type}");

    private static PropertyInfo GetInstanceProperty(this Type type, string name) =>
        type.GetProperty(name, InstanceFlags) ?? throw new InvalidOperationException($"Property {name} not found on {type}");
}