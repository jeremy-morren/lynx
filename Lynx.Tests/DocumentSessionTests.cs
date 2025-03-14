using Lynx.DocumentStore;
using Lynx.DocumentStore.Query;
using Lynx.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Tests;

public class DocumentSessionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task TestStoreAndDelete(bool useAsync, bool useBulk)
    {
        var options = new DocumentStoreOptions
        {
            UseBulkOperationsForUpsert = useBulk,
            BulkOperationThreshold = 1
        };
        var listener = new Mock<IDocumentSessionListener>();

        using var _ = TestContext.CreateContext(out var factory);

        using (var context = factory())
        {
            var store = new DocumentStore<TestContext>(context, Options.Create(options), [listener.Object]);
            var session = store.CreateSession();

            //Upsert
            session.Store(ParentEntity.Create(1));
            session.Store(ParentEntity.Create(2), ParentEntity.Create(3));
            
            session.Store(ParentEntity.Create(3, 1)); //Ensure duplicates are overwritten
            
            session.Store<ParentEntity>(new List<ParentEntity>() { ParentEntity.Create(4) });

            //Insert
            session.Insert(ParentEntity.Create(5));
            session.Insert(ParentEntity.Create(6), ParentEntity.Create(7));
            session.Insert<ParentEntity>(new List<ParentEntity>() { ParentEntity.Create(8) });

            if (useAsync)
                await session.SaveChangesAsync();
            else
                session.SaveChanges();
        }

        using (var context = factory())
        {
            var store = new DocumentStore<TestContext>(context, Options.Create(options), [listener.Object]);

            context.Query<ParentEntity>().Should().HaveCount(8);
            context.Query<ParentEntity>().Single(e => e.Id == 3).Iteration.ShouldBe(1,
                "Existing entity should be overwritten");

            //Upsert and delete
            var session = store.CreateSession();
            session.DeleteWhere<ParentEntity>(x => x.Id == 1);
            session.Delete<ParentEntity>(2);
            
            session.Store(ParentEntity.Create(3, 2)); //Overwrite previous insert

            if (useAsync)
                await session.SaveChangesAsync();
            else
                session.SaveChanges();
        }

        using (var context = factory())
        {
            context.Query<ParentEntity>().Should()
                .NotContain(e => e.Id == 1).And
                .NotContain(e => e.Id == 2).And
                .HaveCount(6)
                .And.AllSatisfy(e => e.Owned.ShouldNotBeNull().Id.ShouldBe(e.Id));
            
            context.Query<ParentEntity>().Single(e => e.Id == 3).Iteration.ShouldBe(2);
        }

        listener.Verify(l => l.OnInsertedOrUpdated(It.IsAny<IReadOnlyList<object>>(), It.IsAny<DbContext>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Replace(bool useAsync, bool useBulk)
    {
        var options = new DocumentStoreOptions
        {
            UseBulkOperationsForUpsert = useBulk,
            BulkOperationThreshold = 1
        };

        var listener = new Mock<IDocumentSessionListener>();

        using var _ = TestContext.CreateContext(out var factory);

        using (var context = factory())
        {
            var store = new DocumentStore<TestContext>(context, Options.Create(options),[listener.Object]);
            var session = store.CreateSession();

            //Insert entities 1-10
            session.Insert(Enumerable.Range(0, 10).Select(i => ParentEntity.Create(i)));

            if (useAsync)
                await session.SaveChangesAsync();
            else
                session.SaveChanges();
        }

        using (var context = factory())
        {
            context.Query<ParentEntity>().Should().HaveCount(10);
            context.Query<ParentEntity>().FilterByIds(Enumerable.Range(2, 3)).Should().HaveCount(3);

            var store = new DocumentStore<TestContext>(context, Options.Create(options),[listener.Object]);
            var session = store.CreateSession();

            //Replace entities 0-2 and 8-9 with entities 2-4
            var entities = Enumerable.Range(2, 3).Select(i => ParentEntity.Create(i)).ToList();
            session.Replace(entities, x => x.Id < 5 || x.Id >= 8);

            if (useAsync)
                await session.SaveChangesAsync();
            else
                session.SaveChanges();
        }

        using (var context = factory())
        {
            //Should have entity ids 2-7
            context.Query<ParentEntity>().Should().HaveCount(6);
            context.Query<ParentEntity>().AsEnumerable()
                .Select(context.Model.GetEntityKey)
                .Should().BeEquivalentTo(Enumerable.Range(2, 6));
        }

        listener.Verify(l => l.OnInsertedOrUpdated(It.IsAny<IReadOnlyList<object>>(), It.IsAny<DbContext>()), Times.Exactly(2));
    }

    [Fact]
    public void PassingEnumerableToSingleMethodShouldThrow()
    {
        using var _ = TestContext.CreateContext(out var factory);

        using var context = factory();
        var store = new DocumentStore<TestContext>(context, Options.Create(new DocumentStoreOptions()));
        var session = store.CreateSession();

        var ex = Assert.Throws<InvalidOperationException>(() => session.Store(new List<ParentEntity>()));
        ex.Message.ShouldContain("Use Store(IEnumerable<T> entities) instead.");

        ex = Assert.Throws<InvalidOperationException>(() => session.Insert(new List<ParentEntity>()));
        ex.Message.ShouldContain("Use Insert(IEnumerable<T> entities) instead.");
    }
    
    [Fact]
    public void SkippingSaveChangesShouldNotCommit()
    {
        using var _ = TestContext.CreateContext(out var factory);

        var listener = new Mock<IDocumentSessionListener>();

        using (var context = factory())
        {
            var store = new DocumentStore<TestContext>(context, Options.Create(new DocumentStoreOptions()), [listener.Object]);
            var session = store.CreateSession();
            session.Store(ParentEntity.Create(1));
        }
        using (var context = factory())
        {
            context.Query<ParentEntity>().Should().BeEmpty();
        }
        listener.Verify(l => l.OnInsertedOrUpdated(It.IsAny<IReadOnlyList<object>>(), It.IsAny<DbContext>()), Times.Never);
    }
}