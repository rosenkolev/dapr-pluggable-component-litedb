using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Google.Protobuf.WellKnownTypes;

using LiteDB;

namespace DaprLiteDbComponent;

internal static class Helper
{
    internal static BsonDocument ReadJsonAsBsonDocument(ReadOnlySpan<byte> jsonSpan) =>
        JsonSerializer.Deserialize(Encoding.UTF8.GetString(jsonSpan)).AsDocument;

    internal static byte[] SerializeBsonDocument(BsonDocument document) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(document));

    internal static BsonExpression[] ListToSingleProtoBufValue(
        Any list,
        Func<string, Any, BsonExpression> selector) =>
        list.Unpack<ListValue>()
            .Values
            .Select(
                val =>
                {
                    var s = val.StructValue.Fields.First();
                    return selector(s.Key, Any.Pack(s.Value));
                })
            .ToArray();

    internal static BsonValue ProtoBufValueToValue(Value value) =>
        value.KindCase switch
        {
            Value.KindOneofCase.NullValue => BsonValue.Null,
            Value.KindOneofCase.NumberValue => value.NumberValue,
            Value.KindOneofCase.StringValue => value.StringValue,
            Value.KindOneofCase.BoolValue => value.BoolValue,
            _ => throw new InvalidOperationException("Invalid value type"),
        };
}
