-- =====================================================================
-- ERP Multi-Tenant Master Seed (company isolation)
-- ---------------------------------------------------------------------
-- Seeds a COMPLETE, INDEPENDENT set of master/reference data for every
-- company in the @Tenants table, so each company is isolated: logging in
-- as a user scoped to company B sees only company B's data.
--
-- Design rules for company isolation:
--   * Every row carries the owning company's CompanyId.
--   * Internal primary keys (Guids) are generated FRESH per company via
--     NEWID() so two companies never collide on a Guid.
--   * Business codes that are unique PER COMPANY (Items, Accounts, UoM,
--     Warehouses, SO Types, Pricing, Territories, Bank Accounts, etc.)
--     are reused verbatim across companies because those tables already
--     have a (CompanyId, Code) unique index.
--   * The two codes that are GLOBALLY unique in the current schema
--     (ap.Vendors.VendorId, ar.Customers.CustomerId) are namespaced with
--     a per-company prefix (built from TenantCode) so they stay unique
--     globally while remaining stable/recognizable per tenant.
--   * Reference data without a CompanyId (PaymentTerms, FOBTerms,
--     ShippingMethods, platform.Currencies) is shared across companies
--     and seeded once.
--
-- Idempotent: every insert is guarded by an IF NOT EXISTS keyed on the
-- company-scoped business code, so re-running is safe.
--
-- Run:
--   sqlcmd -S localhost -U sa -P P@ssw0rd -C -d erp -b -i sql/seed/04-seed-tenants.sql
-- =====================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ---------------------------------------------------------------------
-- 0. TENANT REGISTRY
-- ---------------------------------------------------------------------
-- Add new companies here to grow the seed. US Operations already exists,
-- so it is simply picked up; EU/APAC are created if absent.
-- ---------------------------------------------------------------------
DECLARE @Tenants TABLE (
    Id          UNIQUEIDENTIFIER,
    TenantCode  NVARCHAR(10),   -- used to namespace globally-unique codes
    Name        NVARCHAR(120),
    LegalName   NVARCHAR(160),
    Currency    NVARCHAR(10),
    TaxId       NVARCHAR(50),
    Address     NVARCHAR(200)
);

INSERT @Tenants (Id, TenantCode, Name, LegalName, Currency, TaxId, Address) VALUES
    ('11111111-1111-1111-1111-111111111111', 'US',  'US Operations',    'ERP US Operations Inc.',     'USD', '12-3456789', '123 Main St, New York, NY'),
    (NEWID(),                                 'EU',  'EU Operations',    'ERP EU Operations GmbH',      'EUR', 'EU-9876543', 'Berlinstrasse 1, Berlin, DE'),
    (NEWID(),                                 'APAC','APAC Operations',  'ERP APAC Operations Pte Ltd', 'USD', 'APAC-1122',  '10 Marina Blvd, Singapore');

-- Ensure each tenant company row exists (idempotent by Name; reuse existing Id).
DECLARE @tId UNIQUEIDENTIFIER, @tCode NVARCHAR(10), @tName NVARCHAR(120),
        @tLegal NVARCHAR(160), @tCur NVARCHAR(10), @tTax NVARCHAR(50), @tAddr NVARCHAR(200);

DECLARE curT CURSOR FOR SELECT Id, TenantCode, Name, LegalName, Currency, TaxId, Address FROM @Tenants;
OPEN curT;
WHILE 1=1
BEGIN
    FETCH NEXT FROM curT INTO @tId, @tCode, @tName, @tLegal, @tCur, @tTax, @tAddr;
    IF @@FETCH_STATUS <> 0 BREAK;
    -- If a company with this name already exists (e.g. from a prior partial run),
    -- reuse its Id so downstream masters attach to the right company.
    IF EXISTS (SELECT 1 FROM platform.Companies WHERE Name = @tName)
        SET @tId = (SELECT Id FROM platform.Companies WHERE Name = @tName);
    ELSE
        INSERT INTO platform.Companies (Id, Name, LegalName, BaseCurrency, TaxId, Address, IsActive, CreatedBy, CreatedOn)
        VALUES (@tId, @tName, @tLegal, @tCur, @tTax, @tAddr, 1, 'seed', SYSDATETIMEOFFSET());

    -- Reflect the (possibly resolved) Id back into the tenant registry so the
    -- per-company seed loop uses the correct company id.
    UPDATE @Tenants SET Id = @tId WHERE Name = @tName;
END
CLOSE curT; DEALLOCATE curT;

-- =====================================================================
-- 1. SHARED REFERENCE DATA (no CompanyId -> seeded once, company-agnostic)
-- =====================================================================
DECLARE @ptNet30 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';
DECLARE @ptNet15 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000002';

IF NOT EXISTS (SELECT 1 FROM ap.PaymentTerms WHERE Id = @ptNet30)
BEGIN
    INSERT INTO ap.PaymentTerms (Id, Name, DueDays, DiscountDays, DiscountPercent, IsActive, CreatedBy, CreatedOn) VALUES
    (@ptNet30, 'Net 30', 30, 0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@ptNet15, 'Net 15', 15, 10, 2, 1, 'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM platform.Currencies WHERE Code = 'USD')
BEGIN
    INSERT INTO platform.Currencies (Id, Code, Name, Symbol, DecimalPlaces, IsActive, CreatedBy, CreatedOn) VALUES
    ('10000000-0000-0000-0000-000000000001', 'USD', 'US Dollar',       '$', 2, 1, 'seed', SYSDATETIMEOFFSET()),
    ('10000000-0000-0000-0000-000000000002', 'EUR', 'Euro',            '€', 2, 1, 'seed', SYSDATETIMEOFFSET()),
    ('10000000-0000-0000-0000-000000000003', 'GBP', 'British Pound',   '£', 2, 1, 'seed', SYSDATETIMEOFFSET()),
    ('10000000-0000-0000-0000-000000000004', 'CAD', 'Canadian Dollar', 'C$',2, 1, 'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM pur.FOBTerms WHERE Code = 'FOBORIGIN')
BEGIN
    INSERT INTO pur.FOBTerms (Id, Code, Description, FreightResponsibility, RiskTransferPoint, IsActive, CreatedBy, CreatedOn) VALUES
    ('52000000-0000-0000-0000-000000000001', 'FOBORIGIN', 'FOB Origin',      'Buyer',  'At origin dock',  1, 'seed', SYSDATETIMEOFFSET()),
    ('52000000-0000-0000-0000-000000000002', 'FOBDEST',   'FOB Destination', 'Seller', 'At destination',  1, 'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM pur.ShippingMethods WHERE Code = 'GROUND')
BEGIN
    INSERT INTO pur.ShippingMethods (Id, Code, Description, StandardLeadTimeDays, IsActive, CreatedBy, CreatedOn) VALUES
    ('53000000-0000-0000-0000-000000000001', 'GROUND', 'Ground',  3.0, 1, 'seed', SYSDATETIMEOFFSET()),
    ('53000000-0000-0000-0000-000000000002', 'AIR',    'Air',     1.0, 1, 'seed', SYSDATETIMEOFFSET());
END

-- =====================================================================
-- 2. PER-COMPANY MASTER SEED (the isolation core)
-- =====================================================================
DECLARE @cId UNIQUEIDENTIFIER, @cCode NVARCHAR(10), @cName NVARCHAR(120),
        @cCur NVARCHAR(10);

DECLARE curC CURSOR FOR
    SELECT Id, TenantCode, Name, Currency FROM @Tenants;
OPEN curC;
WHILE 1=1
BEGIN
    FETCH NEXT FROM curC INTO @cId, @cCode, @cName, @cCur;
    IF @@FETCH_STATUS <> 0 BREAK;

    DECLARE @co UNIQUEIDENTIFIER = @cId;
    DECLARE @prefix NVARCHAR(10) = @cCode;   -- namespace for globally-unique codes

    -- ---- Fresh, per-company Guid identity for every entity ----
    DECLARE @acctCash      UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctAR        UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctInventory UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctEquip     UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctAccDep    UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctAP        UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctAccrued   UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctPayroll   UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctRetained  UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctEquity    UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctRevenue   UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctCOGS      UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctSalaries  UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctRent      UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctUtil      UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctOtherExp  UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctSvcRev    UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctSalesTax  UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctGrni      UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctVariance  UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctScrap     UNIQUEIDENTIFIER = NEWID();
    DECLARE @acctInv1400   UNIQUEIDENTIFIER = NEWID();

    DECLARE @fyId      UNIQUEIDENTIFIER = NEWID();
    DECLARE @curUsd    UNIQUEIDENTIFIER = NEWID();
    DECLARE @taxStd    UNIQUEIDENTIFIER = NEWID();
    DECLARE @taxNone   UNIQUEIDENTIFIER = NEWID();
    DECLARE @taxRed    UNIQUEIDENTIFIER = NEWID();
    DECLARE @terr1     UNIQUEIDENTIFIER = NEWID();
    DECLARE @terr2     UNIQUEIDENTIFIER = NEWID();
    DECLARE @srep1     UNIQUEIDENTIFIER = NEWID();
    DECLARE @srep2     UNIQUEIDENTIFIER = NEWID();
    DECLARE @custAcme  UNIQUEIDENTIFIER = NEWID();
    DECLARE @custGlob  UNIQUEIDENTIFIER = NEWID();
    DECLARE @cust3     UNIQUEIDENTIFIER = NEWID();
    DECLARE @cust4     UNIQUEIDENTIFIER = NEWID();
    DECLARE @cust5     UNIQUEIDENTIFIER = NEWID();
    DECLARE @uomEa     UNIQUEIDENTIFIER = NEWID();
    DECLARE @uomLb     UNIQUEIDENTIFIER = NEWID();
    DECLARE @uomBox    UNIQUEIDENTIFIER = NEWID();
    DECLARE @uomCs     UNIQUEIDENTIFIER = NEWID();
    DECLARE @catProd   UNIQUEIDENTIFIER = NEWID();
    DECLARE @catComp   UNIQUEIDENTIFIER = NEWID();
    DECLARE @whMain    UNIQUEIDENTIFIER = NEWID();
    DECLARE @whWest    UNIQUEIDENTIFIER = NEWID();
    DECLARE @whEast    UNIQUEIDENTIFIER = NEWID();
    DECLARE @itemA     UNIQUEIDENTIFIER = NEWID();
    DECLARE @itemB     UNIQUEIDENTIFIER = NEWID();
    DECLARE @itemC     UNIQUEIDENTIFIER = NEWID();
    DECLARE @itemD     UNIQUEIDENTIFIER = NEWID();
    DECLARE @itemSvc   UNIQUEIDENTIFIER = NEWID();
    DECLARE @soStd     UNIQUEIDENTIFIER = NEWID();
    DECLARE @soSvc     UNIQUEIDENTIFIER = NEWID();
    DECLARE @prStd     UNIQUEIDENTIFIER = NEWID();
    DECLARE @prVol     UNIQUEIDENTIFIER = NEWID();
    DECLARE @wc1       UNIQUEIDENTIFIER = NEWID();
    DECLARE @wc2       UNIQUEIDENTIFIER = NEWID();
    DECLARE @bom1      UNIQUEIDENTIFIER = NEWID();
    DECLARE @bom2      UNIQUEIDENTIFIER = NEWID();
    DECLARE @proj1     UNIQUEIDENTIFIER = NEWID();
    DECLARE @proj2     UNIQUEIDENTIFIER = NEWID();
    DECLARE @payReg    UNIQUEIDENTIFIER = NEWID();
    DECLARE @payOT     UNIQUEIDENTIFIER = NEWID();
    DECLARE @emp1      UNIQUEIDENTIFIER = NEWID();
    DECLARE @emp2      UNIQUEIDENTIFIER = NEWID();
    DECLARE @emp3      UNIQUEIDENTIFIER = NEWID();
    DECLARE @ster1     UNIQUEIDENTIFIER = NEWID();
    DECLARE @ster2     UNIQUEIDENTIFIER = NEWID();

    -- ---- Chart of Accounts (reused codes; per-company unique already) ----
    IF NOT EXISTS (SELECT 1 FROM platform.Accounts WHERE CompanyId = @co AND AccountNumber = '1000')
    BEGIN
        INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedBy, CreatedOn) VALUES
        (@acctCash,      @co, '1000', 'Cash',                       0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctAR,        @co, '1200', 'Accounts Receivable',       0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctInventory, @co, '1400', 'Inventory Asset',           0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctEquip,     @co, '1500', 'Equipment',                 0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctAccDep,    @co, '1700', 'Accumulated Depreciation', 0, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctAP,        @co, '2000', 'Accounts Payable',         1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctAccrued,   @co, '2100', 'Accrued Expenses',         1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctPayroll,   @co, '2200', 'Payroll Liabilities',      1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctSalesTax,  @co, '2300', 'Sales Tax Payable',        1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctRetained,  @co, '3000', 'Retained Earnings',        2, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctEquity,    @co, '3100', 'Owner''s Equity',          2, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctRevenue,   @co, '4000', 'Revenue',                  3, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctSvcRev,    @co, '4100', 'Service Revenue',          3, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctCOGS,      @co, '5000', 'Cost of Goods Sold',       4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctSalaries,  @co, '6000', 'Salaries & Wages',         4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctRent,      @co, '6100', 'Rent',                     4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctUtil,      @co, '6200', 'Utilities',                4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctOtherExp,  @co, '7000', 'Other Expense',            4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctGrni,      @co, '2010', 'Goods Received Not Invoiced', 1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctVariance,  @co, '5900', 'Inventory Variance',       4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctScrap,     @co, '6900', 'Scrap & Obsolescence Loss', 4, 0, 1, 'seed', SYSDATETIMEOFFSET());
    END

    IF NOT EXISTS (SELECT 1 FROM gl.Account WHERE CompanyId = @co AND AccountNumber = '1000')
    BEGIN
        INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedBy, CreatedOn) VALUES
        (@acctCash,      @co, '1000', 'Cash',                       0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctAR,        @co, '1200', 'Accounts Receivable',       0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctInventory, @co, '1400', 'Inventory Asset',           0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctEquip,     @co, '1500', 'Equipment',                 0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctAccDep,    @co, '1700', 'Accumulated Depreciation', 0, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctAP,        @co, '2000', 'Accounts Payable',         1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctAccrued,   @co, '2100', 'Accrued Expenses',         1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctPayroll,   @co, '2200', 'Payroll Liabilities',      1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctSalesTax,  @co, '2300', 'Sales Tax Payable',        1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctRetained,  @co, '3000', 'Retained Earnings',        2, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctEquity,    @co, '3100', 'Owner''s Equity',          2, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctRevenue,   @co, '4000', 'Revenue',                  3, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctSvcRev,    @co, '4100', 'Service Revenue',          3, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctCOGS,      @co, '5000', 'Cost of Goods Sold',       4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctSalaries,  @co, '6000', 'Salaries & Wages',         4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctRent,      @co, '6100', 'Rent',                     4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctUtil,      @co, '6200', 'Utilities',                4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctOtherExp,  @co, '7000', 'Other Expense',            4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctGrni,      @co, '2010', 'Goods Received Not Invoiced', 1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctVariance,  @co, '5900', 'Inventory Variance',       4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@acctScrap,     @co, '6900', 'Scrap & Obsolescence Loss', 4, 0, 1, 'seed', SYSDATETIMEOFFSET());
    END

    -- ---- Fiscal Year + 12 monthly periods ----
    IF NOT EXISTS (SELECT 1 FROM platform.FiscalYears WHERE CompanyId = @co AND [Year] = 2026)
    BEGIN
        INSERT INTO platform.FiscalYears (Id, CompanyId, [Year], Description, StartDate, EndDate, IsClosed, CreatedBy, CreatedOn)
        VALUES (@fyId, @co, 2026, @cName + ' FY 2026', '2026-01-01T00:00:00Z', '2026-12-31T00:00:00Z', 0, 'seed', SYSDATETIMEOFFSET());

        INSERT INTO platform.FiscalPeriods (Id, FiscalYearId, CompanyId, PeriodNumber, Description, StartDate, EndDate, [Status], CreatedBy, CreatedOn)
        SELECT NEWID(), @fyId, @co, n, DATENAME(MONTH, DATEFROMPARTS(2026, n, 1)) + ' 2026',
               DATEFROMPARTS(2026, n, 1), EOMONTH(DATEFROMPARTS(2026, n, 1)), 0, 'seed', SYSDATETIMEOFFSET()
        FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12)) AS m(n);
    END

    -- ---- Exchange rates (per company) ----
    IF NOT EXISTS (SELECT 1 FROM platform.ExchangeRates WHERE CompanyId = @co)
    BEGIN
        INSERT INTO platform.ExchangeRates (Id, CompanyId, FromCurrency, ToCurrency, Rate, EffectiveDate, CreatedBy, CreatedOn) VALUES
        (NEWID(), @co, @tCur, 'EUR', 0.9200, '2026-01-01T00:00:00Z', 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @co, @tCur, 'GBP', 0.7900, '2026-01-01T00:00:00Z', 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @co, @tCur, 'CAD', 1.3600, '2026-01-01T00:00:00Z', 'seed', SYSDATETIMEOFFSET());
    END

    -- ---- AR: Tax codes, territories, sales reps, customers (VendorId/CustomerId namespaced) ----
    IF NOT EXISTS (SELECT 1 FROM om.TaxCodes WHERE CompanyId = @co AND Code = 'STAX')
    BEGIN
        INSERT INTO om.TaxCodes (Id, CompanyId, Code, Description, Jurisdiction, Rate, IsTaxable, IsActive, CreatedBy, CreatedOn) VALUES
        (@taxStd,  @co, 'STAX', 'Standard Sales Tax', 'US', 8.25, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (@taxNone, @co, 'NTAX', 'No Tax',            'US', 0.00, 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@taxRed,  @co, 'RTAX', 'Reduced Tax',       'US', 5.00, 1, 1, 'seed', SYSDATETIMEOFFSET());
    END
    -- Re-resolve ids to the actual rows (handle pre-existing rows seeded with different Guids).
    SET @taxStd  = (SELECT Id FROM om.TaxCodes WHERE CompanyId = @co AND Code = 'STAX');
    SET @taxNone = (SELECT Id FROM om.TaxCodes WHERE CompanyId = @co AND Code = 'NTAX');
    SET @taxRed  = (SELECT Id FROM om.TaxCodes WHERE CompanyId = @co AND Code = 'RTAX');

    IF NOT EXISTS (SELECT 1 FROM om.SalesTerritories WHERE CompanyId = @co AND Code = 'EAST')
    BEGIN
        INSERT INTO om.SalesTerritories (Id, CompanyId, Code, Name, Region, DefaultCommissionRate, IsActive, CreatedBy, CreatedOn) VALUES
        (@terr1, @co, 'EAST', 'East Region', 'East', 5.0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@terr2, @co, 'WEST', 'West Region', 'West', 5.0, 1, 'seed', SYSDATETIMEOFFSET());
    END

    IF NOT EXISTS (SELECT 1 FROM om.SalesReps WHERE CompanyId = @co AND Code = 'SR01')
    BEGIN
        INSERT INTO om.SalesReps (Id, CompanyId, Code, Name, CommissionRate, TerritoryId, IsActive, Email, CreatedBy, CreatedOn) VALUES
        (@srep1, @co, 'SR01', 'Dale Cooper',    6.0, @terr1, 1, 'dale@erp.com',  'seed', SYSDATETIMEOFFSET()),
        (@srep2, @co, 'SR02', 'Laura Palmer',   5.5, @terr2, 1, 'laura@erp.com','seed', SYSDATETIMEOFFSET());
    END
    SET @srep1 = (SELECT Id FROM om.SalesReps WHERE CompanyId = @co AND Code = 'SR01');
    SET @srep2 = (SELECT Id FROM om.SalesReps WHERE CompanyId = @co AND Code = 'SR02');

    -- CustomerId is now per-company unique -> reuse identical codes across companies.
    IF NOT EXISTS (SELECT 1 FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C1001')
    BEGIN
        INSERT INTO ar.Customers (Id, CompanyId, CustomerId, Name, LegalName, TaxId, CreditLimit, CreditHoldDays, DefaultPaymentTermId, TaxExempt, CurrencyCode, IsActive, SalesRepId, TaxCodeId, CreatedBy, CreatedOn) VALUES
        (@custAcme, @co, 'C1001', @cName + ' Acme Industries',   @cName + ' Acme Inc.',  '12-1111111', 100000, 0, @ptNet30, 0, @tCur, 1, @srep1, @taxStd, 'seed', SYSDATETIMEOFFSET()),
        (@custGlob, @co, 'C1002', @cName + ' Globex Corporation', @cName + ' Globex Corp', '12-2222222', 250000, 0, @ptNet30, 0, @tCur, 1, @srep1, @taxStd, 'seed', SYSDATETIMEOFFSET()),
        (@cust3,    @co, 'C1003', @cName + ' Initech LLC',        @cName + ' Initech LLC', '12-3333333', 50000,  30, @ptNet15, 0, @tCur, 1, @srep1, @taxStd, 'seed', SYSDATETIMEOFFSET()),
        (@cust4,    @co, 'C1004', @cName + ' Soylent Foods',      @cName + ' Soylent Inc',  '12-4444444', 75000,  0, @ptNet30, 0, @tCur, 1, @srep1, @taxStd, 'seed', SYSDATETIMEOFFSET()),
        (@cust5,    @co, 'C1005', @cName + ' Hooli',              @cName + ' Hooli Inc',    '12-5555555', 300000, 0, @ptNet30, 0, @tCur, 1, @srep1, @taxStd, 'seed', SYSDATETIMEOFFSET());
    END
    SET @custAcme = (SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C1001');
    SET @custGlob = (SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C1002');
    SET @cust3    = (SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C1003');
    SET @cust4    = (SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C1004');
    SET @cust5    = (SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C1005');

    -- ---- CASH: Bank accounts (per-company unique -> reuse codes) ----
    IF NOT EXISTS (SELECT 1 FROM cash.BankAccounts WHERE CompanyId = @co AND AccountCode = 'BANK-01')
    BEGIN
        INSERT INTO cash.BankAccounts (Id, CompanyId, AccountCode, AccountName, AccountNumber, RoutingNumber, BankName, CurrencyCode, AccountType, OpeningBalance, CurrentBalance, GlAccountId, [Status], CreatedBy, CreatedOn) VALUES
        (NEWID(), @co, 'BANK-01', 'Operating Checking', '00001111', '021000021', 'First National', @tCur, 0, 500000.00, 500000.00, @acctCash, 0, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @co, 'BANK-02', 'Payroll Checking',   '00002222', '021000021', 'First National', @tCur, 0, 100000.00, 100000.00, @acctCash, 0, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @co, 'BANK-03', 'Savings',            '00003333', '021000021', 'First National', @tCur, 1, 250000.00, 250000.00, @acctCash, 0, 'seed', SYSDATETIMEOFFSET());
    END

    -- ---- INVENTORY: UoM, categories, warehouses, items (codes reused; per-company unique) ----
    IF NOT EXISTS (SELECT 1 FROM inv.UnitOfMeasures WHERE CompanyId = @co AND Code = 'EA')
    BEGIN
        INSERT INTO inv.UnitOfMeasures (Id, CompanyId, Code, Description, BaseUOM, FactorToBase, IsActive, CreatedBy, CreatedOn) VALUES
        (@uomEa,  @co, 'EA',  'Each',      'EA', 1.0,  1, 'seed', SYSDATETIMEOFFSET()),
        (@uomLb,  @co, 'LB',  'Pound',     'LB', 1.0,  1, 'seed', SYSDATETIMEOFFSET()),
        (@uomBox, @co, 'BOX', 'Box (12 EA)','EA', 12.0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@uomCs,  @co, 'CS',  'Case (24 EA)','EA', 24.0, 1, 'seed', SYSDATETIMEOFFSET());
    END

    IF NOT EXISTS (SELECT 1 FROM inv.ItemCategories WHERE CompanyId = @co AND CategoryCode = 'PROD')
    BEGIN
        INSERT INTO inv.ItemCategories (Id, CategoryCode, CategoryName, CompanyId, InventoryAccountId, COGSAccountId, VarianceAccountId, Description, IsActive, CreatedBy, CreatedOn) VALUES
        (@catProd, 'PROD', 'Finished Goods', @co, @acctInventory, @acctCOGS, @acctOtherExp, 'Manufactured / sold products', 1, 'seed', SYSDATETIMEOFFSET()),
        (@catComp, 'COMP', 'Components',     @co, @acctInventory, @acctCOGS, @acctOtherExp, 'Purchased components',        1, 'seed', SYSDATETIMEOFFSET());
    END
    SET @catProd = (SELECT Id FROM inv.ItemCategories WHERE CompanyId = @co AND CategoryCode = 'PROD');
    SET @catComp = (SELECT Id FROM inv.ItemCategories WHERE CompanyId = @co AND CategoryCode = 'COMP');

    IF NOT EXISTS (SELECT 1 FROM inv.Warehouses WHERE CompanyId = @co AND WarehouseCode = 'WH-MAIN')
    BEGIN
        INSERT INTO inv.Warehouses (Id, WarehouseCode, WarehouseName, CompanyId, WarehouseType, Address, IsActive, CreatedBy, CreatedOn) VALUES
        (@whMain, 'WH-MAIN', 'Main Warehouse', @co, 0, '123 Logistics Way, Newark NJ',     1, 'seed', SYSDATETIMEOFFSET()),
        (@whWest, 'WH-WEST', 'West DC',        @co, 0, '900 Market St, Los Angeles CA',   1, 'seed', SYSDATETIMEOFFSET()),
        (@whEast, 'WH-EAST', 'East DC',        @co, 0, '5 Harbor Blvd, Boston MA',        1, 'seed', SYSDATETIMEOFFSET());
    END

    IF NOT EXISTS (SELECT 1 FROM inv.Items WHERE CompanyId = @co AND ItemCode = 'ITEM-1001')
    BEGIN
        INSERT INTO inv.Items (Id, ItemCode, Description, CompanyId, ItemType, BaseUnitOfMeasure, CostingMethod, ItemCategoryId, [Status], AllowNegativeInventory, IsLotControlled, IsSerialControlled, StandardCost, ReorderPoint, ReorderQuantity, SafetyStock, LeadTimeDays, IsKit, IsHazardousMaterial, CreatedBy, CreatedOn) VALUES
        (@itemA,  'ITEM-1001', 'Widget A',        @co, 0, 'EA', 0, @catProd, 0, 0, 0, 0, 50.00, 100, 500, 50, 7,  0, 0, 'seed', SYSDATETIMEOFFSET()),
        (@itemB,  'ITEM-1002', 'Widget B',        @co, 0, 'EA', 0, @catProd, 0, 0, 0, 0, 75.00, 80,  400, 40, 7,  0, 0, 'seed', SYSDATETIMEOFFSET()),
        (@itemC,  'ITEM-1003', 'Raw Component X', @co, 1, 'EA', 0, @catComp, 0, 0, 0, 0, 12.50, 200, 1000,100, 14, 0, 0, 'seed', SYSDATETIMEOFFSET()),
        (@itemD,  'ITEM-1004', 'Raw Component Y', @co, 1, 'EA', 0, @catComp, 0, 0, 0, 0, 18.00, 200, 1000,100, 14, 0, 0, 'seed', SYSDATETIMEOFFSET()),
        (@itemSvc,'ITEM-1005', 'Service Visit',   @co, 2, 'EA', 0, @catProd, 0, 0, 0, 0, 0.00,  0,   0,   0,  0,  0, 0, 'seed', SYSDATETIMEOFFSET());
    END
    SET @itemA   = (SELECT Id FROM inv.Items WHERE CompanyId = @co AND ItemCode = 'ITEM-1001');
    SET @itemB   = (SELECT Id FROM inv.Items WHERE CompanyId = @co AND ItemCode = 'ITEM-1002');
    SET @itemC   = (SELECT Id FROM inv.Items WHERE CompanyId = @co AND ItemCode = 'ITEM-1003');
    SET @itemD   = (SELECT Id FROM inv.Items WHERE CompanyId = @co AND ItemCode = 'ITEM-1004');
    SET @itemSvc = (SELECT Id FROM inv.Items WHERE CompanyId = @co AND ItemCode = 'ITEM-1005');

    -- ---- OM: Sales order types, pricing rules (codes reused; per-company unique) ----
    IF NOT EXISTS (SELECT 1 FROM om.SalesOrderTypes WHERE CompanyId = @co AND Code = 'SO-STD')
    BEGIN
        INSERT INTO om.SalesOrderTypes (Id, CompanyId, Code, Description, TypeCode, RevenueAccountId, IsActive, CreatedBy, CreatedOn) VALUES
        (@soStd, @co, 'SO-STD', 'Standard Sales Order', 0, @acctRevenue, 1, 'seed', SYSDATETIMEOFFSET()),
        (@soSvc, @co, 'SO-SVC', 'Service Sales Order', 1, @acctSvcRev,  1, 'seed', SYSDATETIMEOFFSET());
    END

    IF NOT EXISTS (SELECT 1 FROM om.PricingRules WHERE CompanyId = @co AND Code = 'PR-STD')
    BEGIN
        INSERT INTO om.PricingRules (Id, CompanyId, Code, Description, Scope, PrioritySequence, DiscountPercent, IsActive, CreatedBy, CreatedOn) VALUES
        (@prStd, @co, 'PR-STD', 'Standard 0% discount baseline', 0, 100, 0.0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@prVol, @co, 'PR-VOL', 'Volume 5% over 100 EA',        2, 10,  5.0, 1, 'seed', SYSDATETIMEOFFSET());
    END

    -- ---- BOM: Work centers + BOM headers (parent=ITEM-1001, comps=X/Y) ----
    IF NOT EXISTS (SELECT 1 FROM bom.WorkCenters WHERE CompanyId = @co AND Code = 'WC-ASSY')
    BEGIN
        INSERT INTO bom.WorkCenters (Id, CompanyId, Code, Name, Department, CapacityHoursPerDay, EfficiencyPercentage, CostRatePerHour, IsActive, CreatedBy, CreatedOn) VALUES
        (@wc1, @co, 'WC-ASSY', 'Assembly',   'Production', 16.0, 95.0, 45.00, 1, 'seed', SYSDATETIMEOFFSET()),
        (@wc2, @co, 'WC-PACK', 'Packaging', 'Production', 16.0, 98.0, 30.00, 1, 'seed', SYSDATETIMEOFFSET());
    END

    IF NOT EXISTS (SELECT 1 FROM bom.BomHeaders WHERE CompanyId = @co AND ParentItemId = @itemA AND Revision = 'A')
    BEGIN
        INSERT INTO bom.BomHeaders (Id, CompanyId, ParentItemId, Revision, Description, BomType, [Status], YieldPercentage, EstimatedMaterialCost, EstimatedLaborCost, EstimatedOverheadCost, CreatedBy, CreatedOn) VALUES
        (@bom1, @co, @itemA, 'A', 'Widget A assembly', 0, 0, 100.0, 30.50, 12.00, 4.00, 'seed', SYSDATETIMEOFFSET()),
        (@bom2, @co, @itemB, 'A', 'Widget B assembly', 0, 0, 100.0, 36.00, 14.00, 5.00, 'seed', SYSDATETIMEOFFSET());

        INSERT INTO bom.BomComponentLines (Id, BomHeaderId, ComponentItemId, QuantityPerParent, UnitOfMeasure, ScrapFactor, OperationSequence, WorkCenterId, IsPhantom, IsCritical, EstimatedUnitCost, CreatedBy, CreatedOn) VALUES
        (NEWID(), @bom1, @itemC, 2.0, 'EA', 2.0, 10, @wc1, 0, 0, 12.50, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @bom1, @itemD, 1.0, 'EA', 1.0, 20, @wc1, 0, 0, 18.00, 'seed', SYSDATETIMEOFFSET());
    END
    SET @wc1 = (SELECT Id FROM bom.WorkCenters WHERE CompanyId = @co AND Code = 'WC-ASSY');
    SET @wc2 = (SELECT Id FROM bom.WorkCenters WHERE CompanyId = @co AND Code = 'WC-PACK');
    SET @bom1 = (SELECT Id FROM bom.BomHeaders WHERE CompanyId = @co AND ParentItemId = @itemA AND Revision = 'A');
    SET @bom2 = (SELECT Id FROM bom.BomHeaders WHERE CompanyId = @co AND ParentItemId = @itemB AND Revision = 'A');

    -- ---- PROJECT: Projects + tasks ----
    IF NOT EXISTS (SELECT 1 FROM proj.Projects WHERE CompanyId = @co AND ProjectCode = 'PRJ-001')
    BEGIN
        INSERT INTO proj.Projects (Id, CompanyId, ProjectCode, Name, Description, ProjectType, [Status], CustomerId, ProjectManager, ContractValue, OriginalBudget, RevisedBudget, CostsToDate, RevenueToDate, PercentComplete, RetainagePercentage, RetainageHeld, IsBilled, IsClosed, ContingencyAmount, ReleasedContingency, ExchangeRate, BillingHold, AccountingMethod, AccruedLoss, CreatedBy, CreatedOn) VALUES
        (@proj1, @co, 'PRJ-001', 'Office Tower Build', 'New HQ construction', 0, 0, @custAcme, 'Jane PM', 1500000.00, 1500000.00, 1500000.00, 0, 0, 0.0, 5.0, 0, 0, 0, 0.0, 0.0, 1.0, 0, 0, 0.0, 'seed', SYSDATETIMEOFFSET()),
        (@proj2, @co, 'PRJ-002', 'Warehouse Fit-out', 'Tenant improvement',   0, 0, @custGlob, 'Bob PM',  400000.00,  400000.00,  400000.00,  0, 0, 0.0, 5.0, 0, 0, 0, 0.0, 0.0, 1.0, 0, 0, 0.0, 'seed', SYSDATETIMEOFFSET());
    END
    SET @proj1 = (SELECT Id FROM proj.Projects WHERE CompanyId = @co AND ProjectCode = 'PRJ-001');
    SET @proj2 = (SELECT Id FROM proj.Projects WHERE CompanyId = @co AND ProjectCode = 'PRJ-002');

    IF NOT EXISTS (SELECT 1 FROM proj.ProjectTasks WHERE ProjectId = @proj1 AND TaskCode = 'T1')
    BEGIN
        INSERT INTO proj.ProjectTasks (Id, ProjectId, TaskCode, Description, BudgetedHours, BudgetedCost, ActualHours, ActualCost, PercentComplete, SortOrder, CreatedBy, CreatedOn) VALUES
        (NEWID(), @proj1, 'T1', 'Site Prep',      200.0, 40000.00, 0, 0, 0.0, 10, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @proj1, 'T2', 'Foundation',     400.0, 90000.00, 0, 0, 0.0, 20, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @proj1, 'T3', 'Superstructure', 600.0, 180000.00,0, 0, 0.0, 30, 'seed', SYSDATETIMEOFFSET());
    END

    -- ---- PAYROLL: Pay codes + employees (codes reused; per-company unique) ----
    IF NOT EXISTS (SELECT 1 FROM pay.PayCodes WHERE CompanyId = @co AND Code = 'REG')
    BEGIN
        INSERT INTO pay.PayCodes (Id, CompanyId, Code, Description, Type, GlAccountNumber, IsOvertime, CountsAsHoursWorked, CreatedBy, CreatedOn) VALUES
        (@payReg, @co, 'REG', 'Regular Wages', 0, '6000', 0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@payOT,  @co, 'OT',  'Overtime Wages',0, '6000', 1, 1, 'seed', SYSDATETIMEOFFSET());
    END

    IF NOT EXISTS (SELECT 1 FROM pay.Employees WHERE CompanyId = @co AND EmployeeCode = 'E0001')
    BEGIN
        INSERT INTO pay.Employees (Id, CompanyId, EmployeeCode, FirstName, LastName, EmploymentType, [Status], HireDate, Email, DefaultProjectId, AllocationPercentage, IsBillable, CreatedBy, CreatedOn) VALUES
        (@emp1, @co, 'E0001', 'Alice',   'Nguyen',    0, 0, '2024-01-15T00:00:00Z', 'alice@erp.com',  @proj1, 100.0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@emp2, @co, 'E0002', 'Bob',     'Martinez',  0, 0, '2024-03-01T00:00:00Z', 'bob@erp.com',    @proj1, 100.0, 1, 'seed', SYSDATETIMEOFFSET()),
        (@emp3, @co, 'E0003', 'Carol',   'Singh',     0, 0, '2023-11-20T00:00:00Z', 'carol@erp.com',  @proj1, 100.0, 1, 'seed', SYSDATETIMEOFFSET());

        INSERT INTO pay.EmployeePayCodes (Id, EmployeeId, PayCodeId, OverrideRate, IsBillable, CreatedBy, CreatedOn) VALUES
        (NEWID(), @emp1, @payReg, 45.00, 1, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @emp1, @payOT,  67.50, 1, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @emp2, @payReg, 52.00, 1, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @emp2, @payOT,  78.00, 1, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @emp3, @payReg, 38.00, 1, 'seed', SYSDATETIMEOFFSET());
    END
    SET @emp1 = (SELECT Id FROM pay.Employees WHERE CompanyId = @co AND EmployeeCode = 'E0001');
    SET @emp2 = (SELECT Id FROM pay.Employees WHERE CompanyId = @co AND EmployeeCode = 'E0002');
    SET @emp3 = (SELECT Id FROM pay.Employees WHERE CompanyId = @co AND EmployeeCode = 'E0003');

    -- ---- FIELD SERVICE: territories, SLAs, rate cards, skills, techs (codes reused) ----
    IF NOT EXISTS (SELECT 1 FROM fs.ServiceTerritories WHERE CompanyId = @co AND Code = 'ST-EAST')
    BEGIN
        INSERT INTO fs.ServiceTerritories (Id, CompanyId, Code, Name, Region, ZipCoverage, TravelCostPerMile, CreatedBy, CreatedOn) VALUES
        (@ster1, @co, 'ST-EAST', 'East Service Territory', 'East', '10001-11999', 0.65, 'seed', SYSDATETIMEOFFSET()),
        (@ster2, @co, 'ST-WEST', 'West Service Territory', 'West', '90001-99999', 0.70, 'seed', SYSDATETIMEOFFSET());
    END

    IF NOT EXISTS (SELECT 1 FROM fs.SlaDefinitions WHERE CompanyId = @co AND Priority = 1)
    BEGIN
        INSERT INTO fs.SlaDefinitions (Id, CompanyId, Name, Priority, ResponseMinutes, ResolutionMinutes, Escalate, CreatedBy, CreatedOn) VALUES
        (NEWID(), @co, 'Standard Response', 1, 240, 1440, 0, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @co, 'Critical Response', 0, 60,  480,  1,     'seed', SYSDATETIMEOFFSET());
    END

    IF NOT EXISTS (SELECT 1 FROM fs.ServiceRateCards WHERE CompanyId = @co AND Name = 'Standard Rates 2026')
    BEGIN
        INSERT INTO fs.ServiceRateCards (Id, CompanyId, Name, EffectiveDate, IsActive, LaborRatePerHour, OvertimeRatePerHour, TripCharge, PartsMarkupPercent, CreatedBy, CreatedOn)
        VALUES (NEWID(), @co, 'Standard Rates 2026', '2026-01-01T00:00:00Z', 1, 85.00, 127.50, 50.00, 15.0, 'seed', SYSDATETIMEOFFSET());
    END

    IF NOT EXISTS (SELECT 1 FROM fs.SkillCertifications WHERE CompanyId = @co AND Code = 'ELEC')
    BEGIN
        INSERT INTO fs.SkillCertifications (Id, CompanyId, Code, Name, Category, Description, CreatedBy, CreatedOn) VALUES
        (NEWID(), @co, 'ELEC', 'Electrical', 'Trade', 'Licensed electrician', 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @co, 'PLMB', 'Plumbing',  'Trade', 'Licensed plumber',     'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @co, 'HVAC', 'HVAC',      'Trade', 'HVAC certified',       'seed', SYSDATETIMEOFFSET());
    END

    -- VendorId is globally unique -> namespace with tenant code.
    DECLARE @v1 UNIQUEIDENTIFIER = NEWID(), @v2 UNIQUEIDENTIFIER = NEWID(),
            @v3 UNIQUEIDENTIFIER = NEWID(), @v4 UNIQUEIDENTIFIER = NEWID(),
            @v5 UNIQUEIDENTIFIER = NEWID();
    -- VendorId is now per-company unique -> reuse identical codes across companies.
    IF NOT EXISTS (SELECT 1 FROM ap.Vendors WHERE CompanyId = @co AND VendorId = 'V1001')
    BEGIN
        INSERT INTO ap.Vendors (Id, CompanyId, VendorId, Name, LegalName, TaxId, DefaultPaymentTermId, IsActive, BackupWithholdingFlag, BackupWithholdingRate, CreatedBy, CreatedOn) VALUES
        (@v1, @co, 'V1001', 'Acme Supply Co',      'Acme Supply Co Inc',      '45-1111111', @ptNet30, 1, 0, 0, 'seed', SYSDATETIMEOFFSET()),
        (@v2, @co, 'V1002', 'Global Components',   'Global Components LLC',   '45-2222222', @ptNet15, 1, 0, 0, 'seed', SYSDATETIMEOFFSET()),
        (@v3, @co, 'V1003', 'Northwind Materials', 'Northwind Materials Corp','45-3333333', @ptNet30, 1, 0, 0, 'seed', SYSDATETIMEOFFSET()),
        (@v4, @co, 'V1004', 'Pacific Freight',     'Pacific Freight Inc',     '45-4444444', @ptNet30, 1, 0, 0, 'seed', SYSDATETIMEOFFSET()),
        (@v5, @co, 'V1005', 'TechParts Direct',    'TechParts Direct LLC',    '45-5555555', @ptNet15, 1, 0, 0, 'seed', SYSDATETIMEOFFSET());
    END

    -- Technicians reference employees (per company).
    IF NOT EXISTS (SELECT 1 FROM fs.Technicians WHERE CompanyId = @co AND Code = 'TECH-01')
    BEGIN
        INSERT INTO fs.Technicians (Id, CompanyId, EmployeeId, Code, FirstName, LastName, DefaultTerritoryId, [Status], Email, Phone, HourlyRate, CreatedBy, CreatedOn) VALUES
        (NEWID(), @co, @emp1, 'TECH-01', 'Alice', 'Nguyen',   @ster1, 0, 'alice@erp.com',  '555-0101', 85.00, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @co, @emp2, 'TECH-02', 'Bob',   'Martinez', @ster1, 0, 'bob@erp.com',    '555-0102', 85.00, 'seed', SYSDATETIMEOFFSET());
    END

    IF NOT EXISTS (SELECT 1 FROM fs.EquipmentAssets WHERE CompanyId = @co AND AssetTag = 'EQ-001')
    BEGIN
        INSERT INTO fs.EquipmentAssets (Id, CompanyId, AssetTag, SerialNumber, Description, ItemId, CustomerId, Ownership, UnderWarranty, CreatedBy, CreatedOn) VALUES
        (NEWID(), @co, 'EQ-001', 'SN-1001', 'Rooftop HVAC Unit A', @itemA, @custAcme, 1, 1, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @co, 'EQ-002', 'SN-1002', 'Rooftop HVAC Unit B', @itemA, @custGlob, 1, 0, 'seed', SYSDATETIMEOFFSET());
    END

    IF NOT EXISTS (SELECT 1 FROM fs.ServiceContracts WHERE CompanyId = @co AND ContractNumber = 'SC-001')
    BEGIN
        INSERT INTO fs.ServiceContracts (Id, CompanyId, ContractNumber, Name, CustomerId, StartDate, EndDate, [Status], BillingType, ContractValue, IncludesWarranty, WarrantyMonths, CreatedBy, CreatedOn) VALUES
        (NEWID(), @co, 'SC-001', @cName + ' Acme Annual Maintenance', @custAcme, '2026-01-01T00:00:00Z', '2026-12-31T00:00:00Z', 0, 0, 24000.00, 1, 12, 'seed', SYSDATETIMEOFFSET()),
        (NEWID(), @co, 'SC-002', @cName + ' Globex Full-Service',    @custGlob, '2026-01-01T00:00:00Z', '2026-12-31T00:00:00Z', 0, 1, 60000.00, 1, 12, 'seed', SYSDATETIMEOFFSET());
    END

    PRINT 'Seeded masters for company ' + @cName + ' (' + @cCode + ').';
END
CLOSE curC; DEALLOCATE curC;
GO

PRINT 'Multi-tenant seed complete.';
GO
