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
import VatPage from '../features/vat/pages/VatPage'
import ArPage from '../features/ar/pages/ArPage'
import ApPage from '../features/ap/pages/ApPage'
import PayrollPage from '../features/payroll/pages/PayrollPage'
import BankReconciliationPage from '../features/bank-reconciliation/pages/BankReconciliationPage'
import GeneralLedgerPage from '../features/general-ledger/pages/GeneralLedgerPage'
import TaxReportPage from '../features/tax-report/pages/TaxReportPage'
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
          <Route path="vat" element={<VatPage />} />
          <Route path="ar" element={<ArPage />} />
          <Route path="ap" element={<ApPage />} />
          <Route path="payroll" element={<PayrollPage />} />
          <Route path="bank-reconciliation" element={<BankReconciliationPage />} />
          <Route path="general-ledger" element={<GeneralLedgerPage />} />
          <Route path="tax-report" element={<TaxReportPage />} />
          <Route path="pnd50" element={<Pnd50Page />} />
          <Route path="compliance" element={<ComplianceCalendarRoute />} />
          <Route path="closing-period" element={<ClosingPeriodPage />} />
          <Route path="audit-log" element={<AuditLogPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}
