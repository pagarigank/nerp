using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ERP.Core.Domain.Common;

public readonly record struct SegmentType : IEquatable<SegmentType>
{
    public const int MaxLength = 20;

    [JsonPropertyName("type")]
    public string Type { get; init; }

    private SegmentType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Segment type cannot be empty", nameof(type));

        if (type.Length > MaxLength)
            throw new ArgumentException($"Segment type cannot exceed {MaxLength} characters", nameof(type));

        Type = type.Trim().ToUpperInvariant();
    }

    public static SegmentType Create(string type) => new(type);

    public static readonly SegmentType Account = new("ACCOUNT");
    public static readonly SegmentType SubAccount = new("SUBACCOUNT");
    public static readonly SegmentType Department = new("DEPARTMENT");
    public static readonly SegmentType Project = new("PROJECT");
    public static readonly SegmentType CostCenter = new("COSTCENTER");
    public static readonly SegmentType Location = new("LOCATION");

    public bool Equals(SegmentType other) => Type == other.Type;

    public override int GetHashCode() => Type.GetHashCode();

    public override string ToString() => Type;
}

[JsonConverter(typeof(AccountKeyJsonConverter))]
public readonly record struct AccountKey : IEquatable<AccountKey>
{
    [JsonPropertyName("segments")]
    public IReadOnlyDictionary<SegmentType, SegmentValue> Segments { get; init; }

    private AccountKey(IReadOnlyDictionary<SegmentType, SegmentValue> segments)
    {
        Segments = segments.ToImmutableDictionary();
    }

    public static AccountKey Create(params (SegmentType Type, SegmentValue Value)[] segments)
    {
        if (segments.Length == 0)
            throw new ArgumentException("Account key must have at least one segment", nameof(segments));

        var dict = segments.ToDictionary(s => s.Type, s => s.Value);
        return new AccountKey(dict);
    }

    public static AccountKey Create(IReadOnlyDictionary<SegmentType, SegmentValue> segments)
    {
        if (segments.Count == 0)
            throw new ArgumentException("Account key must have at least one segment", nameof(segments));

        return new AccountKey(segments);
    }

    public SegmentValue? GetSegment(SegmentType type) => Segments.TryGetValue(type, out var value) ? value : null;

    public bool HasSegment(SegmentType type) => Segments.ContainsKey(type);

    public AccountKey WithSegment(SegmentType type, SegmentValue value)
    {
        var newSegments = Segments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        newSegments[type] = value;
        return new AccountKey(newSegments);
    }

    public AccountKey WithoutSegment(SegmentType type)
    {
        var newSegments = Segments.Where(kvp => kvp.Key != type).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return new AccountKey(newSegments);
    }

    public string ToDisplayString(string separator = "-") =>
        string.Join(separator, Segments.OrderBy(kvp => kvp.Key.Type).Select(kvp => kvp.Value.Value));

    public bool Equals(AccountKey other) =>
        Segments.Count == other.Segments.Count &&
        Segments.All(kvp => other.Segments.TryGetValue(kvp.Key, out var v) && kvp.Value.Equals(v));

    public override int GetHashCode() => Segments.Aggregate(0, (acc, kvp) => HashCode.Combine(acc, kvp.Key, kvp.Value));

    public override string ToString() => ToDisplayString();

    public static implicit operator string(AccountKey key) => key.ToDisplayString();
}

public class AccountKeyJsonConverter : System.Text.Json.Serialization.JsonConverter<AccountKey>
{
    public override AccountKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start object for AccountKey");

        var dict = new Dictionary<SegmentType, SegmentValue>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name");

            var segmentTypeStr = reader.GetString();
            if (string.IsNullOrEmpty(segmentTypeStr))
                throw new JsonException("Segment type cannot be empty");

            var segmentType = SegmentType.Create(segmentTypeStr);

            reader.Read();
            var segmentValueStr = reader.GetString();
            var segmentValue = string.IsNullOrEmpty(segmentValueStr) ? default : SegmentValue.Create(segmentValueStr);

            dict[segmentType] = segmentValue;
        }

        return AccountKey.Create(dict);
    }

    public override void Write(Utf8JsonWriter writer, AccountKey value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value.Segments.OrderBy(k => k.Key.Type))
        {
            writer.WriteString(kvp.Key.Type, kvp.Value.Value);
        }

        writer.WriteEndObject();
    }
}