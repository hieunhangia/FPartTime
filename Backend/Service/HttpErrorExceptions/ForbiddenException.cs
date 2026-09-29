namespace Service.HttpErrorExceptions;

public class ForbiddenException(string message = "Forbidden", Exception? innerException = null)
    : HttpErrorExceptionBase(message, 403, "Forbidden", innerException);
