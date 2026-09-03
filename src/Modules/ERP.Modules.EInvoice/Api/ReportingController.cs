// <copyright file="ReportingController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

// NOTE: This file should be placed in ERP.Api/Controllers/
// It's documented here in the EInvoice module for API documentation purposes

namespace ERP.Modules.EInvoice.Api;

/// <summary>
/// Reporting endpoints for e-Invoice and SST reporting.
/// Routes:
/// - GET /api/v1/reports/e-invoice-status - Submission status report
/// - GET /api/v1/reports/invalid-documents - Invalid/Rejected documents
/// - GET /api/v1/reports/reconciliation - ERP vs MyInvois gap detection
/// - GET /api/v1/reports/certificate-expiry - Certificate expiry alerts
/// </summary>
public static class ReportingControllerRoutes
{
    // Route constants for reporting endpoints
    public const string EInvoiceStatus = "api/v1/reports/e-invoice-status";
    public const string InvalidDocuments = "api/v1/reports/invalid-documents";
    public const string Reconciliation = "api/v1/reports/reconciliation";
    public const string CertificateExpiry = "api/v1/reports/certificate-expiry";
}