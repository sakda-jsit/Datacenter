import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { clientApi } from '../services/clientApi'
import type { CreateClientRequest, UpdateClientRequest } from '../types/client.types'

const CLIENTS_KEY = 'clients'

export function useClientList(params: { pageNumber?: number; pageSize?: number; search?: string }) {
  return useQuery({
    queryKey: [CLIENTS_KEY, params],
    queryFn: () => clientApi.getList(params),
  })
}

export function useClientDetail(id: number) {
  return useQuery({
    queryKey: [CLIENTS_KEY, id],
    queryFn: () => clientApi.getById(id),
    enabled: id > 0,
  })
}

export function useCreateClient() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateClientRequest) => clientApi.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: [CLIENTS_KEY] }),
  })
}

export function useUpdateClient() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateClientRequest }) => clientApi.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: [CLIENTS_KEY] }),
  })
}

export function useDeactivateClient() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => clientApi.deactivate(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [CLIENTS_KEY] }),
  })
}
