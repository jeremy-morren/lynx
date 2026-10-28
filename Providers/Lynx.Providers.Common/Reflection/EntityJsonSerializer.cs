using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Lynx.Providers.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace Lynx.Providers.Common.Reflection;

[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
internal static class EntityJsonSerializer
{
    private static string StreamToJson(MemoryStream stream) =>
        Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);

    /// <summary>
    /// Serializes a structure (owned entity or complex type) mapped to a JSON column.
    /// </summary>
    /// <param name="entity">Owned entity (<see cref="JsonOwnedEntityInfo"/>) or complex property (<see cref="JsonComplexEntityPropertyInfo"/>)</param>
    /// <param name="value">Property value</param>
    public static Expression Serialize(IStructurePropertyInfo entity, Expression value)
    {
        var ms = Expression.Variable(typeof(MemoryStream), "ms");

        return ExpressionHelpers.UsingExpression(
            ms,
            Expression.New(ms.Type),
            () =>
            {
                var writer = Expression.Variable(typeof(Utf8JsonWriter), "writer");
                return ExpressionHelpers.UsingExpression(
                    writer,
                    Expression.New(Utf8JsonWriterConstructor, ms, Expression.Constant(JsonWriterOptions)),
                    () => SerializeJson(entity, value, ms, writer));
            });
    }

    private static Expression SerializeJson(
        IStructurePropertyInfo entity,
        Expression value,
        ParameterExpression ms,
        ParameterExpression writer)
    {
        return Expression.Block(
            WriteStructure(entity, value, writer),
            Expression.Call(writer, ReflectionItems.Utf8JsonWriterFlushMethod), // writer.Flush()
            Expression.Call(StreamToJsonMethod, ms) // return ToJson(ms)
        );
    }

    /// <summary>
    /// Whether the structure is a collection (owned collection or complex collection)
    /// </summary>
    private static bool IsCollection(IStructurePropertyInfo entity) => entity switch
    {
        OwnedEntityInfo owned => owned.IsCollection,
        ComplexEntityPropertyInfo complex => complex.Property.IsCollection,
        _ => throw new NotSupportedException($"Unsupported structure type {entity.GetType()}")
    };

    /// <summary>
    /// CLR type of the structure (the element type for collections)
    /// </summary>
    private static Type GetClrType(IStructurePropertyInfo entity) => entity switch
    {
        OwnedEntityInfo owned => owned.Type.ClrType,
        ComplexEntityPropertyInfo complex => complex.Property.ComplexType.ClrType,
        _ => throw new NotSupportedException($"Unsupported structure type {entity.GetType()}")
    };

    /// <summary>
    /// JSON property name of the structure, when nested inside another JSON document
    /// </summary>
    private static string GetJsonPropertyName(IStructurePropertyInfo entity)
    {
        var name = entity switch
        {
            OwnedEntityInfo owned => owned.EntityType.GetJsonPropertyName(),
            ComplexEntityPropertyInfo complex => complex.Property.ComplexType.GetJsonPropertyName(),
            _ => throw new NotSupportedException($"Unsupported structure type {entity.GetType()}")
        };
        return name ?? entity.PropertyInfo.Name;
    }

    /// <summary>
    /// Writes a structure property value (object, array or null). Does not write the property name.
    /// </summary>
    private static Expression WriteStructure(IStructurePropertyInfo entity, Expression value, ParameterExpression writer)
    {
        var writeBlock = new List<Expression>();
        if (IsCollection(entity))
        {
            // Collection property. write array
            writeBlock.Add(Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteStartArrayMethod));
            var item = Expression.Variable(GetClrType(entity), "item");
            writeBlock.Add(ExpressionHelpers.ForLoop(
                value,
                item,
                WriteObject(entity, item, writer, false)));
            writeBlock.Add(Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteEndArrayMethod));
        }
        else
        {
            // Structure property, write object. Ignore null (because we have our own check below)
            writeBlock.Add(WriteObject(entity, ExpressionHelpers.GetNullableValue(value), writer, true));
        }

        var write = Expression.Block(writeBlock);
        var ifNotNull = ExpressionHelpers.GetIfNotNull(value);
        if (ifNotNull == null)
            return write;

        return Expression.IfThenElse(
            ifNotNull,
            write,
            Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteNullValueMethod));
    }

    /// <summary>
    /// Writes a structure object value to the writer. Does not write the property name.
    /// </summary>
    private static Expression WriteObject(
        IStructurePropertyInfo entity,
        Expression value,
        ParameterExpression writer,
        bool ignoreNull)
    {
        var result = new List<Expression>
        {
            Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteStartObjectMethod)
        };

        foreach (var scalar in entity.ScalarProps)
            result.AddRange(WriteScalar(scalar, value, writer));
        foreach (var complex in entity.ComplexProps)
            result.AddRange(WriteStructureProperty(complex, value, writer));
        foreach (var owned in (entity as OwnedEntityInfo)?.Owned ?? [])
            result.AddRange(WriteStructureProperty(owned, value, writer));

        result.Add(Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteEndObjectMethod));

        var write = Expression.Block(result);
        if (ignoreNull)
            return write;

        var ifNotNull = ExpressionHelpers.GetIfNotNull(value);
        if (ifNotNull == null)
            return write;

        // Null check
        return Expression.IfThenElse(ifNotNull,
            write,
            Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteNullValueMethod));
    }

    /// <summary>
    /// Writes a nested structure (owned entity or complex type) to the writer with the property name.
    /// </summary>
    private static Expression[] WriteStructureProperty(
        IStructurePropertyInfo entity,
        Expression parent,
        ParameterExpression writer)
    {
        var value = Expression.Property(parent, entity.PropertyInfo);
        return
        [
            WritePropertyName(GetJsonPropertyName(entity), writer),
            WriteStructure(entity, value, writer)
        ];
    }

    private static Expression[] WriteScalar(
        ScalarEntityPropertyInfo scalar,
        Expression parent,
        ParameterExpression writer)
    {
        var jsonPropertyName = scalar.Property.GetJsonPropertyName();
        Debug.Assert(jsonPropertyName != null);

        var jsonConverter = scalar.Property.GetJsonValueReaderWriter() ?? scalar.Property.GetTypeMapping().JsonValueReaderWriter;
        Debug.Assert(jsonConverter != null, "jsonConverter != null");

        var writePropertyName = WritePropertyName(jsonPropertyName, writer);
        var value = Expression.Property(parent, scalar.PropertyInfo);
        Expression writeValue = Expression.Call(
            Expression.Constant(jsonConverter),
            ToJsonMethod,
            writer,
            Expression.Convert(value, typeof(object)));

        var ifNotNull = ExpressionHelpers.GetIfNotNull(value);
        if (ifNotNull != null)
        {
            writeValue = Expression.IfThenElse(ifNotNull,
                writeValue,
                Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteNullValueMethod));
        }
        return [writePropertyName, writeValue];
    }

    private static Expression WritePropertyName(string propertyName, ParameterExpression writer)
    {
        var propertyNameEncoded = JsonEncodedText.Encode(propertyName);
        return Expression.Call(writer,
            ReflectionItems.Utf8JsonWriterWritePropertyNameMethod,
            Expression.Constant(propertyNameEncoded));
    }


    private static readonly MethodInfo StreamToJsonMethod =
        typeof(EntityJsonSerializer).GetMethod(nameof(StreamToJson), ReflectionItems.StaticFlags)!;

    private static readonly ConstructorInfo Utf8JsonWriterConstructor =
        typeof(Utf8JsonWriter).GetConstructor(ReflectionItems.InstanceFlags, [typeof(Stream), typeof(JsonWriterOptions)])!;

    private static readonly MethodInfo ToJsonMethod =
        typeof(JsonValueReaderWriter).GetMethod(nameof(JsonValueReaderWriter.ToJson), ReflectionItems.InstanceFlags)!;

    // [NullableContext(2)]
    // public static string Serialize<T>([Nullable(1)] OwnedEntityInfo entity, T value)
    // {
    //     if ((object) value == null)
    //         return (string) null;
    //     MemoryStream ms = new MemoryStream();
    //     try
    //     {
    //         Utf8JsonWriter writer = new Utf8JsonWriter((Stream) ms, EntityJsonSerializer.JsonWriterOptions);
    //         try
    //         {
    //             EntityJsonSerializer.WriteOwnedValue(writer, entity, (object) value);
    //             writer.Flush();
    //             return Encoding.UTF8.GetString(Span<byte>.op_Implicit(ms.GetBuffer().AsSpan<byte>(0, (int) ms.Length)));
    //         }
    //         finally
    //         {
    //             if (writer != null)
    //                 writer.Dispose();
    //         }
    //     }
    //     finally
    //     {
    //         if (ms != null)
    //             ms.Dispose();
    //     }
    // }

    private static readonly JsonWriterOptions JsonWriterOptions =
        new ()
        {
            Indented = false,
            SkipValidation = true
        };
}