namespace Service.HttpErrorExceptions;

public class NotFoundException(string message = "Not Found", Exception? innerException = null)
    : HttpErrorExceptionBase(message, 404, "Not Found", innerException);
