using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace NodaTime.Absolute.EFCore.Storage;

internal class AbsoluteDateTimeTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
{
    private readonly IDateTimeZoneProvider _timeZoneProvider;
    private readonly IServiceProvider _services;

    public AbsoluteDateTimeTypeMappingSourcePlugin(
        IDateTimeZoneProvider timeZoneProvider,
        IServiceProvider services)
    {
        _timeZoneProvider = timeZoneProvider;
        _services = services;
    }

    private AbsoluteDateTimeTypeMapping? _mapping;

    public RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        if (mappingInfo.ClrType != typeof(AbsoluteDateTime))
            return null;
        return _mapping ??= new AbsoluteDateTimeTypeMapping(_timeZoneProvider, _services);
    }
}