using Lynx.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Lynx.DocumentStore.Query;

internal static class LynxQueryableHelpers
{
    public static LynxQueryable<T> CreateLynxQueryable<T>(this DbContext context)
        where T : class
    {
        context.Model.GetEntityType(typeof(T)); //Will throw if the entity type is not found

        var set = context.Set<T>().AsNoTracking();
        var provider = new LynxQueryProvider((EntityQueryProvider)set.Provider, context);
        return new LynxQueryable<T>(provider, set.Expression);
    }
}