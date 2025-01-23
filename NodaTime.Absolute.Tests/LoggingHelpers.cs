using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace NodaTime.Absolute.Tests;

public static class LoggingHelpers
{
    public static IQueryable<T> Log<T>(this IQueryable<T> query, ITestOutputHelper output)
    {
        output.WriteLine(query.ToQueryString());
        output.WriteLine(string.Empty);
        return query;
    }
}