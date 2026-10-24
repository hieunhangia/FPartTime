namespace Domain.Shared.Constants;

public static class BusinessRuleConstants
{
    public const string PhoneNumberRegex = @"^0\d{9}$";
    public const string CCCDNumberRegex = @"^\d{12}$";
    public const string TaxCodeRegex = @"^\d{12}$";

    public const string DefaultAvatarFilePath = "default-candidate-avatar.png";
    public const string DefaultEmployerLogoFilePath = "default-employer-logo.png";

    public static class ObjectStorage
    {
        public const long MaxFileSizeInBytes = 5 * 1024 * 1024;

        public static class EmployerRegistration
        {
            public static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png"];
            public static readonly string[] AllowedDocumentExtensions = [".pdf", ".jpg", ".jpeg", ".png"];
            public const string BasePath = "employer-registrations";
            public const string Document = "document";
            public const string CccdFront = "cccd-front";
            public const string CccdBack = "cccd-back";
        }
    }

    public static class Identity
    {
        public const string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{6,}$";
        public const string OtpRegex = @"^\d{6}$";
        public const int OtpExpiresInMinutes = 5;
        public const int MaxOtpFailedAttempts = 5;
    }

    public static class Models
    {
        public const int PhoneNumberLength = 10;

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

        public static class OrderBase
        {
            public const int DescriptionMaxLength = 500;
        }

        public static class PaymentTransaction
        {
            public const int PaymentReferenceIdMaxLength = 100;
        }

        public static class EmployerSubscriptionPlan
        {
            public const int TierMaxLength = 50;
            public const int DurationMaxLength = 50;
            public const int NameMaxLength = 100;
            public const int DescriptionMaxLength = 500;
        }
    }

    public static class Candidates
    {
        public const int OpenForJobAvailabilityDays = 7;
    }

    public static class Employers
    {
        public const int DefaultMaxActivePosts = 5;

        public static class Subscriptions
        {
            public const int PaymentExpiresInMinutes = 15;
            public const int MonthlyDurationDays = 30;
            public const int YearlyDurationDays = 365;

            public static class Plus
            {
                public const int ExtraActivePosts = 5;
                public const int MonthlyFreeBumps = 5;
                public const int YearlyFreeBumps = 20;
                public const decimal MonthlyPrice = 30_000m;
                public const decimal YearlyPrice = 200_000m;
            }

            public static class Pro
            {
                public const int ExtraActivePosts = 10;
                public const int MonthlyFreeBumps = 10;
                public const int YearlyFreeBumps = 40;
                public const decimal MonthlyPrice = 100_000m;
                public const decimal YearlyPrice = 600_000m;
            }

            public static class Ultra
            {
                public const int ExtraActivePosts = 20;
                public const int MonthlyFreeBumps = 50;
                public const int YearlyFreeBumps = 100;
                public const decimal MonthlyPrice = 200_000m;
                public const decimal YearlyPrice = 1_000_000m;
            }
        }
    }

    public static class Industries
    {
        public const int DefaultPopularLimit = 5;
        public const int MaxPopularLimit = 100;
    }

    public static class Jobs
    {
        public const int MaxVacanciesPerJob = 1000;
        public const int MaxJobSchedulesPerJob = 30;
        public const int MaxJobSalariesPerJob = 10;
        public const int MaxJobExpiryDays = 90;
    }
}
