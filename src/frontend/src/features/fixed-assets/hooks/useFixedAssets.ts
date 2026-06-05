import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fixedAssetApi } from '../services/fixedAssetApi'
import type { AssetAccountMappingInput, FixedAssetInput } from '../types/fixedAsset.types'

const keys = {
  types: ['asset-types'] as const,
  list: (companyId: number, includeInactive: boolean) => ['fixed-assets', companyId, includeInactive] as const,
  detail: (id: number, companyId: number, year: number) => ['fixed-asset', id, companyId, year] as const,
  workpaper: (companyId: number, year: number) => ['fixed-asset-workpaper', companyId, year] as const,
  mappings: (companyId: number) => ['asset-account-mappings', companyId] as const,
}

export function useAssetTypes() {
  return useQuery({ queryKey: keys.types, queryFn: fixedAssetApi.assetTypes, staleTime: 5 * 60 * 1000 })
}

export function useFixedAssets(companyId: number, includeInactive = false, enabled = true) {
  return useQuery({
    queryKey: keys.list(companyId, includeInactive),
    queryFn: () => fixedAssetApi.list(companyId, includeInactive),
    enabled: enabled && companyId > 0,
  })
}

export function useFixedAsset(id: number, companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.detail(id, companyId, year),
    queryFn: () => fixedAssetApi.get(id, companyId, year),
    enabled: enabled && companyId > 0 && id > 0,
  })
}

export function useFixedAssetWorkpaper(companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.workpaper(companyId, year),
    queryFn: () => fixedAssetApi.getWorkpaper(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

function useInvalidate(companyId: number) {
  const qc = useQueryClient()
  return () => {
    qc.invalidateQueries({ queryKey: ['fixed-assets', companyId] })
    qc.invalidateQueries({ queryKey: ['fixed-asset-workpaper', companyId] })
    qc.invalidateQueries({ queryKey: ['fixed-asset'] })
  }
}

export function useCreateFixedAsset(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (data: FixedAssetInput) => fixedAssetApi.create(companyId, data),
    onSuccess: invalidate,
  })
}

export function useUpdateFixedAsset(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: FixedAssetInput }) =>
      fixedAssetApi.update(id, companyId, data),
    onSuccess: invalidate,
  })
}

export function useDeleteFixedAsset(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (id: number) => fixedAssetApi.remove(id, companyId),
    onSuccess: invalidate,
  })
}

export function useAssetAccountMappings(companyId: number, enabled = true) {
  return useQuery({
    queryKey: keys.mappings(companyId),
    queryFn: () => fixedAssetApi.getAccountMappings(companyId),
    enabled: enabled && companyId > 0,
  })
}

export function useUpsertAssetAccountMappings(companyId: number) {
  const invalidate = useInvalidate(companyId)
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (mappings: AssetAccountMappingInput[]) => fixedAssetApi.upsertAccountMappings(companyId, mappings),
    onSuccess: () => {
      invalidate()
      qc.invalidateQueries({ queryKey: keys.mappings(companyId) })
    },
  })
}

export function useGenerateDepreciationAdjustment(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: { fiscalYear: number; assetIds: number[]; set: number; entryDate?: string | null }) =>
      fixedAssetApi.generateAdjustment({ clientCompanyId: companyId, ...input }),
    onSuccess: (_d, vars) => {
      qc.invalidateQueries({ queryKey: ['adjustments', companyId, vars.fiscalYear] })
      qc.invalidateQueries({ queryKey: ['adjusted-trial-balance', companyId, vars.fiscalYear] })
    },
  })
}

export function useGenerateDisposalAdjustment(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: {
      fiscalYear: number
      assetIds: number[]
      gainAccountId: number
      lossAccountId: number
      proceedsAccountId?: number | null
      entryDate?: string | null
    }) => fixedAssetApi.generateDisposal({ clientCompanyId: companyId, ...input }),
    onSuccess: (_d, vars) => {
      qc.invalidateQueries({ queryKey: ['adjustments', companyId, vars.fiscalYear] })
      qc.invalidateQueries({ queryKey: ['adjusted-trial-balance', companyId, vars.fiscalYear] })
    },
  })
}
