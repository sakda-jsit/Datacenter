import { useQuery } from '@tanstack/react-query'
import { trialBalanceApi } from '../services/trialBalanceApi'
import type { TrialBalanceParams } from '../types/trialBalance.types'

export function useTrialBalance(params: TrialBalanceParams, enabled = true) {
  return useQuery({
    queryKey: ['trial-balance', params],
    queryFn: () => trialBalanceApi.getReport(params),
    enabled: enabled && params.clientCompanyId > 0,
  })
}

export function useAccountList(clientCompanyId: number) {
  return useQuery({
    queryKey: ['accounts', clientCompanyId],
    queryFn: () => trialBalanceApi.getAccounts(clientCompanyId),
    enabled: clientCompanyId > 0,
  })
}

export function usePeriodStatus(clientCompanyId: number, year: number) {
  return useQuery({
    queryKey: ['periods', clientCompanyId, year],
    queryFn: () => trialBalanceApi.getPeriodStatus(clientCompanyId, year),
    enabled: clientCompanyId > 0,
  })
}
