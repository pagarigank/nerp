using System.Text.Json.Serialization;

namespace ERP.Core.Common;

public readonly record struct Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; }

    [JsonPropertyName("value")]
    public T? Value => IsSuccess ? _value : throw new InvalidOperationException("Cannot access Value on failure result.");

    [JsonPropertyName("error")]
    public Error? Error => !IsSuccess ? _error : throw new InvalidOperationException("Cannot access Error on success result.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!.Value);
}

public readonly record struct Result
{
    private readonly Error? _error;

    private Result(Error error)
    {
        _error = error;
        IsSuccess = false;
    }

    public Result()
    {
        _error = null;
        IsSuccess = true;
    }

    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; }

    [JsonPropertyName("error")]
    public Error? Error => !IsSuccess ? _error : throw new InvalidOperationException("Cannot access Error on success result.");

    public static Result Success() => new();

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);

    public void Match(Action onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess)
            onSuccess();
        else
            onFailure(_error!.Value);
    }
}

public readonly record struct Error(string Code, string Message, object? Details = null)
{
    public static Error None => new(string.Empty, string.Empty);

    public static Error Validation(string code, string message, object? details = null) =>
        new(code, message, details);

    public static Error NotFound(string code, string message, object? details = null) =>
        new(code, message, details);

    public static Error Conflict(string code, string message, object? details = null) =>
        new(code, message, details);

    public static Error Unauthorized(string code, string message, object? details = null) =>
        new(code, message, details);

    public static Error Forbidden(string code, string message, object? details = null) =>
        new(code, message, details);

    public static Error Internal(string code, string message, object? details = null) =>
        new(code, message, details);

    public static Error BadRequest(string code, string message, object? details = null) =>
        new(code, message, details);
}