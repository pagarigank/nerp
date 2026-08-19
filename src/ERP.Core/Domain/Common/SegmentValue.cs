using System.Text.Json;
using System.Text.Json.Serialization;

namespace ERP.Core.Domain.Common;

[JsonConverter(typeof(SegmentValueJsonConverter))]
public readonly record struct SegmentValue : IEquatable<SegmentValue>
{
    public const int MaxLength = 30;

    [JsonPropertyName("value")]
    public string Value { get; init; }

    private SegmentValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Segment value cannot be empty", nameof(value));

        if (value.Length > MaxLength)
            throw new ArgumentException($"Segment value cannot exceed {MaxLength} characters", nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    public static SegmentValue Create(string value) => new(value);

    public static bool TryCreate(string value, out SegmentValue segmentValue)
    {
        segmentValue = default;
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
            return false;

        segmentValue = new SegmentValue(value);
        return true;
    }

    public bool Equals(SegmentValue other) => Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static implicit operator string(SegmentValue segment) => segment.Value;

    public static explicit operator SegmentValue(string value) => Create(value);
}

public class SegmentValueJsonConverter : System.Text.Json.Serialization.JsonConverter<SegmentValue>
{
    public override SegmentValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return string.IsNullOrEmpty(value) ? default : SegmentValue.Create(value);
    }

    public override void Write(Utf8JsonWriter writer, SegmentValue value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}