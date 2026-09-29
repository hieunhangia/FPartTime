namespace Service.HttpErrorExceptions;

public class BadRequestException(string message = "Bad Request", Exception? innerException = null)
    : HttpErrorExceptionBase(message, 400, "Bad Request", innerException);