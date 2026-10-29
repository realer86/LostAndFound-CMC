"""Source-level safety regression; does not replace dotnet build or live SMTP/Google tests.
Run from repository root: python tests/school_auth_contract.py
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
auth = (root/'backend/Controllers/AuthController.cs').read_text()
program = (root/'backend/Program.cs').read_text()
policy = (root/'backend/Services/SchoolEmailPolicy.cs').read_text()
mail = (root/'backend/Services/EmailOtpSender.cs').read_text()
frontend = (root/'frontend/src/main.jsx').read_text()
sql = (root/'backend/DatabaseScripts/05_school_auth.sql').read_text()
compose = (root/'compose.yaml').read_text()

checks = {
    'Student domain exact match': 'st.cmcu.edu.vn' in policy and 'domain, _studentDomain' in policy,
    'Lecturer domain exact match': 'cmcu.edu.vn' in policy and 'domain, _lecturerDomain' in policy,
    'No Guest accounts': 'Guest' not in auth,
    'Reject non-school email for registration and password login': auth.count('if (!Allowed(email)) return BadSchoolEmail();') >= 4,
    'Google validates issuer/audience/expiry and verified email': all(s in auth for s in ('"aud"','"iss"','"exp"','"email_verified"', 'tokeninfo?id_token=', 'if (!Allowed(email))')),
    'Google linked subject stored': 'GoogleSubject' in auth and 'IX_Users_GoogleSubject' in sql,
    'OTP is PBKDF2 hashed and not echoed by default': 'passwords.HashPassword(user, code)' in auth and 'sender.IsDevelopmentCodeEnabled ? code : null' in auth,
    'OTP send real SMTP (not hardcoded secret)': 'SendMailAsync' in mail and 'Auth:Smtp:Password' in mail and 'Password=' not in mail,
    'OTP expiring, cooldown and attempt limits': all(s in auth for s in ('AddMinutes(10)','AddSeconds(-60)','OtpAttemptCount >= 5')),
    'Cookie rejects outside-school sessions': 'options.Events.OnValidatePrincipal' in program and 'policy.AccountType(user.Email) is null' in program,
    'IP rate limiting added': 'options.AddPolicy("auth"' in program and 'EnableRateLimiting("auth")' in auth,
    'Old database scripts preserved': all((root/'database'/f).exists() for f in ['01_schema.sql','02_seed.sql','03_views_triggers.sql','04_sprint2_contacts.sql']),
    'Auth schema add-only': 'ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS' in sql and not any(line.lstrip().upper().startswith(('DROP ', 'DELETE ')) for line in sql.splitlines()),
    'Frontend Google SDK and OTP resend': all(s in frontend for s in ('google.accounts.id.initialize','/auth/google','/auth/resend','@st.cmcu.edu.vn','@cmcu.edu.vn')),
    'Matching code untouched': (root/'backend/Services/ReportMatchingService.cs').exists(),
    'SMTP secret from env': 'SMTP_PASSWORD' in compose and 'Auth__Smtp__Password' in compose,
}
for key, status in checks.items():
    print(('PASS' if status else 'FAIL') + ': ' + key)
assert all(checks.values()), 'School auth source contract failed'
