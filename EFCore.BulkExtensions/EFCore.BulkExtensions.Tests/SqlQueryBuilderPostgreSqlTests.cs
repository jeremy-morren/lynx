using EFCore.BulkExtensions.SqlAdapters.PostgreSql;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EFCore.BulkExtensions.Tests;

public class SqlQueryBuilderPostgreSqlTests
{
    [Fact]
    public void MergeTableInsertOrUpdateWithoutOnConflictUpdateWhereSqlTest()
    {
        TableInfo tableInfo = GetTestTableInfo();
        tableInfo.IdentityColumnName = "ItemId";
        string actual = PostgreSqlQueryBuilder.MergeTable<Item>(tableInfo, OperationType.InsertOrUpdate);

        string expected = @"INSERT INTO ""dbo"".""Item"" (""ItemId"", ""Name"") " +
                          @"(SELECT ""ItemId"", ""Name"" FROM ""dbo"".""ItemTemp1234"") " +
                          @"ON CONFLICT (""ItemId"") DO UPDATE SET ""Name"" = EXCLUDED.""Name"";";
        Assert.Equal(expected, actual);
    }
    
    [Fact]
    public void MergeTableInsertOrUpdateWithOnConflictUpdateWhereSqlTest()
    {
        TableInfo tableInfo = GetTestTableInfo((existing, inserted) => $"{inserted}.ItemTimestamp > {existing}.ItemTimestamp");
        tableInfo.IdentityColumnName = "ItemId";
        string actual = PostgreSqlQueryBuilder.MergeTable<Item>(tableInfo, OperationType.InsertOrUpdate);

        string expected = @"INSERT INTO ""dbo"".""Item"" (""ItemId"", ""Name"") " +
                          @"(SELECT ""ItemId"", ""Name"" FROM ""dbo"".""ItemTemp1234"") " +
                          @"ON CONFLICT (""ItemId"") DO UPDATE SET ""Name"" = EXCLUDED.""Name"" " +
                          @"WHERE EXCLUDED.ItemTimestamp > ""dbo"".""Item"".ItemTimestamp;";
        Assert.Equal(expected, actual);
    }
    
    [Fact]
    public void MergeTableInsertOrUpdateWithInsertOnlyTest()
    {
        TableInfo tableInfo = GetTestTableInfo();
        tableInfo.IdentityColumnName = "ItemId";
        tableInfo.PropertyColumnNamesUpdateDict = new();
        string actual = PostgreSqlQueryBuilder.MergeTable<Item>(tableInfo, OperationType.InsertOrUpdate);

        string expected = @"INSERT INTO ""dbo"".""Item"" (""ItemId"", ""Name"") " +
                          @"(SELECT ""ItemId"", ""Name"" FROM ""dbo"".""ItemTemp1234"") LIMIT 1 " +
                          @"ON CONFLICT (""ItemId"") DO NOTHING;";

        if (!actual.Contains("LIMIT 1"))
            expected = expected.Replace(" LIMIT 1", "");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MergeTableUpdateOnlyTest()
    {
        TableInfo tableInfo = GetTestTableInfo();
        tableInfo.IdentityColumnName = "ItemId";
        string actual = PostgreSqlQueryBuilder.MergeTable<Item>(tableInfo, OperationType.Update);

        string expected = @"UPDATE ""dbo"".""Item"" SET ""Name"" = ""dbo"".""ItemTemp1234"".""Name"" " +
                          @"FROM ""dbo"".""ItemTemp1234"" " +
                          @"WHERE ""dbo"".""Item"".""ItemId"" = ""dbo"".""ItemTemp1234"".""ItemId"";";
        
        Assert.Equal(expected, actual);
    }
    
    private TableInfo GetTestTableInfo(Func<string, string, string>? onConflictUpdateWhereSql = null)
    {
        var tableInfo = new TableInfo()
        {
            Schema = "dbo",
            TempSchema = "dbo",
            TableName = nameof(Item),
            TempTableName = nameof(Item) + "Temp1234",
            TempTableSufix = "Temp1234",
            PrimaryKeysPropertyColumnNameDict = new Dictionary<string, string> { { nameof(Item.ItemId), nameof(Item.ItemId) } },
            BulkConfig = new BulkConfig()
            {
                OnConflictUpdateWhereSql = onConflictUpdateWhereSql
            }
        };
        const string nameText = nameof(Item.Name);

        tableInfo.PropertyColumnNamesDict.Add(tableInfo.PrimaryKeysPropertyColumnNameDict.Keys.First(), tableInfo.PrimaryKeysPropertyColumnNameDict.Values.First());
        tableInfo.PropertyColumnNamesDict.Add(nameText, nameText);
        //compare on all columns (default)
        tableInfo.PropertyColumnNamesCompareDict = tableInfo.PropertyColumnNamesDict;
        //update all columns (default)
        tableInfo.PropertyColumnNamesUpdateDict = tableInfo.PropertyColumnNamesDict;
        return tableInfo;
    }

    [Fact]
    public void RestructureForBatchWithoutJoinToOtherTablesTest()
    {
        string sql =
            @"UPDATE i SET ""Description"" = @Description, ""Price"" = @Price FROM ""Item"" AS i WHERE i.""ItemId"" <= 1";

        string expected =
            @"UPDATE ""Item"" AS i SET ""Description"" = @Description, ""Price"" = @Price WHERE i.""ItemId"" <= 1";

        var batchUpdate = new PostgreSqlQueryBuilder().RestructureForBatch(sql);

        Assert.Equal(expected, batchUpdate);
    }

    [Fact]
    public void RestructureForBatchWithJoinToOtherTablesTest()
    {
        string sql =
            @"UPDATE i SET ""Description"" = @Description, ""Price"" = @Price FROM ""Item"" AS i INNER JOIN ""User"" AS u ON i.""UserId"" = u.""Id"" WHERE i.""ItemId"" <= 1";

        string expected =
            @"UPDATE ""Item"" AS i SET ""Description"" = @Description, ""Price"" = @Price FROM ""User"" AS u WHERE i.""ItemId"" <= 1 AND i.""UserId"" = u.""Id"" ";

        var batchUpdate = new PostgreSqlQueryBuilder().RestructureForBatch(sql);

        Assert.Equal(expected, batchUpdate);
    }
}
