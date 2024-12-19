﻿using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;

namespace EFCore.BulkExtensions.SqlAdapters;


/// <summary>
/// Contains the table alias and SQL query
/// </summary>
public class ExtractedTableAlias
{
#pragma warning disable CS1591 // No XML comments required
    public string TableAlias { get; set; } = null!;
    public string TableAliasSuffixAs { get; set; } = null!;
    public string Sql { get; set; } = null!;
#pragma warning restore CS1591 // No XML comments required
}

/// <summary>
/// Contains a list of methods for query operations
/// </summary>
public interface IQueryBuilderSpecialization
{
    /// <summary>
    /// Reloads the SQL paramaters
    /// </summary>
    /// <param name="context"></param>
    /// <param name="sqlParameters"></param>
    List<DbParameter> ReloadSqlParameters(DbContext context, List<DbParameter> sqlParameters);

    /// <summary>
    /// Returns the binary expression add operation
    /// </summary>
    /// <param name="binaryExpression"></param>
    string GetBinaryExpressionAddOperation(BinaryExpression binaryExpression);

    /// <summary>
    /// Returns a tuple containing the batch sql reformat table alias
    /// </summary>
    /// <param name="sqlQuery"></param>
    /// <param name="databaseType"></param>
    (string, string) GetBatchSqlReformatTableAliasAndTopStatement(string sqlQuery, SqlType databaseType);

    /// <summary>
    /// Returns the SQL extract table alias data
    /// </summary>
    /// <param name="fullQuery"></param>
    /// <param name="tableAlias"></param>
    /// <param name="tableAliasSuffixAs"></param>
    ExtractedTableAlias GetBatchSqlExtractTableAliasFromQuery(string fullQuery, string tableAlias, string tableAliasSuffixAs);
}
