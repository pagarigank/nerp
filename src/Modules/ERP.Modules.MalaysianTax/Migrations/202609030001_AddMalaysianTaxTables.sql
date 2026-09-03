-- <copyright file="202609030001_AddMalaysianTaxTables.cs" company="ERP Project">
-- Copyright (c) ERP Project. All rights reserved.
-- </copyright>

 migrationBuilder createschema tax -- for tax_codes and tax_rates tables
 migrationBuilder createschema einv -- for e-invoice tables

-- Tax Schema Tables
CREATE TABLE tax.tax_codes (
    id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    company_id UNIQUEIDENTIFIER NOT NULL,
    code NVARCHAR(20) NOT NULL,
    description NVARCHAR(200) NOT NULL,
    ldhn_tax_type NVARCHAR(10) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_on DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    created_by NVARCHAR(256) NOT NULL,
    modified_on DATETIME2,
    modified_by NVARCHAR(256),
    deleted_on DATETIME2,
    deleted_by NVARCHAR(256),
    CONSTRAINT PK_tax_codes PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IX_tax_codes_company_code ON tax.tax_codes (company_id, code);

CREATE TABLE tax.tax_rates (
    id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    company_id UNIQUEIDENTIFIER NOT NULL,
    tax_code_id UNIQUEIDENTIFIER NOT NULL,
    rate DECIMAL(5, 2) NOT NULL,
    effective_from DATETIME2 NOT NULL,
    effective_to DATETIME2,
    is_active BIT NOT NULL DEFAULT 1,
    created_on DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    created_by NVARCHAR(256) NOT NULL,
    modified_on DATETIME2,
    modified_by NVARCHAR(256),
    deleted_on DATETIME2,
    deleted_by NVARCHAR(256),
    CONSTRAINT PK_tax_rates PRIMARY KEY (id),
    CONSTRAINT FK_tax_rates_tax_codes FOREIGN KEY (tax_code_id) REFERENCES tax.tax_codes(id) ON DELETE RESTRICT
);

CREATE INDEX IX_tax_rates_company_code_date ON tax.tax_rates (company_id, tax_code_id, effective_from);

-- eInvoice Schema Tables
CREATE TABLE einv.taxpayer_profiles (
    id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    company_id UNIQUEIDENTIFIER NOT NULL,
    tin NVARCHAR(50) NOT NULL,
    brn NVARCHAR(50),
    sst_registration_number NVARCHAR(50),
    msic_code NVARCHAR(20),
    registered_address NVARCHAR(500),
    contact_number NVARCHAR(50),
    created_by NVARCHAR(256) NOT NULL,
    created_on DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    modified_on DATETIME2,
    modified_by NVARCHAR(256),
    deleted_on DATETIME2,
    deleted_by NVARCHAR(256),
    CONSTRAINT PK_taxpayer_profiles PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IX_taxpayer_profiles_company ON einv.taxpayer_profiles (company_id);

CREATE TABLE einv.e_invoice_documents (
    id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    company_id UNIQUEIDENTIFIER NOT NULL,
    source_document_id UNIQUEIDENTIFIER NOT NULL,
    document_type NVARCHAR(50) NOT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'Queued',
    uuid NVARCHAR(100),
    validation_link NVARCHAR(500),
    payload NVARCHAR(MAX),
    error_message NVARCHAR(MAX),
    submitted_on DATETIME2,
    validated_on DATETIME2,
    cancelled_on DATETIME2,
    cancellation_reason NVARCHAR(MAX),
    expires_on DATETIME2,
    created_on DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    created_by NVARCHAR(256) NOT NULL,
    modified_on DATETIME2,
    modified_by NVARCHAR(256),
    CONSTRAINT PK_e_invoice_documents PRIMARY KEY (id)
);

CREATE INDEX IX_e_invoice_documents_company_status ON einv.e_invoice_documents (company_id, status);
CREATE UNIQUE INDEX IX_e_invoice_documents_company_source ON einv.e_invoice_documents (company_id, source_document_id);
CREATE INDEX IX_e_invoice_documents_validated_on ON einv.e_invoice_documents (validated_on);

CREATE TABLE einv.digital_certificates (
    id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    company_id UNIQUEIDENTIFIER NOT NULL,
    environment NVARCHAR(20) NOT NULL,
    issuer NVARCHAR(200) NOT NULL,
    expiry DATETIME2 NOT NULL,
    encrypted_private_key_ref NVARCHAR(100),
    is_active BIT NOT NULL DEFAULT 1,
    created_on DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    created_by NVARCHAR(256) NOT NULL,
    modified_on DATETIME2,
    modified_by NVARCHAR(256),
    deleted_on DATETIME2,
    deleted_by NVARCHAR(256),
    CONSTRAINT PK_digital_certificates PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IX_digital_certificates_company_env ON einv.digital_certificates (company_id, environment);