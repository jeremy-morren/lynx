using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.DocumentStore.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Lynx.EfCore.Helpers;

internal static class EfCoreQueryableHelpers
{
    public static DbContext GetDbContext<T>(this IQueryable<T> query) where T : class
    {
        if (query is IInfrastructure<IServiceProvider> i)
            return i.Instance.GetRequiredService<ICurrentDbContext>().Context;

        if (query.Provider is EntityQueryProvider provider)
            return GetContext(provider);

        throw new InvalidOperationException("Could not get DbContext from query");
    }

    private static DbContext GetContext(EntityQueryProvider provider)
    {
        var getContext = _getContextFromQueryCompiler ??= BuildGetContextFromEntityQueryProvider(provider);
        return getContext(provider);
    }

    private static Func<EntityQueryProvider, DbContext>? _getContextFromQueryCompiler;

    private static Func<EntityQueryProvider, DbContext> BuildGetContextFromEntityQueryProvider(EntityQueryProvider queryProvider)
    {
        var queryCompilerField = GetField(queryProvider.GetType(), "_queryCompiler");
        var queryCompiler = queryCompilerField.GetValue(queryProvider)!;

        var queryContextFactoryField = GetField(queryCompiler.GetType(), "_queryContextFactory");
        var queryContextFactory = queryContextFactoryField.GetValue(queryCompiler)!;

        var dependenciesProperty = GetProperty(queryContextFactory.GetType(), "Dependencies");
        var dependencies = dependenciesProperty.GetValue(queryContextFactory)!;

        var stateManagerProperty = GetProperty(dependencies.GetType(), "StateManager");
        var stateManager = stateManagerProperty.GetValue(dependencies)!;

        var contextProperty = GetProperty(stateManager.GetType(), "Context");

        // Now build the expression with casts
        var parameter = Expression.Parameter(typeof(EntityQueryProvider), "provider");

        Expression body = parameter;
        body = Expression.Convert(Expression.Field(body, queryCompilerField), queryCompiler.GetType());
        body = Expression.Convert(Expression.Field(body, queryContextFactoryField), queryContextFactory.GetType());
        body = Expression.Convert(Expression.Property(body, dependenciesProperty), dependencies.GetType());
        body = Expression.Convert(Expression.Property(body, stateManagerProperty), stateManager.GetType());

        body = Expression.Property(body, contextProperty);
        return Expression.Lambda<Func<EntityQueryProvider, DbContext>>(body, parameter).Compile();
    }

    private  const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static FieldInfo GetField(Type type, string name) =>
        type.GetField(name, InstanceFlags) ?? throw new InvalidOperationException($"Field {name} not found on {type}");

    private static PropertyInfo GetProperty(Type type, string name) =>
        type.GetProperty(name, InstanceFlags) ?? throw new InvalidOperationException($"Property {name} not found on {type}");
}