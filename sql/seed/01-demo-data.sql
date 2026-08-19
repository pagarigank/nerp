-- =====================================================================
-- ERP Demo/Seed Data
-- Populates Platform, GL, AP, and AR tables so every Phase 0-4 screen
-- shows records. Run after schema migrations have been applied.
--
-- Run:
--   sqlcmd -S localhost -U sa -P P@ssw0rd -C -d erp -b -i sql/seed/01-demo-data.sql
--
-- Notes:
--   * All timestamps are UTC.
--   * gl.Account mirrors platform.Accounts (same GUIDs) because the
--     gl.JournalEntryLines.AccountId FK still references gl.Account.
--   * Deterministic GUIDs keep the script idempotent and readable.
-- =====================================================================

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

-- ---------------------------------------------------------------------
-- Idempotency guard: if the seed marker currency already exists, skip.
-- ---------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM platform.Currencies WHERE Id = '10000000-0000-0000-0000-000000000001')
BEGIN
    PRINT 'Demo data already present; skipping.';
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    RETURN;
END
GO

-- ---------------------------------------------------------------------
-- 1. PLATFORM: Currencies
-- ---------------------------------------------------------------------
INSERT INTO platform.Currencies (Id, Code, Name, Symbol, DecimalPlaces, IsActive, CreatedBy, CreatedOn) VALUES
 ('10000000-0000-0000-0000-000000000001', 'USD', 'US Dollar',          '$', 2, 1, 'seed', SYSDATETIMEOFFSET()),
 ('10000000-0000-0000-0000-000000000002', 'EUR', 'Euro',               '€', 2, 1, 'seed', SYSDATETIMEOFFSET()),
 ('10000000-0000-0000-0000-000000000003', 'GBP', 'British Pound',      '£', 2, 1, 'seed', SYSDATETIMEOFFSET()),
 ('10000000-0000-0000-0000-000000000004', 'CAD', 'Canadian Dollar',    'C$', 2, 1, 'seed', SYSDATETIMEOFFSET());
GO

-- ---------------------------------------------------------------------
-- 2. PLATFORM: Exchange Rates (base USD, effective 2026-07-01)
-- ---------------------------------------------------------------------
INSERT INTO platform.ExchangeRates (Id, CompanyId, FromCurrency, ToCurrency, Rate, EffectiveDate, CreatedBy, CreatedOn) VALUES
 ('70000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'USD', 'EUR', 0.9200, '2026-07-01T00:00:00Z', 'seed', SYSDATETIMEOFFSET()),
 ('70000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'USD', 'GBP', 0.7900, '2026-07-01T00:00:00Z', 'seed', SYSDATETIMEOFFSET()),
 ('70000000-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'USD', 'CAD', 1.3600, '2026-07-01T00:00:00Z', 'seed', SYSDATETIMEOFFSET()),
 ('70000000-0000-0000-0000-000000000004', '11111111-1111-1111-1111-111111111111', 'EUR', 'USD', 1.0870, '2026-07-01T00:00:00Z', 'seed', SYSDATETIMEOFFSET());
GO

-- ---------------------------------------------------------------------
-- 3. PLATFORM: Fiscal Years
-- ---------------------------------------------------------------------
INSERT INTO platform.FiscalYears (Id, CompanyId, Year, Description, StartDate, EndDate, IsClosed, CreatedBy, CreatedOn) VALUES
 ('20000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 2025, 'Fiscal Year 2025', '2025-01-01T00:00:00Z', '2025-12-31T00:00:00Z', 1, 'seed', SYSDATETIMEOFFSET()),
 ('20000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 2026, 'Fiscal Year 2026', '2026-01-01T00:00:00Z', '2026-12-31T00:00:00Z', 0, 'seed', SYSDATETIMEOFFSET());
GO

-- ---------------------------------------------------------------------
-- 4. PLATFORM: Fiscal Periods (12 per year)
--    Status: Open = 0, Closed = 1, Locked = 2
--    2025 -> all Closed; 2026 -> P1-P6 Closed, P7-P12 Open
-- ---------------------------------------------------------------------
DECLARE @companyId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @fy2025 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @fy2026 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';

DECLARE @periods TABLE (Id UNIQUEIDENTIFIER, FiscalYearId UNIQUEIDENTIFIER, YearNo INT, PeriodNumber INT, Description NVARCHAR(200), StartDate DATETIMEOFFSET, EndDate DATETIMEOFFSET, Status INT);

WITH m AS (
    SELECT 1 AS n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6
    UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12
)
INSERT INTO @periods (Id, FiscalYearId, YearNo, PeriodNumber, Description, StartDate, EndDate, Status)
SELECT
    CONVERT(UNIQUEIDENTIFIER, '21000000-0000-0000-0000-00000000' + RIGHT('00' + CONVERT(VARCHAR(2), m.n), 2) + '25'),
    @fy2025, 2025, m.n,
    DATENAME(MONTH, DATEFROMPARTS(2025, m.n, 1)) + ' 2025',
    CONVERT(DATETIMEOFFSET, CONVERT(VARCHAR(10), DATEFROMPARTS(2025, m.n, 1)) + 'T00:00:00Z'),
    CONVERT(DATETIMEOFFSET, CONVERT(VARCHAR(10), EOMONTH(DATEFROMPARTS(2025, m.n, 1))) + 'T00:00:00Z'),
    1
FROM m
UNION ALL
SELECT
    CONVERT(UNIQUEIDENTIFIER, '21000000-0000-0000-0000-00000000' + RIGHT('00' + CONVERT(VARCHAR(2), m.n), 2) + '26'),
    @fy2026, 2026, m.n,
    DATENAME(MONTH, DATEFROMPARTS(2026, m.n, 1)) + ' 2026',
    CONVERT(DATETIMEOFFSET, CONVERT(VARCHAR(10), DATEFROMPARTS(2026, m.n, 1)) + 'T00:00:00Z'),
    CONVERT(DATETIMEOFFSET, CONVERT(VARCHAR(10), EOMONTH(DATEFROMPARTS(2026, m.n, 1))) + 'T00:00:00Z'),
    CASE WHEN m.n <= 6 THEN 1 ELSE 0 END
FROM m;

INSERT INTO platform.FiscalPeriods (Id, FiscalYearId, CompanyId, PeriodNumber, Description, StartDate, EndDate, Status, CreatedBy, CreatedOn)
SELECT Id, FiscalYearId, @companyId, PeriodNumber, Description, StartDate, EndDate, Status, 'seed', SYSDATETIMEOFFSET()
FROM @periods;
GO

-- ---------------------------------------------------------------------
-- 5. PLATFORM: Chart of Accounts (also mirrored into gl.Account below)
--    AccountType: Asset=0, Liability=1, Equity=2, Revenue=3, Expense=4
--    NormalBalance: Debit=0, Credit=1
-- ---------------------------------------------------------------------
DECLARE @companyId2 UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';

INSERT INTO platform.Accounts (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedBy, CreatedOn) VALUES
 ('30000000-0000-0000-0000-000000000001', @companyId2, '1000', 'Cash',                      0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-000000000002', @companyId2, '1200', 'Accounts Receivable',       0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-000000000003', @companyId2, '1300', 'Inventory',                 0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-000000000004', @companyId2, '1500', 'Equipment',                 0, 0, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-000000000005', @companyId2, '1700', 'Accumulated Depreciation',  0, 1, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-000000000006', @companyId2, '2000', 'Accounts Payable',          1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-000000000007', @companyId2, '2100', 'Accrued Expenses',          1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-000000000008', @companyId2, '2200', 'Payroll Liabilities',       1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-000000000009', @companyId2, '3000', 'Retained Earnings',         2, 1, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-00000000000A', @companyId2, '3100', 'Owner''s Equity',          2, 1, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-00000000000B', @companyId2, '4000', 'Revenue',                   3, 1, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-00000000000C', @companyId2, '5000', 'Cost of Goods Sold',        4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-00000000000D', @companyId2, '6000', 'Salaries & Wages',          4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-00000000000E', @companyId2, '6100', 'Rent',                      4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-00000000000F', @companyId2, '6200', 'Utilities',                 4, 0, 1, 'seed', SYSDATETIMEOFFSET()),
 ('30000000-0000-0000-0000-000000000010', @companyId2, '7000', 'Other Expense',             4, 0, 1, 'seed', SYSDATETIMEOFFSET());
GO

-- ---------------------------------------------------------------------
-- 6. GL: Account (mirror of platform.Accounts, same GUIDs, FK target)
-- ---------------------------------------------------------------------
INSERT INTO gl.Account (Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedBy, CreatedOn)
SELECT Id, CompanyId, AccountNumber, Description, AccountType, NormalBalance, IsActive, CreatedBy, CreatedOn
FROM platform.Accounts;
GO

-- ---------------------------------------------------------------------
-- 7. PLATFORM: Segment Types + Segment Values
-- ---------------------------------------------------------------------
INSERT INTO platform.SegmentTypes (Id, CompanyId, Name, Code, DisplayOrder, IsRequired, IsActive, CreatedBy, CreatedOn) VALUES
 ('40000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'Department', 'DEPT', 1, 1, 1, 'seed', SYSDATETIMEOFFSET()),
 ('40000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'Project',    'PROJ', 2, 0, 1, 'seed', SYSDATETIMEOFFSET()),
 ('40000000-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'Location',   'LOC',  3, 0, 1, 'seed', SYSDATETIMEOFFSET());
GO

INSERT INTO platform.SegmentValues (Id, SegmentTypeId, CompanyId, Value, Description, DisplayOrder, IsActive, CreatedBy, CreatedOn) VALUES
 ('41000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'SALES',  'Sales & Marketing', 1, 1, 'seed', SYSDATETIMEOFFSET()),
 ('41000000-0000-0000-0000-000000000002', '40000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'MKT',    'Marketing',        2, 1, 'seed', SYSDATETIMEOFFSET()),
 ('41000000-0000-0000-0000-000000000003', '40000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'OPS',    'Operations',       3, 1, 'seed', SYSDATETIMEOFFSET()),
 ('41000000-0000-0000-0000-000000000004', '40000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'FIN',    'Finance',          4, 1, 'seed', SYSDATETIMEOFFSET()),
 ('41000000-0000-0000-0000-000000000005', '40000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'HR',     'Human Resources',  5, 1, 'seed', SYSDATETIMEOFFSET()),
 ('41000000-0000-0000-0000-000000000006', '40000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'PRJ-0001', 'Downtown HQ Renovation',      1, 1, 'seed', SYSDATETIMEOFFSET()),
 ('41000000-0000-0000-0000-000000000007', '40000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'PRJ-0002', 'Bridge Replacement',           2, 1, 'seed', SYSDATETIMEOFFSET()),
 ('41000000-0000-0000-0000-000000000008', '40000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'PRJ-0003', 'Water Treatment Upgrade',      3, 1, 'seed', SYSDATETIMEOFFSET()),
 ('41000000-0000-0000-0000-000000000009', '40000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'ADMIN',    'General & Administrative',     4, 1, 'seed', SYSDATETIMEOFFSET()),
 ('41000000-0000-0000-0000-00000000000A', '40000000-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'CHI',  'Chicago',    1, 1, 'seed', SYSDATETIMEOFFSET()),
 ('41000000-0000-0000-0000-00000000000B', '40000000-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'NYC',  'New York',   2, 1, 'seed', SYSDATETIMEOFFSET()),
 ('41000000-0000-0000-0000-00000000000C', '40000000-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'AUS',  'Austin',     3, 1, 'seed', SYSDATETIMEOFFSET());
GO

-- ---------------------------------------------------------------------
-- 8. PLATFORM: Users, Roles, Permissions, and links
-- ---------------------------------------------------------------------
INSERT INTO platform.Users (Id, Username, Email, DisplayName, PhoneNumber, IsActive, CreatedBy, CreatedOn) VALUES
 ('50000000-0000-0000-0000-000000000001', 'admin',     'admin@erp.com',     'System Administrator', '555-0100', 1, 'seed', SYSDATETIMEOFFSET()),
 ('50000000-0000-0000-0000-000000000002', 'jdoe',      'jdoe@erp.com',      'John Doe',             '555-0101', 1, 'seed', SYSDATETIMEOFFSET()),
 ('50000000-0000-0000-0000-000000000003', 'asmith',    'asmith@erp.com',    'Amy Smith',            '555-0102', 1, 'seed', SYSDATETIMEOFFSET()),
 ('50000000-0000-0000-0000-000000000004', 'rjohnson',  'rjohnson@erp.com',  'Robert Johnson',       '555-0103', 1, 'seed', SYSDATETIMEOFFSET()),
 ('50000000-0000-0000-0000-000000000005', 'mchen',     'mchen@erp.com',     'Mei Chen',             '555-0104', 1, 'seed', SYSDATETIMEOFFSET());
GO

INSERT INTO platform.Roles (Id, Name, Description, IsActive, CreatedBy, CreatedOn) VALUES
 ('51000000-0000-0000-0000-000000000001', 'Administrator', 'Full system access',                    1, 'seed', SYSDATETIMEOFFSET()),
 ('51000000-0000-0000-0000-000000000002', 'Accountant',    'General ledger and reporting',         1, 'seed', SYSDATETIMEOFFSET()),
 ('51000000-0000-0000-0000-000000000003', 'AP Clerk',      'Accounts payable entry and payments',  1, 'seed', SYSDATETIMEOFFSET()),
 ('51000000-0000-0000-0000-000000000004', 'AR Clerk',      'Accounts receivable and cash receipts',1, 'seed', SYSDATETIMEOFFSET()),
 ('51000000-0000-0000-0000-000000000005', 'Controller',    'Financial controller',                 1, 'seed', SYSDATETIMEOFFSET());
GO

INSERT INTO platform.UserRoles (Id, UserId, RoleId) VALUES
 ('52000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', '51000000-0000-0000-0000-000000000001'),
 ('52000000-0000-0000-0000-000000000002', '50000000-0000-0000-0000-000000000005', '51000000-0000-0000-0000-000000000005'),
 ('52000000-0000-0000-0000-000000000003', '50000000-0000-0000-0000-000000000002', '51000000-0000-0000-0000-000000000002'),
 ('52000000-0000-0000-0000-000000000004', '50000000-0000-0000-0000-000000000003', '51000000-0000-0000-0000-000000000003'),
 ('52000000-0000-0000-0000-000000000005', '50000000-0000-0000-0000-000000000004', '51000000-0000-0000-0000-000000000004');
GO

INSERT INTO platform.Permissions (Id, Module, Action, Description) VALUES
 ('53000000-0000-0000-0000-000000000001', 'Platform', 'View',   'View platform master data'),
 ('53000000-0000-0000-0000-000000000002', 'Platform', 'Admin',  'Administer platform configuration'),
 ('53000000-0000-0000-0000-000000000003', 'GL',       'View',   'View general ledger'),
 ('53000000-0000-0000-0000-000000000004', 'GL',       'Post',   'Post journal batches'),
 ('53000000-0000-0000-0000-000000000005', 'AP',       'View',   'View accounts payable'),
 ('53000000-0000-0000-0000-000000000006', 'AP',       'Post',   'Post vouchers'),
 ('53000000-0000-0000-0000-000000000007', 'AP',       'Pay',    'Issue payments'),
 ('53000000-0000-0000-0000-000000000008', 'AR',       'View',   'View accounts receivable'),
 ('53000000-0000-0000-0000-000000000009', 'AR',       'Post',   'Post invoices'),
 ('53000000-0000-0000-0000-00000000000A', 'AR',       'Apply',  'Apply cash receipts');
GO

INSERT INTO platform.RolePermissions (Id, RoleId, PermissionId) VALUES
 ('54000000-0000-0000-0000-000000000001', '51000000-0000-0000-0000-000000000001', '53000000-0000-0000-0000-000000000001'),
 ('54000000-0000-0000-0000-000000000002', '51000000-0000-0000-0000-000000000001', '53000000-0000-0000-0000-000000000002'),
 ('54000000-0000-0000-0000-000000000003', '51000000-0000-0000-0000-000000000001', '53000000-0000-0000-0000-000000000003'),
 ('54000000-0000-0000-0000-000000000004', '51000000-0000-0000-0000-000000000001', '53000000-0000-0000-0000-000000000004'),
 ('54000000-0000-0000-0000-000000000005', '51000000-0000-0000-0000-000000000001', '53000000-0000-0000-0000-000000000005'),
 ('54000000-0000-0000-0000-000000000006', '51000000-0000-0000-0000-000000000001', '53000000-0000-0000-0000-000000000006'),
 ('54000000-0000-0000-0000-000000000007', '51000000-0000-0000-0000-000000000001', '53000000-0000-0000-0000-000000000007'),
 ('54000000-0000-0000-0000-000000000008', '51000000-0000-0000-0000-000000000001', '53000000-0000-0000-0000-000000000008'),
 ('54000000-0000-0000-0000-000000000009', '51000000-0000-0000-0000-000000000001', '53000000-0000-0000-0000-000000000009'),
 ('54000000-0000-0000-0000-00000000000A', '51000000-0000-0000-0000-000000000001', '53000000-0000-0000-0000-00000000000A'),
 ('54000000-0000-0000-0000-00000000000B', '51000000-0000-0000-0000-000000000005', '53000000-0000-0000-0000-000000000003'),
 ('54000000-0000-0000-0000-00000000000C', '51000000-0000-0000-0000-000000000005', '53000000-0000-0000-0000-000000000004'),
 ('54000000-0000-0000-0000-00000000000D', '51000000-0000-0000-0000-000000000002', '53000000-0000-0000-0000-000000000003'),
 ('54000000-0000-0000-0000-00000000000E', '51000000-0000-0000-0000-000000000003', '53000000-0000-0000-0000-000000000005'),
 ('54000000-0000-0000-0000-00000000000F', '51000000-0000-0000-0000-000000000003', '53000000-0000-0000-0000-000000000006'),
 ('54000000-0000-0000-0000-000000000010', '51000000-0000-0000-0000-000000000003', '53000000-0000-0000-0000-000000000007'),
 ('54000000-0000-0000-0000-000000000011', '51000000-0000-0000-0000-000000000004', '53000000-0000-0000-0000-000000000008'),
 ('54000000-0000-0000-0000-000000000012', '51000000-0000-0000-0000-000000000004', '53000000-0000-0000-0000-000000000009'),
 ('54000000-0000-0000-0000-000000000013', '51000000-0000-0000-0000-000000000004', '53000000-0000-0000-0000-00000000000A');
GO

-- ---------------------------------------------------------------------
-- 9. PLATFORM: Number Sequences
-- ---------------------------------------------------------------------
INSERT INTO platform.NumberSequences (Id, CompanyId, Name, Prefix, NextValue, Increment, MinValue, MaxValue, IsActive, CreatedBy, CreatedOn) VALUES
 ('60000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'GL Batch',      'GL-',  1, 1, 1, 999999, 1, 'seed', SYSDATETIMEOFFSET()),
 ('60000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'AP Voucher Batch', 'AP-', 1, 1, 1, 999999, 1, 'seed', SYSDATETIMEOFFSET()),
 ('60000000-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'AR Invoice Batch', 'AR-', 1, 1, 1, 999999, 1, 'seed', SYSDATETIMEOFFSET()),
 ('60000000-0000-0000-0000-000000000004', '11111111-1111-1111-1111-111111111111', 'Vendor',        'V-',    1, 1, 1, 999999, 1, 'seed', SYSDATETIMEOFFSET()),
 ('60000000-0000-0000-0000-000000000005', '11111111-1111-1111-1111-111111111111', 'Customer',      'C-',    1, 1, 1, 999999, 1, 'seed', SYSDATETIMEOFFSET()),
 ('60000000-0000-0000-0000-000000000006', '11111111-1111-1111-1111-111111111111', 'Payment',       'PMT-',  1, 1, 1, 999999, 1, 'seed', SYSDATETIMEOFFSET()),
 ('60000000-0000-0000-0000-000000000007', '11111111-1111-1111-1111-111111111111', 'Cash Receipt',  'RCPT-', 1, 1, 1, 999999, 1, 'seed', SYSDATETIMEOFFSET()),
 ('60000000-0000-0000-0000-000000000008', '11111111-1111-1111-1111-111111111111', 'Voucher',       'VCH-',  1, 1, 1, 999999, 1, 'seed', SYSDATETIMEOFFSET());
GO

-- ---------------------------------------------------------------------
-- 10. AP: Payment Terms
-- ---------------------------------------------------------------------
INSERT INTO ap.PaymentTerms (Id, Name, DueDays, DiscountDays, DiscountPercent, IsActive, CreatedBy, CreatedOn) VALUES
 ('90000000-0000-0000-0000-000000000001', 'Net 30',        30,  0,  0.0, 1, 'seed', SYSDATETIMEOFFSET()),
 ('90000000-0000-0000-0000-000000000002', 'Net 15',        15,  0,  0.0, 1, 'seed', SYSDATETIMEOFFSET()),
 ('90000000-0000-0000-0000-000000000003', '2/10 Net 30',   30, 10,  2.0, 1, 'seed', SYSDATETIMEOFFSET()),
 ('90000000-0000-0000-0000-000000000004', 'Due on Receipt',  0,  0,  0.0, 1, 'seed', SYSDATETIMEOFFSET());
GO

-- ---------------------------------------------------------------------
-- 11. AP: Vendors + Bank Accounts
--    Form1099Category: None=0, IndependentContractor=1, Rent=2,
--                       Royalties=3, NonEmployeeCompensation=4,
--                       MedicalAndHealth=5, Attorney=6, Other=99
-- ---------------------------------------------------------------------
INSERT INTO ap.Vendors (Id, VendorId, Name, LegalName, TaxId, Form1099Category, DefaultPaymentTermId, IsActive, BackupWithholdingFlag, BackupWithholdingRate, CreatedBy, CreatedOn) VALUES
 ('91000000-0000-0000-0000-000000000001', 'V-1001', 'Acme Construction Supply',     'Acme Construction Supply Inc.',   '12-3456789', 0,  '90000000-0000-0000-0000-000000000001', 1, 0, 0.0,   'seed', SYSDATETIMEOFFSET()),
 ('91000000-0000-0000-0000-000000000002', 'V-1002', 'Smith Electrical Services',    'Smith Electrical Services LLC',   '98-7654321', 1,  '90000000-0000-0000-0000-000000000002', 1, 1, 0.24,  'seed', SYSDATETIMEOFFSET()),
 ('91000000-0000-0000-0000-000000000003', 'V-1003', 'Midwest Rentals LLC',          'Midwest Equipment Rentals LLC',   '55-1212121', 2,  '90000000-0000-0000-0000-000000000001', 1, 0, 0.0,   'seed', SYSDATETIMEOFFSET()),
 ('91000000-0000-0000-0000-000000000004', 'V-1004', 'Office Depot',                 'Office Depot, Inc.',              '34-5678901', 0,  '90000000-0000-0000-0000-000000000003', 1, 0, 0.0,   'seed', SYSDATETIMEOFFSET()),
 ('91000000-0000-0000-0000-000000000005', 'V-1005', 'Pinnacle Consulting Group',    'Pinnacle Consulting Group LLC',   '76-5432109', 99, '90000000-0000-0000-0000-000000000001', 1, 0, 0.0,   'seed', SYSDATETIMEOFFSET());
GO

INSERT INTO ap.VendorBankAccounts (Id, VendorId, BankName, AccountNumber, RoutingNumber, IsDefault) VALUES
 ('92000000-0000-0000-0000-000000000001', '91000000-0000-0000-0000-000000000001', 'Chase',        '1234567890123', '071000013', 1),
 ('92000000-0000-0000-0000-000000000002', '91000000-0000-0000-0000-000000000002', 'Bank of America', '2345678901234', '021000322', 1),
 ('92000000-0000-0000-0000-000000000003', '91000000-0000-0000-0000-000000000003', 'Wells Fargo',  '3456789012345', '121000248', 1),
 ('92000000-0000-0000-0000-000000000004', '91000000-0000-0000-0000-000000000004', 'Chase',        '4567890123456', '071000013', 1),
 ('92000000-0000-0000-0000-000000000005', '91000000-0000-0000-0000-000000000005', 'Citi',         '5678901234567', '021000089', 1);
GO

-- ---------------------------------------------------------------------
-- 12. AP: Voucher Batches + Vouchers + Distributions
--    VoucherBatchStatus: Draft=0, Batched=1, Posted=2, Reversed=3
--    VoucherType: Invoice=0, CreditMemo=1, DebitMemo=2, Prepayment=3
--    Fiscal periods: 2604=Apr 2026, 2605=May, 2606=Jun, 2607=Jul
-- ---------------------------------------------------------------------
INSERT INTO ap.VoucherBatches (Id, CompanyId, BatchNumber, Description, PostingDate, FiscalPeriodId, Status, CreatedBy, CreatedOn) VALUES
 ('93000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'VB-0001', 'July vouchers - draft',    '2026-07-25T00:00:00Z', '21000000-0000-0000-0000-000000000726', 0, 'seed', SYSDATETIMEOFFSET()),
 ('93000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'VB-0002', 'July vouchers - batched',  '2026-07-20T00:00:00Z', '21000000-0000-0000-0000-000000000726', 1, 'seed', SYSDATETIMEOFFSET()),
 ('93000000-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'VB-0003', 'June vendor invoices',     '2026-06-20T00:00:00Z', '21000000-0000-0000-0000-000000000626', 2, 'seed', SYSDATETIMEOFFSET()),
 ('93000000-0000-0000-0000-000000000004', '11111111-1111-1111-1111-111111111111', 'VB-0004', 'May vendor invoices',      '2026-05-15T00:00:00Z', '21000000-0000-0000-0000-000000000526', 2, 'seed', SYSDATETIMEOFFSET()),
 ('93000000-0000-0000-0000-000000000005', '11111111-1111-1111-1111-111111111111', 'VB-0005', 'April vendor invoices',    '2026-04-20T00:00:00Z', '21000000-0000-0000-0000-000000000426', 2, 'seed', SYSDATETIMEOFFSET());
GO

INSERT INTO ap.Vouchers (Id, VoucherBatchId, VendorId, VoucherType, InvoiceNumber, InvoiceDate, DueDate, TotalAmount, DiscountAmount, Description, PaymentTermId, SelectedForPayment, BackupWithholdingAmount, Form1099Amount) VALUES
 ('94000000-0000-0000-0000-000000000001', '93000000-0000-0000-0000-000000000002', '91000000-0000-0000-0000-000000000001', 0, 'INV-2201', '2026-07-10T00:00:00Z', '2026-08-09T00:00:00Z', 5000.00, 0.00, 'Site materials',      '90000000-0000-0000-0000-000000000001', 0, 0.00, 0.00),
 ('94000000-0000-0000-0000-000000000002', '93000000-0000-0000-0000-000000000002', '91000000-0000-0000-0000-000000000002', 0, 'INV-2202', '2026-07-12T00:00:00Z', '2026-07-27T00:00:00Z', 1200.00, 0.00, 'Electrical repair',    '90000000-0000-0000-0000-000000000002', 0, 0.00, 0.00),
 ('94000000-0000-0000-0000-000000000003', '93000000-0000-0000-0000-000000000003', '91000000-0000-0000-0000-000000000001', 0, 'INV-2133', '2026-06-10T00:00:00Z', '2026-07-10T00:00:00Z', 12500.00, 0.00, 'Concrete and steel',   '90000000-0000-0000-0000-000000000001', 1, 0.00, 0.00),
 ('94000000-0000-0000-0000-000000000004', '93000000-0000-0000-0000-000000000003', '91000000-0000-0000-0000-000000000004', 0, 'INV-2134', '2026-06-15T00:00:00Z', '2026-07-15T00:00:00Z', 350.00, 0.00, 'Office supplies',      '90000000-0000-0000-0000-000000000003', 0, 0.00, 0.00),
 ('94000000-0000-0000-0000-000000000005', '93000000-0000-0000-0000-000000000004', '91000000-0000-0000-0000-000000000002', 0, 'INV-2077', '2026-05-05T00:00:00Z', '2026-06-04T00:00:00Z', 4800.00, 0.00, 'Electrical panel upgrade', '90000000-0000-0000-0000-000000000002', 0, 0.00, 0.00),
 ('94000000-0000-0000-0000-000000000006', '93000000-0000-0000-0000-000000000005', '91000000-0000-0000-0000-000000000003', 0, 'INV-2011', '2026-04-01T00:00:00Z', '2026-05-01T00:00:00Z', 2200.00, 0.00, 'Equipment rental',     '90000000-0000-0000-0000-000000000001', 0, 0.00, 0.00),
 ('94000000-0000-0000-0000-000000000007', '93000000-0000-0000-0000-000000000005', '91000000-0000-0000-0000-000000000005', 0, 'INV-2023', '2026-04-15T00:00:00Z', '2026-05-15T00:00:00Z', 9750.00, 0.00, 'Consulting services',  '90000000-0000-0000-0000-000000000001', 0, 0.00, 0.00);
GO

INSERT INTO ap.VoucherDistributions (Id, VoucherId, AccountId, Debit, Credit, ProjectId, TaskId) VALUES
 ('95000000-0000-0000-0000-000000000001', '94000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-00000000000E', 5000.00, 0.00, NULL, NULL),
 ('95000000-0000-0000-0000-000000000002', '94000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000006', 0.00, 5000.00, NULL, NULL),
 ('95000000-0000-0000-0000-000000000003', '94000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000010', 1200.00, 0.00, NULL, NULL),
 ('95000000-0000-0000-0000-000000000004', '94000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000006', 0.00, 1200.00, NULL, NULL),
 ('95000000-0000-0000-0000-000000000005', '94000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-00000000000C', 12500.00, 0.00, NULL, NULL),
 ('95000000-0000-0000-0000-000000000006', '94000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000006', 0.00, 12500.00, NULL, NULL),
 ('95000000-0000-0000-0000-000000000007', '94000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000010', 350.00, 0.00, NULL, NULL),
 ('95000000-0000-0000-0000-000000000008', '94000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000006', 0.00, 350.00, NULL, NULL),
 ('95000000-0000-0000-0000-000000000009', '94000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-00000000000D', 4800.00, 0.00, NULL, NULL),
 ('95000000-0000-0000-0000-00000000000A', '94000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000006', 0.00, 4800.00, NULL, NULL),
 ('95000000-0000-0000-0000-00000000000B', '94000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-00000000000E', 2200.00, 0.00, NULL, NULL),
 ('95000000-0000-0000-0000-00000000000C', '94000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000006', 0.00, 2200.00, NULL, NULL),
 ('95000000-0000-0000-0000-00000000000D', '94000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000010', 9750.00, 0.00, NULL, NULL),
 ('95000000-0000-0000-0000-00000000000E', '94000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000006', 0.00, 9750.00, NULL, NULL);
GO

-- ---------------------------------------------------------------------
-- 13. AP: Payments + Payment Lines
--    PaymentStatus: Selected=0, Issued=1, Cleared=2, Voided=3
--    PaymentMethod: Check=0, ACH=1, WireTransfer=2, CreditCard=3, Cash=4
--    TotalAmount is computed from lines (no column).
-- ---------------------------------------------------------------------
INSERT INTO ap.Payments (Id, CompanyId, VendorId, PaymentReference, PaymentDate, PaymentMethod, CurrencyCode, BankAccountId, Status, CreatedBy, CreatedOn) VALUES
 ('96000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', '91000000-0000-0000-0000-000000000001', 'PMT-0001', '2026-07-20T00:00:00Z', 0, 'USD', NULL, 0, 'seed', SYSDATETIMEOFFSET()),
 ('96000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', '91000000-0000-0000-0000-000000000004', 'PMT-0002', '2026-07-05T00:00:00Z', 0, 'USD', NULL, 1, 'seed', SYSDATETIMEOFFSET()),
 ('96000000-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', '91000000-0000-0000-0000-000000000002', 'PMT-0003', '2026-06-10T00:00:00Z', 1, 'USD', NULL, 3, 'seed', SYSDATETIMEOFFSET());
GO

INSERT INTO ap.PaymentLines (Id, PaymentId, VoucherId, AppliedAmount) VALUES
 ('97000000-0000-0000-0000-000000000001', '96000000-0000-0000-0000-000000000001', '94000000-0000-0000-0000-000000000003', 12500.00),
 ('97000000-0000-0000-0000-000000000002', '96000000-0000-0000-0000-000000000002', '94000000-0000-0000-0000-000000000004', 350.00),
 ('97000000-0000-0000-0000-000000000003', '96000000-0000-0000-0000-000000000003', '94000000-0000-0000-0000-000000000005', 4800.00);
GO

-- ---------------------------------------------------------------------
-- 14. GL: Journal Batches + Lines
--    JournalBatchStatus: Draft=0, Balanced=1, Posted=2, Reversed=3
--    ExchangeRate is required (default 1.0).
-- ---------------------------------------------------------------------
INSERT INTO gl.JournalBatches (Id, CompanyId, BatchNumber, Description, PostingDate, FiscalPeriodId, CurrencyId, Status, CreatedBy, CreatedOn) VALUES
 ('80000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'JB-0001', 'July manual entry - draft', '2026-07-25T00:00:00Z', '21000000-0000-0000-0000-000000000726', NULL, 0, 'seed', SYSDATETIMEOFFSET()),
 ('80000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'JB-0002', 'July cash sale',           '2026-07-01T00:00:00Z', '21000000-0000-0000-0000-000000000726', NULL, 1, 'seed', SYSDATETIMEOFFSET()),
 ('80000000-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'JB-0003', 'June rent accrual',        '2026-06-30T00:00:00Z', '21000000-0000-0000-0000-000000000626', NULL, 2, 'seed', SYSDATETIMEOFFSET()),
 ('80000000-0000-0000-0000-000000000004', '11111111-1111-1111-1111-111111111111', 'JB-0004', 'June utilities accrual',   '2026-06-30T00:00:00Z', '21000000-0000-0000-0000-000000000626', NULL, 2, 'seed', SYSDATETIMEOFFSET()),
 ('80000000-0000-0000-0000-000000000005', '11111111-1111-1111-1111-111111111111', 'JB-0005', 'June consulting revenue',  '2026-06-15T00:00:00Z', '21000000-0000-0000-0000-000000000626', NULL, 2, 'seed', SYSDATETIMEOFFSET());
GO

INSERT INTO gl.JournalEntryLines (Id, JournalBatchId, AccountId, Debit, Credit, Reference, SegmentsJson, CurrencyId, ExchangeRate, ForeignCredit, ForeignDebit) VALUES
 ('81000000-0000-0000-0000-000000000001', '80000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000001', 15000.00, 0.00, 'July cash sale', NULL, NULL, 1.0, NULL, NULL),
 ('81000000-0000-0000-0000-000000000002', '80000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-00000000000B', 0.00, 15000.00, 'July cash sale', NULL, NULL, 1.0, NULL, NULL),
 ('81000000-0000-0000-0000-000000000003', '80000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-00000000000E', 2500.00, 0.00, 'June rent', NULL, NULL, 1.0, NULL, NULL),
 ('81000000-0000-0000-0000-000000000004', '80000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000001', 0.00, 2500.00, 'June rent', NULL, NULL, 1.0, NULL, NULL),
 ('81000000-0000-0000-0000-000000000005', '80000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-00000000000F', 800.00, 0.00, 'June utilities', NULL, NULL, 1.0, NULL, NULL),
 ('81000000-0000-0000-0000-000000000006', '80000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000006', 0.00, 800.00, 'June utilities', NULL, NULL, 1.0, NULL, NULL),
 ('81000000-0000-0000-0000-000000000007', '80000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000001', 12000.00, 0.00, 'June consulting revenue', NULL, NULL, 1.0, NULL, NULL),
 ('81000000-0000-0000-0000-000000000008', '80000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-00000000000B', 0.00, 12000.00, 'June consulting revenue', NULL, NULL, 1.0, NULL, NULL);
GO

-- ---------------------------------------------------------------------
-- 15. GL: Budgets + Budget Lines (monthly amounts, 12 periods)
--    BudgetType: Original=0, Revised=1, Encumbrance=2
-- ---------------------------------------------------------------------
INSERT INTO gl.Budgets (Id, CompanyId, FiscalYearId, Name, Description, BudgetType, IsActive, CreatedBy, CreatedOn) VALUES
 ('82000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', '20000000-0000-0000-0000-000000000002', 'FY2026 Operating Budget', 'Original annual operating budget', 0, 1, 'seed', SYSDATETIMEOFFSET());
GO

;WITH p AS (
    SELECT n FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12)) v(n)
),
amt AS (
    SELECT Id, CASE AccountNumber
        WHEN '4000' THEN 25000
        WHEN '5000' THEN 12000
        WHEN '6000' THEN 18000
        WHEN '6100' THEN 2500
        WHEN '6200' THEN 800
    END AS Amount
    FROM gl.Account
    WHERE CompanyId = '11111111-1111-1111-1111-111111111111'
      AND AccountNumber IN ('4000','5000','6000','6100','6200')
)
INSERT INTO gl.BudgetLines (Id, BudgetId, AccountId, PeriodNumber, Amount)
SELECT NEWID(), '82000000-0000-0000-0000-000000000001', a.Id, p.n, a.Amount
FROM p CROSS JOIN amt a;
GO

-- ---------------------------------------------------------------------
-- 16. GL: Recurring Templates + Lines
--    RecurringFrequency: Monthly=0, Quarterly=1, SemiAnnually=2,
--                        Annually=3, Custom=4
-- ---------------------------------------------------------------------
INSERT INTO gl.RecurringTemplates (Id, CompanyId, Name, Description, Frequency, NextRunDate, LastRunDate, IsActive, CreatedBy, CreatedOn) VALUES
 ('84000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'Monthly Rent Accrual',    'Accrue monthly office rent',     0, '2026-08-01T00:00:00Z', '2026-07-01T00:00:00Z', 1, 'seed', SYSDATETIMEOFFSET()),
 ('84000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'Monthly Depreciation',     'Monthly equipment depreciation', 0, '2026-08-01T00:00:00Z', '2026-07-01T00:00:00Z', 1, 'seed', SYSDATETIMEOFFSET());
GO

INSERT INTO gl.RecurringTemplateLines (Id, RecurringTemplateId, AccountId, FixedDebit, FixedCredit, VariablePct, Reference) VALUES
 ('85000000-0000-0000-0000-000000000001', '84000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-00000000000E', 2500.00, 0.00, NULL, 'Monthly rent'),
 ('85000000-0000-0000-0000-000000000002', '84000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 0.00, 2500.00, NULL, 'Monthly rent'),
 ('85000000-0000-0000-0000-000000000003', '84000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000010', 1000.00, 0.00, NULL, 'Depreciation'),
 ('85000000-0000-0000-0000-000000000004', '84000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000005', 0.00, 1000.00, NULL, 'Depreciation');
GO

-- ---------------------------------------------------------------------
-- 17. GL: Allocation Rules + Lines
--    AllocationMethod: Percentage=0, FixedAmount=1, Equally=2
-- ---------------------------------------------------------------------
INSERT INTO gl.AllocationRules (Id, CompanyId, Name, Description, SourceAccountId, Method, IsActive, CreatedBy, CreatedOn) VALUES
 ('86000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'Overhead Allocation', 'Spread Other Expense across departments', '30000000-0000-0000-0000-000000000010', 0, 1, 'seed', SYSDATETIMEOFFSET());
GO

INSERT INTO gl.AllocationRuleLines (Id, AllocationRuleId, TargetAccountId, Percentage, FixedAmount, Reference) VALUES
 ('87000000-0000-0000-0000-000000000001', '86000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-00000000000D', 0.50, NULL, 'Salaries share'),
 ('87000000-0000-0000-0000-000000000002', '86000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-00000000000E', 0.30, NULL, 'Rent share'),
 ('87000000-0000-0000-0000-000000000003', '86000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-00000000000F', 0.20, NULL, 'Utilities share');
GO

-- ---------------------------------------------------------------------
-- 18. AR: Customers
-- ---------------------------------------------------------------------
INSERT INTO ar.Customers (Id, CustomerId, Name, LegalName, TaxId, CreditLimit, CreditHoldDays, DefaultPaymentTermId, TaxExempt, TaxExemptCertificate, CurrencyCode, IsActive, CreatedBy, CreatedOn) VALUES
 ('a0000000-0000-0000-0000-000000000001', 'C-1001', 'Acme Industries',         'Acme Industries Inc.',      '12-1111111', 100000.00, 0, '90000000-0000-0000-0000-000000000001', 0, NULL, 'USD', 1, 'seed', SYSDATETIMEOFFSET()),
 ('a0000000-0000-0000-0000-000000000002', 'C-1002', 'Beacon Hospital',          'Beacon Health System',      '13-2222222', 250000.00, 0, '90000000-0000-0000-0000-000000000001', 1, 'IL-EX-0042', 'USD', 1, 'seed', SYSDATETIMEOFFSET()),
 ('a0000000-0000-0000-0000-000000000003', 'C-1003', 'City of Springfield',      'City of Springfield',       '14-3333333', 500000.00, 0, '90000000-0000-0000-0000-000000000003', 0, NULL, 'USD', 1, 'seed', SYSDATETIMEOFFSET()),
 ('a0000000-0000-0000-0000-000000000004', 'C-1004', 'Globex Corporation',       'Globex Corp',               '15-4444444', 150000.00, 0, '90000000-0000-0000-0000-000000000002', 0, NULL, 'USD', 1, 'seed', SYSDATETIMEOFFSET()),
 ('a0000000-0000-0000-0000-000000000005', 'C-1005', 'Initech',                  'Initech LLC',               '16-5555555', 50000.00,  0, '90000000-0000-0000-0000-000000000001', 0, NULL, 'USD', 1, 'seed', SYSDATETIMEOFFSET());
GO

-- ---------------------------------------------------------------------
-- 19. AR: Invoice Batches + Invoices + Lines
--    InvoiceBatchStatus: Draft=0, Batched=1, Posted=2, Reversed=3
--    InvoiceStatus: Open=0, PartiallyPaid=1, Paid=2, Voided=3, WriteOff=4
--    Invoice total = (Quantity * UnitPrice) + TaxAmount - DiscountAmount
-- ---------------------------------------------------------------------
INSERT INTO ar.InvoiceBatches (Id, CompanyId, BatchNumber, Description, PostingDate, FiscalPeriodId, Status, CreatedBy, CreatedOn) VALUES
 ('a1000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'IB-0001', 'July invoices - draft', '2026-07-28T00:00:00Z', '21000000-0000-0000-0000-000000000726', 0, 'seed', SYSDATETIMEOFFSET()),
 ('a1000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'IB-0002', 'June posted invoices',  '2026-06-05T00:00:00Z', '21000000-0000-0000-0000-000000000626', 2, 'seed', SYSDATETIMEOFFSET());
GO

INSERT INTO ar.Invoices (Id, InvoiceBatchId, CustomerId, InvoiceNumber, InvoiceDate, DueDate, Description, PaymentTermId, ProjectId, SalesOrderId, Status) VALUES
 ('a2000000-0000-0000-0000-000000000001', 'a1000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'INV-5001', '2026-03-10T00:00:00Z', '2026-04-09T00:00:00Z', 'March construction services',  '90000000-0000-0000-0000-000000000001', NULL, NULL, 0),
 ('a2000000-0000-0000-0000-000000000002', 'a1000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000004', 'INV-5002', '2026-05-10T00:00:00Z', '2026-05-25T00:00:00Z', 'Engineering services',        '90000000-0000-0000-0000-000000000002', NULL, NULL, 0),
 ('a2000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000002', 'INV-5003', '2026-06-15T00:00:00Z', '2026-07-15T00:00:00Z', 'Facility maintenance',        '90000000-0000-0000-0000-000000000001', NULL, NULL, 0),
 ('a2000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000003', 'INV-5004', '2026-07-20T00:00:00Z', '2026-08-19T00:00:00Z', 'Public works project',        '90000000-0000-0000-0000-000000000003', NULL, NULL, 0),
 ('a2000000-0000-0000-0000-000000000005', 'a1000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000005', 'INV-5005', '2026-04-05T00:00:00Z', '2026-05-05T00:00:00Z', 'IT consulting',               '90000000-0000-0000-0000-000000000001', NULL, NULL, 0),
 ('a2000000-0000-0000-0000-000000000006', 'a1000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'INV-5006', '2026-06-01T00:00:00Z', '2026-07-01T00:00:00Z', 'Materials and labor',         '90000000-0000-0000-0000-000000000001', NULL, NULL, 0),
 ('a2000000-0000-0000-0000-000000000007', 'a1000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000002', 'INV-5007', '2026-06-20T00:00:00Z', '2026-07-20T00:00:00Z', 'Medical equipment service',   '90000000-0000-0000-0000-000000000001', NULL, NULL, 1);
GO

INSERT INTO ar.InvoiceLines (Id, InvoiceId, AccountId, Description, Quantity, UnitPrice, TaxAmount, DiscountAmount, PaidAmount, CreditDebitMemoId) VALUES
 ('a3000000-0000-0000-0000-000000000001', 'a2000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-00000000000B', 'March construction services',  15.0, 1000.00, 0.00, 0.00, 0.00, NULL),
 ('a3000000-0000-0000-0000-000000000002', 'a2000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-00000000000B', 'Engineering services',         12.0, 1000.00, 0.00, 0.00, 0.00, NULL),
 ('a3000000-0000-0000-0000-000000000003', 'a2000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-00000000000B', 'Facility maintenance',         22.5, 1000.00, 0.00, 0.00, 0.00, NULL),
 ('a3000000-0000-0000-0000-000000000004', 'a2000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-00000000000B', 'Public works project',          5.75, 1000.00, 0.00, 0.00, 0.00, NULL),
 ('a3000000-0000-0000-0000-000000000005', 'a2000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-00000000000B', 'IT consulting',                 3.4, 1000.00, 0.00, 0.00, 0.00, NULL),
 ('a3000000-0000-0000-0000-000000000006', 'a2000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-00000000000B', 'Materials and labor',           8.0, 1000.00, 0.00, 0.00, 8000.00, NULL),
 ('a3000000-0000-0000-0000-000000000007', 'a2000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-00000000000B', 'Medical equipment service',     9.8, 1000.00, 0.00, 0.00, 3000.00, NULL);
GO

-- ---------------------------------------------------------------------
-- 20. AR: Cash Receipts + Applications
--    CashReceiptStatus: Unapplied=0, PartiallyApplied=1,
--                       FullyApplied=2, Refunded=3
--    PaymentMethod is a string here (e.g. 'Check', 'ACH').
-- ---------------------------------------------------------------------
INSERT INTO ar.CashReceipts (Id, CompanyId, CustomerId, ReceiptReference, TotalAmount, ReceiptDate, PaymentMethod, CurrencyCode, ReferenceNumber, Status, CreatedBy, CreatedOn) VALUES
 ('a4000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'a0000000-0000-0000-0000-000000000002', 'RCPT-0001', 3000.00, '2026-07-01T00:00:00Z', 'Check', 'USD', 'CK-1042',  2, 'seed', SYSDATETIMEOFFSET()),
 ('a4000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'a0000000-0000-0000-0000-000000000001', 'RCPT-0002', 5000.00, '2026-07-25T00:00:00Z', 'ACH',   'USD', 'ACH-2201', 0, 'seed', SYSDATETIMEOFFSET()),
 ('a4000000-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'a0000000-0000-0000-0000-000000000001', 'RCPT-0003', 8000.00, '2026-07-15T00:00:00Z', 'Check', 'USD', 'CK-1039',  2, 'seed', SYSDATETIMEOFFSET());
GO

INSERT INTO ar.CashReceiptApplications (Id, CashReceiptId, InvoiceId, AppliedAmount) VALUES
 ('a5000000-0000-0000-0000-000000000001', 'a4000000-0000-0000-0000-000000000001', 'a2000000-0000-0000-0000-000000000007', 3000.00),
 ('a5000000-0000-0000-0000-000000000002', 'a4000000-0000-0000-0000-000000000003', 'a2000000-0000-0000-0000-000000000006', 8000.00);
GO

-- ---------------------------------------------------------------------
-- 21. AR: Credit/Debit Memos, Statements, Finance Charges
--    MemoType: CreditMemo=0, DebitMemo=1; MemoStatus: Open=0, Applied=1, Voided=2
--    StatementStatus: Generated=0, Delivered=1
--    FinanceChargeStatus: Open=0, Voided=1
-- ---------------------------------------------------------------------
INSERT INTO ar.CreditDebitMemos (Id, InvoiceBatchId, CustomerId, ReferenceNumber, MemoDate, AppliedToInvoiceId, Description, MemoType, Status) VALUES
 ('a6000000-0000-0000-0000-000000000001', 'a1000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'CDM-0001', '2026-06-25T00:00:00Z', NULL, 'Credit for returned materials', 0, 0);
GO

INSERT INTO ar.Statements (Id, CompanyId, CustomerId, AsOfDate, StatementNumber, Status, CreatedBy, CreatedOn) VALUES
 ('a7000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'a0000000-0000-0000-0000-000000000001', '2026-07-31T00:00:00Z', 'STMT-0001', 0, 'seed', SYSDATETIMEOFFSET()),
 ('a7000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'a0000000-0000-0000-0000-000000000002', '2026-07-31T00:00:00Z', 'STMT-0002', 1, 'seed', SYSDATETIMEOFFSET());
GO

INSERT INTO ar.FinanceCharges (Id, CompanyId, CustomerId, ChargeNumber, ChargeDate, ChargeAmount, AnnualRate, Description, Status, CreatedBy, CreatedOn) VALUES
 ('a8000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'a0000000-0000-0000-0000-000000000005', 'FC-0001', '2026-07-31T00:00:00Z', 68.00, 12.0, 'Finance charge on past due balance', 0, 'seed', SYSDATETIMEOFFSET());
GO

-- ---------------------------------------------------------------------
-- Done
-- ---------------------------------------------------------------------
COMMIT TRANSACTION;

PRINT 'Demo data seeded successfully.';
GO
