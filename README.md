# HarvestHavenSecurePortal (.NET 8 Razor Pages)

A Visual Studio 2022-ready Razor Pages app demonstrating web security features:
- Registration w/ JPG-only photo upload + duplicate email check
- Password policy (12+ chars, upper/lower/digit/special) + hashing
- Credit card encryption at rest (ASP.NET Core Data Protection)
- Session auth + session timeout + multi-login detection (SessionStamp)
- reCAPTCHA v3 (Login) server-verified with score threshold
- 2FA via Email OTP (Login -> VerifyOtp)
- Max password age gate (after OTP) -> forces ChangePassword
- Change password w/ min age + password history (current + last 2 blocked)
- Forgot/Reset password via email link (token hashed + expiry)
- Audit logging via SecurityEvents table
- Custom 404/403/500 pages

## 1) Open in Visual Studio 2022
Open the folder or the .csproj.

## 2) Configure secrets
### reCAPTCHA
Set `GoogleReCaptcha:SiteKey` and `GoogleReCaptcha:SecretKey` in `appsettings.json`.

### Gmail SMTP App Password
Recommended: store SMTP AppPassword in User Secrets (VS: Right click project -> Manage User Secrets)

Example user-secrets JSON:
{
  "Smtp": { "AppPassword": "xxxx xxxx xxxx xxxx" }
}

Also set your SMTP username/from in appsettings.json.

## 3) Create database
Open Package Manager Console:
- Add-Migration InitialCreate
- Update-Database

(Ensure your SQL Server LocalDB is available and `DefaultConnection` points to the right database.)

## 4) Run
- Register a new account
- Sign in -> check email OTP -> dashboard
- Try forgot password -> check email link

## Notes
This app uses session-based authentication (not ASP.NET Identity) to match common school security assignments.
