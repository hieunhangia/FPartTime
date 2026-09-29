namespace Service.HttpErrorExceptions;

public class HttpErrorExceptionBase(string message, int statusCode, string title, Exception? innerException)
    : Exception(message, innerException)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}