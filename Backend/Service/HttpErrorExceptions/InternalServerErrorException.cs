namespace Service.HttpErrorExceptions;

public class InternalServerErrorException(string message = "Internal Server Error", Exception? innerException = null)
    : HttpErrorExceptionBase(message, 500, "Internal Server Error", innerException);