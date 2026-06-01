import apiClient from '../../../shared/services/apiClient'
import type { PaginatedResult } from '../../../shared/types/api.types'
import type { ClientDetailDto, ClientListDto, CreateClientRequest, UpdateClientRequest } from '../types/client.types'

export const clientApi = {
  getList: (params: { pageNumber?: number; pageSize?: number; search?: string }) =>
    apiClient.get<PaginatedResult<ClientListDto>>('/clients', { params }).then((r) => r.data),

  getById: (id: number) =>
    apiClient.get<ClientDetailDto>(`/clients/${id}`).then((r) => r.data),

  create: (data: CreateClientRequest) =>
    apiClient.post<{ id: number }>('/clients', data).then((r) => r.data),

  update: (id: number, data: UpdateClientRequest) =>
    apiClient.put(`/clients/${id}`, data),

  deactivate: (id: number) =>
    apiClient.delete(`/clients/${id}`),
}
