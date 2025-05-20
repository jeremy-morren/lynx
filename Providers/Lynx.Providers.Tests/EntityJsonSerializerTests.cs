using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.Provider.Sqlite;
using Lynx.Providers.Common.Models;
using Lynx.Providers.Common.Reflection;
using Lynx.Providers.Tests.Sqlite;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;

namespace Lynx.Providers.Tests;

public class EntityJsonSerializerTests
{
    [Fact]
    public void SerializeJson()
    {
        var serializeProperty = typeof(EntityJsonSerializerTests)
            .GetMethod(nameof(SerializeProperty), BindingFlags.NonPublic | BindingFlags.Static)
            .ShouldNotBeNull();

        using var harness = new SqliteTestHarness();
        using var context = harness.CreateContext();

        var entity = EntityInfoFactoryTests.CreateRootEntity(typeof(Customer), context.Model);

        foreach (var structure in entity.Owned.OfType<JsonOwnedEntityInfo>())
            serializeProperty.MakeGenericMethod(structure.PropertyInfo.PropertyType).Invoke(null, [structure]);
    }

    private static void SerializeProperty<T>(JsonOwnedEntityInfo entity) where T : class
    {
        entity.PropertyInfo.PropertyType.ShouldBe(typeof(T));

        var parameter = Expression.Variable(typeof(T), "input");
        var result = EntityJsonSerializer.Serialize(entity, parameter);
        var action = Expression.Lambda<Func<T?, string>>(result, parameter).Compile();

        foreach (var customer in ParameterDelegateBuilderTestsBase.GetTestCustomers())
        {
            var value = entity.PropertyInfo.GetValue(customer);
            if (value == null)
            {
                action(null).ShouldBe("null");
            }
            else
            {
                var json = action(value.ShouldBeOfType<T>());
                var other = JToken.Parse(JsonHelpers.SerializeJson(value)!);
                JToken.Parse(json).Should().BeEquivalentTo(other);
            }
        }
    }

    [Fact]
    public void TestForEach()
    {
        var add = typeof(List<int>).GetMethod("Add", [typeof(int)]).ShouldNotBeNull();
        var result = Expression.Parameter(typeof(List<int>), "result");
        var collection = Expression.Parameter(typeof(IReadOnlyList<int>), "collection");

        var item = Expression.Variable(typeof(int), "item");
        var body = Expression.Call(result, add, item);
        var forEach = ExpressionHelpers.ForLoop(collection, item, body);

        var action = Expression.Lambda<Action<List<int>, IReadOnlyList<int>>>(forEach, result, collection).Compile();

        var list = new List<int>();
        action(list, Enumerable.Range(5, 10).ToArray());
        list.Should().BeEquivalentTo(Enumerable.Range(5, 10));
    }

}