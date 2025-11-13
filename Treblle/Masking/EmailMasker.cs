using System.Text.RegularExpressions;

namespace Treblle.Net.Masking
{
    public sealed class EmailMasker : DefaultStringMasker, IStringMasker
    {
        private static readonly Regex EmailRegex = new Regex(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$", RegexOptions.Compiled);

        public override bool IsPatternMatch(string input)
        {
            return EmailRegex.IsMatch(input);
        }

        string IStringMasker.Mask(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if (EmailRegex.IsMatch(input))
            {
                // Find the @ symbol and mask only the local part (before @)
                int atIndex = input.IndexOf('@');
                if (atIndex > 0)
                {
                    string localPart = input.Substring(0, atIndex);
                    string domainPart = input.Substring(atIndex); // Includes @
                    return new string('*', localPart.Length) + domainPart;
                }
            }

            return base.Mask(input);
        }
    }
}
