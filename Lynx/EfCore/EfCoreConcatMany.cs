using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Lynx.EfCore.Helpers;

namespace Lynx.EfCore;

/// <summary>
/// Helpers to concatenate multiple queries into a single query.
/// </summary>
public static class EfCoreConcatMany
{
    /// <summary>
    /// Concatenates many queries into a single query.
    /// </summary>
    /// <remarks>
    /// Using EF Core's built in Concat method is brittle
    /// because it throws a <see cref="StackOverflowException"/> with enough queries (presumably recursion is used somewhere)
    /// </remarks>
    public static IQueryable<T> ConcatMany<T>(this IEnumerable<IQueryable<T>> queries)
    {
        ArgumentNullException.ThrowIfNull(queries);

        var list = queries.ToList();
        if (list.Count == 0)
            //No queries, return empty
            return Enumerable.Empty<T>().AsQueryable();

        //Collapse queries in batches, looping until we have one query left
        const int batchSize = 10;
        while (true)
        {
            if (list.Count == 1)
                return list[0]; // One query left, done

            list = list
                .Select((q, i) => new { Query = q, Index = i })
                .GroupBy(x => x.Index / batchSize)
                .Select(g => Concat(g.Select(x => x.Query).ToList()))
                .ToList();
        }
    }

    private static IQueryable<T> Concat<T>(List<IQueryable<T>> queries)
    {
        Debug.Assert(queries.Count > 0);
        if (queries.Count == 1)
            return queries[0];
        var query = queries[0];
        for (var i = 1; i < queries.Count; i++)
            query = query.Concat(queries[i]);
        return query;
    }
}