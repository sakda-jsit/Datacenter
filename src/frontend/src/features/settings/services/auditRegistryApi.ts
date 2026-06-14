import apiClient from '../../../shared/services/apiClient'

// ── ทะเบียนผู้สอบบัญชี / ผู้ทำบัญชี (master ค่ากลางของสำนักงาน) ──

export const AuditorType = { Cpa: 1, TaxAuditor: 2 } as const
export const AUDITOR_TYPE_LABEL: Record<number, string> = {
  1: 'CPA (ผู้สอบบัญชีรับอนุญาต)',
  2: 'TA (ผู้สอบบัญชีภาษีอากร)',
}

export interface Auditor {
  id: number
  name: string
  type: number
  licenseNo?: string | null
  taxId?: string | null
  auditFirmName?: string | null
  auditFirmTaxId?: string | null
  isActive: boolean
}
export type AuditorInput = Omit<Auditor, 'id'>

export interface Bookkeeper {
  id: number
  name: string
  taxId?: string | null
  isActive: boolean
}
export type BookkeeperInput = Omit<Bookkeeper, 'id'>

export const auditorsApi = {
  list: () => apiClient.get<Auditor[]>('/auditors').then((r) => r.data),
  create: (d: AuditorInput) => apiClient.post<{ id: number }>('/auditors', d).then((r) => r.data),
  update: (id: number, d: AuditorInput) => apiClient.put(`/auditors/${id}`, d).then((r) => r.data),
  remove: (id: number) => apiClient.delete(`/auditors/${id}`).then((r) => r.data),
}

export const bookkeepersApi = {
  list: () => apiClient.get<Bookkeeper[]>('/bookkeepers').then((r) => r.data),
  create: (d: BookkeeperInput) => apiClient.post<{ id: number }>('/bookkeepers', d).then((r) => r.data),
  update: (id: number, d: BookkeeperInput) => apiClient.put(`/bookkeepers/${id}`, d).then((r) => r.data),
  remove: (id: number) => apiClient.delete(`/bookkeepers/${id}`).then((r) => r.data),
}
