import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { prepaidApi } from '../services/prepaidApi'
import type { PrepaidExpenseInput } from '../types/prepaid.types'

const keys = {
  list: (companyId: number, includeInactive: boolean) => ['prepaid-list', companyId, includeInactive] as const,
  detail: (id: number, companyId: number, year: number) => ['prepaid-detail', id, companyId, year] as const,
  workpaper: (companyId: number, year: number) => ['prepaid-workpaper', companyId, year] as const,
}

export function usePrepaidList(companyId: number, includeInactive = false, enabled = true) {
  return useQuery({
    queryKey: keys.list(companyId, includeInactive),
    queryFn: () => prepaidApi.list(companyId, includeInactive),
    enabled: enabled && companyId > 0,
  })
}

export function usePrepaidDetail(id: number, companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.detail(id, companyId, year),
    queryFn: () => prepaidApi.get(id, companyId, year),
    enabled: enabled && companyId > 0 && id > 0,
  })
}

export function usePrepaidWorkpaper(companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.workpaper(companyId, year),
    queryFn: () => prepaidApi.getWorkpaper(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

function useInvalidate(companyId: number) {
  const qc = useQueryClient()
  return () => {
    qc.invalidateQueries({ queryKey: ['prepaid-list', companyId] })
    qc.invalidateQueries({ queryKey: ['prepaid-workpaper', companyId] })
    qc.invalidateQueries({ queryKey: ['prepaid-detail'] })
  }
}

export function useCreatePrepaid(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (data: PrepaidExpenseInput) => prepaidApi.create(companyId, data),
    onSuccess: invalidate,
  })
}

export function useUpdatePrepaid(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: PrepaidExpenseInput }) => prepaidApi.update(id, companyId, data),
    onSuccess: invalidate,
  })
}

export function useDeletePrepaid(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (id: number) => prepaidApi.remove(id, companyId),
    onSuccess: invalidate,
  })
}

export function useGeneratePrepaidAdjustment(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: { fiscalYear: number; prepaidIds: number[]; entryDate?: string | null }) =>
      prepaidApi.generateAdjustment({ clientCompanyId: companyId, ...input }),
    onSuccess: (_d, vars) => {
      qc.invalidateQueries({ queryKey: ['adjustments', companyId, vars.fiscalYear] })
      qc.invalidateQueries({ queryKey: ['adjusted-trial-balance', companyId, vars.fiscalYear] })
    },
  })
}
