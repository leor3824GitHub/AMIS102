using System.Text.Json;
using System.Text.Json.Serialization;

namespace FSH.Modules.Auditing.Contracts.Serialization;

/// <summary>
/// Forces an enum to be serialized as its numeric underlying value, overriding any globally
/// registered <see cref="JsonStringEnumConverter"/>. Reads accept either numbers or names.
/// </summary>
public sealed class JsonNumericEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt64(out var longValue))
            {
                return (TEnum)Enum.ToObject(typeof(TEnum), longValue);
            }
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            var name = reader.GetString();
            if (!string.IsNullOrEmpty(name) && Enum.TryParse<TEnum>(name, ignoreCase: true, out var parsed))
            {
                return parsed;
            }
        }

        throw new JsonException($"Unable to convert value to enum {typeof(TEnum).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
        var raw = Convert.ChangeType(value, underlyingType, System.Globalization.CultureInfo.InvariantCulture);

        switch (raw)
        {
            case byte b: writer.WriteNumberValue(b); break;
            case sbyte sb: writer.WriteNumberValue(sb); break;
            case short s: writer.WriteNumberValue(s); break;
            case ushort us: writer.WriteNumberValue(us); break;
            case int i: writer.WriteNumberValue(i); break;
            case uint ui: writer.WriteNumberValue(ui); break;
            case long l: writer.WriteNumberValue(l); break;
            case ulong ul: writer.WriteNumberValue(ul); break;
            default: writer.WriteNumberValue(Convert.ToInt64(raw, System.Globalization.CultureInfo.InvariantCulture)); break;
        }
    }
}
