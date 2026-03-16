using System.Data.Common;
using Lynx.Providers.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Providers.Tests;

public class ProviderTestsBase
{
    protected virtual void SetIdentityInsertOn<T>(DbContext context, DbTransaction transaction) where T : class {}

    internal async Task TestCustomers(
        ILynxProvider provider, ProviderTestType type, Func<string, ITestHarness> createHarness)
    {
        using var lynxHarness = createHarness("lynx");

        List<Customer> customers =
        [
            Customer(1),
            Customer(2) with { Tags = null },
            Customer(3),
            Customer(4) with { Cats = null},
        ];

        ILynxEntityService<Customer> customerSvc;
        ILynxEntityService<Contact> contactSvc;

        using (var context = lynxHarness.CreateContext())
        {
            customerSvc = provider.CreateService<Customer>(context.Model);
            contactSvc = provider.CreateService<Contact>(context.Model);

            IEnumerable<Contact> contacts = customers.Select(c => c.InvoiceContact?.Contact)
                .Concat(customers.Select(c => c.OrderContact?.Contact))
                .Where(c => c != null)!;

            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction();
            var dbTransaction = transaction.GetDbTransaction();

            switch (type)
            {
                case ProviderTestType.Sync:
                    contactSvc.Insert(contacts, dbTransaction);
                    customerSvc.Insert(customers, dbTransaction);
                    break;
                case ProviderTestType.Async:
                    await contactSvc.InsertAsync(contacts, dbTransaction);
                    await customerSvc.InsertAsync(customers, dbTransaction);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await contactSvc.InsertAsync(contacts.ToAsyncEnumerable(), dbTransaction);
                    await customerSvc.InsertAsync(customers.ToAsyncEnumerable(), dbTransaction);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            transaction.Commit();
        }

        foreach (var c in customers)
            c.Name = "New name";
        customers.AddRange([Customer(10), Customer(11)]);

        using (var context = lynxHarness.CreateContext())
        {
            IEnumerable<Contact> contacts = customers.Select(c => c.InvoiceContact?.Contact)
                .Concat(customers.Select(c => c.OrderContact?.Contact))
                .Where(c => c != null)!;

            using var transaction = context.Database.BeginTransaction();
            var dbTransaction = transaction.GetDbTransaction();
            switch (type)
            {
                case ProviderTestType.Sync:
                    contactSvc.Upsert(contacts, dbTransaction);
                    customerSvc.Upsert(customers, dbTransaction);
                    break;
                case ProviderTestType.Async:

                    await contactSvc.UpsertAsync(contacts, dbTransaction);
                    await customerSvc.UpsertAsync(customers, dbTransaction);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await contactSvc.UpsertAsync(contacts.ToAsyncEnumerable(), dbTransaction);
                    await customerSvc.UpsertAsync(customers.ToAsyncEnumerable(), dbTransaction);
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

            using var transaction = context.Database.BeginTransaction();
            context.Customers.AddRange(customers);
            context.SaveChanges();
            transaction.Commit();
        }

        using (var lynxContext = lynxHarness.CreateContext())
        using (var manualContext = manualHarness.CreateContext())
        {
            lynxContext.Customers.AsNoTracking().ToList()
                .Should().BeEquivalentTo(manualContext.Customers.AsNoTracking().ToList());

            lynxContext.Customers.AsNoTracking()
                .Where(c => c.Cats!.Count > 0)
                .ShouldNotBeEmpty();
        }
    }

    internal static async Task TestCities(
        ILynxProvider provider, ProviderTestType type, Func<string, ITestHarness> createHarness)
    {
        var cities = Enumerable.Range(0, 5).Select(City).ToList();

        ILynxEntityService<City> citySvc;
        using var lynxHarness = createHarness("lynx");
        using (var context = lynxHarness.CreateContext())
        {
            citySvc = provider.CreateService<City>(context.Model);

            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction();
            var dbTransaction = transaction.GetDbTransaction();

            switch (type)
            {
                case ProviderTestType.Sync:
                    citySvc.Insert(cities, dbTransaction);
                    break;
                case ProviderTestType.Async:
                    await citySvc.InsertAsync(cities, dbTransaction);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await citySvc.InsertAsync(cities.ToAsyncEnumerable(), dbTransaction);
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
            using var transaction = context.Database.BeginTransaction();
            var dbTransaction = transaction.GetDbTransaction();
            switch (type)
            {
                case ProviderTestType.Sync:
                    citySvc.Upsert(cities, dbTransaction);
                    break;
                case ProviderTestType.Async:
                    await citySvc.UpsertAsync(cities, dbTransaction);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await citySvc.UpsertAsync(cities.ToAsyncEnumerable(), dbTransaction);
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

        ILynxEntityService<ConverterEntity> entitySvc;
        using (var context = lynxHarness.CreateContext())
        {
            entitySvc = provider.CreateService<ConverterEntity>(context.Model);

            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction();
            var dbTransaction = transaction.GetDbTransaction();
            switch (type)
            {
                case ProviderTestType.Sync:
                    entitySvc.Insert(entities, dbTransaction);
                    break;
                case ProviderTestType.Async:
                    await entitySvc.InsertAsync(entities, dbTransaction);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await entitySvc.InsertAsync(entities.ToAsyncEnumerable(), dbTransaction);
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
            using var transaction = context.Database.BeginTransaction();
            var dbTransaction = transaction.GetDbTransaction();
            switch (type)
            {
                case ProviderTestType.Sync:
                    entitySvc.Upsert(entities, dbTransaction);
                    break;
                case ProviderTestType.Async:
                    await entitySvc.UpsertAsync(entities, dbTransaction);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await entitySvc.UpsertAsync(entities.ToAsyncEnumerable(), dbTransaction);
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

    internal async Task TestIdOnly(
        ILynxProvider provider, ProviderTestType type, Func<string, ITestHarness> createHarness)
    {
        var entities = Enumerable.Range(10, 10).Select(IdOnly).ToList();
        
        using var lynxHarness = createHarness("lynx");

        ILynxEntityService<IdOnly> entitySvc;
        using (var context = lynxHarness.CreateContext())
        {
            entitySvc = provider.CreateService<IdOnly>(context.Model);

            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction();
            var dbTransaction = transaction.GetDbTransaction();
            switch (type)
            {
                case ProviderTestType.Sync:
                    entitySvc.Insert(entities, dbTransaction);
                    break;
                case ProviderTestType.Async:
                    await entitySvc.InsertAsync(entities, dbTransaction);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await entitySvc.InsertAsync(entities.ToAsyncEnumerable(), dbTransaction);
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
            var dbTransaction = transaction.GetDbTransaction();
            switch (type)
            {
                case ProviderTestType.Sync:
                    entitySvc.Upsert(entities, dbTransaction);
                    break;
                case ProviderTestType.Async:
                    await entitySvc.UpsertAsync(entities, dbTransaction);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await entitySvc.UpsertAsync(entities.ToAsyncEnumerable(), dbTransaction);
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

            using var transaction = context.Database.BeginTransaction();
            SetIdentityInsertOn<IdOnly>(context, transaction.GetDbTransaction());
            context.IdOnly.AddRange(entities);
            context.SaveChanges();
            transaction.Commit();
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
        },
        Cat = new Cat()
        {
            Name = $"Cat {id}",
        },
        Cats = Enumerable.Range(10,5)
            .Select(i => new Cat()
            {
                Name = $"Cat {i}"
            })
            .ToList(),
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