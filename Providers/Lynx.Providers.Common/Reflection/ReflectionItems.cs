using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Lynx.Providers.Common.Reflection;

internal static class ReflectionItems
{
    public const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    public const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// <see cref="DbParameterCollection.Add"/>
    /// </summary>
    public static readonly MethodInfo AddParameterMethod =
        typeof(DbParameterCollection).GetMethod(nameof(DbParameterCollection.Add), InstanceFlags)!;
    
    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    public static readonly MethodInfo ParameterGetItemMethod =
        typeof(DbParameterCollection).GetMethod("get_Item", InstanceFlags, [typeof(int)])!;

    /// <summary>
    /// <see cref="DbParameter.ParameterName"/>
    /// </summary>
    public static readonly PropertyInfo ParameterNameProperty =
        typeof(DbParameter).GetProperty(nameof(DbParameter.ParameterName), InstanceFlags)!;

    /// <summary>
    /// <see cref="DbParameter.DbType"/>
    /// </summary>
    public static readonly PropertyInfo DbParameterDbTypeProperty =
        typeof(DbParameter).GetProperty(nameof(DbParameter.DbType), InstanceFlags)!;

    /// <summary>
    /// <see cref="DbParameter.Size"/>
    /// </summary>
    public static readonly PropertyInfo DbParameterSizeProperty =
        typeof(DbParameter).GetProperty(nameof(DbParameter.Size), InstanceFlags)!;

    /// <summary>
    /// <see cref="DbParameter.Scale"/>
    /// </summary>
    public static readonly PropertyInfo DbParameterScaleProperty =
        typeof(DbParameter).GetProperty(nameof(DbParameter.Scale), InstanceFlags)!;

    /// <summary>
    /// <see cref="DbParameter.Precision"/>
    /// </summary>
    public static readonly PropertyInfo DbParameterPrecisionProperty =
        typeof(DbParameter).GetProperty(nameof(DbParameter.Precision), InstanceFlags)!;

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

    /// <summary>
    /// <see cref="DBNull.Value"/> typed as object
    /// </summary>
    public static readonly Expression DBNullValue =
        Expression.Convert(
            Expression.Field(null, typeof(DBNull).GetField(nameof(DBNull.Value), StaticFlags)!),
            typeof(object));

    /// <summary>
    /// <see cref="IDisposable.Dispose"/>
    /// </summary>
    public static readonly MethodInfo DisposeMethod =
        typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose), InstanceFlags)!;

    /// <summary>
    /// <see cref="Utf8JsonWriter.Flush"/>
    /// </summary>
    public static readonly MethodInfo Utf8JsonWriterFlushMethod =
        typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.Flush), InstanceFlags)!;

    /// <summary>
    /// <see cref="Utf8JsonWriter.WritePropertyName(JsonEncodedText)"/>
    /// </summary>
    public static readonly MethodInfo Utf8JsonWriterWritePropertyNameMethod =
        typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WritePropertyName), InstanceFlags, [typeof(JsonEncodedText)])!;

    /// <summary>
    /// <see cref="Utf8JsonWriter.WriteNullValue()"/>
    /// </summary>
    public static readonly MethodInfo Utf8JsonWriterWriteNullValueMethod =
        typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteNullValue), InstanceFlags)!;

    /// <summary>
    /// <see cref="Utf8JsonWriter.WriteStartObject()"/>
    /// </summary>
    public static readonly MethodInfo Utf8JsonWriterWriteStartObjectMethod =
        typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteStartObject), InstanceFlags, [])!;

    /// <summary>
    /// <see cref="Utf8JsonWriter.WriteEndObject()"/>
    /// </summary>
    public static readonly MethodInfo Utf8JsonWriterWriteEndObjectMethod =
        typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteEndObject), InstanceFlags, [])!;

    /// <summary>
    /// <see cref="Utf8JsonWriter.WriteStartArray()"/>
    /// </summary>
    public static readonly MethodInfo Utf8JsonWriterWriteStartArrayMethod =
        typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteStartArray), InstanceFlags, [])!;

    /// <summary>
    /// <see cref="Utf8JsonWriter.WriteEndArray()"/>
    /// </summary>
    public static readonly MethodInfo Utf8JsonWriterWriteEndArrayMethod =
        typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteEndArray), InstanceFlags, [])!;
    
    
}