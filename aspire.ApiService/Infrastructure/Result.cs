namespace aspire.ApiService.Infrastructure;

/// <summary>Why an operation failed; endpoints map it to a status code in one place.</summary>
public enum ErrorKind
{
    /// <summary>The request was understood but breaks a rule (400).</summary>
    Validation,

    /// <summary>The addressed resource does not exist (404).</summary>
    NotFound
}

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public IReadOnlyList<string> Errors { get; }
    public ErrorKind? Kind { get; }

    Result(bool isSuccess, T? value, IReadOnlyList<string> errors, ErrorKind? kind)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
        Kind = kind;
    }

    public static Result<T> Success(T? value) => new(true, value, [], null);

    public static Result<T> Failure(params string[] errors)
        => new(false, default, errors.Length == 0 ? ["Unknown error."] : errors, ErrorKind.Validation);

    public static Result<T> Failure(IEnumerable<string> errors)
    {
        var errorList = errors as IReadOnlyList<string> ?? errors.ToArray();
        return new(false, default, errorList.Count == 0 ? ["Unknown error."] : errorList, ErrorKind.Validation);
    }

    public static Result<T> NotFound(string error) => new(false, default, [error], ErrorKind.NotFound);
}
