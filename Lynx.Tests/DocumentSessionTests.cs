using Lynx.DocumentStore;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit.Abstractions;

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

        using var _ = TestDbContext.CreateContext(out var factory);

        using (var context = factory())
        {
            var store = new DocumentStore<TestDbContext>(context, [listener.Object]);
            var session = store.OpenSession();

            //Insert
            session.Store(TestEntity.Create(1), TestEntity.Create(2));
            session.Store(TestEntity.Create(3), TestEntity.Create(4));
            
            session.Store(TestEntity.Create(4, 1)); //Ensure duplicates are overwritten
            
            session.Store<TestEntity>(new List<TestEntity>() { TestEntity.Create(5) });
            
            if (isAsync)
                await session.SaveChangesAsync();
            else
                session.SaveChanges();
        }

        using (var context = factory())
        {
            var store = new DocumentStore<TestDbContext>(context, [listener.Object]);

            context.Query<TestEntity>().Should().HaveCount(5);
            context.Query<TestEntity>().Single(e => e.Id == 4).Iteration.ShouldBe(1);
            
            //Insert and delete
            var session = store.OpenSession();
            session.DeleteWhere<TestEntity>(x => x.Id == 1);
            
            session.Store(TestEntity.Create(4, 2)); //Overwrite previous insert
            
            if (isAsync)
                await session.SaveChangesAsync();
            else
                session.SaveChanges();
        }

        using (var context = factory())
        {
            context.Query<TestEntity>().Should().HaveCount(4);
            context.Query<TestEntity>().ShouldNotContain(e => e.Id == 1);
            
            context.Query<TestEntity>().Should().AllSatisfy(e => e.OwnedType.ShouldNotBeNull().Id.ShouldBe(e.Id));
            
            context.Query<TestEntity>().Single(e => e.Id == 4).Iteration.ShouldBe(2);
        }

        listener.Verify(l => l.OnUpserted(It.IsAny<IReadOnlyList<object>>(), It.IsAny<DbContext>()), Times.Exactly(5));
    }

    [Fact]
    public void PassingEnumerableToStoreSingleShouldThrow()
    {
        using var _ = TestDbContext.CreateContext(out var factory);

        using var context = factory();
        var store = new DocumentStore<TestDbContext>(context);
        var session = store.OpenSession();
        var ex = Assert.Throws<InvalidOperationException>(() => session.Store(new List<TestEntity>()));
        ex.Message.ShouldContain("Use Store(IEnumerable<T> entities) instead.");
    }
    
    [Fact]
    public void SkippingSaveChangesShouldNotCommit()
    {
        using var _ = TestDbContext.CreateContext(out var factory);

        var listener = new Mock<IDocumentSessionListener>();

        using (var context = factory())
        {
            var store = new DocumentStore<TestDbContext>(context, [listener.Object]);
            var session = store.OpenSession();
            session.Store(TestEntity.Create(1));
        }
        using (var context = factory())
        {
            context.Query<TestEntity>().Should().BeEmpty();
        }
        listener.Verify(l => l.OnUpserted(It.IsAny<IReadOnlyList<object>>(), It.IsAny<DbContext>()), Times.Never);
    }

    [Fact]
    public void DocumentStoreQueryShouldIncludeRelatedEntities()
    {
        using var _ = TestDbContext.CreateContext(out var factory);

        using (var context = factory())
        {
            var entity = TestEntity.Create(1) with
            {
                Child = new ChildEntity() { Id = 1 }
            };
            context.Add(entity);
            context.SaveChanges();
        }
        using (var context = factory())
        {
            context.Query<TestEntity>().ShouldNotBeEmpty();
            context.Query<TestEntity>().ShouldAllBe(e => e.Child == null);

            var store = new DocumentStore<TestDbContext>(context);

            store.Query<TestEntity>().ShouldAllBe(e => e.Child != null);
        }
    }
}