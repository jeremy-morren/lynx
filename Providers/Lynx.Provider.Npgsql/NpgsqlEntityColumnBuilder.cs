using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using Lynx.Providers.Common.Models;
using Lynx.Providers.Common.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NpgsqlTypes;

namespace Lynx.Provider.Npgsql;

/// <summary>
/// Builds a delegate to create a row of values for a bulk insert.
/// </summary>
internal static class NpgsqlEntityColumnBuilder<T>
{
    public static List<NpgsqlEntityColumn<T>> GetColumnInfo(RootEntityInfo entity)
    {
        Debug.Assert(entity.Type.ClrType == typeof(T));

        return GetKeys(entity).Concat(GetColumns(entity, [])).ToList();
    }

    private static IEnumerable<NpgsqlEntityColumn<T>> GetColumns(
        IStructureEntity entity,
        ImmutableList<IEntityPropertyInfo> parent)
    {
        var scalarProps =
            from property in entity.ScalarProps
            let getValue = BuildGetValue(parent.Add(property))
            let dbType = NpgsqlProviderDelegateBuilder.GetNpgsqlDbType(property.TypeMapping)
            select new NpgsqlEntityColumn<T>(property, dbType, getValue);

        var complexProps =
            from property in entity.ComplexProps
            from column in GetColumns(property, parent.Add(property))
            select column;

        var owned =
            from o in (entity as EntityInfo)?.Owned ?? []
            from column in GetOwned(o, parent.Add(o))
            select column;

        return scalarProps.Concat(complexProps).Concat(owned);
    }

    private static IEnumerable<NpgsqlEntityColumn<T>> GetOwned(OwnedEntityInfo owned, ImmutableList<IEntityPropertyInfo> path)
    {
        if (owned is not JsonOwnedEntityInfo json)
            return GetColumns(owned, path);

        // Json column, get raw value (Npgsql will handle serialization)
        var getValue = BuildGetValue(path);
        return [new NpgsqlEntityColumn<T>(json, NpgsqlDbType.Jsonb, getValue)];
    }

    private static IEnumerable<NpgsqlEntityColumn<T>> GetKeys(RootEntityInfo entity) =>
        from key in entity.Keys
        let getValue = BuildGetValue([key])
        let dbType = NpgsqlProviderDelegateBuilder.GetNpgsqlDbType(key.TypeMapping)
        select new NpgsqlEntityColumn<T>(key, dbType, getValue);

    private static Func<T, object?> BuildGetValue(ImmutableList<IEntityPropertyInfo> properties)
    {
        // Build lambda chain
        var parameter = Expression.Parameter(typeof(T), typeof(T).Name.ToLowerInvariant());

        var chain = GetProperty(parameter, properties);
        if (chain.Type != typeof(object))
            // Convert result to object
            chain = Expression.Convert(chain, typeof(object));

        return Expression.Lambda<Func<T, object?>>(chain, parameter).Compile();
    }

    private static Expression GetProperty(Expression input, ImmutableList<IEntityPropertyInfo> properties)
    {
        Debug.Assert(properties.Count > 0);

        var column = properties[0];

        Expression property = Expression.Property(input, column.PropertyInfo);
        if (column.Property is IProperty p && p.GetRelationalTypeMapping() is { Converter: {} converter })
            property = ConverterHelpers.InvokeConverter(converter, property);

        if (properties.Count == 1)
            // Last property in chain
            return property;

        var next = GetProperty(property, properties.RemoveAt(0));

        // Check if property is null, before invoking next property
        var ifNotNull = ExpressionHelpers.GetIfNotNull(property);
        if (ifNotNull == null)
            return next; // Property is not nullable, no null check needed

        if (!next.Type.IsValueType || Nullable.GetUnderlyingType(next.Type) != null)
        {
            //Return value of next is a reference type or Nullable<>, so we don't need to convert the result
            return Expression.Condition(ifNotNull,
                next,
                Expression.Constant(null, next.Type));
        }

        Debug.Assert(Nullable.GetUnderlyingType(next.Type) == null);

        //Next is not Nullable<>, so we have to convert result to Nullable<>
        var nullable = typeof(Nullable<>).MakeGenericType(next.Type);
        return Expression.Condition(ifNotNull,
            Expression.Convert(next, nullable),
            Expression.Constant(null, nullable));
    }
}