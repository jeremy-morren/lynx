using Lynx.DocumentStore;
using Microsoft.EntityFrameworkCore;
using Moq;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Tests;

public class DocumentSessionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TestStoreAndDelete(bool isAsync)
    {
        var listener = new Mock<IDocumentSessionListener>();

        using var _ = TestContext.CreateContext(out var factory);

        using (var context = factory())
        {
            var store = new DocumentStore<TestContext>(context, [listener.Object]);
            var session = store.OpenSession();

            //Upsert
            session.Store(ParentEntity.Create(1));
            session.Store(ParentEntity.Create(2), ParentEntity.Create(3));
            
            session.Store(ParentEntity.Create(3, 1)); //Ensure duplicates are overwritten
            
            session.Store<ParentEntity>(new List<ParentEntity>() { ParentEntity.Create(4) });

            //Insert
            session.Insert(ParentEntity.Create(5));
            session.Insert(ParentEntity.Create(6), ParentEntity.Create(7));
            session.Insert<ParentEntity>(new List<ParentEntity>() { ParentEntity.Create(8) });
            
            if (isAsync)
                await session.SaveChangesAsync();
            else
                session.SaveChanges();
        }

        using (var context = factory())
        {
            var store = new DocumentStore<TestContext>(context, [listener.Object]);

            context.Query<ParentEntity>().Should().HaveCount(8);
            context.Query<ParentEntity>().Single(e => e.Id == 3).Iteration.ShouldBe(1,
                "Existing entity should be overwritten");
            
            //Upsert and delete
            var session = store.OpenSession();
            session.DeleteWhere<ParentEntity>(x => x.Id == 1);
            session.Delete<ParentEntity>(2);
            
            session.Store(ParentEntity.Create(3, 2)); //Overwrite previous insert
            
            if (isAsync)
                await session.SaveChangesAsync();
            else
                session.SaveChanges();
        }

        using (var context = factory())
        {
            context.Query<ParentEntity>().Should().HaveCount(6)
                .And.NotContain(e => e.Id == 1)
                .And.NotContain(e => e.Id == 2)
                .And.AllSatisfy(e => e.Owned.ShouldNotBeNull().Id.ShouldBe(e.Id));
            
            context.Query<ParentEntity>().Single(e => e.Id == 3).Iteration.ShouldBe(2);
        }

        listener.Verify(l => l.OnInsertedOrUpdated(It.IsAny<IReadOnlyList<object>>(), It.IsAny<DbContext>()), Times.Exactly(8));
    }

    [Fact]
    public void PassingEnumerableToSingleMethodShouldThrow()
    {
        using var _ = TestContext.CreateContext(out var factory);

        using var context = factory();
        var store = new DocumentStore<TestContext>(context);
        var session = store.OpenSession();

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
            var store = new DocumentStore<TestContext>(context, [listener.Object]);
            var session = store.OpenSession();
            session.Store(ParentEntity.Create(1));
        }
        using (var context = factory())
        {
            context.Query<ParentEntity>().Should().BeEmpty();
        }
        listener.Verify(l => l.OnInsertedOrUpdated(It.IsAny<IReadOnlyList<object>>(), It.IsAny<DbContext>()), Times.Never);
    }
}