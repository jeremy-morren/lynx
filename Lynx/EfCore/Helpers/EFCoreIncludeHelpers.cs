using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Lynx.EfCore.Helpers;

internal static class EfCoreIncludeHelpers
{
    /// <summary>
    /// Gets the path for the selector
    /// </summary>
    public static string GetMembers<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> selector)
    {
        var properties = GetMembers(selector.Body)
            .Reverse() // Reverse to get order from root to leaf
            .Select(m => m.Name);
        return string.Join(".", properties);
    }

    private static IEnumerable<MemberInfo> GetMembers(Expression expression)
    {
        while (true)
        {
            switch (expression)
            {
                case ParameterExpression:
                    yield break; //Reached the end
                case MemberExpression { Member: { } m } member:
                    yield return m;
                    expression = member.Expression ?? throw new InvalidOperationException("Member expression is null");
                    break;
                default:
                    throw new InvalidOperationException($"Unknown expression type {expression.NodeType}");
            }
        }
    }

    /// <summary>
    /// Gets include path from the root of the query.
    /// </summary>
    public static string GetFullIncludePath<TEntity, TProperty>(
        this IIncludableQueryable<TEntity, TProperty?> query)
        where TEntity : class
        where TProperty : class
    {
        var properties = GetIncludeLambdas(query.Expression)
            .Reverse() // Reverse to get order from root to leaf
            .Select(l => l.GetMember()?.Name);

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

    private static MemberInfo? GetMember(this LambdaExpression lambda)
    {
        var expression = lambda.Body;
        while (true)
        {
            switch (expression)
            {
                case MemberExpression { Member: { } member }:
                    return member;
                case MethodCallExpression { Arguments.Count: > 0 } method:
                    expression = method.Arguments[0];
                    break;
                default:
                    //Unknown expression
                    return null;
            }
        }
    }
}