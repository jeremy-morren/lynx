﻿using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace EFCore.BulkExtensions.SqlAdapters.Sqlite;

/// <inheritdoc/>
public class SqliteDialect : IQueryBuilderSpecialization
{
    /// <inheritdoc/>
    public List<DbParameter> ReloadSqlParameters(DbContext context, List<DbParameter> sqlParameters)
    {
        var sqlParametersReloaded = new List<DbParameter>();
        foreach (var parameter in sqlParameters)
        {
            var sqlParameter = parameter;
            sqlParametersReloaded.Add(new SqliteParameter(sqlParameter.ParameterName, sqlParameter.Value));
        }

        return sqlParametersReloaded;
    }

    /// <inheritdoc/>
    public string GetBinaryExpressionAddOperation(BinaryExpression binaryExpression)
    {
        return IsStringConcat(binaryExpression) ? "||" : "+";
    }

    /// <inheritdoc/>
    public (string, string) GetBatchSqlReformatTableAliasAndTopStatement(string sqlQuery, SqlType databaseType)
    {
        return (string.Empty, string.Empty);
    }

    /// <inheritdoc/>
    public ExtractedTableAlias GetBatchSqlExtractTableAliasFromQuery(string fullQuery, string tableAlias,
        string tableAliasSuffixAs)
    {
        var result = new ExtractedTableAlias();
        var match = Regex.Match(fullQuery, @"FROM (""[^""]+"")( AS ""[^""]+"")");
        result.TableAlias = match.Groups[1].Value;
        result.TableAliasSuffixAs = match.Groups[2].Value;
        result.Sql = fullQuery[(match.Index + match.Length)..];

        return result;
    }

    internal static bool IsStringConcat(BinaryExpression binaryExpression)
    {
        var methodProperty = binaryExpression.GetType().GetProperty("Method");
        if (methodProperty == null)
        {
            return false;
        }

        var method = methodProperty.GetValue(binaryExpression) as MethodInfo;
        if (method == null)
        {
            return false;
        }

        return method.DeclaringType == typeof(string) && method.Name == nameof(string.Concat);
    }
}
