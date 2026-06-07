import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { interestIncomeApi } from '../services/interestIncomeApi'
import type { InterestLoanInput } from '../types/interestincome.types'

const keys = {
  list: (companyId: number) => ['interest-list', companyId] as const,
  detail: (id: number, companyId: number, year: number) => ['interest-detail', id, companyId, year] as const,
  workpaper: (companyId: number, year: number) => ['interest-workpaper', companyId, year] as const,
}

export function useInterestLoanList(companyId: number, enabled = true) {
  return useQuery({
    queryKey: keys.list(companyId),
    queryFn: () => interestIncomeApi.list(companyId),
    enabled: enabled && companyId > 0,
  })
}

export function useInterestLoanDetail(id: number, companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.detail(id, companyId, year),
    queryFn: () => interestIncomeApi.get(id, companyId, year),
    enabled: enabled && companyId > 0 && id > 0,
  })
}

export function useInterestWorkpaper(companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.workpaper(companyId, year),
    queryFn: () => interestIncomeApi.getWorkpaper(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

function useInvalidate(companyId: number) {
  const qc = useQueryClient()
  return () => {
    qc.invalidateQueries({ queryKey: ['interest-list', companyId] })
    qc.invalidateQueries({ queryKey: ['interest-workpaper', companyId] })
    qc.invalidateQueries({ queryKey: ['interest-detail'] })
  }
}

export function useCreateInterestLoan(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (data: InterestLoanInput) => interestIncomeApi.create(companyId, data),
    onSuccess: invalidate,
  })
}

export function useUpdateInterestLoan(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: InterestLoanInput }) => interestIncomeApi.update(id, companyId, data),
    onSuccess: invalidate,
  })
}

export function useDeleteInterestLoan(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (id: number) => interestIncomeApi.remove(id, companyId),
    onSuccess: invalidate,
  })
}

export function useGenerateInterestAdjustment(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: { fiscalYear: number; loanIds: number[]; entryDate?: string | null }) =>
      interestIncomeApi.generateAdjustment({ clientCompanyId: companyId, ...input }),
    onSuccess: (_d, vars) => {
      qc.invalidateQueries({ queryKey: ['adjustments', companyId, vars.fiscalYear] })
      qc.invalidateQueries({ queryKey: ['adjusted-trial-balance', companyId, vars.fiscalYear] })
    },
  })
}
