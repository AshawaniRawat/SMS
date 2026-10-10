namespace SchoolERP.Shared.Models;
public sealed record ApiResponse<T>(bool Success, string? Message, T? Data, IEnumerable<string>? Errors)
{
    public static ApiResponse<T> Ok(T data, string? message = null) => new(true, message, data, null);
    public static ApiResponse<T> Fail(IEnumerable<string> errors, string? message = null) => new(false, message, default, errors);
}