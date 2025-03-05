using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Lynx.Provider.Common.Models;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lynx.Provider.Common;

/// <summary>
/// Builds expressions for adding all parameters for an entity to a command.
/// </summary>
internal static class AddParameterDelegateBuilder<TCommand> where TCommand : DbCommand
{
    public static Action<TCommand> Build(IStructureEntity entity)
    {
        var command = Expression.Parameter(typeof(TCommand), "command");
        var parameter = Expression.Variable(CreateParameterMethod.ReturnType, "parameter");

        var add = AddParameters(entity, command, parameter).ToList();
        var block = Expression.Block([parameter], add);

        return Expression.Lambda<Action<TCommand>>(block, command).Compile();
    }

    private static IEnumerable<Expression> AddParameters(
        IStructureEntity entity,
        ParameterExpression command,
        ParameterExpression parameter)
    {
        var keys = (entity as RootEntityInfo)?.Keys ?? [];
        
        var addScalars =
            from scalar in keys.Concat(entity.ScalarProps)
            let block = BuildAddParameter(scalar, command, parameter)
            from e in block
            select e;

        var addComplex =
            from complex in entity.ComplexProps
            let block = AddParameters(complex, command, parameter)
            from e in block
            select e;
        
        return addScalars.Concat(addComplex);
    }

    /// <summary>
    /// Returns an expression that checks if the given expression is not null null, or null if the expression is not nullable.
    /// </summary>
    private static List<Expression> BuildAddParameter(
        ScalarEntityPropertyInfo property,
        ParameterExpression command,
        ParameterExpression parameter)
    {
        var mapping = property.TypeMapping;
        var parameterName = $"@{property.ColumnName.SqlColumnName}";
        var result = new List<Expression>()
        {

            //Create parameter and assign to the command
            Expression.Assign(
                parameter,
                Expression.Call(command, CreateParameterMethod)),

            //Set parameter name
            Expression.Assign(
                Expression.Property(parameter, ReflectionItems.ParameterNameProperty),
                Expression.Constant(parameterName))
        };
        if (mapping.DbType != null)
        {
            //Set DB type
            result.Add(
                Expression.Assign(
                    Expression.Property(parameter, ReflectionItems.DbParameterDbTypeProperty),
                    Expression.Constant(mapping.DbType.Value)));
        }

        if (mapping.Size != null)
        {
            //Set size
            result.Add(
                Expression.Assign(
                    Expression.Property(parameter, ReflectionItems.DbParameterSizeProperty),
                    Expression.Constant(mapping.Size.Value)));
        }
        if (mapping.Scale != null)
        {
            //Set scale
            result.Add(
                Expression.Assign(
                    Expression.Property(parameter, ReflectionItems.DbParameterScaleProperty),
                    Expression.Constant(mapping.Scale.Value)));
        }
        if (mapping.Precision != null)
        {
            //Set precision
            result.Add(
                Expression.Assign(
                    Expression.Property(parameter, ReflectionItems.DbParameterPrecisionProperty),
                    Expression.Constant(mapping.Precision.Value)));
        }
        
        //Add the parameter to the command
        result.Add(
            Expression.Call(
                Expression.Property(command, ReflectionItems.CommandParametersProperty),
                ReflectionItems.AddParameterMethod,
                parameter));

        return result;
    }
    
    /// <summary>
    /// <see cref="DbCommand.CreateParameter"/>
    /// </summary>
    public static readonly MethodInfo CreateParameterMethod =
        typeof(TCommand).GetMethod(nameof(DbCommand.CreateParameter), ReflectionItems.InstanceFlags)!;
}