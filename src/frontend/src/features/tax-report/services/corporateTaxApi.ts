import apiClient from '../../../shared/services/apiClient'
import type {
  CompanyAuditor,
  CompanyAuditorInput,
  TaxComputation,
  TaxComputationInput,
} from '../types/corporateTax.types'

const BASE = '/corporate-tax'

export const corporateTaxApi = {
  getComputation: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<TaxComputation>(`${BASE}/computation`, { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  saveComputation: (clientCompanyId: number, fiscalYear: number, data: TaxComputationInput) =>
    apiClient
      .put<TaxComputation>(`${BASE}/computation`, data, { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  // แบบ ภ.ง.ด.50 (PDF ฟอร์มราชการ) — เฟส A หน้า 1+2
  pnd50Pdf: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get(`${BASE}/pnd50-pdf`, { params: { clientCompanyId, fiscalYear }, responseType: 'blob' })
      .then((r) => r.data as Blob),

  // ผู้ตรวจสอบและรับรองบัญชี (ต่อรอบปี)
  getAuditor: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<CompanyAuditor>(`${BASE}/auditor`, { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  saveAuditor: (clientCompanyId: number, fiscalYear: number, data: CompanyAuditorInput) =>
    apiClient
      .put<CompanyAuditor>(`${BASE}/auditor`, data, { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),
}
