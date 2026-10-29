namespace Frontend.Constants;

public static class BusinessRuleConstants
{
    public const string PhoneNumberRegex = @"^0\d{9}$";
    public const string CCCDNumberRegex = @"^\d{12}$";
    public const string TaxCodeRegex = @"^\d{12}$";

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

    public static class SystemHealth
    {
        public const int CacheDuration = 3;
    }

    public static class Models
    {
        public static class EmployerSubscriptionPlan
        {
            public const int NameMaxLength = 100;
            public const int DescriptionMaxLength = 500;
            public const int MaxDurationDays = 3650;
            public const int MaxExtraActivePosts = int.MaxValue - Employers.DefaultMaxActivePosts;
        }

        public static class Commune
        {
            public const int CodeMaxLength = 10;
        }

        public static class Candidate
        {
            public const int OpenForJobAvailabilityDays = 7;
        }

        public static class Job
        {
            public const int TitleMaxLength = 200;
            public const int DescriptionMaxLength = 1000;
            public const int DetailAddressMaxLength = 200;
        }
    }

    public static class Employers
    {
        public const int DefaultMaxActivePosts = 5;
    }

    public static class Jobs
    {
        public const int MaxVacanciesPerJob = 1000;
        public const int MaxJobSchedulesPerJob = 30;
        public const int MaxJobSalariesPerJob = 10;
        public const decimal MinSalaryAmount = 1000;
        public const decimal MaxSalaryAmount = 500000000;
        public const int MinAge = 15;
        public const int MaxAge = 100;
        public const int MaxJobExpiryDays = 90;
    }
}
