import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { adjustmentApi } from '../services/adjustmentApi'
import type { CreateAdjustmentEntryInput, UpdateAdjustmentEntryInput } from '../types/adjustment.types'

const keys = {
  list: (companyId: number, year: number) => ['adjustments', companyId, year] as const,
  tb: (companyId: number, year: number, includeZero: boolean) =>
    ['adjusted-trial-balance', companyId, year, includeZero] as const,
}

export function useAdjustmentEntries(companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.list(companyId, year),
    queryFn: () => adjustmentApi.list(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

export function useAdjustedTrialBalance(
  companyId: number,
  year: number,
  includeZero: boolean,
  enabled = true,
) {
  return useQuery({
    queryKey: keys.tb(companyId, year, includeZero),
    queryFn: () => adjustmentApi.getTrialBalance(companyId, year, includeZero),
    enabled: enabled && companyId > 0,
  })
}

function useInvalidate(companyId: number, year: number) {
  const qc = useQueryClient()
  return () => {
    qc.invalidateQueries({ queryKey: ['adjustments', companyId, year] })
    qc.invalidateQueries({ queryKey: ['adjusted-trial-balance', companyId, year] })
  }
}

export function useCreateAdjustment(companyId: number, year: number) {
  const invalidate = useInvalidate(companyId, year)
  return useMutation({
    mutationFn: (input: CreateAdjustmentEntryInput) => adjustmentApi.create(input),
    onSuccess: invalidate,
  })
}

export function useUpdateAdjustment(companyId: number, year: number) {
  const invalidate = useInvalidate(companyId, year)
  return useMutation({
    mutationFn: (input: UpdateAdjustmentEntryInput) => adjustmentApi.update(input),
    onSuccess: invalidate,
  })
}

export function useDeleteAdjustment(companyId: number, year: number) {
  const invalidate = useInvalidate(companyId, year)
  return useMutation({
    mutationFn: (id: number) => adjustmentApi.remove(id, companyId),
    onSuccess: invalidate,
  })
}
