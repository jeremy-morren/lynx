using Lynx.Provider.Common;
using Lynx.Provider.Common.Entities;
using Lynx.Provider.Sqlite;
using Microsoft.EntityFrameworkCore;
// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Providers.Tests.Sqlite;

public class SqliteProviderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteCustomers(bool useAsync)
    {
        using var lynxHarness = new SqliteTestHarness();

        List<Customer> customers =
        [
            Customer(1),
            Customer(2) with { Tags = null },
            Customer(3)
        ];
        using (var context = lynxHarness.CreateContext())
        {
            IEnumerable<Contact> contacts = customers.Select(c => c.InvoiceContact?.Contact)
                .Concat(customers.Select(c => c.OrderContact?.Contact))
                .Where(c => c != null)!;

            context.Database.EnsureCreated();

            var customerSvc = SqliteLynxProvider.CreateService<Customer>(context.Model);
            var contactSvc = SqliteLynxProvider.CreateService<Contact>(context.Model);

            using var transaction = context.Database.BeginTransaction();

            if (useAsync)
            {
                await contactSvc.InsertAsync(context.Database.GetDbConnection(), contacts);
                await customerSvc.InsertAsync(context.Database.GetDbConnection(), customers);
            }
            else
            {
                contactSvc.Insert(context.Database.GetDbConnection(), contacts);
                customerSvc.Insert(context.Database.GetDbConnection(), customers);
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

            var customerSvc = SqliteLynxProvider.CreateService<Customer>(context.Model);
            var contactSvc = SqliteLynxProvider.CreateService<Contact>(context.Model);

            if (useAsync)
            {
                await contactSvc.UpsertAsync(context.Database.GetDbConnection(), contacts);
                await customerSvc.UpsertAsync(context.Database.GetDbConnection(), customers);
            }
            else
            {
                contactSvc.Upsert(context.Database.GetDbConnection(), contacts);
                customerSvc.Upsert(context.Database.GetDbConnection(), customers);
            }
        }

        using var manualHarness = new SqliteTestHarness();
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteCities(bool useAsync)
    {
        var cities = Enumerable.Range(0, 5).Select(City).ToList();

        using var lynxHarness = new SqliteTestHarness();
        using (var context = lynxHarness.CreateContext())
        {
            context.Database.EnsureCreated();

            var citySvc = SqliteLynxProvider.CreateService<City>(context.Model);

            using var transaction = context.Database.BeginTransaction();

            if (useAsync)
                await citySvc.InsertAsync(context.Database.GetDbConnection(), cities);
            else
                citySvc.Insert(context.Database.GetDbConnection(), cities);
            transaction.Commit();
        }

        foreach (var c in cities)
            c.Name = "New name";
        cities.AddRange(Enumerable.Range(5, 5).Select(City));

        using (var context = lynxHarness.CreateContext())
        {
            var citySvc = SqliteLynxProvider.CreateService<City>(context.Model);

            if (useAsync)
                await citySvc.UpsertAsync(context.Database.GetDbConnection(), cities);
            else
                citySvc.Upsert(context.Database.GetDbConnection(), cities);
        }

        using var manualHarness = new SqliteTestHarness();
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

    private static City City(int id) => new()
    {
        Id = new CityId(id),
        Name = $"City {id}",
        Country = id % 2 == 0 ? $"Country {id}" : null,
        Location = new CityLocation()
        {
            Elevation = id * 10,
            Latitude = id * 1.1m,
            Longitude = id * 100.1
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

    private static Customer Customer(int id) => new()
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
}