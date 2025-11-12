using System.Collections.Generic;

namespace Treblle.Net
{
    public static class Constants
    {
        /// <summary>
        /// SDK Payload Version - increment this number (21, 22, 23, etc.) when making updates to the SDK
        /// </summary>
        public const int SDK_VERSION = 20;

        /// <summary>
        /// Maximum payload size in bytes (2 MB)
        /// Requests and responses exceeding this size will have their body replaced with size information
        /// </summary>
        public const int MAX_PAYLOAD_BYTES = 2097152; // 2 MB

        /// <summary>
        /// Maximum payload size in megabytes
        /// </summary>
        public const double MAX_PAYLOAD_MB = 2.0;

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
