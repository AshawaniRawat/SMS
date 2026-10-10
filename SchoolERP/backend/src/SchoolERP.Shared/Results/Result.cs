namespace SchoolERP.Shared.Results;
public sealed record Result<T>(bool IsSuccess, T? Value, string? Error, string? ErrorCode = null)
{
    public static Result<T> Ok(T value) => new(true, value, null, null);
    public static Result<T> Fail(string error, string? errorCode = null) => new(false, default, error, errorCode);
}
