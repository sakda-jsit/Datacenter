import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { payrollRatesApi } from '../services/payrollRatesApi'
import type { PayrollRateConfigInput } from '../../payroll/types/payroll.types'

const KEY = ['payroll-rates'] as const

export function usePayrollRates() {
  return useQuery({ queryKey: KEY, queryFn: payrollRatesApi.list })
}

export function useSavePayrollRate() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { id: number | null; data: PayrollRateConfigInput }) =>
      vars.id ? payrollRatesApi.update(vars.id, vars.data) : payrollRatesApi.create(vars.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useDeletePayrollRate() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => payrollRatesApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}
