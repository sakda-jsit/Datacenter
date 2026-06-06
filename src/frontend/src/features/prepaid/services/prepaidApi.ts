import apiClient from '../../../shared/services/apiClient'
import type { AdjustmentEntryDto } from '../../adjustments/types/adjustment.types'
import type {
  PrepaidDetail,
  PrepaidExpense,
  PrepaidExpenseInput,
  PrepaidListItem,
  PrepaidWorkpaper,
} from '../types/prepaid.types'

export const prepaidApi = {
  list: (clientCompanyId: number, includeInactive = false) =>
    apiClient
      .get<PrepaidListItem[]>('/prepaid', { params: { clientCompanyId, includeInactive } })
      .then((r) => r.data),

  get: (id: number, clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<PrepaidDetail>(`/prepaid/${id}`, { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  getWorkpaper: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<PrepaidWorkpaper>('/prepaid/workpaper', { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  create: (clientCompanyId: number, data: PrepaidExpenseInput) =>
    apiClient.post<PrepaidExpense>('/prepaid', { clientCompanyId, data }).then((r) => r.data),

  update: (id: number, clientCompanyId: number, data: PrepaidExpenseInput) =>
    apiClient.put<PrepaidExpense>(`/prepaid/${id}`, { id, clientCompanyId, data }).then((r) => r.data),

  remove: (id: number, clientCompanyId: number) =>
    apiClient.delete(`/prepaid/${id}`, { params: { clientCompanyId } }).then((r) => r.data),

  generateAdjustment: (input: {
    clientCompanyId: number
    fiscalYear: number
    prepaidIds: number[]
    entryDate?: string | null
  }) => apiClient.post<AdjustmentEntryDto>('/prepaid/generate-adjustment', input).then((r) => r.data),
}
