using System.Text.RegularExpressions;

namespace Treblle.Net.Masking
{
    public sealed class CreditCardMasker : DefaultStringMasker, IStringMasker
    {
        private const string creditCardPattern = @"\d{4}-?\d{4}-?\d{4}-?\d{4}";
        private const string creditCardMask = "****-****-****-";

        public override bool IsPatternMatch(string input)
        {
            return Regex.IsMatch(input, creditCardPattern);
        }

        public string Mask(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if (Regex.IsMatch(input, creditCardPattern))
            {
                // Remove non-digit characters from the input
                string sanitizedCard = Regex.Replace(input, @"\D", "");

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