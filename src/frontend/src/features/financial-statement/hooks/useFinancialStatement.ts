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

export function useEquityChanges(
  params: { clientCompanyId: number; fiscalYear: number },
  enabled = true,
) {
  return useQuery({
    queryKey: [FS_KEY, 'equity-changes', params],
    queryFn: () => financialStatementApi.getEquityChanges(params),
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

export function useStatementTaxonomy(clientCompanyId: number, enabled = true) {
  return useQuery({
    queryKey: [FS_KEY, 'taxonomy', clientCompanyId],
    queryFn: () => financialStatementApi.getTaxonomy(clientCompanyId),
    enabled: enabled && clientCompanyId > 0,
  })
}

export function useUnmappedAccounts(
  params: { clientCompanyId: number; fiscalYear: number },
  enabled = true,
) {
  return useQuery({
    queryKey: [FS_KEY, 'unmapped-accounts', params],
    queryFn: () => financialStatementApi.getUnmappedAccounts(params),
    enabled: enabled && params.clientCompanyId > 0,
  })
}

export function useUpsertMapping() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: financialStatementApi.upsertMapping,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [FS_KEY, 'mappings'] })
      qc.invalidateQueries({ queryKey: [FS_KEY, 'unmapped-accounts'] })
    },
  })
}

export function useDeleteMapping() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ clientCompanyId, accountCode }: { clientCompanyId: number; accountCode: string }) =>
      financialStatementApi.deleteMapping(clientCompanyId, accountCode),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [FS_KEY, 'mappings'] })
      qc.invalidateQueries({ queryKey: [FS_KEY, 'unmapped-accounts'] })
    },
  })
}

export function useExternalInputs(
  params: { clientCompanyId: number; fiscalYear: number },
  enabled = true,
) {
  return useQuery({
    queryKey: [FS_KEY, 'external-inputs', params],
    queryFn: () => financialStatementApi.getExternalInputs(params),
    enabled: enabled && params.clientCompanyId > 0,
  })
}

export function useUpsertExternalInput() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: financialStatementApi.upsertExternalInput,
    onSuccess: () => qc.invalidateQueries({ queryKey: [FS_KEY] }),
  })
}

// ── NOTE2 ──

export function useNotesToFs(
  params: { clientCompanyId: number; fiscalYear: number },
  enabled = true,
) {
  return useQuery({
    queryKey: [FS_KEY, 'notes', params],
    queryFn: () => financialStatementApi.getNotes(params),
    enabled: enabled && params.clientCompanyId > 0,
  })
}

export function useNoteTemplates(
  params: { clientCompanyId: number; fiscalYear: number },
  enabled = true,
) {
  return useQuery({
    queryKey: [FS_KEY, 'note-templates', params],
    queryFn: () => financialStatementApi.getNoteTemplates(params),
    enabled: enabled && params.clientCompanyId > 0,
  })
}

export function useUpsertNoteTemplate() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: financialStatementApi.upsertNoteTemplate,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [FS_KEY, 'notes'] })
      qc.invalidateQueries({ queryKey: [FS_KEY, 'note-templates'] })
    },
  })
}

export function useResetNoteTemplate() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: financialStatementApi.resetNoteTemplate,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [FS_KEY, 'notes'] })
      qc.invalidateQueries({ queryKey: [FS_KEY, 'note-templates'] })
    },
  })
}
