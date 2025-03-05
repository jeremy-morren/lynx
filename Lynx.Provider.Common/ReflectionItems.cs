using System.Data.Common;
using System.Reflection;

namespace Lynx.Provider.Common;

internal static class ReflectionItems
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// <see cref="DbCommand.CreateParameter"/>
    /// </summary>
    public static readonly MethodInfo CreateParameterMethod =
        typeof(DbCommand).GetMethod(nameof(DbCommand.CreateParameter), InstanceFlags)!;

    /// <summary>
    /// <see cref="DbParameterCollection.Add"/>
    /// </summary>
    public static readonly MethodInfo AddParameterMethod =
        typeof(DbParameterCollection).GetMethod(nameof(DbParameterCollection.Add), InstanceFlags)!;

    /// <summary>
    /// <see cref="DbParameter.ParameterName"/>
    /// </summary>
    public static readonly PropertyInfo ParameterNameProperty =
        typeof(DbParameter).GetProperty(nameof(DbParameter.ParameterName), InstanceFlags)!;

    /// <summary>
    /// <see cref="DbParameter.Value"/>
    /// </summary>
    public static readonly PropertyInfo ParameterValueProperty =
        typeof(DbParameter).GetProperty(nameof(DbParameter.Value), InstanceFlags)!;

    /// <summary>
    /// <see cref="DbCommand.Parameters"/>
    /// </summary>
    public static readonly PropertyInfo CommandParametersProperty =
        typeof(DbCommand).GetProperty(nameof(DbCommand.Parameters), InstanceFlags)!;
}