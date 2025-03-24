using Lynx.Providers.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Providers.Tests;

public class ProviderBulkTestsBase
{
    internal static async Task TestCustomers(
        ILynxProvider provider, ProviderTestType type, Func<string, ITestHarness> createHarness)
    {
        using var lynxHarness = createHarness("lynx");

        List<Customer> customers =
        [
            Customer(1),
            Customer(2) with { Tags = null },
            Customer(3)
        ];

        ILynxEntityServiceBulk<Customer> customerSvc;
        ILynxEntityServiceBulk<Contact> contactSvc;

        using (var context = lynxHarness.CreateContext())
        {
            customerSvc = provider.CreateService<Customer>(context.Model)
                .ShouldBeAssignableTo<ILynxEntityServiceBulk<Customer>>();
            contactSvc = provider.CreateService<Contact>(context.Model)
                .ShouldBeAssignableTo<ILynxEntityServiceBulk<Contact>>();

            IEnumerable<Contact> contacts = customers.Select(c => c.InvoiceContact?.Contact)
                .Concat(customers.Select(c => c.OrderContact?.Contact))
                .Where(c => c != null)!;

            context.Database.EnsureCreated();

            context.Database.OpenConnection();

            var connection = context.Database.GetDbConnection();
            using var transaction = connection.BeginTransaction();
            switch (type)
            {
                case ProviderTestType.Sync:
                    contactSvc.BulkInsert(contacts, connection);
                    customerSvc.BulkInsert(customers, connection);
                    break;
                case ProviderTestType.Async:
                    await contactSvc.BulkInsertAsync(contacts, connection);
                    await customerSvc.BulkInsertAsync(customers, connection);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await contactSvc.BulkInsertAsync(contacts.ToAsyncEnumerable(), connection);
                    await customerSvc.BulkInsertAsync(customers.ToAsyncEnumerable(), connection);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            transaction.Commit();
        }

        foreach (var c in customers)
            c.Name = "New name";
        customers.AddRange([Customer(4), Customer(5)]);

        using (var context = lynxHarness.CreateContext())
        {
            IEnumerable<Contact> contacts = customers.Select(c => c.InvoiceContact?.Contact)
                .Concat(customers.Select(c => c.OrderContact?.Contact))
                .Where(c => c != null)!;

            context.Database.OpenConnection();

            using var transaction = context.Database.BeginTransaction();
            var connection = transaction.GetDbTransaction().Connection.ShouldNotBeNull();
            switch (type)
            {
                case ProviderTestType.Sync:
                    contactSvc.BulkUpsert(contacts, connection);
                    customerSvc.BulkUpsert(customers, connection);
                    break;
                case ProviderTestType.Async:
                    await contactSvc.BulkUpsertAsync(contacts, connection);
                    await customerSvc.BulkUpsertAsync(customers, connection);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await contactSvc.BulkUpsertAsync(contacts.ToAsyncEnumerable(), connection);
                    await customerSvc.BulkUpsertAsync(customers.ToAsyncEnumerable(), connection);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            transaction.Commit();
        }

        using var manualHarness = createHarness("manual");
        using (var context = manualHarness.CreateContext())
        {
            context.Database.EnsureCreated();
            context.Customers.AddRange(customers);
            context.SaveChanges();
        }

        using (var lynxContext = lynxHarness.CreateContext())
        using (var manualContext = manualHarness.CreateContext())
        {
            lynxContext.Customers.AsNoTracking().ToList()
                .Should().BeEquivalentTo(manualContext.Customers.AsNoTracking().ToList());
        }
    }

    internal static async Task TestCities(
        ILynxProvider provider, ProviderTestType type, Func<string, ITestHarness> createHarness)
    {
        var cities = Enumerable.Range(0, 5).Select(City).ToList();

        ILynxEntityServiceBulk<City> citySvc;
        using var lynxHarness = createHarness("lynx");
        using (var context = lynxHarness.CreateContext())
        {
            citySvc = provider.CreateService<City>(context.Model)
                .ShouldBeAssignableTo<ILynxEntityServiceBulk<City>>();

            context.Database.EnsureCreated();
            context.Database.OpenConnection();

            var connection = context.Database.GetDbConnection();
            using var transaction = connection.BeginTransaction();
            switch (type)
            {
                case ProviderTestType.Sync:
                    citySvc.BulkInsert(cities, connection);
                    break;
                case ProviderTestType.Async:
                    await citySvc.BulkInsertAsync(cities, connection);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await citySvc.BulkInsertAsync(cities.ToAsyncEnumerable(), connection);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            transaction.Commit();
        }

        foreach (var c in cities)
            c.Name = "New name";
        cities.AddRange(Enumerable.Range(5, 5).Select(City));

        using (var context = lynxHarness.CreateContext())
        {
            context.Database.OpenConnection();
            using var transaction = context.Database.BeginTransaction();
            var connection = transaction.GetDbTransaction().Connection.ShouldNotBeNull();

            switch (type)
            {
                case ProviderTestType.Sync:
                    citySvc.BulkUpsert(cities, connection);
                    break;
                case ProviderTestType.Async:
                    await citySvc.BulkUpsertAsync(cities, connection);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await citySvc.BulkUpsertAsync(cities.ToAsyncEnumerable(), connection);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            transaction.Commit();
        }

        using var manualHarness = createHarness("manual");
        using (var context = manualHarness.CreateContext())
        {
            context.Database.EnsureCreated();
            context.Cities.AddRange(cities);
            context.SaveChanges();
        }

        using (var lynxContext = lynxHarness.CreateContext())
        using (var manualContext = manualHarness.CreateContext())
        {
            lynxContext.Cities.AsNoTracking().ToList()
                .Should().BeEquivalentTo(manualContext.Cities.AsNoTracking().ToList());
        }
    }

    internal static async Task TestConverterEntities(
        ILynxProvider provider, ProviderTestType type, Func<string, ITestHarness> createHarness)
    {
        var entities = Enumerable.Range(0, 10).Select(ConverterEntity).ToList();

        using var lynxHarness = createHarness("lynx");

        ILynxEntityServiceBulk<ConverterEntity> entitySvc;
        using (var context = lynxHarness.CreateContext())
        {
            entitySvc = provider.CreateService<ConverterEntity>(context.Model)
                .ShouldBeAssignableTo<ILynxEntityServiceBulk<ConverterEntity>>();

            context.Database.EnsureCreated();
            using var transaction = context.Database.BeginTransaction();
            var connection = transaction.GetDbTransaction().Connection.ShouldNotBeNull();

            switch (type)
            {
                case ProviderTestType.Sync:
                    entitySvc.BulkInsert(entities, connection);
                    break;
                case ProviderTestType.Async:
                    await entitySvc.BulkInsertAsync(entities, connection);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await entitySvc.BulkInsertAsync(entities.ToAsyncEnumerable(), connection);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            transaction.Commit();
        }

        foreach (var e in entities)
            e.Enum = BuildingPurpose.Residential;

        entities.AddRange(Enumerable.Range(10, 10).Select(ConverterEntity));

        using (var context = lynxHarness.CreateContext())
        {
            context.Database.OpenConnection();
            var connection = context.Database.GetDbConnection();
            using var transaction = context.Database.BeginTransaction();
            switch (type)
            {
                case ProviderTestType.Sync:
                    entitySvc.BulkUpsert(entities, connection);
                    break;
                case ProviderTestType.Async:
                    await entitySvc.BulkUpsertAsync(entities, connection);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await entitySvc.BulkUpsertAsync(entities.ToAsyncEnumerable(), connection);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            transaction.Commit();
        }

        using var manualHarness = createHarness("manual");
        using (var context = manualHarness.CreateContext())
        {
            context.Database.EnsureCreated();
            context.ConverterEntities.AddRange(entities);
            context.SaveChanges();
        }

        using (var lynxContext = lynxHarness.CreateContext())
        using (var manualContext = manualHarness.CreateContext())
        {
            lynxContext.ConverterEntities.AsNoTracking().ToList()
                .Should().BeEquivalentTo(manualContext.ConverterEntities.AsNoTracking().ToList());
        }
    }
    
    internal static async Task TestIdOnly(
        ILynxProvider provider, ProviderTestType type, Func<string, ITestHarness> createHarness)
    {
        var entities = Enumerable.Range(10, 10).Select(IdOnly).ToList();
        
        using var lynxHarness = createHarness("lynx");

        ILynxEntityServiceBulk<IdOnly> entitySvc;
        using (var context = lynxHarness.CreateContext())
        {
            entitySvc = provider.CreateService<IdOnly>(context.Model)
                .ShouldBeAssignableTo<ILynxEntityServiceBulk<IdOnly>>();

            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction();
            var connection = context.Database.GetDbConnection();
            switch (type)
            {
                case ProviderTestType.Sync:
                    entitySvc.BulkInsert(entities, connection);
                    break;
                case ProviderTestType.Async:
                    await entitySvc.BulkInsertAsync(entities, connection);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await entitySvc.BulkInsertAsync(entities.ToAsyncEnumerable(), connection);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            transaction.Commit();
        }

        entities = entities
            .Concat(Enumerable.Range(100, 10).Select(IdOnly))
            .ToList();

        using (var context = lynxHarness.CreateContext())
        {
            using var transaction = context.Database.BeginTransaction();
            var connection = transaction.GetDbTransaction().Connection.ShouldNotBeNull();
            switch (type)
            {
                case ProviderTestType.Sync:
                    entitySvc.BulkUpsert(entities, connection);
                    break;
                case ProviderTestType.Async:
                    await entitySvc.BulkUpsertAsync(entities, connection);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await entitySvc.BulkUpsertAsync(entities.ToAsyncEnumerable(), connection);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            transaction.Commit();
        }

        using var manualHarness = createHarness("manual");
        using (var context = manualHarness.CreateContext())
        {
            context.Database.EnsureCreated();
            context.IdOnly.AddRange(entities);
            context.SaveChanges();
        }

        using (var lynxContext = lynxHarness.CreateContext())
        using (var manualContext = manualHarness.CreateContext())
        {
            lynxContext.IdOnly.AsNoTracking().ToList()
                .Should().BeEquivalentTo(manualContext.IdOnly.AsNoTracking().ToList())
                .And.BeEquivalentTo(entities);
        }
    }

    private static Customer Customer(int id) => ProviderTestsBase.Customer(id);

    private static City City(int id) => ProviderTestsBase.City(id);

    private static ConverterEntity ConverterEntity(int id) => ProviderTestsBase.ConverterEntity(id);
    
    private static IdOnly IdOnly(int id) => ProviderTestsBase.IdOnly(id);
}