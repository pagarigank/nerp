-- <copyright file="202609030001_AddMalaysianTaxAndEInvoiceTables.sql" company="ERP Project">
-- Copyright (c) ERP Project. All rights reserved.
-- </copyright>

-- Migration: Add Malaysian Tax (SST) and e-Invoice tables
-- Run this migration to add the tax and e-invoice module schemas

-- ============================================
-- Tax Schema (tax)
-- ============================================
CREATE SCHEMA IF NOT EXISTS tax;

-- Tax Code Master
CREATE TABLE tax.tax_code (
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    code NVARCHAR(50) NOT NULL,
    description NVARCHAR(250) NOT NULL,
    lhdnt_tax_type NVARCHAR(5) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NULL,
    CONSTRAINT UQ_tax_code_code UNIQUE (code)
);

-- Tax Rate Master (with effective-dating)
CREATE TABLE tax.tax_rate (
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    tax_code_id UNIQUEIDENTIFIER NOT NULL,
    rate_percent DECIMAL(5,4) NOT NULL,
    effective_from DATETIME2 NOT NULL,
    effective_to DATETIME2 NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NULL,
    
    CONSTRAINT FK_tax_rate_tax_code FOREIGN KEY (tax_code_id) REFERENCES tax.tax_code(id),
    CONSTRAINT UQ_tax_rate_active UNIQUE (tax_code_id, effective_from, is_active)
);

CREATE INDEX IX_tax_rate_tax_code_id ON tax.tax_rate(tax_code_id);
CREATE INDEX IX_tax_rate_effective_from ON tax.tax_rate(effective_from);

-- ============================================
-- E-Invoice Schema (int) - Additional tables
-- ============================================

-- Taxpayer Profile (Company e-Invoice setup)
CREATE TABLE int.taxpayer_profile (
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    taxpayer_name NVARCHAR(100) NOT NULL,
    tax_identification_number NVARCHAR(20) NOT NULL,
    business_registration_number NVARCHAR(20) NULL,
    sales_tax_number NVARCHAR(20) NULL,
    service_tax_number NVARCHAR(20) NULL,
    msic_code NVARCHAR(10) NOT NULL,
    address_line1 NVARCHAR(100) NOT NULL,
    address_line2 NVARCHAR(100) NULL,
    city NVARCHAR(50) NOT NULL,
    state NVARCHAR(50) NOT NULL,
    postcode NVARCHAR(10) NOT NULL,
    country_code NVARCHAR(2) NOT NULL DEFAULT 'MY',
    contact_person NVARCHAR(100) NOT NULL,
    contact_phone NVARCHAR(20) NOT NULL,
    contact_email NVARCHAR(100) NOT NULL,
    is_sandbox BIT NOT NULL DEFAULT 0,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NULL,
    CONSTRAINT UQ_taxpayer_profile_tin UNIQUE (tax_identification_number)
);

-- e-Invoice Document tracking
CREATE TABLE int.einvoice_document (
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    source_document_id UNIQUEIDENTIFIER NOT NULL,
    source_document_type NVARCHAR(50) NOT NULL,
    document_type NVARCHAR(50) NOT NULL DEFAULT 'Invoice',
    total_amount DECIMAL(18,2) NOT NULL,
    currency_code NVARCHAR(3) NOT NULL DEFAULT 'MYR',
    myinvois_uuid NVARCHAR(100) NULL,
    validation_link NVARCHAR(500) NULL,
    qr_code_payload NVARCHAR(MAX) NULL,
    status INT NOT NULL DEFAULT 0, -- 0=Queued, 1=Signed, 2=Submitted, 3=Valid, 4=Invalid, 5=Cancelled, 6=Rejected
    lhdns_error_code NVARCHAR(50) NULL,
    lhdns_error_message NVARCHAR(500) NULL,
    is_sandbox BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    signed_at DATETIME2 NULL,
    submitted_at DATETIME2 NULL,
    validated_at DATETIME2 NULL,
    cancelled_at DATETIME2 NULL,
    updated_at DATETIME2 NULL,
    submission_uid NVARCHAR(32) NULL,
    content_type NVARCHAR(100) NULL,
    content_hash NVARCHAR(64) NULL,
    created_by_user_id NVARCHAR(100) NULL,
    updated_by_user_id NVARCHAR(100) NULL
);

CREATE INDEX IX_einvoice_document_myinvois_uuid ON int.einvoice_document(myinvois_uuid) WHERE myinvois_uuid IS NOT NULL;
CREATE INDEX IX_einvoice_document_status ON int.einvoice_document(status);
CREATE INDEX IX_einvoice_document_created_at ON int.einvoice_document(created_at);
CREATE INDEX IX_einvoice_document_source ON int.einvoice_document(source_document_id, source_document_type);

-- Digital Certificate for XAdES signing
CREATE TABLE int.digital_certificate (
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    certificate_name NVARCHAR(100) NOT NULL,
    issuer NVARCHAR(100) NOT NULL,
    serial_number NVARCHAR(20) NOT NULL,
    not_before DATETIME2 NOT NULL,
    not_after DATETIME2 NOT NULL,
    thumbprint NVARCHAR(20) NOT NULL,
    vault_object_id NVARCHAR(1000) NULL,
    is_active BIT NOT NULL DEFAULT 1,
    is_sandbox BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NULL,
    created_by_user_id NVARCHAR(100) NULL,
    updated_by_user_id NVARCHAR(100) NULL
);

CREATE INDEX IX_digital_certificate_thumbprint ON int.digital_certificate(thumbprint);
CREATE INDEX IX_digital_certificate_expiry ON int.digital_certificate(not_after, is_active);

-- ============================================
-- Seed Data
-- ============================================

-- Insert default Malaysian tax codes
INSERT INTO tax.tax_code (code, description, lhdnt_tax_type) VALUES
('SST-SALES-5', 'Sales Tax 5% Rate', '01'),
('SST-SALES-10', 'Sales Tax 10% Rate', '01'),
('SST-SERVICE-6', 'Service Tax 6% (Reduced)', '02'),
('SST-SERVICE-8', 'Service Tax 8% Standard', '02'),
('SST-TOURISM', 'Tourism Tax', '03'),
('SST-HVG', 'High-Value Goods Tax', '04'),
('SST-ZRL', 'Zero-Rated Supply', 'ZRL'),
('SST-EXEMPT', 'Exempt Supply', 'E');

-- Insert default tax rates
INSERT INTO tax.tax_rate (tax_code_id, rate_percent, effective_from)
SELECT tc.id, 
       CASE tc.code
           WHEN 'SST-SALES-5' THEN 5.0000
           WHEN 'SST-SALES-10' THEN 10.0000
           WHEN 'SST-SERVICE-6' THEN 6.0000
           WHEN 'SST-SERVICE-8' THEN 8.0000
           WHEN 'SST-TOURISM' THEN 10.0000
           WHEN 'SST-HVG' THEN 10.0000
           ELSE 0.0000
       END,
       GETUTCDATE()
FROM tax.tax_code tc
WHERE tc.code IN ('SST-SALES-5', 'SST-SALES-10', 'SST-SERVICE-6', 'SST-SERVICE-8', 'SST-TOURISM', 'SST-HVG');

GO