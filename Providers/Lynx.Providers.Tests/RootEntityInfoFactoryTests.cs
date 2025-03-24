using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Lynx.Providers.Common.Entities;
using Lynx.Providers.Common.Models;
using Lynx.Providers.Tests.Npgsql;
using Lynx.Providers.Tests.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NodaTime;

namespace Lynx.Providers.Tests;

public class RootEntityInfoFactoryTests
{
    internal static RootEntityInfo CreateRootEntity(Type type, IModel model)
    {
        type.IsValueType.ShouldBeFalse();

        var method = typeof(RootEntityInfoFactory)
            .GetMethod(nameof(RootEntityInfoFactory.Create), BindingFlags.Static | BindingFlags.Public)
            .ShouldNotBeNull();

        method = method.MakeGenericMethod(type);
        return method.Invoke(null, [model]).ShouldNotBeNull().ShouldBeAssignableTo<RootEntityInfo>();
    }

    [Theory]
    [InlineData(typeof(Customer), nameof(Customer.Id))]
    [InlineData(typeof(City), nameof(City.Id))]
    [InlineData(typeof(ConverterEntity), nameof(ConverterEntity.Id1), nameof(ConverterEntity.Id2))]
    public void TestEntityInfoFactory(Type entityType, params string[] keys)
    {
        using var sqliteHarness = new SqliteTestHarness();
        using var npgsqlHarness = new NpgsqlTestHarness([nameof(TestEntityInfoFactory), entityType.Name]);

        var contexts = new[]
        {
            sqliteHarness.CreateContext(),
            npgsqlHarness.CreateContext()
        };
        foreach (var context in contexts)
        {
            var info = CreateRootEntity(entityType, context.Model);

            info.Keys.Should().AllSatisfy(k => k.ColumnName.Should().HaveCount(1));
            info.Keys.Select(k => k.ColumnName.SqlColumnName).Should().BeEquivalentTo(keys);

            keys.Should().AllSatisfy(k => info.ScalarProps.Should().NotContain(p => p.Property.Name == k));

            Verify(info, null, keys, context.Model);
        }
    }

    private static void Verify(EntityInfo info, PropertyChain? baseColumn, string[]? keys, IModel model)
    {
        info.ScalarProps.Should().AllSatisfy(p => p.PropertyInfo.Should().NotBeNull());
        info.ComplexProps.Should().AllSatisfy(p => p.PropertyInfo.Should().NotBeNull());

        info.Type.ClrType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => keys == null || !keys.Contains(p.Name))
            .Should()
            .AllSatisfy(p =>
            {
                if (model.FindEntityType(p.PropertyType) != null)
                    return; // Ignore navigation properties

                IEntityPropertyInfo entityProp;
                if (IsSimpleType(p.PropertyType))
                {
                    entityProp = info.ScalarProps.Where(x => x.PropertyInfo == p).ShouldHaveSingleItem();
                }
                else if (IsComplexType(p))
                {
                    entityProp = info.ComplexProps.Where(x => x.PropertyInfo == p).ShouldHaveSingleItem();
                }
                else if (IsOwnedType(p))
                {
                    var owned = info.Owned.Where(x => x.PropertyInfo == p).ShouldHaveSingleItem();
                    Verify(owned, owned.ColumnName, null, model);
                    entityProp = owned;
                }
                else if (IsSimpleArray(p.PropertyType))
                {
                    //Array of simple types, mapped to single column
                    entityProp = info.ScalarProps.Where(x => x.PropertyInfo == p).ShouldHaveSingleItem();
                }
                else
                {
                    throw new NotImplementedException($"Unknown property {p}. Type {p.PropertyType}");
                }

                if (baseColumn != null)
                    entityProp.ColumnName.Should().StartWith(baseColumn);
                else
                    entityProp.ColumnName.ShouldHaveSingleItem();

                if (HasColumnName(p, out var columnName))
                    entityProp.ColumnName[^1].ShouldBe(columnName);
            });
    }

    private static bool IsSimpleType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive

            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(Guid)

            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(DateOnly)
            
            || type == typeof(Instant)
            || type == typeof(LocalDate)
            || type == typeof(LocalDateTime)

            || type.IsEnum

            || type.IsAssignableTo(typeof(IStrongId));
    }

    private static bool IsComplexType(PropertyInfo property)
    {
        var type = property.PropertyType;

        if (type.IsValueType && type.IsAssignableTo(typeof(IEquatable<>).MakeGenericType(type)))
            //Record struct, treat as complex type
            return true;

        //Check for ComplexTypeAttribute
        return property.GetCustomAttribute<ComplexTypeAttribute>() != null ||
               type.GetCustomAttribute<ComplexTypeAttribute>() != null;
    }

    private static bool IsOwnedType(PropertyInfo property)
    {
        var type = property.PropertyType;
        //If type is enumerable, get the element type
        if (type.IsAssignableTo(typeof(IEnumerable)))
        {
            type = type.GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .GetGenericArguments()[0];
        }

        //Check for OwnedAttribute on property or type
        return property.GetCustomAttribute<OwnedAttribute>() != null ||
               type.GetCustomAttribute<OwnedAttribute>() != null;
    }

    private static bool IsSimpleArray(Type type) =>
        type.IsArray && IsSimpleType(type.GetElementType().ShouldNotBeNull());

    private static bool HasColumnName(PropertyInfo property, [MaybeNullWhen(false)] out string columnName)
    {
        var attribute = property.GetCustomAttribute<ColumnAttribute>();
        columnName = attribute?.Name;
        return attribute != null;
    }
}