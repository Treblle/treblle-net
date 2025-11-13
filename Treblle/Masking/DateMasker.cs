using System.Text.RegularExpressions;

namespace Treblle.Net.Masking
{
    public sealed class DateMasker : DefaultStringMasker, IStringMasker
    {
        private static readonly Regex DateSlashesRegex = new Regex(@"^((0?[1-9]|1[0-2])\/(0?[1-9]|[12][0-9]|3[01])\/)(19|20)\d{2}$", RegexOptions.Compiled);
        private static readonly Regex DateSlashesYearFirstRegex = new Regex(@"^(19|20)\d{2}\/(0[1-9]|1[0-2])\/(0[1-9]|[12][0-9]|3[01])$", RegexOptions.Compiled);
        private static readonly Regex DateDashesRegex = new Regex(@"^(0?[1-9]|[12][0-9]|3[01])-(0?[1-9]|1[0-2])-(19|20)\d{2}$", RegexOptions.Compiled);
        private static readonly Regex DateDashesYearFirstRegex = new Regex(@"^(19|20)\d{2}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])$", RegexOptions.Compiled);
        private const string _dateMask = "$1****";

        public override bool IsPatternMatch(string input)
        {
            return DateSlashesRegex.IsMatch(input) || DateSlashesYearFirstRegex.IsMatch(input) ||
                DateDashesRegex.IsMatch(input) || DateDashesYearFirstRegex.IsMatch(input);
        }

        string IStringMasker.Mask(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if (DateSlashesRegex.IsMatch(input))
                return DateSlashesRegex.Replace(input, _dateMask);

            if (DateSlashesYearFirstRegex.IsMatch(input))
                return DateSlashesYearFirstRegex.Replace(input, _dateMask);

            if (DateDashesRegex.IsMatch(input))
                return DateDashesRegex.Replace(input, _dateMask);

            if (DateDashesYearFirstRegex.IsMatch(input))
                return DateDashesYearFirstRegex.Replace(input, _dateMask);

            return base.Mask(input);
        }
    }
}
