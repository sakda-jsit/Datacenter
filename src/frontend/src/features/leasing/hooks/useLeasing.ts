import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { leasingApi } from '../services/leasingApi'
import type { LeaseContractInput } from '../types/leasing.types'

const keys = {
  list: (companyId: number, includeInactive: boolean) => ['lease-contracts', companyId, includeInactive] as const,
  detail: (id: number, companyId: number, year: number) => ['lease-contract', id, companyId, year] as const,
  workpaper: (companyId: number, year: number) => ['lease-workpaper', companyId, year] as const,
}

export function useLeaseContracts(companyId: number, includeInactive = false, enabled = true) {
  return useQuery({
    queryKey: keys.list(companyId, includeInactive),
    queryFn: () => leasingApi.list(companyId, includeInactive),
    enabled: enabled && companyId > 0,
  })
}

export function useLeaseContract(id: number, companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.detail(id, companyId, year),
    queryFn: () => leasingApi.get(id, companyId, year),
    enabled: enabled && companyId > 0 && id > 0,
  })
}

export function useLeaseWorkpaper(companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.workpaper(companyId, year),
    queryFn: () => leasingApi.getWorkpaper(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

function useInvalidate(companyId: number) {
  const qc = useQueryClient()
  return () => {
    qc.invalidateQueries({ queryKey: ['lease-contracts', companyId] })
    qc.invalidateQueries({ queryKey: ['lease-workpaper', companyId] })
    qc.invalidateQueries({ queryKey: ['lease-contract'] })
  }
}

export function useCreateLeaseContract(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (data: LeaseContractInput) => leasingApi.create(companyId, data),
    onSuccess: invalidate,
  })
}

export function useUpdateLeaseContract(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: LeaseContractInput }) =>
      leasingApi.update(id, companyId, data),
    onSuccess: invalidate,
  })
}

export function useDeleteLeaseContract(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (id: number) => leasingApi.remove(id, companyId),
    onSuccess: invalidate,
  })
}

export function useGenerateLeaseAdjustment(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: { fiscalYear: number; contractIds: number[]; entryDate?: string | null }) =>
      leasingApi.generateAdjustment({ clientCompanyId: companyId, ...input }),
    onSuccess: (_d, vars) => {
      qc.invalidateQueries({ queryKey: ['adjustments', companyId, vars.fiscalYear] })
      qc.invalidateQueries({ queryKey: ['adjusted-trial-balance', companyId, vars.fiscalYear] })
    },
  })
}
