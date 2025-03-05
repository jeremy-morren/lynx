using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using FluentAssertions;
using Lynx.Provider.Common;
using Lynx.Provider.Common.Models;
using Lynx.Providers.Tests.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Shouldly;

namespace Lynx.Providers.Tests;

public class EntityInfoFactoryTests
{
    [Theory]
    [InlineData(typeof(Customer), nameof(Customer.Id))]
    [InlineData(typeof(City), nameof(City.Id))]
    public void TestEntityInfoFactory(Type entityType, params string[] keys)
    {
        using var harness = new SqliteTestHarness();

        using var context = harness.CreateContext();

        var info = EntityInfoFactory.Create(entityType, context.Model);

        info.Keys.Should().AllSatisfy(k => k.ColumnName.Should().HaveCount(1));
        info.Keys.Select(k => k.ColumnName.SqlColumnName).Should().BeEquivalentTo(keys);

        Verify(info, null, keys, context.Model);
    }

    private static void Verify(EntityInfo info, ColumnName? baseColumn, string[]? keys, IModel model)
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
                else
                {
                    //TODO: Handle navigation properties
                    throw new NotImplementedException($"Unknown property type {p.Name}");
                }

                if (baseColumn != null)
                    entityProp.ColumnName.Should().StartWith(baseColumn);
                else
                    entityProp.ColumnName.ShouldHaveSingleItem();
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



            || type.IsAssignableTo(typeof(IStrongId));
    }

    private static bool IsComplexType(PropertyInfo property)
    {
        return property.GetCustomAttribute<ComplexTypeAttribute>() != null ||
               property.PropertyType.GetCustomAttribute<ComplexTypeAttribute>() != null;
    }

    private static bool IsOwnedType(PropertyInfo property)
    {
        return property.GetCustomAttribute<OwnedAttribute>() != null ||
               property.PropertyType.GetCustomAttribute<OwnedAttribute>() != null;
    }
}