import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { financialStatementApi } from '../services/financialStatementApi'

const FS_KEY = 'financial-statement'

export function useBalanceSheet(
  params: { clientCompanyId: number; fiscalYear: number },
  enabled = true,
) {
  return useQuery({
    queryKey: [FS_KEY, 'balance-sheet', params],
    queryFn: () => financialStatementApi.getBalanceSheet(params),
    enabled: enabled && params.clientCompanyId > 0,
  })
}

export function useProfitLoss(
  params: { clientCompanyId: number; fiscalYear: number; monthFrom?: number; monthTo?: number },
  enabled = true,
) {
  return useQuery({
    queryKey: [FS_KEY, 'profit-loss', params],
    queryFn: () => financialStatementApi.getProfitLoss(params),
    enabled: enabled && params.clientCompanyId > 0,
  })
}

export function useAccountMappings(clientCompanyId: number) {
  return useQuery({
    queryKey: [FS_KEY, 'mappings', clientCompanyId],
    queryFn: () => financialStatementApi.getMappings(clientCompanyId),
    enabled: clientCompanyId > 0,
  })
}

export function useUpsertMapping() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: financialStatementApi.upsertMapping,
    onSuccess: () => qc.invalidateQueries({ queryKey: [FS_KEY, 'mappings'] }),
  })
}

export function useDeleteMapping() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ clientCompanyId, accountCode }: { clientCompanyId: number; accountCode: string }) =>
      financialStatementApi.deleteMapping(clientCompanyId, accountCode),
    onSuccess: () => qc.invalidateQueries({ queryKey: [FS_KEY, 'mappings'] }),
  })
}

export function useUpsertExternalInput() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: financialStatementApi.upsertExternalInput,
    onSuccess: () => qc.invalidateQueries({ queryKey: [FS_KEY] }),
  })
}
