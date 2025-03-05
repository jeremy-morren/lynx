using Lynx.Provider.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.Provider.Common;

internal static class EntityInfoFactory
{
    public static RootEntityInfo Create(Type type, IModel model)
    {
        var entityType = model.FindEntityType(type)
            ?? throw new InvalidOperationException($"Entity type {type} not found in model.");

        var info = CreateEntityInternal(entityType, null);
        return new RootEntityInfo
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
    }

    private static EntityInfo CreateEntityInternal(IEntityType entityType, ColumnName? parentColumn)
    {
        var (scalar, complex) = GetProperties(entityType, parentColumn);

        var owned = new List<OwnedEntityInfo>();
        foreach (var navigation in entityType.GetNavigations())
        {
            if (!navigation.ForeignKey.IsOwnership)
                continue; // Skip non-owned navigations
            if (navigation.PropertyInfo == null)
                continue; // Skip shadow properties
            var ownedType = navigation.ForeignKey.DeclaringEntityType;
            var table = ownedType.GetTableMappings().Single();

            if (table.IsSplitEntityTypePrincipal != null)
                throw new NotImplementedException("Owned types with table splitting is not supported");

            var colName = GetColumnName(navigation, parentColumn);
            var result = CreateEntityInternal(ownedType, colName);
            owned.Add(new OwnedEntityInfo
            {
                Parent = entityType,
                EntityType = ownedType,
                PropertyInfo = navigation.PropertyInfo,
                ColumnName = colName,
                Type = result.Type,
                ScalarProps = result.ScalarProps,
                ComplexProps = result.ComplexProps,
                Owned = result.Owned
            });
        }

        return new EntityInfo
        {
            Type = entityType,
            ScalarProps = scalar,
            ComplexProps = complex,
            Owned = owned
        };
    }

    private static (List<EntityPropertyInfo> Scalar, List<ComplexEntityPropertyInfo> Complex) GetProperties(
        ITypeBase parent, ColumnName? parentColumn)
    {
        var scalarProps =
            from p in parent.GetProperties()
            let info = p.PropertyInfo
            where info != null // Exclude shadow properties
            select new EntityPropertyInfo
            {
                Property = p,
                Parent = parent,
                PropertyInfo = info,
                ColumnName = GetColumnName(p, parentColumn),
            };

        var complexProps =
            from p in parent.GetComplexProperties()
            let info = p.PropertyInfo
            where info != null // Exclude shadow properties
            let name = GetColumnName(p, parentColumn)
            let complex = GetProperties(p.ComplexType, name)
            select new ComplexEntityPropertyInfo
            {
                Property = p,
                Parent = parent,
                PropertyInfo = info,
                ColumnName = name,
                ScalarProps = complex.Scalar,
                ComplexProps = complex.Complex
            };

        return (scalarProps.ToList(), complexProps.ToList());
    }

    private static IEnumerable<EntityPropertyInfo> GetKeys(IEntityType entityType)
    {
        var key = entityType.FindPrimaryKey()
                  ?? throw new InvalidOperationException($"Primary key not found for {entityType.ClrType}");

        return
            from p in key.Properties
            select new EntityPropertyInfo
            {
                Property = p,
                Parent = entityType,
                PropertyInfo = null,
                ColumnName = GetColumnName(p, null)
            };
    }

    private static ColumnName GetColumnName(IPropertyBase property, ColumnName? parentColumn)
    {
        var annotation = property.FindAnnotation("Relational:ColumnName");
        var name = annotation?.Value?.ToString() ?? property.Name;
        return parentColumn?.Add(name) ?? ColumnName.NewRoot(name);
    }
}