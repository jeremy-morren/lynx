using System.Data;
using Lynx.ExecutionPlan.Npgsql;
using Lynx.ExecutionPlan.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lynx.ExecutionPlan;

public static class QueryableExecutionPlanExtensions
{
    /// <summary>
    /// Gets the execution plan for the query from the database
    /// </summary>
    /// <param name="queryable"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static IExecutionPlan GetExecutionPlan<T>(this IQueryable<T> queryable) where T : class
    {
        using var command = queryable.CreateDbCommand();
        if (command.Connection != null && command.Connection.State != ConnectionState.Open)
            command.Connection.Open();
        return command switch
        {
            SqliteCommand sqlite => SqliteExecutionPlan.Create(sqlite),
            NpgsqlCommand npgsql => NpgsqlExecutionPlan.Create(npgsql),
            _ => throw new NotImplementedException($"Unsupported database {command.GetType()}")
        };
    }
}