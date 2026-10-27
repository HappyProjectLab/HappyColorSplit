using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HappyColorSplit.Services;

public static class AppVersion
{
    public static string Current { get; } = (typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? throw new InvalidOperationException("Missing application version.")).Split('+')[0];
}

public sealed class ReleaseVersion : IComparable<ReleaseVersion>
{
    private static readonly Regex Pattern = new(@"\Av?(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?\z", RegexOptions.CultureInvariant);
    private readonly BigInteger[] _numbers;
    private readonly string[] _pre;
    public bool IsPrerelease => _pre.Length > 0;
    private ReleaseVersion(BigInteger[] numbers, string[] pre) { _numbers = numbers; _pre = pre; }
    public static bool TryParse(string? value, out ReleaseVersion version)
    {
        version = null!;
        if (value is null || value.Length > 150) return false;
        var match = Pattern.Match(value);
        if (!match.Success) return false;
        string[] pre = match.Groups[4].Success ? match.Groups[4].Value.Split('.') : [];
        if (pre.Any(part => part.Length > 1 && part[0] == '0' && part.All(char.IsAsciiDigit))) return false;
        version = new([BigInteger.Parse(match.Groups[1].Value), BigInteger.Parse(match.Groups[2].Value), BigInteger.Parse(match.Groups[3].Value)], pre);
        return true;
    }
    public int CompareTo(ReleaseVersion? other)
    {
        if (other is null) return 1;
        for (int i = 0; i < 3; i++) { int c = _numbers[i].CompareTo(other._numbers[i]); if (c != 0) return c; }
        if (!IsPrerelease || !other.IsPrerelease) return IsPrerelease ? -1 : other.IsPrerelease ? 1 : 0;
        for (int i = 0; i < Math.Min(_pre.Length, other._pre.Length); i++)
        {
            bool a = _pre[i].All(char.IsAsciiDigit), b = other._pre[i].All(char.IsAsciiDigit);
            int c = a && b ? BigInteger.Parse(_pre[i]).CompareTo(BigInteger.Parse(other._pre[i])) : a != b ? a ? -1 : 1 : string.CompareOrdinal(_pre[i], other._pre[i]);
            if (c != 0) return c;
        }
        return _pre.Length.CompareTo(other._pre.Length);
    }
}
