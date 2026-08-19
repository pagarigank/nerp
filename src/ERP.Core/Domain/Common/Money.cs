using System.Text.Json.Serialization;

namespace ERP.Core.Domain.Common;

public readonly record struct Money : IComparable<Money>, IEquatable<Money>
{
    public const int DecimalPlaces = 4;
    public const decimal MaxValue = 999999999999.9999m;
    public const decimal MinValue = -999999999999.9999m;

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "USD";

    private Money(decimal amount, string currency = "USD")
    {
        if (amount > MaxValue || amount < MinValue)
            throw new ArgumentOutOfRangeException(nameof(amount), $"Amount must be between {MinValue} and {MaxValue}");

        Amount = Math.Round(amount, DecimalPlaces, MidpointRounding.AwayFromZero);
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    public static Money Zero(string currency = "USD") => new(0, currency);

    public static Money From(decimal amount, string currency = "USD") => new(amount, currency);

    public static Money FromCents(long cents, string currency = "USD") => new(cents / 100m, currency);

    public static bool TryParse(string input, string currency, out Money money)
    {
        money = Zero(currency);
        if (decimal.TryParse(input, out var amount))
        {
            money = new Money(amount, currency);
            return true;
        }
        return false;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract {other.Currency} from {Currency}");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public Money Divide(decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide by zero");
        return new Money(Amount / divisor, Currency);
    }

    public Money Negate() => new(-Amount, Currency);

    public Money Abs() => new(Math.Abs(Amount), Currency);

    public int Sign() => Math.Sign(Amount);

    public bool IsZero => Amount == 0;
    public bool IsPositive => Amount > 0;
    public bool IsNegative => Amount < 0;

    public int CompareTo(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot compare {Currency} and {other.Currency}");

        return Amount.CompareTo(other.Amount);
    }

    public bool Equals(Money other) => Currency == other.Currency && Amount == other.Amount;

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString()
    {
        var formatted = Amount.ToString("N" + DecimalPlaces);
        return $"{formatted} {Currency}";
    }

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static Money operator *(Money left, decimal right) => left.Multiply(right);
    public static Money operator *(decimal left, Money right) => right.Multiply(left);
    public static Money operator /(Money left, decimal right) => left.Divide(right);
    public static Money operator -(Money value) => value.Negate();

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;
}