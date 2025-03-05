using System.Data;
using EFCore.BulkExtensions;
using Lynx.DocumentStore;
using Lynx.DocumentStore.Query;
using Lynx.EfCore;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Tests;

public class ForeignKeyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetConstraintsDeferrable(bool async)
    {
        var database = $"{nameof(SetConstraintsDeferrable)}_{async}";
        CleanPostgresDb(database);

        await using var conn = new NpgsqlConnection($"{ConnString};Database={database}");
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        var options = new DbContextOptionsBuilder()
            .UseNpgsql(conn)
            .Options;

        await using (var context = new TestContext(options))
        {
            context.Database.EnsureCreated();

            if (async)
                await ForeignKeyHelpers.ExecuteSetConstraintsDeferrableAsync(context);
            else
                ForeignKeyHelpers.ExecuteSetConstraintsDeferrable(context);

        }

        using (var transaction = conn.BeginTransaction())
        {
            //Save changes in a way that violates the foreign key constraint

            await using (var context = new TestContext(options))
            {
                context.Add(new Child()
                {
                    Id = 1,
                    ForeignId = 1
                });
                context.SaveChanges();
            }
            await using (var context = new TestContext(options))
            {
                //Insert a row that violates the foreign key constraint
                context.Add(new Foreign()
                {
                    Id = 1
                });
                context.SaveChanges();
            }

            transaction.Commit();
        }


        await using (var context = new TestContext(options))
        {
            context.Set<Child>()
                .IncludeAllReferenced()
                .ToList()
                .Should().AllSatisfy(e => e.Foreign.ShouldNotBeNull());
        }

    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteOutOfOrder(bool useAsync)
    {
        var database = $"{nameof(DeleteOutOfOrder)}_{useAsync}";
        CleanPostgresDb(database);

        using var conn = new NpgsqlConnection($"{ConnString};Database={database}");
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var options = new DbContextOptionsBuilder()
            .UseNpgsql(conn)
            .Options;

        using (var context = new TestContext(options))
        {
            context.Database.EnsureCreated();
            ForeignKeyHelpers.ExecuteSetConstraintsDeferrable(context);
        }

        using (var context = new TestContext(options))
        {
            context.Set<Entity1>().AddRange(Enumerable.Range(1, 10).Select(i => Entity1.New(i)));
            context.SaveChanges();
        }

        using (var context = new TestContext(options))
        {
            var session = new DocumentSession(context, []);

            session.Replace<Foreign>(
                Enumerable.Range(1, 10).Select(i => new Foreign() { Id = i }),
                x => true);

            if (useAsync)
                await session.SaveChangesAsync();
            else
                session.SaveChanges();
        }

        using (var context = new TestContext(options))
        {
            context.Set<Foreign>().Should().HaveCount(10);
            context.Set<Entity1>().Should().HaveCount(10);
        }
    }

    private const string ConnString = "Host=localhost;Username=postgres;Password=postgres;Include Error Detail=true";


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Replace(bool useAsync)
    {
        var database = $"{nameof(Replace)}_{useAsync}";
        CleanPostgresDb(database);

        using var conn = new NpgsqlConnection($"{ConnString};Database={database}");
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var listener = new Mock<IDocumentSessionListener>();
        var options = new DbContextOptionsBuilder()
            .UseNpgsql(conn)
            .Options;

        using (var context = new TestContext(options))
        {
            context.Database.EnsureCreated();

            var store = new DocumentStore<TestContext>(context, [listener.Object]);
            var session = store.OpenSession();

            //Insert entities 1-10
            session.Insert(Enumerable.Range(0, 10).Select(i => ParentEntity.Create(i)));

            if (useAsync)
                await session.SaveChangesAsync();
            else
                session.SaveChanges();
        }

        using (var context = new TestContext(options))
        {
            context.Query<ParentEntity>().Should().HaveCount(10);
            context.Query<ParentEntity>().FilterByIds(Enumerable.Range(2, 3)).Should().HaveCount(3);

            var store = new DocumentStore<TestContext>(context, [listener.Object]);
            var session = store.OpenSession();

            //Replace entities 0-2 and 8-9 with entities 2-4
            var entities = Enumerable.Range(2, 3).Select(i => ParentEntity.Create(i)).ToList();
            session.Replace(entities, x => x.Id < 5 || x.Id >= 8);

            if (useAsync)
                await session.SaveChangesAsync();
            else
                session.SaveChanges();
        }

        using (var context = new TestContext(options))
        {
            //Should have entity ids 2-7
            context.Query<ParentEntity>().Should().HaveCount(6);
            context.Query<ParentEntity>().AsEnumerable()
                .Select(context.Model.GetEntityKey)
                .Should().BeEquivalentTo(Enumerable.Range(2, 6));
        }

        listener.Verify(l => l.OnInsertedOrUpdated(It.IsAny<IReadOnlyList<object>>(), It.IsAny<DbContext>()), Times.Exactly(2));
    }

    private static void CleanPostgresDb(string database)
    {
        using var connection = new NpgsqlConnection(ConnString);
        if (connection.State != ConnectionState.Open)
            connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{database}\"";
        command.ExecuteNonQuery();

        command.CommandText = $"CREATE DATABASE \"{database}\"";
        command.ExecuteNonQuery();
    }
}