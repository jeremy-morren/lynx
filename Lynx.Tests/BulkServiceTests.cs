using Lynx.Bulk;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing
// ReSharper disable PossibleUnintendedQueryableAsEnumerable

namespace Lynx.Tests;

public class BulkServiceTests
{
    [Theory]
    [MemberData(nameof(GetInsertUpsertData))]
    public async Task InsertAndUpsert(BulkTestType type, bool useBulkOperation)
    {
        using var _ = TestContext.CreateContext(out var factory);

        using (var context = factory())
        {
            var entities = Enumerable.Range(1, 10).Select(i => Entity1.New(i)).ToArray();

            var service = context.CreateBulkService();
            using var transaction = context.Database.BeginTransaction();
            switch (type)
            {
                case BulkTestType.Sync:
                    service.BulkInsert(entities);
                    break;
                case BulkTestType.Async:
                    await service.BulkInsertAsync(entities);
                    break;
                case BulkTestType.AsyncEnumerable:
                    await service.BulkInsertAsync(entities.ToAsyncEnumerable());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            transaction.Commit();
        }

        using (var context = factory())
        {
            context.Query<Entity1>().Should().HaveCount(10);
            context.Query<Entity1>().Select(e => e.Id).Should().BeEquivalentTo(Enumerable.Range(1, 10));

            var service = context.CreateBulkService();
            using var transaction = context.Database.BeginTransaction();

            var entities = Enumerable.Range(6, 10).Select(i => Entity1.New(i)).ToArray();
            switch (type)
            {
                case BulkTestType.Sync:
                    service.BulkUpsert(entities, useBulkOperation);
                    break;
                case BulkTestType.Async:
                    await service.BulkUpsertAsync(entities, useBulkOperation);
                    break;
                case BulkTestType.AsyncEnumerable:
                    await service.BulkUpsertAsync(entities.ToAsyncEnumerable(), useBulkOperation);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            transaction.Commit();
        }

        using (var context = factory())
        {
            context.Query<Entity1>().Should().HaveCount(15);
            context.Query<Entity1>().Select(e => e.Id).Should().BeEquivalentTo(Enumerable.Range(1, 15));
        }
    }

    [Theory]
    [MemberData(nameof(GetTypes))]
    public async Task InsertWithoutTransactionShouldThrow(BulkTestType type)
    {
        using var _ = TestContext.CreateContext(out var factory);

        await using var context = factory();
        var service = context.CreateBulkService();

        var entities = Array.Empty<Entity1>();
        
        var ex = type switch
        {
            BulkTestType.Sync =>
                Assert.Throws<InvalidOperationException>(() => service.BulkInsert(entities)),

            BulkTestType.Async =>
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.BulkInsertAsync(entities)),
            BulkTestType.AsyncEnumerable =>
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.BulkInsertAsync(entities.ToAsyncEnumerable())),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        ex.Message.ShouldContain(nameof(context.Database.BeginTransaction));
    }
    
    [Theory]
    [MemberData(nameof(GetTypes))]
    public async Task UpsertWithoutTransactionShouldThrow(BulkTestType type)
    {
        using var _ = TestContext.CreateContext(out var factory);

        await using var context = factory();
        var service = context.CreateBulkService();

        var entities = Array.Empty<Entity1>();
        
        var ex = type switch
        {
            BulkTestType.Sync =>
                Assert.Throws<InvalidOperationException>(() => service.BulkUpsert(entities, false)),

            BulkTestType.Async =>
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.BulkUpsertAsync(entities, false)),
            BulkTestType.AsyncEnumerable =>
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.BulkUpsertAsync(entities.ToAsyncEnumerable(), false)),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        ex.Message.ShouldContain(nameof(context.Database.BeginTransaction));
    }

    public enum BulkTestType
    {
        Sync,
        Async,
        AsyncEnumerable
    }

    public static TheoryData<BulkTestType> GetTypes() => new(Enum.GetValues<BulkTestType>());
    public static TheoryData<BulkTestType, bool> GetInsertUpsertData()
    {
        //Parameters: type, useBulkOperation
        var data = new TheoryData<BulkTestType, bool>();
        foreach (var type in Enum.GetValues<BulkTestType>())
        {
            data.Add(type, false);
            data.Add(type, true);
        }

        return data;
    }
}