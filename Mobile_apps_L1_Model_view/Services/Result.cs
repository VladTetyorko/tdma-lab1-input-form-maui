namespace Mobile_apps_L1_Model_view.Services;

// Outcome of a domain action: either a value or a message for the user
public sealed class Result<T>
{
    private Result(T? value, string? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new Result<T>(default, error);
    }
}
