import apiClient from '../../../shared/services/apiClient'
import type {
  ClosingPeriodMonthDto,
  ClosingPeriodOverviewDto,
  ClosingValidationDto,
} from '../types/closingPeriod.types'

export const closingPeriodApi = {
  getPeriods: (clientCompanyId: number, year: number) =>
    apiClient
      .get<ClosingPeriodOverviewDto>('/closing-period', { params: { clientCompanyId, year } })
      .then((r) => r.data),

  getValidation: (clientCompanyId: number, year: number, month: number) =>
    apiClient
      .get<ClosingValidationDto>('/closing-period/validation', {
        params: { clientCompanyId, year, month },
      })
      .then((r) => r.data),

  close: (clientCompanyId: number, year: number, month: number) =>
    apiClient
      .post<ClosingPeriodMonthDto>('/closing-period/close', { clientCompanyId, year, month })
      .then((r) => r.data),

  reopen: (clientCompanyId: number, year: number, month: number, reason?: string) =>
    apiClient
      .post<ClosingPeriodMonthDto>('/closing-period/reopen', { clientCompanyId, year, month, reason })
      .then((r) => r.data),

  lock: (clientCompanyId: number, year: number, month: number) =>
    apiClient
      .post<ClosingPeriodMonthDto>('/closing-period/lock', { clientCompanyId, year, month })
      .then((r) => r.data),
}
