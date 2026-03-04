using System.Text.RegularExpressions;

namespace ZikiBlog.Services;

public class SlugService
{
    private static readonly Regex InvalidChars = new(@"[^a-z0-9\u00C0-\u017F\s-]", RegexOptions.Compiled);
    private static readonly Regex MultiSpace  = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex MultiDash   = new(@"-{2,}", RegexOptions.Compiled);

    public string Generate(string input, int maxLength = 80)
    {
        if (string.IsNullOrWhiteSpace(input)) return Guid.NewGuid().ToString("n");

        var s = input.ToLowerInvariant();
        s = InvalidChars.Replace(s, "");
        s = MultiSpace.Replace(s, " ").Trim();
        s = s.Replace(" ", "-");
        s = MultiDash.Replace(s, "-");
        if (s.Length > maxLength) s = s.Substring(0, maxLength);
        s = s.Trim('-');
        return s;
    }
}
