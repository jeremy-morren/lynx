using Lynx.Providers.Common;
using Microsoft.EntityFrameworkCore;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Providers.Tests;

public class ProviderBulkTestsBase
{
    internal static async Task TestCustomers(
        ILynxProvider provider, bool useAsync, Func<string, ITestHarness> createHarness)
    {
        using var lynxHarness = createHarness("lynx");

        List<Customer> customers =
        [
            Customer(1),
            Customer(2) with { Tags = null },
            Customer(3)
        ];

        ILynxDatabaseServiceBulk<Customer> customerSvc;
        ILynxDatabaseServiceBulk<Contact> contactSvc;

        using (var context = lynxHarness.CreateContext())
        {
            customerSvc = provider.CreateService<Customer>(context.Model)
                .ShouldBeAssignableTo<ILynxDatabaseServiceBulk<Customer>>();
            contactSvc = provider.CreateService<Contact>(context.Model)
                .ShouldBeAssignableTo<ILynxDatabaseServiceBulk<Contact>>();

            IEnumerable<Contact> contacts = customers.Select(c => c.InvoiceContact?.Contact)
                .Concat(customers.Select(c => c.OrderContact?.Contact))
                .Where(c => c != null)!;

            context.Database.EnsureCreated();

            context.Database.OpenConnection();
            using var transaction = context.Database.GetDbConnection().BeginTransaction();
            if (useAsync)
            {
                await contactSvc.BulkInsertAsync(contacts, context.Database.GetDbConnection());
                await customerSvc.BulkInsertAsync(customers, context.Database.GetDbConnection());
            }
            else
            {
                contactSvc.BulkInsert(contacts, context.Database.GetDbConnection());
                customerSvc.BulkInsert(customers, context.Database.GetDbConnection());
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
            using var transaction = context.Database.GetDbConnection().BeginTransaction();
            if (useAsync)
            {
                await contactSvc.BulkUpsertAsync(contacts, context.Database.GetDbConnection());
                await customerSvc.BulkUpsertAsync(customers, context.Database.GetDbConnection());
            }
            else
            {
                contactSvc.BulkUpsert(contacts, context.Database.GetDbConnection());
                customerSvc.BulkUpsert(customers, context.Database.GetDbConnection());
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
        ILynxProvider provider, bool useAsync, Func<string, ITestHarness> createHarness)
    {
        var cities = Enumerable.Range(0, 5).Select(City).ToList();

        ILynxDatabaseServiceBulk<City> citySvc;
        using var lynxHarness = createHarness("lynx");
        using (var context = lynxHarness.CreateContext())
        {
            citySvc = provider.CreateService<City>(context.Model)
                .ShouldBeAssignableTo<ILynxDatabaseServiceBulk<City>>();

            context.Database.EnsureCreated();

            context.Database.OpenConnection();
            using var transaction = context.Database.GetDbConnection().BeginTransaction();
            if (useAsync)
                await citySvc.BulkInsertAsync(cities, context.Database.GetDbConnection());
            else
                citySvc.BulkInsert(cities, context.Database.GetDbConnection());
            transaction.Commit();
        }

        foreach (var c in cities)
            c.Name = "New name";
        cities.AddRange(Enumerable.Range(5, 5).Select(City));

        using (var context = lynxHarness.CreateContext())
        {
            context.Database.OpenConnection();
            using var transaction = context.Database.GetDbConnection().BeginTransaction();
            if (useAsync)
                await citySvc.BulkUpsertAsync(cities, context.Database.GetDbConnection());
            else
                citySvc.BulkUpsert(cities, context.Database.GetDbConnection());
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
        ILynxProvider provider, bool useAsync, Func<string, ITestHarness> createHarness)
    {
        var entities = Enumerable.Range(0, 10).Select(ConverterEntity).ToList();

        using var lynxHarness = createHarness("lynx");

        ILynxDatabaseServiceBulk<ConverterEntity> entitySvc;
        using (var context = lynxHarness.CreateContext())
        {
            entitySvc = provider.CreateService<ConverterEntity>(context.Model)
                .ShouldBeAssignableTo<ILynxDatabaseServiceBulk<ConverterEntity>>();

            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction();
            if (useAsync)
                await entitySvc.BulkInsertAsync(entities, context.Database.GetDbConnection());
            else
                entitySvc.BulkInsert(entities, context.Database.GetDbConnection());
            transaction.Commit();
        }

        foreach (var e in entities)
            e.Enum = BuildingPurpose.Residential;

        entities.AddRange(Enumerable.Range(10, 10).Select(ConverterEntity));

        using (var context = lynxHarness.CreateContext())
        {
            context.Database.OpenConnection();
            using var transaction = context.Database.GetDbConnection().BeginTransaction();
            if (useAsync)
                await entitySvc.BulkUpsertAsync(entities, context.Database.GetDbConnection());
            else
                entitySvc.BulkUpsert(entities, context.Database.GetDbConnection());
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
        ILynxProvider provider, bool useAsync, Func<string, ITestHarness> createHarness)
    {
        var entities = Enumerable.Range(10, 10).Select(IdOnly).ToList();
        
        using var lynxHarness = createHarness("lynx");

        ILynxDatabaseServiceBulk<IdOnly> entitySvc;
        using (var context = lynxHarness.CreateContext())
        {
            entitySvc = provider.CreateService<IdOnly>(context.Model)
                .ShouldBeAssignableTo<ILynxDatabaseServiceBulk<IdOnly>>();

            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction();
            if (useAsync)
                await entitySvc.BulkInsertAsync(entities, context.Database.GetDbConnection());
            else
                entitySvc.BulkInsert(entities, context.Database.GetDbConnection());
            transaction.Commit();
        }

        entities = entities
            .Concat(Enumerable.Range(100, 10).Select(IdOnly))
            .ToList();

        using (var context = lynxHarness.CreateContext())
        {
            using var transaction = context.Database.BeginTransaction();
            if (useAsync)
                await entitySvc.BulkUpsertAsync(entities, context.Database.GetDbConnection());
            else
                entitySvc.BulkUpsert(entities, context.Database.GetDbConnection());
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