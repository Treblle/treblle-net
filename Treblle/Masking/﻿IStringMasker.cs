namespace Treblle.Net.Masking
{
    public interface IStringMasker
    {
        bool IsPatternMatch(string input);
        string Mask(string input);
    }
}