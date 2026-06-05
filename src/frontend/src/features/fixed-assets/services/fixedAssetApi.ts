import apiClient from '../../../shared/services/apiClient'
import type { AdjustmentEntryDto } from '../../adjustments/types/adjustment.types'
import type {
  AssetAccountMapping,
  AssetAccountMappingInput,
  AssetType,
  FixedAsset,
  FixedAssetDetail,
  FixedAssetInput,
  FixedAssetList,
  FixedAssetWorkpaper,
} from '../types/fixedAsset.types'

export const fixedAssetApi = {
  assetTypes: () =>
    apiClient.get<AssetType[]>('/fixed-assets/asset-types').then((r) => r.data),

  list: (clientCompanyId: number, includeInactive = false) =>
    apiClient
      .get<FixedAssetList>('/fixed-assets', { params: { clientCompanyId, includeInactive } })
      .then((r) => r.data),

  get: (id: number, clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<FixedAssetDetail>(`/fixed-assets/${id}`, { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  getWorkpaper: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<FixedAssetWorkpaper>('/fixed-assets/workpaper', { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  create: (clientCompanyId: number, data: FixedAssetInput) =>
    apiClient.post<FixedAsset>('/fixed-assets', { clientCompanyId, data }).then((r) => r.data),

  update: (id: number, clientCompanyId: number, data: FixedAssetInput) =>
    apiClient.put<FixedAsset>(`/fixed-assets/${id}`, { id, clientCompanyId, data }).then((r) => r.data),

  remove: (id: number, clientCompanyId: number) =>
    apiClient.delete(`/fixed-assets/${id}`, { params: { clientCompanyId } }).then((r) => r.data),

  generateAdjustment: (input: {
    clientCompanyId: number
    fiscalYear: number
    assetIds: number[]
    set: number
    entryDate?: string | null
  }) =>
    apiClient.post<AdjustmentEntryDto>('/fixed-assets/generate-adjustment', input).then((r) => r.data),

  generateDisposal: (input: {
    clientCompanyId: number
    fiscalYear: number
    assetIds: number[]
    gainAccountId: number
    lossAccountId: number
    proceedsAccountId?: number | null
    entryDate?: string | null
  }) =>
    apiClient.post<AdjustmentEntryDto>('/fixed-assets/generate-disposal', input).then((r) => r.data),

  getAccountMappings: (clientCompanyId: number) =>
    apiClient
      .get<AssetAccountMapping[]>('/fixed-assets/account-mappings', { params: { clientCompanyId } })
      .then((r) => r.data),

  upsertAccountMappings: (clientCompanyId: number, mappings: AssetAccountMappingInput[]) =>
    apiClient
      .put<AssetAccountMapping[]>('/fixed-assets/account-mappings', { clientCompanyId, mappings })
      .then((r) => r.data),
}
