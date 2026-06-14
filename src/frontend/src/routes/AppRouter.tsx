import { Routes, Route, Navigate } from 'react-router-dom'
import AppLayout from '../shared/components/layout/AppLayout'
import ProtectedRoute from './ProtectedRoute'
import LoginPage from '../features/auth/LoginPage'
import ClientListPage from '../features/clients/pages/ClientListPage'
import ClientFormPage from '../features/clients/pages/ClientFormPage'
import ImportPage from '../features/import/pages/ImportPage'
import ImportValidationPage from '../features/import/pages/ImportValidationPage'
import TrialBalancePage from '../features/trial-balance/pages/TrialBalancePage'
import FinancialStatementPage from '../features/financial-statement/pages/FinancialStatementPage'
import AdjustmentsPage from '../features/adjustments/pages/AdjustmentsPage'
import LeasingPage from '../features/leasing/pages/LeasingPage'
import FixedAssetsPage from '../features/fixed-assets/pages/FixedAssetsPage'
import PrepaidPage from '../features/prepaid/pages/PrepaidPage'
import CashCountPage from '../features/cashcount/pages/CashCountPage'
import InterestIncomePage from '../features/interest-income/pages/InterestIncomePage'
import SubsequentPaymentPage from '../features/subsequent-payment/pages/SubsequentPaymentPage'
import VatPage from '../features/vat/pages/VatPage'
import WhtPage from '../features/wht/pages/WhtPage'
import ArPage from '../features/ar/pages/ArPage'
import ApPage from '../features/ap/pages/ApPage'
import StockPage from '../features/stock/pages/StockPage'
import ReportPackagesPage from '../features/report-packages/pages/ReportPackagesPage'
import EvidencePage from '../features/attachments/pages/EvidencePage'
import PayrollPage from '../features/payroll/pages/PayrollPage'
import PayrollRatesPage from '../features/settings/pages/PayrollRatesPage'
import OfficeProfilePage from '../features/settings/pages/OfficeProfilePage'
import AuditorsPage from '../features/settings/pages/AuditorsPage'
import BookkeepersPage from '../features/settings/pages/BookkeepersPage'
import BankReconciliationPage from '../features/bank-reconciliation/pages/BankReconciliationPage'
import GeneralLedgerPage from '../features/general-ledger/pages/GeneralLedgerPage'
import Pnd50Page from '../features/tax-report/pages/Pnd50Page'
import ComplianceCalendarRoute from '../features/compliance-calendar/pages/ComplianceCalendarRoute'
import ClosingPeriodPage from '../features/closing-period/pages/ClosingPeriodPage'
import AuditLogPage from '../features/audit-log/pages/AuditLogPage'
import DashboardPage from '../features/dashboard/pages/DashboardPage'

export default function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="clients" element={<ClientListPage />} />
          <Route path="clients/new" element={<ClientFormPage />} />
          <Route path="clients/:id/edit" element={<ClientFormPage />} />
          <Route path="import" element={<ImportPage />} />
          <Route path="import/:id/validation" element={<ImportValidationPage />} />
          <Route path="trial-balance" element={<TrialBalancePage />} />
          <Route path="financial-statement" element={<FinancialStatementPage />} />
          <Route path="adjustments" element={<AdjustmentsPage />} />
          <Route path="leasing" element={<LeasingPage />} />
          <Route path="fixed-assets" element={<FixedAssetsPage />} />
          <Route path="prepaid" element={<PrepaidPage />} />
          <Route path="cash-count" element={<CashCountPage />} />
          <Route path="interest-income" element={<InterestIncomePage />} />
          <Route path="subsequent-payment" element={<SubsequentPaymentPage />} />
          <Route path="vat" element={<VatPage />} />
          <Route path="wht" element={<WhtPage />} />
          <Route path="ar" element={<ArPage />} />
          <Route path="ap" element={<ApPage />} />
          <Route path="stock" element={<StockPage />} />
          <Route path="payroll" element={<PayrollPage />} />
          <Route path="bank-reconciliation" element={<BankReconciliationPage />} />
          <Route path="general-ledger" element={<GeneralLedgerPage />} />
          <Route path="pnd50" element={<Pnd50Page />} />
          <Route path="compliance" element={<ComplianceCalendarRoute />} />
          <Route path="closing-period" element={<ClosingPeriodPage />} />
          <Route path="report-packages" element={<ReportPackagesPage />} />
          <Route path="evidence" element={<EvidencePage />} />
          <Route path="audit-log" element={<AuditLogPage />} />
          <Route path="settings/payroll-rates" element={<PayrollRatesPage />} />
          <Route path="settings/office-profile" element={<OfficeProfilePage />} />
          <Route path="settings/auditors" element={<AuditorsPage />} />
          <Route path="settings/bookkeepers" element={<BookkeepersPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}
