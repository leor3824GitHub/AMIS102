# Security Audit — AMIS .NET Starter Kit

**Date:** 2026-06-10
**Scope:** Identity/authentication → authorization → multi-tenancy isolation → database queries → configuration/secrets
**Status:** Findings only (no fixes applied)

---

## Summary

The architecture is fundamentally sound — parameterized LINQ/EF everywhere (no SQL injection surface), automatic tenant query filters, refresh-token rotation with session validation, ownership checks on session revoke (no IDOR), and a fallback authorization policy that requires authentication by default. The real problems are in **authentication hardening and secrets management**, not the data layer.

Priority order for remediation:
1. Rotate/replace committed secrets (#2)
2. Add account lockout (#1)
3. Fix user enumeration + add rate limits to reset endpoints (#3)

---

## High severity

### 1. No account lockout — unlimited password brute-force
- **Where:** [IdentityModule.cs:124-134](src/Modules/Identity/Modules.Identity/IdentityModule.cs#L124-L134), [IdentityService.cs:132-141](src/Modules/Identity/Modules.Identity/Services/IdentityService.cs#L132-L141), [appsettings.json:169-180](src/Host/AMIS.Api/appsettings.json#L169-L180)
- **Issue:** `AddIdentity` configures no `Lockout` options. Login validates through `UserManager.CheckPasswordAsync`, which never increments `AccessFailedCount` or honors lockout. The only throttle is the `auth` rate-limit policy — and `RateLimitingOptions.Enabled` is `false` in the default/dev config. Even with Production's `Enabled=true`, it's a 10/60s fixed window partitioned by IP/tenant — no per-account lockout.
- **Impact:** Unlimited password guessing per account.
- **Fix:** Use `SignInManager.CheckPasswordSignInAsync(..., lockoutOnFailure: true)` and configure `options.Lockout` (MaxFailedAccessAttempts, DefaultLockoutTimeSpan, AllowedForNewUsers).

### 2. Secrets committed to source control
- **Where:** [appsettings.json](src/Host/AMIS.Api/appsettings.json) (git-tracked), Production guard at [Program.cs:36-50](src/Host/AMIS.Api/Program.cs#L36-L50), key consumed at [ConfigureJwtBearerOptions.cs:40-55](src/Modules/Identity/Modules.Identity/Authorization/Jwt/ConfigureJwtBearerOptions.cs#L40-L55)
- **Issue:** `appsettings.json` contains live-looking secrets: DB `Password=password`, Hangfire `admin / Secure1234!Me`, SMTP `anderson22@ethereal.email / rqD44sq5P6U2UDCqD1`, and JWT `SigningKey: "replace-with-256-bit-secret-min-32-chars"`. The Production guard only checks the signing key is non-empty, not that it differs from the placeholder.
- **Impact:** If the placeholder symmetric key ever ships, **anyone can forge JWTs** = complete auth bypass.
- **Fix:** Move secrets to user-secrets/env/Key Vault, rotate everything exposed, reject the placeholder value at startup.

### 3. User enumeration + unthrottled password-reset abuse
- **Where:** [UserPasswordService.cs:30-34](src/Modules/Identity/Modules.Identity/Services/UserPasswordService.cs#L30-L34) (and `ResetPasswordAsync`), endpoint wiring at [IdentityModule.cs:204-214](src/Modules/Identity/Modules.Identity/IdentityModule.cs#L204-L214)
- **Issue:** `ForgotPasswordAsync` throws `NotFoundException("user not found")` (→404) when an email doesn't exist, versus 200 when it does — a clean enumeration oracle. `ResetPasswordAsync` does the same. Login also enumerates by timing (no dummy-hash on missing users). Only `token/issue`, `token/refresh`, and `confirm-email` get `.RequireRateLimiting("auth")` — `forgot-password`, `reset-password`, and `self-register` have **no rate limit**.
- **Impact:** Unlimited account enumeration and reset-email spam.
- **Fix:** Return 200 unconditionally on forgot-password; add `.RequireRateLimiting("auth")` to forgot-password, reset-password, self-register.

---

## Medium severity

### 4. Weak password policy
- **Where:** [IdentityModule.cs:127-130](src/Modules/Identity/Modules.Identity/IdentityModule.cs#L127-L130)
- **Issue:** Digit/lowercase/uppercase/non-alphanumeric requirements all disabled; only length 10 enforced, so `aaaaaaaaaa` is valid. Compounds #1.
- **Fix:** Enable complexity requirements or enforce a passphrase/zxcvbn-style strength check.

### 5. CSP allows scripts from any HTTPS origin, no HSTS
- **Where:** [SecurityHeadersMiddleware.cs:39-48](src/BuildingBlocks/Web/Security/SecurityHeadersMiddleware.cs#L39-L48)
- **Issue:** Emits `script-src 'self' https:`, permitting scripts from any HTTPS host — largely defeats CSP's XSS protection. No `Strict-Transport-Security` header set anywhere.
- **Fix:** Drop the bare `https:` source; add HSTS.

### 6. Permission handler enforces only the first permission
- **Where:** [RequiredPermissionAuthorizationHandler.cs:33](src/Modules/Identity/Modules.Identity/Authorization/RequiredPermissionAuthorizationHandler.cs#L33)
- **Issue:** Checks `requiredPermissions.First()` only. Because the handler succeeds when no permission metadata is present (relying on the fallback policy's `RequireAuthenticatedUser`), any endpoint that forgets `.RequirePermission()` silently downgrades to "any authenticated user from any tenant."
- **Fix:** Enforce all required permissions; consider an analyzer/test that fails the build on endpoints missing an explicit permission.

---

## Low / notes

### 7. SelfRegister endpoint has conflicting auth metadata
- **Where:** [SelfRegisterUserEndpoint.cs:29-31](src/Modules/Identity/Modules.Identity/Features/v1/Users/SelfRegistration/SelfRegisterUserEndpoint.cs#L29-L31)
- **Issue:** Declares both `.RequirePermission(Users.Create)` and `.AllowAnonymous()`. `AllowAnonymous` wins, so registration is fully public. If intended, remove the misleading `RequirePermission`; if not, it's an open-registration hole.

### 8. Hangfire dashboard basic auth
- **Where:** [HangfireCustomBasicAuthenticationFilter.cs:107-110](src/BuildingBlocks/Jobs/HangfireCustomBasicAuthenticationFilter.cs#L107-L110)
- **Issue:** Protected only by basic auth from config; `CredentialsMatch` uses non-constant-time `string.Equals` (minor timing leak). Dev creds are hardcoded (see #2).
- **Fix:** Use a constant-time comparison; source creds from secrets store.

### 9. Tenant resolution order — OK (documented for completeness)
- **Where:** [MultitenancyModule.cs:92-103](src/Modules/Multitenancy/Modules.Multitenancy/MultitenancyModule.cs#L92-L103)
- **Note:** Order is Claim → Header → `?tenant=` query. JWT tenant claim takes precedence, so an authenticated user can't override their tenant via header/query. **Correct — no action needed.**

### 10. Raw SQL — OK (documented for completeness)
- **Where:** [ExpendableDbInitializer.cs:28-31](src/Modules/Expendable/Modules.Expendable/Data/ExpendableDbInitializer.cs#L28-L31)
- **Note:** Only raw SQL in the codebase; uses static strings (no interpolation) — not injectable. **No action needed.**

---

## What's already solid (no action)

- Refresh-token flow rotates tokens, validates the session, and cross-checks the access-token subject ([RefreshTokenCommandHandler.cs](src/Modules/Identity/Modules.Identity/Features/v1/Tokens/RefreshToken/RefreshTokenCommandHandler.cs)).
- Session revoke rejects cross-user requests — no IDOR ([SessionService.cs:142-146](src/Modules/Identity/Modules.Identity/Services/SessionService.cs#L142-L146)).
- Tokens stored/logged only as hashes/fingerprints (never raw).
- `RequireHttpsMetadata = true` on JWT bearer.
- Tenant filters (`IsMultiTenant()`) applied consistently across modules.
- Fallback authorization policy requires authentication by default.
- All data access via EF/LINQ — parameterized, no SQL injection surface.

---

## Remediation checklist

- [x] #2 Placeholder JWT key rejected at startup (`JwtOptions.Validate` in all envs + Production guard also rejects `dev-only-*` keys); dev appsettings now uses a labeled dev-only key; committed SMTP creds scrubbed; CLI template now generates a per-project random dev key. **Remaining (ops):** rotate the exposed ethereal SMTP account; move prod secrets to env/Key Vault (prod already requires env override via empty `appsettings.Production.json` values).
- [x] #1 Account lockout enabled: `SignInManager.CheckPasswordSignInAsync(lockoutOnFailure: true)` + `Lockout` options (5 attempts / 15 min); dummy hash burned on unknown email to blunt timing enumeration.
- [x] #3 Forgot-password returns 200 unconditionally; reset-password returns the same error for unknown email as for a bad token; `forgot-password` (now actually mapped — it was never wired into `IdentityModule`), `reset-password`, and `self-register` all `.RequireRateLimiting("auth")`. **Note:** `RateLimitingOptions.Enabled` is still `false` in dev config; Production config enables it.
- [x] #4 Password complexity enabled (digit/lower/upper/non-alphanumeric + length 10).
- [x] #5 Bare `https:` removed from CSP `script-src` (use `SecurityHeadersOptions.ScriptSources` to allow-list); HSTS (`max-age=31536000; includeSubDomains`) emitted on HTTPS responses.
- [x] #6 Handler now enforces **all** declared permissions. **Remaining:** optional build-time guard for endpoints missing `.RequirePermission()`.
- [x] #7 Misleading `.RequirePermission()` removed from SelfRegister — endpoint is intentionally public (`AllowAnonymous`) and now rate-limited.
- [x] #8 Constant-time credential comparison (`CryptographicOperations.FixedTimeEquals`); plaintext password no longer logged on failed dashboard auth; malformed-header crash in `AreInvalid()` fixed. **Remaining (ops):** source Hangfire creds from a secret store in prod.
