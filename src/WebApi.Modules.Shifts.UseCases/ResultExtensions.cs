namespace soft.square.net.WebApi.Modules.Shifts.UseCases;

public static class ResultExtensions
{
    /// <summary>A failed <see cref="Result"/> as a <see cref="Result{T}"/>: same status, same messages.</summary>
    public static Result<T> As<T>(this Result failure) => failure.Status switch
    {
        ResultStatus.Forbidden => Result<T>.Forbidden([.. failure.Errors]),
        ResultStatus.NotFound => Result<T>.NotFound([.. failure.Errors]),
        ResultStatus.Conflict => Result<T>.Conflict([.. failure.Errors]),
        ResultStatus.Unauthorized => Result<T>.Unauthorized([.. failure.Errors]),
        ResultStatus.Invalid => Result<T>.Invalid([.. failure.ValidationErrors]),
        ResultStatus.Unavailable => Result<T>.Unavailable([.. failure.Errors]),
        ResultStatus.Ok => throw new InvalidOperationException("Only a failed result converts."),
        _ => Result<T>.Error(new ErrorList([.. failure.Errors]))
    };

    /// <summary>A failed <see cref="Result{T}"/> as another <see cref="Result{TOther}"/>: same status, same messages.</summary>
    public static Result<TOther> As<T, TOther>(this Result<T> failure) => failure.Status switch
    {
        ResultStatus.Forbidden => Result<TOther>.Forbidden([.. failure.Errors]),
        ResultStatus.NotFound => Result<TOther>.NotFound([.. failure.Errors]),
        ResultStatus.Conflict => Result<TOther>.Conflict([.. failure.Errors]),
        ResultStatus.Unauthorized => Result<TOther>.Unauthorized([.. failure.Errors]),
        ResultStatus.Invalid => Result<TOther>.Invalid([.. failure.ValidationErrors]),
        ResultStatus.Unavailable => Result<TOther>.Unavailable([.. failure.Errors]),
        ResultStatus.Ok => throw new InvalidOperationException("Only a failed result converts."),
        _ => Result<TOther>.Error(new ErrorList([.. failure.Errors]))
    };
}
