USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'erp')
BEGIN
    CREATE DATABASE [erp]
    COLLATE Latin1_General_CI_AS;
    ALTER DATABASE [erp] SET READ_COMMITTED_SNAPSHOT ON;
    ALTER DATABASE [erp] SET ALLOW_SNAPSHOT_ISOLATION ON;
END
GO

USE [erp];
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'platform')
    EXEC('CREATE SCHEMA [platform]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'gl')
    EXEC('CREATE SCHEMA [gl]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'ap')
    EXEC('CREATE SCHEMA [ap]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'ar')
    EXEC('CREATE SCHEMA [ar]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'cash')
    EXEC('CREATE SCHEMA [cash]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'pur')
    EXEC('CREATE SCHEMA [pur]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'inv')
    EXEC('CREATE SCHEMA [inv]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'om')
    EXEC('CREATE SCHEMA [om]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'bom')
    EXEC('CREATE SCHEMA [bom]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'proj')
    EXEC('CREATE SCHEMA [proj]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'pay')
    EXEC('CREATE SCHEMA [pay]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'fs')
    EXEC('CREATE SCHEMA [fs]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'rpt')
    EXEC('CREATE SCHEMA [rpt]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'int')
    EXEC('CREATE SCHEMA [int]');
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'audit')
    EXEC('CREATE SCHEMA [audit]');
GO

PRINT 'Database and schemas created successfully.';