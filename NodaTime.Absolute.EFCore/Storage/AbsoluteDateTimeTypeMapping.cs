using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.DependencyInjection;
using NodaTime.Absolute.EFCore.Serialization;
using NodaTime.Absolute.EFCore.Storage.Converters;

namespace NodaTime.Absolute.EFCore.Storage;

internal class AbsoluteDateTimeTypeMapping : RelationalTypeMapping
{
    public AbsoluteDateTimeTypeMapping(IDateTimeZoneProvider timeZoneProvider,
        IServiceProvider services)
        : this(CreateParameters(timeZoneProvider, services))
    {
    }

    protected AbsoluteDateTimeTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters)
    {
    }

    protected override string SqlLiteralFormatString => "'{0}'";

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters) =>
        new AbsoluteDateTimeTypeMapping(parameters);

    private static RelationalTypeMappingParameters CreateParameters(
        IDateTimeZoneProvider timeZoneProvider,
        IServiceProvider services)
    {
        var provider = services.GetService<IDatabaseProvider>()
                       ?? throw new InvalidOperationException("No database provider registered");
        return provider.Name switch
        {
            "Microsoft.EntityFrameworkCore.Sqlite" => new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(
                    typeof(AbsoluteDateTime),
                    new AbsoluteDateTimeStringValueConverter(timeZoneProvider),
                    jsonValueReaderWriter: new AbsoluteDateTimeValueJsonReaderWriter(timeZoneProvider)),
                "TEXT"),
            "Npgsql.EntityFrameworkCore.PostgreSQL" => new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(
                    typeof(AbsoluteDateTime),
                    new AbsoluteDateTimeJsonValueConverter(timeZoneProvider),
                    jsonValueReaderWriter: new AbsoluteDateTimeValueJsonReaderWriter(timeZoneProvider)),
                "jsonb"),
            _ => throw new NotImplementedException($"Database provider {provider.Name} is not supported")
        };;
    }
}