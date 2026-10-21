using System.Text.RegularExpressions;

namespace LostFound.Api.Services;

/// <summary>
/// Anonymous chat: reject obvious personal email/mobile numbers in message text.
/// This is a safeguard for demo, NOT a guarantee that free text can never reveal PII.
/// </summary>
public static class PrivacyGuard
{
    private static readonly Regex Email = new(
        @"(?<![\w.+-])[\w.+-]+@[\w.-]+\.[a-zA-Z]{2,}(?![\w.-])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(150));

    private static readonly Regex Mobile = new(
        @"(?<!\d)(?:\+?84|0)[\s.\-]?(?:\d[\s.\-]?){8,10}(?!\d)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(150));

    public static bool ContainsPersonalContact(string content) =>
        Email.IsMatch(content) || Mobile.IsMatch(content);

    // A stable pseudonym, not a real name, phone or email.
    public static string Alias(Guid id) => $"Người dùng {id.ToString("N")[..8]}";
}
