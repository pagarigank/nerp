import { Routes, Route, Navigate } from 'react-router-dom'
import { Suspense } from 'react'
import { useAuth } from '@stores/authStore'
import { MainLayout } from '@layouts/MainLayout'
import { LoginPage } from '@pages/auth/LoginPage'
import { DashboardPage } from '@pages/DashboardPage'
import { NotFoundPage } from '@pages/NotFoundPage'
import { LoadingSpinner } from '@components/ui/LoadingSpinner'
import { ArLayout } from '@pages/ar/ArLayout'
import { CustomersPage } from '@pages/ar/CustomersPage'
import { InvoiceBatchesPage } from '@pages/ar/InvoiceBatchesPage'
import { InvoiceBatchDetailPage } from '@pages/ar/InvoiceBatchDetailPage'
import { CashReceiptsPage } from '@pages/ar/CashReceiptsPage'
import { StatementsPage } from '@pages/ar/StatementsPage'
import { FinanceChargesPage } from '@pages/ar/FinanceChargesPage'
import { CreditMemosPage } from '@pages/ar/CreditMemosPage'
import { CreditLimitPage } from '@pages/ar/CreditLimitPage'
import { ArReportsPage } from '@pages/ar/ArReportsPage'
import { CollectionsPage } from '@pages/ar/CollectionsPage'
import { DunningPage } from '@pages/ar/DunningPage'
import { AllowancePage } from '@pages/ar/AllowancePage'
import { ResaleCertificatesPage } from '@pages/ar/ResaleCertificatesPage'
import { CreditMemoApplyPage } from '@pages/ar/CreditMemoApplyPage'
import { AgingByBasisPage } from '@pages/ar/AgingByBasisPage'
import { CashReceiptMatchPage } from '@pages/ar/CashReceiptMatchPage'
import { PlatformLayout } from '@pages/platform/PlatformLayout'
import { CompaniesPage } from '@pages/platform/CompaniesPage'
import { FiscalPeriodsPage } from '@pages/platform/FiscalPeriodsPage'
import { AccountsPage } from '@pages/platform/AccountsPage'
import { SegmentTypesPage } from '@pages/platform/SegmentTypesPage'
import { SegmentValuesPage } from '@pages/platform/SegmentValuesPage'
import { UsersPage } from '@pages/platform/UsersPage'
import { RolesPage } from '@pages/platform/RolesPage'
import { AuditLogsPage } from '@pages/platform/AuditLogsPage'
import { CurrenciesPage } from '@pages/platform/CurrenciesPage'
import { ExchangeRatesPage } from '@pages/platform/ExchangeRatesPage'
import { NumberSequencesPage } from '@pages/platform/NumberSequencesPage'
import { ApprovalWorkflowPage } from '@pages/platform/ApprovalWorkflowPage'
import { PeriodCloseWizardPage } from '@pages/platform/PeriodCloseWizardPage'
import { ApiKeysPage } from '@pages/platform/ApiKeysPage'
import { ApprovalDelegationsPage } from '@pages/platform/ApprovalDelegationsPage'
import { HolidayCalendarPage } from '@pages/platform/HolidayCalendarPage'
import { SoDPage } from '@pages/platform/SoDPage'
import { PlatformReportsPage } from '@pages/platform/PlatformReportsPage'
import { ConsolidationRunPage } from '@pages/gl/ConsolidationRunPage'
import { GlLayout } from '@pages/gl/GlLayout'
import { JournalBatchesPage } from '@pages/gl/JournalBatchesPage'
import { JournalBatchDetailPage } from '@pages/gl/JournalBatchDetailPage'
import { RecurringTemplatesPage } from '@pages/gl/RecurringTemplatesPage'
import { AllocationRulesPage } from '@pages/gl/AllocationRulesPage'
import { BudgetsPage } from '@pages/gl/BudgetsPage'
import { GlReportsPage } from '@pages/gl/GlReportsPage'
import { RevaluationPage as GlRevaluationPage } from '@pages/gl/RevaluationPage'
import { YearEndClosePage } from '@pages/gl/YearEndClosePage'
import { PostingSuspensePage } from '@pages/gl/PostingSuspensePage'
import { AccountInquiryPage } from '@pages/gl/AccountInquiryPage'
import { PrePostingReviewPage } from '@pages/gl/PrePostingReviewPage'
import { PeriodEndChecklistPage } from '@pages/gl/PeriodEndChecklistPage'
import { BudgetRollForwardPage } from '@pages/gl/BudgetRollForwardPage'
import { ApLayout } from '@pages/ap/ApLayout'
import { VendorsPage } from '@pages/ap/VendorsPage'
import { PaymentTermsPage } from '@pages/ap/PaymentTermsPage'
import { VoucherBatchesPage } from '@pages/ap/VoucherBatchesPage'
import { VoucherBatchDetailPage } from '@pages/ap/VoucherBatchDetailPage'
import { PaymentsPage } from '@pages/ap/PaymentsPage'
import { ApReportsPage } from '@pages/ap/ApReportsPage'
import { ThreeWayMatchPage } from '@pages/ap/ThreeWayMatchPage'
import { BackupWithholdingPage } from '@pages/ap/BackupWithholdingPage'
import { Form1099Page } from '@pages/ap/Form1099Page'
import { MatchExceptionPage } from '@pages/ap/MatchExceptionPage'
import { Form1099ProcessingPage } from '@pages/ap/Form1099ProcessingPage'
import { DuplicateInvoicePage } from '@pages/ap/DuplicateInvoicePage'
import { VendorW9Page } from '@pages/ap/VendorW9Page'
import { BankVerificationPage } from '@pages/ap/BankVerificationPage'
import { CashDiscountPage } from '@pages/ap/CashDiscountPage'
import { EscheatmentPage } from '@pages/ap/EscheatmentPage'
import { GrirAccrualPage } from '@pages/ap/GrirAccrualPage'
import { VendorStatementPage } from '@pages/ap/VendorStatementPage'
import { CashLayout } from '@pages/cash/CashLayout'
import { BankAccountsPage } from '@pages/cash/BankAccountsPage'
import { DepositsPage } from '@pages/cash/DepositsPage'
import { BankStatementsPage } from '@pages/cash/BankStatementsPage'
import { ReconciliationsPage } from '@pages/cash/ReconciliationsPage'
import { ReconciliationWorkspacePage } from '@pages/cash/ReconciliationWorkspacePage'
import { BankTransfersPage } from '@pages/cash/BankTransfersPage'
import { BankFeesPage } from '@pages/cash/BankFeesPage'
import { NsfPage } from '@pages/cash/NsfPage'
import { CashReportsPage } from '@pages/cash/CashReportsPage'
import { BankGlMappingPage } from '@pages/cash/BankGlMappingPage'
import { LockboxPage } from '@pages/cash/LockboxPage'
import { StaleCheckEscheatPage } from '@pages/cash/StaleCheckEscheatPage'
import { PositivePayPage } from '@pages/cash/PositivePayPage'
import { BankFeeAnalysisPage } from '@pages/cash/BankFeeAnalysisPage'
import { CashForecastHorizonPage } from '@pages/cash/CashForecastHorizonPage'
import { OutstandingDepositsPage } from '@pages/cash/OutstandingDepositsPage'
import { PurchasingLayout } from '@pages/purchasing/PurchasingLayout'
import { RequisitionsPage } from '@pages/purchasing/RequisitionsPage'
import { PurchaseOrdersPage } from '@pages/purchasing/PurchaseOrdersPage'
import { VendorQuotesPage } from '@pages/purchasing/VendorQuotesPage'
import { ApprovalQueuePage } from '@pages/purchasing/ApprovalQueuePage'
import { ReceiptsPage } from '@pages/purchasing/ReceiptsPage'
import { POTemplatesPage } from '@pages/purchasing/POTemplatesPage'
import { RequisitionTemplatesPage } from '@pages/purchasing/RequisitionTemplatesPage'
import { VendorItemsPage } from '@pages/purchasing/VendorItemsPage'
import { BuyerAgentsPage } from '@pages/purchasing/BuyerAgentsPage'
import { ShippingMethodsPage } from '@pages/purchasing/ShippingMethodsPage'
import { FOBTermsPage } from '@pages/purchasing/FOBTermsPage'
import { PurchasingReportsPage } from '@pages/purchasing/PurchasingReportsPage'
import { VendorCrud } from '@components/VendorCrud'
import { InventoryLayout } from '@pages/inventory/InventoryLayout'
import { ItemsPage } from '@pages/inventory/ItemsPage'
import { ItemCategoriesPage } from '@pages/inventory/ItemCategoriesPage'
import { WarehousesPage } from '@pages/inventory/WarehousesPage'
import { WarehouseBinsPage } from '@pages/inventory/WarehouseBinsPage'
import { StockPage } from '@pages/inventory/StockPage'
import { TransactionsPage } from '@pages/inventory/TransactionsPage'
import { ReservationsPage } from '@pages/inventory/ReservationsPage'
import { QuarantinePage } from '@pages/inventory/QuarantinePage'
import { ExpirationPage } from '@pages/inventory/ExpirationPage'
import { RevaluationPage } from '@pages/inventory/RevaluationPage'
import { LandedCostPage } from '@pages/inventory/LandedCostPage'
import { LandedCostAllocationsPage } from '@pages/inventory/LandedCostAllocationsPage'
import { CycleCountsPage } from '@pages/inventory/CycleCountsPage'
import { PhysicalCountsPage } from '@pages/inventory/PhysicalCountsPage'
import { NegativeOverridesPage } from '@pages/inventory/NegativeOverridesPage'
import { MovementsPage } from '@pages/inventory/MovementsPage'
import { ReorderSuggestionsPage } from '@pages/inventory/ReorderSuggestionsPage'
import { InventoryReportsPage } from '@pages/inventory/InventoryReportsPage'
import { SubstitutionsPage } from '@pages/inventory/SubstitutionsPage'
import { KitsPage } from '@pages/inventory/KitsPage'
import { ConsignmentPage } from '@pages/inventory/ConsignmentPage'
import { PutAwayPickingPage } from '@pages/inventory/PutAwayPickingPage'
import { StockByLocationPage } from '@pages/inventory/StockByLocationPage'
import { CycleCountSchedulePage } from '@pages/inventory/CycleCountSchedulePage'
import { ScrapPage } from '@pages/inventory/ScrapPage'
import { StockCardPage } from '@pages/inventory/StockCardPage'
import { GlTieOutPage } from '@pages/inventory/GlTieOutPage'
import { UomConversionsPage } from '@pages/inventory/UomConversionsPage'
import { UnitOfMeasuresPage } from '@pages/inventory/UnitOfMeasuresPage'
import { OmLayout } from '@pages/om/OmLayout'
import { SalesOrdersPage } from '@pages/om/SalesOrdersPage'
import { SalesOrderFormPage } from '@pages/om/SalesOrderFormPage'
import { SalesOrderDetailPage } from '@pages/om/SalesOrderDetailPage'
import { ShipmentsPage } from '@pages/om/ShipmentsPage'
import { ShipmentDetailPage } from '@pages/om/ShipmentDetailPage'
import { ReturnsPage } from '@pages/om/ReturnsPage'
import { ReturnDetailPage } from '@pages/om/ReturnDetailPage'
import { MastersPage } from '@pages/om/MastersPage'
import { ReportsPage } from '@pages/om/ReportsPage'
import { QuotesPage } from '@pages/om/QuotesPage'
import { BlanketOrdersPage } from '@pages/om/BlanketOrdersPage'
import { SubstitutionOffersPage } from '@pages/om/SubstitutionOffersPage'
import { RtvPage } from '@pages/om/RtvPage'
import { OrderNotesPage } from '@pages/om/OrderNotesPage'
import { OrderStatusDashboardPage } from '@pages/om/OrderStatusDashboardPage'
import { SalesAnalysisPage } from '@pages/om/SalesAnalysisPage'
import { CommissionRunsPage } from '@pages/om/CommissionRunsPage'
import { AtpPage } from '@pages/om/AtpPage'
import { FreightAllocationPage } from '@pages/om/FreightAllocationPage'
import { PickPackShipPage } from '@pages/om/PickPackShipPage'
import { BomsPage } from '@pages/bom/BomsPage'
import { BuildOrdersPage } from '@pages/bom/BuildOrdersPage'
import { WorkCentersPage } from '@pages/bom/WorkCentersPage'
import { RoutingOperationsPage } from '@pages/bom/RoutingOperationsPage'
import { BomReportsPage } from '@pages/bom/BomReportsPage'
import { ProjectsLayout } from '@pages/projects/ProjectsLayout'
import { ProjectsListPage } from '@pages/projects/ProjectsListPage'
import { ProjectOverviewPage } from '@pages/projects/ProjectOverviewPage'
import { ProjectTasksPage } from '@pages/projects/ProjectTasksPage'
import { ProjectBudgetPage } from '@pages/projects/ProjectBudgetPage'
import { ProjectCostsPage } from '@pages/projects/ProjectCostsPage'
import { ProjectBillingPage } from '@pages/projects/ProjectBillingPage'
import { ProjectChangeOrdersPage } from '@pages/projects/ProjectChangeOrdersPage'
import { ProjectAnalysisPage } from '@pages/projects/ProjectAnalysisPage'
import { ProjectsReportsPage } from '@pages/projects/ProjectsReportsPage'
import { PayrollLayout } from '@pages/payroll/PayrollLayout'
import { EmployeesPage } from '@pages/payroll/EmployeesPage'
import { PayCodesPage } from '@pages/payroll/PayCodesPage'
import { UnionPage } from '@pages/payroll/UnionPage'
import { TimesheetsPage } from '@pages/payroll/TimesheetsPage'
import { RunsPage } from '@pages/payroll/RunsPage'
import { ExpensesPage } from '@pages/payroll/ExpensesPage'
import { TaxPage } from '@pages/payroll/TaxPage'
import { DeductionsPage } from '@pages/payroll/DeductionsPage'
import { PtoPage } from '@pages/payroll/PtoPage'
import { ManualChecksPage } from '@pages/payroll/ManualChecksPage'
import { ReportsPage as PayrollReportsPage } from '@pages/payroll/ReportsPage'
import { GarnishmentsPage } from '@pages/payroll/GarnishmentsPage'
import { SetupPage } from '@pages/payroll/SetupPage'
import { FieldServiceLayout } from '@pages/field-service/FieldServiceLayout'
import { WorkOrdersPage } from '@pages/field-service/WorkOrdersPage'
import { DispatchBoardPage } from '@pages/field-service/DispatchBoardPage'
import { TechniciansPage } from '@pages/field-service/TechniciansPage'
import { ContractsPage } from '@pages/field-service/ContractsPage'
import { EquipmentPage } from '@pages/field-service/EquipmentPage'
import { SlasPage } from '@pages/field-service/SlasPage'
import { TerritoriesPage } from '@pages/field-service/TerritoriesPage'
import { RateCardsPage } from '@pages/field-service/RateCardsPage'
import { EstimatesPage } from '@pages/field-service/EstimatesPage'
import { PmPage } from '@pages/field-service/PmPage'
import { VanStockPage } from '@pages/field-service/VanStockPage'
import { WarrantyPage } from '@pages/field-service/WarrantyPage'
import { ReportsPage as FieldServiceReportsPage } from '@pages/field-service/ReportsPage'
import { ReportsLayout } from '@pages/reporting/ReportsLayout'
import { ReportsCatalogPage } from '@pages/reporting/ReportsCatalogPage'
import { ExecutiveDashboardPage } from '@pages/reporting/ExecutiveDashboardPage'
import { ReportViewerPage } from '@pages/reporting/ReportViewerPage'
import StatementDesignerPage from '@pages/reporting/StatementDesignerPage'
import QuickQueryPage from '@pages/reporting/QuickQueryPage'
import DrillBackPage from '@pages/reporting/DrillBackPage'
import ReportSchedulerPage from '@pages/reporting/ReportSchedulerPage'
import { ReportCategoriesPage } from '@pages/reporting/ReportCategoriesPage'
import { ReportParameterSetsPage } from '@pages/reporting/ReportParameterSetsPage'
import { ReportUsagePage } from '@pages/reporting/ReportUsagePage'
import { DataMartStatusPage } from '@pages/reporting/DataMartStatusPage'

function ProtectedRoutes() {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <LoadingSpinner size="lg" />
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <MainLayout />
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/" element={<ProtectedRoutes />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={
          <Suspense fallback={<LoadingSpinner size="lg" />}>
            <DashboardPage />
          </Suspense>
        } />
        
        <Route path="platform" element={<PlatformLayout />}>
          <Route index element={<Navigate to="companies" replace />} />
          <Route path="companies" element={<CompaniesPage />} />
          <Route path="fiscal-periods" element={<FiscalPeriodsPage />} />
          <Route path="accounts" element={<AccountsPage />} />
          <Route path="segment-types" element={<SegmentTypesPage />} />
          <Route path="segment-values" element={<SegmentValuesPage />} />
          <Route path="users" element={<UsersPage />} />
          <Route path="roles" element={<RolesPage />} />
          <Route path="audit-logs" element={<AuditLogsPage />} />
          <Route path="currencies" element={<CurrenciesPage />} />
          <Route path="exchange-rates" element={<ExchangeRatesPage />} />
          <Route path="number-sequences" element={<NumberSequencesPage />} />
          <Route path="approval-workflows" element={<ApprovalWorkflowPage />} />
          <Route path="period-close" element={<PeriodCloseWizardPage />} />
          <Route path="api-keys" element={<ApiKeysPage />} />
          <Route path="approval-delegations" element={<ApprovalDelegationsPage />} />
          <Route path="holiday-calendar" element={<HolidayCalendarPage />} />
          <Route path="sod" element={<SoDPage />} />
          <Route path="reports" element={<PlatformReportsPage />} />
        </Route>
        <Route path="gl" element={<GlLayout />}>
          <Route index element={<Navigate to="journal-batches" replace />} />
          <Route path="journal-batches" element={<JournalBatchesPage />} />
          <Route path="journal-batches/:batchId" element={<JournalBatchDetailPage />} />
          <Route path="recurring-templates" element={<RecurringTemplatesPage />} />
          <Route path="allocation-rules" element={<AllocationRulesPage />} />
          <Route path="budgets" element={<BudgetsPage />} />
          <Route path="revaluation" element={<GlRevaluationPage />} />
          <Route path="reports" element={<GlReportsPage />} />
          <Route path="consolidation" element={<ConsolidationRunPage />} />
          <Route path="account-inquiry" element={<AccountInquiryPage />} />
          <Route path="pre-posting" element={<PrePostingReviewPage />} />
          <Route path="period-end-checklist" element={<PeriodEndChecklistPage />} />
          <Route path="year-end-close" element={<YearEndClosePage />} />
          <Route path="posting-suspense" element={<PostingSuspensePage />} />
          <Route path="budget-rollforward" element={<BudgetRollForwardPage />} />
        </Route>
        <Route path="ap" element={<ApLayout />}>
          <Route index element={<Navigate to="vendors" replace />} />
          <Route path="vendors" element={<VendorsPage />} />
          <Route path="payment-terms" element={<PaymentTermsPage />} />
          <Route path="voucher-batches" element={<VoucherBatchesPage />} />
          <Route path="voucher-batches/:batchId" element={<VoucherBatchDetailPage />} />
          <Route path="payments" element={<PaymentsPage />} />
          <Route path="three-way-match" element={<ThreeWayMatchPage />} />
          <Route path="backup-withholding" element={<BackupWithholdingPage />} />
          <Route path="1099" element={<Form1099Page />} />
          <Route path="match-exceptions" element={<MatchExceptionPage />} />
          <Route path="1099-processing" element={<Form1099ProcessingPage />} />
          <Route path="duplicate-invoice" element={<DuplicateInvoicePage />} />
          <Route path="vendor-w9" element={<VendorW9Page />} />
          <Route path="bank-verification" element={<BankVerificationPage />} />
          <Route path="cash-discount" element={<CashDiscountPage />} />
          <Route path="escheatment" element={<EscheatmentPage />} />
          <Route path="grir-accrual" element={<GrirAccrualPage />} />
          <Route path="vendor-statements" element={<VendorStatementPage />} />
          <Route path="reports" element={<ApReportsPage />} />
        </Route>
        <Route path="ar" element={<ArLayout />}>
          <Route index element={<Navigate to="customers" replace />} />
          <Route path="customers" element={<CustomersPage />} />
          <Route path="invoice-batches" element={<InvoiceBatchesPage />} />
          <Route path="invoice-batches/:batchId" element={<InvoiceBatchDetailPage />} />
          <Route path="cash-receipts" element={<CashReceiptsPage />} />
          <Route path="memos" element={<CreditMemosPage />} />
          <Route path="credit-limit" element={<CreditLimitPage />} />
          <Route path="statements" element={<StatementsPage />} />
          <Route path="finance-charges" element={<FinanceChargesPage />} />
          <Route path="reports" element={<ArReportsPage />} />
          <Route path="collections" element={<CollectionsPage />} />
          <Route path="dunning" element={<DunningPage />} />
          <Route path="allowance" element={<AllowancePage />} />
          <Route path="resale-certificates" element={<ResaleCertificatesPage />} />
          <Route path="credit-memo-apply" element={<CreditMemoApplyPage />} />
          <Route path="aging-by-basis" element={<AgingByBasisPage />} />
          <Route path="cash-receipt-match" element={<CashReceiptMatchPage />} />
        </Route>
        <Route path="cash" element={<CashLayout />}>
          <Route index element={<Navigate to="bank-accounts" replace />} />
          <Route path="bank-accounts" element={<BankAccountsPage />} />
          <Route path="deposits" element={<DepositsPage />} />
          <Route path="bank-statements" element={<BankStatementsPage />} />
          <Route path="reconciliations" element={<ReconciliationsPage />} />
          <Route path="reconciliations/:sessionId" element={<ReconciliationWorkspacePage />} />
          <Route path="transfers" element={<BankTransfersPage />} />
          <Route path="bank-fees" element={<BankFeesPage />} />
          <Route path="nsf" element={<NsfPage />} />
          <Route path="reports" element={<CashReportsPage />} />
          <Route path="gl-mapping" element={<BankGlMappingPage />} />
          <Route path="lockbox" element={<LockboxPage />} />
          <Route path="stale-checks" element={<StaleCheckEscheatPage />} />
          <Route path="positive-pay" element={<PositivePayPage />} />
          <Route path="fee-analysis" element={<BankFeeAnalysisPage />} />
          <Route path="forecast-horizon" element={<CashForecastHorizonPage />} />
          <Route path="outstanding-deposits" element={<OutstandingDepositsPage />} />
        </Route>
        <Route path="purchasing" element={<PurchasingLayout />}>
          <Route index element={<Navigate to="requisitions" replace />} />
          <Route path="requisitions" element={<RequisitionsPage />} />
          <Route path="purchase-orders" element={<PurchaseOrdersPage />} />
          <Route path="vendor-quotes" element={<VendorQuotesPage />} />
          <Route path="approval-queue" element={<ApprovalQueuePage />} />
          <Route path="receipts" element={<ReceiptsPage />} />
          <Route path="po-templates" element={<POTemplatesPage />} />
          <Route path="requisition-templates" element={<RequisitionTemplatesPage />} />
          <Route path="vendor-items" element={<VendorItemsPage />} />
          <Route path="vendors" element={<VendorCrud />} />
          <Route path="buyer-agents" element={<BuyerAgentsPage />} />
          <Route path="shipping-methods" element={<ShippingMethodsPage />} />
          <Route path="fob-terms" element={<FOBTermsPage />} />
          <Route path="reports" element={<PurchasingReportsPage />} />
        </Route>
        <Route path="inventory" element={<InventoryLayout />}>
          <Route index element={<Navigate to="items" replace />} />
          <Route path="items" element={<ItemsPage />} />
          <Route path="categories" element={<ItemCategoriesPage />} />
          <Route path="warehouses" element={<WarehousesPage />} />
          <Route path="bins" element={<WarehouseBinsPage />} />
          <Route path="stock" element={<StockPage />} />
          <Route path="transactions" element={<TransactionsPage />} />
          <Route path="reservations" element={<ReservationsPage />} />
          <Route path="quarantine" element={<QuarantinePage />} />
          <Route path="expiration" element={<ExpirationPage />} />
          <Route path="revaluation" element={<RevaluationPage />} />
          <Route path="landed-cost" element={<LandedCostPage />} />
          <Route path="landed-cost-allocations" element={<LandedCostAllocationsPage />} />
          <Route path="cycle-counts" element={<CycleCountsPage />} />
          <Route path="physical-counts" element={<PhysicalCountsPage />} />
          <Route path="negative-overrides" element={<NegativeOverridesPage />} />
          <Route path="movements" element={<MovementsPage />} />
          <Route path="reorder" element={<ReorderSuggestionsPage />} />
          <Route path="reports" element={<InventoryReportsPage />} />
          <Route path="substitutions" element={<SubstitutionsPage />} />
          <Route path="kits" element={<KitsPage />} />
          <Route path="consignment" element={<ConsignmentPage />} />
          <Route path="put-away-picking" element={<PutAwayPickingPage />} />
          <Route path="stock-by-location" element={<StockByLocationPage />} />
          <Route path="cycle-count-schedule" element={<CycleCountSchedulePage />} />
          <Route path="stock-card" element={<StockCardPage />} />
          <Route path="uom-conversions" element={<UomConversionsPage />} />
          <Route path="uoms" element={<UnitOfMeasuresPage />} />
          <Route path="scrap" element={<ScrapPage />} />
          <Route path="gl-tie-out" element={<GlTieOutPage />} />
        </Route>
        <Route path="om" element={<OmLayout />}>
          <Route index element={<Navigate to="sales-orders" replace />} />
          <Route path="sales-orders" element={<SalesOrdersPage />} />
          <Route path="sales-orders/new" element={<SalesOrderFormPage />} />
          <Route path="sales-orders/:id" element={<SalesOrderDetailPage />} />
          <Route path="shipments" element={<ShipmentsPage />} />
          <Route path="shipments/new" element={<ShipmentsPage />} />
          <Route path="shipments/:id" element={<ShipmentDetailPage />} />
          <Route path="returns" element={<ReturnsPage />} />
          <Route path="returns/new" element={<ReturnsPage />} />
          <Route path="returns/:id" element={<ReturnDetailPage />} />
          <Route path="quotes" element={<QuotesPage />} />
          <Route path="blanket-orders" element={<BlanketOrdersPage />} />
          <Route path="substitution-offers" element={<SubstitutionOffersPage />} />
          <Route path="rtv" element={<RtvPage />} />
          <Route path="order-notes" element={<OrderNotesPage />} />
          <Route path="order-dashboard" element={<OrderStatusDashboardPage />} />
          <Route path="sales-analysis" element={<SalesAnalysisPage />} />
          <Route path="commissions" element={<CommissionRunsPage />} />
          <Route path="atp" element={<AtpPage />} />
          <Route path="freight" element={<FreightAllocationPage />} />
          <Route path="pick-pack-ship" element={<PickPackShipPage />} />
          <Route path="reports" element={<ReportsPage />} />
          <Route path="masters" element={<MastersPage />} />
        </Route>
        <Route path="bom/*">
          <Route index element={<BomsPage />} />
          <Route path="work-centers" element={<WorkCentersPage />} />
          <Route path="routing-operations" element={<RoutingOperationsPage />} />
          <Route path="build-orders" element={<BuildOrdersPage />} />
          <Route path="reports" element={<BomReportsPage />} />
        </Route>
        <Route path="projects" element={<ProjectsLayout />}>
          <Route index element={<ProjectsListPage />} />
          <Route path="overview" element={<ProjectOverviewPage />} />
          <Route path="tasks" element={<ProjectTasksPage />} />
          <Route path="budget" element={<ProjectBudgetPage />} />
          <Route path="costs" element={<ProjectCostsPage />} />
          <Route path="billing" element={<ProjectBillingPage />} />
          <Route path="change-orders" element={<ProjectChangeOrdersPage />} />
          <Route path="analysis" element={<ProjectAnalysisPage />} />
          <Route path="reports" element={<ProjectsReportsPage />} />
        </Route>
        <Route path="payroll" element={<PayrollLayout />}>
          <Route index element={<Navigate to="employees" replace />} />
          <Route path="employees" element={<EmployeesPage />} />
          <Route path="paycodes" element={<PayCodesPage />} />
          <Route path="union" element={<UnionPage />} />
          <Route path="timesheets" element={<TimesheetsPage />} />
          <Route path="runs" element={<RunsPage />} />
          <Route path="expenses" element={<ExpensesPage />} />
          <Route path="tax" element={<TaxPage />} />
          <Route path="deductions" element={<DeductionsPage />} />
          <Route path="pto" element={<PtoPage />} />
          <Route path="manual" element={<ManualChecksPage />} />
          <Route path="reports" element={<PayrollReportsPage />} />
          <Route path="garnishments" element={<GarnishmentsPage />} />
          <Route path="setup" element={<SetupPage />} />
        </Route>
        <Route path="field-service" element={<FieldServiceLayout />}>
          <Route index element={<Navigate to="work-orders" replace />} />
          <Route path="work-orders" element={<WorkOrdersPage />} />
          <Route path="dispatch" element={<DispatchBoardPage />} />
          <Route path="technicians" element={<TechniciansPage />} />
          <Route path="contracts" element={<ContractsPage />} />
          <Route path="equipment" element={<EquipmentPage />} />
          <Route path="slas" element={<SlasPage />} />
          <Route path="territories" element={<TerritoriesPage />} />
          <Route path="rate-cards" element={<RateCardsPage />} />
          <Route path="estimates" element={<EstimatesPage />} />
          <Route path="pm" element={<PmPage />} />
          <Route path="van-stock" element={<VanStockPage />} />
          <Route path="warranty" element={<WarrantyPage />} />
          <Route path="reports" element={<FieldServiceReportsPage />} />
        </Route>
        <Route path="reporting" element={<ReportsLayout />}>
          <Route index element={<Navigate to="catalog" replace />} />
          <Route path="catalog" element={<ReportsCatalogPage />} />
          <Route path="executive" element={<ExecutiveDashboardPage />} />
          <Route path="viewer" element={<ReportViewerPage />} />
          <Route path="designer" element={<StatementDesignerPage />} />
          <Route path="quick-query" element={<QuickQueryPage />} />
          <Route path="drill-back" element={<DrillBackPage />} />
          <Route path="scheduler" element={<ReportSchedulerPage />} />
          <Route path="categories" element={<ReportCategoriesPage />} />
          <Route path="parameter-sets" element={<ReportParameterSetsPage />} />
          <Route path="usage" element={<ReportUsagePage />} />
          <Route path="sync-status" element={<DataMartStatusPage />} />
        </Route>
        <Route path="integration/*" element={<div className="p-8 text-center"><h2 className="text-2xl font-bold">Integration</h2><p className="mt-2 text-gray-500">Coming soon...</p></div>} />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App
