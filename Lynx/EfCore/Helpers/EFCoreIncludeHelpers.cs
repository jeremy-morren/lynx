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
        var properties = GetMembers(selector.Body).Select(m => m.Name);
        return string.Join(".", properties);
    }

    /// <summary>
    /// Gets include members from the root of the query.
    /// </summary>
    public static IEnumerable<PropertyInfo> GetFullIncludeMembers(this IQueryable query) =>
        GetIncludeLambdas(query.Expression)
            .SelectMany(l => l.GetMembers().Cast<PropertyInfo>());

    /// <summary>
    /// Gets include path from the root of the query.
    /// </summary>
    public static string GetFullIncludePath(this IQueryable query)
    {
        var members = GetFullIncludeMembers(query);
        return string.Join(".", members.Select(m => m.Name));
    }

    /// <summary>
    /// Gets the include lambdas from the query expression i.e. calls to .Include(x => x.Property)
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    private static List<LambdaExpression> GetIncludeLambdas(Expression expression)
    {
        //TODO: Handle string includes

        var result = new List<LambdaExpression>();
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
                break;

            result.Add(lambda);

            //Check if we have reached the root include
            if (method.Name == include)
                break;

            expression = previous;
        }

        result.Reverse(); // Reverse to get order from root to leaf
        return result;
    }

    /// <summary>
    /// Gets the members from a MemberExpression (e.g. x => x.Property.SubProperty)
    /// </summary>
    private static List<MemberInfo> GetMembers(Expression expression)
    {
        var result = new List<MemberInfo>();
        while (true)
        {
            if (expression is ParameterExpression)
                break; //Reached the end
            if (expression is MemberExpression { Member: { } m } member)
            {
                result.Add(m);
                expression = member.Expression ?? throw new InvalidOperationException("Member expression is null");
            }
            else
            {
                throw new NotImplementedException($"Unknown expression type {expression.NodeType}");
            }
        }
        result.Reverse();
        return result;
    }

    /// <summary>
    /// Gets the members from a lamba expression (e.g. x => x.Property.SubProperty)
    /// </summary>
    /// <param name="lambda"></param>
    /// <returns></returns>
    private static IEnumerable<MemberInfo> GetMembers(this LambdaExpression lambda)
    {
        var expression = lambda.Body;
        while (true)
        {
            switch (expression)
            {
                case MemberExpression member:
                    return GetMembers(member);
                case MethodCallExpression { Arguments.Count: > 0 } method:
                    expression = method.Arguments[0];
                    break;
                default:
                    throw new NotImplementedException($"Unknown expression type {expression.NodeType}");
            }
        }
    }
}