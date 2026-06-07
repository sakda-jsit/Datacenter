import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { cashCountApi } from '../services/cashCountApi'
import type { CashCountInput } from '../types/cashcount.types'

const keys = {
  list: (companyId: number, year?: number) => ['cashcount-list', companyId, year] as const,
  detail: (id: number, companyId: number) => ['cashcount-detail', id, companyId] as const,
  workpaper: (companyId: number, year: number) => ['cashcount-workpaper', companyId, year] as const,
}

export function useCashCountList(companyId: number, year?: number, enabled = true) {
  return useQuery({
    queryKey: keys.list(companyId, year),
    queryFn: () => cashCountApi.list(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

export function useCashCountDetail(id: number, companyId: number, enabled = true) {
  return useQuery({
    queryKey: keys.detail(id, companyId),
    queryFn: () => cashCountApi.get(id, companyId),
    enabled: enabled && companyId > 0 && id > 0,
  })
}

export function useCashCountWorkpaper(companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.workpaper(companyId, year),
    queryFn: () => cashCountApi.getWorkpaper(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

function useInvalidate(companyId: number) {
  const qc = useQueryClient()
  return () => {
    qc.invalidateQueries({ queryKey: ['cashcount-list', companyId] })
    qc.invalidateQueries({ queryKey: ['cashcount-workpaper', companyId] })
    qc.invalidateQueries({ queryKey: ['cashcount-detail'] })
  }
}

export function useCreateCashCount(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (data: CashCountInput) => cashCountApi.create(companyId, data),
    onSuccess: invalidate,
  })
}

export function useUpdateCashCount(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: CashCountInput }) => cashCountApi.update(id, companyId, data),
    onSuccess: invalidate,
  })
}

export function useDeleteCashCount(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (id: number) => cashCountApi.remove(id, companyId),
    onSuccess: invalidate,
  })
}

export function useGenerateCashCountAdjustment(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: { fiscalYear: number; cashCountIds: number[]; counterpartAccountId: number; entryDate?: string | null }) =>
      cashCountApi.generateAdjustment({ clientCompanyId: companyId, ...input }),
    onSuccess: (_d, vars) => {
      qc.invalidateQueries({ queryKey: ['adjustments', companyId, vars.fiscalYear] })
      qc.invalidateQueries({ queryKey: ['adjusted-trial-balance', companyId, vars.fiscalYear] })
    },
  })
}
