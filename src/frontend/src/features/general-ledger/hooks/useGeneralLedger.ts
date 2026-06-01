import { useQuery } from '@tanstack/react-query'
import { generalLedgerApi } from '../services/generalLedgerApi'
import type { GeneralLedgerParams } from '../types/generalLedger.types'

export function useGeneralLedger(params: GeneralLedgerParams, enabled = true) {
  return useQuery({
    queryKey: ['general-ledger', params],
    queryFn: () => generalLedgerApi.getReport(params),
    enabled: enabled && params.clientCompanyId > 0,
  })
}
