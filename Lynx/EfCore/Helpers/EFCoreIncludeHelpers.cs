using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

namespace Lynx.EfCore.Helpers;

internal static class EfCoreIncludeHelpers
{
    /// <summary>
    /// Gets include path from the root of the query.
    /// </summary>
    public static string GetFullIncludePath<TEntity, TProperty>(
        this IIncludableQueryable<TEntity, IEnumerable<TProperty>?> query)
        where TEntity : class
        where TProperty : class
    {
        var properties = GetIncludeLambdas(query.Expression)
            .Reverse() // Reverse to get order from root to leaf
            .Select(l => l.GetMemberAccess().Name);

        return string.Join(".", properties);
    }

    private static IEnumerable<LambdaExpression> GetIncludeLambdas(Expression expression)
    {
        while (true)
        {
            const string include = nameof(EntityFrameworkQueryableExtensions.Include);
            const string thenInclude = nameof(EntityFrameworkQueryableExtensions.ThenInclude);
            //Get the property from the last include call
            if (expression is not MethodCallExpression
                {
                    Method: { Name: include or thenInclude } method,
                    Arguments: [
                        { } previous,
                        UnaryExpression { Operand: LambdaExpression lambda }
                    ]
                })
                yield break;

            yield return lambda;

            //Check if we have reached the root include
            if (method.Name == include)
                yield break;

            expression = previous;
        }
    }
}