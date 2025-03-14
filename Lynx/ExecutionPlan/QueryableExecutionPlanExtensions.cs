using System.Data.Common;
using Lynx.ExecutionPlan.Npgsql;
using Lynx.ExecutionPlan.Sqlite;
using Lynx.Providers.Common;
using Microsoft.EntityFrameworkCore;

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
        using var _ = OpenConnection.Open(command.Connection);
        return command.GetType().Name switch
        {
            "SqliteCommand" => SqliteExecutionPlan.Create(command),
            "NpgsqlCommand" => NpgsqlExecutionPlan.Create(command),
            _ => throw new NotImplementedException($"Unsupported database {command.GetType()}")
        };
    }

    /// <summary>
    /// Gets the execution plan for the command from the database
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public static IExecutionPlan GetExecutionPlan(this DbCommand command)
    {
        using var _ = OpenConnection.Open(command.Connection);
        return command.GetType().Name switch
        {
            "SqliteCommand" => SqliteExecutionPlan.Create(command),
            "NpgsqlCommand" => NpgsqlExecutionPlan.Create(command),
            _ => throw new NotImplementedException($"Unsupported database {command.GetType()}")
        };
    }
}