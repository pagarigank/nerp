-- De-duplicate warehouses and items, keeping the earliest (MIN Id) per code.
-- Soft-delete (DeletedOn) the extra rows so we don't lose history.
SET NOCOUNT ON;
DECLARE @now datetimeoffset = SYSDATETIMEOFFSET();
DECLARE @by sysname = 'seed-dedup';

-- Warehouses: keep one per WarehouseCode
UPDATE inv.Warehouses
SET DeletedOn = @now, DeletedBy = @by
WHERE Id NOT IN (
  SELECT MIN(Id) FROM inv.Warehouses GROUP BY WarehouseCode
);

-- Items: keep one per ItemCode
UPDATE inv.Items
SET DeletedOn = @now, DeletedBy = @by
WHERE Id NOT IN (
  SELECT MIN(Id) FROM inv.Items GROUP BY ItemCode
);

SELECT 'After dedup' AS s,
  (SELECT COUNT(DISTINCT WarehouseCode) FROM inv.Warehouses WHERE DeletedOn IS NULL) AS DistinctWarehouses,
  (SELECT COUNT(DISTINCT ItemCode) FROM inv.Items WHERE DeletedOn IS NULL) AS DistinctItems;
