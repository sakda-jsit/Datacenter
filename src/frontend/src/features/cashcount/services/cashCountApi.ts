import apiClient from '../../../shared/services/apiClient'
import type { AdjustmentEntryDto } from '../../adjustments/types/adjustment.types'
import type {
  CashCount,
  CashCountInput,
  CashCountListItem,
  CashCountWorkpaper,
} from '../types/cashcount.types'

export const cashCountApi = {
  list: (clientCompanyId: number, fiscalYear?: number, includeInactive = false) =>
    apiClient
      .get<CashCountListItem[]>('/cash-count', { params: { clientCompanyId, fiscalYear, includeInactive } })
      .then((r) => r.data),

  get: (id: number, clientCompanyId: number) =>
    apiClient.get<CashCount>(`/cash-count/${id}`, { params: { clientCompanyId } }).then((r) => r.data),

  getWorkpaper: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<CashCountWorkpaper>('/cash-count/workpaper', { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  create: (clientCompanyId: number, data: CashCountInput) =>
    apiClient.post<CashCount>('/cash-count', { clientCompanyId, data }).then((r) => r.data),

  update: (id: number, clientCompanyId: number, data: CashCountInput) =>
    apiClient.put<CashCount>(`/cash-count/${id}`, { id, clientCompanyId, data }).then((r) => r.data),

  remove: (id: number, clientCompanyId: number) =>
    apiClient.delete(`/cash-count/${id}`, { params: { clientCompanyId } }).then((r) => r.data),

  generateAdjustment: (input: {
    clientCompanyId: number
    fiscalYear: number
    cashCountIds: number[]
    counterpartAccountId: number
    entryDate?: string | null
  }) => apiClient.post<AdjustmentEntryDto>('/cash-count/generate-adjustment', input).then((r) => r.data),
}
