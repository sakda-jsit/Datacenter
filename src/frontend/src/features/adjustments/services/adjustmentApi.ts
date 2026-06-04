import apiClient from '../../../shared/services/apiClient'
import type {
  AdjustedTrialBalanceReportDto,
  AdjustmentEntryDto,
  CreateAdjustmentEntryInput,
  UpdateAdjustmentEntryInput,
} from '../types/adjustment.types'

export const adjustmentApi = {
  list: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<AdjustmentEntryDto[]>('/adjustments', { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  getTrialBalance: (clientCompanyId: number, fiscalYear: number, includeZeroBalance = false) =>
    apiClient
      .get<AdjustedTrialBalanceReportDto>('/adjustments/trial-balance', {
        params: { clientCompanyId, fiscalYear, includeZeroBalance },
      })
      .then((r) => r.data),

  create: (input: CreateAdjustmentEntryInput) =>
    apiClient.post<AdjustmentEntryDto>('/adjustments', input).then((r) => r.data),

  update: (input: UpdateAdjustmentEntryInput) =>
    apiClient.put<AdjustmentEntryDto>(`/adjustments/${input.id}`, input).then((r) => r.data),

  remove: (id: number, clientCompanyId: number) =>
    apiClient.delete(`/adjustments/${id}`, { params: { clientCompanyId } }).then((r) => r.data),
}
