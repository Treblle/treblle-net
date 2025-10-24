using System.Text.RegularExpressions;

namespace Treblle.Net.Masking
{
    public sealed class EmailMasker : DefaultStringMasker, IStringMasker
    {
        private static readonly Regex EmailRegex = new Regex(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$", RegexOptions.Compiled);
        private static readonly Regex ReplaceRegex = new Regex(@"([^@]+)", RegexOptions.Compiled);

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
                return ReplaceRegex.Replace(
                    input,
                    match => new string('*', match.Length)
                );
            }

            return base.Mask(input);
        }
    }
}
