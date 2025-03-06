using Lynx.Provider.Common;
using Lynx.Provider.Common.Entities;
using Lynx.Provider.Sqlite;
using Microsoft.EntityFrameworkCore;
// ReSharper disable MethodHasAsyncOverload
// ReSharper disable UseAwaitUsing

namespace Lynx.Providers.Tests.Sqlite;

public class InsertSqliteTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteCustomers(bool useAsync)
    {
        using var lynxHarness = new SqliteTestHarness();

        List<Customer> customers =
        [
            Customer.New(1),
            Customer.New(2) with { Tags = null },
            Customer.New(3)
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
        customers.AddRange([Customer.New(4), Customer.New(5)]);

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
}