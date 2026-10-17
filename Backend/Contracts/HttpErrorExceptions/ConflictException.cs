namespace Contracts.HttpErrorExceptions;

public class ConflictException(string message = "Conflict", Exception? innerException = null)
    : HttpErrorExceptionBase(message, 409, "Conflict", innerException);