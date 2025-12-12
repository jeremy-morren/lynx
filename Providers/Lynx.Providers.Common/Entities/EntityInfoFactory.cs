using System.Diagnostics;
using Lynx.Providers.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable LoopCanBeConvertedToQuery

namespace Lynx.Providers.Common.Entities;

internal static class EntityInfoFactory
{
    public static RootEntityInfo<T> CreateRoot<T>(IModel model) where T : class
    {
        var type = typeof(T);
        var entityType = model.FindEntityType(type)
            ?? throw new InvalidOperationException($"Entity type '{type}' not found in model.");

        var info = CreateEntityInternal(entityType, null, null);
        var root = new RootEntityInfo<T>
        {
            Type = info.Type,
            ScalarProps = info.ScalarProps,
            ComplexProps = info.ComplexProps,
            Owned = info.Owned,
            Keys = GetKeys(entityType).ToList(),

            TableName = entityType.GetTableName() ??
                        throw new InvalidOperationException($"Table name not found for {type}"),
            Schema = entityType.GetSchema()
        };

        SetColumnIndex(root, 0);
        return root;
        
    }

    private static EntityInfo CreateEntityInternal(IEntityType entityType, PropertyChain? parentName, PropertyChain? parentColumn)
    {
        var (scalar, complex) = GetProperties(entityType, parentName, parentColumn);

        var owned = new List<OwnedEntityInfo>();
        foreach (var navigation in entityType.GetNavigations())
        {
            if (!navigation.ForeignKey.IsOwnership)
                continue; // Skip non-owned navigations
            if (navigation.PropertyInfo == null)
                continue; // Skip shadow properties
            owned.Add(CreateOwned(entityType, navigation, parentName, parentColumn));
        }

        return new EntityInfo
        {
            Type = entityType,
            ScalarProps = scalar,
            ComplexProps = complex,
            Owned = owned
        };
    }

    private static (List<ScalarEntityPropertyInfo> Scalar, List<ComplexEntityPropertyInfo> Complex) GetProperties(
        ITypeBase parent, PropertyChain? parentName, PropertyChain? parentColumn)
    {
        var scalarProps =
            from p in parent.GetProperties()
            where !p.IsPrimaryKey() // Exclude keys
            where p.ValueGenerated == ValueGenerated.Never // Exclude computed columns
            let info = p.PropertyInfo
            where info != null // Exclude shadow properties
            select new ScalarEntityPropertyInfo
            {
                Name = parentName?.Add(p.Name) ?? PropertyChain.NewRoot(p.Name),
                Property = p,
                Parent = parent,
                PropertyInfo = info,
                ColumnName = GetColumnName(p, parentColumn),
            };

        var complexProps =
            from p in parent.GetComplexProperties()
            let info = p.PropertyInfo
            where info != null // Exclude shadow properties
            let name = parentName?.Add(p.Name) ?? PropertyChain.NewRoot(p.Name)
            let columnName = GetColumnName(p, parentColumn)
            let properties = GetProperties(p.ComplexType, name, columnName)
            select new ComplexEntityPropertyInfo
            {
                Name = name,
                Property = p,
                Parent = parent,
                PropertyInfo = info,
                ColumnName = columnName,
                ScalarProps = properties.Scalar,
                ComplexProps = properties.Complex
            };

        return (scalarProps.ToList(), complexProps.ToList());
    }

    private static OwnedEntityInfo CreateOwned(
        IEntityType parent,
        INavigation navigation,
        PropertyChain? parentName,
        PropertyChain? parentColumn)
    {
        var ownedType = navigation.ForeignKey.DeclaringEntityType;
        var table = ownedType.GetTableMappings().Single();

        if (table.IsSplitEntityTypePrincipal != null)
            throw new NotImplementedException("Owned types with table splitting is not supported");

        var name = parentName?.Add(navigation.Name) ?? PropertyChain.NewRoot(navigation.Name);
        var columnName = GetColumnName(ownedType, navigation, parentColumn);
        var result = CreateEntityInternal(ownedType, name, columnName);

        Debug.Assert(navigation.PropertyInfo != null);
        var owned = new OwnedEntityInfo
        {
            Name = name,
            Parent = parent,
            EntityType = ownedType,
            Navigation = navigation,
            PropertyInfo = navigation.PropertyInfo,
            ColumnName = columnName,
            Type = result.Type,
            ScalarProps = result.ScalarProps,
            ComplexProps = result.ComplexProps,
            Owned = result.Owned
        };
        if (ownedType.IsMappedToJson())
            return JsonOwnedEntityInfo.New(owned);
        return owned;
    }

    private static IEnumerable<ScalarEntityPropertyInfo> GetKeys(IEntityType entityType)
    {
        var key = entityType.FindPrimaryKey()
                  ?? throw new InvalidOperationException($"Primary key not found for {entityType.ClrType}");

        return
            from p in key.Properties
            select new ScalarEntityPropertyInfo
            {
                Name = PropertyChain.NewRoot(p.Name),
                Property = p,
                Parent = entityType,
                PropertyInfo = p.PropertyInfo 
                               ?? throw new InvalidOperationException("Shadow key properties not supported"),
                ColumnName = GetColumnName(p, null)
            };
    }

    private static int SetColumnIndex(IStructureEntity entity, int startIndex)
    {
        //Key columns are first
        if (entity is RootEntityInfo root)
            foreach (var p in root.Keys) 
                p.ColumnIndex = startIndex++;
        
        foreach (var s in entity.ScalarProps) 
            s.ColumnIndex = startIndex++;

        foreach (var c in entity.ComplexProps) 
            startIndex = SetColumnIndex(c, startIndex);

        foreach (var o in (entity as EntityInfo)?.Owned ?? [])
        {
            if (o is JsonOwnedEntityInfo json)
                //JSON owned type, only one column in the table
                json.ColumnIndex = startIndex++;
            else
                startIndex = SetColumnIndex(o, startIndex);
        }

        return startIndex;
    }

    /// <summary>
    /// Gets the column name for the given property.
    /// </summary>
    private static PropertyChain GetColumnName(IPropertyBase property, PropertyChain? parentColumn)
    {
        var annotation = property.FindAnnotation(RelationalAnnotationNames.ColumnName);
        var name = annotation?.Value?.ToString() ?? property.Name;
        return parentColumn?.Add(name) ?? PropertyChain.NewRoot(name);
    }

    /// <summary>
    /// Gets the column name for the given owned navigation.
    /// </summary>
    private static PropertyChain GetColumnName(IEntityType owned, INavigation navigation, PropertyChain? parentColumn)
    {
        var annotation = owned.FindAnnotation(RelationalAnnotationNames.ContainerColumnName)
                            ?? navigation.FindAnnotation(RelationalAnnotationNames.ColumnName);
        var name = annotation?.Value?.ToString() ?? navigation.Name;
        return parentColumn?.Add(name) ?? PropertyChain.NewRoot(name);
    }
}