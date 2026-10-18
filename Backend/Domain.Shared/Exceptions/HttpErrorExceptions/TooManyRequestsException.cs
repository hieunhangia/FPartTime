namespace Domain.Shared.Exceptions.HttpErrorExceptions;

public class TooManyRequestsException(string message = "Too Many Requests", Exception? innerException = null)
    : HttpErrorExceptionBase(message, 429, "Too Many Requests", innerException);