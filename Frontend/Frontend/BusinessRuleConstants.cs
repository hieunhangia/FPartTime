namespace Frontend;

public static class BusinessRuleConstants
{
    public const string PhoneNumberRegex = @"^0\d{9}$";

    public static class Identity
    {
        public const string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{6,}$";
        public const string OtpRegex = @"^\d{6}$";
    }
}