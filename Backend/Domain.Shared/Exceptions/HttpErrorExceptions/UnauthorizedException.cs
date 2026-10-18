namespace Domain.Shared.Exceptions.HttpErrorExceptions;

public class UnauthorizedException(string message = "Unauthorized", Exception? innerException = null)
    : HttpErrorExceptionBase(message, 401, "Unauthorized", innerException);