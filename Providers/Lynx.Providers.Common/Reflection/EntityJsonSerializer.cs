using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Lynx.Providers.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace Lynx.Providers.Common.Reflection;

[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
internal static class EntityJsonSerializer
{
    private static string StreamToJson(MemoryStream stream) =>
        Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);

    public static Expression Serialize(OwnedEntityInfo entity, Expression value)
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
        OwnedEntityInfo entity,
        Expression value,
        ParameterExpression ms,
        ParameterExpression writer)
    {
        return Expression.Block(
            WriteOwned(entity, value, writer),
            Expression.Call(writer, ReflectionItems.Utf8JsonWriterFlushMethod), // writer.Flush()
            Expression.Call(StreamToJsonMethod, ms) // return ToJson(ms)
        );
    }

    /// <summary>
    /// Writes an owned entity property value. Does not write the property name.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="value"></param>
    /// <param name="writer"></param>
    /// <returns></returns>
    private static Expression WriteOwned(OwnedEntityInfo entity, Expression value, ParameterExpression writer)
    {
        var writeBlock = new List<Expression>();
        if (entity.IsCollection)
        {
            // Collection property. write array
            writeBlock.Add(Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteStartArrayMethod));
            var item = Expression.Variable(entity.Type.ClrType, "item");
            writeBlock.Add(ExpressionHelpers.ForLoop(
                value,
                item,
                WriteOwnedObject(entity, item, writer, false)));
            writeBlock.Add(Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteEndArrayMethod));
        }
        else
        {
            // Structure property, write object. Ignore null (because we have our own check below)
            writeBlock.Add(WriteOwnedObject(entity, value, writer, true));
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
    /// Writes an owned entity object value to the writer. Does not write the property name.
    /// </summary>
    private static Expression WriteOwnedObject(
        OwnedEntityInfo entity,
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
            result.AddRange(WriteComplex(complex, value, writer));
        foreach (var owned in entity.Owned)
            result.AddRange(WriteOwnedProperty(owned, value, writer));

        result.Add(Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteEndObjectMethod));

        var write = Expression.Block(result);
        if (ignoreNull) return write;

        var ifNotNull = ExpressionHelpers.GetIfNotNull(value);
        if (ifNotNull == null)
            return write;

        // Null check
        return Expression.IfThenElse(ifNotNull,
            write,
            Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteNullValueMethod));
    }

    /// <summary>
    /// Writes an owned entity value to the writer with the property name.
    /// </summary>
    private static Expression[] WriteOwnedProperty(
        OwnedEntityInfo entity,
        Expression parent,
        ParameterExpression writer)
    {
        var jsonPropertyName = entity.EntityType.GetJsonPropertyName();
        Debug.Assert(jsonPropertyName != null, "jsonPropertyName != null");
        var value = Expression.Property(parent, entity.PropertyInfo);
        return
        [
            WritePropertyName(jsonPropertyName, writer),
            WriteOwned(entity, value, writer)
        ];
    }

    private static Expression[] WriteComplex(ComplexEntityPropertyInfo entity, Expression parent, ParameterExpression writer)
    {
        var jsonPropertyName = entity.PropertyInfo.Name;
        var writePropertyName = WritePropertyName(jsonPropertyName, writer);

        var value = Expression.Property(parent, entity.PropertyInfo);

        var writeBlock = new List<Expression>
        {
            Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteStartObjectMethod),
        };

        foreach (var scalar in entity.ScalarProps)
            writeBlock.AddRange(WriteScalar(scalar, value, writer));
        foreach (var complex in entity.ComplexProps)
            writeBlock.AddRange(WriteComplex(complex, value, writer));

        writeBlock.Add(Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteEndObjectMethod));

        Expression write = Expression.Block(writeBlock);

        var ifNotNull = ExpressionHelpers.GetIfNotNull(value);
        if (ifNotNull != null)
            write = Expression.IfThenElse(ifNotNull,
                write,
                Expression.Call(writer, ReflectionItems.Utf8JsonWriterWriteNullValueMethod));
        return
        [
            writePropertyName,
            write
        ];
    }

    private static Expression[] WriteScalar(
        ScalarEntityPropertyInfo scalar,
        Expression parent,
        ParameterExpression writer)
    {
        var jsonPropertyName = scalar.Property.GetJsonPropertyName();
        Debug.Assert(jsonPropertyName != null, "jsonPropertyName != null");

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