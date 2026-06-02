import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { closingPeriodApi } from '../services/closingPeriodApi'

const KEYS = {
  periods: (id: number, year: number) => ['closing-period', 'periods', id, year] as const,
  validation: (id: number, year: number, month: number) =>
    ['closing-period', 'validation', id, year, month] as const,
}

export function useClosingPeriods(clientCompanyId: number, year: number) {
  return useQuery({
    queryKey: KEYS.periods(clientCompanyId, year),
    queryFn: () => closingPeriodApi.getPeriods(clientCompanyId, year),
    enabled: clientCompanyId > 0,
  })
}

export function useClosingValidation(clientCompanyId: number, year: number, month: number | null) {
  return useQuery({
    queryKey: KEYS.validation(clientCompanyId, year, month ?? 0),
    queryFn: () => closingPeriodApi.getValidation(clientCompanyId, year, month as number),
    enabled: clientCompanyId > 0 && month != null && month >= 1 && month <= 12,
  })
}

export function useCloseMutations(clientCompanyId: number, year: number) {
  const qc = useQueryClient()
  const invalidate = () =>
    qc.invalidateQueries({ queryKey: ['closing-period'] })

  const close = useMutation({
    mutationFn: (month: number) => closingPeriodApi.close(clientCompanyId, year, month),
    onSuccess: invalidate,
  })

  const reopen = useMutation({
    mutationFn: ({ month, reason }: { month: number; reason?: string }) =>
      closingPeriodApi.reopen(clientCompanyId, year, month, reason),
    onSuccess: invalidate,
  })

  const lock = useMutation({
    mutationFn: (month: number) => closingPeriodApi.lock(clientCompanyId, year, month),
    onSuccess: invalidate,
  })

  return { close, reopen, lock }
}
