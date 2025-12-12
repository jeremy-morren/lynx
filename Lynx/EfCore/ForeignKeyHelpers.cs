using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lynx.EfCore;

/// <summary>
/// Sets all foreign key constraints to be deferrable.
/// </summary>
public static class ForeignKeyHelpers
{
    /// <summary>
    /// Sets the cascade mode for all foreign keys in the model to the specified value
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="deleteBehavior"></param>
    public static void SetForeignKeyCascadeMode(ModelBuilder builder, DeleteBehavior deleteBehavior)
    {
        foreach (var t in builder.Model.GetEntityTypes())
            foreach (var n in t.GetNavigations())
                n.ForeignKey.DeleteBehavior = deleteBehavior;
    }

    /// <summary>
    /// Sets all foreign key constraints in the database to be deferrable.
    /// </summary>
    public static void ExecuteSetConstraintsDeferrable(DbContext context)
    {
        var sql = context.Model.GenerateSetConstraintsDeferrableScript();
        context.Database.ExecuteSqlRaw(sql);
    }

    /// <summary>
    /// Sets all foreign key constraints in the database to be deferrable.
    /// </summary>
    public static Task ExecuteSetConstraintsDeferrableAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        var sql = context.Model.GenerateSetConstraintsDeferrableScript();
        return context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    /// <summary>
    /// Generates a script to set all foreign key constraints to be deferrable.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public static string GenerateSetConstraintsDeferrableScript(this IModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Set all foreign key constraints to be deferrable");
        sb.AppendLine("BEGIN TRANSACTION;");
        foreach (var cmd in GetConstraintsDeferrableCommands(model))
            sb.AppendLine(cmd);
        sb.AppendLine("COMMIT;");
        return sb.ToString();
    }

    private static IEnumerable<string> GetConstraintsDeferrableCommands(IModel model) =>
        from type in model.GetEntityTypes()
        from key in type.GetForeignKeys()
        from constraint in key.GetMappedConstraints()
        let tableName = constraint.Table.GetPostgresTableName()
        select $"ALTER TABLE {tableName} ALTER CONSTRAINT \"{constraint.Name}\" DEFERRABLE INITIALLY DEFERRED;";


    private static string GetPostgresTableName(this ITable table)
    {
        var name = $"\"{table.Name}\"";
        return table.Schema == null ? name : $"\"{table.Schema}\".{name}";
    }
}