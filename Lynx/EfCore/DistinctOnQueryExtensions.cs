using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Lynx.EfCore.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Lynx.EfCore;

/// <summary>
/// Helpers to enable the use of the PostgreSQL DISTINCT ON clause in LINQ queries.
/// </summary>
public static partial class DistinctOnQueryExtensions
{
    /// <summary>
    /// Equivalent of <c>DistinctBy</c> that uses the PostgreSQL <c>DISTINCT ON</c> clause.
    /// </summary>
    /// <param name="source">Source query</param>
    /// <param name="keySelector">Key selector (properties to inside <c>DISTINCT ON</c>)</param>
    /// <param name="orderBySelector">Additional properties to sort by</param>
    /// <param name="orderByDescending">
    ///     Flags for whether to sort ascending (<c>false</c>) or descending (<c>true</c>).
    ///     Count must match total of properties in <paramref name="keySelector"/> and <paramref name="orderByDescending"/>
    /// </param>
    /// <param name="resultSelector">Selector for result columns. Cannot be an anonymous type due to EF core limitations.</param>
    /// <typeparam name="TEntity">The source type. Must be an entity registered in model</typeparam>
    /// <typeparam name="TResult">The result type. Can be a scalar type.</typeparam>
    /// <returns></returns>
    /// <remarks>
    /// See <see href="https://www.postgresql.org/docs/current/sql-select.html#SQL-DISTINCT">SELECT - SQL DISTINCT</see>
    /// and <see href="https://github.com/npgsql/efcore.pg/issues/894">Generate PostgreSQL DISTINCT ON</see>
    /// </remarks>
    [System.Diagnostics.Contracts.Pure, LinqTunnel]
    public static IQueryable<TResult> SqlDistinctOn<TEntity, TResult>(
        this IQueryable<TEntity> source,
        Expression<Func<TEntity, object?>> keySelector,
        Expression<Func<TEntity, object?>> orderBySelector,
        bool[] orderByDescending,
        Expression<Func<TEntity, TResult>>? resultSelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(orderBySelector);
        ArgumentNullException.ThrowIfNull(orderByDescending);

        var distinct = GetProperties(keySelector);
        var orderBy = GetProperties(orderBySelector);

        var context = source.GetDbContext();
        var entity = context.Model.GetEntityType(typeof(TEntity));

        IQueryable src = resultSelector != null
            ? source.Select(resultSelector)
            : source;
        using var command = src.CreateDbCommand();
        var sql = RewriteSelect(
            command.CommandText,
            entity.GetTableName()!,
            distinct.Select(p => GetColumnName<TEntity>(context, p)).ToList(),
            orderBy.Select(p => GetColumnName<TEntity>(context, p)).ToList(),
            orderByDescending);

        return context.Database.SqlQueryRaw<TResult>(sql, command.Parameters.Cast<object>().ToArray());
    }

    private static List<PropertyInfo> GetProperties<TIn, TOut>(Expression<Func<TIn, TOut>> selector)
    {
        //TODO: Handle complex/referenced entities

        var body = selector.Body;

        //For object selectors, the body will be a Convert(expression, object)
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
            body = unary.Operand;

        //Either a simple property x => x.Property
        //Or a complex property x => new { x.Property1, x.Property2 }


        if (body is MemberExpression { Member: PropertyInfo property1 })
            //Simple property
            return [property1];
        switch (body)
        {
            case NewExpression newExp:
            {
                // New anonymous object
                var properties = new List<PropertyInfo>(newExp.Arguments.Count);
                foreach (var argument in newExp.Arguments)
                {
                    if (argument is MemberExpression { Member: PropertyInfo property })
                        properties.Add(property);
                    else
                        throw new ArgumentException($"Could not extract property from expression {argument}");
                }
                return properties;
            }
            case MemberInitExpression memberInit:
            {
                // New object with initializers
                var properties = new List<PropertyInfo>(memberInit.Bindings.Count);
                foreach (var binding in memberInit.Bindings)
                {
                    if (binding is MemberAssignment { Expression: MemberExpression { Member: PropertyInfo property } })
                        properties.Add(property);
                    else
                        throw new ArgumentException($"Could not extract property from expression {binding}");
                }
                return properties;
            }
            default:
                throw new ArgumentException($"Could not extract property list from expression {selector}");
        }
    }

    /// <summary>
    /// Rewrites the SQL to use PostgreSQL's DISTINCT ON clause.
    /// </summary>
    private static string RewriteSelect(
        string sql,
        string tableName,
        List<string> distinctOn,
        List<string> orderBy,
        bool[] orderByDescending)
    {
        if (distinctOn.Count + orderBy.Count != orderByDescending.Length)
            throw new ArgumentException(
                $"The number of distinct and order by properties ({distinctOn.Count + orderBy.Count}) does not match the number of descending flags ({orderByDescending.Length}).");

        if (!sql.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("SQL must start with SELECT", nameof(sql));

        if (sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("SQL must not contain ORDER BY", nameof(sql));

        var fromIndex = sql.IndexOf("FROM ", StringComparison.OrdinalIgnoreCase);
        if (fromIndex < 0)
            throw new ArgumentException("SQL must contain FROM", nameof(sql));

        var tableAlias = GetTableAlias(tableName, sql);

        var sb = new StringBuilder(sql.Length);
        sb.Append("SELECT DISTINCT ON (");
        foreach (var property in distinctOn)
            sb.Append($"{tableAlias}.{property}, ");
        sb.Length -= 2;
        sb.Append(") ");

        var selectColumns = sql.Substring("SELECT ".Length, fromIndex - "SELECT ".Length);
        sb.Append(selectColumns);
        if (!selectColumns.Contains(','))
            sb.Append(" AS \"Value\""); //For a single column, SqlQueryRaw expects it to be named "Value"

        sb.AppendLine(sql.Substring(fromIndex));

        sb.Append("ORDER BY ");
        for (var i = 0; i < distinctOn.Count; i++)
        {
            var ascending = orderByDescending[i] ? "DESC" : "ASC";
            sb.Append($"{tableAlias}.{distinctOn[i]} {ascending}, ");
        }
        for (var i = 0; i < orderBy.Count; i++)
        {
            var ascending = orderByDescending[distinctOn.Count + i] ? "DESC" : "ASC";
            sb.Append($"{tableAlias}.{orderBy[i]} {ascending}, ");
        }
        sb.Length -= 2;
        return sb.ToString();
    }

    private static string GetColumnName<T>(DbContext context, PropertyInfo property)
    {
        var entity = context.Model.GetEntityType(typeof(T));
        var efProperty = entity.FindProperty(property) ??
                         throw new InvalidOperationException($"Property {property.Name} not found in {entity}.");
        return $"\"{efProperty.GetColumnName()}\"";
    }

    internal static string GetTableAlias(string tableName, string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(tableName);

        var matches = TableAliasRegex().Matches(sql);
        var match = matches.SingleOrDefault(m => m.Groups["TableName"].Value.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new ArgumentException($"Table '{tableName}' not found in SQL '{sql}'", nameof(sql));
        return match.Groups["Alias"].Value;
    }

    [GeneratedRegex("""
                    [\s\.]"(?<TableName>\w+)"\s+AS\s+(?<Alias>"*\w+"*)
                    """, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TableAliasRegex();
}