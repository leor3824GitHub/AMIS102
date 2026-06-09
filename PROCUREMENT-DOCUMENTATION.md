# Procurement Planning & Acquisition Documentation

> Comprehensive guide to procurement domain overhaul, module flows, and domain fixes.

---

## Quick Links

- **[PROCUREMENTPLANNING-DOMAIN-OVERHAUL-PLAN.md](PROCUREMENTPLANNING-DOMAIN-OVERHAUL-PLAN.md)** — Strategic domain redesign
- **[PROCUREMENTPLANNING-MODULE-FLOW.md](PROCUREMENTPLANNING-MODULE-FLOW.md)** — Process flows and workflows
- **[PROCUREMENT_DOMAIN_FIX_PLAN.md](PROCUREMENT_DOMAIN_FIX_PLAN.md)** — Detailed implementation fixes and patterns

---

## Overview

The Procurement module documentation covers three key areas:

1. **Domain Overhaul** — Strategic redesign of procurement domain structures
2. **Module Flow** — End-to-end procurement workflows and process maps
3. **Domain Fixes** — Targeted technical improvements and pattern implementations

---

## Core Concepts

### Procurement Phases

The procurement lifecycle spans three main modules:

1. **ProcurementPlanning** — Annual planning and budget allocation
   - Prepare annual procurement program
   - Allocate budget to items/categories
   - Plan procurement activities

2. **ProcurementAcquisition** — Execution of procurement activities
   - Issue purchase requests (PR)
   - Generate request for quotation (RFQ)
   - Conduct canvass/bidding (AoC)
   - Issue purchase orders (PO)
   - Receive and inspect items (IAR)
   - Accept goods into inventory

3. **AssetManagement / Expendable** — Post-acquisition handling
   - Register fixed assets
   - Track consumable supplies
   - Manage accountability

---

## Key Workflows

### Asset Acquisition Flow

```
ProcurementPlanning          ProcurementAcquisition          AssetManagement/Expendable
────────────────            ──────────────────────          ─────────────────────────
Annual Plan ─────►          PR  ──►  RFQ  ──►  AoC  ──►  PO
															 │
											 ┌──────────────┴──────────────┐
											 ▼                             ▼
											IAR                     Inspection
											 │                             │
											 └──────────────┬──────────────┘
															▼
													Asset Registry / Inventory
```

### Finance Integration

Purchase Order connects to:
- **Budget Utilization Records (BUR)** — Encumbrance tracking
- **Disbursement Vouchers (DV)** — Actual payment recording
- **Payment-First Gate** — Optional requirement before goods receipt

---

## Domain Architecture

### Procurement Entities (High Level)

**ProcurementPlanning Domain:**
- AnnualProcurementPlan
- ProcurementItem
- BudgetAllocation
- HistoricalPriceData

**ProcurementAcquisition Domain:**
- PurchaseRequest
- RequestForQuotation
- CanvassOfPrice / Award of Contract
- PurchaseOrder
- AssetInspectionAcceptanceReport
- GoodsReceipt

**Finance Domain:**
- BudgetUtilizationRecord
- DisbursementVoucher

---

## Implementation Status

| Area | Status | Details |
|------|--------|---------|
| Domain Entities | ✅ Complete | All aggregates defined |
| Workflows | ✅ Mapped | End-to-end flows documented |
| API Endpoints | ✅ Implemented | RESTful service layer |
| Validations | ✅ Complete | Business rule enforcement |
| Tests | ✅ In Progress | Unit and integration tests |

---

## Best Practices

### Procurement Request Handling

1. **Initiate** — Submit PR with required details and attachment
2. **Validate** — System checks budget availability and completeness
3. **Approve** — Required signatories review and approve
4. **Canvass** — Issue RFQ to suppliers, collect quotations
5. **Award** — Evaluate bids, prepare AoC/Canvass result
6. **Order** — Issue PO linking to AoC and supplier award
7. **Receipt** — Receive goods, conduct inspection (IAR)
8. **Accept** — Inspector approves, custodian accepts
9. **Register** — Materialize asset or add to inventory

### Cross-Module Integration Points

- **Procurement ↔ Finance** — PO → BUR/DV; payment-first gate validation
- **Procurement ↔ AssetManagement** — IAR → AssetRegistry; event-driven integration
- **Procurement ↔ Expendable** — IAR → ProductInventory; supply request fulfillment

---

## Related Documentation

- **[ASSETMANAGEMENT-DOCUMENTATION.md](ASSETMANAGEMENT-DOCUMENTATION.md)** — Asset acquisition flow integration
- **[IAR-IMPLEMENTATION-GUIDE.md](IAR-IMPLEMENTATION-GUIDE.md)** — Inspection & acceptance details
- **[EXPENDABLE-DOCUMENTATION.md](EXPENDABLE-DOCUMENTATION.md)** — Consumable supplies integration
- **[CLAUDE.md](CLAUDE.md)** — Development patterns

---

## Support

For detailed information on any procurement topic:
- **Strategic Design** → [PROCUREMENTPLANNING-DOMAIN-OVERHAUL-PLAN.md](PROCUREMENTPLANNING-DOMAIN-OVERHAUL-PLAN.md)
- **Workflows** → [PROCUREMENTPLANNING-MODULE-FLOW.md](PROCUREMENTPLANNING-MODULE-FLOW.md)
- **Technical Implementation** → [PROCUREMENT_DOMAIN_FIX_PLAN.md](PROCUREMENT_DOMAIN_FIX_PLAN.md)

