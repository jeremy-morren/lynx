using System.Data.Common;
using Lynx.Providers.Common;
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

#if NET10_0_OR_GREATER
    internal static async Task TestOrders(
        ILynxProvider provider, ProviderTestType type, Func<string, ITestHarness> createHarness)
    {
        var orders = Enumerable.Range(0, 6).Select(Order).ToList();

        ILynxEntityService<Order> orderSvc;
        using var lynxHarness = createHarness("lynx");
        using (var context = lynxHarness.CreateContext())
        {
            orderSvc = provider.CreateService<Order>(context.Model);

            context.Database.EnsureCreated();

            using var transaction = context.Database.BeginTransaction();
            await Write(orderSvc, orders, false, transaction.GetDbTransaction());
            transaction.Commit();
        }

        // Change the orders (including setting/clearing optional complex types) and add new ones
        foreach (var o in orders)
        {
            o.ShippingAddress = o.ShippingAddress == null ? Address(o.Id + 100) : null;
            o.ExtraDetails = o.ExtraDetails == null ? OrderDetails(o.Id + 100) : null;
            o.Lines.Add(OrderLine(o.Id + 100));
        }
        orders.AddRange(Enumerable.Range(6, 3).Select(Order));

        using (var context = lynxHarness.CreateContext())
        {
            using var transaction = context.Database.BeginTransaction();
            await Write(orderSvc, orders, true, transaction.GetDbTransaction());
            transaction.Commit();
        }

        using var manualHarness = createHarness("manual");
        using (var context = manualHarness.CreateContext())
        {
            context.Database.EnsureCreated();
            context.Orders.AddRange(orders);
            context.SaveChanges();
        }

        using (var lynxContext = lynxHarness.CreateContext())
        using (var manualContext = manualHarness.CreateContext())
        {
            var lynxOrders = lynxContext.Orders.AsNoTracking().OrderBy(o => o.Id).ToList();
            lynxOrders.Should().BeEquivalentTo(manualContext.Orders.AsNoTracking().OrderBy(o => o.Id).ToList());

            // Query inside JSON and optional complex types
            lynxContext.Orders.Count(o => o.ShippingAddress != null)
                .ShouldBe(orders.Count(o => o.ShippingAddress != null));
            lynxContext.Orders.Count(o => o.ExtraDetails != null)
                .ShouldBe(orders.Count(o => o.ExtraDetails != null));
            // NB: EF Core 10 SQLite doesn't translate a custom JSON property name (Notes_Json) in queries, so compare with EF Core
            lynxContext.Orders.Count(o => o.Details.Notes == "Notes 1")
                .ShouldBe(manualContext.Orders.Count(o => o.Details.Notes == "Notes 1"));
            lynxContext.Orders.Count(o => o.Details.Delivery != null && o.Details.Delivery.Street == "Street 1002").ShouldBe(1);
            lynxContext.Orders.Count(o => o.Lines.Any(l => l.Product == "Product 101")).ShouldBe(1);
        }

        return;

        async Task Write(ILynxEntityService<Order> svc, List<Order> items, bool upsert, DbTransaction transaction)
        {
            switch (type)
            {
                case ProviderTestType.Sync when upsert:
                    svc.Upsert(items, transaction);
                    break;
                case ProviderTestType.Sync:
                    svc.Insert(items, transaction);
                    break;
                case ProviderTestType.Async when upsert:
                    await svc.UpsertAsync(items, transaction);
                    break;
                case ProviderTestType.Async:
                    await svc.InsertAsync(items, transaction);
                    break;
                case ProviderTestType.AsyncEnumerable when upsert:
                    await svc.UpsertAsync(items.ToAsyncEnumerable(), transaction);
                    break;
                case ProviderTestType.AsyncEnumerable:
                    await svc.InsertAsync(items.ToAsyncEnumerable(), transaction);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }

    public static Order Order(int id) => new()
    {
        Id = id,
        BillingAddress = Address(id),
        ShippingAddress = id % 2 == 0 ? Address(id + 50) : null,
        LegalSystem = id % 3 == 0 ? null : new LegalSystem(id % 2 == 0, true),
        Details = OrderDetails(id),
        ExtraDetails = id % 2 == 1 ? OrderDetails(id + 50) : null,
        Lines = Enumerable.Range(0, id % 3).Select(i => OrderLine(id * 10 + i)).ToList()
    };

    private static Address Address(int id) => new()
    {
        Street = $"Street {id}",
        City = $"City {id}"
    };

    private static OrderDetails OrderDetails(int id) => new()
    {
        Notes = $"Notes {id}",
        City = id % 2 == 0 ? new CityId(id) : null,
        PlacedAt = id % 3 == 0 ? null : MillisecondClock.Now,
        Tags = Enumerable.Range(0, id % 4).Select(i => $"Tag {i}").ToArray(),
        Delivery = id % 2 == 0 ? Address(id + 1000) : null,
        Lines = Enumerable.Range(0, id % 3).Select(i => OrderLine(id * 10 + i)).ToList()
    };

    private static OrderLine OrderLine(int id) => new()
    {
        Product = $"Product {id}",
        Quantity = id * 1.5m,
        Due = id % 2 == 0 ? MillisecondClock.LocalNow : null,
        Destination = id % 3 == 0 ? Address(id + 2000) : null
    };
#endif

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