using System.Text.RegularExpressions;

namespace Treblle.Net.Masking
{
    public sealed class SocialSecurityMasker : DefaultStringMasker, IStringMasker
    {
        private static readonly Regex SocialSecurityRegex = new Regex(@"^\d{3}-\d{2}-\d{4}$", RegexOptions.Compiled);
        private static readonly Regex ReplaceRegex = new Regex(@"^(\d{3}-\d{2}-)(\d{4})$", RegexOptions.Compiled);
        private const string _mask = "***-**-$2";

        public override bool IsPatternMatch(string input)
        {
            return SocialSecurityRegex.IsMatch(input);
        }

        string IStringMasker.Mask(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if (SocialSecurityRegex.IsMatch(input))
            {
                // Replace the first part of the SSN with asterisks
                return ReplaceRegex.Replace(input, _mask);
            }

            return base.Mask(input);
        }
    }
}
