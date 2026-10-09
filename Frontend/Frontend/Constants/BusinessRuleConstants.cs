namespace Frontend.Constants;

public static class BusinessRuleConstants
{
    public const string PhoneNumberRegex = @"^0\d{9}$";
    public const string CCCDNumberRegex = @"^\d{12}$";

    public static class FileUpload
    {
        public const long MaxFileSizeInBytes = 5 * 1024 * 1024;
        public static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png"];
        public static readonly string[] AllowedDocumentExtensions = [".pdf", ".jpg", ".jpeg", ".png"];
    }

    public static class Identity
    {
        public const string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{6,}$";
        public const string OtpRegex = @"^\d{6}$";
    }
}