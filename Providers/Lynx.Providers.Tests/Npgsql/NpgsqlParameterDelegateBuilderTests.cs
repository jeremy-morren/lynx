using System.Collections.Immutable;
using System.Data;
using Lynx.Providers.Common.Entities;
using Lynx.Providers.Common.Reflection;
using Lynx.Provider.Npgsql;
using Npgsql;
using NpgsqlTypes;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

namespace Lynx.Providers.Tests.Npgsql;

public class NpgsqlParameterDelegateBuilderTests : ParameterDelegateBuilderTestsBase
{
    [Theory]
    [MemberData(nameof(GetEntityTypes))]
    public void BuildAddDelegate(Type entityType)
    {
        using var harness = new NpgsqlTestHarness([nameof(BuildAddDelegate), entityType.Name]);
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.Create(entityType, context.Model);
        var action = AddParameterDelegateBuilder<NpgsqlCommand, NpgsqlProviderDelegateBuilder>.Build(entity);
        var command = new NpgsqlCommand();
        action(command);

        VerifyEntityInfo(entity, command);
    }

    [Fact]
    public void BuildCitySetParametersDelegate()
    {
        using var harness = new NpgsqlTestHarness([nameof(BuildCitySetParametersDelegate)]);
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.Create(typeof(City), context.Model);

        var addParameters = AddParameterDelegateBuilder<NpgsqlCommand, NpgsqlProviderDelegateBuilder>.Build(entity);

        var command = new NpgsqlCommand();
        addParameters(command);

        var setParameters = SetParameterValueDelegateBuilder<NpgsqlCommand, NpgsqlProviderDelegateBuilder, City>.Build(entity);

        Assert.All(GetTestCities(), city =>
        {
            setParameters(command, city);

            Verify([nameof(City.Id)], city.Id.Value);
            Verify([nameof(City.Name)], city.Name);
            Verify([nameof(City.Country)], city.Country);
            Verify([nameof(City.Population)], city.Population);
            Verify([nameof(City.Location), nameof(CityLocation.Latitude)], city.Location?.Latitude);
            Verify([nameof(City.Location), nameof(CityLocation.Longitude)], city.Location?.Longitude);
            Verify([nameof(City.Location), nameof(CityLocation.Elevation)], city.Location?.Elevation);
            Verify([nameof(City.Location), nameof(CityLocation.StreetWidths)], city.Location?.StreetWidths);
            Verify([nameof(City.LegalSystem), nameof(LegalSystem.CommonLaw)], city.LegalSystem.CommonLaw);
            Verify([nameof(City.LegalSystem), nameof(LegalSystem.CivilLaw)], city.LegalSystem.CivilLaw);
            Verify([nameof(City.FamousBuilding), nameof(Building.Name)], city.FamousBuilding?.Name);
            Verify([nameof(City.FamousBuilding), nameof(Building.Purpose)], city.FamousBuilding?.Purpose);
            Verify([nameof(City.FamousBuilding), nameof(Building.Owner), nameof(BuildingOwner.Company)], city.FamousBuilding?.Owner?.Company);
            Verify([nameof(City.FamousBuilding), nameof(Building.Owner), nameof(BuildingOwner.Since)], city.FamousBuilding?.Owner?.Since);
            Verify([nameof(City.Buildings)], city.Buildings);

            command.Parameters.Count.ShouldBe(entity.GetAllScalarColumns().Count() + entity.Keys.Count);

            return;

            void Verify(ImmutableArray<string> property, object? value) =>
                VerifyParameter(property, value, entity, command);
        });
    }

    [Fact]
    public void BuildCustomerSetParametersDelegate()
    {
        using var harness = new NpgsqlTestHarness([nameof(BuildCustomerSetParametersDelegate)]);
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.Create(typeof(Customer), context.Model);

        var addParameters = AddParameterDelegateBuilder<NpgsqlCommand, NpgsqlProviderDelegateBuilder>.Build(entity);

        var command = new NpgsqlCommand();
        addParameters(command);

        var setParameters = SetParameterValueDelegateBuilder<NpgsqlCommand, NpgsqlProviderDelegateBuilder, Customer>.Build(entity);

        Assert.All(GetTestCustomers(), customer =>
        {
            setParameters(command, customer);

            Verify([nameof(Customer.Id)], customer.Id);
            Verify([nameof(Customer.Name)], customer.Name);
            Verify([nameof(Customer.Tags)], customer.Tags);
            Verify([nameof(Customer.BillingAddress), nameof(Address.City)], customer.BillingAddress?.City);
            Verify([nameof(Customer.BillingAddress), nameof(Address.Street)], customer.BillingAddress?.Street);
            Verify([nameof(Customer.ShippingAddress), nameof(Address.City)], customer.ShippingAddress?.City);
            Verify([nameof(Customer.ShippingAddress), nameof(Address.Street)], customer.ShippingAddress?.Street);
            Verify([nameof(Customer.OrderContact), nameof(CustomerContactInfo.ContactId)], customer.OrderContact?.ContactId);
            Verify([nameof(Customer.OrderContact), nameof(CustomerContactInfo.LastContact)], customer.OrderContact?.LastContact);
            Verify([nameof(Customer.InvoiceContact), nameof(CustomerContactInfo.ContactId)], customer.InvoiceContact?.ContactId);
            Verify([nameof(Customer.InvoiceContact), nameof(CustomerContactInfo.LastContact)], customer.InvoiceContact?.LastContact);

            command.Parameters.Count.ShouldBe(entity.Keys.Count + entity.GetAllScalarColumns().Count());

            return;

            void Verify(ImmutableArray<string> property, object? value) =>
                VerifyParameter(property, value, entity, command);
        });
    }

    [Fact]
    public void BuildConverterEntitySetParametersDelegate()
    {
        using var harness = new NpgsqlTestHarness([nameof(BuildConverterEntitySetParametersDelegate)]);
        using var context = harness.CreateContext();

        var entity = EntityInfoFactory.Create(typeof(ConverterEntity), context.Model);

        var addParameters = AddParameterDelegateBuilder<NpgsqlCommand, NpgsqlProviderDelegateBuilder>.Build(entity);

        var command = new NpgsqlCommand();
        addParameters(command);

        var setParameters = SetParameterValueDelegateBuilder<NpgsqlCommand, NpgsqlProviderDelegateBuilder, ConverterEntity>.Build(entity);

        Assert.All(GetTestConverterEntities(), value =>
        {
            setParameters(command, value);
            
            Verify([nameof(ConverterEntity.Id1)], value.Id1.Value);
            Verify([nameof(ConverterEntity.Id2)], value.Id2.Value);
            Verify([nameof(ConverterEntity.NullableId)], value.NullableId?.Value);
            Verify([nameof(ConverterEntity.NullableValueId)], value.NullableValueId?.Value);
            Verify([nameof(ConverterEntity.ReferenceId)], value.ReferenceId?.Value);
            Verify([nameof(ConverterEntity.ReferenceNullableIntId)], value.ReferenceNullableIntId?.Value);
            Verify([nameof(ConverterEntity.ReferenceIntId)], value.ReferenceIntId?.Value);
            Verify([nameof(ConverterEntity.IntValue)], value.IntValue);
            Verify([nameof(ConverterEntity.StringValue)], value.StringValue);
            Verify([nameof(ConverterEntity.IntValueNull)], value.IntValueNull);
            Verify([nameof(ConverterEntity.Enum)], value.Enum?.ToString());
            
            command.Parameters.Count.ShouldBe(entity.Keys.Count + entity.GetAllScalarColumns().Count());

            return;

            void Verify(ImmutableArray<string> property, object? v) =>
                VerifyParameter(property, v, entity, command);
        });
    }

    /// <summary>
    /// Test type mappings. See https://www.npgsql.org/doc/types/basic.html#write-mappings
    /// </summary>
    /// <returns></returns>
    [Theory]
    [InlineData(DbType.Boolean, NpgsqlDbType.Boolean)]
    [InlineData(DbType.Int16, NpgsqlDbType.Smallint)]
    [InlineData(DbType.Int32, NpgsqlDbType.Integer)]
    [InlineData(DbType.Int64, NpgsqlDbType.Bigint)]
    [InlineData(DbType.Single, NpgsqlDbType.Real)]
    [InlineData(DbType.Double, NpgsqlDbType.Double)]
    [InlineData(DbType.Decimal, NpgsqlDbType.Numeric)]
    [InlineData(DbType.VarNumeric, NpgsqlDbType.Numeric)]
    [InlineData(DbType.Currency, NpgsqlDbType.Money)]

    [InlineData(DbType.String, NpgsqlDbType.Text)]
    [InlineData(DbType.StringFixedLength, NpgsqlDbType.Text)]
    [InlineData(DbType.AnsiString, NpgsqlDbType.Text)]
    [InlineData(DbType.AnsiStringFixedLength, NpgsqlDbType.Text)]

    [InlineData(DbType.Binary, NpgsqlDbType.Bytea)]
    [InlineData(DbType.DateTime, NpgsqlDbType.TimestampTz)]
    [InlineData(DbType.DateTimeOffset, NpgsqlDbType.TimestampTz)]
    [InlineData(DbType.DateTime2, NpgsqlDbType.Timestamp)]
    [InlineData(DbType.Date, NpgsqlDbType.Date)]
    [InlineData(DbType.Time, NpgsqlDbType.Time)]
    public void TestTypeMappings(DbType dbType, NpgsqlDbType npgsqlDbType)
    {
        NpgsqlProviderDelegateBuilder.GetDbType(dbType).ShouldBe(npgsqlDbType);
    }
}