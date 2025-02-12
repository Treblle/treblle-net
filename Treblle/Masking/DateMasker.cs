using System.Text.RegularExpressions;

namespace Treblle.Net.Masking
{
    public sealed class DateMasker : DefaultStringMasker, IStringMasker
    {
        private const string _datePatternSlashes = @"^((0?[1-9]|1[0-2])\/(0?[1-9]|[12][0-9]|3[01])\/)(19|20)\d{2}$";
        private const string _datePatternSlashesYearFirst = @"^(19|20)\d{2}\/(0[1-9]|1[0-2])\/(0[1-9]|[12][0-9]|3[01])$";
        private const string _datePatternDashes = @"^(0?[1-9]|[12][0-9]|3[01])-(0?[1-9]|1[0-2])-(19|20)\d{2}$";
        private const string _datePatternDashesYearFirst = @"^(19|20)\d{2}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])$";
        private const string _dateMask = "$1****";

        public override bool IsPatternMatch(string input)
        {
            return Regex.IsMatch(input, _datePatternSlashes) || Regex.IsMatch(input, _datePatternSlashesYearFirst) ||
                (Regex.IsMatch(input, _datePatternDashes) || Regex.IsMatch(input, _datePatternDashesYearFirst));
        }

        public string Mask(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if (Regex.IsMatch(input, _datePatternSlashes))
                return Regex.Replace(input, _datePatternSlashes, _dateMask);

            if (Regex.IsMatch(input, _datePatternSlashesYearFirst))
                return Regex.Replace(input, _datePatternSlashesYearFirst, _dateMask);

            if (Regex.IsMatch(input, _datePatternDashes))
                return Regex.Replace(input,_datePatternDashes, _dateMask);

            if (Regex.IsMatch(input, _datePatternDashesYearFirst))
                return Regex.Replace(input, _datePatternDashesYearFirst, _dateMask);

            return base.Mask(input);
        }
    }
}
