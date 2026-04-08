using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FSH.Modules.MasterData.Domain;
using FSH.Framework.Shared.Multitenancy;

namespace FSH.Modules.MasterData.Data;

internal sealed class MasterDataDbInitializer(
    ILogger<MasterDataDbInitializer> logger,
    MasterDataDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for master data module", context.TenantInfo?.Identifier);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        // Offices
        if (!await context.Offices.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var offices = new[]
            {
                Office.Create("OFF-HQ",     "Headquarters",          "Main corporate office — executive and administration"),
                Office.Create("OFF-OPS",    "Operations Center",     "Core operations, logistics, and dispatch"),
                Office.Create("OFF-WH1",    "Main Warehouse",        "Primary distribution and storage facility"),
                Office.Create("OFF-WH2",    "Satellite Warehouse",   "Secondary storage and regional fulfilment"),
                Office.Create("OFF-BR-N",   "North Branch",          "Northern regional sales and service office"),
                Office.Create("OFF-BR-S",   "South Branch",          "Southern regional sales and service office"),
                Office.Create("OFF-BR-E",   "East Branch",           "Eastern regional sales and service office"),
                Office.Create("OFF-BR-W",   "West Branch",           "Western regional sales and service office"),
                Office.Create("OFF-IT",     "IT Hub",                "Technology infrastructure and support center"),
                Office.Create("OFF-REMOTE", "Remote / Work-From-Home","Designated remote workforce location"),
            };
            await context.Offices.AddRangeAsync(offices, cancellationToken).ConfigureAwait(false);
        }

        // Departments
        if (!await context.Departments.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var departments = new[]
            {
                Department.Create("AGS", "Administrative & General Services","Administrative & General Services", "380"),
                Department.Create("FI", "Finance","Finance Department", "385"),
                Department.Create("QA", "Quality Assurance","Quality Assurance Department", "190"),
                Department.Create("BSM", "Buffer Stock Management","Buffer Stock Management Department", "180"),
                Department.Create("FA", "Facility Management","Facility Management Department", "170"),
                Department.Create("COA", "Commission on Audit","Commission on Audit ", "391"),
            }; 
            await context.Departments.AddRangeAsync(departments, cancellationToken).ConfigureAwait(false);
        }

        // Positions
        if (!await context.Positions.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var positions = new[]
            {
                // Executive
                Position.Create("POS-CEO",   "Chief Executive Officer",     "Top executive responsible for overall company strategy"),
                Position.Create("POS-CFO",   "Chief Financial Officer",     "Head of finance and financial planning"),
                Position.Create("POS-CTO",   "Chief Technology Officer",    "Head of technology and engineering"),
                Position.Create("POS-COO",   "Chief Operating Officer",     "Head of operations and process excellence"),
                // Management
                Position.Create("POS-DIR",   "Director",                    "Department or division director"),
                Position.Create("POS-MGR",   "Manager",                     "Team or unit manager"),
                Position.Create("POS-ASMGR", "Assistant Manager",           "Supports manager in team leadership"),
                Position.Create("POS-SUP",   "Supervisor",                  "Front-line supervisor overseeing daily tasks"),
                Position.Create("POS-TL",    "Team Leader",                 "Leads a small operational team"),
                // Professional / Technical
                Position.Create("POS-SR-ENG","Senior Engineer",             "Experienced engineer handling complex tasks"),
                Position.Create("POS-ENG",   "Engineer",                    "Mid-level engineer or technical specialist"),
                Position.Create("POS-JR-ENG","Junior Engineer",             "Entry-level engineering role"),
                Position.Create("POS-ANALYST","Analyst",                    "Business or data analyst"),
                Position.Create("POS-SPEC",  "Specialist",                  "Subject-matter specialist"),
                // Clerical / Support
                Position.Create("POS-SR-CLK","Senior Clerk",                "Senior administrative or clerical staff"),
                Position.Create("POS-CLK",   "Clerk",                       "General clerical and administrative support"),
                Position.Create("POS-ASST",  "Administrative Assistant",    "Provides administrative support to teams"),
                Position.Create("POS-RECEP", "Receptionist",                "Front-desk and visitor management"),
                // Operations
                Position.Create("POS-WH-STF","Warehouse Staff",             "Handles receiving, storing, and dispatching goods"),
                Position.Create("POS-DRIVER","Driver / Courier",            "Company vehicle operator and delivery personnel"),
            };
            await context.Positions.AddRangeAsync(positions, cancellationToken).ConfigureAwait(false);
        }

        // Units of Measure
        if (!await context.UnitOfMeasures.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var uoms = new[]
            {
                // Count
                UnitOfMeasure.Create("UOM-PCS",  "Piece",        "Individual countable item"),
                UnitOfMeasure.Create("UOM-PAK",  "Pack",         "Bundled group of items sold together"),
                UnitOfMeasure.Create("UOM-BOX",  "Box",          "Standard box or carton"),
                UnitOfMeasure.Create("UOM-CTN",  "Carton",       "Larger corrugated carton"),
                UnitOfMeasure.Create("UOM-PAL",  "Pallet",       "Full pallet load"),
                UnitOfMeasure.Create("UOM-RIM",  "Ream",         "500 sheets of paper"),
                UnitOfMeasure.Create("UOM-SET",  "Set",          "Matched set of items"),
                UnitOfMeasure.Create("UOM-PAIR", "Pair",         "Two matching items"),
                // Weight
                UnitOfMeasure.Create("UOM-KG",   "Kilogram",     "SI unit of mass — 1 000 g"),
                UnitOfMeasure.Create("UOM-G",    "Gram",         "SI unit of mass"),
                UnitOfMeasure.Create("UOM-MG",   "Milligram",    "One-thousandth of a gram"),
                UnitOfMeasure.Create("UOM-LB",   "Pound",        "Imperial unit of mass ≈ 453.6 g"),
                // Volume
                UnitOfMeasure.Create("UOM-L",    "Liter",        "SI unit of volume"),
                UnitOfMeasure.Create("UOM-ML",   "Milliliter",   "One-thousandth of a liter"),
                UnitOfMeasure.Create("UOM-GAL",  "Gallon",       "US liquid gallon ≈ 3.785 L"),
                // Length / Area
                UnitOfMeasure.Create("UOM-M",    "Meter",        "SI unit of length"),
                UnitOfMeasure.Create("UOM-CM",   "Centimeter",   "One-hundredth of a meter"),
                UnitOfMeasure.Create("UOM-MM",   "Millimeter",   "One-thousandth of a meter"),
                UnitOfMeasure.Create("UOM-FT",   "Foot",         "Imperial unit of length ≈ 30.48 cm"),
                UnitOfMeasure.Create("UOM-SQM",  "Square Meter", "Area measurement"),
                // Time / Service
                UnitOfMeasure.Create("UOM-HR",   "Hour",         "Unit of time for labor or service billing"),
                UnitOfMeasure.Create("UOM-DAY",  "Day",          "Calendar day for rental or service billing"),
                UnitOfMeasure.Create("UOM-MONTH","Month",        "Calendar month for subscription or lease billing"),
            };
            await context.UnitOfMeasures.AddRangeAsync(uoms, cancellationToken).ConfigureAwait(false);
        }

        // Suppliers
        if (!await context.Suppliers.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var suppliers = new[]
            {
                // Office & stationery
                Supplier.Create("SUP-001", "Acme Supplies Co.", "General office and janitorial supplies", "Alice Smith", "alice@acme-supplies.example", "+63-2-8800-1001", "1 Acme Way, Makati City"),
                Supplier.Create("SUP-002", "OfficePro Distributors", "Office supplies and consumables", "David Kim", "david@officepro.example", "+63-2-8800-1002", "8 Office Plaza, BGC, Taguig"),
                Supplier.Create("SUP-003", "PaperWorks Inc.", "Paper, printing, and stationery", "Mia Santos", "mia@paperworks.example", "+63-2-8800-1003", "22 Paper Ave, Quezon City"),

                // Food & beverages
                Supplier.Create("SUP-004", "Global Foods Corp.", "Dry goods and pantry staples", "Bob Jones", "bob@globalfoods.example", "+63-2-8800-1004", "12 Market St, Pasig City"),
                Supplier.Create("SUP-005", "FreshMart Trading", "Fresh produce and dairy", "Rachel Cruz", "rachel@freshmart.example", "+63-2-8800-1005", "55 Farm Rd, Caloocan City"),
                Supplier.Create("SUP-006", "BrewLine Beverages", "Coffee, tea, and bottled drinks", "Marco Tan", "marco@brewline.example", "+63-2-8800-1006", "30 Brew St, Mandaluyong City"),

                // Electronics & IT
                Supplier.Create("SUP-007", "TechSource Philippines", "Computers, peripherals, and accessories", "Carol Lee", "carol@techsource.example", "+63-2-8800-1007", "42 Tech Park, Eastwood, Quezon City"),
                Supplier.Create("SUP-008", "NetGear Solutions", "Networking equipment and cabling", "James Reyes", "james@netgear-solutions.example", "+63-2-8800-1008", "18 Network Blvd, Ortigas Center"),
                Supplier.Create("SUP-009", "PowerCell Energy", "Batteries, UPS, and power supplies", "Lena Uy", "lena@powercell.example", "+63-2-8800-1009", "7 Power Drive, Parañaque City"),

                // Furniture & fixtures
                Supplier.Create("SUP-010", "Furnishings Co.", "Office furniture and ergonomic chairs", "Eve Turner", "eve@furnishings.example", "+63-2-8800-1010", "99 Furniture Rd, Las Piñas City"),
                Supplier.Create("SUP-011", "ModSpace Interiors", "Modular workstations and storage", "Anton Flores", "anton@modspace.example", "+63-2-8800-1011", "61 Design Ave, Alabang, Muntinlupa"),

                // Cleaning & janitorial
                Supplier.Create("SUP-012", "CleanPro Janitorial", "Cleaning chemicals and janitorial equipment", "Nina Bautista", "nina@cleanpro.example", "+63-2-8800-1012", "14 Clean St, Valenzuela City"),
                Supplier.Create("SUP-013", "HygieneHub Supply", "Hygiene products and PPE", "Eric Villanueva", "eric@hygienehub.example", "+63-2-8800-1013", "35 Safety Lane, Marikina City"),

                // Medical & safety
                Supplier.Create("SUP-014", "MediStock Philippines", "First aid, medicines, and medical supplies", "Dr. Sofia Lim", "sofia@medistock.example", "+63-2-8800-1014", "10 Health Blvd, San Juan City"),
                Supplier.Create("SUP-015", "SafeGuard Trading", "Safety gear, fire extinguishers, and PPE", "Rico Mendoza", "rico@safeguard.example", "+63-2-8800-1015", "88 Safety Rd, Pasay City"),

                // Electrical & hardware
                Supplier.Create("SUP-016", "BrightWire Electrical", "Electrical supplies and lighting", "Donna Aguilar", "donna@brightwire.example", "+63-2-8800-1016", "5 Circuit Ave, Navotas City"),
                Supplier.Create("SUP-017", "IronClad Hardware", "Tools, hardware, and construction materials", "Roy Castillo", "roy@ironclad.example", "+63-2-8800-1017", "27 Hardware St, Malabon City"),

                // Printing & signage
                Supplier.Create("SUP-018", "PrintMaster Solutions", "Commercial printing and tarpaulin", "Grace Chua", "grace@printmaster.example", "+63-2-8800-1018", "3 Print Blvd, Cubao, Quezon City"),

                // Transport & logistics
                Supplier.Create("SUP-019", "SpeedLink Logistics", "Courier and freight services", "Dennis Ocampo", "dennis@speedlink.example", "+63-2-8800-1019", "200 Logistics Way, Bicutan, Taguig"),

                // Miscellaneous
                Supplier.Create("SUP-020", "AllSource Trading", "General merchandise and seasonal items", "Patricia Domingo", "patricia@allsource.example", "+63-2-8800-1020", "1 Trade Center, Binondo, Manila"),
            };
            await context.Suppliers.AddRangeAsync(suppliers, cancellationToken).ConfigureAwait(false);
        }

        // Categories
        if (!await context.Categories.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var categories = new[]
            {
                // Food & consumables
                Category.Create("CAT-BEV",   "Beverages",             "Water, coffee, tea, juices, and soft drinks"),
                Category.Create("CAT-FOOD",  "Food & Pantry",         "Dry goods, snacks, and pantry staples"),
                // Office supplies
                Category.Create("CAT-STAT",  "Stationery & Paper",    "Pens, paper, folders, and desk supplies"),
                Category.Create("CAT-PRINT", "Printing Supplies",     "Ink, toner, printing paper, and printer accessories"),
                Category.Create("CAT-FORM",  "Forms & Documents",     "Official forms, logbooks, and record books"),
                // Technology
                Category.Create("CAT-COMP",  "Computers & Laptops",   "Desktop PCs, laptops, and workstations"),
                Category.Create("CAT-PERIPH","Peripherals",           "Keyboards, mice, monitors, and input devices"),
                Category.Create("CAT-NET",   "Networking",            "Routers, switches, cables, and network equipment"),
                Category.Create("CAT-PHONE", "Phones & Comms",        "Mobile phones, desk phones, and communication devices"),
                Category.Create("CAT-MEDIA", "Storage Media",         "USB drives, memory cards, and external drives"),
                // Furniture & fixtures
                Category.Create("CAT-FURN",  "Furniture",             "Desks, tables, chairs, and shelving"),
                Category.Create("CAT-FIXTURE","Fixtures & Fittings",  "Cabinets, partitions, and built-in fixtures"),
                // Cleaning & hygiene
                Category.Create("CAT-CLEAN", "Cleaning Supplies",     "Detergents, mops, brooms, and sanitation products"),
                Category.Create("CAT-HYG",   "Hygiene & PPE",         "Hand sanitizer, gloves, masks, and protective equipment"),
                // Medical & safety
                Category.Create("CAT-MED",   "Medical Supplies",      "First aid kits, medicines, and health supplies"),
                Category.Create("CAT-SAFE",  "Safety Equipment",      "Fire extinguishers, safety signs, and protective gear"),
                // Electrical & hardware
                Category.Create("CAT-ELEC",  "Electrical Supplies",   "Cables, outlets, light bulbs, and electrical parts"),
                Category.Create("CAT-TOOLS", "Tools & Hardware",      "Hand tools, power tools, and hardware"),
                // Facilities & utilities
                Category.Create("CAT-FAC",   "Facilities & Utilities","Maintenance materials, plumbing, and facility supplies"),
                // Miscellaneous
                Category.Create("CAT-MISC",  "Miscellaneous",         "Items that do not fit other categories"),
            };
            await context.Categories.AddRangeAsync(categories, cancellationToken).ConfigureAwait(false);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // EmployeeProfiles — spread across offices, departments, and positions
        if (!await context.Employees.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var tenantId = context.TenantInfo?.Identifier ?? MultitenancyConstants.Root.Id;

            // Load lookup IDs by code for deterministic assignment
            var offices     = await context.Offices.ToDictionaryAsync(o => o.Code, o => o.Id, cancellationToken).ConfigureAwait(false);
            var departments = await context.Departments.ToDictionaryAsync(d => d.Code, d => d.Id, cancellationToken).ConfigureAwait(false);
            var positions   = await context.Positions.ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken).ConfigureAwait(false);

            Guid Off(string code)  => offices.TryGetValue(code, out var id)     ? id : offices.Values.First();
            Guid Dep(string code)  => departments.TryGetValue(code, out var id) ? id : departments.Values.First();
            Guid Pos(string code)  => positions.TryGetValue(code, out var id)   ? id : positions.Values.First();

            var employees = new[]
            {
                // Executive
                EmployeeProfile.Create(tenantId, "EMP-001", "Ricardo",   "Santos",    Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-CEO"),    null, "r.santos@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-002", "Maria",     "Reyes",     Off("OFF-HQ"),     Dep("FI"),   Pos("POS-CFO"),    null, "m.reyes@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-003", "Jonathan",  "Cruz",      Off("OFF-IT"),     Dep("AGS"),  Pos("POS-CTO"),    null, "j.cruz@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-004", "Angelica",  "Dela Cruz", Off("OFF-OPS"),    Dep("BSM"),  Pos("POS-COO"),    null, "a.delacruz@company.example"),
                // Finance
                EmployeeProfile.Create(tenantId, "EMP-005", "Patricia",  "Domingo",   Off("OFF-HQ"),     Dep("FI"),   Pos("POS-MGR"),    null, "p.domingo@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-006", "Edwin",     "Navarro",   Off("OFF-HQ"),     Dep("FI"),   Pos("POS-SR-CLK"), null, "e.navarro@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-007", "Sheila",    "Aquino",    Off("OFF-HQ"),     Dep("FI"),   Pos("POS-CLK"),    null, "s.aquino@company.example"),
                // Human Resources
                EmployeeProfile.Create(tenantId, "EMP-008", "Carla",     "Mendoza",   Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-MGR"),    null, "c.mendoza@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-009", "Dennis",    "Ocampo",    Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-SPEC"),   null, "d.ocampo@company.example"),
                // IT
                EmployeeProfile.Create(tenantId, "EMP-010", "Roel",      "Caperig",   Off("OFF-IT"),     Dep("AGS"),  Pos("POS-SR-ENG"), null, "admin@root.com"),
                EmployeeProfile.Create(tenantId, "EMP-011", "Nina",      "Bautista",  Off("OFF-IT"),     Dep("AGS"),  Pos("POS-ENG"),    null, "n.bautista@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-012", "Marco",     "Tan",       Off("OFF-IT"),     Dep("AGS"),  Pos("POS-JR-ENG"), null, "m.tan@company.example"),
                // Purchasing
                EmployeeProfile.Create(tenantId, "EMP-013", "Grace",     "Chua",      Off("OFF-HQ"),     Dep("BSM"),  Pos("POS-MGR"),    null, "g.chua@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-014", "Anton",     "Flores",    Off("OFF-HQ"),     Dep("BSM"),  Pos("POS-SPEC"),   null, "a.flores@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-015", "Lena",      "Uy",        Off("OFF-HQ"),     Dep("BSM"),  Pos("POS-CLK"),    null, "l.uy@company.example"),
                // Operations
                EmployeeProfile.Create(tenantId, "EMP-016", "Rico",      "Mendoza",   Off("OFF-OPS"),    Dep("BSM"),  Pos("POS-SUP"),    null, "ri.mendoza@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-017", "Donna",     "Aguilar",   Off("OFF-OPS"),    Dep("BSM"),  Pos("POS-ANALYST"),null, "d.aguilar@company.example"),
                // Warehouse
                EmployeeProfile.Create(tenantId, "EMP-018", "Roy",       "Castillo",  Off("OFF-WH1"),    Dep("BSM"),  Pos("POS-SUP"),    null, "r.castillo@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-019", "James",     "Reyes",     Off("OFF-WH1"),    Dep("BSM"),  Pos("POS-WH-STF"), null, "j.reyes@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-020", "Rachel",    "Cruz",      Off("OFF-WH2"),    Dep("BSM"),  Pos("POS-WH-STF"), null, "ra.cruz@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-021", "Eric",      "Villanueva",Off("OFF-WH1"),    Dep("BSM"),  Pos("POS-DRIVER"), null, "e.villanueva@company.example"),
                // Sales & Marketing
                EmployeeProfile.Create(tenantId, "EMP-022", "Sofia",     "Lim",       Off("OFF-BR-N"),   Dep("AGS"), Pos("POS-MGR"),    null, "s.lim@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-023", "Kevin",     "Fernandez", Off("OFF-BR-S"),   Dep("AGS"), Pos("POS-SPEC"),   null, "k.fernandez@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-024", "Hannah",    "Garcia",    Off("OFF-BR-E"),   Dep("AGS"), Pos("POS-SPEC"),   null, "h.garcia@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-025", "Luis",      "Torres",    Off("OFF-BR-W"),   Dep("AGS"), Pos("POS-CLK"),    null, "l.torres@company.example"),
                // Customer Service
                EmployeeProfile.Create(tenantId, "EMP-026", "Mia",       "Santos",    Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-TL"),     null, "mia.santos@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-027", "Paul",      "Ramos",     Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-ASST"),   null, "p.ramos@company.example"),
                // Admin
                EmployeeProfile.Create(tenantId, "EMP-028", "Jasmine",   "Pascual",   Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-ASST"),   null, "j.pascual@company.example"),
                EmployeeProfile.Create(tenantId, "EMP-029", "Carlo",     "Soriano",   Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-RECEP"),  null, "c.soriano@company.example"),
                // Legal
                EmployeeProfile.Create(tenantId, "EMP-030", "Veronica",  "Manalo",    Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-DIR"),    null, "v.manalo@company.example"),
            };

            await context.Employees.AddRangeAsync(employees, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Report Signatories
        if (!await context.ReportSignatories.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var tenantId = context.TenantInfo?.Identifier ?? MultitenancyConstants.Root.Id;

            var signatories = new[]
            {
                // Vehicle Inventory Report
                Domain.ReportSignatory.Create(tenantId, "VehicleInventory", 1, "PREPARED BY:", "Roel D. Caperig", "Procurement Mngt. Officer IV"),
                Domain.ReportSignatory.Create(tenantId, "VehicleInventory", 2, "CERTIFIED CORRECT:", "Hyde Beth M. Pascual", "Supervising Administrative Officer"),
                Domain.ReportSignatory.Create(tenantId, "VehicleInventory", 3, "NOTED:", "Leo V. Damole", "Regional Manager II"),

                // Physical Count Report
                Domain.ReportSignatory.Create(tenantId, "PhysicalCount", 1, "CERTIFIED CORRECT / MEMBER:", "Roel D. Caperig", "Procurement Mngt. Officer IV"),
                Domain.ReportSignatory.Create(tenantId, "PhysicalCount", 2, "MEMBER:", "", "SAO"),
                Domain.ReportSignatory.Create(tenantId, "PhysicalCount", 3, "APPROVED BY:", "", "Regional Manager II"),
                Domain.ReportSignatory.Create(tenantId, "PhysicalCount", 4, "MEMBER:", "", "Acting Accountant IV"),
                Domain.ReportSignatory.Create(tenantId, "PhysicalCount", 5, "CHAIRPERSON:", "", "Assistant Regional Manager II"),
                Domain.ReportSignatory.Create(tenantId, "PhysicalCount", 6, "VERIFIED BY:", "", "State Auditor IV / Audit Team Leader"),
            };

            await context.ReportSignatories.AddRangeAsync(signatories, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("[{Tenant}] seeded master data.", context.TenantInfo?.Identifier);
    }
}


