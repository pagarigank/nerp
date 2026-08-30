-- =====================================================================
-- ERP Demo Master / Reference Seed (Phases 0-12)
-- Idempotent-ish: safe to run after 02-reset-modules.sql. Seeds masters
-- and reference data for every business module under ONE company so all
-- data segmentation is consistent:
--   CompanyId = 11111111-1111-1111-1111-111111111111  (US Operations)
--
-- Run:
--   sqlcmd -S localhost -U sa -P P@ssw0rd -C -d erp -b -i sql/seed/03-seed-masters.sql
--
-- NOTE: masters/reference data only. No transactional documents (no invoices,
-- vouchers, journal batches, POs, SOs, shipments, work orders, payroll runs).
-- =====================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @co UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';

-- Reusable account GUIDs (same ids in platform.Accounts and gl.Account)
DECLARE @acctCash      UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';
DECLARE @acctAR        UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000002';
DECLARE @acctInventory UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000003';
DECLARE @acctEquip     UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000004';
DECLARE @acctAccDep    UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000005';
DECLARE @acctAP        UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000006';
DECLARE @acctAccrued   UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000007';
DECLARE @acctPayroll   UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000008';
DECLARE @acctRetained  UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000009';
DECLARE @acctEquity    UNIQUEIDENTIFIER = '30000000-0000-0000-0000-00000000000A';
DECLARE @acctRevenue   UNIQUEIDENTIFIER = '30000000-0000-0000-0000-00000000000B';
DECLARE @acctCOGS      UNIQUEIDENTIFIER = '30000000-0000-0000-0000-00000000000C';
DECLARE @acctSalaries  UNIQUEIDENTIFIER = '30000000-0000-0000-0000-00000000000D';
DECLARE @acctRent      UNIQUEIDENTIFIER = '30000000-0000-0000-0000-00000000000E';
DECLARE @acctUtil      UNIQUEIDENTIFIER = '30000000-0000-0000-0000-00000000000F';
DECLARE @acctOtherExp  UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000010';
DECLARE @acctSvcRev    UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000011';
DECLARE @acctSalesTax  UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000012';

-- ---------------------------------------------------------------------
-- 1. CHART OF ACCOUNTS (platform.Accounts mirrored into gl.Account)
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM platform.Accounts WHERE CompanyId = @co AND AccountNumber = '1000')
BEGIN
    INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedBy, CreatedOn) VALUES
    (@acctCash,      @co, '1000', 'Cash',                       0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@acctAR,        @co, '1200', 'Accounts Receivable',       0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@acctInventory, @co, '1300', 'Inventory',                 0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
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
    (@acctOtherExp,  @co, '7000', 'Other Expense',            4, 0, 1, 'seed', SYSDATETIMEOFFSET());
END

-- Mirror into gl.Account (gl.JournalEntryLines.AccountId FK references gl.Account)
IF NOT EXISTS (SELECT 1 FROM gl.Account WHERE CompanyId = @co AND AccountNumber = '1000')
BEGIN
    INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedBy, CreatedOn) VALUES
    (@acctCash,      @co, '1000', 'Cash',                       0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@acctAR,        @co, '1200', 'Accounts Receivable',       0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@acctInventory, @co, '1300', 'Inventory',                 0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
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
    (@acctOtherExp,  @co, '7000', 'Other Expense',            4, 0, 1, 'seed', SYSDATETIMEOFFSET());
END

-- Inventory/GL posting handlers hardcode account numbers 1400 (Inventory Asset),
-- 2010 (GRNI), 5900 (Variance) and 6900 (Scrap). Ensure these exist for the
-- company. 1400 is added alongside the legacy 1300 so posting resolves correctly.
DECLARE @acctGrni     UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000020';
DECLARE @acctVariance UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000021';
DECLARE @acctScrap    UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000022';
DECLARE @acctInv1400  UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000023';

IF NOT EXISTS (SELECT 1 FROM platform.Accounts WHERE CompanyId = @co AND AccountNumber = '1400')
    INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedBy, CreatedOn) VALUES
    (@acctInv1400,  @co, '1400', 'Inventory Asset',            0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@acctGrni,     @co, '2010', 'Goods Received Not Invoiced', 1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
    (@acctVariance, @co, '5900', 'Inventory Variance',         4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@acctScrap,    @co, '6900', 'Scrap & Obsolescence Loss',  4, 0, 1, 'seed', SYSDATETIMEOFFSET());

IF NOT EXISTS (SELECT 1 FROM gl.Account WHERE CompanyId = @co AND AccountNumber = '1400')
    INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedBy, CreatedOn) VALUES
    (@acctInv1400,  @co, '1400', 'Inventory Asset',            0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@acctGrni,     @co, '2010', 'Goods Received Not Invoiced', 1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
    (@acctVariance, @co, '5900', 'Inventory Variance',         4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@acctScrap,    @co, '6900', 'Scrap & Obsolescence Loss',  4, 0, 1, 'seed', SYSDATETIMEOFFSET());

-- ---------------------------------------------------------------------
-- 2. PLATFORM: Fiscal Years + Periods + Currencies + Exchange Rates
-- ---------------------------------------------------------------------
DECLARE @fyId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';
DECLARE @curUsd UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM platform.FiscalYears WHERE CompanyId = @co AND [Year] = 2026)
BEGIN
    SET @fyId = (SELECT Id FROM platform.FiscalYears WHERE CompanyId = @co AND [Year] = 2026);
    IF @fyId IS NULL
    BEGIN
        SET @fyId = NEWID();
        INSERT INTO platform.FiscalYears (Id, CompanyId, Year, Description, StartDate, EndDate, IsClosed, CreatedBy, CreatedOn)
        VALUES (@fyId, @co, 2026, 'Fiscal Year 2026', '2026-01-01T00:00:00Z', '2026-12-31T00:00:00Z', 0, 'seed', SYSDATETIMEOFFSET());
    END

    INSERT INTO platform.FiscalPeriods (Id, FiscalYearId, CompanyId, PeriodNumber, Description, StartDate, EndDate, Status, CreatedBy, CreatedOn)
    SELECT NEWID(), @fyId, @co, n, DATENAME(MONTH, DATEFROMPARTS(2026, n, 1)) + ' 2026',
           DATEFROMPARTS(2026, n, 1), EOMONTH(DATEFROMPARTS(2026, n, 1)), 0, 'seed', SYSDATETIMEOFFSET()
    FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12)) AS m(n)
    WHERE NOT EXISTS (SELECT 1 FROM platform.FiscalPeriods WHERE CompanyId = @co AND PeriodNumber = n);
END

IF NOT EXISTS (SELECT 1 FROM platform.Currencies WHERE Code = 'USD')
BEGIN
    INSERT INTO platform.Currencies (Id, Code, Name, Symbol, DecimalPlaces, IsActive, CreatedBy, CreatedOn) VALUES
    (@curUsd,                       'USD', 'US Dollar',       '$', 2, 1, 'seed', SYSDATETIMEOFFSET()),
    ('10000000-0000-0000-0000-000000000002', 'EUR', 'Euro',        '€', 2, 1, 'seed', SYSDATETIMEOFFSET()),
    ('10000000-0000-0000-0000-000000000003', 'GBP', 'British Pound', '£', 2, 1, 'seed', SYSDATETIMEOFFSET()),
    ('10000000-0000-0000-0000-000000000004', 'CAD', 'Canadian Dollar','C$',2, 1, 'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM platform.ExchangeRates WHERE CompanyId = @co)
BEGIN
    INSERT INTO platform.ExchangeRates (Id, CompanyId, FromCurrency, ToCurrency, Rate, EffectiveDate, CreatedBy, CreatedOn) VALUES
    ('70000000-0000-0000-0000-000000000001', @co, 'USD', 'EUR', 0.9200, '2026-01-01T00:00:00Z', 'seed', SYSDATETIMEOFFSET()),
    ('70000000-0000-0000-0000-000000000002', @co, 'USD', 'GBP', 0.7900, '2026-01-01T00:00:00Z', 'seed', SYSDATETIMEOFFSET()),
    ('70000000-0000-0000-0000-000000000003', @co, 'USD', 'CAD', 1.3600, '2026-01-01T00:00:00Z', 'seed', SYSDATETIMEOFFSET());
END

-- ---------------------------------------------------------------------
-- 3. AP: Payment Terms + Vendors
-- ---------------------------------------------------------------------
DECLARE @ptNet30 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';
DECLARE @ptNet15 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000002';

IF NOT EXISTS (SELECT 1 FROM ap.PaymentTerms WHERE Name = 'Net 30')
BEGIN
    INSERT INTO ap.PaymentTerms (Id, Name, DueDays, DiscountDays, DiscountPercent, IsActive, CreatedBy, CreatedOn) VALUES
    (@ptNet30, 'Net 30', 30, 0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@ptNet15, 'Net 15', 15, 10, 2, 1, 'seed', SYSDATETIMEOFFSET());
END
SET @ptNet30 = ISNULL((SELECT Id FROM ap.PaymentTerms WHERE Name = 'Net 30'), @ptNet30);
SET @ptNet15 = ISNULL((SELECT Id FROM ap.PaymentTerms WHERE Name = 'Net 15'), @ptNet15);

IF NOT EXISTS (SELECT 1 FROM ap.Vendors WHERE VendorId = 'V-1001')
BEGIN
    INSERT INTO ap.Vendors (Id, VendorId, Name, LegalName, TaxId, DefaultPaymentTermId, IsActive, BackupWithholdingFlag, BackupWithholdingRate, CreatedBy, CreatedOn) VALUES
    (NEWID(), 'V-1001', 'Acme Supply Co',     'Acme Supply Co Inc',     '45-1111111', @ptNet30, 1, 0, 0, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), 'V-1002', 'Global Components',  'Global Components LLC',  '45-2222222', @ptNet15, 1, 0, 0, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), 'V-1003', 'Northwind Materials','Northwind Materials Corp','45-3333333', @ptNet30, 1, 0, 0, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), 'V-1004', 'Pacific Freight',    'Pacific Freight Inc',    '45-4444444', @ptNet30, 1, 0, 0, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), 'V-1005', 'TechParts Direct',   'TechParts Direct LLC',   '45-5555555', @ptNet15, 1, 0, 0, 'seed', SYSDATETIMEOFFSET());
END

-- ---------------------------------------------------------------------
-- 4. AR: Customers + Tax Codes (TaxCodes lives in om, referenced by Customers)
-- ---------------------------------------------------------------------
DECLARE @taxStd UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000001';
DECLARE @custAcme UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000001';
DECLARE @custGlob UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000002';

IF NOT EXISTS (SELECT 1 FROM om.TaxCodes WHERE CompanyId = @co AND Code = 'STAX')
BEGIN
    INSERT INTO om.TaxCodes (Id, CompanyId, Code, Description, Jurisdiction, Rate, IsTaxable, IsActive, CreatedBy, CreatedOn) VALUES
    (@taxStd, @co, 'STAX', 'Standard Sales Tax', 'US', 8.25, 1, 1, 'seed', SYSDATETIMEOFFSET()),
    ('60000000-0000-0000-0000-000000000002', @co, 'NTAX', 'No Tax', 'US', 0.00, 0, 1, 'seed', SYSDATETIMEOFFSET()),
    ('60000000-0000-0000-0000-000000000003', @co, 'RTAX', 'Reduced Tax', 'US', 5.00, 1, 1, 'seed', SYSDATETIMEOFFSET());
END
SET @taxStd = ISNULL((SELECT Id FROM om.TaxCodes WHERE CompanyId = @co AND Code = 'STAX'), @taxStd);
DECLARE @terr1 UNIQUEIDENTIFIER = '62000000-0000-0000-0000-000000000001';
DECLARE @terr2 UNIQUEIDENTIFIER = '62000000-0000-0000-0000-000000000002';
IF NOT EXISTS (SELECT 1 FROM om.SalesTerritories WHERE CompanyId = @co AND Code = 'EAST')
BEGIN
    INSERT INTO om.SalesTerritories (Id, CompanyId, Code, Name, Region, DefaultCommissionRate, IsActive, CreatedBy, CreatedOn) VALUES
    (@terr1, @co, 'EAST', 'East Region', 'East', 5.0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@terr2, @co, 'WEST', 'West Region', 'West', 5.0, 1, 'seed', SYSDATETIMEOFFSET());
END

DECLARE @srep1 UNIQUEIDENTIFIER = '63000000-0000-0000-0000-000000000001';
IF NOT EXISTS (SELECT 1 FROM om.SalesReps WHERE CompanyId = @co AND Code = 'SR01')
BEGIN
    INSERT INTO om.SalesReps (Id, CompanyId, Code, Name, CommissionRate, TerritoryId, IsActive, Email, CreatedBy, CreatedOn) VALUES
    (@srep1, @co, 'SR01', 'Dale Cooper', 6.0, @terr1, 1, 'dale@erp.com', 'seed', SYSDATETIMEOFFSET()),
    ('63000000-0000-0000-0000-000000000002', @co, 'SR02', 'Laura Palmer', 5.5, @terr2, 1, 'laura@erp.com', 'seed', SYSDATETIMEOFFSET());
END
SET @srep1 = ISNULL((SELECT Id FROM om.SalesReps WHERE CompanyId = @co AND Code = 'SR01'), @srep1);
SET @terr1 = ISNULL((SELECT Id FROM om.SalesTerritories WHERE CompanyId = @co AND Code = 'EAST'), @terr1);
SET @terr2 = ISNULL((SELECT Id FROM om.SalesTerritories WHERE CompanyId = @co AND Code = 'WEST'), @terr2);

IF NOT EXISTS (SELECT 1 FROM ar.Customers WHERE CustomerId = 'C-1001')
BEGIN
    INSERT INTO ar.Customers (Id, CompanyId, CustomerId, Name, LegalName, TaxId, CreditLimit, CreditHoldDays, DefaultPaymentTermId, TaxExempt, CurrencyCode, IsActive, SalesRepId, TaxCodeId, CreatedBy, CreatedOn) VALUES
    (@custAcme, @co, 'C-1001', 'Acme Industries',  'Acme Industries Inc.',  '12-1111111', 100000, 0, @ptNet30, 0, 'USD', 1, @srep1, @taxStd, 'seed', SYSDATETIMEOFFSET()),
    (@custGlob, @co, 'C-1002', 'Globex Corporation','Globex Corp',          '12-2222222', 250000, 0, @ptNet30, 0, 'USD', 1, @srep1, @taxStd, 'seed', GETDATE()),
    ('A0000000-0000-0000-0000-000000000003', @co, 'C-1003', 'Initech LLC',    'Initech LLC',          '12-3333333', 50000,  30, @ptNet15, 0, 'USD', 1, @srep1, @taxStd, 'seed', SYSDATETIMEOFFSET()),
    ('A0000000-0000-0000-0000-000000000004', @co, 'C-1004', 'Soylent Foods',  'Soylent Foods Inc',    '12-4444444', 75000,  0, @ptNet30, 0, 'USD', 1, @srep1, @taxStd, 'seed', SYSDATETIMEOFFSET()),
    ('A0000000-0000-0000-0000-000000000005', @co, 'C-1005', 'Hooli',          'Hooli Inc',            '12-5555555', 300000, 0, @ptNet30, 0, 'USD', 1, @srep1, @taxStd, 'seed', SYSDATETIMEOFFSET());
END
SET @custAcme = ISNULL((SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C-1001'), ISNULL((SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C1001'), @custAcme));
SET @custGlob = ISNULL((SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C-1002'), ISNULL((SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C1002'), @custGlob));

-- Repair-safe: ensure the legacy C-100x customers carry the correct CompanyId
-- (an earlier 03 version omitted CompanyId, leaving them on the zero GUID).
UPDATE ar.Customers SET CompanyId = @co
WHERE CompanyId = '00000000-0000-0000-0000-000000000000'
  AND CustomerId IN ('C-1001','C-1002','C-1003','C-1004','C-1005');

-- ---------------------------------------------------------------------
-- 5. CASH: Bank Accounts
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM cash.BankAccounts WHERE AccountCode = 'BANK-01')
BEGIN
    INSERT INTO cash.BankAccounts (Id, CompanyId, AccountCode, AccountName, AccountNumber, RoutingNumber, BankName, CurrencyCode, AccountType, OpeningBalance, CurrentBalance, GlAccountId, Status, CreatedBy, CreatedOn) VALUES
    (NEWID(), @co, 'BANK-01', 'Operating Checking', '00001111', '021000021', 'First National', 'USD', 0, 500000.00, 500000.00, @acctCash, 0, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @co, 'BANK-02', 'Payroll Checking',   '00002222', '021000021', 'First National', 'USD', 0, 100000.00, 100000.00, @acctCash, 0, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @co, 'BANK-03', 'Savings',            '00003333', '021000021', 'First National', 'USD', 1, 250000.00, 250000.00, @acctCash, 0, 'seed', SYSDATETIMEOFFSET());
END

-- Repair-safe: realign bank GL links to the actual Cash account (a prior run may
-- have left NULL or a dangling GlAccountId on BANK-01/02/03 or other US banks).
DECLARE @usCash UNIQUEIDENTIFIER = (SELECT Id FROM platform.Accounts WHERE CompanyId = @co AND AccountNumber = '1000');
IF @usCash IS NOT NULL
    UPDATE cash.BankAccounts SET GlAccountId = @usCash
    WHERE CompanyId = @co AND (GlAccountId IS NULL OR NOT EXISTS (SELECT 1 FROM platform.Accounts a WHERE a.Id = GlAccountId));

-- ---------------------------------------------------------------------
-- 6. INVENTORY: UoM, Categories, Warehouses, Items
-- ---------------------------------------------------------------------
DECLARE @uomEa UNIQUEIDENTIFIER = '70000000-0000-0000-0007-000000000001';
DECLARE @uomLb UNIQUEIDENTIFIER = '70000000-0000-0000-0007-000000000002';
DECLARE @catProd UNIQUEIDENTIFIER = '71000000-0000-0000-0007-000000000001';
DECLARE @catComp UNIQUEIDENTIFIER = '71000000-0000-0000-0007-000000000002';
DECLARE @whMain UNIQUEIDENTIFIER = '72000000-0000-0000-0007-000000000001';
DECLARE @itemA UNIQUEIDENTIFIER = '73000000-0000-0000-0007-000000000001';
DECLARE @itemB UNIQUEIDENTIFIER = '73000000-0000-0000-0007-000000000002';
DECLARE @itemC UNIQUEIDENTIFIER = '73000000-0000-0000-0007-000000000003';
DECLARE @itemD UNIQUEIDENTIFIER = '73000000-0000-0000-0007-000000000004';

IF NOT EXISTS (SELECT 1 FROM inv.UnitOfMeasures WHERE CompanyId = @co AND Code = 'EA')
BEGIN
    INSERT INTO inv.UnitOfMeasures (Id, CompanyId, Code, Description, BaseUOM, FactorToBase, IsActive, CreatedBy, CreatedOn) VALUES
    (@uomEa, @co, 'EA',  'Each',     'EA', 1.0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@uomLb, @co, 'LB',  'Pound',    'LB', 1.0, 1, 'seed', SYSDATETIMEOFFSET()),
    ('70000000-0000-0000-0007-000000000003', @co, 'BOX', 'Box (12 EA)','EA', 12.0, 1, 'seed', SYSDATETIMEOFFSET()),
    ('70000000-0000-0000-0007-000000000004', @co, 'CS',  'Case (24 EA)','EA', 24.0, 1, 'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM inv.ItemCategories WHERE CompanyId = @co AND CategoryCode = 'PROD')
BEGIN
    INSERT INTO inv.ItemCategories (Id, CategoryCode, CategoryName, CompanyId, InventoryAccountId, COGSAccountId, VarianceAccountId, Description, IsActive, CreatedBy, CreatedOn) VALUES
    (@catProd, 'PROD', 'Finished Goods',  @co, @acctInventory, @acctCOGS, @acctOtherExp, 'Manufactured / sold products', 1, 'seed', SYSDATETIMEOFFSET()),
    (@catComp, 'COMP', 'Components',      @co, @acctInventory, @acctCOGS, @acctOtherExp, 'Purchased components',        1, 'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM inv.Warehouses WHERE CompanyId = @co AND WarehouseCode = 'WH-MAIN')
BEGIN
    INSERT INTO inv.Warehouses (Id, WarehouseCode, WarehouseName, CompanyId, WarehouseType, Address, IsActive, CreatedBy, CreatedOn) VALUES
    (@whMain, 'WH-MAIN', 'Main Warehouse', @co, 0, '123 Logistics Way, Newark NJ', 1, 'seed', SYSDATETIMEOFFSET()),
    ('72000000-0000-0000-0007-000000000002', 'WH-WEST', 'West DC', @co, 0, '900 Market St, Los Angeles CA', 1, 'seed', SYSDATETIMEOFFSET()),
    ('72000000-0000-0000-0007-000000000003', 'WH-EAST', 'East DC', @co, 0, '5 Harbor Blvd, Boston MA', 1, 'seed', SYSDATETIMEOFFSET());
END
SET @whMain = ISNULL((SELECT Id FROM inv.Warehouses WHERE CompanyId = @co AND WarehouseCode = 'WH-MAIN'), @whMain);

IF NOT EXISTS (SELECT 1 FROM inv.Items WHERE ItemCode = 'ITEM-1001')
BEGIN
    INSERT INTO inv.Items (Id, ItemCode, Description, CompanyId, ItemType, BaseUnitOfMeasure, CostingMethod, ItemCategoryId, Status, AllowNegativeInventory, IsLotControlled, IsSerialControlled, StandardCost, ReorderPoint, ReorderQuantity, SafetyStock, LeadTimeDays, IsKit, IsHazardousMaterial, CreatedBy, CreatedOn) VALUES
    (@itemA, 'ITEM-1001', 'Widget A',        @co, 0, 'EA', 0, @catProd, 0, 0, 0, 0, 50.00,  100, 500, 50, 7,  0, 0, 'seed', SYSDATETIMEOFFSET()),
    (@itemB, 'ITEM-1002', 'Widget B',        @co, 0, 'EA', 0, @catProd, 0, 0, 0, 0, 75.00,  80,  400, 40, 7,  0, 0, 'seed', SYSDATETIMEOFFSET()),
    (@itemC, 'ITEM-1003', 'Raw Component X', @co, 1, 'EA', 0, @catComp, 0, 0, 0, 0, 12.50,  200, 1000,100, 14, 0, 0, 'seed', SYSDATETIMEOFFSET()),
    (@itemD, 'ITEM-1004', 'Raw Component Y', @co, 1, 'EA', 0, @catComp, 0, 0, 0, 0, 18.00,  200, 1000,100, 14, 0, 0, 'seed', SYSDATETIMEOFFSET()),
    ('73000000-0000-0000-0007-000000000005', 'ITEM-1005', 'Service Visit', @co, 2, 'EA', 0, @catProd, 0, 0, 0, 0, 0.00, 0, 0, 0, 0, 0, 0, 'seed', SYSDATETIMEOFFSET());
END

-- ---------------------------------------------------------------------
-- 7. OM: Sales Order Types, Shipping Methods
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM om.SalesOrderTypes WHERE Code = 'SO-STD')
BEGIN
    INSERT INTO om.SalesOrderTypes (Id, CompanyId, Code, Description, TypeCode, RevenueAccountId, IsActive, CreatedBy, CreatedOn) VALUES
    ('64000000-0000-0000-0008-000000000001', @co, 'SO-STD', 'Standard Sales Order', 0, @acctRevenue, 1, 'seed', SYSDATETIMEOFFSET()),
    ('64000000-0000-0000-0008-000000000002', @co, 'SO-SVC', 'Service Sales Order', 1, @acctSvcRev,  1, 'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM om.PricingRules WHERE Code = 'PR-STD')
BEGIN
    INSERT INTO om.PricingRules (Id, CompanyId, Code, Description, Scope, PrioritySequence, DiscountPercent, IsActive, CreatedBy, CreatedOn) VALUES
    ('65000000-0000-0000-0008-000000000001', @co, 'PR-STD',  'Standard 0% discount baseline', 0, 100, 0.0, 1, 'seed', SYSDATETIMEOFFSET()),
    ('65000000-0000-0000-0008-000000000002', @co, 'PR-VOL',  'Volume 5% over 100 EA',       2, 10,  5.0, 1, 'seed', SYSDATETIMEOFFSET());
END

-- ---------------------------------------------------------------------
-- 8. BOM: Work Centers + BOM Headers (parent = ITEM-1001, comps = X/Y)
-- ---------------------------------------------------------------------
DECLARE @wc1 UNIQUEIDENTIFIER = '80000000-0000-0000-0009-000000000001';
IF NOT EXISTS (SELECT 1 FROM bom.WorkCenters WHERE CompanyId = @co AND Code = 'WC-ASSY')
BEGIN
    INSERT INTO bom.WorkCenters (Id, CompanyId, Code, Name, Department, CapacityHoursPerDay, EfficiencyPercentage, CostRatePerHour, IsActive, CreatedBy, CreatedOn) VALUES
    (@wc1, @co, 'WC-ASSY', 'Assembly', 'Production', 16.0, 95.0, 45.00, 1, 'seed', SYSDATETIMEOFFSET()),
    ('80000000-0000-0000-0009-000000000002', @co, 'WC-PACK', 'Packaging', 'Production', 16.0, 98.0, 30.00, 1, 'seed', SYSDATETIMEOFFSET());
END
SET @itemA = ISNULL((SELECT Id FROM inv.Items WHERE CompanyId = @co AND ItemCode = 'ITEM-1001'), @itemA);
SET @itemB = ISNULL((SELECT Id FROM inv.Items WHERE CompanyId = @co AND ItemCode = 'ITEM-1002'), @itemB);
SET @itemC = ISNULL((SELECT Id FROM inv.Items WHERE CompanyId = @co AND ItemCode = 'ITEM-1003'), @itemC);
SET @itemD = ISNULL((SELECT Id FROM inv.Items WHERE CompanyId = @co AND ItemCode = 'ITEM-1004'), @itemD);
SET @uomEa = ISNULL((SELECT Id FROM inv.UnitOfMeasures WHERE CompanyId = @co AND Code = 'EA'), @uomEa);
SET @uomLb = ISNULL((SELECT Id FROM inv.UnitOfMeasures WHERE CompanyId = @co AND Code = 'LB'), @uomLb);
SET @catProd = ISNULL((SELECT Id FROM inv.ItemCategories WHERE CompanyId = @co AND CategoryCode = 'PROD'), @catProd);
SET @catComp = ISNULL((SELECT Id FROM inv.ItemCategories WHERE CompanyId = @co AND CategoryCode = 'COMP'), @catComp);
SET @wc1 = ISNULL((SELECT Id FROM bom.WorkCenters WHERE CompanyId = @co AND Code = 'WC-ASSY'), @wc1);

DECLARE @bom1 UNIQUEIDENTIFIER = '81000000-0000-0000-0009-000000000001';
IF NOT EXISTS (SELECT 1 FROM bom.BomHeaders WHERE CompanyId = @co AND Revision = 'A' AND ParentItemId = (SELECT Id FROM inv.Items WHERE CompanyId = @co AND ItemCode = 'ITEM-1001'))
BEGIN
    INSERT INTO bom.BomHeaders (Id, CompanyId, ParentItemId, Revision, Description, BomType, Status, YieldPercentage, EstimatedMaterialCost, EstimatedLaborCost, EstimatedOverheadCost, CreatedBy, CreatedOn) VALUES
    (@bom1, @co, @itemA, 'A', 'Widget A assembly', 0, 0, 100.0, 30.50, 12.00, 4.00, 'seed', SYSDATETIMEOFFSET()),
    ('81000000-0000-0000-0009-000000000002', @co, @itemB, 'A', 'Widget B assembly', 0, 0, 100.0, 36.00, 14.00, 5.00, 'seed', SYSDATETIMEOFFSET());
END
SET @bom1 = ISNULL((SELECT Id FROM bom.BomHeaders WHERE CompanyId = @co AND Revision = 'A' AND ParentItemId = (SELECT Id FROM inv.Items WHERE CompanyId = @co AND ItemCode = 'ITEM-1001')), @bom1);

-- Component lines
IF NOT EXISTS (SELECT 1 FROM bom.BomComponentLines WHERE BomHeaderId = @bom1)
BEGIN
    INSERT INTO bom.BomComponentLines (Id, BomHeaderId, ComponentItemId, QuantityPerParent, UnitOfMeasure, ScrapFactor, OperationSequence, WorkCenterId, IsPhantom, IsCritical, EstimatedUnitCost, CreatedBy, CreatedOn) VALUES
    (NEWID(), @bom1, @itemC, 2.0, 'EA', 2.0, 10, @wc1, 0, 0, 12.50, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @bom1, @itemD, 1.0, 'EA', 1.0, 20, @wc1, 0, 0, 18.00, 'seed', SYSDATETIMEOFFSET());
END

-- ---------------------------------------------------------------------
-- 9. PROJECT: Projects + Tasks
-- ---------------------------------------------------------------------
-- Repair-safe: unconditionally relink PRJ-001/PRJ-002 to the per-company
-- C1001/C1002 customers (re-resolved by code) so legacy fixed-GUID links
-- left by older seed versions are corrected on every run.
SET @custAcme = (SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C1001');
IF @custAcme IS NULL SET @custAcme = (SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C-1001');
SET @custGlob = (SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C1002');
IF @custGlob IS NULL SET @custGlob = (SELECT Id FROM ar.Customers WHERE CompanyId = @co AND CustomerId = 'C-1002');
UPDATE proj.Projects SET CustomerId = @custAcme WHERE CompanyId = @co AND ProjectCode = 'PRJ-001' AND @custAcme IS NOT NULL;
UPDATE proj.Projects SET CustomerId = @custGlob WHERE CompanyId = @co AND ProjectCode = 'PRJ-002' AND @custGlob IS NOT NULL;

DECLARE @proj1 UNIQUEIDENTIFIER = '90000000-0000-0000-000A-000000000001';
IF NOT EXISTS (SELECT 1 FROM proj.Projects WHERE ProjectCode = 'PRJ-001')
BEGIN
    INSERT INTO proj.Projects (Id, CompanyId, ProjectCode, Name, Description, ProjectType, Status, CustomerId, ProjectManager, ContractValue, OriginalBudget, RevisedBudget, CostsToDate, RevenueToDate, PercentComplete, RetainagePercentage, RetainageHeld, IsBilled, IsClosed, ContingencyAmount, ReleasedContingency, ExchangeRate, BillingHold, AccountingMethod, AccruedLoss, CreatedBy, CreatedOn) VALUES
    (@proj1, @co, 'PRJ-001', 'Office Tower Build', 'New HQ construction', 0, 0, @custAcme, 'Jane PM', 1500000.00, 1500000.00, 1500000.00, 0, 0, 0.0, 5.0, 0, 0, 0, 0.0, 0.0, 1.0, 0, 0, 0.0, 'seed', SYSDATETIMEOFFSET()),
    ('90000000-0000-0000-000A-000000000002', @co, 'PRJ-002', 'Warehouse Fit-out', 'Tenant improvement', 0, 0, @custGlob, 'Bob PM', 400000.00,  400000.00,  400000.00,  0, 0, 0.0, 5.0, 0, 0, 0, 0.0, 0.0, 1.0, 0, 0, 0.0, 'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM proj.ProjectTasks WHERE TaskCode = 'T1')
BEGIN
    INSERT INTO proj.ProjectTasks (Id, ProjectId, TaskCode, Description, BudgetedHours, BudgetedCost, ActualHours, ActualCost, PercentComplete, SortOrder, CreatedBy, CreatedOn) VALUES
    (NEWID(), @proj1, 'T1', 'Site Prep',      200.0, 40000.00, 0, 0, 0.0, 10, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @proj1, 'T2', 'Foundation',     400.0, 90000.00, 0, 0, 0.0, 20, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @proj1, 'T3', 'Superstructure', 600.0, 180000.00,0, 0, 0.0, 30, 'seed', SYSDATETIMEOFFSET());
END

-- ---------------------------------------------------------------------
-- 10. PAYROLL: Employees + Pay Codes + Employee Pay Codes
-- ---------------------------------------------------------------------
DECLARE @payReg UNIQUEIDENTIFIER = 'B0000000-0000-0000-000B-000000000001';
DECLARE @payOT  UNIQUEIDENTIFIER = 'B0000000-0000-0000-000B-000000000002';
DECLARE @emp1  UNIQUEIDENTIFIER = 'B1000000-0000-0000-000B-000000000001';
DECLARE @emp2  UNIQUEIDENTIFIER = 'B1000000-0000-0000-000B-000000000002';

IF NOT EXISTS (SELECT 1 FROM pay.PayCodes WHERE CompanyId = @co AND Code = 'REG')
BEGIN
    INSERT INTO pay.PayCodes (Id, CompanyId, Code, Description, Type, GlAccountNumber, IsOvertime, CountsAsHoursWorked, CreatedBy, CreatedOn) VALUES
    (@payReg, @co, 'REG', 'Regular Wages', 0, '6000', 0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@payOT,  @co, 'OT',  'Overtime Wages',0, '6000', 1, 1, 'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM pay.Employees WHERE Id = @emp1 OR EmployeeCode = 'E0001')
BEGIN
    INSERT INTO pay.Employees (Id, CompanyId, EmployeeCode, FirstName, LastName, EmploymentType, Status, HireDate, Email, DefaultProjectId, AllocationPercentage, IsBillable, CreatedBy, CreatedOn) VALUES
    (@emp1, @co, 'E0001', 'Alice', 'Nguyen',  0, 0, '2024-01-15T00:00:00Z', 'alice@erp.com',  @proj1, 100.0, 1, 'seed', SYSDATETIMEOFFSET()),
    (@emp2, @co, 'E0002', 'Bob',   'Martinez',0, 0, '2024-03-01T00:00:00Z', 'bob@erp.com',    @proj1, 100.0, 1, 'seed', SYSDATETIMEOFFSET()),
    ('B1000000-0000-0000-000B-000000000003', @co, 'E0003', 'Carol', 'Singh', 0, 0, '2023-11-20T00:00:00Z', 'carol@erp.com', @proj1, 100.0, 1, 'seed', SYSDATETIMEOFFSET());

    -- link employees to payroll pay codes
    INSERT INTO pay.EmployeePayCodes (Id, EmployeeId, PayCodeId, OverrideRate, IsBillable, CreatedBy, CreatedOn) VALUES
    (NEWID(), @emp1, @payReg, 45.00, 1, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @emp1, @payOT,  67.50, 1, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @emp2, @payReg, 52.00, 1, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @emp2, @payOT,  78.00, 1, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), 'B1000000-0000-0000-000B-000000000003', @payReg, 38.00, 1, 'seed', SYSDATETIMEOFFSET());
END
SET @payReg = ISNULL((SELECT Id FROM pay.PayCodes WHERE CompanyId = @co AND Code = 'REG'), @payReg);

-- ---------------------------------------------------------------------
-- 11. FIELD SERVICE: Territories, SLAs, Rate Cards, Skills, Technicians,
--     Equipment, Service Contracts
-- ---------------------------------------------------------------------
DECLARE @ster1 UNIQUEIDENTIFIER = 'C0000000-0000-0000-000C-000000000001';
IF NOT EXISTS (SELECT 1 FROM fs.ServiceTerritories WHERE CompanyId = @co AND Code = 'ST-EAST')
BEGIN
    INSERT INTO fs.ServiceTerritories (Id, CompanyId, Code, Name, Region, ZipCoverage, TravelCostPerMile, CreatedBy, CreatedOn) VALUES
    (@ster1, @co, 'ST-EAST', 'East Service Territory', 'East', '10001-11999', 0.65, 'seed', SYSDATETIMEOFFSET()),
    ('C0000000-0000-0000-000C-000000000002', @co, 'ST-WEST', 'West Service Territory', 'West', '90001-99999', 0.70, 'seed', SYSDATETIMEOFFSET());
END
SET @ster1 = ISNULL((SELECT Id FROM fs.ServiceTerritories WHERE CompanyId = @co AND Code = 'ST-EAST'), @ster1);

IF NOT EXISTS (SELECT 1 FROM fs.SlaDefinitions WHERE Name = 'Standard Response')
BEGIN
    INSERT INTO fs.SlaDefinitions (Id, CompanyId, Name, Priority, ResponseMinutes, ResolutionMinutes, Escalate, CreatedBy, CreatedOn) VALUES
    (NEWID(), @co, 'Standard Response', 1, 240, 1440, 0, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @co, 'Critical Response', 0, 60,  480,  1,     'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM fs.ServiceRateCards WHERE Name = 'Standard Rates 2026')
BEGIN
    INSERT INTO fs.ServiceRateCards (Id, CompanyId, Name, EffectiveDate, IsActive, LaborRatePerHour, OvertimeRatePerHour, TripCharge, PartsMarkupPercent, CreatedBy, CreatedOn) VALUES
    (NEWID(), @co, 'Standard Rates 2026', '2026-01-01T00:00:00Z', 1, 85.00, 127.50, 50.00, 15.0, 'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM fs.SkillCertifications WHERE Code = 'ELEC')
BEGIN
    INSERT INTO fs.SkillCertifications (Id, CompanyId, Code, Name, Category, Description, CreatedBy, CreatedOn) VALUES
    (NEWID(), @co, 'ELEC', 'Electrical',    'Trade', 'Licensed electrician', 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @co, 'PLMB', 'Plumbing',      'Trade', 'Licensed plumber',     'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @co, 'HVAC', 'HVAC',          'Trade', 'HVAC certified',      'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM fs.Technicians WHERE Code = 'TECH-01')
BEGIN
    INSERT INTO fs.Technicians (Id, CompanyId, EmployeeId, Code, FirstName, LastName, DefaultTerritoryId, Status, Email, Phone, HourlyRate, CreatedBy, CreatedOn) VALUES
    (NEWID(), @co, @emp1, 'TECH-01', 'Alice', 'Nguyen',   @ster1, 0, 'alice@erp.com',  '555-0101', 85.00, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @co, @emp2, 'TECH-02', 'Bob',   'Martinez', @ster1, 0, 'bob@erp.com',    '555-0102', 85.00, 'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM fs.EquipmentAssets WHERE AssetTag = 'EQ-001')
BEGIN
    INSERT INTO fs.EquipmentAssets (Id, CompanyId, AssetTag, SerialNumber, Description, ItemId, CustomerId, Ownership, UnderWarranty, CreatedBy, CreatedOn) VALUES
    (NEWID(), @co, 'EQ-001', 'SN-1001', 'Rooftop HVAC Unit A', @itemA, @custAcme, 1, 1, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @co, 'EQ-002', 'SN-1002', 'Rooftop HVAC Unit B', @itemA, @custGlob, 1, 0, 'seed', SYSDATETIMEOFFSET());
END

IF NOT EXISTS (SELECT 1 FROM fs.ServiceContracts WHERE ContractNumber = 'SC-001')
BEGIN
    INSERT INTO fs.ServiceContracts (Id, CompanyId, ContractNumber, Name, CustomerId, StartDate, EndDate, Status, BillingType, ContractValue, IncludesWarranty, WarrantyMonths, CreatedBy, CreatedOn) VALUES
    (NEWID(), @co, 'SC-001', 'Acme Annual Maintenance', @custAcme, '2026-01-01T00:00:00Z', '2026-12-31T00:00:00Z', 0, 0, 24000.00, 1, 12, 'seed', SYSDATETIMEOFFSET()),
    (NEWID(), @co, 'SC-002', 'Globex Full-Service',     @custGlob, '2026-01-01T00:00:00Z', '2026-12-31T00:00:00Z', 0, 1, 60000.00, 1, 12, 'seed', SYSDATETIMEOFFSET());
END

PRINT 'Master seed complete. All data under company 11111111-1111-1111-1111-111111111111.';
