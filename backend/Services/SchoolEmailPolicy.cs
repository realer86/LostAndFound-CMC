using System.ComponentModel.DataAnnotations;

namespace LostFound.Api.Services;

/// <summary>Only exact, school-controlled email domains may create or use a session.</summary>
public sealed class SchoolEmailPolicy
{
    private readonly string _studentDomain;
    private readonly string _lecturerDomain;
    private readonly bool _allowLocalTestAccounts;

    public SchoolEmailPolicy(IConfiguration config, IHostEnvironment environment)
    {
        _studentDomain = NormalizeDomain(config["Auth:StudentEmailDomain"] ?? "st.cmcu.edu.vn");
        _lecturerDomain = NormalizeDomain(config["Auth:LecturerEmailDomain"] ?? "cmcu.edu.vn");
        _allowLocalTestAccounts = environment.IsDevelopment() && config.GetValue<bool>("DevSeed:Enabled");
        if (string.IsNullOrEmpty(_studentDomain) || string.IsNullOrEmpty(_lecturerDomain) || _studentDomain == _lecturerDomain)
            throw new InvalidOperationException("School email domains must be distinct and non-empty.");
    }

    private static string NormalizeDomain(string value) => value.Trim().TrimStart('@').ToLowerInvariant();

    public static string NormalizeEmail(string? value) => (value ?? "").Trim().ToLowerInvariant();

    public string? AccountType(string? value)
    {
        var schoolType = SchoolAccountType(value);
        return schoolType ?? (_allowLocalTestAccounts ? DevelopmentTestUsers.AccountType(value) : null);
    }

    // Registration and Google Login only accept real school emails, never local demo accounts.
    public bool IsSchoolEmail(string? value) => SchoolAccountType(value) is not null;
    public bool IsLocalTestAccount(string? value) =>
        _allowLocalTestAccounts && DevelopmentTestUsers.AccountType(value) is not null;

    private string? SchoolAccountType(string? value)
    {
        var email = NormalizeEmail(value);
        if (email.Length > 180 || !new EmailAddressAttribute().IsValid(email)) return null;
        var at = email.LastIndexOf('@');
        if (at < 1 || at == email.Length - 1) return null;
        var domain = email[(at + 1)..];
        if (string.Equals(domain, _studentDomain, StringComparison.Ordinal)) return "STUDENT";
        if (string.Equals(domain, _lecturerDomain, StringComparison.Ordinal)) return "LECTURER";
        return null;
    }

    public string AllowedDomains => $"@{_studentDomain} hoặc @{_lecturerDomain}";
}
