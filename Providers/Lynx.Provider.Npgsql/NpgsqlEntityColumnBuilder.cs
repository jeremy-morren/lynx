using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.Providers.Common.Models;
using Lynx.Providers.Common.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using NpgsqlTypes;
// ReSharper disable InvertIf

namespace Lynx.Provider.Npgsql;

/// <summary>
/// Builds a delegate to create a row of values for a bulk insert.
/// </summary>
/// <typeparam name="TEntity">Entity Type</typeparam>
/// <typeparam name="TWriter">Npgsql binary writer type</typeparam>
internal static class NpgsqlEntityColumnBuilder<TEntity, TWriter>
{
    public static NpgsqlEntityColumn<TEntity, TWriter>[] GetColumnInfo(RootEntityInfo entity)
    {
        Debug.Assert(entity.Type.ClrType == typeof(TEntity));

        return GetKeys(entity).Concat(GetColumns(entity, [])).ToArray();
    }

    private static IEnumerable<NpgsqlEntityColumn<TEntity, TWriter>> GetColumns(
        IStructureEntity entity,
        ImmutableList<IEntityPropertyInfo> parent)
    {
        var scalarProps =
            from property in entity.ScalarProps
            let dbType = NpgsqlProviderDelegateBuilder.GetNpgsqlDbType(property.TypeMapping)
            let write = BuildWrite(parent.Add(property), dbType)
            let writeAsync = BuildWriteAsync(parent.Add(property), dbType)
            select new NpgsqlEntityColumn<TEntity, TWriter>(property, dbType, write, writeAsync);

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

    private static IEnumerable<NpgsqlEntityColumn<TEntity, TWriter>> GetOwned(OwnedEntityInfo owned, ImmutableList<IEntityPropertyInfo> path)
    {
        if (owned is not JsonOwnedEntityInfo json)
            return GetColumns(owned, path);

        // Json column, get raw value (Npgsql will handle serialization)
        const NpgsqlDbType dbType = NpgsqlDbType.Jsonb;
        var write = BuildWrite(path, dbType);
        var writeAsync = BuildWriteAsync(path, dbType);
        return [new NpgsqlEntityColumn<TEntity, TWriter>(json, dbType, write, writeAsync)];
    }

    private static IEnumerable<NpgsqlEntityColumn<TEntity, TWriter>> GetKeys(RootEntityInfo entity) =>
        from key in entity.Keys
        let dbType = NpgsqlProviderDelegateBuilder.GetNpgsqlDbType(key.TypeMapping)
        let write = BuildWrite([key], dbType)
        let writeAsync = BuildWriteAsync([key], dbType)
        select new NpgsqlEntityColumn<TEntity, TWriter>(key, dbType, write, writeAsync);

    private static Action<TEntity, TWriter> BuildWrite(ImmutableList<IEntityPropertyInfo> properties, NpgsqlDbType? dbType)
    {
        var entity = Expression.Parameter(typeof(TEntity), typeof(TEntity).Name.ToLowerInvariant());
        var writer = Expression.Parameter(typeof(TWriter), "writer");

        var (getValue, ifNotNull) = GetProperty(entity, properties);

        Expression expression;
        if (dbType != null)
        {
            var method = WriteWithTypeMethod.MakeGenericMethod(getValue.Type);
            var dbTypeValue = Expression.Constant(dbType.Value, typeof(NpgsqlDbType));
            expression = Expression.Call(writer, method, getValue, dbTypeValue);
        }
        else
        {
            var method = WriteWithoutTypeMethod.MakeGenericMethod(getValue.Type);
            expression = Expression.Call(writer, method, getValue);
        }

        if (ifNotNull != null)
        {
            //Call WriteNull if null
            var writeNull = Expression.Call(writer, WriteNullMethod);
            expression = Expression.IfThenElse(ifNotNull, expression, writeNull);
        }
        return Expression.Lambda<Action<TEntity, TWriter>>(expression, entity, writer).Compile();
    }

    private static Func<TEntity, TWriter, CancellationToken, Task> BuildWriteAsync(ImmutableList<IEntityPropertyInfo> properties, NpgsqlDbType? dbType)
    {
        var entity = Expression.Parameter(typeof(TEntity), typeof(TEntity).Name.ToLowerInvariant());
        var writer = Expression.Parameter(typeof(TWriter), "writer");
        var cancellationToken = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var (getValue, ifNotNull) = GetProperty(entity, properties);

        Expression expression;
        if (dbType != null)
        {
            var method = WriteAsyncWithTypeMethod.MakeGenericMethod(getValue.Type);
            var dbTypeValue = Expression.Constant(dbType.Value, typeof(NpgsqlDbType));
            expression = Expression.Call(writer, method, getValue, dbTypeValue, cancellationToken);
        }
        else
        {
            var method = WriteAsyncWithoutTypeMethod.MakeGenericMethod(getValue.Type);
            expression = Expression.Call(writer, method, getValue, cancellationToken);
        }

        if (ifNotNull != null)
        {
            //Call WriteNull if null
            var writeNull = Expression.Call(writer, WriteNullAsyncMethod, cancellationToken);
            expression = Expression.Condition(ifNotNull, expression, writeNull);
        }
        return Expression.Lambda<Func<TEntity, TWriter, CancellationToken, Task>>(expression, entity, writer, cancellationToken).Compile();
    }

    private static (Expression GetValue, Expression? IfNotNull) GetProperty(Expression input, ImmutableList<IEntityPropertyInfo> properties)
    {
        Debug.Assert(properties.Count > 0);

        var column = properties[0];

        Expression property = Expression.Property(input, column.PropertyInfo);
        if (column.Property is IProperty p && p.GetRelationalTypeMapping() is { Converter: {} converter })
            property = ConverterHelpers.InvokeConverter(converter, property);

        var ifNotNull = ExpressionHelpers.GetIfNotNull(property);

        // Get underlying value if Nullable<>
        property = ExpressionHelpers.GetNullableValue(property);

        if (properties.Count == 1)
            // Last property in chain
            return (property, ifNotNull);

        var (getNextValue, nextIfNotNull) = GetProperty(property, properties.RemoveAt(0));
        ifNotNull = CombineNullChecks(ifNotNull, nextIfNotNull);

        return (getNextValue, ifNotNull);
    }

    /// <summary>
    /// Combines two null checks into a single expression.
    /// </summary>
    private static Expression? CombineNullChecks(Expression? a, Expression? b)
    {
        Debug.Assert(a == null || a.Type == typeof(bool));
        Debug.Assert(b == null || b.Type == typeof(bool));
        if (a == null)
            return b;
        if (b == null)
            return a;
        return Expression.AndAlso(a, b);
    }

    #region Reflection

    private static readonly MethodInfo WriteWithTypeMethod = GetMethod<TWriter>(
        nameof(NpgsqlBinaryImporter.Write),
        true,
        l => l.Length == 2 && l[1].ParameterType == typeof(NpgsqlDbType));

    private static readonly MethodInfo WriteWithoutTypeMethod = GetMethod<TWriter>(
        nameof(NpgsqlBinaryImporter.Write),
        true,
        l => l.Length == 1);

    private static readonly MethodInfo WriteNullMethod = GetMethod<TWriter>(
        nameof(NpgsqlBinaryImporter.WriteNull),
        false,
        l => l.Length == 0);

    private static readonly MethodInfo WriteAsyncWithTypeMethod = GetMethod<TWriter>(
        nameof(NpgsqlBinaryImporter.WriteAsync),
        true,
        l => l.Length == 3 && l[1].ParameterType == typeof(NpgsqlDbType));

    private static readonly MethodInfo WriteAsyncWithoutTypeMethod =
        GetMethod<TWriter>(nameof(NpgsqlBinaryImporter.WriteAsync),
            true,
            l => l.Length == 2 && l[1].ParameterType == typeof(CancellationToken));

    private static readonly MethodInfo WriteNullAsyncMethod = GetMethod<TWriter>(
        nameof(NpgsqlBinaryImporter.WriteNullAsync),
        false,
        l => l.Length == 1 && l[0].ParameterType == typeof(CancellationToken));

    private static MethodInfo GetMethod<T>(string name, bool isGeneric, Func<ParameterInfo[], bool> checkParameters) =>
        typeof(T).GetMethods(ReflectionItems.InstanceFlags)
            .Single(m => m.Name == name
                         && m.IsGenericMethod == isGeneric
                         && checkParameters(m.GetParameters()));

    #endregion
}