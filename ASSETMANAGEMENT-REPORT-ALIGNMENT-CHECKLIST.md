# AssetManagement Report Alignment Checklist

## Scope

Cross-check current API report outputs against official form expectations for:

1. ICS (Inventory Custodian Slip) derived reports
2. PAR (Property Acknowledgement Receipt) related reports
3. SMIR (Semi-Expendable Issuance Record) related reports
4. PPEIR/PTR (PPE Issuance and Transfer) reports

## Current API Surfaces Reviewed

1. `GetRegSPIQuery` + handler
2. `GetRSPIQuery` + handler
3. `GetSPCQuery` contract
4. `GetPropertyHistoryQuery` contract + handler
5. `GetPTRQuery` contract

## Alignment Matrix

### RegSPI (Registry of Semi-Expendable Property Issued)

Status: Improved (employee display, totals, signatory projection, deterministic ordering, and ICS sections added)

Available fields:

1. ICS No
2. Date
3. Fund Cluster
4. Property No
5. Item Code / Item Name
6. Asset Type
7. Unit Cost
8. EUL
9. Expires On
10. ICS Status
11. Issued-From employee display fields (name, position, office)
12. Requested employee header fields (employee no, name, office, department, position)
13. Page and overall amount totals metadata
14. Signatory block projection (`SortOrder`, `Label`, `Name`, `Title`)
15. Section metadata grouped by ICS (`ICSNo`, `Date`, `FundCluster`, `LineCount`, `AmountTotal`)
16. Deterministic ordering for printable output (`Date`, `ICSNo`, `PropertyNo`)

Gaps to verify against required form columns:

1. Final print-layout ordering/parity of signatory rows and section blocks against approved template

### RSPI (Report of Semi-Expendable Property Issued)

Status: Improved (employee display, totals, signatory projection, deterministic ordering, and ICS sections added)

Available fields:

1. ICS No
2. ICS Date
3. ICS Status
4. Fund Cluster
5. Received By Employee Id
6. Received By employee display fields (name, position, office)
7. Issued From Employee Id
8. Issued From employee display fields (name, position, office)
9. Property No
10. Item Code / Item Name
11. Asset Type
12. Unit Cost
13. Expires On
14. Page and overall amount totals metadata
15. Signatory block projection (`SortOrder`, `Label`, `Name`, `Title`)
16. Section metadata grouped by ICS (`ICSNo`, `ICSDate`, `FundCluster`, `LineCount`, `AmountTotal`)
17. Deterministic ordering for printable output (`ICSDate`, `ICSNo`, `PropertyNo`)

Gaps to verify:

1. Printed section/signatory layout parity with approved RSPI template

### SPC (Semi-Expendable Property Card)

Status: Contract-level aligned, handler verification pending

Contract includes movement card essentials:

1. Date
2. Document Type/No
3. Quantity In / Quantity Out
4. Unit Cost
5. Running Balance
6. Remarks

Gaps to verify:

1. Exact movement event mapping priority and tie-break ordering
2. Running balance behavior under back-dated transactions

### Property History

Status: Aligned for lifecycle audit view

Current output includes:

1. Core item identity and threshold snapshot
2. Current custodian
3. Event timeline with source document trace

Gaps to verify:

1. Whether document-print parity requires additional per-event fields

### PTR (Derived from PPEIR)

Status: Improved (officer display projection added)

Available contract fields:

1. PTR No
2. Date
3. From/To accountable officer IDs
4. Transfer Type
5. Approved/Released/Received By IDs
6. Item lines (property number, description, amount, condition, reason)
7. Name/position/office display fields for From/To/Approved/Released/Received officers

Gaps to verify:

1. Address and organizational labels exactly as required by form
2. Final print-layout parity for signature block ordering

## Recommended Next Steps

1. Add regression tests for report query handlers covering expected ordering, filtering, and totals.
2. Validate each report endpoint against sample official form outputs and lock shape with approval snapshots.
3. Keep current registry/current-state sources for dashboards, while legal document reports remain sourced from immutable document tables.

## Verification Status (2026-05-09)

1. Completed: API/data-level cross-check for RegSPI, RSPI, and PTR fields/order/totals/signatories based on handler projections and regression tests.
2. Completed: Regression coverage now locks RSPI/RegSPI deterministic ordering, section totals, signatory projection, summary totals, and PTR officer projection/item ordering.
3. Pending external artifact: Final visual print-layout parity (exact section/signature row rendering) requires approved report templates/snapshots, which are not currently present in this repository.

## This Pass (Implemented)

1. RegSPI now includes form-ready employee display fields for report header and issued-from officer lines.
2. RSPI now includes fund cluster plus received-by / issued-from employee display fields.
3. Employee display fields are resolved through `GetEmployeeReferenceByIdQuery` from MasterData contracts and preserve original ID fields for compatibility.
4. RSPI and RegSPI now include additive totals metadata (`PageLineCount`, `PageAmountTotal`, `OverallAmountTotal`) for report summary rows.
5. PTR now includes additive officer display metadata (name, position, office) for printable signature/name blocks.
6. RSPI and RegSPI now include additive signatory-block projection (`Signatories`) sourced via `GetReportSignatoriesQuery` from MasterData contracts.
7. RSPI and RegSPI now include additive per-ICS section metadata (`Sections`) for grouped rendering.
8. RSPI and RegSPI line ordering is now deterministic for template rendering (`Date`/`ICSNo`/`PropertyNo`).
9. Added regression tests for RSPI/RegSPI query handlers validating deterministic ordering, section totals, signatory projection, and summary totals.
10. Added regression test for PTR query handler validating officer display projection and line ordering by item number.

## Evidence References

1. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/Reports/RegistryOfSPIssued/GetRegSPIQuery.cs`
2. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/Reports/RegistryOfSPIssued/GetRegSPIQueryHandler.cs`
3. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/Reports/ReportOfSPIssued/GetRSPIQuery.cs`
4. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/Reports/ReportOfSPIssued/GetRSPIQueryHandler.cs`
5. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/Reports/SemiExpendablePropertyCard/GetSPCQuery.cs`
6. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/Reports/PropertyHistory/GetPropertyHistoryQuery.cs`
7. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/PPEIssuanceReports/GetPTR/GetPTRQuery.cs`
8. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/PPEIssuanceReports/GetPTR/GetPTRQueryHandler.cs`
9. `src/Tests/AssetManagement.Tests/Handlers/Reports/ReportQueryHandlerAlignmentTests.cs`
