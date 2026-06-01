import apiClient from '../../../shared/services/apiClient'
import type {
  AccountListDto,
  PeriodStatusDto,
  TrialBalanceParams,
  TrialBalanceReportDto,
} from '../types/trialBalance.types'

export const trialBalanceApi = {
  getReport: (params: TrialBalanceParams) =>
    apiClient
      .get<TrialBalanceReportDto>('/trial-balance', { params })
      .then((r) => r.data),

  getAccounts: (clientCompanyId: number, activeOnly = true) =>
    apiClient
      .get<AccountListDto[]>('/trial-balance/accounts', {
        params: { clientCompanyId, activeOnly },
      })
      .then((r) => r.data),

  getPeriodStatus: (clientCompanyId: number, year: number) =>
    apiClient
      .get<PeriodStatusDto[]>('/trial-balance/periods', {
        params: { clientCompanyId, year },
      })
      .then((r) => r.data),
}
