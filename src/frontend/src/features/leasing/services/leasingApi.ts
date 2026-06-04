import apiClient from '../../../shared/services/apiClient'
import type { AdjustmentEntryDto } from '../../adjustments/types/adjustment.types'
import type {
  LeaseContract,
  LeaseContractDetail,
  LeaseContractInput,
  LeaseContractListItem,
  LeaseWorkpaper,
} from '../types/leasing.types'

export const leasingApi = {
  list: (clientCompanyId: number, includeInactive = false) =>
    apiClient
      .get<LeaseContractListItem[]>('/leasing', { params: { clientCompanyId, includeInactive } })
      .then((r) => r.data),

  get: (id: number, clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<LeaseContractDetail>(`/leasing/${id}`, { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  getWorkpaper: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<LeaseWorkpaper>('/leasing/workpaper', { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  create: (clientCompanyId: number, data: LeaseContractInput) =>
    apiClient.post<LeaseContract>('/leasing', { clientCompanyId, data }).then((r) => r.data),

  update: (id: number, clientCompanyId: number, data: LeaseContractInput) =>
    apiClient.put<LeaseContract>(`/leasing/${id}`, { id, clientCompanyId, data }).then((r) => r.data),

  remove: (id: number, clientCompanyId: number) =>
    apiClient.delete(`/leasing/${id}`, { params: { clientCompanyId } }).then((r) => r.data),

  generateAdjustment: (input: {
    clientCompanyId: number
    fiscalYear: number
    contractIds: number[]
    entryDate?: string | null
  }) =>
    apiClient.post<AdjustmentEntryDto>('/leasing/generate-adjustment', input).then((r) => r.data),
}
