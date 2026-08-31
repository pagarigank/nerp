-- Backend data cleanup: remove junk test rows + rename meaningful ones with clean codes/names
-- ERP convention: soft-delete (DeletedOn/DeletedBy) for transactional/history entities; update names for kept rows.

DECLARE @now datetimeoffset = SYSDATETIMEOFFSET();
DECLARE @by nvarchar(100) = 'system-rename';

-- 1) SOFT-DELETE random junk WAREHOUSES (code == name, auto-generated, company 11111111)
UPDATE inv.Warehouses
SET DeletedOn = @now, DeletedBy = @by
WHERE DeletedOn IS NULL
  AND WarehouseCode IN (
    'E2EMTFUN70Q','E2EMTFUYQ9A','E2EMTFVM6EN',
    'QAMTG1Q9NE','QAMTG28ZBJ0','QAMTGK0T3X0','QAMTGKB0UG0','QAMTGL9CPE0',
    'QAMTGLQ6OL0','QAMTGM4C0X0','QAMTGNY7T00','QAMTGS0N1D0','QAMTGTBJYQ0'
  );

-- 2) SOFT-DELETE random junk ITEMS (code == name, auto-generated, company 11111111)
UPDATE inv.Items
SET DeletedOn = @now, DeletedBy = @by
WHERE DeletedOn IS NULL
  AND ItemCode IN (
    'E2EMTFUN70Q','E2EMTFUYQ9A','E2EMTFVM6EN',
    'QAMTG1Q9NE','QAMTG28ZBJ0','QAMTGK0T3X0','QAMTGKB0UG0','QAMTGL9CPE0',
    'QAMTGLQ6OL0','QAMTGM4C0X0','QAMTGNY7T00','QAMTGS0N1D0','QAMTGTBJYQ0'
  );

-- 3) SOFT-DELETE my own test-artifact BINS
UPDATE inv.WarehouseBins
SET DeletedOn = @now, DeletedBy = @by
WHERE DeletedOn IS NULL
  AND BinCode IN ('API-1','T-EDIT','V-1','X-1','Z-50','Z-99');

-- 4) RENAME meaningful WAREHOUSES with clean codes + friendlier names
UPDATE inv.Warehouses SET WarehouseCode = 'WH-MAIN', WarehouseName = 'Main Warehouse - Manila'     WHERE WarehouseCode = 'WH-MAIN';
UPDATE inv.Warehouses SET WarehouseCode = 'WH-EAST', WarehouseName = 'East Distribution Center'     WHERE WarehouseCode = 'WH-EAST';
UPDATE inv.Warehouses SET WarehouseCode = 'WH-WEST', WarehouseName = 'West Distribution Center'     WHERE WarehouseCode = 'WH-WEST';

-- 5) RENAME meaningful ITEMS with clean codes + descriptive names
UPDATE inv.Items SET ItemCode = 'ITEM-1001', Description = 'Blue Widget A (Finished Good)'   WHERE ItemCode = 'ITEM-1001';
UPDATE inv.Items SET ItemCode = 'ITEM-1002', Description = 'Green Widget B (Finished Good)'  WHERE ItemCode = 'ITEM-1002';
UPDATE inv.Items SET ItemCode = 'ITEM-1003', Description = 'Raw Steel Component X'           WHERE ItemCode = 'ITEM-1003';
UPDATE inv.Items SET ItemCode = 'ITEM-1004', Description = 'Raw Aluminum Component Y'        WHERE ItemCode = 'ITEM-1004';
UPDATE inv.Items SET ItemCode = 'ITEM-1005', Description = 'On-site Service Visit'           WHERE ItemCode = 'ITEM-1005';

-- 6) RENAME demo BINS with meaningful location labels (BinCode stays A-01/A-02; add aisle/rack/shelf)
UPDATE inv.WarehouseBins SET Aisle='A', Rack='R1', Shelf='S1' WHERE BinCode='A-01' AND DeletedOn IS NULL;
UPDATE inv.WarehouseBins SET Aisle='A', Rack='R1', Shelf='S2' WHERE BinCode='A-02' AND DeletedOn IS NULL;

SELECT 'done' AS result;
