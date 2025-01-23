using Microsoft.EntityFrameworkCore.Storage;

namespace NodaTime.Absolute.EFCore.Storage;

internal class AbsoluteDateTimeTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
{
    private readonly AbsoluteDateTimeTypeMapping _mapping;

    public AbsoluteDateTimeTypeMappingSourcePlugin(IDateTimeZoneProvider timeZoneProvider)
    {
        _mapping = new AbsoluteDateTimeTypeMapping(timeZoneProvider);
    }

    public RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        return mappingInfo.ClrType == typeof(AbsoluteDateTime) ? _mapping : null;
    }
}