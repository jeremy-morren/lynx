using Lynx.DocumentStore;
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

        using var _ = TestDbContext.Create(listener, out var factory);

        using (var context = factory())
        {
            //Insert
            var session = context.CreateSession();
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
            context.Query<TestEntity>().Should().HaveCount(5);
            context.Query<TestEntity>().Single(e => e.Id == 4).Iteration.ShouldBe(1);
            
            //Insert and delete
            var session = context.CreateSession();
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

        listener.Verify(l => l.OnUpserted(It.IsAny<IReadOnlyList<object>>()), Times.Exactly(5));
    }

    [Fact]
    public void PassingEnumerableToStoreSingleShouldThrow()
    {
        using var _ = TestDbContext.Create(out var factory);

        var context = factory();
        var session = context.CreateSession();
        var ex = Assert.Throws<InvalidOperationException>(() => session.Store(new List<TestEntity>()));
        ex.Message.ShouldContain("Use Store(IEnumerable<T> entities) instead.");
    }
    
    [Fact]
    public void SkippingSaveChangesShouldNotCommit()
    {
        var listener = new Mock<IDocumentSessionListener>();
        using var _ = TestDbContext.Create(listener, out var factory);

        using (var context = factory())
        {
            var session = context.CreateSession();
            session.Store(TestEntity.Create(1));
        }

        using (var context = factory())
        {
            context.Query<TestEntity>().Should().BeEmpty();
        }
        listener.Verify(l => l.OnUpserted(It.IsAny<IReadOnlyList<object>>()), Times.Never);
    }

    [Fact]
    public void ListenersShouldBeOptionsScoped()
    {
        var listener1= new Mock<IDocumentSessionListener>();
        var listener2 = new Mock<IDocumentSessionListener>();

        using var _ = TestDbContext.Create(listener1, out var factory1);
        using var __ = TestDbContext.Create(listener1, out var factory2);
        
        using (var context = factory1())
        {
            var session = context.CreateSession();
            session.Store(TestEntity.Create(1));
            session.SaveChanges();
        }
        using (var context = factory2())
        {
            var session = context.CreateSession();
            session.Store(TestEntity.Create(2)); //Don't save changes, should not trigger listener
        }
        
        listener1.Verify(l => l.OnUpserted(It.IsAny<IReadOnlyList<object>>()), Times.Once);
        listener2.Verify(l => l.OnUpserted(It.IsAny<IReadOnlyList<object>>()), Times.Never);
    }
}