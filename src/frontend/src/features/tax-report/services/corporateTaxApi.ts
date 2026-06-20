import apiClient from '../../../shared/services/apiClient'
import type {
  Cit50MappingItem,
  Cit50MappingView,
  CompanyDefaultSignersInput,
  CompanySigners,
  CompanyYearSignersInput,
  SignerAssignment,
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

  // ภาพรวมมอบหมายผู้ลงนามของทุกบริษัท (จัดการรวมศูนย์)
  getSignerAssignments: (params: { search?: string; auditorId?: number; bookkeeperId?: number }) =>
    apiClient
      .get<SignerAssignment[]>(`${BASE}/signer-assignments`, { params })
      .then((r) => r.data),

  // แมพบัญชี → บรรทัด CIT50 schedule (4=ต้นทุนผลิต, 5=รายได้อื่น, 6=รายจ่ายอื่น, 8=ขายและบริหาร)
  getCit50Mapping: (clientCompanyId: number, fiscalYear: number, scheduleNo = 8) =>
    apiClient
      .get<Cit50MappingView>(`${BASE}/cit50-mapping`, { params: { clientCompanyId, fiscalYear, scheduleNo } })
      .then((r) => r.data),

  // scope = prefix ของแท็บที่บันทึก (R4_/R5_/R6_/R8/BS_) — ล้าง mapping เฉพาะ scope นั้น
  saveCit50Mapping: (clientCompanyId: number, items: Cit50MappingItem[], scope?: string) =>
    apiClient
      .put(`${BASE}/cit50-mapping`, items, { params: { clientCompanyId, scope } })
      .then((r) => r.data),

  // แมพบัญชี → บรรทัดงบดุล ภ.ง.ด.50 (รายการ 9) — บันทึกผ่าน saveCit50Mapping เดิม
  getCit50BsMapping: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<Cit50MappingView>(`${BASE}/cit50-bs-mapping`, { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),
}
