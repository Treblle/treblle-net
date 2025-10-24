using System.Text.RegularExpressions;

namespace Treblle.Net.Masking
{
    public sealed class CreditCardMasker : DefaultStringMasker, IStringMasker
    {
        private static readonly Regex CreditCardRegex = new Regex(@"\d{4}-?\d{4}-?\d{4}-?\d{4}", RegexOptions.Compiled);
        private static readonly Regex NonDigitRegex = new Regex(@"\D", RegexOptions.Compiled);
        private const string creditCardMask = "****-****-****-";

        public override bool IsPatternMatch(string input)
        {
            return CreditCardRegex.IsMatch(input);
        }

        public string Mask(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if (CreditCardRegex.IsMatch(input))
            {
                // Remove non-digit characters from the input
                string sanitizedCard = NonDigitRegex.Replace(input, "");

                // If the result isn't 16 digits long, return original
                if (sanitizedCard.Length != 16)
                {
                    return input;
                }

                // Return the masked card
                return $"{creditCardMask}{sanitizedCard.Substring(sanitizedCard.Length - 4)}";
            }

            return base.Mask(input);
        }
    }
}