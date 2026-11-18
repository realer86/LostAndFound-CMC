
"""Safety checks for opt-in local accounts. Not a .NET/runtime integration test."""

import re
from pathlib import Path

R = Path(__file__).resolve().parents[1]


def read(path):
    return (R / path).read_text(encoding="utf-8")


seed = read("backend/Services/DevelopmentTestUsers.cs")
policy = read("backend/Services/SchoolEmailPolicy.cs")
auth = read("backend/Controllers/AuthController.cs")
program = read("backend/Program.cs")
compose = read("compose.yaml")
env = read(".env.example")
docs = read("docs/DEVELOPMENT_TEST_USERS.md")

# Ignore whitespace and line breaks when checking C# source.
seed_compact = re.sub(r"\s+", "", seed)

checks = {
    "Exactly four reserved .invalid identities":
        seed.count("@foundly.invalid") == 4
        and all(name in seed for name in (
            "student01",
            "student02",
            "lecturer01",
            "admin"
        )),

    "Explicit opt-in and Development guard":
        'if(!config.GetValue<bool>("DevSeed:Enabled"))return;' in seed_compact
        and "if(!environment.IsDevelopment())" in seed_compact,

    "No hardcoded test password":
        'config["DevSeed:Password"]' in seed
        and "password.Length < 16" in seed
        and "DEV_SEED_PASSWORD=" in env
        and "DEV_SEED_PASSWORD:=" not in compose,

    "Passwords hashed":
        "hasher.HashPassword(user, password)" in seed
        and "EmailVerifiedAt = DateTime.UtcNow" in seed,

    "Existing test accounts update only when configured password changes":
        all(k in seed_compact for k in (
            "FirstOrDefaultAsync(u=>u.Email==persona.Email)",
            "PasswordVerificationResult.Failed",
            "existingUser.PasswordHash=hasher.HashPassword(existingUser,password)",
            "existingUser.SessionVersion++",
            "continue;"
        )),

    "No wildcard demo email authorization":
        "Personas.FirstOrDefault(persona=>persona.Email==normalized)"
        in seed_compact,

    "Policy blocks tests by default":
        'environment.IsDevelopment() && config.GetValue<bool>("DevSeed:Enabled")'
        in policy
        and "_allowLocalTestAccounts ? DevelopmentTestUsers.AccountType(value) : null"
        in policy,

    "Public register and Google restricted to real school domains":
        "private bool Allowed(string email) => schoolEmails.IsSchoolEmail(email);"
        in auth
        and "if (!Allowed(email)) return BadSchoolEmail();" in auth,

    "Only password login allows local test identities":
        "if (!Allowed(email) && !schoolEmails.IsLocalTestAccount(email)) return BadSchoolEmail();"
        in auth,

    "Cookie revalidates account type":
        "policy.AccountType(user.Email) is null" in program,

    "Admin login enforces admin password":
        "passwordHasher.VerifyHashedPassword"
        in read("backend/Controllers/AdminAuthController.cs"),

    "Seeding after schema setup":
        program.index('"06_operations.sql"')
        < program.index("DevelopmentTestUsers.SeedIfEnabledAsync"),

    "Compose config opt-in defaults off":
        "DEV_SEED_ENABLED:-false" in compose
        and "DEV_SEED_PASSWORD:-" in compose,

    "Documented disabling":
        "DEV_SEED_ENABLED=false" in docs,

    "Matching implementation remains":
        (R / "backend/Services/ReportMatchingService.cs").exists()
}

for name, ok in checks.items():
    print(("PASS" if ok else "FAIL") + ": " + name)

assert all(checks.values()), "Dev seed contract failed"

print(f"\nDev Seed Contract: {sum(checks.values())}/{len(checks)} PASS")
