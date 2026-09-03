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

-- Company SST Registration (Sales Tax, Service Tax, Tourism Tax)
CREATE TABLE tax.company_sst_registration (
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    company_id UNIQUEIDENTIFIER NOT NULL,
    is_registered_for_sales_tax BIT NOT NULL DEFAULT 0,
    sales_tax_registration_number NVARCHAR(50) NULL,
    sales_tax_registration_date DATETIME2 NULL,
    sales_tax_deregistration_date DATETIME2 NULL,
    is_registered_for_service_tax BIT NOT NULL DEFAULT 0,
    service_tax_registration_number NVARCHAR(50) NULL,
    service_tax_registration_date DATETIME2 NULL,
    service_tax_deregistration_date DATETIME2 NULL,
    is_registered_for_tourism_tax BIT NOT NULL DEFAULT 0,
    tourism_tax_registration_number NVARCHAR(50) NULL,
    is_active BIT NOT NULL DEFAULT 1,
    rmcd_portal_reference NVARCHAR(100) NULL,
    notes NVARCHAR(500) NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NULL
);

CREATE INDEX IX_company_sst_registration_company_id ON tax.company_sst_registration(company_id);
CREATE INDEX IX_company_sst_registration_company_active ON tax.company_sst_registration(company_id, is_active);

-- Tax Determination Rules (item_category, customer_type, place_of_supply → tax_code)
CREATE TABLE tax.tax_determination_rule (
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    company_id UNIQUEIDENTIFIER NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    item_category NVARCHAR(50) NOT NULL DEFAULT '*',
    customer_type NVARCHAR(50) NOT NULL DEFAULT '*',
    place_of_supply NVARCHAR(50) NOT NULL DEFAULT '*',
    tax_code_id UNIQUEIDENTIFIER NOT NULL,
    priority INT NOT NULL DEFAULT 100,
    is_active BIT NOT NULL DEFAULT 1,
    effective_from DATETIME2 NULL,
    effective_to DATETIME2 NULL,
    description NVARCHAR(250) NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NULL,
    CONSTRAINT FK_tax_determination_rule_tax_code FOREIGN KEY (tax_code_id) REFERENCES tax.tax_code(id)
);

CREATE INDEX IX_tax_determination_rule_company ON tax.tax_determination_rule(company_id);
CREATE INDEX IX_tax_determination_rule_item_category ON tax.tax_determination_rule(item_category);
CREATE INDEX IX_tax_determination_rule_customer_type ON tax.tax_determination_rule(customer_type);
CREATE INDEX IX_tax_determination_rule_place_of_supply ON tax.tax_determination_rule(place_of_supply);
CREATE INDEX IX_tax_determination_rule_company_active_priority ON tax.tax_determination_rule(company_id, is_active, priority);
CREATE INDEX IX_tax_determination_rule_tax_code ON tax.tax_determination_rule(tax_code_id);

-- Seed default rules: Sales Tax and Service Tax for domestic business customers
INSERT INTO tax.tax_determination_rule (company_id, item_category, customer_type, place_of_supply, tax_code_id, priority, description)
SELECT '00000000-0000-0000-0000-000000000000', 'SERVICES', '*', 'DOMESTIC', tc.id, 50, 'Default: Services → Service Tax 8%'
FROM tax.tax_code tc WHERE tc.code = 'SST-SERVICE-8'
UNION ALL
SELECT '00000000-0000-0000-0000-000000000000', '*', 'BUSINESS', 'DOMESTIC', tc.id, 100, 'Default: Goods (business customer) → Sales Tax 10%'
FROM tax.tax_code tc WHERE tc.code = 'SST-SALES-10'
UNION ALL
SELECT '00000000-0000-0000-0000-000000000000', '*', '*', 'EXPORT', tc.id, 200, 'Default: Exports → Zero-Rated'
FROM tax.tax_code tc WHERE tc.code = 'SST-ZRL';

-- ============================================
-- E-Invoice Schema (int) - Additional tables
-- ============================================

-- Taxpayer Profile (Company e-Invoice setup)
-- Aligned with EF entity: TaxpayerProfile in ERP.Modules.EInvoice
CREATE TABLE int.taxpayer_profile (
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    company_id UNIQUEIDENTIFIER NOT NULL,
    tax_identification_number NVARCHAR(50) NOT NULL,
    business_registration_number NVARCHAR(50) NULL,
    sales_tax_number NVARCHAR(50) NULL,
    service_tax_number NVARCHAR(50) NULL,
    msic_code NVARCHAR(20) NULL,
    address NVARCHAR(200) NULL,
    city NVARCHAR(100) NULL,
    state NVARCHAR(100) NULL,
    postcode NVARCHAR(20) NULL,
    is_subject_to_einvoice BIT NOT NULL DEFAULT 0,
    created_at DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    updated_at DATETIMEOFFSET NULL,
    CONSTRAINT UQ_taxpayer_profile_company UNIQUE (company_id)
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

-- e-Invoice Customer Extension (TIN, ID type, SST for buyers)
CREATE TABLE ar.einvoice_customer_extension (
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    customer_id UNIQUEIDENTIFIER NOT NULL,
    tax_identification_number NVARCHAR(50) NULL,
    id_type NVARCHAR(20) NULL,
    id_value NVARCHAR(50) NULL,
    sst_registration_number NVARCHAR(50) NULL,
    sst_billing_address NVARCHAR(200) NULL,
    sst_billing_city NVARCHAR(100) NULL,
    sst_billing_state NVARCHAR(100) NULL,
    sst_billing_postal_code NVARCHAR(20) NULL,
    sst_billing_country NVARCHAR(3) NULL DEFAULT 'MY',
    is_subject_to_einvoice BIT NOT NULL DEFAULT 0,
    created_by NVARCHAR(256) NOT NULL,
    created_on DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    modified_by NVARCHAR(256) NULL,
    modified_on DATETIMEOFFSET NULL,
    deleted_by NVARCHAR(256) NULL,
    deleted_on DATETIMEOFFSET NULL
);

CREATE INDEX IX_einvoice_customer_extension_customer ON ar.einvoice_customer_extension(customer_id);
CREATE INDEX IX_einvoice_customer_extension_tin ON ar.einvoice_customer_extension(tax_identification_number) WHERE tax_identification_number IS NOT NULL;

-- e-Invoice Vendor Extension (TIN, ID type, SST for suppliers, self-billing)
CREATE TABLE ap.einvoice_vendor_extension (
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    vendor_id UNIQUEIDENTIFIER NOT NULL,
    tax_identification_number NVARCHAR(50) NULL,
    id_type NVARCHAR(20) NULL,
    id_value NVARCHAR(50) NULL,
    sst_registration_number NVARCHAR(50) NULL,
    sst_billing_address NVARCHAR(200) NULL,
    sst_billing_city NVARCHAR(100) NULL,
    sst_billing_state NVARCHAR(100) NULL,
    sst_billing_postal_code NVARCHAR(20) NULL,
    sst_billing_country NVARCHAR(3) NULL DEFAULT 'MY',
    is_subject_to_einvoice BIT NOT NULL DEFAULT 0,
    can_issue_self_billed BIT NOT NULL DEFAULT 0,
    created_by NVARCHAR(256) NOT NULL,
    created_on DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    modified_by NVARCHAR(256) NULL,
    modified_on DATETIMEOFFSET NULL,
    deleted_by NVARCHAR(256) NULL,
    deleted_on DATETIMEOFFSET NULL
);

CREATE INDEX IX_einvoice_vendor_extension_vendor ON ap.einvoice_vendor_extension(vendor_id);
CREATE INDEX IX_einvoice_vendor_extension_tin ON ap.einvoice_vendor_extension(tax_identification_number) WHERE tax_identification_number IS NOT NULL;

-- API Credential (Client ID/Secret per environment for MyInvois auth)
CREATE TABLE int.api_credential (
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    company_id UNIQUEIDENTIFIER NOT NULL,
    environment NVARCHAR(20) NOT NULL,
    client_id NVARCHAR(200) NOT NULL,
    encrypted_client_secret NVARCHAR(500) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    expires_at DATETIME2 NULL,
    last_rotated_at DATETIME2 NULL,
    description NVARCHAR(250) NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NULL
);

CREATE INDEX IX_api_credential_company_env ON int.api_credential(company_id, environment);
CREATE INDEX IX_api_credential_active ON int.api_credential(is_active);

-- e-Invoice Status History (audit trail for document lifecycle)
CREATE TABLE int.einvoice_status_history (
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    einvoice_document_id UNIQUEIDENTIFIER NOT NULL,
    previous_status NVARCHAR(50) NULL,
    new_status NVARCHAR(50) NOT NULL,
    actor_user_id NVARCHAR(100) NOT NULL DEFAULT 'system',
    reason NVARCHAR(500) NULL,
    lhdn_error_code NVARCHAR(50) NULL,
    lhdn_error_message NVARCHAR(500) NULL,
    myinvois_uuid NVARCHAR(100) NULL,
    validation_link NVARCHAR(500) NULL,
    occurred_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_status_history_document FOREIGN KEY (einvoice_document_id) REFERENCES int.einvoice_document(id)
);

CREATE INDEX IX_einvoice_status_history_document ON int.einvoice_status_history(einvoice_document_id);
CREATE INDEX IX_einvoice_status_history_occurred ON int.einvoice_status_history(occurred_at);
CREATE INDEX IX_einvoice_status_history_doc_occurred ON int.einvoice_status_history(einvoice_document_id, occurred_at);

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