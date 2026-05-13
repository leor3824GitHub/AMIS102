# Module2-Expendable Integration Checklist

## Pre-Integration Tasks

- [ ] Review module structure and implementation
- [ ] Read EXPENDABLE-MODULE-README.md for complete documentation
- [ ] Read EXPENDABLE-QUICKSTART.md for developer guide
- [ ] Review MODULE-IMPLEMENTATION-SUMMARY.md for detailed file listing

## Integration Steps

### Step 1: Add Projects to Solution
- [ ] Add `Modules.Expendable.Contracts.csproj` to solution
- [ ] Add `Modules.Expendable.csproj` to solution
- [ ] Verify build succeeds

```bash
dotnet sln add src/Modules/Expendable/Modules.Expendable.Contracts/Modules.Expendable.Contracts.csproj
dotnet sln add src/Modules/Expendable/Modules.Expendable/Modules.Expendable.csproj
dotnet build src/AMIS.Framework.slnx
```

### Step 2: Register Module in Application

#### In Playground.Api Program.cs:
```csharp
// Add this with other module registrations
builder.AddModule<ExpenableModule>();
```

- [ ] Module registration added
- [ ] Application builds successfully

### Step 3: Create Database Migration

```bash
cd src/Modules/Expendable/Modules.Expendable
dotnet ef migrations add AddExpenableModule -o Data/Migrations
```

- [ ] Migration file created
- [ ] Migration file includes all entities (Product, Purchase, SupplyRequest, Cart, Inventory)
- [ ] Migration includes OutboxMessage and InboxMessage tables

### Step 4: Update Database

```bash
dotnet ef database update --startup-project ../../Playground/Playground.Api
```

- [ ] Database updated successfully
- [ ] Expendable schema created
- [ ] All tables present in database:
  - [ ] Products
  - [ ] Purchases
  - [ ] SupplyRequests
  - [ ] EmployeeShoppingCarts
  - [ ] EmployeeInventory
  - [ ] InventoryBatches
  - [ ] InventoryConsumptions
  - [ ] OutboxMessages
  - [ ] InboxMessages

### Step 5: Verify Endpoints

Start application and test endpoints:

```bash
dotnet run --project src/Playground/Playground.Api
```

Test each endpoint group:

#### Products
- [ ] POST `/api/v1/expendable/products` - Create
- [ ] PUT `/api/v1/expendable/products/{id}` - Update
- [ ] POST `/api/v1/expendable/products/{id}/activate` - Activate
- [ ] POST `/api/v1/expendable/products/{id}/deactivate` - Deactivate
- [ ] GET `/api/v1/expendable/products/{id}` - Get
- [ ] GET `/api/v1/expendable/products` - Search

#### Purchases
- [ ] POST `/api/v1/expendable/purchases` - Create
- [ ] POST `/api/v1/expendable/purchases/{id}/submit` - Submit
- [ ] POST `/api/v1/expendable/purchases/{id}/approve` - Approve
- [ ] GET `/api/v1/expendable/purchases/{id}` - Get
- [ ] GET `/api/v1/expendable/purchases` - Search

#### Supply Requests
- [ ] POST `/api/v1/expendable/supply-requests` - Create
- [ ] POST `/api/v1/expendable/supply-requests/{id}/submit` - Submit
- [ ] POST `/api/v1/expendable/supply-requests/{id}/approve` - Approve
- [ ] GET `/api/v1/expendable/supply-requests/{id}` - Get
- [ ] GET `/api/v1/expendable/supply-requests` - Search

#### Shopping Cart
- [ ] POST `/api/v1/expendable/cart/get-or-create` - Get/Create
- [ ] POST `/api/v1/expendable/cart/{id}/add-item` - Add
- [ ] GET `/api/v1/expendable/cart/{id}` - Get
- [ ] POST `/api/v1/expendable/cart/{id}/convert-to-request` - Convert

### Step 6: Add Permissions

Implement in your authorization module:

```csharp
// Add these permissions to your permission seed/configuration
Permissions.Expendable.Products.View
Permissions.Expendable.Products.Create
Permissions.Expendable.Products.Update
Permissions.Expendable.Products.Delete
Permissions.Expendable.Products.Activate
Permissions.Expendable.Products.Deactivate

Permissions.Expendable.Purchases.View
Permissions.Expendable.Purchases.Create
Permissions.Expendable.Purchases.Update
Permissions.Expendable.Purchases.Delete
Permissions.Expendable.Purchases.Approve
Permissions.Expendable.Purchases.Receive

Permissions.Expendable.SupplyRequests.View
Permissions.Expendable.SupplyRequests.Create
Permissions.Expendable.SupplyRequests.Update
Permissions.Expendable.SupplyRequests.Delete
Permissions.Expendable.SupplyRequests.Approve
Permissions.Expendable.SupplyRequests.Reject

Permissions.Expendable.ShoppingCarts.View
Permissions.Expendable.ShoppingCarts.Create
Permissions.Expendable.ShoppingCarts.Edit
Permissions.Expendable.ShoppingCarts.Clear
Permissions.Expendable.ShoppingCarts.Convert

Permissions.Expendable.Inventory.View
Permissions.Expendable.Inventory.Receive
Permissions.Expendable.Inventory.Consume
Permissions.Expendable.Inventory.ViewReports
```

- [ ] All permissions configured
- [ ] Permissions assigned to appropriate roles
- [ ] Tested with different user roles

### Step 7: Add Feature Flags (Optional)

Configure in your feature management system:

```
Expendable:ProductManagement - true/false
Expendable:PurchaseOrders - true/false
Expendable:SupplyRequests - true/false
Expendable:ShoppingCart - true/false
Expendable:InventoryTracking - true/false
```

- [ ] Feature flags configured
- [ ] Feature access tested

### Step 8: Create Tests

Create `src/Tests/Expendable.Tests` folder with tests for:

- [ ] Domain entity tests
  - [ ] Product entity tests
  - [ ] Purchase entity tests
  - [ ] SupplyRequest entity tests
  - [ ] EmployeeShoppingCart entity tests
  - [ ] EmployeeInventory entity tests

- [ ] Handler tests
  - [ ] Command handler tests
  - [ ] Query handler tests
  - [ ] Validator tests

- [ ] API endpoint tests
  - [ ] Integration tests
  - [ ] Authorization tests
  - [ ] Multi-tenancy tests

- [ ] Database tests
  - [ ] Entity configuration tests
  - [ ] Soft delete filter tests
  - [ ] Query filter tests

### Step 9: Integration Tests

- [ ] Run full solution build
- [ ] Run all tests pass
- [ ] No warnings or errors
- [ ] Multi-tenant isolation verified

### Step 10: Documentation

- [ ] API documentation updated
- [ ] Permission documentation added
- [ ] Feature documentation added
- [ ] Deployment guide updated
- [ ] Release notes prepared

## Post-Integration Tasks

### Seed Data (Optional)

```bash
# Create seed data migration or use DbContext
dotnet ef migrations add SeedExpenableData
```

- [ ] Sample products created
- [ ] Sample suppliers created
- [ ] Sample users with permissions created

### Monitoring & Logging

- [ ] Logging configured for module
- [ ] Error handling tested
- [ ] Performance monitored
- [ ] Alerts configured (if applicable)

### Documentation Review

- [ ] All documentation reviewed and updated
- [ ] README.md in root references module
- [ ] Getting started guide includes module
- [ ] Architecture documentation updated
- [ ] API documentation updated

## Validation Checklist

### Code Quality
- [ ] No compilation warnings
- [ ] No StyleCop violations
- [ ] Code follows AMIS conventions
- [ ] All using statements correct
- [ ] Namespaces properly organized
- [ ] No TODO comments remaining (unless intentional)

### Functionality
- [ ] All CRUD operations work
- [ ] Pagination works
- [ ] Filters work
- [ ] Status transitions work
- [ ] Multi-tenancy works
- [ ] Soft deletes work
- [ ] Concurrency handling works
- [ ] Validation works

### Performance
- [ ] Queries use indexes
- [ ] No N+1 queries
- [ ] Pagination limits data transfer
- [ ] Response times acceptable (<500ms for list operations)

### Security
- [ ] Authorization working
- [ ] Input validation working
- [ ] SQL injection prevention
- [ ] Multi-tenant data isolation verified
- [ ] No sensitive data in logs

### Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] End-to-end tests pass
- [ ] Coverage acceptable (>80%)
- [ ] Edge cases tested

## Rollout Plan

### Phase 1: Development
- [ ] Complete integration
- [ ] Internal testing
- [ ] Documentation review

### Phase 2: QA
- [ ] QA testing
- [ ] User acceptance testing
- [ ] Performance testing
- [ ] Security testing

### Phase 3: Production
- [ ] Create production backup
- [ ] Run migrations
- [ ] Deploy code
- [ ] Verify endpoints
- [ ] Monitor performance

### Phase 4: Post-Deployment
- [ ] Monitor for errors
- [ ] Gather user feedback
- [ ] Document issues
- [ ] Plan improvements

## Common Issues & Resolutions

| Issue | Cause | Resolution |
|-------|-------|-----------|
| Build fails | Missing dependencies | Restore NuGet packages |
| Migration fails | Existing schema | Use `-Force` flag |
| Endpoints 404 | Module not registered | Check Program.cs |
| Authorization fails | Permissions not assigned | Add permissions to roles |
| Multi-tenancy issues | TenantId not set | Check context accessor |
| Concurrency errors | Version mismatch | Reload entity before update |

## Rollback Plan

If issues occur:

```bash
# Revert last migration
dotnet ef migrations remove

# Delete migration files
# Restore from backup
# Redeploy previous version
```

- [ ] Rollback procedure tested
- [ ] Backup procedures in place
- [ ] Communication plan ready

## Sign-Off

- [ ] Development Lead: _______________  Date: _______
- [ ] QA Lead: _______________  Date: _______
- [ ] Architecture Lead: _______________  Date: _______
- [ ] Product Owner: _______________  Date: _______

## Notes

```
Additional notes for integration team:

_________________________________________________________________________

_________________________________________________________________________

_________________________________________________________________________
```

---

**Integration Started**: _______________  
**Completion Date**: _______________  
**Issues Encountered**: _______________  
**Resolved By**: _______________  

For questions or issues during integration, refer to:
- EXPENDABLE-MODULE-README.md
- EXPENDABLE-QUICKSTART.md
- MODULE-IMPLEMENTATION-SUMMARY.md

