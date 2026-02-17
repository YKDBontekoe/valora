namespace Valora.Application.Common.Models;

public class Result
{
    internal Result(bool succeeded, IEnumerable<string> errors, string? errorCode = null)
    {
        Succeeded = succeeded;
        Errors = errors.ToArray();
        ErrorCode = errorCode;
    }

    public bool Succeeded { get; init; }

    public string[] Errors { get; init; }

    public string? ErrorCode { get; init; }

    public static Result Success()
    {
        return new Result(true, Array.Empty<string>());
    }

    public static Result Failure(IEnumerable<string> errors, string? errorCode = null)
    {
        return new Result(false, errors, errorCode);
    }
}
