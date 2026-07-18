namespace aspire.ApiService.Infrastructure;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public IReadOnlyList<string> Errors { get; }

    Result(bool isSuccess, T? value, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public static Result<T> Success(T? value) => new(true, value, []);

    public static Result<T> Failure(params string[] errors)
        => new(false, default, errors.Length == 0 ? ["Unknown error."] : errors);

    public static Result<T> Failure(IEnumerable<string> errors)
    {
        var errorList = errors as IReadOnlyList<string> ?? errors.ToArray();
        return new(false, default, errorList.Count == 0 ? ["Unknown error."] : errorList);
    }
}