import apiClient from '../../../shared/services/apiClient'
import type { WhtEntryItem, WhtReport, WhtSendResult } from '../types/wht.types'

export const whtApi = {
  report: (clientCompanyId: number, year: number) =>
    apiClient
      .get<WhtReport>('/wht/report', { params: { clientCompanyId, year } })
      .then((r) => r.data),

  years: (clientCompanyId: number) =>
    apiClient.get<number[]>('/wht/years', { params: { clientCompanyId } }).then((r) => r.data),

  entries: (clientCompanyId: number, year: number, month = 0, formType?: number) =>
    apiClient
      .get<WhtEntryItem[]>('/wht', { params: { clientCompanyId, year, month, formType } })
      .then((r) => r.data),

  // PDF หนังสือรับรอง (preview) — ดึงเป็น blob (ต้องแนบ JWT ผ่าน axios)
  certificate: (clientCompanyId: number, entryIds: number[]) =>
    apiClient
      .get('/wht/certificate', {
        params: { clientCompanyId, entryIds },
        paramsSerializer: { indexes: null }, // entryIds=1&entryIds=2
        responseType: 'blob',
      })
      .then((r) => r.data as Blob),

  setPayeeEmail: (clientCompanyId: number, taxId: string, email: string | null) =>
    apiClient
      .put('/wht/payee-email', { clientCompanyId, taxId, email })
      .then((r) => r.data),

  send: (clientCompanyId: number, entryIds: number[]) =>
    apiClient
      .post<WhtSendResult[]>('/wht/send', { clientCompanyId, entryIds })
      .then((r) => r.data),
}
