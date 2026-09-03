-- Digital Certificate for XAdES signing
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'digital_certificate' AND schema_id = SCHEMA_ID('int'))
BEGIN
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
END
GO

-- e-Invoice Customer Extension
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'einvoice_customer_extension' AND schema_id = SCHEMA_ID('ar'))
BEGIN
    CREATE TABLE ar.einvoice_customer_extension (
        id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        customer_id UNIQUEIDENTIFIER NOT NULL,
        company_id UNIQUEIDENTIFIER NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
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
        created_by NVARCHAR(256) NOT NULL DEFAULT 'system',
        created_on DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        modified_by NVARCHAR(256) NULL,
        modified_on DATETIMEOFFSET NULL,
        deleted_by NVARCHAR(256) NULL,
        deleted_on DATETIMEOFFSET NULL
    );
    CREATE INDEX IX_einvoice_customer_extension_customer ON ar.einvoice_customer_extension(customer_id);
    CREATE INDEX IX_einvoice_customer_extension_tin ON ar.einvoice_customer_extension(tax_identification_number);
END
GO

-- e-Invoice Vendor Extension
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'einvoice_vendor_extension' AND schema_id = SCHEMA_ID('ap'))
BEGIN
    CREATE TABLE ap.einvoice_vendor_extension (
        id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        vendor_id UNIQUEIDENTIFIER NOT NULL,
        company_id UNIQUEIDENTIFIER NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
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
        created_by NVARCHAR(256) NOT NULL DEFAULT 'system',
        created_on DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        modified_by NVARCHAR(256) NULL,
        modified_on DATETIMEOFFSET NULL,
        deleted_by NVARCHAR(256) NULL,
        deleted_on DATETIMEOFFSET NULL
    );
    CREATE INDEX IX_einvoice_vendor_extension_vendor ON ap.einvoice_vendor_extension(vendor_id);
    CREATE INDEX IX_einvoice_vendor_extension_tin ON ap.einvoice_vendor_extension(tax_identification_number);
END
GO

-- API Credential
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'api_credential' AND schema_id = SCHEMA_ID('int'))
BEGIN
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
END
GO

-- e-Invoice Status History
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'einvoice_status_history' AND schema_id = SCHEMA_ID('int'))
BEGIN
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
END
GO

-- Seed tax codes if not already present
IF NOT EXISTS (SELECT 1 FROM tax.tax_code WHERE code = 'SST-SALES-5')
BEGIN
    INSERT INTO tax.tax_code (code, description, lhdnt_tax_type) VALUES
    ('SST-SALES-5', 'Sales Tax 5% Rate', '01'),
    ('SST-SALES-10', 'Sales Tax 10% Rate', '01'),
    ('SST-SERVICE-6', 'Service Tax 6% (Reduced)', '02'),
    ('SST-SERVICE-8', 'Service Tax 8% Standard', '02'),
    ('SST-TOURISM', 'Tourism Tax', '03'),
    ('SST-HVG', 'High-Value Goods Tax', '04'),
    ('SST-ZRL', 'Zero-Rated Supply', 'ZRL'),
    ('SST-EXEMPT', 'Exempt Supply', 'E');

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
END
GO

-- Seed default tax determination rules
IF NOT EXISTS (SELECT 1 FROM tax.tax_determination_rule WHERE description = 'Default: Services')
BEGIN
    INSERT INTO tax.tax_determination_rule (company_id, item_category, customer_type, place_of_supply, tax_code_id, priority, description)
    SELECT '00000000-0000-0000-0000-000000000000', 'SERVICES', '*', 'DOMESTIC', tc.id, 50, 'Default: Services'
    FROM tax.tax_code tc WHERE tc.code = 'SST-SERVICE-8';
END
GO
