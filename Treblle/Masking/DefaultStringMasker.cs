using System.Text.RegularExpressions;

namespace Treblle.Net.Masking
{
    public class DefaultStringMasker : IStringMasker
    {
        private static readonly Regex MaskAllRegex = new Regex(".", RegexOptions.Compiled | RegexOptions.Singleline);

        public virtual bool IsPatternMatch(string input)
        {
            return false;
        }

        public virtual string Mask(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return MaskAllRegex.Replace(input, "*");
        }
    }
}
