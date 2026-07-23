-- ============================================================================
-- Re-stamp MasterData office-ownership codes: old RegProvCode -> Office.Code
-- ============================================================================
--
-- WHY
--   The agency office code is now Office.Code (e.g. "X003"), derived from the
--   tenant's linked office. Rows created under the previous rule carry the old
--   Office.RegProvCode (e.g. "003"), so they no longer match their agency's
--   ownership filter and read as "belonging to another agency". This rewrites
--   those stamps to the new code.
--
-- SAFETY
--   * Employees are re-stamped EXACTLY via their OfficeId foreign key.
--   * The 5 reference tables + the Offices ownership stamp are reverse-mapped by
--     matching the stamp against Office.RegProvCode. Of 131 seeded offices, 122
--     have Code == RegProvCode (no-op); only the 9 that differ (X000-X007, SARM)
--     actually change.
--   * Deterministic: the only shared RegProvCode is "SID" (offices SID and SARM).
--     Rows stamped "SID" are intentionally LEFT as "SID" (the office whose
--     Code == RegProvCode wins), because under the old rule SID- and SARM-owned
--     rows were both stamped "SID" and are already indistinguishable. SARM-owned
--     reference rows cannot be recovered — re-create them if SARM is ever a tenant.
--   * Idempotent: new codes (X0xx) match no RegProvCode, so re-running is a no-op.
--
-- USAGE
--   psql "<connection string>" -f scripts/restamp-office-codes.sql
--   Run ONCE per database. Review the row counts it prints (RAISE NOTICE) before
--   trusting the result. Wrapped in a transaction — nothing commits on error.
--
--   Alternative (dev phase): wiping + re-seeding the tenant's MasterData is
--   cleaner than this script and sidesteps the SARM edge case entirely.
-- ============================================================================

BEGIN;

-- 1) Employees — exact, via the office FK (handles SID/SARM correctly too). ----
UPDATE "masterdata"."EmployeeProfiles" e
SET    "OfficeCode" = o."Code"
FROM   "masterdata"."Offices" o
WHERE  e."OfficeId" = o."Id"
  AND  e."OfficeCode" IS DISTINCT FROM o."Code";

-- 2) Reference tables + Offices ownership stamp — reverse-map RegProvCode -> Code.
--    The `o."Code" <> o."RegProvCode"` clause limits work to the 9 differing
--    offices; the NOT EXISTS clause keeps "SID"-stamped rows on SID (see SAFETY).
DO $$
DECLARE
    tbl   text;
    tables text[] := ARRAY[
        'Categories', 'Departments', 'Positions',
        'Suppliers',  'UnitOfMeasures', 'Offices'
    ];
    n int;
BEGIN
    FOREACH tbl IN ARRAY tables LOOP
        EXECUTE format($f$
            UPDATE %1$I."%2$s" t
            SET    "OfficeCode" = o."Code"
            FROM   %1$I."Offices" o
            WHERE  t."OfficeCode" = o."RegProvCode"
              AND  o."Code" <> o."RegProvCode"
              AND  NOT EXISTS (
                     SELECT 1 FROM %1$I."Offices" x
                     WHERE  x."RegProvCode" = t."OfficeCode"
                       AND  x."Code" = x."RegProvCode"
                   )
        $f$, 'masterdata', tbl);
        GET DIAGNOSTICS n = ROW_COUNT;
        RAISE NOTICE 're-stamped %: % row(s)', tbl, n;
    END LOOP;
END $$;

-- 3) OPTIONAL — OrganizationProfile fallback code. Cosmetic for LINKED tenants
--    (the code is resolved from the linked office on read), but keeps the stored
--    fallback consistent for any tenant not yet linked to an office. Comment out
--    if you would rather leave stored profile codes untouched.
UPDATE "masterdata"."OrganizationProfiles" p
SET    "AnnexECode" = o."Code"
FROM   "masterdata"."Offices" o
WHERE  p."AnnexECode" = o."RegProvCode"
  AND  o."Code" <> o."RegProvCode"
  AND  NOT EXISTS (
         SELECT 1 FROM "masterdata"."Offices" x
         WHERE  x."RegProvCode" = p."AnnexECode"
           AND  x."Code" = x."RegProvCode"
       );

COMMIT;
