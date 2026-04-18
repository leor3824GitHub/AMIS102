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
        // Offices — NFA Central Office departments (Location codes per NFA chart of accounts)
        if (!await context.Offices.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var offices = new[]
            {
                // ── Central Office — Code=short abbreviation, RegProvCode=abbreviation, LocationCode=NFA location code ─
                // Office.Create(code, name, description, regProvCode, locationCode)
                Office.Create("ASD",  "Accounting Services Department",                              null, "ASD",  "8300"),
                Office.Create("ACE",  "Agricultural Commodity Exchange",                             null, "ACE",  "9805"),
                Office.Create("BTF",  "Budget, Treasury and Fund Management Department",             null, "BTF",  "8400"),
                Office.Create("COA",  "Commission on Audit",                                         null, "COA",  "9802"),
                Office.Create("MSD",  "Corporate Planning Management Services Department",           null, "MSD",  "8000"),
                Office.Create("CDF",  "Corn Development Fund",                                       null, "CDF",  "9804"),
                Office.Create("OCS",  "Council Secretariat",                                         null, "OCS",  "7501"),
                Office.Create("FDC",  "Food Development Center",                                     null, "FDC",  "9100"),
                Office.Create("GAD",  "Gender and Development",                                      null, "GAD",  "8201"),
                Office.Create("GSD",  "General Services Department",                                 null, "GSD",  "8600"),
                Office.Create("DMO",  "Grains Marketing Operations Department",                      null, "DMO",  "8900"),
                Office.Create("PMD",  "GSD - Procurement Management Division",                       null, "PMD",  "8608"),
                Office.Create("HRS",  "Human Resource Management Department",                        null, "HRS",  "8500"),
                Office.Create("ISD",  "Industry Services Department",                                null, "ISD",  "8800"),
                Office.Create("IAS",  "Internal Audit Services",                                     null, "IAS",  "7600"),
                Office.Create("IRP",  "Irrigated Rice Production",                                   null, "IRP",  "9806"),
                Office.Create("LAD",  "Legal Affairs Department",                                    null, "LAD",  "7900"),
                Office.Create("EA",   "NFA Employers Association",                                   null, "EA",   "9803"),
                Office.Create("ADM",  "Office of the Administrator",                                 null, "ADM",  "7700"),
                Office.Create("AMO",  "Office of the Asst. Admin. for Marketing Operations",         null, "AMO",  "8702"),
                Office.Create("DAO",  "Office of the Deputy Admin. for Marketing Operations",        null, "DAO",  "8701"),
                Office.Create("DAF",  "Office of the Deputy Admin. for Finance and Administration",  null, "DAF",  "8200"),
                Office.Create("AFA",  "Office of the Administrator for Finance and Administration",  null, "AFA",  "8202"),
                Office.Create("PRV",  "Provident Fund",                                              null, "PRV",  "9801"),
                Office.Create("PAD",  "Public Affairs Department",                                   null, "PAD",  "7800"),
                Office.Create("SID",  "Security Services and Investigation Department",              null, "SID",  "8100"),
                Office.Create("SARM", "SSID Armory",                                                 null, "SID",  "8104"),
                Office.Create("STF",  "Staff House",                                                 null, "STF",  "8606"),
                Office.Create("TRS",  "Technical Research and Services Department",                  null, "TRS",  "9000"),

                // ── Regional/Provincial — Code=Reg/Prov code, LocationCode=4-digit NFA code ─
                // Region I — La Union
                Office.Create("100",  "La Union - Regional Office - Reg. I",                         null, "100",  "0100"),
                Office.Create("101",  "La Union - Provincial Office",                                null, "101",  "0101"),
                Office.Create("106",  "Abra",                                                        null, "106",  "0102"),
                Office.Create("104",  "Ilocos Sur - Vigan",                                          null, "104",  "0103"),
                Office.Create("105",  "Ilocos Norte - Laoag",                                        null, "105",  "0104"),
                Office.Create("103",  "Benguet - Baguio",                                            null, "103",  "0105"),
                Office.Create("102",  "Eastern Pangasinan - Binalonan",                              null, "102",  "0106"),
                Office.Create("107",  "Western Pangasinan - Lingayen",                               null, "107",  "0107"),

                // Region II — Isabela
                Office.Create("200",  "Isabela - Regional Office - Reg. II",                         null, "200",  "0200"),
                Office.Create("201",  "Isabela - Provincial Office",                                 null, "201",  "0201"),
                Office.Create("205",  "Quirino",                                                     null, "205",  "0202"),
                Office.Create("204",  "Ifugao - Lagawe",                                             null, "204",  "0203"),
                Office.Create("203",  "Mountain Province - Bontoc",                                  null, "203",  "0204"),
                Office.Create("206",  "Nueva Vizcaya - Bayombong",                                   null, "206",  "0205"),
                Office.Create("202",  "Kalinga Apayao - Tabuk",                                      null, "202",  "0206"),
                Office.Create("208",  "Cagayan - Tuguegarao",                                        null, "208",  "0207"),
                Office.Create("207",  "North Western Cagayan Apayao - Allacapan",                    null, "207",  "0208"),

                // Region III — Cabanatuan City
                Office.Create("300",  "Cabanatuan City - Regional Office - Reg. III",                null, "300",  "0300"),
                Office.Create("301",  "Nueva Ecija - Provincial Office",                             null, "301",  "0301"),
                Office.Create("306",  "Bulacan",                                                     null, "306",  "0302"),
                Office.Create("304",  "Bataan",                                                      null, "304",  "0303"),
                Office.Create("302",  "Pampanga",                                                    null, "302",  "0304"),
                Office.Create("303",  "Tarlac",                                                      null, "303",  "0305"),
                Office.Create("307",  "Zambales",                                                    null, "307",  "0306"),
                Office.Create("305",  "Aurora",                                                      null, "305",  "0307"),

                // Region IV — Batangas City
                Office.Create("400",  "Batangas City - Regional Office - Reg. IV",                   null, "400",  "0400"),
                Office.Create("401",  "Batangas - Provincial Office",                                null, "401",  "0401"),
                Office.Create("402",  "Infanta",                                                     null, "402",  "0402"),
                Office.Create("409",  "Laguna",                                                      null, "409",  "0403"),
                Office.Create("410",  "Mamburao",                                                    null, "410",  "0404"),
                Office.Create("404",  "Marinduque",                                                  null, "404",  "0405"),
                Office.Create("405",  "Occidental Mindoro",                                          null, "405",  "0406"),
                Office.Create("406",  "Oriental Mindoro",                                            null, "406",  "0407"),
                Office.Create("408",  "Palawan",                                                     null, "408",  "0408"),
                Office.Create("403",  "Quezon - Lucena",                                             null, "403",  "0409"),
                Office.Create("407",  "Romblon",                                                     null, "407",  "0410"),

                // Region V — Legazpi City
                Office.Create("500",  "Legazpi City - Regional Office - Reg. V",                     null, "500",  "0500"),
                Office.Create("501",  "Albay - Provincial Office",                                   null, "501",  "0501"),
                Office.Create("503",  "Camarines Norte - Daet",                                      null, "503",  "0502"),
                Office.Create("502",  "Camarines Sur - Naga",                                        null, "502",  "0503"),
                Office.Create("506",  "Catanduanes - Virac",                                         null, "506",  "0504"),
                Office.Create("505",  "Masbate",                                                     null, "505",  "0505"),
                Office.Create("504",  "Sorsogon",                                                    null, "504",  "0506"),

                // Region VI — Iloilo
                Office.Create("600",  "Iloilo - Regional Office - Reg. VI",                          null, "600",  "0600"),
                Office.Create("601",  "Iloilo - Provincial Office",                                  null, "601",  "0601"),
                Office.Create("604",  "Antique",                                                     null, "604",  "0602"),
                Office.Create("603",  "Aklan",                                                       null, "603",  "0603"),
                Office.Create("605",  "Negros Occidental - Bacolod",                                 null, "605",  "0604"),
                Office.Create("602",  "Capiz - Roxas",                                               null, "602",  "0605"),

                // Region VII — Cebu
                Office.Create("700",  "Cebu - Regional Office - Reg. VII",                           null, "700",  "0700"),
                Office.Create("701",  "Cebu - Provincial Office",                                    null, "701",  "0701"),
                Office.Create("703",  "Bohol",                                                       null, "703",  "0702"),
                Office.Create("704",  "Negros Oriental - Dumaguete",                                 null, "704",  "0703"),
                Office.Create("702",  "Siquijor",                                                    null, "702",  "0704"),

                // Region VIII — Northern Leyte
                Office.Create("800",  "Northern Leyte - Regional Office - Reg. VIII",                null, "800",  "0800"),
                Office.Create("801",  "Northern Leyte - Provincial Office",                          null, "801",  "0801"),
                Office.Create("803",  "Eastern Samar - Borongan",                                    null, "803",  "0802"),
                Office.Create("802",  "Northern Samar - Catarman",                                   null, "802",  "0803"),
                Office.Create("805",  "Western Samar - Catbalogan",                                  null, "805",  "0804"),
                Office.Create("804",  "Southern Leyte - Maasin",                                     null, "804",  "0805"),
                Office.Create("806",  "Naval - Biliran",                                             null, "806",  "0806"),

                // Region IX — Zamboanga City
                Office.Create("900",  "Zamboanga City - Regional Office - Reg. IX",                  null, "900",  "0900"),
                Office.Create("901",  "Zamboanga - Provincial Office",                               null, "901",  "0901"),
                Office.Create("905",  "Dipolog - Zamboanga del Norte",                               null, "905",  "0902"),
                Office.Create("902",  "Pagadian City - Zamboanga del Sur",                           null, "902",  "0903"),
                Office.Create("908",  "Ipil - Malangas",                                             null, "908",  "0904"),

                // Region X — Cagayan de Oro
                Office.Create("X000", "Cagayan de Oro - Regional Office - Reg. X",                   null, "000",  "1000"),
                Office.Create("X001", "Misamis Oriental - Provincial Office",                        null, "001",  "1001"),
                Office.Create("X007", "Bukidnon",                                                    null, "007",  "1002"),
                Office.Create("X006", "Camiguin",                                                    null, "006",  "1003"),
                Office.Create("906",  "Misamis Occidental - Ozamiz",                                 null, "906",  "1004"),
                Office.Create("125",  "Lanao del Norte - Iligan",                                    null, "125",  "1005"),

                // Region XI — General Santos City
                Office.Create("1100", "General Santos City - Regional Office - Reg. XI",             null, "1100", "1100"),
                Office.Create("111",  "General Santos City - Provincial Office",                     null, "111",  "1101"),
                Office.Create("114",  "Digos - Davao del Sur",                                       null, "114",  "1102"),
                Office.Create("112",  "Davao City",                                                  null, "112",  "1103"),
                Office.Create("115",  "Davao Oriental - Mati",                                       null, "115",  "1104"),
                Office.Create("113",  "Davao del Norte - Tagum",                                     null, "113",  "1105"),
                Office.Create("116",  "Compostela Valley",                                           null, "116",  "1106"),

                // Region XII — Tacurong Sultan Kudarat
                Office.Create("120",  "Tacurong Sultan Kudarat - Regional Office - Reg. XII",        null, "120",  "1200"),
                Office.Create("123",  "Isulan - Provincial Office",                                  null, "123",  "1201"),
                Office.Create("122",  "North Cotabato - Kidapawan",                                  null, "122",  "1202"),
                Office.Create("121",  "South Cotabato - Marbel",                                     null, "121",  "1203"),

                // Region XIII — Metro Manila
                Office.Create("1300", "Metro Manila Office - Regional Office - Reg. XIII",           null, "1300", "1300"),
                Office.Create("NDO",  "North District Office - Valenzuela",                          null, "NDO",  "1301"),
                Office.Create("CDO",  "Central District Office",                                     null, "CDO",  "1302"),
                Office.Create("SDO",  "South District Office - FTI",                                 null, "SDO",  "1303"),
                Office.Create("CVT",  "Cavite",                                                      null, "CVT",  "1304"),
                Office.Create("EDO",  "East District Office - Marikina",                             null, "EDO",  "1305"),
                Office.Create("MTO",  "Metro Transport Office",                                      null, "MTO",  "1306"),
                Office.Create("1309", "Batanes",                                                     null, "1309", "1309"),

                // Region XIV — Cotabato
                Office.Create("140",  "Cotabato - Regional Office - Reg. XIV",                       null, "140",  "1400"),
                Office.Create("141",  "Maguindanao",                                                 null, "141",  "1401"),
                Office.Create("144",  "Lanao del Sur - Marawi",                                      null, "144",  "1402"),
                Office.Create("142",  "Sulu - Jolo",                                                 null, "142",  "1403"),
                Office.Create("143",  "Tawi-Tawi",                                                   null, "143",  "1404"),
                Office.Create("1405", "Basilan",                                                     null, null,   "1405"),

                // Region XV — Caraga
                Office.Create("00B",  "Caraga - Regional Office - Reg. XV",                      null, "00B",  "1500"),
                Office.Create("X002", "Agusan del Norte",                                            null, "002",  "1501"),
                Office.Create("X003", "Agusan del Sur",                                              null, "003",  "1502"),
                Office.Create("X004", "Surigao del Norte",                                           null, "004",  "1503"),
                Office.Create("X005", "Surigao del Sur",                                             null, "005",  "1504"),
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
                Supplier.Create("SUP-001", "Acme Supplies Co.", "123-456-789-000", "NON-VAT", "General office and janitorial supplies", "Alice Smith", "alice@acme-supplies.example", "+63-2-8800-1001", "1 Acme Way, Makati City"),
                Supplier.Create("SUP-002", "OfficePro Distributors", "234-567-890-001", "NON-VAT", "Office supplies and consumables", "David Kim", "david@officepro.example", "+63-2-8800-1002", "8 Office Plaza, BGC, Taguig"),
                Supplier.Create("SUP-003", "PaperWorks Inc.", "345-678-901-002", "NON-VAT", "Paper, printing, and stationery", "Mia Santos", "mia@paperworks.example", "+63-2-8800-1003", "22 Paper Ave, Quezon City"),

                // Food & beverages
                Supplier.Create("SUP-004", "Global Foods Corp.", "456-789-012-003", "NON-VAT", "Dry goods and pantry staples", "Bob Jones", "bob@globalfoods.example", "+63-2-8800-1004", "12 Market St, Pasig City"),
                Supplier.Create("SUP-005", "FreshMart Trading", "567-890-123-004", "NON-VAT", "Fresh produce and dairy", "Rachel Cruz", "rachel@freshmart.example", "+63-2-8800-1005", "55 Farm Rd, Caloocan City"),
                Supplier.Create("SUP-006", "BrewLine Beverages", "678-901-234-005", "NON-VAT", "Coffee, tea, and bottled drinks", "Marco Tan", "marco@brewline.example", "+63-2-8800-1006", "30 Brew St, Mandaluyong City"),

                // Electronics & IT
                Supplier.Create("SUP-007", "TechSource Philippines", "789-012-345-006", "NON-VAT", "Computers, peripherals, and accessories", "Carol Lee", "carol@techsource.example", "+63-2-8800-1007", "42 Tech Park, Eastwood, Quezon City"),
                Supplier.Create("SUP-008", "NetGear Solutions", "890-123-456-007", "NON-VAT", "Networking equipment and cabling", "James Reyes", "james@netgear-solutions.example", "+63-2-8800-1008", "18 Network Blvd, Ortigas Center"),
                Supplier.Create("SUP-009", "PowerCell Energy", "901-234-567-008", "NON-VAT", "Batteries, UPS, and power supplies", "Lena Uy", "lena@powercell.example", "+63-2-8800-1009", "7 Power Drive, Parañaque City"),

                // Furniture & fixtures
                Supplier.Create("SUP-010", "Furnishings Co.", "112-233-445-009", "NON-VAT", "Office furniture and ergonomic chairs", "Eve Turner", "eve@furnishings.example", "+63-2-8800-1010", "99 Furniture Rd, Las Piñas City"),
                Supplier.Create("SUP-011", "ModSpace Interiors", "223-344-556-010", "NON-VAT", "Modular workstations and storage", "Anton Flores", "anton@modspace.example", "+63-2-8800-1011", "61 Design Ave, Alabang, Muntinlupa"),

                // Cleaning & janitorial
                Supplier.Create("SUP-012", "CleanPro Janitorial", "334-455-667-011", "NON-VAT", "Cleaning chemicals and janitorial equipment", "Nina Bautista", "nina@cleanpro.example", "+63-2-8800-1012", "14 Clean St, Valenzuela City"),
                Supplier.Create("SUP-013", "HygieneHub Supply", "445-566-778-012", "NON-VAT", "Hygiene products and PPE", "Eric Villanueva", "eric@hygienehub.example", "+63-2-8800-1013", "35 Safety Lane, Marikina City"),

                // Medical & safety
                Supplier.Create("SUP-014", "MediStock Philippines", "556-677-889-013", "NON-VAT", "First aid, medicines, and medical supplies", "Dr. Sofia Lim", "sofia@medistock.example", "+63-2-8800-1014", "10 Health Blvd, San Juan City"),
                Supplier.Create("SUP-015", "SafeGuard Trading", "667-788-990-014", "NON-VAT", "Safety gear, fire extinguishers, and PPE", "Rico Mendoza", "rico@safeguard.example", "+63-2-8800-1015", "88 Safety Rd, Pasay City"),

                // Electrical & hardware
                Supplier.Create("SUP-016", "BrightWire Electrical", "778-899-101-015", "NON-VAT", "Electrical supplies and lighting", "Donna Aguilar", "donna@brightwire.example", "+63-2-8800-1016", "5 Circuit Ave, Navotas City"),
                Supplier.Create("SUP-017", "IronClad Hardware", "889-910-212-016", "NON-VAT", "Tools, hardware, and construction materials", "Roy Castillo", "roy@ironclad.example", "+63-2-8800-1017", "27 Hardware St, Malabon City"),

                // Printing & signage
                Supplier.Create("SUP-018", "PrintMaster Solutions", "990-121-323-017", "NON-VAT", "Commercial printing and tarpaulin", "Grace Chua", "grace@printmaster.example", "+63-2-8800-1018", "3 Print Blvd, Cubao, Quezon City"),

                // Transport & logistics
                Supplier.Create("SUP-019", "SpeedLink Logistics", "101-232-434-018", "NON-VAT", "Courier and freight services", "Dennis Ocampo", "dennis@speedlink.example", "+63-2-8800-1019", "200 Logistics Way, Bicutan, Taguig"),

                // Miscellaneous
                Supplier.Create("SUP-020", "AllSource Trading", "212-343-545-019", "NON-VAT", "General merchandise and seasonal items", "Patricia Domingo", "patricia@allsource.example", "+63-2-8800-1020", "1 Trade Center, Binondo, Manila"),
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

        // Capitalization Thresholds
        if (!await context.CapitalizationThresholds.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var threshold = Domain.CapitalizationThreshold.Create(
                circularName: "COA Circular No. 2022-004",
                description: "Guidelines on the implementation of Section 23 of the General Provisions of RA No. 11639 (FY 2022 GAA) relative to the increase in the capitalization threshold from P15,000.00 to P50,000.00. Tangible items below P50,000.00 shall be accounted as semi-expendable property; P50,000.00 and above shall be capitalized as PPE. Semi-expendable property is further classified as low-valued (≤ P5,000.00) and high-valued (> P5,000.00 but < P50,000.00).",
                capitalizationAmount: 50_000.00m,
                semiExpendableLowValueThreshold: 5_000.00m,
                effectivityDate: new DateOnly(2022, 6, 15));

            threshold.Activate();
            await context.CapitalizationThresholds.AddAsync(threshold, cancellationToken).ConfigureAwait(false);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // EmployeeProfiles — spread across offices, departments, and positions
        if (!await context.Employees.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
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
                EmployeeProfile.Create("EMP-001", "Ricardo",   "Santos",    Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-CEO"),    null, "r.santos@company.example"),
                EmployeeProfile.Create("EMP-002", "Maria",     "Reyes",     Off("OFF-HQ"),     Dep("FI"),   Pos("POS-CFO"),    null, "m.reyes@company.example"),
                EmployeeProfile.Create("EMP-003", "Jonathan",  "Cruz",      Off("OFF-IT"),     Dep("AGS"),  Pos("POS-CTO"),    null, "j.cruz@company.example"),
                EmployeeProfile.Create("EMP-004", "Angelica",  "Dela Cruz", Off("OFF-OPS"),    Dep("BSM"),  Pos("POS-COO"),    null, "a.delacruz@company.example"),
                // Finance
                EmployeeProfile.Create("EMP-005", "Patricia",  "Domingo",   Off("OFF-HQ"),     Dep("FI"),   Pos("POS-MGR"),    null, "p.domingo@company.example"),
                EmployeeProfile.Create("EMP-006", "Edwin",     "Navarro",   Off("OFF-HQ"),     Dep("FI"),   Pos("POS-SR-CLK"), null, "e.navarro@company.example"),
                EmployeeProfile.Create("EMP-007", "Sheila",    "Aquino",    Off("OFF-HQ"),     Dep("FI"),   Pos("POS-CLK"),    null, "s.aquino@company.example"),
                // Human Resources
                EmployeeProfile.Create("EMP-008", "Carla",     "Mendoza",   Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-MGR"),    null, "c.mendoza@company.example"),
                EmployeeProfile.Create("EMP-009", "Dennis",    "Ocampo",    Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-SPEC"),   null, "d.ocampo@company.example"),
                // IT
                EmployeeProfile.Create("EMP-010", "Roel",      "Caperig",   Off("OFF-IT"),     Dep("AGS"),  Pos("POS-SR-ENG"), null, "admin@root.com"),
                EmployeeProfile.Create("EMP-011", "Nina",      "Bautista",  Off("OFF-IT"),     Dep("AGS"),  Pos("POS-ENG"),    null, "n.bautista@company.example"),
                EmployeeProfile.Create("EMP-012", "Marco",     "Tan",       Off("OFF-IT"),     Dep("AGS"),  Pos("POS-JR-ENG"), null, "m.tan@company.example"),
                // Purchasing
                EmployeeProfile.Create("EMP-013", "Grace",     "Chua",      Off("OFF-HQ"),     Dep("BSM"),  Pos("POS-MGR"),    null, "g.chua@company.example"),
                EmployeeProfile.Create("EMP-014", "Anton",     "Flores",    Off("OFF-HQ"),     Dep("BSM"),  Pos("POS-SPEC"),   null, "a.flores@company.example"),
                EmployeeProfile.Create("EMP-015", "Lena",      "Uy",        Off("OFF-HQ"),     Dep("BSM"),  Pos("POS-CLK"),    null, "l.uy@company.example"),
                // Operations
                EmployeeProfile.Create("EMP-016", "Rico",      "Mendoza",   Off("OFF-OPS"),    Dep("BSM"),  Pos("POS-SUP"),    null, "ri.mendoza@company.example"),
                EmployeeProfile.Create("EMP-017", "Donna",     "Aguilar",   Off("OFF-OPS"),    Dep("BSM"),  Pos("POS-ANALYST"),null, "d.aguilar@company.example"),
                // Warehouse
                EmployeeProfile.Create("EMP-018", "Roy",       "Castillo",  Off("OFF-WH1"),    Dep("BSM"),  Pos("POS-SUP"),    null, "r.castillo@company.example"),
                EmployeeProfile.Create("EMP-019", "James",     "Reyes",     Off("OFF-WH1"),    Dep("BSM"),  Pos("POS-WH-STF"), null, "j.reyes@company.example"),
                EmployeeProfile.Create("EMP-020", "Rachel",    "Cruz",      Off("OFF-WH2"),    Dep("BSM"),  Pos("POS-WH-STF"), null, "ra.cruz@company.example"),
                EmployeeProfile.Create("EMP-021", "Eric",      "Villanueva",Off("OFF-WH1"),    Dep("BSM"),  Pos("POS-DRIVER"), null, "e.villanueva@company.example"),
                // Sales & Marketing
                EmployeeProfile.Create("EMP-022", "Sofia",     "Lim",       Off("OFF-BR-N"),   Dep("AGS"), Pos("POS-MGR"),    null, "s.lim@company.example"),
                EmployeeProfile.Create("EMP-023", "Kevin",     "Fernandez", Off("OFF-BR-S"),   Dep("AGS"), Pos("POS-SPEC"),   null, "k.fernandez@company.example"),
                EmployeeProfile.Create("EMP-024", "Hannah",    "Garcia",    Off("OFF-BR-E"),   Dep("AGS"), Pos("POS-SPEC"),   null, "h.garcia@company.example"),
                EmployeeProfile.Create("EMP-025", "Luis",      "Torres",    Off("OFF-BR-W"),   Dep("AGS"), Pos("POS-CLK"),    null, "l.torres@company.example"),
                // Customer Service
                EmployeeProfile.Create("EMP-026", "Mia",       "Santos",    Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-TL"),     null, "mia.santos@company.example"),
                EmployeeProfile.Create("EMP-027", "Paul",      "Ramos",     Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-ASST"),   null, "p.ramos@company.example"),
                // Admin
                EmployeeProfile.Create("EMP-028", "Jasmine",   "Pascual",   Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-ASST"),   null, "j.pascual@company.example"),
                EmployeeProfile.Create("EMP-029", "Carlo",     "Soriano",   Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-RECEP"),  null, "c.soriano@company.example"),
                // Legal
                EmployeeProfile.Create("EMP-030", "Veronica",  "Manalo",    Off("OFF-HQ"),     Dep("AGS"),  Pos("POS-DIR"),    null, "v.manalo@company.example"),
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

        // Property Classes — NFA COA GAM Annex A (Account codes per NFA chart of accounts)
        if (!await context.PropertyClasses.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var seed = new (string Code, string Name, string? Desc, (string ItemCode, string Name)[] Items)[]
            {
                ("LL", "Land", "Account Code: 10601010",
                [
                    ("01", "Land"),
                ]),
                ("LI", "Land Improvements", "Account Code: 10602990",
                [
                    ("01", "Other Land Improvements"),
                ]),
                ("BS", "Building and Other Structures", "Account Code: 10604010",
                [
                    ("01", "Buildings"),
                ]),
                ("OS", "Other Structures", "Account Code: 10604990",
                [
                    ("01", "Warehouses"),
                    ("02", "Staff House"),
                    ("03", "Motorpool"),
                    ("04", "Truck Scale"),
                    ("05", "Power House"),
                    ("06", "Canteen"),
                    ("07", "Laboratory"),
                    ("08", "Guard House"),
                    ("09", "Training Center"),
                    ("10", "Storage/Stock Room"),
                    ("11", "Comfort Room"),
                ]),
                ("ME", "Machinery and Equipment", "Account Code: 10605010",
                [
                    ("FM", "Farm Machinery Equipment"),
                    ("WE", "Warehouse Equipment"),
                    ("PE", "Plant and Machinery Equipment"),
                ]),
                ("OE", "Office Equipment", "Account Code: 10605020",
                [
                    ("01", "Adding Machine"),
                    ("02", "Calculators"),
                    ("03", "Airconditioning Equipment"),
                    ("04", "Bundy Clock/Biometric Machine"),
                    ("05", "Shredding Machine"),
                    ("06", "Checkwriter/Currency Detector"),
                    ("07", "Electric Fan"),
                    ("08", "Mimeographing Machine"),
                    ("09", "Photocopying Machine"),
                    ("10", "Steel Filing Cabinet"),
                    ("11", "Steel Cabinets"),
                    ("12", "Steel Safety Vault"),
                    ("13", "Typewriter"),
                    ("14", "Binding Machine"),
                    ("15", "Photo Stenciling Machine"),
                    ("16", "Letter Scale"),
                    ("17", "Heavy Duty Puncher"),
                    ("18", "Hand Truck/Push Cart"),
                    ("19", "Kitchen Equipment"),
                ]),
                ("DP", "ICT Equipment", "Account Code: 10605030",
                [
                    ("01", "Personal Computers"),
                    ("02", "Printer"),
                    ("03", "Computer Peripheral"),
                    ("04", "Software Application"),
                    ("05", "Monitor"),
                    ("06", "Computer Accessories"),
                    ("07", "Scanner"),
                    ("08", "UPS"),
                    ("09", "AVR"),
                    ("10", "Access Point"),
                    ("11", "DVD/LCD Writer"),
                    ("12", "Speaker"),
                    ("13", "Servers"),
                ]),
                ("CM", "Communication Equipment", "Account Code: 10605070",
                [
                    ("01", "Radio Equipment"),
                    ("02", "Telephone"),
                    ("03", "Audio-Visual Equipment"),
                    ("04", "Projector"),
                    ("05", "Projector Screen"),
                    ("06", "Camera"),
                    ("07", "Power Bank"),
                    ("08", "Paging System"),
                    ("09", "Mobile/Fax"),
                ]),
                ("FR", "Disaster Response Equipment", "Account Code: 10605090",
                [
                    ("01", "Rescue Equipment"),
                    ("02", "Firefighting Equipment"),
                ]),
                ("MD", "Medical Equipment", "Account Code: 10605110",
                [
                    ("01", "Medical Equipment"),
                    ("02", "Dental Equipment"),
                    ("03", "Laboratory Equipment"),
                ]),
                ("SP", "Sports Equipment", "Account Code: 10605130",
                [
                    ("01", "Sports Equipment"),
                    ("02", "Table/Board Equipment"),
                    ("03", "Weight Plates"),
                ]),
                ("TS", "Technical Equipment", "Account Code: 10605140",
                [
                    ("01", "Drafting Equipment"),
                    ("02", "Tensile Tester"),
                    ("03", "Moisture Meters"),
                    ("04", "Lab Apparatus"),
                    ("05", "Hand Sheller"),
                    ("06", "Gas Mask/Gloves"),
                    ("07", "Safety Goggles"),
                    ("08", "Thermometer"),
                    ("09", "Bin Probe"),
                    ("10", "Protective Suit"),
                    ("11", "Engraver"),
                    ("12", "Pallet Truck"),
                ]),
                ("LT", "Motor Vehicles", "Account Code: 10606010",
                [
                    ("01", "Bus"),
                    ("02", "Truck"),
                    ("03", "AUV"),
                    ("04", "Jeep"),
                    ("05", "Trailers"),
                    ("06", "Cars"),
                    ("07", "Motorcycle"),
                    ("08", "Custom Vehicles"),
                ]),
                ("WC", "Watercrafts", "Account Code: 10606040",
                [
                    ("01", "Motorboat"),
                    ("02", "Engine"),
                    ("03", "Barge"),
                    ("04", "Amphibian"),
                ]),
                ("FF", "Furniture and Fixtures", "Account Code: 10607010",
                [
                    ("01", "Bed"),
                    ("02", "Table"),
                    ("03", "Bookshelves"),
                    ("04", "Frames"),
                    ("05", "Blinds"),
                    ("06", "Cart"),
                    ("07", "Rack"),
                    ("08", "Plant Box"),
                    ("09", "Cabinet"),
                    ("10", "Chair"),
                    ("11", "Sofa"),
                    ("12", "Signages"),
                    ("13", "Folding Stage"),
                    ("14", "Rostrum"),
                    ("15", "Mirror"),
                    ("16", "Elevator Guide"),
                    ("17", "Trashcan"),
                ]),
                ("OP", "Other PPE", "Account Code: 10698990",
                [
                    ("HT", "Handtools"),
                    ("SE", "Ordnance Equipment"),
                    ("LF", "Lightning Facilities"),
                    ("KF", "Store Equipment"),
                    ("TN", "Tent"),
                    ("PD", "Petroleum Dispenser"),
                    ("OP", "Cold Storage Room"),
                ]),
            };

            foreach (var cls in seed)
            {
                var propertyClass = PropertyClass.Create(cls.Code, cls.Name, cls.Desc);
                context.PropertyClasses.Add(propertyClass);

                foreach (var item in cls.Items)
                {
                    var classItem = PropertyClassItem.Create(
                        propertyClass.Id,
                        propertyClass.Code,
                        item.ItemCode,
                        item.Name,
                        null);
                    context.PropertyClassItems.Add(classItem);
                }
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("[{Tenant}] seeded master data.", context.TenantInfo?.Identifier);
    }

}


