namespace Repository.Constants;

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
        public const int OtpExpiresInMinutes = 5;
        public const int MaxOtpFailedAttempts = 5;
        public const int RefreshTokenCleanupIntervalInHours = 24;
    }

    public static class Models
    {
        public const int PhoneNumberLength = 10;

        public static class RefreshToken
        {
            public const int TokenMaxLength = 200;
        }

        public static class Commune
        {
            public const int CodeMaxLength = 10;
            public const int NameMaxLength = 100;
        }

        public static class Province
        {
            public const int CodeMaxLength = 10;
            public const int NameMaxLength = 100;
        }

        public static class Candidate
        {
            public const int AvatarFilePathMaxLength = 200;
            public const int BioMaxLength = 500;
            public const int FullNameMaxLength = 100;
        }

        public static class EmployerRegistrationRequest
        {
            public const int CompanyNameMaxLength = 100;
            public const int TaxCodeMaxLength = 50;
            public const int VerificationDocumentPathMaxLength = 200;
            public const int FullNameMaxLength = 100;
            public const int CCCDNumberMaxLength = 12;
            public const int RejectReasonMaxLength = 500;
        }

        public static class Employer
        {
            public const int LogoFilePathMaxLength = 200;
            public const int DescriptionMaxLength = 1000;
            public const int CompanyNameMaxLength = 100;
            public const int TaxCodeMaxLength = 50;
            public const int VerificationDocumentFilePathMaxLength = 200;
        }

        public static class Industry
        {
            public const int NameMaxLength = 100;
        }

        public static class Job
        {
            public const int TitleMaxLength = 200;
            public const int DescriptionMaxLength = 1000;
            public const int DetailAddressMaxLength = 200;
        }

        public static class JobReport
        {
            public const int ReasonMaxLength = 500;
        }

        public static class Notification
        {
            public const int TitleMaxLength = 200;
            public const int ContentMaxLength = 1000;
        }
    }

    public static class Employers
    {
        public const int DefaultMaxActivePosts = 5;
    }
}