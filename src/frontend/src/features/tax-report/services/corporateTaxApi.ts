import apiClient from '../../../shared/services/apiClient'
import type {
  CompanyDefaultSignersInput,
  CompanySigners,
  CompanyYearSignersInput,
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

  // ผู้ลงนาม (ค่าเริ่มต้นบริษัท + override รายปี + resolved)
  getSigners: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<CompanySigners>(`${BASE}/signers`, { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  setDefaultSigners: (clientCompanyId: number, data: CompanyDefaultSignersInput) =>
    apiClient
      .put<CompanySigners>(`${BASE}/signers/default`, data, { params: { clientCompanyId } })
      .then((r) => r.data),

  saveYearSigners: (clientCompanyId: number, fiscalYear: number, data: CompanyYearSignersInput) =>
    apiClient
      .put<CompanySigners>(`${BASE}/signers/year`, data, { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),
}
