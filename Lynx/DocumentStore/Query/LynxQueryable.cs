using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Lynx.DocumentStore.Query;

internal class LynxQueryable<T> : EntityQueryable<T>
{
    public DbContext Context => ((LynxQueryProvider)Provider).Context;

    public LynxQueryable(LynxQueryProvider provider, Expression expression)
        : base(provider, expression)
    {
    }
}