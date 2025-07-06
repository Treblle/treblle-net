using System.Collections.Generic;

namespace Treblle.Net
{
    public static class Constants
    {
        public static readonly Dictionary<string, string> MaskingMap = new Dictionary<string, string>()
        {
            { "password", "DefaultStringMasker" },
            { "pwd", "DefaultStringMasker" },
            { "secret", "DefaultStringMasker" },
            { "password_confirmation", "DefaultStringMasker" },
            { "passwordConfirmation", "DefaultStringMasker" },
            { "cc", "CreditCardMasker" },
            { "card_number", "CreditCardMasker" },
            { "cardNumber", "CreditCardMasker" },
            { "ccv", "CreditCardMasker" },
            { "ssn", "SocialSecurityMasker" },
            { "credit_score", "DefaultStringMasker" },
            { "creditScore", "DefaultStringMasker" },
            { "email", "EmailMasker" },
            { "account.*", "DefaultStringMasker" },
            { "user.email", "EmailMasker" },
            { "user.dob", "DateMasker" },
            { "user.password","DefaultStringMasker" },
            { "user.ss", "SocialSecurityMasker" },
            { "user.payments.cc", "CreditCardMasker" }
        };
    }
}
