import apiClient from '../../../shared/services/apiClient'
import type { AdjustmentEntryDto } from '../../adjustments/types/adjustment.types'
import type {
  InterestLoan,
  InterestLoanDetail,
  InterestLoanInput,
  InterestLoanListItem,
  InterestWorkpaper,
} from '../types/interestincome.types'

export const interestIncomeApi = {
  list: (clientCompanyId: number, includeInactive = false) =>
    apiClient
      .get<InterestLoanListItem[]>('/interest-income', { params: { clientCompanyId, includeInactive } })
      .then((r) => r.data),

  get: (id: number, clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<InterestLoanDetail>(`/interest-income/${id}`, { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  getWorkpaper: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<InterestWorkpaper>('/interest-income/workpaper', { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  create: (clientCompanyId: number, data: InterestLoanInput) =>
    apiClient.post<InterestLoan>('/interest-income', { clientCompanyId, data }).then((r) => r.data),

  update: (id: number, clientCompanyId: number, data: InterestLoanInput) =>
    apiClient.put<InterestLoan>(`/interest-income/${id}`, { id, clientCompanyId, data }).then((r) => r.data),

  remove: (id: number, clientCompanyId: number) =>
    apiClient.delete(`/interest-income/${id}`, { params: { clientCompanyId } }).then((r) => r.data),

  generateAdjustment: (input: {
    clientCompanyId: number
    fiscalYear: number
    loanIds: number[]
    entryDate?: string | null
  }) => apiClient.post<AdjustmentEntryDto>('/interest-income/generate-adjustment', input).then((r) => r.data),
}
