using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Common;

namespace ERP.Shared.Kernel.Posting;

[JsonConverter(typeof(AccountKeyJsonConverter))]
public sealed class AccountKey : ValueObject
{
    private readonly ImmutableDictionary<string, string> _segments;

    public IReadOnlyDictionary<string, string> Segments => _segments;

    private AccountKey(ImmutableDictionary<string, string> segments)
    {
        _segments = segments;
    }

    public static AccountKey Create(params (string Key, string Value)[] segments)
    {
        var dict = segments.ToImmutableDictionary(s => s.Key.ToUpperInvariant(), s => s.Value);
        return new AccountKey(dict);
    }

    public static AccountKey Create(IDictionary<string, string> segments)
    {
        var dict = segments.ToImmutableDictionary(s => s.Key.ToUpperInvariant(), s => s.Value);
        return new AccountKey(dict);
    }

    public string? this[string key] => _segments.TryGetValue(key.ToUpperInvariant(), out var value) ? value : null;

    public bool ContainsKey(string key) => _segments.ContainsKey(key.ToUpperInvariant());

    public AccountKey WithSegment(string key, string value)
    {
        var newSegments = _segments.SetItem(key.ToUpperInvariant(), value);
        return new AccountKey(newSegments);
    }

    public AccountKey WithoutSegment(string key)
    {
        var newSegments = _segments.Remove(key.ToUpperInvariant());
        return new AccountKey(newSegments);
    }

    public string ToCompositeString(string separator = "-")
    {
        return string.Join(separator, _segments.Values);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        return _segments.OrderBy(kvp => kvp.Key).SelectMany(kvp => new object?[] { kvp.Key, kvp.Value });
    }

    public override string ToString() => ToCompositeString();

    private class AccountKeyJsonConverter : JsonConverter<AccountKey>
    {
        public override AccountKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return AccountKey.Create();

            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
            return dict != null ? AccountKey.Create(dict) : AccountKey.Create();
        }

        public override void Write(Utf8JsonWriter writer, AccountKey value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value._segments, options);
        }
    }
}