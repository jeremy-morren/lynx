using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Lynx.DocumentStore.Query;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class LynxQueryProvider : EntityQueryProvider
{
    public DbContext Context { get; }

    public LynxQueryProvider(EntityQueryProvider provider, DbContext context)
        : base(GetQueryCompiler(provider))
    {
        Context = context;
    }

    public override IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new LynxQueryable<TElement>(this, expression);

    private static readonly Func<EntityQueryProvider, IQueryCompiler> GetQueryCompiler = BuildGetQueryCompiler();

    private static Func<EntityQueryProvider, IQueryCompiler> BuildGetQueryCompiler()
    {
        var field = typeof(EntityQueryProvider).GetField("_queryCompiler", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Field '_queryCompiler' not found on {typeof(EntityQueryProvider)}");

        var parameter = Expression.Parameter(typeof(EntityQueryProvider), "provider");
        var body = Expression.Field(parameter, field);
        return Expression.Lambda<Func<EntityQueryProvider, IQueryCompiler>>(body, parameter).Compile();
    }
}