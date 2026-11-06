"""Static regressions for password recovery. Run real API/database/SMTP tests before deployment."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(file):return (root/file).read_text('utf-8')
auth=read('backend/Controllers/AuthController.cs')
program=read('backend/Program.cs')
entities=read('backend/Domain/Entities.cs')
schema=read('backend/DatabaseScripts/08_password_recovery.sql')
mail=read('backend/Services/EmailOtpSender.cs')
frontend=read('frontend/src/main.jsx')
admin=read('backend/Controllers/AdminAuthController.cs')
seed=read('backend/Services/DevelopmentTestUsers.cs')
forgot=auth.split('[HttpPost("forgot-password")',1)[1].split('[HttpPost("reset-password")',1)[0]
reset=auth.split('[HttpPost("reset-password")',1)[1].split('[HttpPost("logout")',1)[0]
checks={
    'Recovery uses independent table, not registration OTP columns':
        'class PasswordResetTicket' in entities and 'db.PasswordResetTickets' in forgot and 'db.PasswordResetTickets' in reset and
        not any(t in forgot+reset for t in ('VerifyTokenHash','VerifyExpiresAt','OtpAttemptCount','OtpSentAt')),
    'Recovery is limited to verified school accounts':
        'if (!Allowed(email))' in forgot and 'user.EmailVerifiedAt is null' in forgot and 'user.EmailVerifiedAt is null' in reset,
    'Password recovery generic response even for throttled and unknown accounts':
        'return Ok(RecoveryResponse)' in forgot and 'StatusCode(429' not in forgot and
        'sender.IsDevelopmentCodeEnabled ? code' not in forgot,
    'Password reset OTP generated securely, hashed and expires in 10 minutes':
        'RandomNumberGenerator.GetInt32' in auth and 'passwords.HashPassword(user, code)' in forgot and 'now.AddMinutes(10)' in forgot,
    'Recovery policy isolated from existing auth endpoints':
        'EnableRateLimiting("password-recovery")' in forgot and 'EnableRateLimiting("password-recovery")' in reset and
        'AddPolicy("password-recovery"' in program,
    'Account OTP limits: cooldown, hour, day and wrong-code lockout':
        all(k in forgot for k in ('AddSeconds(-60)','RequestsInWindow >= 3','RequestsToday >= 10','FailedAttemptsInWindow >= 10')) and
        all(k in reset for k in ('FailedAttempts >= 5','ticket.FailedAttempts++','ticket.TokenHash = null')),
    'Race-proof serialized recovery and atomic OTP consumption':
        'FOR UPDATE' in forgot and 'FOR UPDATE' in reset and
        'BeginTransactionAsync' in forgot and 'BeginTransactionAsync' in reset and
        reset.index('user.PasswordHash =')<reset.rindex('ticket.TokenHash = null')<reset.rindex('CommitAsync'),
    'Password reset emits an audit record without secrets':
        'Action = "PASSWORD_RESET"' in reset and 'db.AuditLogs.Add(' in reset and 'logger.LogInformation' not in reset,
    'Old student/admin sessions are invalid after reset':
        'user.SessionVersion++' in reset and 'foundly:session-version' in auth and
        'foundly:session-version' in admin and program.count('FindFirstValue("foundly:session-version")')==2,
    'Test users rehash changed password and invalidate cookies':
        'existingUser.SessionVersion++' in seed and 'PasswordVerificationResult.Failed' in seed,
    'Email purpose is explicitly password reset':
        'OtpEmailPurpose.ResetPassword' in forgot and 'Mã đặt lại mật khẩu Foundly' in mail,
    'Migration is additive; database start-up executes it':
        'ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "SessionVersion"' in schema and
        'CREATE TABLE IF NOT EXISTS "PasswordResetTickets"' in schema and
        '08_password_recovery.sql' in program and not any(x in schema.upper() for x in ('DROP TABLE','TRUNCATE','DELETE FROM')),
    'Frontend has a recovery page and 12-character password minimum':
        "'/auth/forgot-password'" in frontend and "'/auth/reset-password'" in frontend and
        "mode==='reset'?12" in frontend,
    'Admin EyeOff import matches icon usage':
        'EyeOff,' in read('admin/src/main.jsx') and 'Eyeoff' not in read('admin/src/main.jsx'),
}
for name,ok in checks.items(): print(('PASS' if ok else 'FAIL')+': '+name)
assert all(checks.values()),'password recovery security contract failed'
