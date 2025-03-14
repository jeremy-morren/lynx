using Lynx.Providers.Common;
using Microsoft.EntityFrameworkCore;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Providers.Tests;

public class ProviderTestsBase
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

        ILynxDatabaseService<Customer> customerSvc;
        ILynxDatabaseService<Contact> contactSvc;

        using (var context = lynxHarness.CreateContext())
        {
            customerSvc = provider.CreateService<Customer>(context.Model);
            contactSvc = provider.CreateService<Contact>(context.Model);

            IEnumerable<Contact> contacts = customers.Select(c => c.InvoiceContact?.Contact)
                .Concat(customers.Select(c => c.OrderContact?.Contact))
                .Where(c => c != null)!;

            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction();

            if (useAsync)
            {
                await contactSvc.InsertAsync(contacts, context.Database.GetDbConnection());
                await customerSvc.InsertAsync(customers, context.Database.GetDbConnection());
            }
            else
            {
                contactSvc.Insert(contacts, context.Database.GetDbConnection());
                customerSvc.Insert(customers, context.Database.GetDbConnection());
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

            if (useAsync)
            {
                await contactSvc.UpsertAsync(contacts, context.Database.GetDbConnection());
                await customerSvc.UpsertAsync(customers, context.Database.GetDbConnection());
            }
            else
            {
                contactSvc.Upsert(contacts, context.Database.GetDbConnection());
                customerSvc.Upsert(customers, context.Database.GetDbConnection());
            }
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

        ILynxDatabaseService<City> citySvc;
        using var lynxHarness = createHarness("lynx");
        using (var context = lynxHarness.CreateContext())
        {
            citySvc = provider.CreateService<City>(context.Model);

            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction();

            if (useAsync)
                await citySvc.InsertAsync(cities, context.Database.GetDbConnection());
            else
                citySvc.Insert(cities, context.Database.GetDbConnection());
            transaction.Commit();
        }

        foreach (var c in cities)
            c.Name = "New name";
        cities.AddRange(Enumerable.Range(5, 5).Select(City));

        using (var context = lynxHarness.CreateContext())
        {
            if (useAsync)
                await citySvc.UpsertAsync(cities, context.Database.GetDbConnection());
            else
                citySvc.Upsert(cities, context.Database.GetDbConnection());
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

        ILynxDatabaseService<ConverterEntity> entitySvc;
        using (var context = lynxHarness.CreateContext())
        {
            entitySvc = provider.CreateService<ConverterEntity>(context.Model);

            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction();
            if (useAsync)
                await entitySvc.InsertAsync(entities, context.Database.GetDbConnection());
            else
                entitySvc.Insert(entities, context.Database.GetDbConnection());
            transaction.Commit();
        }

        foreach (var e in entities)
            e.Enum = BuildingPurpose.Residential;

        entities.AddRange(Enumerable.Range(10, 10).Select(ConverterEntity));

        using (var context = lynxHarness.CreateContext())
        {
            if (useAsync)
                await entitySvc.UpsertAsync(entities, context.Database.GetDbConnection());
            else
                entitySvc.Upsert(entities, context.Database.GetDbConnection());
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

        ILynxDatabaseService<IdOnly> entitySvc;
        using (var context = lynxHarness.CreateContext())
        {
            entitySvc = provider.CreateService<IdOnly>(context.Model);

            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction();
            if (useAsync)
                await entitySvc.InsertAsync(entities, context.Database.GetDbConnection());
            else
                entitySvc.Insert(entities, context.Database.GetDbConnection());
            transaction.Commit();
        }

        entities = entities
            .Concat(Enumerable.Range(100, 10).Select(IdOnly))
            .ToList();

        using (var context = lynxHarness.CreateContext())
        {
            if (useAsync)
                await entitySvc.UpsertAsync(entities, context.Database.GetDbConnection());
            else
                entitySvc.Upsert(entities, context.Database.GetDbConnection());
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
    
    public static Customer Customer(int id) => new()
    {
        Id = id,
        Name = $"Customer {id}",
        Tags = [$"Tag 1 {id}", $"Tag 2 {id}"],
        OrderContact = new CustomerContactInfo()
        {
            ContactId = id,
            Contact = new Contact()
            {
                Id = id,
            }
        },
        BillingAddress = new Address()
        {
            Street = $"Billing street {id}",
            City = $"Billing city {id}"
        },
        ShippingAddress = new Address()
        {
            Street = $"Shipping street {id}",
            City = $"Shipping city {id}"
        }
    };


    public static City City(int id) => new()
    {
        Id = new CityId(id),
        Name = $"City {id}",
        Country = id % 2 == 0 ? $"Country {id}" : null,
        Location = new CityLocation()
        {
            Elevation = id * 10,
            Latitude = id * 1.1m,
            Longitude = id * 100.1,
            StreetWidths = Enumerable.Range(0, id).Select(i => (short)i).ToArray()
        },
        Population = id % 3 == 0 ? null : id * 1000,
        LegalSystem = new LegalSystem(id % 2 == 0, id % 3 == 0),
        Buildings = Enumerable.Range(0, id)
            .Select(i => new Building() { Name = $"Building {i}" })
            .ToList(),
        FamousBuilding = id % 2 == 0
            ? new Building()
            {
                Name = $"Famous building {id}",
                Owner = new BuildingOwner(),
                Purpose = (BuildingPurpose)(id * 5)
            }
            : null
    };

    public static ConverterEntity ConverterEntity(int id) => new()
    {
        Id1 = new StringId(id.ToString()),
        Id2 = new CityId(id * 2),
        IntValue = id % 2,
        NullableId = id % 2 == 0 ? new StringId((id * 3).ToString()) : null,
        ReferenceId = id % 3 == 0 ? new ReferenceStringId((id * 4).ToString()) : null,
        StringValue = id % 2 == 0 ? $"String {id}" : null,
        IntValueNull = id % 3 == 0 ? null : id,
        NullableValueId = id % 2 == 0 ? new CityId(id * 4) : null,
        ReferenceIntId = id % 3 == 0 ? new ReferenceIntId(id * 5) : null,
        ReferenceNullableIntId = id % 2 == 0 ? new ReferenceNullableIntId(id * 6) : null,
        Enum = Enum.GetValues<BuildingPurpose>()[id % Enum.GetValues<BuildingPurpose>().Length],
    };
    
    public static IdOnly IdOnly(int id) => new()
    {
        Id = id,
    };
}