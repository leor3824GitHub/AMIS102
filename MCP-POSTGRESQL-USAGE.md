# MCP PostgreSQL Server Usage Guide

## Overview

This project uses the **Model Context Protocol (MCP) PostgreSQL Server** to provide structured access to the Docker PostgreSQL database. The MCP server acts as a bridge between Claude AI and the PostgreSQL database, enabling natural language queries and database inspections.

**Connection Details:**
- **URL:** `postgresql://postgres:password@localhost:5432/amis104`
- **Host:** localhost  
- **Port:** 5432
- **Database:** amis104
- **Username:** postgres
- **Password:** password

---

## Configuration

### VS Code Settings

The MCP PostgreSQL server is configured in `.vscode/settings.json`:

```json
{
  "modelcontextprotocol": {
    "postgres": {
      "command": "npx",
      "args": ["@modelcontextprotocol/server-postgres"],
      "env": {
        "DATABASE_URL": "postgresql://postgres:password@localhost:5432/amis104"
      }
    }
  }
}
```

### Prerequisites

Ensure the following are running:
- ✅ **Docker PostgreSQL container** (e.g., `amis-postgres` or Aspire-managed containers)
- ✅ **VS Code with MCP extension installed**
- ✅ **Node.js and npm** (to run the MCP server via npx)

---

## Database Schema

The database consists of **3 separate DbContexts** across multiple schemas:

### 1. Expendable Module (`expendable` schema)
**Inventory & Supply Chain Management**

Core Tables:
- `Products` - Product catalog with pricing and status
- `ProductInventory` - Central warehouse stock ledger
- `EmployeeInventory` - Individual employee stock tracking
- `InventoryBatches` - FIFO batch tracking (JSON-stored)
- `InventoryConsumptions` - Consumption audit trail
- `Purchases` - Purchase orders
- `PurchaseLineItems` - PO line items (JSON-stored)
- `SupplyRequests` - Employee supply requests
- `SupplyRequestItems` - Request line items (JSON-stored)
- `EmployeeShoppingCarts` - Shopping carts
- `CartItems` - Cart contents (JSON-stored)
- `OutboxMessages` - Event sourcing outbox
- `InboxMessages` - Event sourcing inbox

### 2. Audit Module (`audit` schema)
**Event Auditing & Logging**

- `AuditRecords` - Complete audit trail with timestamps, severity, user info, and JSON payload

### 3. Multitenancy Module (`tenant` schema)
**Tenant Management**

- `Tenants` - Tenant registry with connection strings
- `TenantThemes` - Tenant-specific UI themes
- `TenantProvisionings` - Tenant onboarding workflows
- `TenantProvisioningSteps` - Individual provisioning steps

---

## Common Operations

### 1. Query Products by Tenant

```sql
SELECT id, "Name", "SKU", "UnitPrice", "Status"
FROM expendable."Products"
WHERE "TenantId" = 'your-tenant-id'
ORDER BY "CreatedOnUtc" DESC;
```

### 2. View Inventory Levels

```sql
SELECT 
  p."Name",
  pi."QuantityAvailable",
  pi."QuantityReserved",
  pi."WarehouseLocationName"
FROM expendable."ProductInventory" pi
JOIN expendable."Products" p ON pi."ProductId" = p."Id"
WHERE pi."TenantId" = 'your-tenant-id'
AND pi."QuantityAvailable" < p."MinimumStockLevel";
```

### 3. Track Consumption History

```sql
SELECT 
  ic."ConsumptionDate",
  ic."EmployeeId",
  p."Name",
  ic."QuantityConsumed",
  ic."Reason"
FROM expendable."InventoryConsumptions" ic
JOIN expendable."Products" p ON ic."ProductId" = p."Id"
WHERE ic."TenantId" = 'your-tenant-id'
AND ic."ConsumptionDate" >= NOW() - INTERVAL '30 days'
ORDER BY ic."ConsumptionDate" DESC;
```

### 4. Check Purchase Order Status

```sql
SELECT 
  "PurchaseOrderNumber",
  "Status",
  "OrderDate",
  "ExpectedDeliveryDate",
  "DeliveryDate",
  "TotalAmount"
FROM expendable."Purchases"
WHERE "TenantId" = 'your-tenant-id'
AND "Status" != 5  -- Not canceled
ORDER BY "OrderDate" DESC;
```

### 5. Review Audit Records

```sql
SELECT 
  "OccurredAtUtc",
  "EventType",
  "UserId",
  "UserName",
  "Severity",
  "Source",
  "PayloadJson"
FROM audit."AuditRecords"
WHERE "TenantId" = 'your-tenant-id'
AND "OccurredAtUtc" >= NOW() - INTERVAL '24 hours'
ORDER BY "OccurredAtUtc" DESC
LIMIT 100;
```

### 6. List All Tenants

```sql
SELECT 
  "Id",
  "Identifier",
  "Name",
  "AdminEmail",
  "IsActive",
  "ValidUpto"
FROM tenant."Tenants"
ORDER BY "Identifier";
```

### 7. Monitor Tenant Provisioning

```sql
SELECT 
  tp."TenantId",
  tp."Status",
  tp."CurrentStep",
  tp."CreatedUtc",
  tp."StartedUtc",
  tp."CompletedUtc",
  COUNT(tps."Id") as "TotalSteps"
FROM tenant."TenantProvisionings" tp
LEFT JOIN tenant."TenantProvisioningSteps" tps ON tp."Id" = tps."ProvisioningId"
GROUP BY tp."Id", tp."TenantId", tp."Status", tp."CurrentStep", tp."CreatedUtc", tp."StartedUtc", tp."CompletedUtc"
ORDER BY tp."CreatedUtc" DESC;
```

---

## Using with Claude AI

### Example Prompts

**Access Schema via MCP:**
- "Use the MCP PostgreSQL connection to check the schema"
- "Query the database to list all products for tenant X"
- "Check for any inventory items below minimum stock level"
- "Show me the last 10 purchases for this tenant"
- "Review audit logs for the past week"

### Workflow

1. **Request Information**: Ask Claude to query the database
   ```
   "Check how many employees have items in their shopping carts"
   ```

2. **Claude Uses MCP**: The AI automatically constructs and executes the SQL query via the MCP server

3. **Receive Results**: Get structured data about your database state

4. **Make Decisions**: Use the data to inform business logic or troubleshooting

---

## Key Database Features

### Multi-Tenancy
- All tenant-aware tables include a `TenantId` column
- Queries should always filter by tenant to ensure data isolation
- Example: `WHERE "TenantId" = 'your-tenant-id'`

### Soft Deletes
- Entities can be marked as deleted without removing data
- Check: `WHERE "IsDeleted" = false`
- Columns: `IsDeleted`, `DeletedBy`, `DeletedOnUtc`

### Audit Trail
- Most tables include: `CreatedBy`, `CreatedOnUtc`, `LastModifiedBy`, `LastModifiedOnUtc`
- `AuditRecords` table provides comprehensive event logging
- Audit data includes severity levels and correlation tracking

### Concurrency Control
- `Version` column (byte array) enables optimistic locking
- Prevents lost updates in concurrent scenarios

### JSON Storage
- Collections stored as JSONB for flexibility:
  - `CartItems` in `EmployeeShoppingCarts`
  - `PurchaseLineItems` in `Purchases`
  - `SupplyRequestItems` in `SupplyRequests`
  - `InventoryBatches` in `EmployeeInventory`

### Event Sourcing
- `OutboxMessages` table for reliable event publishing
- `InboxMessages` table for idempotent event processing
- Ensures at-least-once delivery of domain events

---

## Status Enums

### Product Status
- `0` = Draft
- `1` = Active
- `2` = Discontinued

### Purchase Status
- `0` = Draft
- `1` = Pending
- `2` = Received
- `3` = Partially Received
- `4` = Rejected
- `5` = Canceled

### SupplyRequest Status
- `0` = Draft
- `1` = Submitted
- `2` = Approved
- `3` = Rejected
- `4` = Fulfilled
- `5` = Canceled

### Cart Status
- `0` = Active
- `1` = Converted to Request
- `2` = Abandoned

### Provisioning Status
- `0` = NotStarted
- `1` = InProgress
- `2` = Completed
- `3` = Failed

---

## Troubleshooting

### Connection Issues

**Problem**: "Cannot connect to PostgreSQL"

**Solution**:
1. Verify Docker container is running:
   ```powershell
   docker ps | grep postgres
   ```

2. Check connection string in `.vscode/settings.json`

3. Ensure `DATABASE_URL` environment variable is set correctly

### Query Performance

**Problem**: Slow queries on large datasets

**Solutions**:
- Always filter by `TenantId` first
- Use indexes (listed in schema documentation)
- Limit results: `LIMIT 1000`
- Check `EXPLAIN PLAN` for query optimization

### Data Consistency

**Problem**: Stale data or unexpected results

**Solutions**:
- Verify soft-delete filter: `WHERE "IsDeleted" = false`
- Check tenant isolation: `WHERE "TenantId" = 'correct-id'`
- Review audit logs for recent changes
- Check for pending outbox messages

---

## Best Practices

✅ **Always filter by TenantId** - Ensures data isolation in multi-tenant scenarios

✅ **Exclude soft-deleted records** - Add `AND "IsDeleted" = false` to queries

✅ **Use LIMIT on large queries** - Prevent excessive data transfer

✅ **Monitor Outbox/Inbox tables** - Ensure events are being processed

✅ **Review Audit logs regularly** - Track system changes and user actions

✅ **Check Version column for concurrency** - Handle optimistic locking conflicts

✅ **Validate JSON columns** - Use PostgreSQL `->` operator for querying nested data

---

## Example: Querying JSON Data

### Extract Cart Items

```sql
SELECT 
  cart."Id",
  jsonb_array_elements(cart."Items") ->> 'ProductId' as "ProductId",
  jsonb_array_elements(cart."Items") ->> 'Quantity' as "Quantity"
FROM expendable."EmployeeShoppingCarts" cart
WHERE cart."TenantId" = 'your-tenant-id'
AND cart."Status" = 0;
```

### Check Batch Tracking

```sql
SELECT 
  inv."EmployeeId",
  jsonb_array_elements(inv."Batches") ->> 'BatchNumber' as "BatchNumber",
  jsonb_array_elements(inv."Batches") ->> 'QuantityReceived' as "QuantityReceived"
FROM expendable."EmployeeInventory" inv
WHERE inv."TenantId" = 'your-tenant-id';
```

---

## Contact & Support

For issues or questions about:
- **MCP Server Setup**: Check VS Code MCP extension documentation
- **Database Schema**: Refer to migration files in `src/Host/Migrations.PostgreSQL`
- **Domain Logic**: Review module-specific code in `src/Modules/`

---

**Last Updated**: March 9, 2026  
**Database Version**: PostgreSQL 16+ (Alpine)  
**MCP Server**: @modelcontextprotocol/server-postgres
