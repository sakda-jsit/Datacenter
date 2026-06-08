import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { bankApi } from '../services/bankApi'

export function useBankAccounts(companyId: number, enabled = true) {
  return useQuery({
    queryKey: ['bank-accounts', companyId],
    queryFn: () => bankApi.accounts(companyId),
    enabled: enabled && companyId > 0,
  })
}

export function useBankBook(companyId: number, bankAccountCode: string, year: number, enabled = true) {
  return useQuery({
    queryKey: ['bank-book', companyId, bankAccountCode, year],
    queryFn: () => bankApi.book(companyId, bankAccountCode, year),
    enabled: enabled && companyId > 0 && !!bankAccountCode,
  })
}

export function useBankYears(companyId: number, enabled = true) {
  return useQuery({
    queryKey: ['bank-years', companyId],
    queryFn: () => bankApi.years(companyId),
    enabled: enabled && companyId > 0,
  })
}

// ── Reconciliation ──
export function useBankStatementImports(companyId: number, bankAccountId?: number, enabled = true) {
  return useQuery({
    queryKey: ['bank-stmt-imports', companyId, bankAccountId ?? 0],
    queryFn: () => bankApi.imports(companyId, bankAccountId),
    enabled: enabled && companyId > 0,
  })
}

export function useBankReconciliation(companyId: number, importId: number, enabled = true) {
  return useQuery({
    queryKey: ['bank-recon', companyId, importId],
    queryFn: () => bankApi.reconciliation(companyId, importId),
    enabled: enabled && companyId > 0 && importId > 0,
  })
}

export function useReconMutations(companyId: number) {
  const qc = useQueryClient()
  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ['bank-stmt-imports', companyId] })
    qc.invalidateQueries({ queryKey: ['bank-recon', companyId] })
  }
  return {
    match: useMutation({
      mutationFn: (v: { importId: number; statementLineId: number; bankTransactionId: number }) =>
        bankApi.match(v.importId, companyId, v.statementLineId, v.bankTransactionId),
      onSuccess: invalidate,
    }),
    unmatch: useMutation({
      mutationFn: (v: { importId: number; statementLineId: number }) =>
        bankApi.unmatch(v.importId, companyId, v.statementLineId),
      onSuccess: invalidate,
    }),
    remove: useMutation({
      mutationFn: (importId: number) => bankApi.deleteImport(importId, companyId),
      onSuccess: invalidate,
    }),
    generateAdjustment: useMutation({
      mutationFn: (input: { importId: number; fiscalYear: number; statementLineIds: number[]; bankGlAccountId: number; counterpartAccountId: number }) =>
        bankApi.generateAdjustment({ clientCompanyId: companyId, ...input }),
      onSuccess: (_d, v) => {
        invalidate()
        qc.invalidateQueries({ queryKey: ['adjustments', companyId, v.fiscalYear] })
        qc.invalidateQueries({ queryKey: ['adjusted-trial-balance', companyId, v.fiscalYear] })
      },
    }),
  }
}
